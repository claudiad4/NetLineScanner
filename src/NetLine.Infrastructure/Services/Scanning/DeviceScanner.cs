using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Scanning;

/// <summary>
/// Iterates all registered <see cref="IMonitoringComponent"/>s against every device in parallel.
/// Produces a <see cref="DeviceScanResult"/> enriched with legacy ping/SNMP fields derived from
/// the Ping and System components so the API shape stays backward compatible.
/// </summary>
public class DeviceScanner : IDeviceScanner
{
    private readonly IReadOnlyList<IMonitoringComponent> _components;
    private readonly ILogger<DeviceScanner> _logger;

    public DeviceScanner(
        IEnumerable<IMonitoringComponent> components,
        ILogger<DeviceScanner> logger)
    {
        _components = components.ToList();
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken)
    {
        var deviceList = devices.ToList();
        var scanResults = new List<DeviceScanResult>();

        await Parallel.ForEachAsync(
            deviceList,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (device, ct) =>
            {
                var componentResults = await RunComponentsAsync(device, ct);
                var result = BuildScanResult(device, componentResults);

                lock (scanResults)
                {
                    scanResults.Add(result);
                }
            });

        return scanResults.AsReadOnly();
    }

    private async Task<IReadOnlyList<ComponentResult>> RunComponentsAsync(DeviceInfo device, CancellationToken ct)
    {
        var tasks = _components.Select(async c =>
        {
            try
            {
                return await c.CollectAsync(device, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Component {Component} failed for device {DeviceId}", c.Name, device.Id);
                return ComponentResult.Fail(device.Id, c.Category, c.Name, ex.Message);
            }
        });

        return await Task.WhenAll(tasks);
    }

    private static DeviceScanResult BuildScanResult(DeviceInfo device, IReadOnlyList<ComponentResult> componentResults)
    {
        var pingMs = ExtractPingRtt(componentResults);
        var snmp = ExtractLegacySnmp(componentResults);

        return new DeviceScanResult(
            device.Id,
            device.IpAddress,
            pingMs,
            snmp,
            componentResults);
    }

    private static long? ExtractPingRtt(IReadOnlyList<ComponentResult> results)
    {
        var pingResult = results.FirstOrDefault(r => r.ComponentName == "Ping");
        if (pingResult is null || !pingResult.Success) return null;

        var avg = pingResult.Metrics.FirstOrDefault(m => m.Key == "ping.rtt_avg_ms");
        return avg?.NumericValue is double v ? (long)Math.Round(v) : null;
    }

    private static SNMPScanResult ExtractLegacySnmp(IReadOnlyList<ComponentResult> results)
    {
        var sys = results.FirstOrDefault(r => r.ComponentName == "System");
        if (sys is null || !sys.Success)
        {
            return new SNMPScanResult { Success = false, ErrorMessage = sys?.ErrorMessage ?? "System component missing" };
        }

        string? Text(string key) => sys.Metrics.FirstOrDefault(m => m.Key == key)?.TextValue;

        var ifCount = results.FirstOrDefault(r => r.ComponentName == "NetworkInterface")
            ?.Metrics.FirstOrDefault(m => m.Key == "net.if.total")?.NumericValue;

        return new SNMPScanResult
        {
            Success = true,
            Description = Text("system.descr"),
            Name = Text("system.name"),
            Location = Text("system.location"),
            Contact = Text("system.contact"),
            UpTime = Text("system.uptime"),
            InterfacesCount = ifCount.HasValue ? (int)ifCount.Value : null
        };
    }
}
