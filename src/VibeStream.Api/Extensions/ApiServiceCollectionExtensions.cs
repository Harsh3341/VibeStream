using System.Text.Json.Serialization;

namespace VibeStream.Api.Extensions;

/// <summary>Web-layer service registration: controllers + JSON conventions, OpenAPI, health checks.</summary>
public static class ApiServiceCollectionExtensions
{
	/// <summary>Controllers and the JSON conventions the whole API shares.</summary>
	public static IServiceCollection AddApiControllers(this IServiceCollection services)
	{
		services
			.AddControllers()
			.AddJsonOptions(options =>
			{
				// Serialise enums as strings ("Chill", not 3). Numeric enums are a versioning trap:
				// inserting a member shifts every later value and clients silently misread old data.
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

				// Omit nulls: smaller payloads, and "absent" reads as absent.
				options.JsonSerializerOptions.DefaultIgnoreCondition =
					JsonIgnoreCondition.WhenWritingNull;
			});

		return services;
	}

	/// <summary>OpenAPI document generation (rendered by Scalar in the pipeline).</summary>
	public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
	{
		services.AddOpenApi();

		return services;
	}

	/// <summary>The name of the CORS policy the SPA uses. One const so it can't be mistyped.</summary>
	public const string SpaCorsPolicy = "spa";

	/// <summary>
	/// CORS for the Next.js SPA. AllowCredentials is REQUIRED because auth rides in a cookie — and the
	/// spec forbids pairing it with a wildcard origin, so we pin the exact frontend origin from config.
	/// </summary>
	public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
	{
		string frontendOrigin = configuration["Frontend:BaseUrl"] ?? "http://127.0.0.1:3000";

		services.AddCors(options =>
		{
			options.AddPolicy(SpaCorsPolicy, policy => policy
				.WithOrigins(frontendOrigin)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials());
		});

		return services;
	}

	/// <summary>
	/// The LIVENESS check only. Dependency (readiness) checks are registered by the Infrastructure
	/// layer — see AddInfrastructure — because whether Postgres/Redis are reachable is its concern,
	/// not the web layer's.
	/// </summary>
	public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
	{
		// "self" has no dependencies: if the pipeline can execute this delegate, the process is alive.
		// It must NOT check Postgres/Redis — a failing dependency on a liveness probe makes an
		// orchestrator restart healthy replicas, turning a database blip into a full outage.
		services.AddHealthChecks()
			.AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
				tags: ["live"]);

		return services;
	}
}
