namespace VibeStream.Application.Ingestion;

/// <summary>
/// Pulls a user's recent Spotify plays and persists any not already stored. The core of the ingestion
/// pipeline (plan §10): fetch → normalize tracks/artists → idempotently insert listening events.
/// </summary>
public interface IListeningIngestionService
{
	/// <summary>
	/// Fetches recently-played for <paramref name="userId"/> and inserts the new plays. Returns how many
	/// listening events were actually added (0 when nothing new). Safe to call repeatedly — overlapping
	/// plays between polls are deduplicated.
	/// </summary>
	Task<int> IngestRecentPlaysAsync(Guid userId, CancellationToken cancellationToken);
}
