using Microsoft.EntityFrameworkCore;
using NetLine.Application.DTO.Dashboards;
using NetLine.Application.Interfaces.Dashboards;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Services.Dashboards;

public sealed class DeviceDashboardService(AppDbContext dbContext) : IDeviceDashboardService
{
    private const string PingMetricKey = "PingResponseMs";

    public async Task<DeviceDashboardDto> GetDeviceDashboardAsync(int deviceInfoId, CancellationToken cancellationToken = default)
    {
        var pingHistoryTask = dbContext.DeviceMetrics
            .AsNoTracking()
            .Where(metric => metric.DeviceInfoId == deviceInfoId
                             && metric.MetricKey == PingMetricKey
                             && metric.NumericValue.HasValue)
            .OrderBy(metric => metric.Timestamp)
            .Select(metric => new PingLatencyPointDto
            {
                Timestamp = metric.Timestamp,
                ValueMs = metric.NumericValue!.Value
            })
            .ToListAsync(cancellationToken);

        var readStatsTask = dbContext.DeviceAlerts
            .AsNoTracking()
            .Where(alert => alert.DeviceInfoId == deviceInfoId)
            .GroupBy(_ => 1)
            .Select(group => new AlertReadStatsDto
            {
                ReadCount = group.Count(alert => alert.IsRead),
                UnreadCount = group.Count(alert => !alert.IsRead)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var alertTypesTask = dbContext.DeviceAlerts
            .AsNoTracking()
            .Where(alert => alert.DeviceInfoId == deviceInfoId)
            .GroupBy(alert => alert.Type)
            .OrderByDescending(group => group.Count())
            .Select(group => new AlertTypeCountDto
            {
                AlertType = group.Key.ToString(),
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        await Task.WhenAll(pingHistoryTask, readStatsTask, alertTypesTask);

        return new DeviceDashboardDto
        {
            DeviceInfoId = deviceInfoId,
            PingLatencyHistory = pingHistoryTask.Result,
            AlertReadStats = readStatsTask.Result ?? new AlertReadStatsDto(),
            AlertTypes = alertTypesTask.Result
        };
    }
}
