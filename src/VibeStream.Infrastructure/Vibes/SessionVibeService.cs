using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Vibes;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Vibes;

/// <summary>
/// Gives each session a vibe: profile every track in the session, average the profiles, classify the
/// result, and store it as a <c>VibeSnapshot</c>. Only sessions WITHOUT a snapshot are computed, so this
/// is cheap on repeat runs. It also clears orphaned snapshots — when the session rebuild deletes the
/// latest session, its snapshot's SessionId is SET NULL, and those null-linked rows are cleaned up here
/// (we only ever persist session-linked snapshots). Scoped.
/// </summary>
public sealed class SessionVibeService : ISessionVibeService
{
	private readonly VibeStreamDbContext _dbContext;
	private readonly IAudioProfiler _audioProfiler;
	private readonly IVibeClassifier _vibeClassifier;
	private readonly TimeProvider _timeProvider;

	public SessionVibeService(
		VibeStreamDbContext dbContext,
		IAudioProfiler audioProfiler,
		IVibeClassifier vibeClassifier,
		TimeProvider timeProvider)
	{
		_dbContext = dbContext;
		_audioProfiler = audioProfiler;
		_vibeClassifier = vibeClassifier;
		_timeProvider = timeProvider;
	}

	public async Task RebuildSessionVibesAsync(Guid userId, CancellationToken cancellationToken)
	{
		// Remove snapshots orphaned by a session rebuild (SessionId set null). We never persist a
		// standalone snapshot, so a null SessionId here always means "orphan".
		await _dbContext.VibeSnapshots
			.Where(snapshot => snapshot.UserId == userId && snapshot.SessionId == null)
			.ExecuteDeleteAsync(cancellationToken);

		// Sessions that still need a vibe.
		List<Guid> sessionIds = await _dbContext.ListeningSessions
			.Where(session => session.UserId == userId
				&& !_dbContext.VibeSnapshots.Any(snapshot => snapshot.SessionId == session.Id))
			.Select(session => session.Id)
			.ToListAsync(cancellationToken);

		if (sessionIds.Count == 0)
		{
			return;
		}

		DateTimeOffset now = _timeProvider.GetUtcNow();

		foreach (Guid sessionId in sessionIds)
		{
			List<TrackVibeInput> inputs = await _dbContext.SessionTracks
				.Where(sessionTrack => sessionTrack.SessionId == sessionId)
				.Select(sessionTrack => new TrackVibeInput(
					sessionTrack.Track.Popularity,
					sessionTrack.Track.ReleaseYear,
					sessionTrack.Track.DurationMs,
					sessionTrack.PlayedAt))
				.ToListAsync(cancellationToken);

			if (inputs.Count == 0)
			{
				continue;
			}

			List<AudioProfile> profiles = inputs
				.Select(input => _audioProfiler.Profile(new TrackSignals(
					input.Popularity,
					input.ReleaseYear,
					input.DurationMs,
					input.PlayedAt.UtcDateTime.Hour)))
				.ToList();

			AudioProfile sessionProfile = AudioProfile.Average(profiles);
			VibeResult vibe = _vibeClassifier.Classify(sessionProfile);

			_dbContext.VibeSnapshots.Add(new VibeSnapshot
			{
				UserId = userId,
				SessionId = sessionId,
				Name = vibe.Name,
				Energy = sessionProfile.Energy,
				Valence = sessionProfile.Valence,
				Danceability = sessionProfile.Danceability,
				Confidence = sessionProfile.Confidence,
				CreatedAt = now,
			});
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	/// <summary>The per-track fields needed to profile a session track. Named so it survives materialization.</summary>
	private sealed record TrackVibeInput(int Popularity, int? ReleaseYear, int DurationMs, DateTimeOffset PlayedAt);
}
