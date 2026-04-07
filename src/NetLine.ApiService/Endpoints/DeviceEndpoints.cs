namespace NetLine.ApiService.Endpoints;
using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;

    public static class DeviceEndpoints
    {

    public static void MapDeviceEndpoints(this WebApplication app)
    {
        // IP scanning - teraz zwraca i Ping i SNMP
        app.MapGet("/api/scan/{ip}", async (string ip, ISNMPService snmp, IICMPService ping) =>
        {
            var pingMs = await ping.GetPingResponseTimeAsync(ip);
            var snmpResult = await snmp.GetDeviceInfoAsync(ip);

            // Zwracamy obiekt łączony, żeby frontend dostał komplet informacji
            return Results.Ok(new
            {
                Ip = ip,
                PingMs = pingMs,
                Snmp = snmpResult,
                IsOnline = pingMs.HasValue
            });
        })
        .WithName("ScanDevice");

        // List of devices
        app.MapGet("/api/devices", async (AppDbContext db) =>
        {
            return await db.DevicesInfo.ToListAsync();
        })
        .WithName("GetDevices");

        // Add device - tutaj łączymy wyniki obu usług
        app.MapPost("/api/devices", async (string ip, string userLabel, string type, AppDbContext db, ISNMPService snmp, IICMPService ping) =>
        {
            var existing = await db.DevicesInfo.AnyAsync(d => d.IpAddress == ip);
            if (existing) return Results.BadRequest("This IP is already in the database.");

            // 1. Sprawdzamy Ping
            var pingMs = await ping.GetPingResponseTimeAsync(ip);

            // 2. Sprawdzamy SNMP
            var scan = await snmp.GetDeviceInfoAsync(ip);

            var newDevice = new DeviceInfo
            {
                IpAddress = ip,
                UserDefinedName = userLabel,
                DeviceType = type,
                // Logika statusu
                Status = pingMs.HasValue ? (scan.Success ? "Online" : "Limited") : "Offline",
                PingResponseTimeMs = pingMs,
                // Mapowanie danych SNMP
                SysName = scan.Name ?? "Unknown",
                SysDescr = scan.Description ?? "Unknown",
                SysLocation = scan.Location ?? "Unknown",
                SysContact = scan.Contact ?? "Unknown",
                SysUpTime = scan.UpTime,
                SysInterfacesCount = scan.InterfacesCount,
                LastScanned = DateTime.UtcNow
            };

            db.DevicesInfo.Add(newDevice);
            await db.SaveChangesAsync();

            return Results.Created($"/api/devices/{newDevice.Id}", newDevice);
        })
        .WithName("AddDevice");
    }
}
