using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.Domain.Entities;

// note for Claudia & Adam: this class is too big, think about splitting it into smaller pieces in the future

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

    public string? SysName { get; set; }
    public string? SysDescr { get; set; }
    public string? SysLocation { get; set; }
    public string? SysContact { get; set; }
    public string? SysUpTime { get; set; }     
    public int? SysInterfacesCount { get; set; }   

    public DateTime LastScanned { get; set; } = DateTime.UtcNow;

    public int OfficeId { get; set; }
    public Office Office { get; set; } = default!;
}