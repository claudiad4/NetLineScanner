using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Domain.Entities;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DeviceInfo> Devices { get; set; } = [];
}
