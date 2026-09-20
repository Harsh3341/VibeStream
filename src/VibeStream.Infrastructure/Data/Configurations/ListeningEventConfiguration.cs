using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using VibeStream.Domain.Entities;

  namespace VibeStream.Infrastructure.Data.Configurations;

  public class ListeningEventConfiguration : IEntityTypeConfiguration<ListeningEvent>
  {
      public void Configure(EntityTypeBuilder<ListeningEvent> builder)
      {
          builder.HasKey(listeningEvent => listeningEvent.Id);

          builder.Property(listeningEvent => listeningEvent.UserId)
              .IsRequired();

          builder.Property(listeningEvent => listeningEvent.TrackId)
              .IsRequired();

          builder.Property(listeningEvent => listeningEvent.PlayedAt)
              .IsRequired();

          builder.Property(listeningEvent => listeningEvent.CreatedAt)
              .IsRequired();

          // THE idempotency guarantee. Spotify's recently-played endpoint overlaps between polls:
          // the same play comes back on the next fetch. This UNIQUE index makes the database itself
          // reject the duplicate — the worker just catches the unique-violation and moves on, instead
          // of us trying to "check if it exists" first (which races under concurrent polls).
          // The natural key of a play is (who, what, when).
          builder.HasIndex(listeningEvent => new
          {
              listeningEvent.UserId,
              listeningEvent.TrackId,
              listeningEvent.PlayedAt
          }).IsUnique();

          // History-read index. Every "show me my recent plays" query is
          // WHERE UserId = @u ORDER BY PlayedAt DESC. This index serves both the filter and the sort,
          // so Postgres reads it backwards instead of sorting the whole result set.
          builder.HasIndex(listeningEvent => new
          {
              listeningEvent.UserId,
              listeningEvent.PlayedAt
          }).IsDescending(false, true);

          // FK + navigation to Track. Restrict, not Cascade: deleting a Track that people have
          // listened to should be blocked — the play history is the valuable data, the Track is
          // reference data that serves it.
          builder.HasOne(listeningEvent => listeningEvent.Track)
              .WithMany()
              .HasForeignKey(listeningEvent => listeningEvent.TrackId)
              .OnDelete(DeleteBehavior.Restrict);
      }
  }
