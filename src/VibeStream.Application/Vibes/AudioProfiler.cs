namespace VibeStream.Application.Vibes;

/// <summary>
/// Turns available metadata into a 0–1 <see cref="AudioProfile"/> with a deterministic scoring model.
/// The weights and curves below are heuristics — honest proxies, not measured audio features — chosen
/// so the output is explainable and stable. Every score is clamped to [0, 1]. Stateless singleton.
/// </summary>
public sealed class AudioProfiler : IAudioProfiler
{
	// Release-year is normalized against this window; a track from OldestYear scores 0, NewestYear 1.
	private const int OldestYear = 1970;
	private const int NewestYear = 2025;

	public AudioProfile Profile(TrackSignals signals)
	{
		double popularity = Math.Clamp(signals.Popularity / 100.0, 0.0, 1.0);
		double durationEnergy = DurationEnergy(signals.DurationMs);
		double timeEnergy = TimeEnergy(signals.PlayedHour);

		// Energy: shorter tracks and daytime/afternoon plays skew higher.
		double energy = Clamp01(0.55 * durationEnergy + 0.45 * timeEnergy);

		// Valence (positivity): mainstream popularity, newer releases, and daytime plays skew happier.
		double valence = Clamp01(
			0.45 * popularity
			+ 0.25 * Recency(signals.ReleaseYear)
			+ 0.30 * TimeValence(signals.PlayedHour));

		// Danceability: popularity blended with the time-of-day energy curve.
		double danceability = Clamp01(0.5 * popularity + 0.5 * timeEnergy);

		return new AudioProfile(energy, valence, danceability, Confidence(signals));
	}

	// Shorter → more energetic: ~2 min ≈ 1.0, ~6 min ≈ 0.0.
	private static double DurationEnergy(int durationMs)
	{
		double minutes = durationMs / 60000.0;

		return Clamp01((6.0 - minutes) / 4.0);
	}

	// Peaks mid-afternoon (15:00), troughs overnight; wraps around midnight.
	private static double TimeEnergy(int hour)
	{
		return Clamp01(1.0 - WrappedDistance(hour, 15) / 12.0);
	}

	// Peaks early afternoon (13:00); late night is lowest.
	private static double TimeValence(int hour)
	{
		return Clamp01(1.0 - WrappedDistance(hour, 13) / 13.0);
	}

	private static double Recency(int? releaseYear)
	{
		if (releaseYear is null)
		{
			// Unknown → neutral, so a missing year doesn't skew valence either way.
			return 0.5;
		}

		return Clamp01((releaseYear.Value - OldestYear) / (double)(NewestYear - OldestYear));
	}

	// More signals present → more confident. Popularity is always sent (0 default), so a real >0 value
	// and a known release year each add certainty.
	private static double Confidence(TrackSignals signals)
	{
		double confidence = 0.4;

		if (signals.Popularity > 0)
		{
			confidence += 0.3;
		}

		if (signals.ReleaseYear is not null)
		{
			confidence += 0.3;
		}

		return confidence;
	}

	// Shortest distance between two hours on a 24-hour clock (so 23:00 and 01:00 are 2 apart, not 22).
	private static double WrappedDistance(int hour, int target)
	{
		double distance = Math.Abs(hour - target);

		return Math.Min(distance, 24 - distance);
	}

	private static double Clamp01(double value)
	{
		return Math.Clamp(value, 0.0, 1.0);
	}
}
