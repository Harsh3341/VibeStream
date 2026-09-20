using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using VibeStream.Application.Analytics;
using VibeStream.Application.Caching;
using VibeStream.Application.Ingestion;
using VibeStream.Application.Sessions;
using VibeStream.Application.Spotify;
using VibeStream.Application.Vibes;
using VibeStream.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using VibeStream.Infrastructure.Analytics;
using VibeStream.Infrastructure.Caching;
using VibeStream.Infrastructure.Data;
using VibeStream.Infrastructure.Ingestion;
using VibeStream.Infrastructure.Sessions;
using VibeStream.Infrastructure.Spotify;

namespace VibeStream.Infrastructure.DependencyInjection;

/// <summary>
/// The single entry point the composition root uses to wire the Infrastructure layer.
///
/// CONCEPT: keeping this here (rather than in Program.cs) means the API and the Worker both get the
/// same infrastructure with one call, and neither needs to know that Redis is StackExchange or that
/// readiness is checked with a raw connection. Those are Infrastructure's secrets to keep.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		AddDatabase(services, configuration);
		AddRedis(services, configuration);
		AddSpotify(services, configuration);
		AddIngestion(services, configuration);
		AddSessions(services);
		AddVibes(services);
		AddAnalytics(services);
		AddInfrastructureHealthChecks(services);

		return services;
	}

	private static void AddVibes(IServiceCollection services)
	{
		// Pure, stateless rules engines — shared singletons.
		services.AddSingleton<IAudioProfiler, AudioProfiler>();
		services.AddSingleton<IVibeClassifier, VibeClassifier>();
	}

	private static void AddAnalytics(IServiceCollection services)
	{
		// The writer maintains daily rollups.
		services.AddScoped<IDailyStatisticsService, DailyStatisticsService>();

		// The reader is registered as the concrete type, then wrapped by the caching decorator that the
		// rest of the app resolves via IAnalyticsService. This is manual decorator registration: build
		// the inner service, hand it to the wrapper.
		services.AddScoped<AnalyticsService>();
		services.AddScoped<IAnalyticsService>(provider => new CachingAnalyticsService(
			provider.GetRequiredService<AnalyticsService>(),
			provider.GetRequiredService<ICacheService>()));

		// Bumps a user's cache version after ingestion.
		services.AddScoped<IAnalyticsCacheInvalidator, AnalyticsCacheInvalidator>();
	}

	private static void AddSessions(IServiceCollection services)
	{
		// The detector is pure and stateless — one shared instance is fine.
		services.AddSingleton<ISessionDetector, SessionDetector>();

		// The build service touches the scoped DbContext.
		services.AddScoped<ISessionBuildService, SessionBuildService>();
	}

	private static void AddIngestion(IServiceCollection services, IConfiguration configuration)
	{
		// Bind the loop tunables (poll interval, token buffer). No ValidateOnStart — the defaults are
		// sensible, so a missing section just means "use defaults".
		services.AddOptions<IngestionOptions>()
			.Bind(configuration.GetSection(IngestionOptions.SectionName));

		// Scoped: both use the scoped DbContext. Resolved inside a per-cycle scope by the worker.
		services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();
		services.AddScoped<IListeningIngestionService, ListeningIngestionService>();
	}

	private static void AddSpotify(IServiceCollection services, IConfiguration configuration)
	{
		// Bind the "Spotify" section onto SpotifyOptions, then guard the values we can't run without.
		// ValidateOnStart moves the failure from "first request that needs Spotify throws a confusing
		// NullReference" to "the app refuses to boot with a clear message" — fail fast, fail loud.
		services.AddOptions<SpotifyOptions>()
			.Bind(configuration.GetSection(SpotifyOptions.SectionName))
			.Validate(
				options => !string.IsNullOrWhiteSpace(options.ClientId),
				"Spotify:ClientId is not configured.")
			.Validate(
				options => !string.IsNullOrWhiteSpace(options.ClientSecret),
				"Spotify:ClientSecret is not configured.")
			.Validate(
				options => !string.IsNullOrWhiteSpace(options.RedirectUri),
				"Spotify:RedirectUri is not configured.")
			.ValidateOnStart();

		// One clock for the whole app. TimeProvider.System is the real clock; tests substitute a fake
		// to control "now" when asserting token expiry. TryAdd so a test host can register its own
		// first without this overwriting it. Injected everywhere instead of calling DateTimeOffset.UtcNow
		// directly, which is what makes time-dependent logic testable.
		services.TryAddSingleton(TimeProvider.System);

		// Typed HttpClient: the factory builds a SpotifyOAuthClient with an HttpClient whose BaseAddress
		// is the accounts host. The typed-client pattern gives this integration its own pre-configured
		// client and is where a resilience/retry handler will slot in later (M4/M10).
		services.AddHttpClient<ISpotifyOAuthClient, SpotifyOAuthClient>(client =>
		{
			client.BaseAddress = new Uri("https://accounts.spotify.com/");
		});

		// The data-reads client lives on a DIFFERENT host (api.spotify.com, not accounts.spotify.com),
		// which is exactly why it's a separate typed client with its own BaseAddress.
		services.AddHttpClient<ISpotifyClient, SpotifyApiClient>(client =>
		{
			client.BaseAddress = new Uri("https://api.spotify.com/");
		});

		// Callback orchestration. Scoped because it uses the scoped DbContext to upsert the user +
		// connection within one unit of work.
		services.AddScoped<ISpotifyAuthenticationService, SpotifyAuthenticationService>();
	}

	private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
	{
		// Throw here (not fall back to a default) because a missing DB connection string is a
		// misconfiguration we want to fail loudly at startup — unlike Redis, which is only a cache
		// and is allowed to be briefly absent. Postgres is the source of truth; no silent default.
		string connectionString = configuration.GetConnectionString("Postgres")
		                          ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

		services.AddDbContext<VibeStreamDbContext>(options =>
		{
			options.UseNpgsql(connectionString);

			// The whole reason for the package: snake_case everywhere, applied to the model as EF
			// builds it. Do this in ONE place so tables and columns are consistent across every entity.
			options.UseSnakeCaseNamingConvention();
		});
	}

	private static void AddRedis(IServiceCollection services, IConfiguration configuration)
	{
		// Fall back to the local Docker default so a clean clone runs with no configuration.
		string connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

		// -----------------------------------------------------------------------------------------
		// ONE multiplexer for the whole process, shared and thread-safe — this is how StackExchange
		// wants to be used. Creating a multiplexer per operation is the classic Redis performance
		// mistake (each one opens its own socket and does its own handshake).
		//
		// AbortOnConnectFail = false: if Redis is briefly unavailable at startup, connect lazily and
		// keep retrying in the background instead of throwing and taking the whole app down. The
		// readiness check is what surfaces the outage; the app itself stays live (Postgres is the
		// source of truth, Redis is only a cache).
		// -----------------------------------------------------------------------------------------
		services.AddSingleton<IConnectionMultiplexer>(_ =>
		{
			ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
			options.AbortOnConnectFail = false;

			return ConnectionMultiplexer.Connect(options);
		});

		// Cache-aside over that multiplexer. Singleton: it holds no per-request state and the multiplexer
		// it wraps is itself a singleton.
		services.AddSingleton<ICacheService, RedisCacheService>();
	}

	private static void AddInfrastructureHealthChecks(IServiceCollection services)
	{
		// AddTypeActivatedCheck constructs the check via the service provider (injecting
		// IConfiguration / IConnectionMultiplexer) without needing the check type itself registered.
		//
		// The "ready" tag is what the /health/ready endpoint filters on — see the API pipeline. These
		// are dependency checks, so they belong to readiness, never to liveness.
		services.AddHealthChecks()
			.AddTypeActivatedCheck<PostgresHealthCheck>("postgres", failureStatus: null, tags: ["ready"])
			.AddTypeActivatedCheck<RedisHealthCheck>("redis", failureStatus: null, tags: ["ready"]);
	}
}
