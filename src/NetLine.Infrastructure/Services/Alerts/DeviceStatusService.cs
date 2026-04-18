using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Alerts;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Services.Alerts;

/// <summary>
/// Consumes the component-based scan results, persists time-series metrics,
/// updates the device's "current snapshot" columns and generates alerts.
/// </summary>
public class DeviceStatusService : IDeviceStatusService
{
    private const double HighLatencyMs = 200;
    private const double HighLossPct = 20;
    private const double HighCpuPct = 90;
    private const double HighMemoryPct = 90;

    private readonly ILogger<DeviceStatusService> _logger;
    private readonly AppDbContext _dbContext;

    public DeviceStatusService(ILogger<DeviceStatusService> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DeviceAlert>> ProcessScanResultsAsync(
        List<DeviceInfo> devices,
        IReadOnlyList<DeviceScanResult> scanResults,
        CancellationToken cancellationToken)
    {
        var byDeviceId = scanResults.ToDictionary(r => r.DeviceId);
        var createdAlerts = new List<DeviceAlert>();

        foreach (var device in devices)
        {
            if (!byDeviceId.TryGetValue(device.Id, out var scanResult))
            {
                _logger.LogWarning("No scan result for device {DeviceId}", device.Id);
                continue;
            }

            PersistMetrics(device, scanResult);

            var snapshot = ScanSnapshot.From(scanResult);

            var oldStatus = device.Status;
            var newStatus = DetermineStatus(snapshot);

            device.LastScanned = DateTime.UtcNow;
            device.PingResponseTimeMs = snapshot.PingResponseTimeMs;
            device.Status = newStatus;
            UpdateSnmpSnapshot(device, newStatus, snapshot);

            if (oldStatus != newStatus)
            {
                createdAlerts.Add(AddStatusChangeAlert(device, newStatus));
            }

            createdAlerts.AddRange(AddThresholdAlerts(device, scanResult));
            createdAlerts.AddRange(AddComponentFailureAlerts(device, scanResult));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return createdAlerts;
    }

    private void PersistMetrics(DeviceInfo device, DeviceScanResult scanResult)
    {
        foreach (var component in scanResult.ComponentResults)
        {
            if (!component.Success) continue;
            foreach (var metric in component.Metrics)
            {
                _dbContext.DeviceMetrics.Add(new DeviceMetric
                {
                    DeviceInfoId = device.Id,
                    Timestamp = component.CollectedAt,
                    Category = component.Category,
                    ComponentName = Truncate(component.ComponentName, 64)!,
                    MetricKey = Truncate(metric.Key, 128)!,
                    NumericValue = metric.NumericValue,
                    TextValue = Truncate(metric.TextValue, 512),
                    Unit = Truncate(metric.Unit, 32),
                    Label = Truncate(metric.Label, 128)
                });
            }
        }
    }

    private static string DetermineStatus(ScanSnapshot snapshot)
    {
        if (snapshot.PingReachable && snapshot.SystemOk) return "Online";
        if (snapshot.PingReachable) return "Limited";
        return "Offline";
    }

    private DeviceAlert AddStatusChangeAlert(DeviceInfo device, string newStatus)
    {
        var (type, message) = newStatus switch
        {
            "Offline" => (AlertType.WentOffline, $"Urzadzenie {device.UserDefinedName} przestalo odpowiadac."),
            _ => (AlertType.CameOnline, $"Urzadzenie {device.UserDefinedName} wrocilo do sieci.")
        };

        var alert = new DeviceAlert
        {
            DeviceInfoId = device.Id,
            Timestamp = DateTime.UtcNow,
            Type = type,
            Message = message
        };
        _dbContext.DeviceAlerts.Add(alert);

        _logger.LogInformation("Status change for device {DeviceId}: {Status} - {Message}", device.Id, newStatus, message);
        return alert;
    }

    private List<DeviceAlert> AddThresholdAlerts(DeviceInfo device, DeviceScanResult scanResult)
    {
        var created = new List<DeviceAlert>();
        var metrics = scanResult.ComponentResults.SelectMany(c => c.Metrics).ToList();

        var rtt = Find(metrics, "ping.rtt_avg_ms");
        if (rtt is double r && r > HighLatencyMs)
        {
            created.Add(AddAlert(device, AlertType.HighLatency, $"Wysokie opoznienie: {r:F0} ms"));
        }

        var loss = Find(metrics, "ping.loss_pct");
        if (loss is double l && l >= HighLossPct && l < 100)
        {
            created.Add(AddAlert(device, AlertType.HighPacketLoss, $"Utracone pakiety: {l:F0}%"));
        }

        var cpu = Find(metrics, "cpu.usage_avg");
        if (cpu is double c && c >= HighCpuPct)
        {
            created.Add(AddAlert(device, AlertType.HighCpuUsage, $"Wysokie obciazenie CPU: {c:F0}%"));
        }

        var mem = Find(metrics, "memory.usage_pct");
        if (mem is double m && m >= HighMemoryPct)
        {
            created.Add(AddAlert(device, AlertType.HighMemoryUsage, $"Wysokie uzycie pamieci: {m:F0}%"));
        }

        var ifDown = metrics.FirstOrDefault(x => x.Key == "net.if.down");
        if (ifDown?.NumericValue is double d && d > 0)
        {
            created.Add(AddAlert(device, AlertType.InterfaceDown, $"Interfejsy w stanie down: {d:F0}"));
        }

        return created;
    }

    private List<DeviceAlert> AddComponentFailureAlerts(DeviceInfo device, DeviceScanResult scanResult)
    {
        var created = new List<DeviceAlert>();
        foreach (var failed in scanResult.ComponentResults.Where(r => !r.Success))
        {
            created.Add(AddAlert(device, AlertType.ComponentFailure,
                $"Komponent {failed.ComponentName} nie odpowiedzial: {failed.ErrorMessage}"));
        }
        return created;
    }

    private DeviceAlert AddAlert(DeviceInfo device, AlertType type, string message)
    {
        var alert = new DeviceAlert
        {
            DeviceInfoId = device.Id,
            Timestamp = DateTime.UtcNow,
            Type = type,
            Message = message
        };
        _dbContext.DeviceAlerts.Add(alert);
        return alert;
    }

    private static double? Find(IEnumerable<ComponentMetric> metrics, string key)
        => metrics.FirstOrDefault(m => m.Key == key)?.NumericValue;

    private static string? Truncate(string? value, int max)
        => value is { Length: var n } && n > max ? value[..max] : value;

    private static void UpdateSnmpSnapshot(DeviceInfo device, string newStatus, ScanSnapshot snapshot)
    {
        if (newStatus == "Online" && snapshot.SystemOk)
        {
            device.SysName = snapshot.SysName;
            device.SysDescr = snapshot.SysDescr;
            device.SysLocation = snapshot.SysLocation;
            device.SysContact = snapshot.SysContact;
            device.SysUpTime = snapshot.SysUpTime;
            device.SysInterfacesCount = snapshot.SysInterfacesCount;
        }
        else if (newStatus == "Offline")
        {
            device.PingResponseTimeMs = null;
            device.SysName = null;
            device.SysDescr = null;
            device.SysLocation = null;
            device.SysContact = null;
            device.SysUpTime = null;
            device.SysInterfacesCount = null;
        }
        // "Limited": keep last snapshot
    }

    /// <summary>
    /// Distills the values that DeviceStatusService cares about out of the raw
    /// component results, so the rest of the class doesn't have to re-walk them.
    /// </summary>
    private readonly record struct ScanSnapshot(
        bool PingReachable,
        long? PingResponseTimeMs,
        bool SystemOk,
        string? SysName,
        string? SysDescr,
        string? SysLocation,
        string? SysContact,
        string? SysUpTime,
        int? SysInterfacesCount)
    {
        public static ScanSnapshot From(DeviceScanResult scan)
        {
            var metrics = scan.AllMetrics.ToList();
            var pingReachable = metrics.FirstOrDefault(m => m.Key == "ping.reachable")?.TextValue == "true";
            var rtt = metrics.FirstOrDefault(m => m.Key == "ping.rtt_avg_ms")?.NumericValue;
            var systemOk = scan.ComponentResults.Any(c => c.ComponentName == "System" && c.Success);

            int? ifCount = null;
            var ifMetric = metrics.FirstOrDefault(m => m.Key == "net.if.total");
            if (ifMetric?.NumericValue is double v) ifCount = (int)Math.Round(v);

            return new ScanSnapshot(
                PingReachable: pingReachable,
                PingResponseTimeMs: pingReachable && rtt.HasValue ? (long?)Math.Round(rtt.Value) : null,
                SystemOk: systemOk,
                SysName: metrics.FirstOrDefault(m => m.Key == "system.name")?.TextValue,
                SysDescr: metrics.FirstOrDefault(m => m.Key == "system.descr")?.TextValue,
                SysLocation: metrics.FirstOrDefault(m => m.Key == "system.location")?.TextValue,
                SysContact: metrics.FirstOrDefault(m => m.Key == "system.contact")?.TextValue,
                SysUpTime: metrics.FirstOrDefault(m => m.Key == "system.uptime")?.TextValue,
                SysInterfacesCount: ifCount);
        }
    }
}
