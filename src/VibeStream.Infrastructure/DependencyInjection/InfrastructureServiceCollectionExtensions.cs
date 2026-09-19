using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VibeStream.Infrastructure.HealthChecks;

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
		AddRedis(services, configuration);
		AddInfrastructureHealthChecks(services);

		return services;
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
