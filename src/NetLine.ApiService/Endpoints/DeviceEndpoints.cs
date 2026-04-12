using NetLine.Application.Interfaces.Devices;
using NetLine.Domain.Models;

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
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", async (IDeviceManager svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithName("GetDevicesList");

        group.MapPost("/", async (AddDeviceRequest request, IDeviceManager svc) =>
        {
            var device = await svc.AddAsync(request);
            return Results.Created($"/api/devices/{device.Id}", device);
        })
        .WithName("CreateNewDevice");
    }
}