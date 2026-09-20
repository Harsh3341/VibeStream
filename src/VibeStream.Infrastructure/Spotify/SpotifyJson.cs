using System.Text.Json;

namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// One shared JSON configuration for talking to Spotify. Their API is snake_case
/// (<c>access_token</c>, <c>expires_in</c>, <c>display_name</c>), so a single SnakeCaseLower naming
/// policy maps every response onto our PascalCase records — no per-property <c>[JsonPropertyName]</c>
/// noise, and no chance of two call sites configuring the mapping differently.
/// </summary>
internal static class SpotifyJson
{
	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};
}
