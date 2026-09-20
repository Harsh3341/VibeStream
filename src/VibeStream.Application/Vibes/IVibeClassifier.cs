namespace VibeStream.Application.Vibes;

/// <summary>
/// Maps an <see cref="AudioProfile"/> to a named vibe (plan §13). Behind an interface so it's testable
/// and replaceable — a future version could swap the rules for a model without touching callers.
/// </summary>
public interface IVibeClassifier
{
	VibeResult Classify(AudioProfile profile);
}
