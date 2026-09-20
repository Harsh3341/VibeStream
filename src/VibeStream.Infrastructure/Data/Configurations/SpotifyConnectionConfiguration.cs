using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

/// <summary>
/// EF mapping for SpotifyConnection — a one-to-one with User (unique FK), holding the OAuth tokens.
/// </summary>
public class SpotifyConnectionConfiguration : IEntityTypeConfiguration<SpotifyConnection>
{
	public void Configure(EntityTypeBuilder<SpotifyConnection> builder)
	{
		builder.HasKey(connection => connection.Id);

		// Tokens are opaque strings; Spotify's are long, so cap generously rather than by exact size.
		builder.Property(connection => connection.AccessToken)
				.IsRequired()
				.HasMaxLength(2000);

		builder.Property(connection => connection.RefreshToken)
				.IsRequired()
				.HasMaxLength(2000);

		builder.Property(connection => connection.ExpiresAt)
				.IsRequired();

		builder.Property(connection => connection.CreatedAt)
				.IsRequired();

		builder.Property(connection => connection.UpdatedAt)
				.IsRequired();

		// ── One-to-one: SpotifyConnection → User ─────────────────────────────────────────────────
		// HasOne(User) WithOne() — WithOne (not WithMany) is what makes this 1:1. The FK lives on this
		// (dependent) side.
		//
		// DeleteBehavior.Cascade: a connection is meaningless without its user, so deleting the user
		// removes it. Contrast Track→Artist (Restrict) — a connection is a CHILD, an artist is
		// reference data.
		builder.HasOne(connection => connection.User)
				.WithOne()
				.HasForeignKey<SpotifyConnection>(connection => connection.UserId)
				.OnDelete(DeleteBehavior.Cascade);

		// The 1:1 GUARANTEE: a unique index on the FK. Without this, WithOne()/WithMany() only differ
		// in the object model — the database would still happily store two connections for one user.
		// This index is what actually enforces "one connection per user".
		builder.HasIndex(connection => connection.UserId)
				.IsUnique();
	}
}
