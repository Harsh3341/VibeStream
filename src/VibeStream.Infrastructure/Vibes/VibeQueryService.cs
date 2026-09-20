using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Vibes;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Vibes;

/// <summary>Reads vibes from stored snapshots. Scoped: uses the DbContext.</summary>
public sealed class VibeQueryService : IVibeQueryService
{
	private readonly VibeStreamDbContext _dbContext;
	private readonly TimeProvider _timeProvider;

	public VibeQueryService(VibeStreamDbContext dbContext, TimeProvider timeProvider)
	{
		_dbContext = dbContext;
		_timeProvider = timeProvider;
	}

	public Task<VibeView?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
	{
		// Projection is inlined (not a shared method) because EF must translate it to SQL — it can't
		// call an arbitrary C# method inside a Select.
		return _dbContext.VibeSnapshots
			.Where(snapshot => snapshot.UserId == userId)
			.OrderByDescending(snapshot => snapshot.CreatedAt)
			.Select(snapshot => new VibeView(
				snapshot.Id,
				snapshot.SessionId,
				snapshot.Name,
				snapshot.Energy,
				snapshot.Valence,
				snapshot.Danceability,
				snapshot.Confidence,
				snapshot.CreatedAt))
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<VibeView>> GetHistoryAsync(
		Guid userId,
		int limit,
		CancellationToken cancellationToken)
	{
		return await _dbContext.VibeSnapshots
			.Where(snapshot => snapshot.UserId == userId)
			.OrderByDescending(snapshot => snapshot.CreatedAt)
			.Take(limit)
			.Select(snapshot => new VibeView(
				snapshot.Id,
				snapshot.SessionId,
				snapshot.Name,
				snapshot.Energy,
				snapshot.Valence,
				snapshot.Danceability,
				snapshot.Confidence,
				snapshot.CreatedAt))
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<VibeShare>> GetListeningDnaAsync(
		Guid userId,
		int days,
		CancellationToken cancellationToken)
	{
		DateTimeOffset since = _timeProvider.GetUtcNow().AddDays(-Math.Max(days, 1));

		List<string> names = await _dbContext.VibeSnapshots
			.Where(snapshot => snapshot.UserId == userId && snapshot.CreatedAt >= since)
			.Select(snapshot => snapshot.Name)
			.ToListAsync(cancellationToken);

		if (names.Count == 0)
		{
			return [];
		}

		return names
			.GroupBy(name => name)
			.Select(group => new VibeShare(
				group.Key,
				group.Count(),
				Math.Round(100.0 * group.Count() / names.Count, 1)))
			.OrderByDescending(share => share.Count)
			.ThenBy(share => share.Name)
			.ToList();
	}
}
