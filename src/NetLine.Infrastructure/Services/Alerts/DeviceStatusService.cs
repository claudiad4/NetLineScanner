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

    public async Task ProcessScanResultsAsync(
        List<DeviceInfo> devices,
        IReadOnlyList<DeviceScanResult> scanResults,
        CancellationToken cancellationToken)
    {
        var byDeviceId = scanResults.ToDictionary(r => r.DeviceId);

        foreach (var device in devices)
        {
            if (!byDeviceId.TryGetValue(device.Id, out var scanResult))
            {
                _logger.LogWarning("No scan result for device {DeviceId}", device.Id);
                continue;
            }

            PersistMetrics(device, scanResult);

            var oldStatus = device.Status;
            var newStatus = DetermineStatus(scanResult);

            device.LastScanned = DateTime.UtcNow;
            device.PingResponseTimeMs = scanResult.PingResponseTimeMs;
            device.Status = newStatus;
            UpdateSnmpSnapshot(device, newStatus, scanResult.SnmpData);

            if (oldStatus != newStatus)
            {
                AddStatusChangeAlert(device, newStatus);
            }

            AddThresholdAlerts(device, scanResult);
            AddComponentFailureAlerts(device, scanResult);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private static string DetermineStatus(DeviceScanResult scanResult)
    {
        var pingReachable = scanResult.PingResponseTimeMs.HasValue;
        var snmpOk = scanResult.SnmpData.Success;

        if (pingReachable && snmpOk) return "Online";
        if (pingReachable) return "Limited";
        return "Offline";
    }

    private void AddStatusChangeAlert(DeviceInfo device, string newStatus)
    {
        var (type, message) = newStatus switch
        {
            "Offline" => (AlertType.WentOffline, $"Urzadzenie {device.UserDefinedName} przestalo odpowiadac."),
            _ => (AlertType.CameOnline, $"Urzadzenie {device.UserDefinedName} wrocilo do sieci.")
        };

        _dbContext.DeviceAlerts.Add(new DeviceAlert
        {
            DeviceInfoId = device.Id,
            Timestamp = DateTime.UtcNow,
            Type = type,
            Message = message
        });

        _logger.LogInformation("Status change for device {DeviceId}: {Status} — {Message}", device.Id, newStatus, message);
    }

    private void AddThresholdAlerts(DeviceInfo device, DeviceScanResult scanResult)
    {
        var metrics = scanResult.ComponentResults.SelectMany(c => c.Metrics).ToList();

        var rtt = Find(metrics, "ping.rtt_avg_ms");
        if (rtt is double r && r > HighLatencyMs)
        {
            AddAlert(device, AlertType.HighLatency, $"Wysokie opoznienie: {r:F0} ms");
        }

        var loss = Find(metrics, "ping.loss_pct");
        if (loss is double l && l >= HighLossPct && l < 100)
        {
            AddAlert(device, AlertType.HighPacketLoss, $"Utracone pakiety: {l:F0}%");
        }

        var cpu = Find(metrics, "cpu.usage_avg");
        if (cpu is double c && c >= HighCpuPct)
        {
            AddAlert(device, AlertType.HighCpuUsage, $"Wysokie obciazenie CPU: {c:F0}%");
        }

        var mem = Find(metrics, "memory.usage_pct");
        if (mem is double m && m >= HighMemoryPct)
        {
            AddAlert(device, AlertType.HighMemoryUsage, $"Wysokie uzycie pamieci: {m:F0}%");
        }

        var ifDown = metrics.FirstOrDefault(x => x.Key == "net.if.down");
        if (ifDown?.NumericValue is double d && d > 0)
        {
            AddAlert(device, AlertType.InterfaceDown, $"Interfejsy w stanie down: {d:F0}");
        }
    }

    private void AddComponentFailureAlerts(DeviceInfo device, DeviceScanResult scanResult)
    {
        foreach (var failed in scanResult.ComponentResults.Where(r => !r.Success))
        {
            AddAlert(device, AlertType.ComponentFailure,
                $"Komponent {failed.ComponentName} nie odpowiedzial: {failed.ErrorMessage}");
        }
    }

    private void AddAlert(DeviceInfo device, AlertType type, string message)
    {
        _dbContext.DeviceAlerts.Add(new DeviceAlert
        {
            DeviceInfoId = device.Id,
            Timestamp = DateTime.UtcNow,
            Type = type,
            Message = message
        });
    }

    private static double? Find(IEnumerable<ComponentMetric> metrics, string key)
        => metrics.FirstOrDefault(m => m.Key == key)?.NumericValue;

    private static string? Truncate(string? value, int max)
        => value is { Length: var n } && n > max ? value[..max] : value;

    private static void UpdateSnmpSnapshot(DeviceInfo device, string newStatus, SNMPScanResult snmp)
    {
        if (newStatus == "Online" && snmp.Success)
        {
            device.SysName = snmp.Name;
            device.SysDescr = snmp.Description;
            device.SysLocation = snmp.Location;
            device.SysContact = snmp.Contact;
            device.SysUpTime = snmp.UpTime;
            device.SysInterfacesCount = snmp.InterfacesCount;
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
        // "Limited": keep last snapshot, just refresh what we got
    }
}
