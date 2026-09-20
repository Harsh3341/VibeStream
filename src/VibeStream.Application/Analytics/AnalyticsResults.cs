namespace VibeStream.Application.Analytics;

/// <summary>Snapshot of the current day (plan §14 "Today"), computed live from today's events.</summary>
public sealed record TodaySummary(
	int ListeningSeconds,
	int TracksPlayed,
	int UniqueArtists,
	int UniqueTracks,
	int Sessions);

/// <summary>One day on the weekly chart — from the precomputed DailyStatistic rollup.</summary>
public sealed record DailyPoint(DateOnly Date, int ListeningSeconds, int TrackCount);

/// <summary>An artist and how many times the user played it in the window.</summary>
public sealed record TopArtist(Guid ArtistId, string Name, int PlayCount);

/// <summary>A track (with its artist's name) and its play count in the window.</summary>
public sealed record TopTrack(Guid TrackId, string Name, string ArtistName, int PlayCount);

/// <summary>The last-7-days view (plan §14): daily totals, top lists, and the busiest hour.</summary>
public sealed record WeeklySummary(
	IReadOnlyList<DailyPoint> Daily,
	IReadOnlyList<TopArtist> TopArtists,
	IReadOnlyList<TopTrack> TopTracks,

	// Hour of day (0–23, UTC) with the most plays, or null when there's no activity in the window.
	int? PeakHourUtc);
