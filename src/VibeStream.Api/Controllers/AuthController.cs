using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Spotify;

namespace VibeStream.Api.Controllers;

/// <summary>
/// The Spotify OAuth endpoints. The browser drives this flow via top-level navigations, so login and
/// callback return REDIRECTS, not JSON. All three are anonymous — the whole point is to authenticate a
/// user who has no token yet.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
	private readonly ISpotifyOAuthClient _oauthClient;
	private readonly ISpotifyAuthenticationService _authenticationService;
	private readonly IJwtTokenIssuer _tokenIssuer;
	private readonly IConfiguration _configuration;

	public AuthController(
		ISpotifyOAuthClient oauthClient,
		ISpotifyAuthenticationService authenticationService,
		IJwtTokenIssuer tokenIssuer,
		IConfiguration configuration)
	{
		_oauthClient = oauthClient;
		_authenticationService = authenticationService;
		_tokenIssuer = tokenIssuer;
		_configuration = configuration;
	}

	/// <summary>Starts login: set the anti-CSRF state cookie and redirect to Spotify's consent screen.</summary>
	[HttpGet("spotify/login")]
	public IActionResult Login()
	{
		// 256 bits of randomness, hex-encoded (URL-safe as-is). This is the `state` we'll re-check.
		string state = RandomNumberGenerator.GetHexString(64);

		Response.Cookies.Append(AuthCookie.StateCookieName, state, AuthCookie.BuildStateCookie(Request.IsHttps));

		return Redirect(_oauthClient.BuildAuthorizationUrl(state));
	}

	/// <summary>
	/// Spotify redirects here after consent. Validate state, exchange the code, issue the JWT cookie,
	/// then bounce the browser to the frontend.
	/// </summary>
	[HttpGet("spotify/callback")]
	public async Task<IActionResult> Callback(
		[FromQuery] string? code,
		[FromQuery] string? state,
		[FromQuery] string? error,
		CancellationToken cancellationToken)
	{
		// The state cookie is single-use — clear it whatever happens next.
		Request.Cookies.TryGetValue(AuthCookie.StateCookieName, out string? expectedState);
		Response.Cookies.Delete(AuthCookie.StateCookieName);

		// The user declined on Spotify's screen (or Spotify reported a problem). Not an error on our side.
		if (!string.IsNullOrEmpty(error))
		{
			return Redirect(BuildFrontendUrl($"error={Uri.EscapeDataString(error)}"));
		}

		// State must be present on both sides and match, compared in fixed time so a mismatch can't be
		// probed by timing. A failure here means a forged/replayed callback — reject it outright.
		if (string.IsNullOrEmpty(state)
			|| string.IsNullOrEmpty(expectedState)
			|| !CryptographicOperations.FixedTimeEquals(
				Encoding.UTF8.GetBytes(state),
				Encoding.UTF8.GetBytes(expectedState)))
		{
			return BadRequest("Invalid OAuth state.");
		}

		if (string.IsNullOrEmpty(code))
		{
			return BadRequest("Missing authorization code.");
		}

		AuthenticatedUser user = await _authenticationService.CompleteLoginAsync(code, cancellationToken);

		JwtToken token = _tokenIssuer.IssueToken(user.UserId, user.SpotifyUserId);
		Response.Cookies.Append(AuthCookie.Name, token.Value, AuthCookie.Build(token.ExpiresAt, Request.IsHttps));

		return Redirect(BuildFrontendUrl());
	}

	/// <summary>Logs out by clearing the auth cookie. Idempotent, so it stays anonymous.</summary>
	[HttpPost("logout")]
	public IActionResult Logout()
	{
		Response.Cookies.Delete(AuthCookie.Name);

		return NoContent();
	}

	private string BuildFrontendUrl(string? query = null)
	{
		// Where to send the browser once auth is done. The SPA arrives in M9; until then this is
		// configurable and defaults to root.
		string baseUrl = _configuration["Frontend:BaseUrl"] ?? "/";

		return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
	}
}
