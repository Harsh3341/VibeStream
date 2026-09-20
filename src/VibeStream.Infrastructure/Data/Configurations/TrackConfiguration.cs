using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

/// <summary>EF mapping for Track — the natural-key pattern plus its first relationship (→ Artist).</summary>
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> builder)
	{
		builder.HasKey(track => track.Id);

		builder.Property(track => track.SpotifyTrackId)
				.IsRequired()
				.HasMaxLength(64);

		builder.HasIndex(track => track.SpotifyTrackId)
				.IsUnique();

		builder.Property(track => track.Name)
				.IsRequired()
				.HasMaxLength(300);

		builder.Property(track => track.AlbumName)
				.IsRequired()
				.HasMaxLength(300);

		// Nullable column, so NO IsRequired. Image URLs run long; cap generously.
		builder.Property(track => track.AlbumImageUrl)
				.HasMaxLength(500);

		// ── Relationship: Track → Artist ────────────────────────────────────────────────────────
		// Read it as a sentence: a Track HasOne Artist, that artist WithMany tracks, joined by the
		// ArtistId foreign key.
		//
		// .WithMany() has NO argument because Artist has no Tracks collection (we skipped it earlier as
		// premature). If we later add Artist.Tracks, this becomes .WithMany(artist => artist.Tracks).
		//
		// DeleteBehavior.Restrict: refuse to delete an artist that still has tracks. The default for a
		// required relationship is Cascade — which would wipe a band's whole catalogue when you delete
		// the artist row. Restrict is the right call for reference data.
		builder.HasOne(track => track.Artist)
				.WithMany()
				.HasForeignKey(track => track.ArtistId)
				.OnDelete(DeleteBehavior.Restrict);
	}
}
