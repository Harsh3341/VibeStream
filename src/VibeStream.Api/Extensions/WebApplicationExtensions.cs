using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace VibeStream.Api.Extensions;

/// <summary>Builds the HTTP pipeline and maps endpoints.</summary>
public static class WebApplicationExtensions
{
	/// <summary>
	/// The middleware pipeline. Order matters: middleware is a nested onion — first registered is the
	/// outermost, first to see the request and last to see the response. This M1 pipeline is
	/// deliberately small; auth, error handling and caching arrive in their own milestones.
	/// </summary>
	public static WebApplication UseApiPipeline(this WebApplication app)
	{
		// Developer tooling — Development only. Publishing the API's shape hands an attacker a map,
		// so both the raw document and the UI stay gated on environment.
		if (app.Environment.IsDevelopment())
		{
			// Raw spec at /openapi/v1.json
			app.MapOpenApi();

			// Scalar renders it at /scalar/v1 — the modern replacement for the Swagger UI page.
			app.MapScalarApiReference(options =>
			{
				options.WithTitle("VibeStream — API reference");
			});
		}

		// Authentication BEFORE authorization — the fixed order. Authentication populates HttpContext.User
		// from the cookie's JWT; authorization then evaluates [Authorize] against it. Both sit before
		// MapControllers so the user is resolved by the time an endpoint runs.
		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();
		app.MapVibeStreamHealthChecks();

		return app;
	}

	/// <summary>
	/// Three health endpoints for three audiences. See AddApiHealthChecks / AddInfrastructure for why
	/// liveness and readiness must not be the same check.
	/// </summary>
	private static WebApplication MapVibeStreamHealthChecks(this WebApplication app)
	{
		// Everything, with detail — for humans and dashboards.
		app.MapHealthChecks("/health", new HealthCheckOptions
		{
			ResponseWriter = WriteHealthResponseAsync
		});

		// LIVENESS: "is the process wedged?" — no dependency checks.
		app.MapHealthChecks("/health/live", new HealthCheckOptions
		{
			Predicate = registration => registration.Tags.Contains("live"),
			ResponseWriter = WriteHealthResponseAsync
		});

		// READINESS: "can I serve traffic?" — includes Postgres + Redis.
		app.MapHealthChecks("/health/ready", new HealthCheckOptions
		{
			Predicate = registration => registration.Tags.Contains("ready"),
			ResponseWriter = WriteHealthResponseAsync
		});

		return app;
	}

	/// <summary>
	/// Writes health results as JSON. The built-in writer returns the bare string "Healthy", which is
	/// useless for telling WHICH dependency failed. The failing check's exception is never written —
	/// it would leak connection strings to anyone who can reach /health.
	/// </summary>
	private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
	{
		context.Response.ContentType = "application/json";

		string json = JsonSerializer.Serialize(new
		{
			Status = report.Status.ToString(),
			TotalDurationMs = report.TotalDuration.TotalMilliseconds,
			Checks = report.Entries.Select(entry => new
			{
				Name = entry.Key,
				Status = entry.Value.Status.ToString(),
				DurationMs = entry.Value.Duration.TotalMilliseconds,
				Description = entry.Value.Description
			})
		});

		return context.Response.WriteAsync(json);
	}
}
