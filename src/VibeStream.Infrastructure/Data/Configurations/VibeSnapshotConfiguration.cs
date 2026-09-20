using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

public class VibeSnapshotConfiguration : IEntityTypeConfiguration<VibeSnapshot>
{
	public void Configure(EntityTypeBuilder<VibeSnapshot> builder)
	{
		builder.HasKey(snapshot => snapshot.Id);

		builder.Property(snapshot => snapshot.UserId)
			.IsRequired();

		builder.Property(snapshot => snapshot.Name)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(snapshot => snapshot.CreatedAt)
			.IsRequired();

		// Reading a user's vibe history: WHERE UserId = @u ORDER BY CreatedAt DESC.
		builder.HasIndex(snapshot => new
		{
			snapshot.UserId,
			snapshot.CreatedAt
		}).IsDescending(false, true);

		// OPTIONAL relationship to ListeningSession. SessionId is nullable (Guid?) because a vibe
		// can describe a session OR stand alone as a "current vibe" with no session. When the parent
		// session is deleted, SetNull keeps the snapshot but clears the link — the vibe history is
		// worth preserving on its own, so we don't Cascade-delete it with the session.
		builder.HasOne<ListeningSession>()
			.WithMany()
			.HasForeignKey(snapshot => snapshot.SessionId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}
