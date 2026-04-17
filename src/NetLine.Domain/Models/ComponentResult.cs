using NetLine.Domain.Entities;

namespace NetLine.Domain.Models;

/// <summary>
/// Outcome of one monitoring component running against one device.
/// </summary>
public sealed class ComponentResult
{
    public required int DeviceId { get; init; }
    public required MonitoringCategory Category { get; init; }
    public required string ComponentName { get; init; }
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ComponentMetric> Metrics { get; init; } = Array.Empty<ComponentMetric>();

    public static ComponentResult Ok(
        int deviceId,
        MonitoringCategory category,
        string componentName,
        IReadOnlyList<ComponentMetric> metrics) =>
        new()
        {
            DeviceId = deviceId,
            Category = category,
            ComponentName = componentName,
            Success = true,
            Metrics = metrics
        };

    public static ComponentResult Fail(
        int deviceId,
        MonitoringCategory category,
        string componentName,
        string errorMessage) =>
        new()
        {
            DeviceId = deviceId,
            Category = category,
            ComponentName = componentName,
            Success = false,
            ErrorMessage = errorMessage
        };
}
