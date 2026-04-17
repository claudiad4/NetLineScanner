using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.Domain.Entities;

/// <summary>
/// Time-series sample produced by a monitoring component.
/// One row per metric per collection cycle; queried for history / charts.
/// </summary>
[Table("device_metrics")]
public class DeviceMetric
{
    [Key]
    public long Id { get; set; }

    public int DeviceInfoId { get; set; }
    public DeviceInfo Device { get; set; } = default!;

    public DateTime Timestamp { get; set; }

    public MonitoringCategory Category { get; set; }

    [Required, MaxLength(64)]
    public string ComponentName { get; set; } = default!;

    [Required, MaxLength(128)]
    public string MetricKey { get; set; } = default!;

    public double? NumericValue { get; set; }

    [MaxLength(512)]
    public string? TextValue { get; set; }

    [MaxLength(32)]
    public string? Unit { get; set; }

    [MaxLength(128)]
    public string? Label { get; set; }
}
