namespace VibeStream.Application.Ingestion;

/// <summary>
/// Hands out a currently-valid Spotify access token for a user, refreshing and persisting a new one
/// first if the stored token has expired (or is about to). Callers — the worker, any API sync — never
/// touch refresh logic; they just ask for a token they can use right now.
/// </summary>
public interface ISpotifyTokenService
{
	/// <summary>
	/// Returns a valid access token for <paramref name="userId"/>, refreshing via the stored refresh
	/// token when needed. Throws if the user has no Spotify connection.
	/// </summary>
	Task<string> GetValidAccessTokenAsync(Guid userId, CancellationToken cancellationToken);
}
