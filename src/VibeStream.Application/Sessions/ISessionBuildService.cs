namespace VibeStream.Application.Sessions;

/// <summary>
/// Turns a user's stored listening events into persisted <c>ListeningSession</c> + <c>SessionTrack</c>
/// rows via the <see cref="ISessionDetector"/>. Called after ingestion so newly imported plays roll up
/// into sessions.
/// </summary>
public interface ISessionBuildService
{
	/// <summary>
	/// (Re)builds the affected sessions for <paramref name="userId"/> and returns how many session rows
	/// were written. Safe to call repeatedly.
	/// </summary>
	Task<int> RebuildSessionsAsync(Guid userId, CancellationToken cancellationToken);
}
