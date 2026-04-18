using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Scanning;

/// <summary>
/// Runs every registered monitoring component in parallel for one or many devices.
/// </summary>
public interface IDeviceScanner
{
    Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken);

    Task<DeviceScanResult> ScanDeviceAsync(
        DeviceInfo device,
        CancellationToken cancellationToken);
}
