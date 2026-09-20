using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using VibeStream.Application.Caching;

namespace VibeStream.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache-aside over the shared <see cref="IConnectionMultiplexer"/> from M1. Values are
/// stored as JSON strings. Every Redis call is wrapped so a cache outage NEVER surfaces as an error —
/// reads fall back to the factory, writes are skipped — because Postgres is the source of truth and the
/// cache is an optimization (plan §17).
/// </summary>
public sealed class RedisCacheService : ICacheService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly IConnectionMultiplexer _multiplexer;
	private readonly ILogger<RedisCacheService> _logger;

	public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
	{
		_multiplexer = multiplexer;
		_logger = logger;
	}

	public async Task<T> GetOrSetAsync<T>(
		string key,
		Func<CancellationToken, Task<T>> factory,
		TimeSpan ttl,
		CancellationToken cancellationToken)
	{
		try
		{
			RedisValue cached = await _multiplexer.GetDatabase().StringGetAsync(key);

			if (cached.HasValue)
			{
				// RedisValue converts implicitly to both string and byte[], so pick one explicitly.
				T? hit = JsonSerializer.Deserialize<T>(cached.ToString(), JsonOptions);

				if (hit is not null)
				{
					return hit;
				}
			}
		}
		catch (RedisException exception)
		{
			// Fail open: a cache read problem must not break the request.
			_logger.LogWarning(exception, "Cache read failed for {Key}; using the source of truth.", key);
		}

		T fresh = await factory(cancellationToken);

		try
		{
			string serialized = JsonSerializer.Serialize(fresh, JsonOptions);
			await _multiplexer.GetDatabase().StringSetAsync(key, serialized, ttl);
		}
		catch (RedisException exception)
		{
			_logger.LogWarning(exception, "Cache write failed for {Key}; value served uncached.", key);
		}

		return fresh;
	}

	public async Task<long> IncrementAsync(string key, long amount, CancellationToken cancellationToken)
	{
		try
		{
			// StringIncrement on a missing key initializes it to 0 first, so amount 0 reads the current
			// version and amount 1 bumps it. (StackExchange.Redis has no CancellationToken overload here.)
			return await _multiplexer.GetDatabase().StringIncrementAsync(key, amount);
		}
		catch (RedisException exception)
		{
			_logger.LogWarning(exception, "Cache increment failed for {Key}.", key);

			// Fail open: return a stable value so the caller can carry on (uncached).
			return amount;
		}
	}
}
