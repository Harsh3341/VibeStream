namespace VibeStream.Application.Analytics;

/// <summary>
/// Invalidates a user's cached analytics after their data changes (called by the worker once new plays
/// are ingested). Kept as its own abstraction so the ingestion side depends on "invalidate this user"
/// without knowing the cache key scheme.
/// </summary>
public interface IAnalyticsCacheInvalidator
{
	Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);
}
