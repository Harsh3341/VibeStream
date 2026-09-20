namespace VibeStream.Infrastructure.Ingestion;

/// <summary>
/// Tunables for the background ingestion loop, bound from the "Ingestion" configuration section.
/// </summary>
public sealed class IngestionOptions
{
	public const string SectionName = "Ingestion";

	// How long the worker sleeps between polling cycles. Spotify's recently-played only updates every
	// so often and rate limits apply, so there's no value polling aggressively.
	public int PollIntervalSeconds { get; set; } = 60;

	// Refresh the access token this many seconds BEFORE it actually expires, so a token doesn't lapse
	// mid-request due to clock drift or a slow call.
	public int TokenExpiryBufferSeconds { get; set; } = 60;

	public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);

	public TimeSpan TokenExpiryBuffer => TimeSpan.FromSeconds(TokenExpiryBufferSeconds);
}
