using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

/// <summary>
/// EF mapping for Artist. With snake_case naming (wired in step 2c) this becomes table `artists`
/// with columns `id`, `spotify_artist_id`, `name` — no ToTable/HasColumnName needed.
/// </summary>
public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
	public void Configure(EntityTypeBuilder<Artist> builder)
	{
		builder.HasKey(artist => artist.Id);

		builder.Property(artist => artist.SpotifyArtistId)
			.IsRequired()
			.HasMaxLength(64);

		// The NATURAL KEY pattern: we look artists up by their Spotify id, and must never store the
		// same artist twice. A unique index enforces that at the database level — the same idea you'll
		// use for Track, User, and (composite) ListeningEvent.
		builder.HasIndex(artist => artist.SpotifyArtistId)
			.IsUnique();

		builder.Property(artist => artist.Name)
			.IsRequired()
			.HasMaxLength(300);
	}
}
