namespace VibeStream.Application.Caching;

/// <summary>
/// A small cache-aside abstraction over the cache (Redis). The rest of the app depends on this, not on
/// StackExchange.Redis. CONTRACT: the cache is never correctness-critical (plan §17) — every method
/// fails OPEN, so a cache outage degrades to hitting the source of truth, never to an error.
/// </summary>
public interface ICacheService
{
	/// <summary>
	/// Returns the cached value for <paramref name="key"/>, or runs <paramref name="factory"/>, caches
	/// its result under <paramref name="ttl"/>, and returns it. On any cache failure the factory result
	/// is returned uncached.
	/// </summary>
	Task<T> GetOrSetAsync<T>(
		string key,
		Func<CancellationToken, Task<T>> factory,
		TimeSpan ttl,
		CancellationToken cancellationToken);

	/// <summary>
	/// Atomically adds <paramref name="amount"/> to a counter and returns the new value (creating it at
	/// 0 first). Passing 0 reads the current value. Used for versioned-key invalidation.
	/// </summary>
	Task<long> IncrementAsync(string key, long amount, CancellationToken cancellationToken);
}
