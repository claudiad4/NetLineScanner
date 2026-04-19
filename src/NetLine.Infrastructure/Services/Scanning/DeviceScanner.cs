using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Scanning;

/// <summary>
/// Runs every registered <see cref="IMonitoringComponent"/> in parallel for each
/// device and returns the collected metrics as a flat <see cref="DeviceScanResult"/>.
/// </summary>
public class DeviceScanner : IDeviceScanner
{
    private readonly IReadOnlyList<IMonitoringComponent> _components;
    private readonly ILogger<DeviceScanner> _logger;

    public DeviceScanner(IEnumerable<IMonitoringComponent> components, ILogger<DeviceScanner> logger)
    {
        _components = components.ToList();
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken)
    {
        var scanResults = new ConcurrentBag<DeviceScanResult>();

        await Parallel.ForEachAsync(
            devices,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (device, ct) =>
            {
                var componentResults = await RunComponentsAsync(device, _components, ct);
                scanResults.Add(new DeviceScanResult(device.Id, device.IpAddress, componentResults));
            });

        return scanResults.ToList().AsReadOnly();
    }

    public async Task<DeviceScanResult> ScanDeviceAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var componentResults = await RunComponentsAsync(device, _components, cancellationToken);
        return new DeviceScanResult(device.Id, device.IpAddress, componentResults);
    }

    public async Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<(DeviceInfo Device, IReadOnlyList<IMonitoringComponent> Components)> workItems,
        CancellationToken cancellationToken)
    {
        var scanResults = new ConcurrentBag<DeviceScanResult>();

        await Parallel.ForEachAsync(
            workItems,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (item, ct) =>
            {
                var componentResults = await RunComponentsAsync(item.Device, item.Components, ct);
                scanResults.Add(new DeviceScanResult(item.Device.Id, item.Device.IpAddress, componentResults));
            });

        return scanResults.ToList().AsReadOnly();
    }

    private async Task<IReadOnlyList<ComponentResult>> RunComponentsAsync(
        DeviceInfo device,
        IReadOnlyList<IMonitoringComponent> components,
        CancellationToken ct)
    {
        var tasks = components.Select(async c =>
        {
            try
            {
                return await c.CollectAsync(device, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Component {Component} failed for device {DeviceId}", c.Name, device.Id);
                return ComponentResult.Fail(device.Id, c.Category, c.Name, ex.Message);
            }
        });

        return await Task.WhenAll(tasks);
    }
}
