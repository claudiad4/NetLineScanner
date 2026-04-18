using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Hubs;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Services;

public sealed class DeviceScanWorker : BackgroundService
{
    private readonly IDeviceScanQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DeviceHub> _hub;
    private readonly ILogger<DeviceScanWorker> _logger;

    public DeviceScanWorker(
        IDeviceScanQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<DeviceHub> hub,
        ILogger<DeviceScanWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var deviceId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ScanOneAsync(deviceId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial scan failed for device {DeviceId}", deviceId);
            }
        }
    }

    private async Task ScanOneAsync(int deviceId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scanner = scope.ServiceProvider.GetRequiredService<IDeviceScanner>();

        var device = await db.DevicesInfo.FirstOrDefaultAsync(d => d.Id == deviceId, ct);
        if (device is null)
        {
            _logger.LogWarning("Queued scan for missing device {DeviceId}", deviceId);
            return;
        }

        var scan = await scanner.ScanDeviceAsync(device, ct);
        DeviceManager.ApplyInitialSnapshot(device, scan);
        await db.SaveChangesAsync(ct);

        await _hub.Clients.All.SendAsync("DeviceStatusUpdated", cancellationToken: ct);
    }
}
