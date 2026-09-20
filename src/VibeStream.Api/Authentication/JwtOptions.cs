using System.ComponentModel.DataAnnotations;

namespace VibeStream.Api.Authentication;

/// <summary>
/// Settings for the JWT this API issues (after the Spotify callback) and validates (on every
/// authenticated request). JWT lives entirely in the API layer: only the HTTP surface authenticates
/// callers — the Worker never does — so this concern doesn't belong in Infrastructure.
/// </summary>
public sealed class JwtOptions
{
	public const string SectionName = "Jwt";

	[Required(AllowEmptyStrings = false)]
	public string Issuer { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string Audience { get; set; } = string.Empty;

	/// <summary>
	/// Symmetric signing key.
	///
	/// SECURITY: HMAC-SHA256 needs a key of at least 256 bits, so the minimum is 32 characters — a
	/// shorter key makes the token forgeable and the handler throws at runtime. The dev value lives in
	/// appsettings.Development.json ONLY; for anything real, keep it out of the repo with:
	///
	///     dotnet user-secrets set "Jwt:SigningKey" "&lt;a long random value&gt;"
	/// </summary>
	[Required(AllowEmptyStrings = false)]
	[MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters for HMAC-SHA256.")]
	public string SigningKey { get; set; } = string.Empty;

	// How long an issued token stays valid. Bounded so a typo can't mint a decade-long token.
	[Range(1, 1440)]
	public int ExpiryMinutes { get; set; } = 60;
}
