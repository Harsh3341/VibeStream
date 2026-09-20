using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Analytics;

namespace VibeStream.Api.Controllers;

/// <summary>
/// Read-only analytics over the caller's own data (plan §14/§15). [Authorize] on the controller means
/// every action requires a valid auth cookie; the user id comes from the token, never from the client,
/// so one user can't request another's stats.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
	private readonly IAnalyticsService _analyticsService;

	public AnalyticsController(IAnalyticsService analyticsService)
	{
		_analyticsService = analyticsService;
	}

	[HttpGet("today")]
	public async Task<ActionResult<TodaySummary>> Today(CancellationToken cancellationToken)
	{
		return Ok(await _analyticsService.GetTodayAsync(User.GetUserId(), cancellationToken));
	}

	[HttpGet("weekly")]
	public async Task<ActionResult<WeeklySummary>> Weekly(CancellationToken cancellationToken)
	{
		return Ok(await _analyticsService.GetWeeklyAsync(User.GetUserId(), cancellationToken));
	}

	[HttpGet("top-artists")]
	public async Task<ActionResult<IReadOnlyList<TopArtist>>> TopArtists(
		CancellationToken cancellationToken,
		[FromQuery] int days = 7,
		[FromQuery] int limit = 10)
	{
		IReadOnlyList<TopArtist> artists = await _analyticsService.GetTopArtistsAsync(
			User.GetUserId(), ClampDays(days), ClampLimit(limit), cancellationToken);

		return Ok(artists);
	}

	[HttpGet("top-tracks")]
	public async Task<ActionResult<IReadOnlyList<TopTrack>>> TopTracks(
		CancellationToken cancellationToken,
		[FromQuery] int days = 7,
		[FromQuery] int limit = 10)
	{
		IReadOnlyList<TopTrack> tracks = await _analyticsService.GetTopTracksAsync(
			User.GetUserId(), ClampDays(days), ClampLimit(limit), cancellationToken);

		return Ok(tracks);
	}

	// Clamp client-supplied ranges so a caller can't ask for a decade of data or an unbounded list.
	private static int ClampDays(int days) => Math.Clamp(days, 1, 365);

	private static int ClampLimit(int limit) => Math.Clamp(limit, 1, 50);
}
