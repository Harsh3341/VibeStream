using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Analytics;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Analytics;

/// <summary>
/// Computes analytics from stored data. Reads pull a windowed projection of events (bounded by a date
/// filter) and aggregate in memory — robust across providers and plenty fast at personal scale — while
/// the weekly daily chart reuses the precomputed DailyStatistic rollups. Days are bucketed in UTC.
/// Scoped: uses the DbContext.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
	private const int WeeklyDays = 7;

	private readonly VibeStreamDbContext _dbContext;
	private readonly TimeProvider _timeProvider;

	public AnalyticsService(VibeStreamDbContext dbContext, TimeProvider timeProvider)
	{
		_dbContext = dbContext;
		_timeProvider = timeProvider;
	}

	public async Task<TodaySummary> GetTodayAsync(Guid userId, CancellationToken cancellationToken)
	{
		DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
		DateTimeOffset todayStart = StartOfDay(today);

		List<EventProjection> events = await LoadEventsSinceAsync(userId, todayStart, cancellationToken);

		int sessions = await _dbContext.ListeningSessions
			.CountAsync(session => session.UserId == userId && session.StartedAt >= todayStart, cancellationToken);

		return new TodaySummary(
			ListeningSeconds: ToSeconds(events),
			TracksPlayed: events.Count,
			UniqueArtists: events.Select(play => play.ArtistId).Distinct().Count(),
			UniqueTracks: events.Select(play => play.TrackId).Distinct().Count(),
			Sessions: sessions);
	}

	public async Task<WeeklySummary> GetWeeklyAsync(Guid userId, CancellationToken cancellationToken)
	{
		DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
		DateOnly weekStart = today.AddDays(-(WeeklyDays - 1));

		// Daily totals come from the precomputed rollups, zero-filled so the chart has all 7 days.
		Dictionary<DateOnly, DailyStatistic> rollups = await _dbContext.DailyStatistics
			.Where(statistic => statistic.UserId == userId && statistic.Date >= weekStart)
			.ToDictionaryAsync(statistic => statistic.Date, cancellationToken);

		List<DailyPoint> daily = [];
		for (int offset = 0; offset < WeeklyDays; offset++)
		{
			DateOnly date = weekStart.AddDays(offset);

			daily.Add(rollups.TryGetValue(date, out DailyStatistic? statistic)
				? new DailyPoint(date, statistic.ListeningSeconds, statistic.TrackCount)
				: new DailyPoint(date, 0, 0));
		}

		// Top lists and peak hour come from the week's events.
		List<EventProjection> events = await LoadEventsSinceAsync(userId, StartOfDay(weekStart), cancellationToken);

		return new WeeklySummary(
			Daily: daily,
			TopArtists: RankArtists(events, limit: 10),
			TopTracks: RankTracks(events, limit: 10),
			PeakHourUtc: PeakHour(events));
	}

	public async Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken)
	{
		List<EventProjection> events = await LoadEventsForDaysAsync(userId, days, cancellationToken);

		return RankArtists(events, limit);
	}

	public async Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken)
	{
		List<EventProjection> events = await LoadEventsForDaysAsync(userId, days, cancellationToken);

		return RankTracks(events, limit);
	}

	private Task<List<EventProjection>> LoadEventsForDaysAsync(
		Guid userId,
		int days,
		CancellationToken cancellationToken)
	{
		DateOnly from = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime).AddDays(-(Math.Max(days, 1) - 1));

		return LoadEventsSinceAsync(userId, StartOfDay(from), cancellationToken);
	}

	private Task<List<EventProjection>> LoadEventsSinceAsync(
		Guid userId,
		DateTimeOffset since,
		CancellationToken cancellationToken)
	{
		// Nav access in a projection (event → track → artist) translates to joins reliably.
		return _dbContext.ListeningEvents
			.Where(listeningEvent => listeningEvent.UserId == userId && listeningEvent.PlayedAt >= since)
			.Select(listeningEvent => new EventProjection(
				listeningEvent.PlayedAt,
				listeningEvent.TrackId,
				listeningEvent.Track.Name,
				listeningEvent.Track.ArtistId,
				listeningEvent.Track.Artist.Name,
				listeningEvent.Track.DurationMs))
			.ToListAsync(cancellationToken);
	}

	private static int ToSeconds(IEnumerable<EventProjection> events)
	{
		// Full-listen assumption: recently-played reports completed plays.
		return (int)(events.Sum(play => (long)play.DurationMs) / 1000L);
	}

	private static IReadOnlyList<TopArtist> RankArtists(IEnumerable<EventProjection> events, int limit)
	{
		return events
			.GroupBy(play => new { play.ArtistId, play.ArtistName })
			.Select(group => new TopArtist(group.Key.ArtistId, group.Key.ArtistName, group.Count()))
			.OrderByDescending(artist => artist.PlayCount)
			.ThenBy(artist => artist.Name)
			.Take(limit)
			.ToList();
	}

	private static IReadOnlyList<TopTrack> RankTracks(IEnumerable<EventProjection> events, int limit)
	{
		return events
			.GroupBy(play => new { play.TrackId, play.TrackName, play.ArtistName })
			.Select(group => new TopTrack(group.Key.TrackId, group.Key.TrackName, group.Key.ArtistName, group.Count()))
			.OrderByDescending(track => track.PlayCount)
			.ThenBy(track => track.Name)
			.Take(limit)
			.ToList();
	}

	private static int? PeakHour(IReadOnlyCollection<EventProjection> events)
	{
		if (events.Count == 0)
		{
			return null;
		}

		return events
			.GroupBy(play => play.PlayedAt.UtcDateTime.Hour)
			.OrderByDescending(group => group.Count())
			.ThenBy(group => group.Key)
			.First()
			.Key;
	}

	private static DateTimeOffset StartOfDay(DateOnly date)
	{
		return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
	}

	/// <summary>Flat per-play row loaded for in-memory aggregation. Named so it survives materialization.</summary>
	private sealed record EventProjection(
		DateTimeOffset PlayedAt,
		Guid TrackId,
		string TrackName,
		Guid ArtistId,
		string ArtistName,
		int DurationMs);
}
