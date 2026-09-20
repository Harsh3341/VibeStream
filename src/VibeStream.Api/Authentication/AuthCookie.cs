namespace VibeStream.Api.Authentication;

/// <summary>
/// The one place the auth cookie is defined. The JWT rides in an HttpOnly cookie rather than a body/
/// header the SPA can read, so a cross-site script can't steal it — the browser attaches it
/// automatically and JavaScript never sees the value (plan §27: tokens must not reach the frontend).
/// </summary>
public static class AuthCookie
{
	public const string Name = "vibestream_auth";

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
}
