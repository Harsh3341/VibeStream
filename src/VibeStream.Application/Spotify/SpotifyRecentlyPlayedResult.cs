namespace VibeStream.Application.Spotify;

/// <summary>
/// The recently-played response: a batch of "this track played at this time" items. The ingestion
/// worker (M4) turns each item into a ListeningEvent, deduplicated by (user, track, played-at).
/// </summary>
public sealed record SpotifyRecentlyPlayedResult(
	IReadOnlyList<SpotifyPlayHistoryItem> Items);

/// <summary>One play in the history: what track, and when Spotify says it was played.</summary>
public sealed record SpotifyPlayHistoryItem(
	SpotifyTrack Track,

	// This is the value that drives history, session detection, AND idempotency — see ListeningEvent.
	DateTimeOffset PlayedAt);
