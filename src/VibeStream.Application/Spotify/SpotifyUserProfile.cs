namespace VibeStream.Application.Spotify;

/// <summary>
/// A Spotify user's public profile. Fetched right after the OAuth callback to identify the user:
/// <see cref="Id"/> becomes <c>User.SpotifyUserId</c> (the natural key we look them up by).
/// </summary>
public sealed record SpotifyUserProfile(
	string Id,

	// Spotify doesn't guarantee a display name, so it's nullable here and on the User entity.
	string? DisplayName,

	// Requires the user-read-email scope. Nullable in case it's absent.
	string? Email);
