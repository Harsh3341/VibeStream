namespace VibeStream.Application.Vibes;

/// <summary>
/// The REAL, available signals we derive a vibe from. Spotify's audio-features endpoint is deprecated
/// for new apps, so instead of energy/tempo/acousticness we use what recently-played actually gives us:
/// popularity, release year, track length, and the hour it was played. This record is the honest input
/// to the engine — decision #1 of the project made concrete.
/// </summary>
public sealed record TrackSignals(
	int Popularity,
	int? ReleaseYear,
	int DurationMs,

	// Hour of day (0–23, UTC) the track was played — the time-of-day flavour.
	int PlayedHour);

/// <summary>
/// A normalized 0–1 feature vector DERIVED from <see cref="TrackSignals"/> — our stand-in for Spotify's
/// missing audio features. It's what the classifier consumes, keeping "derive features" and "label them"
/// as two separable, testable steps.
/// </summary>
public sealed record AudioProfile(double Energy, double Valence, double Danceability, double Confidence)
{
	/// <summary>Component-wise mean — used to roll a session's per-track profiles into one.</summary>
	public static AudioProfile Average(IReadOnlyCollection<AudioProfile> profiles)
	{
		if (profiles.Count == 0)
		{
			return new AudioProfile(0, 0, 0, 0);
		}

		return new AudioProfile(
			profiles.Average(profile => profile.Energy),
			profiles.Average(profile => profile.Valence),
			profiles.Average(profile => profile.Danceability),
			profiles.Average(profile => profile.Confidence));
	}
}

/// <summary>
/// The classifier's output (plan §13): a human label plus the scores behind it and how much to trust it.
/// </summary>
public sealed record VibeResult(string Name, double EnergyScore, double MoodScore, double Confidence);
