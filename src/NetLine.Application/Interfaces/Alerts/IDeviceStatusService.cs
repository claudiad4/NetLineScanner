using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Alerts;

public interface IDeviceStatusService
{
    Task ProcessScanResultsAsync(
        List<DeviceInfo> devices,
        IReadOnlyList<DeviceScanResult> scanResults,
        CancellationToken cancellationToken);
}