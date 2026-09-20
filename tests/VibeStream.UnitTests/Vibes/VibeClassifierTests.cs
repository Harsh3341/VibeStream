using VibeStream.Application.Vibes;

namespace VibeStream.UnitTests.Vibes;

/// <summary>
/// The classifier is pure rules, so every quadrant of the energy/valence plane has one expected label.
/// </summary>
public class VibeClassifierTests
{
	private readonly VibeClassifier _classifier = new();

	[Theory]
	[InlineData(0.80, 0.70, 0.50, "High Energy")]   // high energy, positive
	[InlineData(0.80, 0.20, 0.50, "Dark Energy")]   // high energy, negative
	[InlineData(0.20, 0.70, 0.50, "Chill")]         // low energy, positive
	[InlineData(0.20, 0.20, 0.50, "Late Night")]    // low energy, negative
	[InlineData(0.50, 0.70, 0.80, "Feel Good")]     // mid energy, positive, danceable
	[InlineData(0.50, 0.45, 0.30, "Focus")]         // mid energy, neutral, not danceable
	public void Classify_NamesTheVibeForEachQuadrant(
		double energy,
		double valence,
		double danceability,
		string expected)
	{
		VibeResult result = _classifier.Classify(new AudioProfile(energy, valence, danceability, 0.9));

		Assert.Equal(expected, result.Name);
	}

	[Fact]
	public void Classify_EchoesScoresAndConfidence()
	{
		AudioProfile profile = new(Energy: 0.72, Valence: 0.61, Danceability: 0.5, Confidence: 0.83);

		VibeResult result = _classifier.Classify(profile);

		Assert.Equal(profile.Energy, result.EnergyScore);
		Assert.Equal(profile.Valence, result.MoodScore);
		Assert.Equal(profile.Confidence, result.Confidence);
	}
}
