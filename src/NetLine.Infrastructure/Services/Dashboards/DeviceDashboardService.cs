using Microsoft.EntityFrameworkCore;
using NetLine.Application.DTO.Dashboards;
using NetLine.Application.Interfaces.Dashboards;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Services.Dashboards;

public sealed class DeviceDashboardService(IDbContextFactory<AppDbContext> contextFactory)
    : IDeviceDashboardService
{
    private const string PingMetricKey = "PingResponseMs";

    public async Task<DeviceDashboardDto> GetDeviceDashboardAsync(
        int deviceInfoId,
        CancellationToken cancellationToken = default)
    {
        var pingHistoryTask = GetPingLatencyHistoryAsync(deviceInfoId, cancellationToken);
        var readStatsTask = GetAlertReadStatsAsync(deviceInfoId, cancellationToken);
        var alertTypesTask = GetAlertTypesAsync(deviceInfoId, cancellationToken);

        await Task.WhenAll(pingHistoryTask, readStatsTask, alertTypesTask);

        return new DeviceDashboardDto
        {
            DeviceInfoId = deviceInfoId,
            PingLatencyHistory = pingHistoryTask.Result,
            AlertReadStats = readStatsTask.Result ?? new AlertReadStatsDto(),
            AlertTypes = alertTypesTask.Result
        };
    }

    private async Task<List<PingLatencyPointDto>> GetPingLatencyHistoryAsync(
        int deviceInfoId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.DeviceMetrics
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
    }

    private async Task<AlertReadStatsDto?> GetAlertReadStatsAsync(
        int deviceInfoId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.DeviceAlerts
            .AsNoTracking()
            .Where(alert => alert.DeviceInfoId == deviceInfoId)
            .GroupBy(_ => 1)
            .Select(group => new AlertReadStatsDto
            {
                ReadCount = group.Count(alert => alert.IsRead),
                UnreadCount = group.Count(alert => !alert.IsRead)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<AlertTypeCountDto>> GetAlertTypesAsync(
        int deviceInfoId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.DeviceAlerts
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
    }
}
