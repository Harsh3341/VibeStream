using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VibeStream.Application.Spotify;

namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// The Spotify Web API data reads behind <see cref="ISpotifyClient"/>, over a typed HttpClient bound to
/// <c>api.spotify.com</c> (BaseAddress set in DI). Every call carries the caller's access token as a
/// Bearer header; obtaining/refreshing that token is <see cref="ISpotifyOAuthClient"/>'s job, not ours.
/// </summary>
public sealed class SpotifyApiClient : ISpotifyClient
{
	private readonly HttpClient _httpClient;

	public SpotifyApiClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<SpotifyUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken)
	{
		SpotifyUserResponse? payload = await GetAsync<SpotifyUserResponse>(
			"v1/me", accessToken, cancellationToken);

		if (payload is null)
		{
			throw new InvalidOperationException("Spotify /v1/me returned an empty body.");
		}

		return new SpotifyUserProfile(payload.Id, payload.DisplayName, payload.Email);
	}

	public async Task<SpotifyRecentlyPlayedResult> GetRecentlyPlayedAsync(
		string accessToken,
		CancellationToken cancellationToken)
	{
		// 50 is Spotify's per-request maximum for this endpoint.
		SpotifyRecentlyPlayedResponse? payload = await GetAsync<SpotifyRecentlyPlayedResponse>(
			"v1/me/player/recently-played?limit=50", accessToken, cancellationToken);

		if (payload?.Items is null)
		{
			return new SpotifyRecentlyPlayedResult([]);
		}

		List<SpotifyPlayHistoryItem> items = payload.Items
			.Select(item => new SpotifyPlayHistoryItem(MapTrack(item.Track), item.PlayedAt))
			.ToList();

		return new SpotifyRecentlyPlayedResult(items);
	}

	public async Task<SpotifyCurrentlyPlaying?> GetCurrentlyPlayingAsync(
		string accessToken,
		CancellationToken cancellationToken)
	{
		SpotifyCurrentlyPlayingResponse? payload = await GetAsync<SpotifyCurrentlyPlayingResponse>(
			"v1/me/player/currently-playing", accessToken, cancellationToken);

		// payload == null   -> 204, nothing playing.
		// payload.Item null  -> playing a non-track (ad/podcast); nothing for us to record.
		if (payload?.Item is null)
		{
			return null;
		}

		return new SpotifyCurrentlyPlaying(MapTrack(payload.Item), payload.IsPlaying, payload.ProgressMs);
	}

	private async Task<T?> GetAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new(HttpMethod.Get, path);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

		// 204 No Content is Spotify's "nothing to report" (used by currently-playing when idle). It's a
		// success, not an error, so map it to null rather than letting EnsureSuccessStatusCode pass an
		// empty body to the deserializer.
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			return default;
		}

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<T>(SpotifyJson.Options, cancellationToken);
	}

	/// <summary>
	/// Maps Spotify's rich track JSON down to our flat <see cref="SpotifyTrack"/>: first (primary)
	/// artist, first (largest) album image. Throws if a track has no artist — that shouldn't happen and
	/// we'd rather fail loudly than silently invent one.
	/// </summary>
	private static SpotifyTrack MapTrack(SpotifyTrackResponse track)
	{
		if (track.Artists is not [SpotifyArtistResponse primary, ..])
		{
			throw new InvalidOperationException($"Spotify track '{track.Id}' has no artists.");
		}

		string? albumImageUrl = track.Album?.Images is [SpotifyImageResponse largest, ..]
			? largest.Url
			: null;

		return new SpotifyTrack(
			Id: track.Id,
			Name: track.Name,
			DurationMs: track.DurationMs,
			AlbumName: track.Album?.Name ?? string.Empty,
			AlbumImageUrl: albumImageUrl,
			Artist: new SpotifyArtist(primary.Id, primary.Name));
	}
}
