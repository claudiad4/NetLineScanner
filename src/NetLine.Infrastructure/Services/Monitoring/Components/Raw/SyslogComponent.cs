using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Services.Monitoring.Syslog;

namespace NetLine.Infrastructure.Services.Monitoring.Components.Raw;

/// <summary>
/// Emits raw syslog lines collected for a device by <see cref="SyslogReceiver"/>.
/// </summary>
public sealed class SyslogComponent : IMonitoringComponent
{
    private readonly ISyslogBuffer _buffer;
    private readonly ILogger<SyslogComponent> _logger;

    public SyslogComponent(ISyslogBuffer buffer, ILogger<SyslogComponent> logger)
    {
        _buffer = buffer;
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Raw;
    public string Name => "Syslog";

    public Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var lines = _buffer.Snapshot(device.IpAddress);
        var metrics = new List<ComponentMetric>(lines.Count + 1)
        {
            ComponentMetric.Numeric("raw.log.count", lines.Count, "count", "Collected log lines")
        };

        for (var i = 0; i < lines.Count; i++)
        {
            metrics.Add(ComponentMetric.Text($"raw.log.{i}", lines[i]));
        }

        _logger.LogDebug("Syslog component returned {Count} lines for {Device}", lines.Count, device.IpAddress);
        return Task.FromResult(ComponentResult.Ok(device.Id, Category, Name, metrics));
    }
}
