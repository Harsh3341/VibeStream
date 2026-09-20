namespace VibeStream.Application.Listening;

/// <summary>
/// Fetches what the user is playing RIGHT NOW — inherently live, so this one calls Spotify (with a
/// refreshed token) rather than reading local data. Returns null when nothing is playing.
/// </summary>
public interface INowPlayingService
{
	Task<NowPlayingView?> GetAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record NowPlayingView(
	string TrackName,
	string ArtistName,
	string AlbumName,
	string? AlbumImageUrl,
	bool IsPlaying,
	int ProgressMs,
	int DurationMs);
