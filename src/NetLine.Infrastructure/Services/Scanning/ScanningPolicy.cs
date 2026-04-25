using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Application.Models.Scanning;
using NetLine.Domain.Entities;

namespace NetLine.Infrastructure.Services.Scanning;

/// <summary>
/// In-memory tiered scheduling policy. Each monitoring component is assigned to a tier
/// (Light / Medium / Heavy) by its <see cref="IMonitoringComponent.Name"/>; a component is
/// reported due when the tier interval has elapsed since its last recorded run for the device.
/// </summary>
public class ScanningPolicy : IScanningPolicy, IScanScheduleQuery
{
    private static readonly TimeSpan LightInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MediumInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan HeavyInterval = TimeSpan.FromHours(6);

    private enum ScanTier { Light, Medium, Heavy }

    private static readonly IReadOnlyDictionary<string, ScanTier> TierByName =
        new Dictionary<string, ScanTier>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ping"] = ScanTier.Light,
            ["Dns"] = ScanTier.Medium,
            ["Syslog"] = ScanTier.Medium,
            ["System"] = ScanTier.Medium,
            ["CPU"] = ScanTier.Medium,
            ["Memory"] = ScanTier.Medium,
            ["NetworkInterface"] = ScanTier.Medium,
            ["PortScan"] = ScanTier.Heavy,
        };

    private readonly IReadOnlyList<IMonitoringComponent> _components;
    private readonly ILogger<ScanningPolicy> _logger;
    private readonly ConcurrentDictionary<(int DeviceId, string ComponentName), DateTime> _lastRun = new();
    private readonly ConcurrentDictionary<string, byte> _unknownNamesLogged = new(StringComparer.OrdinalIgnoreCase);

    public ScanningPolicy(IEnumerable<IMonitoringComponent> components, ILogger<ScanningPolicy> logger)
    {
        _components = components.ToList();
        _logger = logger;
    }

    public IReadOnlyList<IMonitoringComponent> GetDueComponents(DeviceInfo device, DateTime utcNow)
    {
        var due = new List<IMonitoringComponent>(_components.Count);
        foreach (var component in _components)
        {
            var interval = IntervalFor(component.Name);
            if (!_lastRun.TryGetValue((device.Id, component.Name), out var lastRun)
                || utcNow - lastRun >= interval)
            {
                due.Add(component);
            }
        }
        return due;
    }

    public void MarkRan(int deviceId, string componentName, DateTime utcNow)
    {
        _lastRun[(deviceId, componentName)] = utcNow;
    }

    public void MarkAllRan(int deviceId, DateTime utcNow)
    {
        foreach (var component in _components)
        {
            _lastRun[(deviceId, component.Name)] = utcNow;
        }
    }

    public IReadOnlyList<DeviceScanSchedule> GetSchedule(DeviceInfo device, DateTime utcNow)
    {
        var schedule = new List<DeviceScanSchedule>(_components.Count);
        foreach (var component in _components)
        {
            var interval = IntervalFor(component.Name);
            var nextScanUtc = _lastRun.TryGetValue((device.Id, component.Name), out var lastRun)
                ? lastRun + interval
                : utcNow;
            var tier = TierLabelFor(component.Name);
            schedule.Add(new DeviceScanSchedule(
                component.Name,
                component.Category,
                tier,
                nextScanUtc,
                interval));
        }
        schedule.Sort((a, b) => a.NextScanUtc.CompareTo(b.NextScanUtc));
        return schedule;
    }

    private static string TierLabelFor(string componentName)
    {
        if (TierByName.TryGetValue(componentName, out var tier))
        {
            return tier switch
            {
                ScanTier.Light => "Light",
                ScanTier.Heavy => "Heavy",
                _ => "Medium",
            };
        }
        return "Medium";
    }

    private TimeSpan IntervalFor(string componentName)
    {
        if (TierByName.TryGetValue(componentName, out var tier))
        {
            return tier switch
            {
                ScanTier.Light => LightInterval,
                ScanTier.Heavy => HeavyInterval,
                _ => MediumInterval,
            };
        }

        if (_unknownNamesLogged.TryAdd(componentName, 0))
        {
            _logger.LogWarning(
                "ScanningPolicy: unknown component '{ComponentName}' — defaulting to Medium tier.",
                componentName);
        }
        return MediumInterval;
    }
}
