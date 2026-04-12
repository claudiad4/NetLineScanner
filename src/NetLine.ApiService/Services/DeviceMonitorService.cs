using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Hubs;
using NetLine.Application.Interfaces.Alerts;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Services;

/// <summary>
/// Background service that orchestrates device monitoring.
/// Responsible for coordinating scanning, status updates, and real-time notifications.
/// Delegates specific concerns to IDeviceScanner and IDeviceStatusService.
/// </summary>
public class DeviceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    public DeviceMonitorService(
        IServiceProvider serviceProvider,
        ILogger<DeviceMonitorService> logger,
        IHubContext<DeviceHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device monitoring started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deviceScanner = scope.ServiceProvider.GetRequiredService<IDeviceScanner>();
                var statusService = scope.ServiceProvider.GetRequiredService<IDeviceStatusService>();

                // 1. Fetch all devices from database
                var devices = await dbContext.DevicesInfo.ToListAsync(stoppingToken);

                if (devices.Count == 0)
                {
                    _logger.LogDebug("No devices to monitor.");
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                // 2. Scan all devices concurrently
                var scanResults = await deviceScanner.ScanDevicesAsync(devices, stoppingToken);

                // 3. Process scan results (update status, generate alerts, sanitize data)
                await statusService.ProcessScanResultsAsync(devices, scanResults, stoppingToken);

                // 4. Notify clients of status updates
                await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", cancellationToken: stoppingToken);

                _logger.LogDebug("Device monitoring cycle completed. Scanned {DeviceCount} devices.", devices.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Device monitoring was cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during device monitoring.");
            }

            // Wait before next scan
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Device monitoring stopped.");
    }
}