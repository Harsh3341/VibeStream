namespace VibeStream.Application.Sessions;

/// <summary>
/// Groups consecutive plays into listening sessions using the gap rule (plan §12). PURE and
/// dependency-free: it takes ordered plays and returns session boundaries, nothing more — which is
/// exactly what makes the rule easy to unit-test in isolation from the database and Spotify.
/// </summary>
public interface ISessionDetector
{
	/// <summary>
	/// Splits <paramref name="orderedPlays"/> (ascending by PlayedAt) into sessions. A gap of more than
	/// the threshold between two consecutive plays starts a new session.
	/// </summary>
	IReadOnlyList<DetectedSession> DetectSessions(IReadOnlyList<PlayedTrack> orderedPlays);
}

/// <summary>A single play: which track, and when. The minimal input the gap rule needs.</summary>
public sealed record PlayedTrack(Guid TrackId, DateTimeOffset PlayedAt);

/// <summary>A detected session: its span and the ordered plays it contains.</summary>
public sealed record DetectedSession(
	DateTimeOffset StartedAt,
	DateTimeOffset EndedAt,
	IReadOnlyList<PlayedTrack> Plays);
