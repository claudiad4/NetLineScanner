using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Models;

namespace NetLine.ApiService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceBasicInfo> DevicesBasicInfo => Set<DeviceBasicInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceBasicInfo>()
            .HasIndex(d => d.UniqueIdOrName)
            .IsUnique(); // opcjonalnie: wymuś unikalność nazwy/ID
    }
}
