using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using VibeStream.Application.Ingestion;
using VibeStream.Application.Spotify;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Ingestion;

/// <summary>
/// Fetches recently-played, normalizes it into Artist/Track reference data, and idempotently inserts
/// ListeningEvents. Scoped (uses the scoped DbContext). Idempotency has two layers: an in-memory filter
/// against already-stored keys (so overlapping polls don't churn), and the DB's unique index on
/// (UserId, TrackId, PlayedAt) as the ultimate guarantee (plan §11).
/// </summary>
public sealed class ListeningIngestionService : IListeningIngestionService
{
	private readonly ISpotifyTokenService _tokenService;
	private readonly ISpotifyClient _spotifyClient;
	private readonly VibeStreamDbContext _dbContext;
	private readonly TimeProvider _timeProvider;
	private readonly ILogger<ListeningIngestionService> _logger;

	public ListeningIngestionService(
		ISpotifyTokenService tokenService,
		ISpotifyClient spotifyClient,
		VibeStreamDbContext dbContext,
		TimeProvider timeProvider,
		ILogger<ListeningIngestionService> logger)
	{
		_tokenService = tokenService;
		_spotifyClient = spotifyClient;
		_dbContext = dbContext;
		_timeProvider = timeProvider;
		_logger = logger;
	}

	public async Task<int> IngestRecentPlaysAsync(Guid userId, CancellationToken cancellationToken)
	{
		string accessToken = await _tokenService.GetValidAccessTokenAsync(userId, cancellationToken);

		SpotifyRecentlyPlayedResult recent = await _spotifyClient.GetRecentlyPlayedAsync(
			accessToken, cancellationToken);

		if (recent.Items.Count == 0)
		{
			return 0;
		}

		Dictionary<string, Track> tracksBySpotifyId = await NormalizeTracksAsync(recent, cancellationToken);

		int inserted = await PersistEventsAsync(userId, recent, tracksBySpotifyId, cancellationToken);

		_logger.LogInformation(
			"Ingested {Inserted} new play(s) of {Fetched} fetched for user {UserId}.",
			inserted,
			recent.Items.Count,
			userId);

		return inserted;
	}

	/// <summary>
	/// Ensures every artist and track in the batch exists, then persists the reference data on its own.
	/// Saving it separately means an event-level conflict later can't roll back the tracks/artists.
	/// </summary>
	private async Task<Dictionary<string, Track>> NormalizeTracksAsync(
		SpotifyRecentlyPlayedResult recent,
		CancellationToken cancellationToken)
	{
		List<SpotifyArtist> batchArtists = recent.Items
			.Select(item => item.Track.Artist)
			.DistinctBy(artist => artist.Id)
			.ToList();

		HashSet<string> artistSpotifyIds = batchArtists.Select(artist => artist.Id).ToHashSet();

		Dictionary<string, Artist> artists = await _dbContext.Artists
			.Where(artist => artistSpotifyIds.Contains(artist.SpotifyArtistId))
			.ToDictionaryAsync(artist => artist.SpotifyArtistId, cancellationToken);

		foreach (SpotifyArtist source in batchArtists)
		{
			if (!artists.ContainsKey(source.Id))
			{
				Artist artist = new()
				{
					SpotifyArtistId = source.Id,
					Name = source.Name,
				};

				_dbContext.Artists.Add(artist);
				artists[source.Id] = artist;
			}
		}

		List<SpotifyTrack> batchTracks = recent.Items
			.Select(item => item.Track)
			.DistinctBy(track => track.Id)
			.ToList();

		HashSet<string> trackSpotifyIds = batchTracks.Select(track => track.Id).ToHashSet();

		Dictionary<string, Track> tracks = await _dbContext.Tracks
			.Where(track => trackSpotifyIds.Contains(track.SpotifyTrackId))
			.ToDictionaryAsync(track => track.SpotifyTrackId, cancellationToken);

		foreach (SpotifyTrack source in batchTracks)
		{
			if (!tracks.ContainsKey(source.Id))
			{
				Track track = new()
				{
					SpotifyTrackId = source.Id,
					Name = source.Name,

					// The artist was ensured above, so its app-generated Id is available without a save.
					ArtistId = artists[source.Artist.Id].Id,
					AlbumName = source.AlbumName,
					AlbumImageUrl = source.AlbumImageUrl,
					DurationMs = source.DurationMs,
				};

				_dbContext.Tracks.Add(track);
				tracks[source.Id] = track;
			}
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return tracks;
	}

	private async Task<int> PersistEventsAsync(
		Guid userId,
		SpotifyRecentlyPlayedResult recent,
		Dictionary<string, Track> tracksBySpotifyId,
		CancellationToken cancellationToken)
	{
		// Preload the keys we might collide with: this user's events since the oldest play in the batch.
		// The unique index is the real guard; this just avoids provoking it on every overlapping poll.
		DateTimeOffset earliest = recent.Items.Min(item => item.PlayedAt);

		HashSet<(Guid TrackId, DateTimeOffset PlayedAt)> seen = (await _dbContext.ListeningEvents
				.Where(listeningEvent => listeningEvent.UserId == userId && listeningEvent.PlayedAt >= earliest)
				.Select(listeningEvent => new { listeningEvent.TrackId, listeningEvent.PlayedAt })
				.ToListAsync(cancellationToken))
			.Select(key => (key.TrackId, key.PlayedAt))
			.ToHashSet();

		DateTimeOffset now = _timeProvider.GetUtcNow();
		List<ListeningEvent> newEvents = [];

		foreach (SpotifyPlayHistoryItem item in recent.Items)
		{
			Track track = tracksBySpotifyId[item.Track.Id];

			// HashSet.Add returns false if the key was already present — one call dedupes against BOTH
			// what's in the DB and earlier items in this same batch.
			if (!seen.Add((track.Id, item.PlayedAt)))
			{
				continue;
			}

			newEvents.Add(new ListeningEvent
			{
				UserId = userId,
				TrackId = track.Id,
				PlayedAt = item.PlayedAt,
				CreatedAt = now,
			});
		}

		if (newEvents.Count == 0)
		{
			return 0;
		}

		_dbContext.ListeningEvents.AddRange(newEvents);

		try
		{
			await _dbContext.SaveChangesAsync(cancellationToken);
			return newEvents.Count;
		}
		catch (DbUpdateException exception) when (IsUniqueViolation(exception))
		{
			// A concurrent writer beat us to one of these keys (shouldn't happen with a single worker,
			// but the index makes it safe). The desired end state — the row exists — already holds, so
			// detach the pending inserts and let the next cycle reconcile.
			_logger.LogWarning(
				exception,
				"Unique conflict inserting listening events for user {UserId}; duplicates skipped.",
				userId);

			foreach (ListeningEvent pending in newEvents)
			{
				_dbContext.Entry(pending).State = EntityState.Detached;
			}

			return 0;
		}
	}

	private static bool IsUniqueViolation(DbUpdateException exception)
	{
		return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
	}
}
