using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Domain.Entities;

public enum AlertType
{
    WentOffline, 
    CameOnline,  
    HighLatency  
}

public class DeviceAlert
{
    public int Id { get; set; }
    public int DeviceInfoId { get; set; }
    public DeviceInfo Device { get; set; } 

    public AlertType Type { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; } = false;
}