using VibeStream.Application.Analytics;
using VibeStream.Application.Caching;

namespace VibeStream.Infrastructure.Analytics;

/// <summary>
/// A caching DECORATOR around the real <see cref="AnalyticsService"/>. Same interface, so callers are
/// unaware — it just adds cache-aside in front of each read (plan §17). The inner service stays a pure
/// database query; caching is layered on, not tangled in. Each read first resolves the user's cache
/// version, then reads/populates a versioned key so invalidation is a single counter bump.
/// </summary>
public sealed class CachingAnalyticsService : IAnalyticsService
{
	private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

	private readonly IAnalyticsService _inner;
	private readonly ICacheService _cache;

	public CachingAnalyticsService(IAnalyticsService inner, ICacheService cache)
	{
		_inner = inner;
		_cache = cache;
	}

	public async Task<TodaySummary> GetTodayAsync(Guid userId, CancellationToken cancellationToken)
	{
		long version = await CurrentVersionAsync(userId, cancellationToken);

		return await _cache.GetOrSetAsync(
			AnalyticsCacheKeys.Today(userId, version),
			token => _inner.GetTodayAsync(userId, token),
			Ttl,
			cancellationToken);
	}

	public async Task<WeeklySummary> GetWeeklyAsync(Guid userId, CancellationToken cancellationToken)
	{
		long version = await CurrentVersionAsync(userId, cancellationToken);

		return await _cache.GetOrSetAsync(
			AnalyticsCacheKeys.Weekly(userId, version),
			token => _inner.GetWeeklyAsync(userId, token),
			Ttl,
			cancellationToken);
	}

	public async Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken)
	{
		long version = await CurrentVersionAsync(userId, cancellationToken);

		return await _cache.GetOrSetAsync(
			AnalyticsCacheKeys.TopArtists(userId, version, days, limit),
			token => _inner.GetTopArtistsAsync(userId, days, limit, token),
			Ttl,
			cancellationToken);
	}

	public async Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken)
	{
		long version = await CurrentVersionAsync(userId, cancellationToken);

		return await _cache.GetOrSetAsync(
			AnalyticsCacheKeys.TopTracks(userId, version, days, limit),
			token => _inner.GetTopTracksAsync(userId, days, limit, token),
			Ttl,
			cancellationToken);
	}

	// Amount 0 reads (and lazily initializes to 0) the user's current cache version.
	private Task<long> CurrentVersionAsync(Guid userId, CancellationToken cancellationToken)
	{
		return _cache.IncrementAsync(AnalyticsCacheKeys.Version(userId), 0, cancellationToken);
	}
}
