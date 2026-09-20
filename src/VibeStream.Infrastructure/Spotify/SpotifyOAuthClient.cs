using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using VibeStream.Application.Spotify;

namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// Talks to Spotify's OAuth endpoints on <c>accounts.spotify.com</c> (the BaseAddress is set on the
/// typed HttpClient in DI). Implements the token lifecycle behind <see cref="ISpotifyOAuthClient"/> so
/// no other layer knows about form encoding, Basic auth, or the accounts host.
/// </summary>
public sealed class SpotifyOAuthClient : ISpotifyOAuthClient
{
	private readonly HttpClient _httpClient;
	private readonly SpotifyOptions _options;
	private readonly TimeProvider _timeProvider;

	public SpotifyOAuthClient(HttpClient httpClient, IOptions<SpotifyOptions> options, TimeProvider timeProvider)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_timeProvider = timeProvider;
	}

	public string BuildAuthorizationUrl(string state)
	{
		// Every value is URL-escaped: redirect_uri and the space-joined scopes contain characters
		// (":", "/", " ") that would otherwise break the query. Uri.EscapeDataString lives in System,
		// so this stays host-agnostic — no ASP.NET WebUtilities dependency in Infrastructure.
		string scopes = string.Join(' ', _options.Scopes);

		string query = string.Join('&',
		[
			$"client_id={Uri.EscapeDataString(_options.ClientId)}",
			"response_type=code",
			$"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}",
			$"scope={Uri.EscapeDataString(scopes)}",
			$"state={Uri.EscapeDataString(state)}",
		]);

		// Build the absolute URL off the client's configured BaseAddress so the accounts host is
		// defined in exactly one place (the DI registration), not duplicated here.
		return new Uri(_httpClient.BaseAddress!, $"authorize?{query}").ToString();
	}

	public Task<SpotifyTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
	{
		Dictionary<string, string> form = new()
		{
			["grant_type"] = "authorization_code",
			["code"] = code,
			["redirect_uri"] = _options.RedirectUri,
		};

		return RequestTokenAsync(form, cancellationToken);
	}

	public Task<SpotifyTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
	{
		Dictionary<string, string> form = new()
		{
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
		};

		return RequestTokenAsync(form, cancellationToken);
	}

	private async Task<SpotifyTokenResult> RequestTokenAsync(
		IDictionary<string, string> form,
		CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new(HttpMethod.Post, "api/token")
		{
			Content = new FormUrlEncodedContent(form),
		};

		// Client authentication via HTTP Basic: base64("client_id:client_secret"). Sending the secret
		// as a header (not in the body) is Spotify's recommended method for confidential clients and
		// keeps it out of any body logging.
		string credentials = Convert.ToBase64String(
			Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

		using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

		// Throws on any non-2xx (bad code, revoked refresh token, invalid client). The auth service
		// above translates this into a clean redirect-with-error; here we just refuse to return junk.
		response.EnsureSuccessStatusCode();

		SpotifyTokenResponse? payload = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(
			SpotifyJson.Options, cancellationToken);

		if (payload is null)
		{
			throw new InvalidOperationException("Spotify token endpoint returned an empty body.");
		}

		return new SpotifyTokenResult(
			AccessToken: payload.AccessToken,
			RefreshToken: payload.RefreshToken,
			ExpiresAt: _timeProvider.GetUtcNow().AddSeconds(payload.ExpiresIn),
			Scope: payload.Scope ?? string.Empty);
	}
}
