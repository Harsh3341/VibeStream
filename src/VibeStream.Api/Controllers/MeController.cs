using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Listening;

namespace VibeStream.Api.Controllers;

/// <summary>The signed-in user: their profile and what they're playing right now (plan §15).</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
	private readonly IListeningQueryService _listeningQueryService;
	private readonly INowPlayingService _nowPlayingService;

	public MeController(
		IListeningQueryService listeningQueryService,
		INowPlayingService nowPlayingService)
	{
		_listeningQueryService = listeningQueryService;
		_nowPlayingService = nowPlayingService;
	}

	[HttpGet]
	public async Task<ActionResult<UserProfileView>> Profile(CancellationToken cancellationToken)
	{
		UserProfileView? profile = await _listeningQueryService.GetProfileAsync(
			User.GetUserId(), cancellationToken);

		return profile is null ? NotFound() : Ok(profile);
	}

	/// <summary>Current track, or 204 No Content when nothing is playing.</summary>
	[HttpGet("now-playing")]
	public async Task<ActionResult<NowPlayingView>> NowPlaying(CancellationToken cancellationToken)
	{
		NowPlayingView? nowPlaying = await _nowPlayingService.GetAsync(User.GetUserId(), cancellationToken);

		return nowPlaying is null ? NoContent() : Ok(nowPlaying);
	}
}
