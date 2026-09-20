using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

public class ListeningSessionConfiguration : IEntityTypeConfiguration<ListeningSession>
{
	public void Configure(EntityTypeBuilder<ListeningSession> builder)
	{
		builder.HasKey(session => session.Id);

		builder.Property(session => session.UserId)
			.IsRequired();

		builder.Property(session => session.StartedAt)
			.IsRequired();

		builder.Property(session => session.EndedAt)
			.IsRequired();

		builder.Property(session => session.CreatedAt)
			.IsRequired();

		// Listing a user's sessions is WHERE UserId = @u ORDER BY StartedAt DESC.
		builder.HasIndex(session => new
		{
			session.UserId,
			session.StartedAt
		}).IsDescending(false, true);

		// The parent side of the 1-to-many. A session HAS its tracks, and deleting the session
		// deletes them — SessionTrack rows have no meaning without their session, so Cascade is
		// correct here (contrast with Restrict on reference data like Track/Artist).
		builder.HasMany(session => session.Tracks)
			.WithOne()
			.HasForeignKey(sessionTrack => sessionTrack.SessionId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
