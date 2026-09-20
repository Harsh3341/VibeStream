namespace VibeStream.Application.Spotify;

/// <summary>
/// What the user is playing right now. The client returns <c>null</c> (not this with IsPlaying=false)
/// when nothing is playing — Spotify answers 204 No Content in that case, which is a distinct state
/// from "paused mid-track", so the absence is modelled by a null result rather than a flag.
/// </summary>
public sealed record SpotifyCurrentlyPlaying(
	SpotifyTrack Track,

	// True while actively playing, false when paused on a track.
	bool IsPlaying,

	// How far into the track, in milliseconds.
	int ProgressMs);
