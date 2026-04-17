using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Monitoring;

/// <summary>
/// Strategy contract for a single monitoring concern (CPU, memory, ping, DNS, ...).
/// Each implementation is independently registered in DI and invoked by the scanner.
/// </summary>
public interface IMonitoringComponent
{
    MonitoringCategory Category { get; }

    string Name { get; }

    Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken);
}
