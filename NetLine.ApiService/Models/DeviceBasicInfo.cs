using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetLine.ApiService.Models;

[Table("devicesbasicinfo")]
public class DeviceBasicInfo
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = default!;  // router/serwer/switch

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = default!;      // np. Online/Offline/Degraded

    [Required]
    [MaxLength(100)]
    public string UniqueIdOrName { get; set; } = default!; // np. "RTR-01" albo "CoreSwitch"
}
