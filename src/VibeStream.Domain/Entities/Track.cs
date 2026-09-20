namespace VibeStream.Domain.Entities;

/// <summary>
/// A Spotify track, stored locally. Belongs to one <see cref="Artist"/>.
/// </summary>
public class Track
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	// Natural key from Spotify - gets a UNIQUE index in the EF config, same as Artist.
	public required string SpotifyTrackId { get; set; }

	public required string Name { get; set; }

	// ── The relationship to Artist ──────────────────────────────────────────────────────────────
	// The FK (ArtistId) is the column actually stored. The navigation (Artist) is the object you
	// read in code. EF ties them together from the configuration in step 2.
	public required Guid ArtistId { get; set; }

	public Artist Artist { get; set; } = null!;

	public required string AlbumName { get; set; }

	// Nullable on purpose: not every track has album art, and Spotify can return none.
	public string? AlbumImageUrl { get; set; }

	// Spotify returns track length in milliseconds. int holds ~24 days of ms — plenty.
	public int DurationMs { get; set; }

	// Spotify popularity, 0–100, captured at ingest. A real, available signal the Vibe engine (M8)
	// uses (mainstream vs niche) now that audio-features is deprecated.
	public int Popularity { get; set; }

	// Release year, parsed from the album's release date (whose precision varies). Nullable when
	// Spotify doesn't give one. Another Vibe input (nostalgic vs current).
	public int? ReleaseYear { get; set; }
}
