namespace VibeStream.Domain.Entities;

/// <summary>
/// A run of consecutive listening (gaps ≤ 15 min stay in the same session — see M5).
/// Built from ListeningEvents; not fetched from Spotify.
/// </summary>
public class ListeningSession
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	public required Guid UserId { get; set; }

	public required DateTimeOffset StartedAt { get; set; }

	public required DateTimeOffset EndedAt { get; set; }

	// Denormalized count of tracks in this session. Source of truth is the SessionTrack rows below;
	// this is a cached copy so listing sessions doesn't need a COUNT join per row.
	public int TrackCount { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	// Collection navigation: a session HAS its tracks, and we DO load them (clicking a session in the
	// UI shows its track list). This one earns its place — contrast with Artist.Tracks, which we
	// skipped because we never query it.
	public List<SessionTrack> Tracks { get; set; } = [];
}
