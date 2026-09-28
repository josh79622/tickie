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
    }

    public DbSet<Project> Projects { get; set; }
}