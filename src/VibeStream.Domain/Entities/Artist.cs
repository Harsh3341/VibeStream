namespace VibeStream.Domain.Entities;

/// <summary>
/// A Spotify artist. We store our own copy (rather than re-fetching from Spotify) so analytics and
/// history queries hit our database, not the Spotify API.
/// </summary>
public class Artist
{
	// Version-7 GUID: unique like any GUID, but TIME-ORDERED, so inserts append to the end of the
	// primary-key index instead of scattering across it. Generated in the app (not by the database),
	// so we know the id the moment we create the object — no round-trip, and it's testable.
	public Guid Id { get; set; } = Guid.CreateVersion7();

	// Spotify's own id for this artist (e.g. "0OdUWJ0sBjDrqHygGUXeCF"). This is the NATURAL key we
	// dedupe on — we'll put a UNIQUE index on it in the EF config (step 2), so we never store the
	// same artist twice.
	public required string SpotifyArtistId { get; set; }

	public required string Name { get; set; }
}
