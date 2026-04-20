using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces.Identity;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public bool IsGlobalAdmin =>
        Principal?.IsInRole(IdentitySeeder.GlobalAdminRole) == true;

    public bool IsOfficeAdmin =>
        Principal?.IsInRole(IdentitySeeder.OfficeAdminRole) == true;

    public bool IsUser =>
        Principal?.IsInRole(IdentitySeeder.UserRole) == true;

    public int? OfficeId
    {
        get
        {
            var id = UserId;
            if (id is null) return null;

            // Single round-trip; EF caches per-scope so this is cheap.
            return _db.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => u.OfficeId)
                .FirstOrDefault();
        }
    }

    public async Task<IReadOnlyCollection<int>> GetManagedOfficeIdsAsync(CancellationToken ct = default)
    {
        var id = UserId;
        if (id is null) return Array.Empty<int>();

        return await _db.OfficeAdminAssignments
            .AsNoTracking()
            .Where(a => a.UserId == id)
            .Select(a => a.OfficeId)
            .ToListAsync(ct);
    }
}
