using VibeStream.Application.Sessions;

namespace VibeStream.UnitTests.Sessions;

/// <summary>
/// Covers the six cases the plan calls out for the gap rule (§12): empty, one track, continuous,
/// the exact 15-minute boundary, over the boundary, and multiple sessions. The detector is pure, so
/// these run with no database or Spotify — just data in, sessions out.
/// </summary>
public class SessionDetectorTests
{
	private static readonly DateTimeOffset Origin = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

	private readonly SessionDetector _detector = new();

	private static PlayedTrack PlayAt(TimeSpan offsetFromOrigin)
	{
		return new PlayedTrack(Guid.NewGuid(), Origin + offsetFromOrigin);
	}

	[Fact]
	public void EmptyHistory_ProducesNoSessions()
	{
		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions([]);

		Assert.Empty(sessions);
	}

	[Fact]
	public void SingleTrack_ProducesOneSessionSpanningThatMoment()
	{
		PlayedTrack play = PlayAt(TimeSpan.Zero);

		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions([play]);

		DetectedSession session = Assert.Single(sessions);
		Assert.Equal(play.PlayedAt, session.StartedAt);
		Assert.Equal(play.PlayedAt, session.EndedAt);
		Assert.Single(session.Plays);
	}

	[Fact]
	public void ContinuousTracksWithinGap_StayInOneSession()
	{
		List<PlayedTrack> plays =
		[
			PlayAt(TimeSpan.Zero),
			PlayAt(TimeSpan.FromMinutes(4)),
			PlayAt(TimeSpan.FromMinutes(9)),
			PlayAt(TimeSpan.FromMinutes(14)),
		];

		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions(plays);

		DetectedSession session = Assert.Single(sessions);
		Assert.Equal(4, session.Plays.Count);
		Assert.Equal(plays[0].PlayedAt, session.StartedAt);
		Assert.Equal(plays[^1].PlayedAt, session.EndedAt);
	}

	[Fact]
	public void ExactlyFifteenMinuteGap_StaysInSameSession()
	{
		List<PlayedTrack> plays =
		[
			PlayAt(TimeSpan.Zero),
			PlayAt(SessionDetector.MaxGap),
		];

		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions(plays);

		DetectedSession session = Assert.Single(sessions);
		Assert.Equal(2, session.Plays.Count);
	}

	[Fact]
	public void GapGreaterThanThreshold_StartsANewSession()
	{
		List<PlayedTrack> plays =
		[
			PlayAt(TimeSpan.Zero),
			PlayAt(SessionDetector.MaxGap + TimeSpan.FromSeconds(1)),
		];

		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions(plays);

		Assert.Equal(2, sessions.Count);
		Assert.Single(sessions[0].Plays);
		Assert.Single(sessions[1].Plays);
	}

	[Fact]
	public void MultipleSessions_AreSplitAtEveryLargeGap()
	{
		List<PlayedTrack> plays =
		[
			// Session 1: 10:00, 10:04, 10:09, 10:14
			PlayAt(TimeSpan.Zero),
			PlayAt(TimeSpan.FromMinutes(4)),
			PlayAt(TimeSpan.FromMinutes(9)),
			PlayAt(TimeSpan.FromMinutes(14)),

			// Session 2: a single play at 12:30, well over 15 minutes after session 1.
			PlayAt(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30)),

			// Session 3: 13:00 and 13:05 — 30 min after the 12:30 play (new session), then 5 min apart.
			PlayAt(TimeSpan.FromHours(3)),
			PlayAt(TimeSpan.FromHours(3) + TimeSpan.FromMinutes(5)),
		];

		IReadOnlyList<DetectedSession> sessions = _detector.DetectSessions(plays);

		Assert.Equal(3, sessions.Count);
		Assert.Equal(4, sessions[0].Plays.Count);
		Assert.Single(sessions[1].Plays);
		Assert.Equal(2, sessions[2].Plays.Count);
	}
}
