namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// Strongly-typed Spotify OAuth configuration, bound from the "Spotify" configuration section.
///
/// CONCEPT: the Options pattern turns loose configuration keys into one validated, injectable object.
/// Nothing in the codebase reads a raw <c>IConfiguration["Spotify:ClientSecret"]</c> scattered around;
/// callers inject <c>IOptions&lt;SpotifyOptions&gt;</c> and get a typed value — or a loud failure at
/// startup if it's missing (see the ValidateOnStart wiring in the DI extension).
///
/// SECRETS: ClientId/ClientSecret are NEVER committed. In development they come from user-secrets; in
/// production from environment variables. appsettings.json holds only empty placeholders so the shape
/// of the section is documented in source without leaking a credential.
/// </summary>
public sealed class SpotifyOptions
{
	// The configuration section these values bind from. Kept as one const so the section name isn't
	// repeated as a magic string at the binding site.
	public const string SectionName = "Spotify";

	// From the Spotify developer dashboard. Semi-public, but still treated as configuration, not source.
	public string ClientId { get; set; } = string.Empty;

	// The secret half of the credential. Treated like a password: never logged, never committed.
	public string ClientSecret { get; set; } = string.Empty;

	// Where Spotify redirects the browser back to after the user authorizes. This must match a redirect
	// URI registered in the Spotify dashboard BYTE-FOR-BYTE, or Spotify rejects the callback.
	public string RedirectUri { get; set; } = string.Empty;

	// The OAuth scopes we request — the MINIMUM this project needs (plan §8): read the profile, the
	// recently-played history, and what's currently playing. No write scopes, nothing extra. These are
	// part of the app's contract rather than environment config, so they default here in code; binding
	// can still override them if a section lists its own.
	public IReadOnlyList<string> Scopes { get; set; } =
	[
		"user-read-email",
		"user-read-private",
		"user-read-recently-played",
		"user-read-currently-playing",
	];
}
