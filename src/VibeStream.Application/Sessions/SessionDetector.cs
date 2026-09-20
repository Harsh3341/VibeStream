namespace VibeStream.Application.Sessions;

/// <summary>
/// The initial gap-based session rule (plan §12): consecutive plays no more than <see cref="MaxGap"/>
/// apart belong to the same session; a larger gap starts a new one. Stateless, so it's registered as a
/// singleton and shared freely.
/// </summary>
public sealed class SessionDetector : ISessionDetector
{
	// A gap of EXACTLY 15 minutes keeps the same session (<=); anything longer splits. This boundary is
	// covered by a unit test so the "<=" can't silently drift to "<".
	public static readonly TimeSpan MaxGap = TimeSpan.FromMinutes(15);

	public IReadOnlyList<DetectedSession> DetectSessions(IReadOnlyList<PlayedTrack> orderedPlays)
	{
		List<DetectedSession> sessions = [];

		if (orderedPlays.Count == 0)
		{
			return sessions;
		}

		// Walk the ordered plays, accumulating into the current session until a gap breaks it.
		List<PlayedTrack> current = [orderedPlays[0]];

		for (int index = 1; index < orderedPlays.Count; index++)
		{
			TimeSpan gap = orderedPlays[index].PlayedAt - orderedPlays[index - 1].PlayedAt;

			if (gap <= MaxGap)
			{
				current.Add(orderedPlays[index]);
			}
			else
			{
				sessions.Add(BuildSession(current));
				current = [orderedPlays[index]];
			}
		}

		// The final open session always has at least the last play in it.
		sessions.Add(BuildSession(current));

		return sessions;
	}

	private static DetectedSession BuildSession(List<PlayedTrack> plays)
	{
		return new DetectedSession(plays[0].PlayedAt, plays[^1].PlayedAt, plays);
	}
}
