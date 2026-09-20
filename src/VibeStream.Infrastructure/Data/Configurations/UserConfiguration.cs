using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.HasKey(user => user.Id);

		// Spotify's user id is how we find this row on every login ("do we already know this user?").
		// Bounded length keeps it index-friendly; Spotify ids are short, 64 is generous headroom.
		builder.Property(user => user.SpotifyUserId)
			.IsRequired()
			.HasMaxLength(64);

		// UNIQUE: one VibeStream row per Spotify account. This is also the lookup index for login —
		// WHERE SpotifyUserId = @id hits it directly.
		builder.HasIndex(user => user.SpotifyUserId)
			.IsUnique();

		// Nullable in the entity (Spotify may omit it), so no IsRequired here — just a length cap.
		builder.Property(user => user.DisplayName)
			.HasMaxLength(300);

		builder.Property(user => user.CreatedAt)
			.IsRequired();

		builder.Property(user => user.UpdatedAt)
			.IsRequired();
	}
}
