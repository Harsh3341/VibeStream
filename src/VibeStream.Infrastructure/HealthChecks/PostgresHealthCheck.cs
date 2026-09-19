using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace VibeStream.Infrastructure.HealthChecks;

/// <summary>
/// READINESS check for PostgreSQL.
///
/// CONCEPT: a readiness check answers "can I serve traffic right now?", so it is allowed — expected,
/// even — to touch dependencies. It opens a real connection and runs <c>SELECT 1</c>; a started-but-
/// unreachable database is exactly the state readiness exists to catch.
///
/// The connection string is read on each call rather than cached in the constructor, so a missing
/// configuration surfaces as an Unhealthy result (with a clear reason) instead of throwing while the
/// DI container is being built — a health check should report failure, not prevent startup.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
	private readonly IConfiguration _configuration;

	public PostgresHealthCheck(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		string? connectionString = _configuration.GetConnectionString("Postgres");

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return HealthCheckResult.Unhealthy("ConnectionStrings:Postgres is not configured.");
		}

		try
		{
			await using NpgsqlConnection connection = new(connectionString);
			await connection.OpenAsync(cancellationToken);

			await using NpgsqlCommand command = new("SELECT 1", connection);
			await command.ExecuteScalarAsync(cancellationToken);

			return HealthCheckResult.Healthy("PostgreSQL is reachable.");
		}
		catch (Exception exception)
		{
			// The exception is attached for logging, never serialised to the /health response —
			// a connection string in a public error body is a credential leak.
			return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.", exception);
		}
	}
}
