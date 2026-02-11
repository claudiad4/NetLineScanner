using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.ApiService.Models;

[Table("deviceinfo")]
public class DeviceInfo
{
    [Key]
    public int Id { get; set; }

    // --- COMPONENT ---
    [Required] public string DeviceType { get; set; } = "Unknown"; // router, serwer, switch
    [Required] public string Status { get; set; } = "Offline";     // Online/Offline/Degraded
    [Required] public string UniqueIdOrName { get; set; } = default!;
    [Required] public string IpAddress { get; set; } = default!;

    // --- CPU ---
    public int? CpuUsagePercent { get; set; }

    // --- HEALTH ---
    public string? HealthEndpointStatus { get; set; } // status /health
    public double AvailabilityPercent { get; set; } = 0;
    public int FailedAttemptsCount { get; set; } = 0;

    // --- MEMORY ---
    public long? MemoryUsedMB { get; set; }
    public long? MemoryFreeMB { get; set; }
    public int? MemoryUsagePercent { get; set; }

    // --- NETWORK ---
    public int? Ttl { get; set; }
    public long? PingResponseTimeMs { get; set; }
    public double? PacketLossPercent { get; set; }
    public string? NetworkInterfacesStatus { get; set; } // up/down
    public double? DownloadSpeed { get; set; }
    public double? UploadSpeed { get; set; }
    public string? OpenPorts { get; set; }
    public string? DnsResponse { get; set; }

    // --- POWER ---
    public string? PowerStatus { get; set; } // on/off

    // --- RAW ---
    public string? RawLogs { get; set; }

    // --- SYSTEM ---
    public string? SystemLoadAverage { get; set; }
    public string? LoggedInUsers { get; set; }
    public string? OsName { get; set; } // Windows/Linux
    public string? SystemVersion { get; set; }

    public DateTime LastScanned { get; set; } = DateTime.UtcNow;
}