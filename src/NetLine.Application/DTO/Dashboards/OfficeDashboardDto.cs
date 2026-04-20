namespace NetLine.Application.DTO.Dashboards;

public sealed class DeviceStatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class DailyAlertTrendPointDto
{
    public DateOnly Date { get; init; }
    public int AlertCount { get; init; }
}

public sealed class DeviceAlertCountDto
{
    public string DeviceName { get; init; } = string.Empty;
    public int AlertCount { get; init; }
}

public sealed class OfficeDashboardDto
{
    public int OfficeId { get; init; }
    public IReadOnlyList<DeviceStatusCountDto> HealthOverview { get; init; } = [];
    public IReadOnlyList<DailyAlertTrendPointDto> AlertTrend { get; init; } = [];
    public IReadOnlyList<DeviceAlertCountDto> TopFailingDevices { get; init; } = [];
}
