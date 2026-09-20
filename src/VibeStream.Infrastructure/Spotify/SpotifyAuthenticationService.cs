using Microsoft.EntityFrameworkCore;
using VibeStream.Application.Spotify;
using VibeStream.Domain.Entities;
using VibeStream.Infrastructure.Data;

namespace VibeStream.Infrastructure.Spotify;

/// <summary>
/// Implements the OAuth-callback orchestration against the database. Scoped (it uses the scoped
/// DbContext). Idempotent by design: logging in again updates the existing user and connection rather
/// than creating duplicates — the unique index on User.SpotifyUserId and the 1:1 SpotifyConnection
/// back it up.
/// </summary>
public sealed class SpotifyAuthenticationService : ISpotifyAuthenticationService
{
	private readonly ISpotifyOAuthClient _oauthClient;
	private readonly ISpotifyClient _apiClient;
	private readonly VibeStreamDbContext _dbContext;
	private readonly TimeProvider _timeProvider;

	public SpotifyAuthenticationService(
		ISpotifyOAuthClient oauthClient,
		ISpotifyClient apiClient,
		VibeStreamDbContext dbContext,
		TimeProvider timeProvider)
	{
		_oauthClient = oauthClient;
		_apiClient = apiClient;
		_dbContext = dbContext;
		_timeProvider = timeProvider;
	}

	public async Task<AuthenticatedUser> CompleteLoginAsync(string code, CancellationToken cancellationToken)
	{
		// 1. Trade the one-time code for tokens, then 2. use the access token to learn who this is.
		SpotifyTokenResult tokens = await _oauthClient.ExchangeCodeAsync(code, cancellationToken);
		SpotifyUserProfile profile = await _apiClient.GetCurrentUserAsync(tokens.AccessToken, cancellationToken);

		DateTimeOffset now = _timeProvider.GetUtcNow();

		// 3. Upsert the user by their Spotify id (the natural key).
		User? user = await _dbContext.Users
			.FirstOrDefaultAsync(candidate => candidate.SpotifyUserId == profile.Id, cancellationToken);

		if (user is null)
		{
			user = new User
			{
				SpotifyUserId = profile.Id,
				DisplayName = profile.DisplayName,
				CreatedAt = now,
				UpdatedAt = now,
			};

			_dbContext.Users.Add(user);
		}
		else
		{
			user.DisplayName = profile.DisplayName;
			user.UpdatedAt = now;
		}

		// 4. Upsert the connection (1:1 with the user). user.Id is available pre-save because the entity
		//    generates its own Guid v7 in the constructor — no round-trip needed to link the connection.
		SpotifyConnection? connection = await _dbContext.SpotifyConnections
			.FirstOrDefaultAsync(candidate => candidate.UserId == user.Id, cancellationToken);

		if (connection is null)
		{
			connection = new SpotifyConnection
			{
				UserId = user.Id,
				AccessToken = tokens.AccessToken,

				// The authorization_code grant always returns a refresh token; guard anyway so a
				// contract change fails loudly instead of storing an empty string.
				RefreshToken = tokens.RefreshToken
					?? throw new InvalidOperationException(
						"Spotify did not return a refresh token on code exchange."),
				ExpiresAt = tokens.ExpiresAt,
				CreatedAt = now,
				UpdatedAt = now,
			};

			_dbContext.SpotifyConnections.Add(connection);
		}
		else
		{
			connection.AccessToken = tokens.AccessToken;

			// Keep the existing refresh token when Spotify omits a new one.
			if (tokens.RefreshToken is not null)
			{
				connection.RefreshToken = tokens.RefreshToken;
			}

			connection.ExpiresAt = tokens.ExpiresAt;
			connection.UpdatedAt = now;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return new AuthenticatedUser(user.Id, user.SpotifyUserId);
	}
}
