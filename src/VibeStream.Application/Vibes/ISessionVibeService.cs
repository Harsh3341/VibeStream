namespace VibeStream.Application.Vibes;

/// <summary>
/// Computes and persists a <c>VibeSnapshot</c> for each of a user's sessions that doesn't have one yet.
/// Runs after session detection so every session ends up with a deterministic vibe (plan §8 DoD).
/// </summary>
public interface ISessionVibeService
{
	Task RebuildSessionVibesAsync(Guid userId, CancellationToken cancellationToken);
}
