using System.Diagnostics;

namespace VibeStream.Api.Middleware;

/// <summary>
/// One structured log line per request: method, path, status, and duration (plan §20). Uses message-
/// template properties (not string interpolation) so a log sink can index StatusCode/DurationMs as
/// real fields. Wrapped in try/finally so the line is written even when the request throws.
/// </summary>
public sealed class RequestLoggingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<RequestLoggingMiddleware> _logger;

	public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		long startTimestamp = Stopwatch.GetTimestamp();

		try
		{
			await _next(context);
		}
		finally
		{
			double durationMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

			_logger.LogInformation(
				"HTTP {Method} {Path} responded {StatusCode} in {DurationMs:0.0}ms",
				context.Request.Method,
				context.Request.Path.Value,
				context.Response.StatusCode,
				durationMs);
		}
	}
}
