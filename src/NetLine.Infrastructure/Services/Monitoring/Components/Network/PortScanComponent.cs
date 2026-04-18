using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Monitoring.Components.Network;

/// <summary>
/// TCP connect scan against a small curated list of management and service ports.
/// Uses a short timeout per port and runs probes in parallel.
/// </summary>
public sealed class PortScanComponent : IMonitoringComponent
{
    private const int ConnectTimeoutMs = 500;

    private static readonly (int Port, string Service)[] DefaultPorts =
    {
        (21, "FTP"),
        (22, "SSH"),
        (23, "Telnet"),
        (25, "SMTP"),
        (53, "DNS"),
        (80, "HTTP"),
        (110, "POP3"),
        (139, "NetBIOS"),
        (143, "IMAP"),
        (443, "HTTPS"),
        (445, "SMB"),
        (3306, "MySQL"),
        (3389, "RDP"),
        (5432, "PostgreSQL"),
        (8080, "HTTP-Alt")
    };

    private readonly ILogger<PortScanComponent> _logger;

    public PortScanComponent(ILogger<PortScanComponent> logger)
    {
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Network;
    public string Name => "PortScan";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var probes = DefaultPorts.Select(p => ProbeAsync(device.IpAddress, p.Port, p.Service, cancellationToken)).ToArray();
        var results = await Task.WhenAll(probes);

        var metrics = new List<ComponentMetric>();
        var openCount = 0;

        foreach (var (port, service, open) in results)
        {
            metrics.Add(new ComponentMetric(
                Key: $"port.{port}.open",
                NumericValue: open ? 1 : 0,
                TextValue: open ? "open" : "closed",
                Unit: "state",
                Label: $"{service} ({port})"));
            if (open) openCount++;
        }

        metrics.Add(ComponentMetric.Numeric("port.open_count", openCount, "count", "Open ports"));

        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    private static async Task<(int Port, string Service, bool Open)> ProbeAsync(string ip, int port, string service, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs, ct));
            return (port, service, completed == connectTask && client.Connected);
        }
        catch
        {
            return (port, service, false);
        }
    }
}
