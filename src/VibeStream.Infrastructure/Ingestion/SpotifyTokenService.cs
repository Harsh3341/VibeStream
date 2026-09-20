using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VibeStream.Application.Ingestion;
using VibeStream.Application.Spotify;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Ingestion;

/// <summary>
/// Returns a valid access token for a user, transparently refreshing the stored one when it's within
/// the expiry buffer. The refreshed token is persisted so the next call (and the next worker cycle)
/// starts from the new value. Scoped — it uses the scoped DbContext.
/// </summary>
public sealed class SpotifyTokenService : ISpotifyTokenService
{
	private readonly VibeStreamDbContext _dbContext;
	private readonly ISpotifyOAuthClient _oauthClient;
	private readonly TimeProvider _timeProvider;
	private readonly IngestionOptions _options;

	public SpotifyTokenService(
		VibeStreamDbContext dbContext,
		ISpotifyOAuthClient oauthClient,
		TimeProvider timeProvider,
		IOptions<IngestionOptions> options)
	{
		_dbContext = dbContext;
		_oauthClient = oauthClient;
		_timeProvider = timeProvider;
		_options = options.Value;
	}

	public async Task<string> GetValidAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
	{
		SpotifyConnection connection = await _dbContext.SpotifyConnections
			.FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken)
			?? throw new InvalidOperationException($"No Spotify connection for user {userId}.");

		DateTimeOffset now = _timeProvider.GetUtcNow();

		// Still comfortably valid — use it as-is. The buffer means we refresh a little early rather than
		// risk a token lapsing between this check and the API call that uses it.
		if (connection.ExpiresAt > now.Add(_options.TokenExpiryBuffer))
		{
			return connection.AccessToken;
		}

		SpotifyTokenResult refreshed = await _oauthClient.RefreshAccessTokenAsync(
			connection.RefreshToken, cancellationToken);

		connection.AccessToken = refreshed.AccessToken;

		// Spotify usually omits the refresh token on a refresh response — keep the existing one then.
		if (refreshed.RefreshToken is not null)
		{
			connection.RefreshToken = refreshed.RefreshToken;
		}

		connection.ExpiresAt = refreshed.ExpiresAt;
		connection.UpdatedAt = now;

		await _dbContext.SaveChangesAsync(cancellationToken);

		return connection.AccessToken;
	}
}
