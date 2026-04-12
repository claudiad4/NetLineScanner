using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces.Devices;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/scan/{ip}", async (string ip, IDeviceManager svc) =>
            Results.Ok(await svc.ScanAsync(ip)))
            .WithName("ScanDevice")
            .WithOpenApi();

        var group = app.MapGroup("/api/devices")
            .WithOpenApi();
            //.RequireAuthorization();

        group.MapGet("/", async (IDeviceManager svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithName("GetDevicesList");

        group.MapPost("/", async (AddDeviceRequest request, IDeviceManager svc) =>
        {
            var device = await svc.AddAsync(request);
            return Results.Created($"/api/devices/{device.Id}", device);
        })
        .WithName("CreateNewDevice");

        group.MapGet("/", async (AppDbContext db, int? officeId) =>
        {
            var query = db.DevicesInfo.AsQueryable();
            if (officeId.HasValue)
                query = query.Where(d => d.OfficeId == officeId);
            return Results.Ok(await query.ToListAsync());
        })
        .WithName("GetDevicesList");
    }
}