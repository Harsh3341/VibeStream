using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeStream.Api.Authentication;
using VibeStream.Application.Listening;

namespace VibeStream.Api.Controllers;

/// <summary>Listening sessions (plan §15): the list, and a single session with its tracks.</summary>
[ApiController]
[Route("api/v1/sessions")]
[Authorize]
public sealed class SessionsController : ControllerBase
{
	private readonly IListeningQueryService _listeningQueryService;

	public SessionsController(IListeningQueryService listeningQueryService)
	{
		_listeningQueryService = listeningQueryService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<SessionView>>> List(
		CancellationToken cancellationToken,
		[FromQuery] int limit = 50)
	{
		IReadOnlyList<SessionView> sessions = await _listeningQueryService.GetSessionsAsync(
			User.GetUserId(), Math.Clamp(limit, 1, 200), cancellationToken);

		return Ok(sessions);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<SessionDetailView>> Detail(Guid id, CancellationToken cancellationToken)
	{
		SessionDetailView? session = await _listeningQueryService.GetSessionAsync(
			User.GetUserId(), id, cancellationToken);

		return session is null ? NotFound() : Ok(session);
	}
}
