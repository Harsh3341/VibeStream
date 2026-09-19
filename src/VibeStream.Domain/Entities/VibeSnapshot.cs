namespace VibeStream.Domain.Entities;

/// <summary>
/// A computed "vibe" — the deterministic classifier's output (M8), saved for history.
/// Can belong to a session, or stand alone as a moment-in-time ("current vibe").
/// </summary>
public class VibeSnapshot
{
	public Guid Id { get; set; } = Guid.CreateVersion7();

	public required Guid UserId { get; set; }

	// Nullable: a vibe can describe a SESSION, or a moment with no session ("current vibe").
	// A nullable FK is how you model an OPTIONAL relationship.
	public Guid? SessionId { get; set; }

	// The label the classifier produced ("Chill", "High Energy", "Late Night", …).
	public required string Name { get; set; }

	// Derived scores, 0.0–1.0. Because Spotify's audio-features endpoint is deprecated, these come
	// from OUR deterministic engine over available metadata (popularity, genres, duration, release
	// year, time-of-day) — not from Spotify. This is decision #1 of the project, made concrete.
	public double Energy { get; set; }

	public double Valence { get; set; }

	public double Danceability { get; set; }

	// How much the engine trusts this classification (0.0–1.0) — lower when inputs are sparse.
	public double Confidence { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
}
