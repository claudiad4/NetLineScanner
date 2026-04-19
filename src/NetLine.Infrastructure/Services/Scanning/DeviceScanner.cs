using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Scanning;

/// <summary>
/// Runs registered <see cref="IMonitoringComponent"/>s for each device, but only
/// the ones whose tier (<see cref="ScanFrequency"/>) is due. Tier timestamps on
/// <see cref="DeviceInfo"/> are stamped after a successful run so the caller's
/// tracked DbContext can persist them with SaveChanges.
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
                var componentResults = await RunComponentsAsync(device, ct);
                scanResults.Add(new DeviceScanResult(device.Id, device.IpAddress, componentResults));
            });

        return scanResults.ToList().AsReadOnly();
    }

    public async Task<DeviceScanResult> ScanDeviceAsync(
        DeviceInfo device,
        CancellationToken cancellationToken,
        bool forceAllTiers = false)
    {
        var componentResults = await RunComponentsAsync(device, cancellationToken, forceAllTiers);
        return new DeviceScanResult(device.Id, device.IpAddress, componentResults);
    }

    private async Task<IReadOnlyList<ComponentResult>> RunComponentsAsync(
        DeviceInfo device,
        CancellationToken ct,
        bool forceAllTiers = false)
    {
        var now = DateTime.UtcNow;
        var dueTiers = forceAllTiers
            ? new HashSet<ScanFrequency> { ScanFrequency.Light, ScanFrequency.Medium, ScanFrequency.Heavy }
            : GetDueTiers(device, now);

        if (dueTiers.Count == 0)
        {
            return Array.Empty<ComponentResult>();
        }

        var componentsToRun = forceAllTiers
            ? _components.ToList()
            : _components.Where(c => dueTiers.Contains(c.Frequency)).ToList();
        if (componentsToRun.Count == 0)
        {
            return Array.Empty<ComponentResult>();
        }

        _logger.LogDebug(
            "device {DeviceId} due tiers: {Tiers}, running {ComponentCount} component(s)",
            device.Id, string.Join(",", dueTiers), componentsToRun.Count);

        var tasks = componentsToRun.Select(async c =>
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

        var results = await Task.WhenAll(tasks);

        StampTimestamps(device, dueTiers, now);

        return results;
    }

    private static HashSet<ScanFrequency> GetDueTiers(DeviceInfo device, DateTime now)
    {
        var due = new HashSet<ScanFrequency>();
        if (IsDue(device.LastLightScanAt, ScanIntervals.Light, now)) due.Add(ScanFrequency.Light);
        if (IsDue(device.LastMediumScanAt, ScanIntervals.Medium, now)) due.Add(ScanFrequency.Medium);
        if (IsDue(device.LastHeavyScanAt, ScanIntervals.Heavy, now)) due.Add(ScanFrequency.Heavy);
        return due;
    }

    private static bool IsDue(DateTime? lastScan, TimeSpan interval, DateTime now)
        => lastScan is null || now - lastScan.Value >= interval;

    private static void StampTimestamps(DeviceInfo device, HashSet<ScanFrequency> executed, DateTime now)
    {
        if (executed.Contains(ScanFrequency.Light)) device.LastLightScanAt = now;
        if (executed.Contains(ScanFrequency.Medium)) device.LastMediumScanAt = now;
        if (executed.Contains(ScanFrequency.Heavy)) device.LastHeavyScanAt = now;
        device.LastScanned = now;
    }
}
