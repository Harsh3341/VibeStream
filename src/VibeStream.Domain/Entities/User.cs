namespace VibeStream.Domain.Entities;

/// <summary>
/// A VibeStream user, identified by their Spotify account. One row per connected Spotify user.
/// </summary>
public class User
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	// Spotify's user id — the natural key we look this user up by. UNIQUE index in the config.
	public required string SpotifyUserId { get; set; }

	// Nullable: Spotify doesn't always return a display name, so we can't promise one.
	public string? DisplayName { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }
}
