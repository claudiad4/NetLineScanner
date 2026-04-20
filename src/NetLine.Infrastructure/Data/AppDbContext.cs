using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NetLine.Domain.Entities;
using NetLine.Infrastructure;
using NetLine.Infrastructure.Identity;

namespace NetLine.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceInfo> DevicesInfo => Set<DeviceInfo>();
    public DbSet<DeviceAlert> DeviceAlerts { get; set; }
    public DbSet<Office> Offices { get; set; }
    public DbSet<DeviceMetric> DeviceMetrics => Set<DeviceMetric>();
    public DbSet<OfficeAdminAssignment> OfficeAdminAssignments => Set<OfficeAdminAssignment>();

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

        modelBuilder.Entity<DeviceMetric>(b =>
        {
            b.HasOne(m => m.Device)
                .WithMany()
                .HasForeignKey(m => m.DeviceInfoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(m => new { m.DeviceInfoId, m.Timestamp });
            b.HasIndex(m => new { m.DeviceInfoId, m.MetricKey, m.Timestamp });
        });

        // User (role: "User") -> exactly one Office (optional on DB level,
        // enforced on application level for the "User" role).
        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasOne(u => u.Office)
                .WithMany()
                .HasForeignKey(u => u.OfficeId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(u => u.OfficeId);
        });

        // OfficeAdmin (role: "OfficeAdmin") -> many Offices via join table.
        modelBuilder.Entity<OfficeAdminAssignment>(b =>
        {
            b.ToTable("OfficeAdminAssignments");
            b.HasKey(x => new { x.UserId, x.OfficeId });

            b.HasOne(x => x.User)
                .WithMany(u => u.ManagedOffices)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Office)
                .WithMany()
                .HasForeignKey(x => x.OfficeId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.OfficeId);
        });
    }
}
