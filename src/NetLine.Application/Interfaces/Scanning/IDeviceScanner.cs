using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Scanning;

/// <summary>
/// Responsible for parallel asynchronous scanning of devices.
/// Performs Ping (ICMP) and SNMP queries concurrently for each device.
/// </summary>
public interface IDeviceScanner
{
    /// <summary>
    /// Scans multiple devices concurrently, collecting ping and SNMP data.
    /// </summary>
    /// <param name="devices">List of devices to scan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scan results for all devices</returns>
    Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken);
}