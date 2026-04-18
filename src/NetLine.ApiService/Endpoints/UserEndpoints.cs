using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure;
using NetLine.Infrastructure.Data;
using NetLine.Infrastructure.Identity;

namespace NetLine.ApiService.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization(new AuthorizeAttribute { Roles = IdentitySeeder.AdminRole })
            .WithOpenApi();

        group.MapPost("/", async (
            CreateUserRequest request,
            UserManager<AppUser> userManager) =>
        {
            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                OfficeId = request.OfficeId,
                FirstName = request.FirstName ?? "",
                LastName = request.LastName ?? ""
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            var role = string.IsNullOrWhiteSpace(request.Role)
                ? IdentitySeeder.UserRole
                : request.Role;
            await userManager.AddToRoleAsync(user, role);

            return Results.Created($"/api/users/{user.Id}", new
            {
                user.Id,
                user.Email,
                user.OfficeId,
                user.FirstName,
                user.LastName,
                Role = role
            });
        })
        .WithName("CreateUser");

        group.MapGet("/", async (UserManager<AppUser> userManager) =>
        {
            var users = await userManager.Users.ToListAsync();
            var list = new List<object>(users.Count);
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                list.Add(new
                {
                    u.Id,
                    u.Email,
                    u.OfficeId,
                    u.FirstName,
                    u.LastName,
                    Role = roles.FirstOrDefault() ?? ""
                });
            }
            return Results.Ok(list);
        })
        .WithName("GetUsers");

        group.MapPut("/{id}", async (
            string id,
            UpdateUserRequest request,
            UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            user.OfficeId = request.OfficeId;
            if (request.FirstName is not null) user.FirstName = request.FirstName;
            if (request.LastName is not null) user.LastName = request.LastName;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Results.BadRequest(updateResult.Errors);

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, request.Role);
            }

            return Results.Ok(new { user.Id, user.Email, user.OfficeId });
        })
        .WithName("UpdateUser");

        group.MapDelete("/{id}", async (string id, UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.DeleteAsync(user);
            return Results.Ok();
        })
        .WithName("DeleteUser");
    }
}

public record CreateUserRequest(
    string Email,
    string Password,
    int? OfficeId,
    string? Role = null,
    string? FirstName = null,
    string? LastName = null);

public record UpdateUserRequest(
    int? OfficeId,
    string? Role = null,
    string? FirstName = null,
    string? LastName = null);
