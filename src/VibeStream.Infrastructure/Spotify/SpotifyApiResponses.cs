namespace VibeStream.Infrastructure.Spotify;

// The raw shapes of the Spotify Web API JSON we consume. All INTERNAL to Infrastructure: they exist
// only for deserialization and are mapped to the clean Application DTOs by SpotifyApiClient. Property
// names bind from snake_case via SpotifyJson (display_name → DisplayName, duration_ms → DurationMs).

/// <summary>Response of <c>GET /v1/me</c>.</summary>
internal sealed record SpotifyUserResponse
{
	public string Id { get; init; } = string.Empty;

	public string? DisplayName { get; init; }

	public string? Email { get; init; }
}

/// <summary>Response of <c>GET /v1/me/player/recently-played</c>.</summary>
internal sealed record SpotifyRecentlyPlayedResponse
{
	public IReadOnlyList<SpotifyPlayHistoryItemResponse> Items { get; init; } = [];
}

internal sealed record SpotifyPlayHistoryItemResponse
{
	public SpotifyTrackResponse Track { get; init; } = new();

	// ISO 8601 timestamp — System.Text.Json parses it straight into DateTimeOffset.
	public DateTimeOffset PlayedAt { get; init; }
}

/// <summary>Response of <c>GET /v1/me/player/currently-playing</c> (200; the endpoint 204s when idle).</summary>
internal sealed record SpotifyCurrentlyPlayingResponse
{
	public bool IsPlaying { get; init; }

	public int ProgressMs { get; init; }

	// Null when the current item isn't a track (e.g. an ad or podcast episode).
	public SpotifyTrackResponse? Item { get; init; }
}

internal sealed record SpotifyTrackResponse
{
	public string Id { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public int DurationMs { get; init; }

	public SpotifyAlbumResponse? Album { get; init; }

	public IReadOnlyList<SpotifyArtistResponse> Artists { get; init; } = [];
}

internal sealed record SpotifyAlbumResponse
{
	public string Name { get; init; } = string.Empty;

	// Ordered largest-first by Spotify, so the first image is the highest resolution.
	public IReadOnlyList<SpotifyImageResponse> Images { get; init; } = [];
}

internal sealed record SpotifyArtistResponse
{
	public string Id { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;
}

internal sealed record SpotifyImageResponse
{
	public string Url { get; init; } = string.Empty;
}
