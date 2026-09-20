namespace VibeStream.Application.Vibes;

/// <summary>
/// Read side for vibes: the current vibe, the history, and the "listening DNA" breakdown (the feature
/// deferred from M6, now that vibes exist). All from stored VibeSnapshots — local data, no Spotify call.
/// </summary>
public interface IVibeQueryService
{
	/// <summary>The most recent vibe snapshot, or null if the user has none yet.</summary>
	Task<VibeView?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);

	/// <summary>Recent vibe snapshots, newest first, capped at <paramref name="limit"/>.</summary>
	Task<IReadOnlyList<VibeView>> GetHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken);

	/// <summary>
	/// Listening DNA: the share of each vibe label across the last <paramref name="days"/> days, as a
	/// percentage of the user's snapshots.
	/// </summary>
	Task<IReadOnlyList<VibeShare>> GetListeningDnaAsync(Guid userId, int days, CancellationToken cancellationToken);
}

/// <summary>A vibe snapshot as the API returns it.</summary>
public sealed record VibeView(
	Guid Id,
	Guid? SessionId,
	string Name,
	double Energy,
	double Valence,
	double Danceability,
	double Confidence,
	DateTimeOffset CreatedAt);

/// <summary>One slice of the listening-DNA breakdown.</summary>
public sealed record VibeShare(string Name, int Count, double Percentage);
