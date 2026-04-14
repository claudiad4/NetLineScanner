using Microsoft.EntityFrameworkCore;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Endpoints;

public static class OfficeEndpoints
{
    public static void MapOfficeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/offices")
            .WithOpenApi();

        group.MapGet("/", async (AppDbContext db) =>
        {
            var offices = await db.Offices.ToListAsync();
            return Results.Ok(offices);
        })
        .WithName("GetOffices");

        group.MapGet("/{id}", async (int id, AppDbContext db) =>
        {
            var office = await db.Offices.FindAsync(id);
            return office is null ? Results.NotFound() : Results.Ok(office);
        })
        .WithName("GetOffice");

        group.MapPost("/", async (Office office, AppDbContext db) =>
        {
            db.Offices.Add(office);
            await db.SaveChangesAsync();
            return Results.Created($"/api/offices/{office.Id}", office);
        })
        .WithName("CreateOffice");

        group.MapPut("/{id}", async (int id, Office updated, AppDbContext db) =>
        {
            var office = await db.Offices.FindAsync(id);
            if (office is null)
                return Results.NotFound();

            office.Name = updated.Name;
            office.Location = updated.Location;
            await db.SaveChangesAsync();
            return Results.Ok(office);
        })
        .WithName("UpdateOffice");

        group.MapDelete("/{id}", async (int id, AppDbContext db) =>
        {
            var office = await db.Offices.FindAsync(id);
            if (office is null)
                return Results.NotFound();

            db.Offices.Remove(office);
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("DeleteOffice");
    }
}
