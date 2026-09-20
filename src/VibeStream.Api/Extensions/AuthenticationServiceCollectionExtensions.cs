using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VibeStream.Api.Authentication;

namespace VibeStream.Api.Extensions;

/// <summary>
/// AUTHENTICATION and AUTHORIZATION wiring — two different questions:
///   AUTHENTICATION  "who are you?"  — validates the token, populates HttpContext.User (UseAuthentication).
///   AUTHORIZATION   "may you?"      — evaluates policies against that user            (UseAuthorization).
/// The middleware order is fixed: authentication first, or authorization has nothing to evaluate.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
	public static IServiceCollection AddJwtAuthentication(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Bind + validate the options used by the token ISSUER (the DI path). ValidateOnStart + data
		// annotations mean a too-short signing key or a missing issuer fails the app at boot.
		services.AddOptions<JwtOptions>()
			.Bind(configuration.GetSection(JwtOptions.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

		// Read the section DIRECTLY for the bearer handler: it needs the signing key while the container
		// is still being BUILT, so there's no IOptions to resolve yet. The explicit throw turns a
		// missing section into a clear message instead of a NullReferenceException at startup.
		JwtOptions jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
			?? throw new InvalidOperationException(
				$"Missing '{JwtOptions.SectionName}' configuration section. See appsettings.Development.json.");

		services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				// Turn OFF the legacy WS-Federation claim remapping that rewrites "sub" into a long URI.
				// With it off, the claims in the token are exactly the claims in User.Claims.
				options.MapInboundClaims = false;

				// No OIDC discovery here — we validate locally with a symmetric key — so metadata over
				// HTTPS is irrelevant. Leave the flag explicit for readers.
				options.RequireHttpsMetadata = false;

				options.TokenValidationParameters = new TokenValidationParameters
				{
					// Each of these guards a real attack; every `false` would be a vulnerability.
					ValidateIssuer = true,
					ValidIssuer = jwtOptions.Issuer,

					ValidateAudience = true,
					ValidAudience = jwtOptions.Audience,

					// The single most important line: without it the signature isn't checked and any
					// claim can be forged.
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

					ValidateLifetime = true,

					// Default skew is FIVE minutes (an expired token keeps working that long). 30s covers
					// genuine clock drift without meaningfully extending a token's life.
					ClockSkew = TimeSpan.FromSeconds(30),

					NameClaimType = JwtRegisteredClaimNames.Sub,
				};

				// THE cookie-auth pivot: pull the token from our HttpOnly cookie instead of the
				// Authorization header. The SPA never touches the token — the browser sends the cookie
				// automatically and JavaScript can't read it.
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						if (context.Request.Cookies.TryGetValue(AuthCookie.Name, out string? token)
							&& !string.IsNullOrEmpty(token))
						{
							context.Token = token;
						}

						return Task.CompletedTask;
					},
				};
			});

		// No roles in this app (it's single-user-per-account), so authorization is just "are you
		// authenticated?" via [Authorize]. Named policies can be added here if that changes.
		services.AddAuthorization();

		return services;
	}
}
