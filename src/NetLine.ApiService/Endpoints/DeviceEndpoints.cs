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

        // Get all alerts
        app.MapGet("/api/alerts", async (AppDbContext db) =>
        {
            return await db.DeviceAlerts
                .Include(a => a.Device)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        })
        .WithName("GetAlerts");

        // Mark alert as read
        app.MapPut("/api/alerts/{id}/read", async (int id, AppDbContext db) =>
        {
            var alert = await db.DeviceAlerts.FindAsync(id);
            if (alert == null) return Results.NotFound();

            alert.IsRead = true;
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("MarkAlertAsRead");

        // Mark all alerts as read
        app.MapPut("/api/alerts/mark-all-as-read", async (AppDbContext db) =>
        {
            var alerts = await db.DeviceAlerts.Where(a => !a.IsRead).ToListAsync();
            foreach (var alert in alerts)
            {
                alert.IsRead = true;
            }
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("MarkAllAlertsAsRead");

        // Clear all alerts
        app.MapDelete("/api/alerts", async (AppDbContext db) =>
        {
            db.DeviceAlerts.RemoveRange(await db.DeviceAlerts.ToListAsync());
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("ClearAllAlerts");
    }
}
