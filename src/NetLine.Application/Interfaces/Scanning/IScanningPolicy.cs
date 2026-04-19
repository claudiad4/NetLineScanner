using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;

namespace NetLine.Application.Interfaces.Scanning;

/// <summary>
/// Decides which monitoring components are due to run for a given device at a given time,
/// based on per-tier cadences (Light / Medium / Heavy). Tracks last-run timestamps per
/// (device, component) in memory.
/// </summary>
public interface IScanningPolicy
{
    IReadOnlyList<IMonitoringComponent> GetDueComponents(DeviceInfo device, DateTime utcNow);

    void MarkRan(int deviceId, string componentName, DateTime utcNow);

    void MarkAllRan(int deviceId, DateTime utcNow);
}
