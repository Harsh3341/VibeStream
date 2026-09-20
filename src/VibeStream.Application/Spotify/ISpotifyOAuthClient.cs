namespace VibeStream.Application.Spotify;

/// <summary>
/// The OAuth token lifecycle with Spotify — the "how do we get and keep credentials" half of the
/// integration. Infrastructure implements it against Spotify's accounts endpoints; the Application
/// layer depends only on this abstraction (plan §9), so nothing above Infrastructure knows about
/// HttpClient, form encoding, or the accounts.spotify.com base URL.
/// </summary>
public interface ISpotifyOAuthClient
{
	/// <summary>
	/// Builds the URL we redirect the browser to so the user can authorize the app. <paramref name="state"/>
	/// is an opaque anti-CSRF token we generate, hand to Spotify, and re-check on the callback — it ties
	/// the redirect we started to the callback we receive (plan §27: "Validate OAuth state").
	/// </summary>
	string BuildAuthorizationUrl(string state);

	/// <summary>
	/// Exchanges the one-time authorization <paramref name="code"/> from the callback for an access +
	/// refresh token pair.
	/// </summary>
	Task<SpotifyTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

	/// <summary>
	/// Mints a fresh access token from a stored refresh token when the current one has expired. The
	/// result's RefreshToken may be null — Spotify often omits it here, meaning "keep the old one".
	/// </summary>
	Task<SpotifyTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
