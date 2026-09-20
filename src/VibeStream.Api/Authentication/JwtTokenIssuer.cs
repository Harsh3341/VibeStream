using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace VibeStream.Api.Authentication;

/// <summary>
/// Issues HMAC-SHA256-signed JWTs. A JWT is SIGNED, not encrypted — anyone can base64-decode it and
/// read the claims (try jwt.io), so the signature guarantees integrity, not secrecy. Never put a
/// secret in a claim. The Spotify tokens, notably, stay in the database and never go in here.
/// </summary>
public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
	private readonly JwtOptions _options;
	private readonly TimeProvider _timeProvider;

	public JwtTokenIssuer(
		// IOptions (not Snapshot/Monitor): rotating the signing key mid-process would invalidate every
		// token already issued, so there's nothing to gain from hot-reloading it.
		IOptions<JwtOptions> options,
		TimeProvider timeProvider)
	{
		_options = options.Value;
		_timeProvider = timeProvider;
	}

	public JwtToken IssueToken(Guid userId, string spotifyUserId)
	{
		DateTimeOffset issuedAt = _timeProvider.GetUtcNow();
		DateTimeOffset expiresAt = issuedAt.AddMinutes(_options.ExpiryMinutes);

		List<Claim> claims =
		[
			// "sub" — the subject: which VibeStream user this token is about.
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),

			// Convenience claim so a handler can see the Spotify id without a DB hit.
			new Claim(VibeStreamClaims.SpotifyUserId, spotifyUserId),

			// "jti" — unique token id; the key a revocation list would use.
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

			// "iat" — issued-at as Unix seconds (the format the spec requires).
			new Claim(
				JwtRegisteredClaimNames.Iat,
				issuedAt.ToUnixTimeSeconds().ToString(),
				ClaimValueTypes.Integer64),
		];

		// HMAC-SHA256: one symmetric secret both signs and verifies. Fine when a single service does
		// both, as here. Across trust boundaries you'd reach for RS256 so verifiers hold only the
		// public key and can't mint their own tokens.
		SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(_options.SigningKey));
		SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

		JwtSecurityToken token = new(
			issuer: _options.Issuer,
			audience: _options.Audience,
			claims: claims,
			notBefore: issuedAt.UtcDateTime,
			expires: expiresAt.UtcDateTime,
			signingCredentials: credentials);

		string value = new JwtSecurityTokenHandler().WriteToken(token);

		return new JwtToken(value, expiresAt);
	}
}

/// <summary>
/// Non-standard claim names, as constants — a misspelled claim string fails silently with no compiler
/// error, so we make it a build-time symbol instead.
/// </summary>
public static class VibeStreamClaims
{
	public const string SpotifyUserId = "spotify_user_id";
}
