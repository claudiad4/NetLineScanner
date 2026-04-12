using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NetLine.Domain.Entities;

namespace NetLine.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceInfo> DevicesInfo => Set<DeviceInfo>();
    public DbSet<DeviceAlert> DeviceAlerts { get; set; }
    public DbSet<Office> Offices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceInfo>()
            .HasIndex(d => d.IpAddress)
            .IsUnique();

        modelBuilder.Entity<DeviceAlert>()
            .HasOne(da => da.Device)
            .WithMany()
            .HasForeignKey(da => da.DeviceInfoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceInfo>()
            .HasOne(d => d.Office)
            .WithMany(o => o.Devices)
            .HasForeignKey(d => d.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}