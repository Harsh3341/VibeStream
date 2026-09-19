namespace VibeStream.Domain.Entities;

/// <summary>
/// One track's place within a session — the ordered link between ListeningSession and Track.
/// Keyed by (SessionId, Position); it has no identity of its own.
/// </summary>
public class SessionTrack
{
	// Half of the composite primary key. FK to the owning session.
	public required Guid SessionId { get; set; }

	// The other half: this track's order within the session (0, 1, 2, …).
	public required int Position { get; set; }

	// Which track. FK + navigation, so expanding a session can show the track and its artist.
	public required Guid TrackId { get; set; }

	public Track Track { get; set; } = null!;

	// When this track played — mirrors the originating ListeningEvent.PlayedAt.
	public required DateTimeOffset PlayedAt { get; set; }
}
