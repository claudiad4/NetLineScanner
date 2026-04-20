using Microsoft.AspNetCore.Identity;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Identity;

namespace NetLine.Infrastructure;

public class AppUser : IdentityUser
{
    // Used by the "User" role — exactly one office per regular user.
    public int? OfficeId { get; set; }
    public Office? Office { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;

    // Used by the "OfficeAdmin" role — many offices per admin.
    // GlobalAdmin ignores both: they see everything without assignments.
    public ICollection<OfficeAdminAssignment> ManagedOffices { get; set; }
        = new List<OfficeAdminAssignment>();
}
