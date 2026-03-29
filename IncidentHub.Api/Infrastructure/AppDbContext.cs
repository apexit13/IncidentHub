using IncidentHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentTimeline> IncidentTimelines => Set<IncidentTimeline>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Incident ──────────────────────────────────────────────────
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // Generate the Guid in the handler, not the DB

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.AssignedTo)
                .HasMaxLength(500); // Auth0 sub claim or display name

            // Store enums as int — readable in SQL, cheap to index
            entity.Property(e => e.Severity)
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .HasConversion<int>();

            // DateTimeOffset serialises timezone-correctly to datetimeoffset(7)
            // in SQL Server — do not override to datetime2, it loses offset info
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Index used by the default dashboard query (open incidents, sorted by severity)
            entity.HasIndex(e => new { e.Status, e.Severity })
                .HasDatabaseName("IX_Incidents_Status_Severity");

            // Relationship: one Incident has many IncidentTimeline entries
            entity.HasMany(e => e.Timeline)
                .WithOne(t => t.Incident)
                .HasForeignKey(t => t.IncidentId)
                .OnDelete(DeleteBehavior.Cascade); // deleting an incident removes its timeline
        });

        // ── IncidentTimeline ──────────────────────────────────────────
        modelBuilder.Entity<IncidentTimeline>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.ChangedBy)
                .HasMaxLength(500);

            entity.Property(e => e.NewStatus)
                .HasConversion<int?>(); // nullable int — null for the initial "raised" entry

            entity.Property(e => e.Timestamp)
                .IsRequired();

            // Index used when rendering the timeline panel for a specific incident
            entity.HasIndex(e => new { e.IncidentId, e.Timestamp })
                .HasDatabaseName("IX_IncidentTimelines_IncidentId_Timestamp");
        });
    }
}
