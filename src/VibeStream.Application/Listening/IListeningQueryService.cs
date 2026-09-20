namespace VibeStream.Application.Listening;

/// <summary>
/// Read side for the profile, listening history, and sessions the UI shows (plan §15/§22). All from
/// local data. The controller stays thin; the query shapes live here.
/// </summary>
public interface IListeningQueryService
{
	Task<UserProfileView?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

	Task<IReadOnlyList<RecentPlayView>> GetRecentPlaysAsync(
		Guid userId,
		int limit,
		CancellationToken cancellationToken);

	Task<IReadOnlyList<SessionView>> GetSessionsAsync(
		Guid userId,
		int limit,
		CancellationToken cancellationToken);

	/// <summary>A session with its ordered tracks, or null if it isn't the user's or doesn't exist.</summary>
	Task<SessionDetailView?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}

public sealed record UserProfileView(Guid Id, string SpotifyUserId, string? DisplayName);

public sealed record RecentPlayView(
	Guid TrackId,
	string TrackName,
	string ArtistName,
	string AlbumName,
	string? AlbumImageUrl,
	DateTimeOffset PlayedAt);

public sealed record SessionView(
	Guid Id,
	DateTimeOffset StartedAt,
	DateTimeOffset EndedAt,
	int TrackCount,

	// The session's vibe label, or null if it hasn't been classified yet.
	string? Vibe);

public sealed record SessionDetailView(
	Guid Id,
	DateTimeOffset StartedAt,
	DateTimeOffset EndedAt,
	int TrackCount,
	string? Vibe,
	IReadOnlyList<SessionTrackView> Tracks);

public sealed record SessionTrackView(
	int Position,
	Guid TrackId,
	string TrackName,
	string ArtistName,
	string? AlbumImageUrl,
	DateTimeOffset PlayedAt);
