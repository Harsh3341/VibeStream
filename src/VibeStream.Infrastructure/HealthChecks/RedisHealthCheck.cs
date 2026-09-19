using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace VibeStream.Infrastructure.HealthChecks;

/// <summary>
/// READINESS check for Redis.
///
/// It reuses the singleton <see cref="IConnectionMultiplexer"/> — the same connection the rest of the
/// app uses — rather than dialling a throwaway connection, so this reports the health of the
/// connection the application actually depends on. A PING round-trip is the cheapest proof that the
/// server is not just reachable but responding.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
	private readonly IConnectionMultiplexer _connectionMultiplexer;

	public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
	{
		_connectionMultiplexer = connectionMultiplexer;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			IDatabase database = _connectionMultiplexer.GetDatabase();
			TimeSpan latency = await database.PingAsync();

			return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds:F0} ms.");
		}
		catch (Exception exception)
		{
			return HealthCheckResult.Unhealthy("Redis is unreachable.", exception);
		}
	}
}
