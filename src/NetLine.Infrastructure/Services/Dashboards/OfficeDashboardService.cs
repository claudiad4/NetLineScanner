using Microsoft.EntityFrameworkCore;
using NetLine.Application.DTO.Dashboards;
using NetLine.Application.Interfaces.Dashboards;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Services.Dashboards;

public sealed class OfficeDashboardService(AppDbContext dbContext) : IOfficeDashboardService
{
    public async Task<OfficeDashboardDto> GetOfficeDashboardAsync(int officeId, CancellationToken cancellationToken = default)
    {
        var healthOverviewTask = dbContext.DevicesInfo
            .AsNoTracking()
            .Where(device => device.OfficeId == officeId)
            .GroupBy(device => device.Status)
            .OrderBy(group => group.Key)
            .Select(group => new DeviceStatusCountDto
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var alertTrendTask = dbContext.DeviceAlerts
            .AsNoTracking()
            .Where(alert => alert.Device.OfficeId == officeId)
            .GroupBy(alert => alert.Timestamp.Date)
            .OrderBy(group => group.Key)
            .Select(group => new DailyAlertTrendPointDto
            {
                Date = DateOnly.FromDateTime(group.Key),
                AlertCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var topFailingDevicesTask = dbContext.DeviceAlerts
            .AsNoTracking()
            .Where(alert => alert.Device.OfficeId == officeId)
            .GroupBy(alert => new { alert.DeviceInfoId, alert.Device.UserDefinedName })
            .Select(group => new DeviceAlertCountDto
            {
                DeviceName = group.Key.UserDefinedName,
                AlertCount = group.Count()
            })
            .OrderByDescending(item => item.AlertCount)
            .ThenBy(item => item.DeviceName)
            .Take(5)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(healthOverviewTask, alertTrendTask, topFailingDevicesTask);

        return new OfficeDashboardDto
        {
            OfficeId = officeId,
            HealthOverview = healthOverviewTask.Result,
            AlertTrend = alertTrendTask.Result,
            TopFailingDevices = topFailingDevicesTask.Result
        };
    }
}
