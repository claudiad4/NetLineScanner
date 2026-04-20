using NetLine.Domain.Entities;

namespace NetLine.Infrastructure.Identity;

public class OfficeAdminAssignment
{
    public string UserId { get; set; } = default!;
    public AppUser User { get; set; } = default!;

    public int OfficeId { get; set; }
    public Office Office { get; set; } = default!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
