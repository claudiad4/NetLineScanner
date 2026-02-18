using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.Domain.Entities;

[Table("deviceinfo")]
public class DeviceInfo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserDefinedName { get; set; } = default!;
    [Required]
    public string DeviceType { get; set; } = "Other";
    [Required]
    public string IpAddress { get; set; } = default!;

    public string Status { get; set; } = "Offline";
    public long? PingResponseTimeMs { get; set; }

    // --- Dane podstawowe SNMP ---
    public string? SysName { get; set; }
    public string? SysDescr { get; set; }
    public string? SysLocation { get; set; }
    public string? SysContact { get; set; }
    public string? SysUpTime { get; set; }      // Czas od ostatniego restartu
    public int? SysInterfacesCount { get; set; }   // Ile kart sieciowych ma urządzenie

    public DateTime LastScanned { get; set; } = DateTime.UtcNow;
}