namespace VibeStream.Application.Vibes;

/// <summary>
/// A deterministic rules engine (NOT an external AI — plan §13) that reads the energy/valence plane and
/// names the vibe. Danceability breaks the mid-energy tie between an upbeat "Feel Good" and neutral
/// "Focus". The scores are echoed back so the UI can show the reasoning; confidence passes through from
/// the profile. Stateless singleton.
/// </summary>
public sealed class VibeClassifier : IVibeClassifier
{
	public VibeResult Classify(AudioProfile profile)
	{
		string name = Label(profile.Energy, profile.Valence, profile.Danceability);

		return new VibeResult(name, profile.Energy, profile.Valence, profile.Confidence);
	}

	private static string Label(double energy, double valence, double danceability)
	{
		bool highEnergy = energy >= 0.6;
		bool lowEnergy = energy < 0.4;
		bool positive = valence >= 0.55;
		bool negative = valence < 0.4;

		// High energy splits on mood: upbeat vs intense.
		if (highEnergy && positive)
		{
			return "High Energy";
		}

		if (highEnergy && negative)
		{
			return "Dark Energy";
		}

		// Low energy splits on mood: mellow-positive vs late-night-down.
		if (lowEnergy && positive)
		{
			return "Chill";
		}

		if (lowEnergy && negative)
		{
			return "Late Night";
		}

		// Mid energy: a danceable, positive track is "Feel Good"; otherwise it's heads-down "Focus".
		if (positive && danceability >= 0.6)
		{
			return "Feel Good";
		}

		return "Focus";
	}
}
