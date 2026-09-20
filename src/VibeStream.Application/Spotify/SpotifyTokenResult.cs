namespace VibeStream.Application.Spotify;

/// <summary>
/// The result of an OAuth token operation (code exchange or refresh) — the shape our own code cares
/// about, mapped from Spotify's raw token JSON by the Infrastructure client.
///
/// CONCEPT: this is a boundary DTO. Spotify's response has <c>expires_in</c> (seconds from now); we
/// convert that to an absolute <see cref="ExpiresAt"/> inside the client (using an injected clock) so
/// callers store one unambiguous instant and never have to remember "seconds from when?".
/// </summary>
public sealed record SpotifyTokenResult(
	string AccessToken,

	// Nullable on purpose: Spotify returns a refresh_token when you exchange the auth code, but a
	// REFRESH response often omits it — you're expected to keep reusing the one you already have.
	// The caller treats null as "keep the existing refresh token".
	string? RefreshToken,

	// Absolute expiry (now + expires_in), computed in the client. Stored on SpotifyConnection.ExpiresAt.
	DateTimeOffset ExpiresAt,

	// The space-separated scopes Spotify actually granted — may differ from what we asked for.
	string Scope);
