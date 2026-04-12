using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/alerts")
            .WithOpenApi();

        group.MapGet("/", async (AppDbContext db, int? deviceId, int limit = 50) =>
        {
            var query = db.DeviceAlerts
                .Include(a => a.Device)
                .OrderByDescending(a => a.Timestamp)
                .AsQueryable();

            if (deviceId.HasValue)
                query = query.Where(a => a.DeviceInfoId == deviceId);

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
    }
}