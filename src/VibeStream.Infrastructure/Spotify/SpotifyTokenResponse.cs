namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// The raw shape of Spotify's token endpoint (<c>POST /api/token</c>) response. INTERNAL to
/// Infrastructure: it exists only to deserialize Spotify's JSON, which the client then maps to the
/// clean <c>SpotifyTokenResult</c> the rest of the app sees. Property names bind from snake_case via
/// <see cref="SpotifyJson"/> (access_token → AccessToken, and so on).
/// </summary>
internal sealed record SpotifyTokenResponse
{
	public string AccessToken { get; init; } = string.Empty;

	// Present when exchanging the auth code; usually ABSENT on refresh responses (reuse the old one).
	public string? RefreshToken { get; init; }

	// Lifetime in SECONDS from now. The client converts this to an absolute instant with the clock.
	public int ExpiresIn { get; init; }

	public string? Scope { get; init; }

	public string? TokenType { get; init; }
}
