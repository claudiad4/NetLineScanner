namespace NetLine.ApiService.Endpoints;
using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;

    public static class DeviceEndpoints
    {
        
        public static void MapDeviceEndpoints(this WebApplication app)
        {
            // IP scanning
            app.MapGet("/api/scan/{ip}", async (string ip, ISNMPService snmp) =>
            {
                var result = await snmp.GetDeviceInfoAsync(ip);

                if (result.Success)
                {
                    return Results.Ok(result);
                }

                return Results.NotFound(new
                {
                    error = "Device is not responding.",
                    details = result.ErrorMessage
                });
            })
            .WithName("ScanDevice");

        // List of the devices in the database
        app.MapGet("/api/devices", async (AppDbContext db) =>
            {
                return await db.DevicesInfo.ToListAsync();
            })
            .WithName("GetDevices");

            // Add the device to  the database
            app.MapPost("/api/devices", async (string ip, string userLabel, string type, AppDbContext db, ISNMPService snmp) =>
            {
                var existing = await db.DevicesInfo.AnyAsync(d => d.IpAddress == ip);
                if (existing) return Results.BadRequest("This IP is already in the database.");

                // SNMP protocol scanning
                var scan = await snmp.GetDeviceInfoAsync(ip);

                
                var newDevice = new DeviceInfo
                {
                    IpAddress = ip,
                    UserDefinedName = userLabel,
                    DeviceType = type,
                    Status = scan.Success ? "Online" : "Offline",
                    PingResponseTimeMs = scan.PingResponseTimeMs,
                    SysName = scan.Name ?? "Uknown",
                    SysDescr = scan.Description ?? "Uknown",
                    SysLocation = scan.Location ?? "Uknown",
                    SysContact = scan.Contact ?? "Unkown",
                    LastScanned = DateTime.UtcNow
                };

                db.DevicesInfo.Add(newDevice);
                await db.SaveChangesAsync();

                return Results.Created($"/api/devices/{newDevice.Id}", newDevice);
            })
            .WithName("AddDevice");
        }
    }
