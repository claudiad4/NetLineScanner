using Microsoft.AspNetCore.Identity;
using NetLine.Infrastructure;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
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
                OfficeId = request.OfficeId
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            await userManager.AddToRoleAsync(user, "User");

            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email, user.OfficeId });
        })
        .WithName("CreateUser");

        group.MapGet("/", async (UserManager<AppUser> userManager) =>
        {
            var users = userManager.Users.ToList();
            return Results.Ok(users.Select(u => new { u.Id, u.Email, u.OfficeId }));
        })
        .WithName("GetUsers");

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

public record CreateUserRequest(string Email, string Password, int? OfficeId);