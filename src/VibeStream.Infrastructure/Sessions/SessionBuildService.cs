using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Sessions;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Sessions;

/// <summary>
/// Persists detected sessions. Uses a "recompute the tail" strategy: only the most recent session can
/// be affected by newer plays (recently-played only moves forward in time), so we delete just that one
/// and rebuild from its start. Earlier sessions are already sealed by a &gt;15-minute gap and are left
/// untouched — which also means their VibeSnapshot links (M8) survive. Scoped: uses the DbContext.
/// </summary>
public sealed class SessionBuildService : ISessionBuildService
{
	private readonly VibeStreamDbContext _dbContext;
	private readonly ISessionDetector _sessionDetector;
	private readonly TimeProvider _timeProvider;

	public SessionBuildService(
		VibeStreamDbContext dbContext,
		ISessionDetector sessionDetector,
		TimeProvider timeProvider)
	{
		_dbContext = dbContext;
		_sessionDetector = sessionDetector;
		_timeProvider = timeProvider;
	}

	public async Task<int> RebuildSessionsAsync(Guid userId, CancellationToken cancellationToken)
	{
		ListeningSession? lastSession = await _dbContext.ListeningSessions
			.Where(session => session.UserId == userId)
			.OrderByDescending(session => session.StartedAt)
			.FirstOrDefaultAsync(cancellationToken);

		// Recompute from the last session's start (or from the beginning if there are none yet).
		DateTimeOffset from = lastSession?.StartedAt ?? DateTimeOffset.MinValue;

		if (lastSession is not null)
		{
			// It's about to be recomputed (possibly extended). Its SessionTracks cascade-delete at the DB.
			_dbContext.ListeningSessions.Remove(lastSession);
		}

		List<PlayedTrack> plays = await _dbContext.ListeningEvents
			.Where(listeningEvent => listeningEvent.UserId == userId && listeningEvent.PlayedAt >= from)
			.OrderBy(listeningEvent => listeningEvent.PlayedAt)
			.Select(listeningEvent => new PlayedTrack(listeningEvent.TrackId, listeningEvent.PlayedAt))
			.ToListAsync(cancellationToken);

		if (plays.Count == 0)
		{
			// Nothing to rebuild — still save so a pending delete (if any) is applied.
			await _dbContext.SaveChangesAsync(cancellationToken);
			return 0;
		}

		IReadOnlyList<DetectedSession> detected = _sessionDetector.DetectSessions(plays);
		DateTimeOffset now = _timeProvider.GetUtcNow();

		foreach (DetectedSession detectedSession in detected)
		{
			ListeningSession session = new()
			{
				UserId = userId,
				StartedAt = detectedSession.StartedAt,
				EndedAt = detectedSession.EndedAt,

				// Denormalized count — the SessionTrack rows are the source of truth, this is the cache.
				TrackCount = detectedSession.Plays.Count,
				CreatedAt = now,
			};

			for (int position = 0; position < detectedSession.Plays.Count; position++)
			{
				PlayedTrack play = detectedSession.Plays[position];

				// SessionId is set explicitly (it's a required member) and equals the session's own
				// ctor-generated Guid — no save needed to know it.
				session.Tracks.Add(new SessionTrack
				{
					SessionId = session.Id,
					Position = position,
					TrackId = play.TrackId,
					PlayedAt = play.PlayedAt,
				});
			}

			_dbContext.ListeningSessions.Add(session);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return detected.Count;
	}
}
