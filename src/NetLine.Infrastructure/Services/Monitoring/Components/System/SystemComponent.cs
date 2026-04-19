using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Services.Monitoring.Snmp;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NetLine.Infrastructure.Services.Monitoring.Components.System;
/// </summary>
public sealed class SystemComponent : IMonitoringComponent
{
    private readonly ISnmpClient _snmp;
    private readonly ILogger<SystemComponent> _logger;
    public SystemComponent(ISnmpClient snmp, ILogger<SystemComponent> logger)
    {
        _snmp = snmp;
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.System;
    public string Name => "System";
    public ScanFrequency Frequency => ScanFrequency.Medium;

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var oids = new[]
        {
            OIDDictionary.SysDescr,
            OIDDictionary.SysName,
            OIDDictionary.SysLocation,
            OIDDictionary.SysContact,
            OIDDictionary.SysUpTime,
            OIDDictionary.HrSystemNumUsers,
            OIDDictionary.HrSystemProcesses,
            OIDDictionary.LaLoad1,
            OIDDictionary.LaLoad5,
            OIDDictionary.LaLoad15
        };

        var data = await _snmp.GetAsync(device.IpAddress, oids, cancellationToken);
        if (data is null)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "SNMP timeout or unreachable");
        }

        var metrics = new List<ComponentMetric>();

        var descr = data[0].Data.ToString();
        var name = data[1].Data.ToString();
        var location = data[2].Data.ToString();
        var contact = data[3].Data.ToString();
        var upTime = data[4].Data.ToString();

        if (!string.IsNullOrWhiteSpace(descr)) metrics.Add(ComponentMetric.Text("system.descr", descr));
        if (!string.IsNullOrWhiteSpace(name)) metrics.Add(ComponentMetric.Text("system.name", name));
        if (!string.IsNullOrWhiteSpace(location)) metrics.Add(ComponentMetric.Text("system.location", location));
        if (!string.IsNullOrWhiteSpace(contact)) metrics.Add(ComponentMetric.Text("system.contact", contact));
        if (!string.IsNullOrWhiteSpace(upTime)) metrics.Add(ComponentMetric.Text("system.uptime", upTime));

        var (osFamily, osVersion) = DetectOs(descr);
        if (osFamily is not null) metrics.Add(ComponentMetric.Text("system.os_family", osFamily));
        if (osVersion is not null) metrics.Add(ComponentMetric.Text("system.os_version", osVersion));

        if (int.TryParse(data[5].Data.ToString(), out var users))
            metrics.Add(ComponentMetric.Numeric("system.users", users, "count", "Logged users"));

        if (int.TryParse(data[6].Data.ToString(), out var procs))
            metrics.Add(ComponentMetric.Numeric("system.processes", procs, "count", "Processes"));

        TryAddLoad(metrics, "system.load_1", data[7].Data.ToString(), "1 min");
        TryAddLoad(metrics, "system.load_5", data[8].Data.ToString(), "5 min");
        TryAddLoad(metrics, "system.load_15", data[9].Data.ToString(), "15 min");

        _logger.LogDebug("System component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    private static void TryAddLoad(List<ComponentMetric> metrics, string key, string? raw, string label)
    {
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            metrics.Add(ComponentMetric.Numeric(key, value, "load", label));
        }
    }

    private static (string? family, string? version) DetectOs(string? sysDescr)
    {
        if (string.IsNullOrWhiteSpace(sysDescr)) return (null, null);

        var lower = sysDescr.ToLowerInvariant();
        if (lower.Contains("windows")) return ("Windows", ExtractVersion(sysDescr));
        if (lower.Contains("linux")) return ("Linux", ExtractVersion(sysDescr));
        if (lower.Contains("darwin") || lower.Contains("mac os")) return ("macOS", ExtractVersion(sysDescr));
        if (lower.Contains("freebsd")) return ("FreeBSD", ExtractVersion(sysDescr));
        if (lower.Contains("cisco")) return ("Cisco IOS", ExtractVersion(sysDescr));
        if (lower.Contains("mikrotik") || lower.Contains("routeros")) return ("RouterOS", ExtractVersion(sysDescr));
        return ("Other", null);
    }

    private static string? ExtractVersion(string sysDescr)
    {
        var match = Regex.Match(sysDescr, @"\d+(\.\d+){1,3}");
        return match.Success ? match.Value : null;
    }
}
