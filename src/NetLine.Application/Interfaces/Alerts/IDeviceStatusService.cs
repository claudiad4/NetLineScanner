using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Alerts;

public interface IDeviceStatusService
{
    /// <summary>
    /// Persists metrics, updates device snapshots and creates alerts based on the
    /// scan results. Returns the alerts that were created in this cycle so callers
    /// can fan them out (e.g. via SignalR).
    /// </summary>
    Task<IReadOnlyList<DeviceAlert>> ProcessScanResultsAsync(
        List<DeviceInfo> devices,
        IReadOnlyList<DeviceScanResult> scanResults,
        CancellationToken cancellationToken);
}
