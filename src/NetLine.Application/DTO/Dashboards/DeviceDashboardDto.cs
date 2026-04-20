namespace NetLine.Application.DTO.Dashboards;

public sealed class PingLatencyPointDto
{
    public DateTime Timestamp { get; init; }
    public double ValueMs { get; init; }
}

public sealed class AlertReadStatsDto
{
    public int ReadCount { get; init; }
    public int UnreadCount { get; init; }
}

public sealed class AlertTypeCountDto
{
    public string AlertType { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class DeviceDashboardDto
{
    public int DeviceInfoId { get; init; }
    public IReadOnlyList<PingLatencyPointDto> PingLatencyHistory { get; init; } = [];
    public AlertReadStatsDto AlertReadStats { get; init; } = new();
    public IReadOnlyList<AlertTypeCountDto> AlertTypes { get; init; } = [];
}
