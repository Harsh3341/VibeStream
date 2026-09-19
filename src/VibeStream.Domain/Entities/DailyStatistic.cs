namespace VibeStream.Domain.Entities;

/// <summary>
/// A per-user, per-day rollup. Analytics read this precomputed row instead of scanning every event.
/// One row per (UserId, Date), enforced by a unique index (config step) so the pipeline can upsert.
/// </summary>
public class DailyStatistic
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	public required Guid UserId { get; set; }

	// A calendar day — no time, no zone. DateOnly maps to Postgres `date`.
	public required DateOnly Date { get; set; }

	// Precomputed aggregates for the day.
	public int ListeningSeconds { get; set; }

	public int TrackCount { get; set; }

	public int ArtistCount { get; set; }
}
