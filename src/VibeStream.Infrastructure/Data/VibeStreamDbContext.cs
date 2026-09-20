using Microsoft.EntityFrameworkCore;
using VibeStream.Domain.Entities;

namespace VibeStream.Infrastructure.Data;

/// <summary>
/// The EF Core context — our unit of work over PostgreSQL.
///
/// A DbContext is three things at once: a connection factory, a CHANGE TRACKER (it remembers every
/// entity it handed you and what you changed), and a transaction boundary (one SaveChangesAsync ==
/// one transaction). Because it tracks changes it is NOT thread-safe and must be short-lived — which
/// is why it's registered SCOPED (one per request) and a singleton must never hold one.
/// </summary>
public class VibeStreamDbContext : DbContext
{
	// Options are INJECTED, not built here. That inversion is what lets tests swap the real Postgres
	// connection for a test one without touching this class.
	public VibeStreamDbContext(DbContextOptions<VibeStreamDbContext> options)
			: base(options)
	{
	}

	// DbSet<T> == "the table of T" and the root of every LINQ query against it.
	// Expression-bodied `=> Set<T>()` (not `{ get; set; }`) can't be null, can't be reassigned, and
	// needs no `= null!;` to satisfy the nullable analyzer.
	public DbSet<User> Users => Set<User>();
	public DbSet<SpotifyConnection> SpotifyConnections => Set<SpotifyConnection>();
	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<ListeningEvent> ListeningEvents => Set<ListeningEvent>();
	public DbSet<ListeningSession> ListeningSessions => Set<ListeningSession>();
	public DbSet<SessionTrack> SessionTracks => Set<SessionTrack>();
	public DbSet<DailyStatistic> DailyStatistics => Set<DailyStatistic>();
	public DbSet<VibeSnapshot> VibeSnapshots => Set<VibeSnapshot>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Each entity's mapping lives in its own IEntityTypeConfiguration<T> under Data/Configurations.
		// This reflects over THIS assembly and applies them all, so OnModelCreating never grows into a
		// 500-line method — adding an entity becomes "add a config class", not "edit this file".
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(VibeStreamDbContext).Assembly);
	}
}
