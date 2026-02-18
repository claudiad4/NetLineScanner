using Microsoft.EntityFrameworkCore;
using NetLine.Domain;

namespace NetLine.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceInfo> DevicesInfo => Set<DeviceInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceInfo>()
            .HasIndex(d => d.IpAddress)
            .IsUnique();
    }
}