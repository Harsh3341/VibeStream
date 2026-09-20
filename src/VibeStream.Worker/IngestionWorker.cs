using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VibeStream.Application.Ingestion;
using VibeStream.Application.Sessions;
using VibeStream.Infrastructure.Data;
using VibeStream.Infrastructure.Ingestion;

namespace VibeStream.Worker;

/// <summary>
/// The background ingestion loop (plan §10): every poll interval, find connected users and import their
/// recent Spotify plays. A BackgroundService is a SINGLETON, so it never holds scoped services directly
/// — it opens a DI scope per unit of work to get a fresh DbContext, exactly as scoped services expect.
///
/// Failure is isolated at two levels: a whole cycle can fail (and the loop still ticks again), and a
/// single user can fail (and the other users still run). A revoked token or a Spotify blip degrades one
/// user's import, never the worker.
/// </summary>
public sealed class IngestionWorker : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IngestionOptions _options;
	private readonly ILogger<IngestionWorker> _logger;

	public IngestionWorker(
		IServiceScopeFactory scopeFactory,
		IOptions<IngestionOptions> options,
		ILogger<IngestionWorker> logger)
	{
		_scopeFactory = scopeFactory;
		_options = options.Value;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"Ingestion worker started; polling every {IntervalSeconds}s.",
			_options.PollIntervalSeconds);

		using PeriodicTimer timer = new(_options.PollInterval);

		// Run one cycle immediately, then once per tick. The do/while keeps the first run from waiting a
		// full interval on startup.
		do
		{
			try
			{
				await RunCycleAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception exception)
			{
				// A cycle-level failure (e.g. the database is momentarily down) must not kill the loop.
				_logger.LogError(exception, "Ingestion cycle failed; retrying next interval.");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));

		_logger.LogInformation("Ingestion worker stopping.");
	}

	private async Task RunCycleAsync(CancellationToken cancellationToken)
	{
		List<Guid> userIds = await LoadConnectedUserIdsAsync(cancellationToken);

		if (userIds.Count == 0)
		{
			_logger.LogDebug("No connected users to ingest this cycle.");
			return;
		}

		foreach (Guid userId in userIds)
		{
			// A scope PER USER → a fresh DbContext per user, so one user's tracked entities never bleed
			// into the next and the change tracker stays small.
			await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

			IListeningIngestionService ingestion =
				scope.ServiceProvider.GetRequiredService<IListeningIngestionService>();
			ISessionBuildService sessionBuilder =
				scope.ServiceProvider.GetRequiredService<ISessionBuildService>();

			try
			{
				int inserted = await ingestion.IngestRecentPlaysAsync(userId, cancellationToken);

				// Only rebuild sessions when there's new activity — nothing new means nothing to regroup.
				if (inserted > 0)
				{
					await sessionBuilder.RebuildSessionsAsync(userId, cancellationToken);
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Ingestion failed for user {UserId}.", userId);
			}
		}
	}

	private async Task<List<Guid>> LoadConnectedUserIdsAsync(CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

		VibeStreamDbContext dbContext = scope.ServiceProvider.GetRequiredService<VibeStreamDbContext>();

		return await dbContext.SpotifyConnections
			.Select(connection => connection.UserId)
			.ToListAsync(cancellationToken);
	}
}
