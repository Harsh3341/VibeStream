using VibeStream.Application.Analytics;
using VibeStream.Application.Caching;

namespace VibeStream.Infrastructure.Analytics;

/// <summary>
/// Invalidates a user's analytics cache by bumping their version counter — one atomic increment orphans
/// every cached entry for that user at once (they then TTL out). Fails open through the cache service,
/// so a Redis hiccup here never breaks ingestion; stale entries simply expire on their own.
/// </summary>
public sealed class AnalyticsCacheInvalidator : IAnalyticsCacheInvalidator
{
	private readonly ICacheService _cache;

	public AnalyticsCacheInvalidator(ICacheService cache)
	{
		_cache = cache;
	}

	public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
	{
		return _cache.IncrementAsync(AnalyticsCacheKeys.Version(userId), 1, cancellationToken);
	}
}
