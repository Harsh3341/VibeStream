namespace VibeStream.Application.Analytics;

/// <summary>
/// The read side of analytics (plan §14) — everything computed from LOCAL data (events, sessions,
/// daily rollups), never re-fetched from Spotify. The API exposes these; the service keeps the query
/// logic out of controllers.
/// </summary>
public interface IAnalyticsService
{
	Task<TodaySummary> GetTodayAsync(Guid userId, CancellationToken cancellationToken);

	Task<WeeklySummary> GetWeeklyAsync(Guid userId, CancellationToken cancellationToken);

	/// <summary>Most-played artists over the last <paramref name="days"/> days, top <paramref name="limit"/>.</summary>
	Task<IReadOnlyList<TopArtist>> GetTopArtistsAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken);

	Task<IReadOnlyList<TopTrack>> GetTopTracksAsync(
		Guid userId,
		int days,
		int limit,
		CancellationToken cancellationToken);
}
