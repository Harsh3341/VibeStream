namespace VibeStream.Application.Spotify;

/// <summary>
/// Orchestrates the "user just came back from Spotify" step of the OAuth flow: exchange the code,
/// identify the user, and persist their account + tokens. Lives behind an abstraction so the API's
/// controller stays thin and testable — it hands over a code and gets back who to issue a JWT for.
/// </summary>
public interface ISpotifyAuthenticationService
{
	/// <summary>
	/// Completes login for a callback authorization <paramref name="code"/>: exchanges it for tokens,
	/// fetches the Spotify profile, and upserts the <c>User</c> + <c>SpotifyConnection</c>. Returns the
	/// identifiers the caller needs to mint an auth token.
	/// </summary>
	Task<AuthenticatedUser> CompleteLoginAsync(string code, CancellationToken cancellationToken);
}

/// <summary>The persisted user's identity — enough for the API to issue a JWT, nothing more.</summary>
public sealed record AuthenticatedUser(Guid UserId, string SpotifyUserId);
