namespace VibeStream.Infrastructure.Analytics;

/// <summary>
/// The analytics cache key scheme, in one place so the reader (that builds keys) and the invalidator
/// (that bumps the version) can't drift apart.
///
/// VERSIONED KEYS: every entry embeds a per-user version number. Invalidation just increments that one
/// counter (O(1)); all previously-cached keys become unreachable and expire on their own TTL. This
/// avoids scanning/deleting many keys (SCAN/KEYS) on every ingestion — a clean way to invalidate a
/// whole family of entries at once.
/// </summary>
internal static class AnalyticsCacheKeys
{
	public static string Version(Guid userId)
	{
		return $"analytics:{userId}:version";
	}

	public static string Today(Guid userId, long version)
	{
		return $"analytics:{userId}:{version}:today";
	}

	public static string Weekly(Guid userId, long version)
	{
		return $"analytics:{userId}:{version}:weekly";
	}

	public static string TopArtists(Guid userId, long version, int days, int limit)
	{
		return $"analytics:{userId}:{version}:top-artists:{days}:{limit}";
	}

	public static string TopTracks(Guid userId, long version, int days, int limit)
	{
		return $"analytics:{userId}:{version}:top-tracks:{days}:{limit}";
	}
}
