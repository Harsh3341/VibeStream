using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Analytics;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Analytics;

/// <summary>
/// Rebuilds recent <c>DailyStatistic</c> rows from events. Recomputes a bounded recent window rather
/// than all of history: recently-played only adds recent plays, so older days' rollups don't change.
/// Days are bucketed in UTC (a documented simplification — per-user time zones can come later). Scoped.
/// </summary>
public sealed class DailyStatisticsService : IDailyStatisticsService
{
	// How many days back to recompute each run. Comfortably covers the 7-day analytics window while
	// staying bounded regardless of how much history exists.
	private const int WindowDays = 30;

	private readonly VibeStreamDbContext _dbContext;
	private readonly TimeProvider _timeProvider;

	public DailyStatisticsService(VibeStreamDbContext dbContext, TimeProvider timeProvider)
	{
		_dbContext = dbContext;
		_timeProvider = timeProvider;
	}

	public async Task RebuildRecentAsync(Guid userId, CancellationToken cancellationToken)
	{
		DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
		DateOnly windowStart = today.AddDays(-(WindowDays - 1));
		DateTimeOffset windowStartInstant = new(windowStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

		// Pull the recent plays with the track fields we aggregate (duration for time, artist for
		// uniqueness). Grouping happens in memory to sidestep provider-specific date-bucketing.
		List<PlayProjection> plays = await _dbContext.ListeningEvents
			.Where(listeningEvent => listeningEvent.UserId == userId
				&& listeningEvent.PlayedAt >= windowStartInstant)
			.Select(listeningEvent => new PlayProjection(
				listeningEvent.PlayedAt,
				listeningEvent.Track.DurationMs,
				listeningEvent.Track.ArtistId))
			.ToListAsync(cancellationToken);

		Dictionary<DateOnly, DailyStatistic> existingByDate = await _dbContext.DailyStatistics
			.Where(statistic => statistic.UserId == userId && statistic.Date >= windowStart)
			.ToDictionaryAsync(statistic => statistic.Date, cancellationToken);

		foreach (IGrouping<DateOnly, PlayProjection> dayGroup in
			plays.GroupBy(play => DateOnly.FromDateTime(play.PlayedAt.UtcDateTime)))
		{
			// Assume a full listen — Spotify's recently-played reports completed plays, not partial ones.
			int listeningSeconds = (int)(dayGroup.Sum(play => (long)play.DurationMs) / 1000L);
			int trackCount = dayGroup.Count();
			int artistCount = dayGroup.Select(play => play.ArtistId).Distinct().Count();

			if (existingByDate.TryGetValue(dayGroup.Key, out DailyStatistic? statistic))
			{
				statistic.ListeningSeconds = listeningSeconds;
				statistic.TrackCount = trackCount;
				statistic.ArtistCount = artistCount;
			}
			else
			{
				_dbContext.DailyStatistics.Add(new DailyStatistic
				{
					UserId = userId,
					Date = dayGroup.Key,
					ListeningSeconds = listeningSeconds,
					TrackCount = trackCount,
					ArtistCount = artistCount,
				});
			}
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	/// <summary>The per-play fields the daily rollup aggregates. Named (not anonymous) so it survives the
	/// materializing query into in-memory grouping.</summary>
	private sealed record PlayProjection(DateTimeOffset PlayedAt, int DurationMs, Guid ArtistId);
}
