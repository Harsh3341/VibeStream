using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Listening;

namespace VibeStream.Api.Controllers;

/// <summary>Listening history (plan §15). [Authorize]; scoped to the caller.</summary>
[ApiController]
[Route("api/v1/listening")]
[Authorize]
public sealed class ListeningController : ControllerBase
{
	private readonly IListeningQueryService _listeningQueryService;

	public ListeningController(IListeningQueryService listeningQueryService)
	{
		_listeningQueryService = listeningQueryService;
	}

	[HttpGet("recent")]
	public async Task<ActionResult<IReadOnlyList<RecentPlayView>>> Recent(
		CancellationToken cancellationToken,
		[FromQuery] int limit = 50)
	{
		IReadOnlyList<RecentPlayView> recent = await _listeningQueryService.GetRecentPlaysAsync(
			User.GetUserId(), Math.Clamp(limit, 1, 200), cancellationToken);

		return Ok(recent);
	}
}
