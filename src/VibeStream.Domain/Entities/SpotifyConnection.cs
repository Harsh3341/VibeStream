namespace VibeStream.Domain.Entities;

/// <summary>
/// A user's Spotify OAuth tokens. Server-side only — these NEVER reach the browser.
/// One connection per user (enforced by a unique index on UserId in the config).
/// </summary>
public class SpotifyConnection
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	// FK + navigation — same pattern as Track → Artist.
	public required Guid UserId { get; set; }

	public User User { get; set; } = null!;

	// Short-lived: used to call the Spotify API on the user's behalf.
	public required string AccessToken { get; set; }

	// Long-lived: used to mint a new AccessToken when the current one expires.
	public required string RefreshToken { get; set; }

	// When AccessToken stops working. The worker checks this and refreshes as needed (M3/M4).
	public DateTimeOffset ExpiresAt { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }
}
