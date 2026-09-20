using System.Net;
using Microsoft.Extensions.Logging;

namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// Resilience for Spotify's data API (plan §18): retry on 429 (honouring Retry-After), 5xx, and network
/// failures, with bounded exponential backoff and a hard attempt cap — never retry forever. Applied ONLY
/// to the data client, whose calls are idempotent GETs; each attempt clones the request (GETs have no
/// body, only the Bearer header) so re-sending is safe. Token POSTs aren't retried here — a failed token
/// exchange is handled at the application level.
/// </summary>
public sealed class SpotifyResilienceHandler : DelegatingHandler
{
	private const int MaxRetries = 3;
	private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

	private readonly ILogger<SpotifyResilienceHandler> _logger;

	public SpotifyResilienceHandler(ILogger<SpotifyResilienceHandler> logger)
	{
		_logger = logger;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		for (int attempt = 0; ; attempt++)
		{
			HttpResponseMessage? response = null;

			try
			{
				response = await base.SendAsync(Clone(request), cancellationToken);
			}
			catch (HttpRequestException exception) when (attempt < MaxRetries)
			{
				// Network-level failure — back off and try again.
				_logger.LogWarning(exception, "Spotify request failed (attempt {Attempt}); retrying.", attempt + 1);
				await Task.Delay(Backoff(attempt), cancellationToken);
				continue;
			}

			if (attempt < MaxRetries && TryGetRetryDelay(response, attempt, out TimeSpan delay))
			{
				_logger.LogWarning(
					"Spotify responded {StatusCode} (attempt {Attempt}); retrying in {DelayMs}ms.",
					(int)response.StatusCode,
					attempt + 1,
					delay.TotalMilliseconds);

				response.Dispose();
				await Task.Delay(delay, cancellationToken);
				continue;
			}

			return response;
		}
	}

	private static bool TryGetRetryDelay(HttpResponseMessage response, int attempt, out TimeSpan delay)
	{
		if (response.StatusCode == HttpStatusCode.TooManyRequests)
		{
			// Respect Retry-After when Spotify sends it; otherwise fall back to backoff.
			TimeSpan retryAfter = response.Headers.RetryAfter?.Delta ?? Backoff(attempt);
			delay = retryAfter > MaxDelay ? MaxDelay : retryAfter;
			return true;
		}

		if ((int)response.StatusCode >= 500)
		{
			delay = Backoff(attempt);
			return true;
		}

		delay = TimeSpan.Zero;
		return false;
	}

	// 1s, 2s, 4s… capped. Bounded and finite (plan §18).
	private static TimeSpan Backoff(int attempt)
	{
		double seconds = Math.Pow(2, attempt);
		TimeSpan delay = TimeSpan.FromSeconds(seconds);

		return delay > MaxDelay ? MaxDelay : delay;
	}

	// GET requests carry only headers (the per-request Bearer token), so a header copy is a full clone.
	private static HttpRequestMessage Clone(HttpRequestMessage request)
	{
		HttpRequestMessage clone = new(request.Method, request.RequestUri);

		foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		return clone;
	}
}
