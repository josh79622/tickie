using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Entities;

namespace Tickie.Manager.Data;

public class TickieDbContext : DbContext
{
    public TickieDbContext(DbContextOptions<TickieDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.FolderPath)
            .IsUnique();

        modelBuilder.Entity<Phase>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(ph => ph.ProjectId);

        modelBuilder.Entity<Phase>()
            .HasIndex(ph => new { ph.ProjectId, ph.Order })
            .IsUnique();

        modelBuilder.Entity<Phase>()
            .HasIndex(ph => new { ph.ProjectId, ph.Name })
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Ticket>()
            .HasOne<Phase>()
            .WithMany()
            .HasForeignKey(t => t.PhaseId);

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<UserSettings>()
            .Property(s => s.Id)
            .ValueGeneratedNever();
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Phase> Phases { get; set; }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
}