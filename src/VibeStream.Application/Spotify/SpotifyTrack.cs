namespace VibeStream.Application.Spotify;

/// <summary>
/// A track as Spotify describes it, flattened to what VibeStream stores. The Infrastructure client
/// maps Spotify's richer JSON down to this; the ingestion pipeline (M4) normalizes it into the
/// <c>Track</c> and <c>Artist</c> entities.
/// </summary>
public sealed record SpotifyTrack(
	// Spotify's track id → Track.SpotifyTrackId (natural key).
	string Id,

	string Name,

	int DurationMs,

	string AlbumName,

	// Nullable: not every album has cover art in the response.
	string? AlbumImageUrl,

	// SIMPLIFICATION: Spotify tracks can list several artists; our model keeps one primary artist per
	// track (see the Track entity), so we carry only the first/primary here. Documented in the plan's
	// trade-offs — good enough for a personal listening dashboard, and easy to extend later.
	SpotifyArtist Artist);

/// <summary>The primary artist of a <see cref="SpotifyTrack"/> — its id becomes Artist.SpotifyArtistId.</summary>
public sealed record SpotifyArtist(
	string Id,
	string Name);
