namespace VibeStream.Application.Spotify;

/// <summary>
/// The Spotify Web API data reads — the "what is the user doing" half of the integration (plan §9).
/// Each call takes an already-valid access token; obtaining/refreshing tokens is
/// <see cref="ISpotifyOAuthClient"/>'s job, keeping token lifecycle and data access separate.
///
/// Controllers and the worker depend on THIS, never on Spotify directly (plan §9: "Do not call Spotify
/// directly from controllers").
/// </summary>
public interface ISpotifyClient
{
	/// <summary>
	/// The profile of the token's owner. Called on the OAuth callback to identify the user
	/// (Id → User.SpotifyUserId) before we store their connection.
	/// </summary>
	Task<SpotifyUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken);

	/// <summary>The user's recently-played history — the ingestion worker's primary data source (M4).</summary>
	Task<SpotifyRecentlyPlayedResult> GetRecentlyPlayedAsync(string accessToken, CancellationToken cancellationToken);

	/// <summary>
	/// What's playing right now, or <c>null</c> when nothing is (Spotify returns 204 No Content).
	/// </summary>
	Task<SpotifyCurrentlyPlaying?> GetCurrentlyPlayingAsync(string accessToken, CancellationToken cancellationToken);
}
