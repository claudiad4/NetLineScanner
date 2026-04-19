using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Hubs;
using NetLine.Application.Interfaces.Devices;
using NetLine.Domain.Entities;
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

        group.MapGet("/", async (AppDbContext db, int? officeId) =>
        {
            var query = db.DevicesInfo.AsQueryable();
            if (officeId.HasValue)
                query = query.Where(d => d.OfficeId == officeId);
            return Results.Ok(await query.ToListAsync());
        })
        .WithName("GetDevicesList");

        group.MapGet("/{id}", async (int id, AppDbContext db) =>
        {
            var device = await db.DevicesInfo.FindAsync(id);
            return device is null ? Results.NotFound() : Results.Ok(device);
        })
        .WithName("GetDevice");

        group.MapGet("/{id}/metrics/latest", async (int id, AppDbContext db) =>
        {
            var latest = await db.DeviceMetrics
                .Where(m => m.DeviceInfoId == id)
                .GroupBy(m => m.MetricKey)
                .Select(g => g.OrderByDescending(x => x.Timestamp).First())
                .ToListAsync();
            return Results.Ok(latest);
        })
        .WithName("GetDeviceLatestMetrics");

        group.MapPost("/", async (AddDeviceRequest request, IDeviceManager svc) =>
        {
            var device = await svc.AddAsync(request);
            return Results.Created($"/api/devices/{device.Id}", device);
        })
        .WithName("CreateNewDevice");

        group.MapPost("/{id:int}/scan", async (
                int id,
                IDeviceManager svc,
                IHubContext<DeviceHub> hub,
                CancellationToken ct) =>
        {
            try
            {
                var result = await svc.ScanNowAsync(id, ct);
                await hub.Clients.All.SendAsync("DeviceStatusUpdated", cancellationToken: ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("ScanDeviceNow");

        group.MapDelete("/{id}", async (int id, AppDbContext db) =>
        {
            var device = await db.DevicesInfo.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            db.DevicesInfo.Remove(device);
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("DeleteDevice");

        group.MapPut("/{id}", async (int id, DeviceInfo updated, AppDbContext db) =>
        {
            var device = await db.DevicesInfo.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            device.UserDefinedName = updated.UserDefinedName;
            device.DeviceType = updated.DeviceType;
            await db.SaveChangesAsync();
            return Results.Ok(device);
        })
        .WithName("UpdateDevice");
    }
}