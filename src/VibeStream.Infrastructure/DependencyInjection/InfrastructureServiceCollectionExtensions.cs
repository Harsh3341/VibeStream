using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VibeStream.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using VibeStream.Infrastructure.Data;
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
		AddInfrastructureHealthChecks(services);

		return services;
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
