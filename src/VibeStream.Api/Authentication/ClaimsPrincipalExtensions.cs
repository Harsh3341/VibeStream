using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace VibeStream.Api.Authentication;

/// <summary>
/// Reads VibeStream's identity out of the authenticated principal. The JWT's "sub" claim carries the
/// user's Guid (see <see cref="JwtTokenIssuer"/>), so authenticated endpoints get the current user with
/// one call instead of digging through claims each time.
/// </summary>
public static class ClaimsPrincipalExtensions
{
	public static Guid GetUserId(this ClaimsPrincipal principal)
	{
		string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

		if (Guid.TryParse(subject, out Guid userId))
		{
			return userId;
		}

		// Reaching here means a request passed [Authorize] but the token lacks a usable subject — a
		// misconfiguration, not a client error, so it throws rather than returning a wrong user's data.
		throw new InvalidOperationException("The authenticated principal has no valid 'sub' user id.");
	}
}
