using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace NetLine.Infrastructure.Identity;

public static class IdentitySeeder
{
    public const string GlobalAdminRole = "GlobalAdmin";
    public const string OfficeAdminRole = "OfficeAdmin";
    public const string UserRole = "User";

    public static readonly string[] AllRoles =
    {
        GlobalAdminRole,
        OfficeAdminRole,
        UserRole
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
