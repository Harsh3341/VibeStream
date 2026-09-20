namespace VibeStream.Application.Vibes;

/// <summary>
/// Derives an <see cref="AudioProfile"/> from the real signals we have. PURE — no I/O — so the mapping
/// from metadata to scores is fully unit-testable.
/// </summary>
public interface IAudioProfiler
{
	AudioProfile Profile(TrackSignals signals);
}
