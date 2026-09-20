namespace VibeStream.Application.Analytics;

/// <summary>
/// Maintains the per-day <c>DailyStatistic</c> rollups from stored events (plan §14). Recomputing a
/// precomputed row per day means analytics reads don't rescan the whole event history — the (UserId,
/// Date) unique index from M2 is what makes the upsert safe.
/// </summary>
public interface IDailyStatisticsService
{
	/// <summary>
	/// Recomputes the daily rollups for <paramref name="userId"/> across the recent window and upserts
	/// them. Called after ingestion so the stats track newly imported plays.
	/// </summary>
	Task RebuildRecentAsync(Guid userId, CancellationToken cancellationToken);
}
