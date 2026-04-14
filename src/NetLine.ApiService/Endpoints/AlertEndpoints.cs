using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/alerts")
            .WithOpenApi();

        group.MapGet("/", async (AppDbContext db, int? deviceId, int? officeId, int limit = 50) =>
        {
            var query = db.DeviceAlerts
                .Include(a => a.Device)
                .OrderByDescending(a => a.Timestamp)
                .AsQueryable();

            if (deviceId.HasValue)
                query = query.Where(a => a.DeviceInfoId == deviceId);

            if (officeId.HasValue)
                query = query.Where(a => a.Device.OfficeId == officeId);

            var alerts = await query.Take(limit).ToListAsync();
            return Results.Ok(alerts);
        })
        .WithName("GetAlerts");

        group.MapPut("/{id}/read", async (int id, AppDbContext db) =>
        {
            var alert = await db.DeviceAlerts.FindAsync(id);
            if (alert is null)
                return Results.NotFound();

            alert.IsRead = true;
            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("MarkAlertAsRead");

        group.MapPut("/mark-all-as-read", async (AppDbContext db, int? officeId) =>
        {
            var query = db.DeviceAlerts.Where(a => !a.IsRead);
            if (officeId.HasValue)
                query = query.Where(a => a.Device.OfficeId == officeId);

            var unread = await query.ToListAsync();
            foreach (var alert in unread)
                alert.IsRead = true;

            await db.SaveChangesAsync();
            return Results.Ok(new { count = unread.Count });
        })
        .WithName("MarkAllAlertsAsRead");

        group.MapDelete("/", async (AppDbContext db, int? officeId) =>
        {
            var query = db.DeviceAlerts.AsQueryable();
            if (officeId.HasValue)
                query = query.Where(a => a.Device.OfficeId == officeId);

            var toDelete = await query.ToListAsync();
            db.DeviceAlerts.RemoveRange(toDelete);
            await db.SaveChangesAsync();
            return Results.Ok(new { count = toDelete.Count });
        })
        .WithName("ClearAllAlerts");
    }
}
