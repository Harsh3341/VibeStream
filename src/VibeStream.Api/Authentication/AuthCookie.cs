namespace VibeStream.Api.Authentication;

/// <summary>
/// The one place the auth cookie is defined. The JWT rides in an HttpOnly cookie rather than a body/
/// header the SPA can read, so a cross-site script can't steal it — the browser attaches it
/// automatically and JavaScript never sees the value (plan §27: tokens must not reach the frontend).
/// </summary>
public static class AuthCookie
{
	public const string Name = "vibestream_auth";

	// Short-lived cookie holding the OAuth `state` value between /login and /callback. It's compared
	// against the state Spotify echoes back, which ties the callback to the redirect WE started —
	// an attacker can't forge a callback because they can't set this cookie in the victim's browser.
	public const string StateCookieName = "vibestream_oauth_state";

	/// <summary>
	/// Builds the cookie policy for the auth token.
	/// </summary>
	/// <param name="expiresAt">Matches the JWT's own expiry, so the cookie and token die together.</param>
	/// <param name="secure">
	/// HTTPS-only when true. Production must set this; local dev over http://127.0.0.1 relaxes it, since
	/// a Secure cookie is silently dropped on a non-HTTPS response.
	/// </param>
	public static CookieOptions Build(DateTimeOffset expiresAt, bool secure)
	{
		return new CookieOptions
		{
			// The core protection: unreadable from JavaScript.
			HttpOnly = true,

			Secure = secure,

			// Lax lets the browser send the cookie on the top-level redirect back from Spotify AND on
			// same-site requests from our own SPA, while still blocking it on cross-site sub-requests —
			// a pragmatic CSRF defence for a cookie-auth API.
			SameSite = SameSiteMode.Lax,

			Expires = expiresAt,

			Path = "/",
		};
	}

	/// <summary>
	/// Builds the short-lived cookie that carries the OAuth <c>state</c> across the redirect to Spotify
	/// and back. SameSite=Lax is required: the callback is a top-level navigation from Spotify's domain,
	/// and Lax (unlike Strict) still sends the cookie on that return trip.
	/// </summary>
	public static CookieOptions BuildStateCookie(bool secure)
	{
		return new CookieOptions
		{
			HttpOnly = true,
			Secure = secure,
			SameSite = SameSiteMode.Lax,

			// Enough time to authorize with Spotify, not enough to linger. The cookie is deleted on the
			// callback regardless.
			MaxAge = TimeSpan.FromMinutes(10),

			Path = "/",
		};
	}
}
