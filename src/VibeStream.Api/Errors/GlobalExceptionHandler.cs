using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VibeStream.Api.Errors;

/// <summary>
/// Turns any unhandled exception into an RFC 9457 ProblemDetails response. The exception is LOGGED with
/// full detail server-side but never serialized to the client — a stack trace or message in the body
/// can leak internals (connection strings, file paths). Registered via AddExceptionHandler +
/// UseExceptionHandler.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
	private readonly IProblemDetailsService _problemDetailsService;
	private readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(
		IProblemDetailsService problemDetailsService,
		ILogger<GlobalExceptionHandler> logger)
	{
		_problemDetailsService = problemDetailsService;
		_logger = logger;
	}

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		_logger.LogError(
			exception,
			"Unhandled exception for {Method} {Path}",
			httpContext.Request.Method,
			httpContext.Request.Path);

		httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

		return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
		{
			HttpContext = httpContext,
			ProblemDetails = new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "An unexpected error occurred.",
				Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
			},
		});
	}
}
