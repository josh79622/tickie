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

        modelBuilder.Entity<StatusChange>()
            .Property(sc => sc.FromStatus)
            .HasConversion<string>();
        
        modelBuilder.Entity<StatusChange>()
            .Property(sc => sc.ToStatus)
            .HasConversion<string>();

        modelBuilder.Entity<StatusChange>()
            .HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(sc => sc.TicketId);
        
        modelBuilder.Entity<StatusChangeTicket>()
            .HasKey(sct => new { sct.StatusChangeId, sct.TicketId });

        modelBuilder.Entity<StatusChangeTicket>()
            .HasOne<StatusChange>()
            .WithMany()
            .HasForeignKey(sct => sct.StatusChangeId);

        modelBuilder.Entity<StatusChangeTicket>()
            .HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(sct => sct.TicketId);

        modelBuilder.Entity<TicketDependency>()
            .HasKey(td => new { td.TicketId, td.PrerequisiteTicketId });

        modelBuilder.Entity<TicketDependency>()
            .HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(td => td.TicketId);

        modelBuilder.Entity<TicketDependency>()
            .HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(td => td.PrerequisiteTicketId);

        modelBuilder.Entity<TicketDependency>()
            .ToTable(t => t.HasCheckConstraint("CK_TicketDependencies_NotSelf", "\"TicketId\" <> \"PrerequisiteTicketId\""));

        modelBuilder.Entity<Label>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(l => l.ProjectId);

        modelBuilder.Entity<Label>()
            .HasIndex(l => new { l.ProjectId, l.Name })
            .IsUnique();

        modelBuilder.Entity<TicketLabel>()
            .HasKey(tl => new { tl.TicketId, tl.LabelId });

        modelBuilder.Entity<TicketLabel>()
            .HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(tl => tl.TicketId);

        modelBuilder.Entity<TicketLabel>()
            .HasOne<Label>()
            .WithMany()
            .HasForeignKey(tl => tl.LabelId);
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Phase> Phases { get; set; }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<StatusChange> StatusChanges { get; set; }
    public DbSet<StatusChangeTicket> StatusChangeTickets { get; set; }
    public DbSet<TicketDependency> TicketDependencies { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<TicketLabel> TicketLabels { get; set; }
}
