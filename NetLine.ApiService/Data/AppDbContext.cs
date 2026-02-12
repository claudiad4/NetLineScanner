using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Models;

namespace NetLine.ApiService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceInfo> DevicesInfo => Set<DeviceInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tutaj definiujemy, że adres IP powinien być unikalny w naszej bazie
        modelBuilder.Entity<DeviceInfo>()
            .HasIndex(d => d.IpAddress)
            .IsUnique();
    }
}