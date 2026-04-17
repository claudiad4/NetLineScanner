namespace NetLine.Domain.Entities;

public enum AlertType
{
    WentOffline,
    CameOnline,
    HighLatency,
    HighPacketLoss,
    HighCpuUsage,
    HighMemoryUsage,
    InterfaceDown,
    ComponentFailure
}

public class DeviceAlert
{
    public int Id { get; set; }
    public int DeviceInfoId { get; set; }
    public DeviceInfo Device { get; set; } = default!;

    public AlertType Type { get; set; }
    public string Message { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; } = false;
}
