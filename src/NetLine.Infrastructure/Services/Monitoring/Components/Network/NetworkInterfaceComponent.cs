using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Services.Monitoring.Snmp;

namespace NetLine.Infrastructure.Services.Monitoring.Components.Network;
/// </summary>
public sealed class NetworkInterfaceComponent : IMonitoringComponent
{
    private readonly ISnmpClient _snmp;
    private readonly ILogger<NetworkInterfaceComponent> _logger;
    private static readonly ConcurrentDictionary<int, OctetSnapshot> _lastSnapshot = new();
    public NetworkInterfaceComponent(ISnmpClient snmp, ILogger<NetworkInterfaceComponent> logger)
    {
        _snmp = snmp;
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Network;
    public string Name => "NetworkInterface";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var descr = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.IfDescr, cancellationToken);
        if (descr.Count == 0)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "IF-MIB unavailable");
        }

        var operStatus = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.IfOperStatus, cancellationToken);
        var adminStatus = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.IfAdminStatus, cancellationToken);
        var inOctets = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.IfInOctets, cancellationToken);
        var outOctets = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.IfOutOctets, cancellationToken);

        var descrByIndex = IndexByLastOid(descr);
        var operByIndex = IndexByLastOid(operStatus);
        var adminByIndex = IndexByLastOid(adminStatus);
        var inByIndex = IndexByLastOid(inOctets);
        var outByIndex = IndexByLastOid(outOctets);

        var metrics = new List<ComponentMetric>();
        var now = DateTime.UtcNow;
        var snapshot = new OctetSnapshot { Timestamp = now, Counters = new Dictionary<int, (long inOct, long outOct)>() };
        _lastSnapshot.TryGetValue(device.Id, out var previous);

        var upCount = 0;
        var downCount = 0;

        foreach (var (index, nameVar) in descrByIndex)
        {
            var ifName = nameVar.Data.ToString() ?? $"if{index}";
            var label = $"{ifName} (#{index})";

            if (operByIndex.TryGetValue(index, out var oper) && int.TryParse(oper.Data.ToString(), out var operVal))
            {
                metrics.Add(new ComponentMetric($"net.if.{index}.oper_status", operVal, MapIfStatus(operVal), "state", label));
                if (operVal == 1) upCount++; else downCount++;
            }

            if (adminByIndex.TryGetValue(index, out var admin) && int.TryParse(admin.Data.ToString(), out var adminVal))
            {
                metrics.Add(new ComponentMetric($"net.if.{index}.admin_status", adminVal, MapIfStatus(adminVal), "state", label));
            }

            long? inOct = inByIndex.TryGetValue(index, out var inV) && long.TryParse(inV.Data.ToString(), out var inI) ? inI : null;
            long? outOct = outByIndex.TryGetValue(index, out var outV) && long.TryParse(outV.Data.ToString(), out var outI) ? outI : null;

            if (inOct.HasValue && outOct.HasValue)
            {
                snapshot.Counters[index] = (inOct.Value, outOct.Value);

                if (previous is not null && previous.Counters.TryGetValue(index, out var prev))
                {
                    var secs = (now - previous.Timestamp).TotalSeconds;
                    if (secs > 0)
                    {
                        var inBps = DeltaPerSecond(prev.inOct, inOct.Value, secs);
                        var outBps = DeltaPerSecond(prev.outOct, outOct.Value, secs);
                        metrics.Add(ComponentMetric.Numeric($"net.if.{index}.download_bps", inBps, "bps", label));
                        metrics.Add(ComponentMetric.Numeric($"net.if.{index}.upload_bps", outBps, "bps", label));
                    }
                }
            }
        }

        _lastSnapshot[device.Id] = snapshot;

        metrics.Add(ComponentMetric.Numeric("net.if.total", descrByIndex.Count, "count", "Interfaces"));
        metrics.Add(ComponentMetric.Numeric("net.if.up", upCount, "count", "Interfaces up"));
        metrics.Add(ComponentMetric.Numeric("net.if.down", downCount, "count", "Interfaces down"));

        _logger.LogDebug("NetworkInterface component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    private static Dictionary<int, Lextm.SharpSnmpLib.Variable> IndexByLastOid(IReadOnlyList<Lextm.SharpSnmpLib.Variable> variables)
    {
        var map = new Dictionary<int, Lextm.SharpSnmpLib.Variable>();
        foreach (var v in variables)
        {
            var parts = v.Id.ToString().Split('.');
            if (int.TryParse(parts[^1], out var idx))
            {
                map[idx] = v;
            }
        }
        return map;
    }

    private static double DeltaPerSecond(long previous, long current, double seconds)
    {
        var delta = current >= previous ? current - previous : current; // counter wrap → start over
        return Math.Round(delta * 8.0 / seconds, 2); // bits per second
    }

    private static string MapIfStatus(int v) => v switch
    {
        1 => "up",
        2 => "down",
        3 => "testing",
        4 => "unknown",
        5 => "dormant",
        6 => "notPresent",
        7 => "lowerLayerDown",
        _ => v.ToString()
    };

    private sealed class OctetSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<int, (long inOct, long outOct)> Counters { get; set; } = new();
    }
}
