using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

public class SessionTrackConfiguration : IEntityTypeConfiguration<SessionTrack>
{
	public void Configure(EntityTypeBuilder<SessionTrack> builder)
	{
		// COMPOSITE primary key — this entity has no Id of its own. Its identity IS
		// "which position in which session". EF orders the key columns as listed here.
		builder.HasKey(sessionTrack => new
		{
			sessionTrack.SessionId,
			sessionTrack.Position
		});

		builder.Property(sessionTrack => sessionTrack.TrackId)
			.IsRequired();

		builder.Property(sessionTrack => sessionTrack.PlayedAt)
			.IsRequired();

		// FK + navigation to Track. Restrict: you can't delete a Track that a session references —
		// the session's history stays intact. (The session→sessiontrack Cascade lives on the
		// parent config; this side only owns the track relationship.)
		builder.HasOne(sessionTrack => sessionTrack.Track)
			.WithMany()
			.HasForeignKey(sessionTrack => sessionTrack.TrackId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
