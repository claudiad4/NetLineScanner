using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace NetLine.ApiService.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        // IP scanning endpoint - public
        app.MapGet("/api/scan/{ip}", ScanDevice)
            .WithName("ScanDevice")
            .WithOpenApi();

        // Usuwamy .WithName("Devices") z grupy, zostawiamy tylko trasę
        var group = app.MapGroup("/api/devices")
            .WithOpenApi()
            .RequireAuthorization();

        // Nadajemy unikalne nazwy konkretnym metodom
        group.MapGet("/", GetDevices)
            .WithName("GetDevicesList"); // Unikalna nazwa dla GET

        group.MapPost("/", AddDevice)
            .WithName("CreateNewDevice"); // Unikalna nazwa dla POST
    }

    private static async Task<IResult> ScanDevice(
        string ip,
        ISNMPService snmp,
        IICMPService ping)
    {
        try
        {
            var pingMs = await ping.GetPingResponseTimeAsync(ip);
            var snmpResult = await snmp.GetDeviceInfoAsync(ip);

            return Results.Ok(new
            {
                Ip = ip,
                PingMs = pingMs,
                Snmp = snmpResult,
                IsOnline = pingMs.HasValue
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDevices(AppDbContext db)
    {
        try
        {
            var devices = await db.DevicesInfo.ToListAsync();
            return Results.Ok(devices);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving devices: {ex.Message}");
        }
    }

    private static async Task<IResult> AddDevice(
        [FromQuery] string ip,
        [FromQuery] string userLabel,
        [FromQuery] string type,
        AppDbContext db,
        ISNMPService snmp,
        IICMPService ping)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(userLabel))
                return Results.BadRequest("IP and device label are required.");

            // Check if IP already exists
            var existing = await db.DevicesInfo.AnyAsync(d => d.IpAddress == ip);
            if (existing)
                return Results.BadRequest("This IP is already in the database.");

            // Scan the device
            var pingMs = await ping.GetPingResponseTimeAsync(ip);
            var snmpScan = await snmp.GetDeviceInfoAsync(ip);

            // Determine status
            string status = pingMs.HasValue 
                ? (snmpScan.Success ? "Online" : "Limited") 
                : "Offline";

            var newDevice = new DeviceInfo
            {
                IpAddress = ip,
                UserDefinedName = userLabel,
                DeviceType = type ?? "Other",
                Status = status,
                PingResponseTimeMs = pingMs,
                SysName = snmpScan.Name,
                SysDescr = snmpScan.Description,
                SysLocation = snmpScan.Location,
                SysContact = snmpScan.Contact,
                SysUpTime = snmpScan.UpTime,
                SysInterfacesCount = snmpScan.InterfacesCount,
                LastScanned = DateTime.UtcNow
            };

            db.DevicesInfo.Add(newDevice);
            await db.SaveChangesAsync();

            return Results.Created($"/api/devices/{newDevice.Id}", newDevice);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error adding device: {ex.Message}");
        }
    }
}
