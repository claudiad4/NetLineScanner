using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Services.Monitoring.Snmp;

namespace NetLine.Infrastructure.Services.Monitoring.Components.Memory;
/// </summary>
public sealed class MemoryComponent : IMonitoringComponent
{
    private readonly ISnmpClient _snmp;
    private readonly ILogger<MemoryComponent> _logger;
    public MemoryComponent(ISnmpClient snmp, ILogger<MemoryComponent> logger)
    {
        _snmp = snmp;
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Memory;
    public string Name => "Memory";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var metrics = new List<ComponentMetric>();

        var ucd = await _snmp.GetAsync(device.IpAddress,
            new[] { OIDDictionary.MemTotalReal, OIDDictionary.MemAvailReal, OIDDictionary.MemTotalFree },
            cancellationToken);

        double? totalKb = null;
        double? freeKb = null;

        if (ucd is not null)
        {
            if (double.TryParse(ucd[0].Data.ToString(), out var t)) totalKb = t;
            if (double.TryParse(ucd[1].Data.ToString(), out var a)) freeKb = a;
            else if (double.TryParse(ucd[2].Data.ToString(), out var f)) freeKb = f;
        }

        if (totalKb is null || freeKb is null)
        {
            (totalKb, freeKb) = await ReadFromHostResourcesAsync(device.IpAddress, cancellationToken);
        }

        if (totalKb is null || freeKb is null || totalKb == 0)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "Memory OIDs unavailable");
        }

        var totalMb = totalKb.Value / 1024.0;
        var freeMb = freeKb.Value / 1024.0;
        var usedMb = Math.Max(0, totalMb - freeMb);
        var usagePct = totalMb > 0 ? usedMb / totalMb * 100.0 : 0;

        metrics.Add(ComponentMetric.Numeric("memory.total_mb", Math.Round(totalMb, 2), "MB", "Total RAM"));
        metrics.Add(ComponentMetric.Numeric("memory.free_mb", Math.Round(freeMb, 2), "MB", "Free RAM"));
        metrics.Add(ComponentMetric.Numeric("memory.used_mb", Math.Round(usedMb, 2), "MB", "Used RAM"));
        metrics.Add(ComponentMetric.Numeric("memory.usage_pct", Math.Round(usagePct, 2), "%", "Usage %"));

        _logger.LogDebug("Memory component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    private async Task<(double? totalKb, double? freeKb)> ReadFromHostResourcesAsync(string ip, CancellationToken ct)
    {
        var descr = await _snmp.WalkAsync(ip, OIDDictionary.HrStorageDescr, ct);
        if (descr.Count == 0) return (null, null);

        int? ramIndex = null;
        foreach (var v in descr)
        {
            var text = v.Data.ToString();
            if (!string.IsNullOrEmpty(text) &&
                (text.Contains("RAM", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("Physical Memory", StringComparison.OrdinalIgnoreCase)))
            {
                var oidParts = v.Id.ToString().Split('.');
                if (int.TryParse(oidParts[^1], out var idx))
                {
                    ramIndex = idx;
                    break;
                }
            }
        }

        if (ramIndex is null) return (null, null);

        var detail = await _snmp.GetAsync(ip, new[]
        {
            $"{OIDDictionary.HrStorageAllocationUnits}.{ramIndex}",
            $"{OIDDictionary.HrStorageSize}.{ramIndex}",
            $"{OIDDictionary.HrStorageUsed}.{ramIndex}"
        }, ct);

        if (detail is null) return (null, null);

        if (!long.TryParse(detail[0].Data.ToString(), out var unit)) return (null, null);
        if (!long.TryParse(detail[1].Data.ToString(), out var size)) return (null, null);
        if (!long.TryParse(detail[2].Data.ToString(), out var used)) return (null, null);

        var totalBytes = (double)size * unit;
        var usedBytes = (double)used * unit;
        var freeBytes = Math.Max(0, totalBytes - usedBytes);

        return (totalBytes / 1024.0, freeBytes / 1024.0);
    }
}
