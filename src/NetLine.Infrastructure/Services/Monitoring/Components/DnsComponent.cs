using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Monitoring.Components;

/// <summary>
/// Performs a reverse DNS lookup of the device's IP and records the
/// hostname and response time. A failure still produces a result
/// (with reachable=false) so downstream code can flag it.
/// </summary>
public sealed class DnsComponent : IMonitoringComponent
{
    private readonly ILogger<DnsComponent> _logger;

    public DnsComponent(ILogger<DnsComponent> logger)
    {
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Network;
    public string Name => "Dns";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var metrics = new List<ComponentMetric>();
        var sw = Stopwatch.StartNew();

        try
        {
            var entry = await Dns.GetHostEntryAsync(device.IpAddress, cancellationToken);
            sw.Stop();
            metrics.Add(ComponentMetric.Text("dns.resolved", "true"));
            metrics.Add(ComponentMetric.Text("dns.hostname", entry.HostName));
            metrics.Add(ComponentMetric.Numeric("dns.response_ms", sw.ElapsedMilliseconds, "ms", "DNS response"));
        }
        catch (Exception ex)
        {
            sw.Stop();
            metrics.Add(ComponentMetric.Text("dns.resolved", "false"));
            metrics.Add(ComponentMetric.Numeric("dns.response_ms", sw.ElapsedMilliseconds, "ms", "DNS response"));
            _logger.LogDebug("DNS lookup failed for {Ip}: {Message}", device.IpAddress, ex.Message);
        }

        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }
}
