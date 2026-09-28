using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Entities;

namespace Tickie.Manager.Data;

public class TickieDbContext : DbContext
{
    public TickieDbContext(DbContextOptions<TickieDbContext> options) : base(options)
    {
    }
    public DbSet<Project> Projects { get; set; }
}