using VibeStream.Application.Vibes;

namespace VibeStream.UnitTests.Vibes;

/// <summary>
/// The profiler maps metadata to scores heuristically, so tests assert PROPERTIES (ranges, ordering,
/// determinism) rather than brittle exact numbers.
/// </summary>
public class AudioProfilerTests
{
	private readonly AudioProfiler _profiler = new();

	[Fact]
	public void Profile_KeepsEveryScoreInUnitRange()
	{
		AudioProfile profile = _profiler.Profile(new TrackSignals(
			Popularity: 73,
			ReleaseYear: 2019,
			DurationMs: 210_000,
			PlayedHour: 22));

		Assert.InRange(profile.Energy, 0.0, 1.0);
		Assert.InRange(profile.Valence, 0.0, 1.0);
		Assert.InRange(profile.Danceability, 0.0, 1.0);
		Assert.InRange(profile.Confidence, 0.0, 1.0);
	}

	[Fact]
	public void Profile_ShorterTrackHasMoreEnergyThanLonger()
	{
		TrackSignals shortTrack = new(Popularity: 50, ReleaseYear: 2020, DurationMs: 150_000, PlayedHour: 15);
		TrackSignals longTrack = shortTrack with { DurationMs = 330_000 };

		Assert.True(_profiler.Profile(shortTrack).Energy > _profiler.Profile(longTrack).Energy);
	}

	[Fact]
	public void Profile_ConfidenceIsHighestWithAllSignalsAndLowestWithNone()
	{
		AudioProfile rich = _profiler.Profile(new TrackSignals(60, 2021, 200_000, 12));
		AudioProfile sparse = _profiler.Profile(new TrackSignals(0, null, 200_000, 12));

		Assert.Equal(1.0, rich.Confidence);
		Assert.Equal(0.4, sparse.Confidence);
	}

	[Fact]
	public void Profile_IsDeterministic()
	{
		TrackSignals signals = new(Popularity: 40, ReleaseYear: 2005, DurationMs: 240_000, PlayedHour: 9);

		Assert.Equal(_profiler.Profile(signals), _profiler.Profile(signals));
	}
}
