using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.ApiService.Models;

[Table("deviceinfo")]
public class DeviceInfo
{
    [Key]
    public int Id { get; set; }

    // Data from user input during adding a device manually
    [Required]
    public string UserDefinedName { get; set; } = default!; // Twoja własna nazwa (np. "Serwer w Piwnicy")

    [Required]
    public string DeviceType { get; set; } = "Other"; // Host, Server, Switch, Router, Firewall, AP, Other

    // Data from the network
    [Required]
    public string IpAddress { get; set; } = default!;

    public string Status { get; set; } = "Offline"; // Online / Offline
    public long? PingResponseTimeMs { get; set; }

    // --- Dane from SNMP (OID) ---
    public string? SysName { get; set; }      // sysName.0
    public string? SysDescr { get; set; }     // sysDescr.0
    public string? SysLocation { get; set; }  // sysLocation.0
    public string? SysContact { get; set; }   // sysContact.0

    // --- Metadata ---
    public DateTime LastScanned { get; set; } = DateTime.UtcNow;
}