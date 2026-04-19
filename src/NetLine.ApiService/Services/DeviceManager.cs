using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces.Devices;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Services;

public class DeviceManager : IDeviceManager
{
    private readonly AppDbContext _db;
    private readonly IDeviceScanner _scanner;
    private readonly IScanningPolicy _scanningPolicy;

    public DeviceManager(AppDbContext db, IDeviceScanner scanner, IScanningPolicy scanningPolicy)
    {
        _db = db;
        _scanner = scanner;
        _scanningPolicy = scanningPolicy;
    }

    public async Task<IEnumerable<DeviceInfo>> GetAllAsync()
        => await _db.DevicesInfo.ToListAsync();

    public async Task<DeviceScanResult> ScanAsync(string ip)
    {
        var ephemeral = new DeviceInfo
        {
            IpAddress = ip,
            UserDefinedName = ip,
            DeviceType = "Other"
        };
        return await _scanner.ScanDeviceAsync(ephemeral, CancellationToken.None);
    }

    public async Task<DeviceInfo> AddAsync(AddDeviceRequest request)
    {
        var exists = await _db.DevicesInfo.AnyAsync(d => d.IpAddress == request.Ip);
        if (exists)
            throw new InvalidOperationException("This IP is already in the database.");

        var device = new DeviceInfo
        {
            IpAddress = request.Ip,
            UserDefinedName = request.UserLabel,
            DeviceType = request.Type,
            OfficeId = request.OfficeId,
            Status = "Offline",
            LastScanned = DateTime.UtcNow
        };

        try
        {
            var scan = await _scanner.ScanDeviceAsync(device, CancellationToken.None);
            ApplyInitialSnapshot(device, scan);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to scan device at {request.Ip}: {ex.Message}", ex);
        }

        _db.DevicesInfo.Add(device);
        await _db.SaveChangesAsync();

        _scanningPolicy.MarkAllRan(device.Id, DateTime.UtcNow);

        return device;
    }

    private static void ApplyInitialSnapshot(DeviceInfo device, DeviceScanResult scan)
    {
        var metrics = scan.AllMetrics.ToList();

        var pingReachable = metrics.FirstOrDefault(m => m.Key == "ping.reachable")?.TextValue == "true";
        var rtt = metrics.FirstOrDefault(m => m.Key == "ping.rtt_avg_ms")?.NumericValue;
        var systemOk = scan.ComponentResults.Any(c => c.ComponentName == "System" && c.Success);

        device.PingResponseTimeMs = pingReachable && rtt.HasValue ? (long?)Math.Round(rtt.Value) : null;
        device.Status = pingReachable
            ? (systemOk ? "Online" : "Limited")
            : "Offline";

        device.SysName = metrics.FirstOrDefault(m => m.Key == "system.name")?.TextValue;
        device.SysDescr = metrics.FirstOrDefault(m => m.Key == "system.descr")?.TextValue;
        device.SysLocation = metrics.FirstOrDefault(m => m.Key == "system.location")?.TextValue;
        device.SysContact = metrics.FirstOrDefault(m => m.Key == "system.contact")?.TextValue;
        device.SysUpTime = metrics.FirstOrDefault(m => m.Key == "system.uptime")?.TextValue;
        device.LastScanned = DateTime.UtcNow;
    }
}
