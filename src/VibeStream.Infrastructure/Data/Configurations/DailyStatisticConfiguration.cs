using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data.Configurations;

public class DailyStatisticConfiguration : IEntityTypeConfiguration<DailyStatistic>
{
	public void Configure(EntityTypeBuilder<DailyStatistic> builder)
	{
		builder.HasKey(statistic => statistic.Id);

		builder.Property(statistic => statistic.UserId)
			.IsRequired();

		builder.Property(statistic => statistic.Date)
			.IsRequired();

		// UNIQUE (UserId, Date): exactly one rollup row per user per day. This is what lets the
		// analytics pipeline UPSERT — recompute a day and either insert a fresh row or overwrite
		// the existing one, with the database guaranteeing there's never two rows for the same day.
		builder.HasIndex(statistic => new
		{
			statistic.UserId,
			statistic.Date
		}).IsUnique();
	}
}
