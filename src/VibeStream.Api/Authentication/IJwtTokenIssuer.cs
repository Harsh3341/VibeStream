namespace VibeStream.Api.Authentication;

/// <summary>
/// Issues signed JWTs for an authenticated VibeStream user. Called by the OAuth callback once the
/// Spotify identity is confirmed and the user row exists.
/// </summary>
public interface IJwtTokenIssuer
{
	/// <summary>
	/// Mints a token whose subject is the VibeStream <paramref name="userId"/> (the value downstream
	/// endpoints read to know who's calling). <paramref name="spotifyUserId"/> rides along as a
	/// convenience claim.
	/// </summary>
	JwtToken IssueToken(Guid userId, string spotifyUserId);
}

/// <summary>A minted token and the instant it expires — the cookie is set to expire at the same time.</summary>
public sealed record JwtToken(string Value, DateTimeOffset ExpiresAt);
