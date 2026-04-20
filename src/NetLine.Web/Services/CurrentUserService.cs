using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using NetLine.Infrastructure;
using NetLine.Infrastructure.Identity;

namespace NetLine.Web.Services;

/// <summary>
/// Helper around the current authenticated user. Provides the user's
/// role and assigned office so pages can branch between Admin and User views.
/// </summary>
public class CurrentUserService(
    AuthenticationStateProvider authStateProvider,
    UserManager<AppUser> userManager)
{
    public const string GlobalAdminRole = IdentitySeeder.GlobalAdminRole;
    public const string OfficeAdminRole = IdentitySeeder.OfficeAdminRole;
    public const string UserRole = IdentitySeeder.UserRole;

    public async Task<CurrentUserInfo?> GetAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var principal = authState.User;
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
            return null;

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return null;

        var isGlobalAdmin = await userManager.IsInRoleAsync(user, GlobalAdminRole);
        var isOfficeAdmin = await userManager.IsInRoleAsync(user, OfficeAdminRole);

        return new CurrentUserInfo(
            user.Id,
            user.Email ?? "",
            user.OfficeId,
            isGlobalAdmin,
            isOfficeAdmin);
    }

    public async Task<bool> IsGlobalAdminAsync()
    {
        var info = await GetAsync();
        return info?.IsGlobalAdmin == true;
    }

    public async Task<bool> IsOfficeAdminAsync()
    {
        var info = await GetAsync();
        return info?.IsOfficeAdmin == true;
    }
}

public record CurrentUserInfo(
    string Id,
    string Email,
    int? OfficeId,
    bool IsGlobalAdmin,
    bool IsOfficeAdmin)
{
    public bool IsAdmin => IsGlobalAdmin || IsOfficeAdmin;
}
