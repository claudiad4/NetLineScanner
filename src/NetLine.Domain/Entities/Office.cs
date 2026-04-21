namespace NetLine.Domain.Entities;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? AdminId { get; set; }

    public ICollection<DeviceInfo> Devices { get; set; } = [];
}
