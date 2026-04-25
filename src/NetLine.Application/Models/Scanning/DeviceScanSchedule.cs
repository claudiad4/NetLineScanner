using NetLine.Domain.Entities;

namespace NetLine.Application.Models.Scanning;

public record DeviceScanSchedule(
    string ComponentName,
    MonitoringCategory Category,
    string Tier,
    DateTime NextScanUtc,
    TimeSpan Interval);
