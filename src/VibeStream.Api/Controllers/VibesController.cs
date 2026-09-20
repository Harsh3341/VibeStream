using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Vibes;

namespace VibeStream.Api.Controllers;

/// <summary>
/// Vibe reads over the caller's own data (plan §15): the current vibe, the history, and the listening
/// DNA breakdown. [Authorize]; the user id comes from the token, never the client.
/// </summary>
[ApiController]
[Route("api/v1/vibes")]
[Authorize]
public sealed class VibesController : ControllerBase
{
	private readonly IVibeQueryService _vibeQueryService;

	public VibesController(IVibeQueryService vibeQueryService)
	{
		_vibeQueryService = vibeQueryService;
	}

	/// <summary>The user's most recent vibe, or 404 if they have none yet.</summary>
	[HttpGet("current")]
	public async Task<ActionResult<VibeView>> Current(CancellationToken cancellationToken)
	{
		VibeView? current = await _vibeQueryService.GetCurrentAsync(User.GetUserId(), cancellationToken);

		return current is null ? NotFound() : Ok(current);
	}

	[HttpGet("history")]
	public async Task<ActionResult<IReadOnlyList<VibeView>>> History(
		CancellationToken cancellationToken,
		[FromQuery] int limit = 50)
	{
		IReadOnlyList<VibeView> history = await _vibeQueryService.GetHistoryAsync(
			User.GetUserId(), Math.Clamp(limit, 1, 200), cancellationToken);

		return Ok(history);
	}

	/// <summary>Listening DNA — each vibe's share of the last <c>days</c> days (deferred from M6).</summary>
	[HttpGet("dna")]
	public async Task<ActionResult<IReadOnlyList<VibeShare>>> Dna(
		CancellationToken cancellationToken,
		[FromQuery] int days = 30)
	{
		IReadOnlyList<VibeShare> dna = await _vibeQueryService.GetListeningDnaAsync(
			User.GetUserId(), Math.Clamp(days, 1, 365), cancellationToken);

		return Ok(dna);
	}
}
