using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Listening;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Listening;

/// <summary>Reads profile, recent plays, and sessions for the UI. Scoped: uses the DbContext.</summary>
public sealed class ListeningQueryService : IListeningQueryService
{
	private readonly VibeStreamDbContext _dbContext;

	public ListeningQueryService(VibeStreamDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public Task<UserProfileView?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
	{
		return _dbContext.Users
			.Where(user => user.Id == userId)
			.Select(user => new UserProfileView(user.Id, user.SpotifyUserId, user.DisplayName))
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<RecentPlayView>> GetRecentPlaysAsync(
		Guid userId,
		int limit,
		CancellationToken cancellationToken)
	{
		return await _dbContext.ListeningEvents
			.Where(listeningEvent => listeningEvent.UserId == userId)
			.OrderByDescending(listeningEvent => listeningEvent.PlayedAt)
			.Take(limit)
			.Select(listeningEvent => new RecentPlayView(
				listeningEvent.TrackId,
				listeningEvent.Track.Name,
				listeningEvent.Track.Artist.Name,
				listeningEvent.Track.AlbumName,
				listeningEvent.Track.AlbumImageUrl,
				listeningEvent.PlayedAt))
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<SessionView>> GetSessionsAsync(
		Guid userId,
		int limit,
		CancellationToken cancellationToken)
	{
		return await _dbContext.ListeningSessions
			.Where(session => session.UserId == userId)
			.OrderByDescending(session => session.StartedAt)
			.Take(limit)
			.Select(session => new SessionView(
				session.Id,
				session.StartedAt,
				session.EndedAt,
				session.TrackCount,

				// Correlated subquery for the vibe label — LEFT-JOIN semantics, null when unclassified.
				_dbContext.VibeSnapshots
					.Where(snapshot => snapshot.SessionId == session.Id)
					.Select(snapshot => snapshot.Name)
					.FirstOrDefault()))
			.ToListAsync(cancellationToken);
	}

	public async Task<SessionDetailView?> GetSessionAsync(
		Guid userId,
		Guid sessionId,
		CancellationToken cancellationToken)
	{
		SessionView? header = await _dbContext.ListeningSessions
			.Where(session => session.Id == sessionId && session.UserId == userId)
			.Select(session => new SessionView(
				session.Id,
				session.StartedAt,
				session.EndedAt,
				session.TrackCount,
				_dbContext.VibeSnapshots
					.Where(snapshot => snapshot.SessionId == session.Id)
					.Select(snapshot => snapshot.Name)
					.FirstOrDefault()))
			.FirstOrDefaultAsync(cancellationToken);

		if (header is null)
		{
			return null;
		}

		List<SessionTrackView> tracks = await _dbContext.SessionTracks
			.Where(sessionTrack => sessionTrack.SessionId == sessionId)
			.OrderBy(sessionTrack => sessionTrack.Position)
			.Select(sessionTrack => new SessionTrackView(
				sessionTrack.Position,
				sessionTrack.TrackId,
				sessionTrack.Track.Name,
				sessionTrack.Track.Artist.Name,
				sessionTrack.Track.AlbumImageUrl,
				sessionTrack.PlayedAt))
			.ToListAsync(cancellationToken);

		return new SessionDetailView(
			header.Id,
			header.StartedAt,
			header.EndedAt,
			header.TrackCount,
			header.Vibe,
			tracks);
	}
}
