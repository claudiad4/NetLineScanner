using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Monitoring.Components.Network;

/// <summary>
/// Sends a burst of ICMP echo requests and reports RTT, jitter, packet loss and
/// the number of failed probes (maps to the "Health" metric requested by the user).
/// </summary>
public sealed class PingComponent : IMonitoringComponent
{
    private const int ProbeCount = 4;
    private const int TimeoutMs = 500;

    private readonly ILogger<PingComponent> _logger;

    public PingComponent(ILogger<PingComponent> logger)
    {
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Network;
    public string Name => "Ping";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var rtts = new List<long>(ProbeCount);
        var failed = 0;

        for (var i = 0; i < ProbeCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(device.IpAddress, TimeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    rtts.Add(reply.RoundtripTime == 0 ? 1 : reply.RoundtripTime);
                }
                else
                {
                    failed++;
                }
            }
            catch
            {
                failed++;
            }
        }

        var lossPct = (double)failed / ProbeCount * 100.0;
        var metrics = new List<ComponentMetric>
        {
            ComponentMetric.Numeric("ping.probes", ProbeCount, "count", "Probes sent"),
            ComponentMetric.Numeric("ping.failed", failed, "count", "Failed probes"),
            ComponentMetric.Numeric("ping.loss_pct", Math.Round(lossPct, 2), "%", "Packet loss")
        };

        if (rtts.Count > 0)
        {
            var avg = rtts.Average();
            metrics.Add(ComponentMetric.Numeric("ping.rtt_avg_ms", Math.Round(avg, 2), "ms", "Avg RTT"));
            metrics.Add(ComponentMetric.Numeric("ping.rtt_min_ms", rtts.Min(), "ms", "Min RTT"));
            metrics.Add(ComponentMetric.Numeric("ping.rtt_max_ms", rtts.Max(), "ms", "Max RTT"));
            if (rtts.Count > 1)
            {
                var jitter = rtts.Zip(rtts.Skip(1), (a, b) => Math.Abs(b - a)).Average();
                metrics.Add(ComponentMetric.Numeric("ping.jitter_ms", Math.Round(jitter, 2), "ms", "Jitter"));
            }
            metrics.Add(ComponentMetric.Text("ping.reachable", "true"));
        }
        else
        {
            metrics.Add(ComponentMetric.Text("ping.reachable", "false"));
        }

        _logger.LogDebug("Ping component {Device}: {Failed}/{Total} failed", device.IpAddress, failed, ProbeCount);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }
}
