using VibeStream.Application.Ingestion;
using VibeStream.Application.Listening;
using VibeStream.Application.Spotify;

namespace VibeStream.Infrastructure.Listening;

/// <summary>
/// Live now-playing: get a valid token (refreshing if needed), ask Spotify, map to a UI DTO. Scoped —
/// the token service it uses is scoped. Returns null when nothing is playing (Spotify 204).
/// </summary>
public sealed class NowPlayingService : INowPlayingService
{
	private readonly ISpotifyTokenService _tokenService;
	private readonly ISpotifyClient _spotifyClient;

	public NowPlayingService(ISpotifyTokenService tokenService, ISpotifyClient spotifyClient)
	{
		_tokenService = tokenService;
		_spotifyClient = spotifyClient;
	}

	public async Task<NowPlayingView?> GetAsync(Guid userId, CancellationToken cancellationToken)
	{
		string accessToken = await _tokenService.GetValidAccessTokenAsync(userId, cancellationToken);

		SpotifyCurrentlyPlaying? current = await _spotifyClient.GetCurrentlyPlayingAsync(
			accessToken, cancellationToken);

		if (current is null)
		{
			return null;
		}

		return new NowPlayingView(
			current.Track.Name,
			current.Track.Artist.Name,
			current.Track.AlbumName,
			current.Track.AlbumImageUrl,
			current.IsPlaying,
			current.ProgressMs,
			current.Track.DurationMs);
	}
}
