namespace VibeStream.Domain.Entities;

/// <summary>
/// One "this track was played at this time" record — the append-only heart of the system.
/// Deduplicated by a UNIQUE (UserId, TrackId, PlayedAt) index (declared in the EF config).
/// </summary>
public class ListeningEvent
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	// Who listened. FK only, no navigation — we always query events BY a user, we never navigate
	// from an event back to the User object, so a nav property here would be one we never use.
	public required Guid UserId { get; set; }

	// What they listened to. FK + navigation, because history queries load the track (and its
	// artist) to display it.
	public required Guid TrackId { get; set; }

	public Track Track { get; set; } = null!;

	// When Spotify says it was played. This drives history, session detection, AND idempotency.
	public required DateTimeOffset PlayedAt { get; set; }

	// When WE ingested it — distinct from PlayedAt, useful for debugging the pipeline.
	public DateTimeOffset CreatedAt { get; set; }
}
