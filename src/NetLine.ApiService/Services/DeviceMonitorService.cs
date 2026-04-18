using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Hubs;
using NetLine.Application.Interfaces.Alerts;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Services;

/// <summary>
/// Background service that orchestrates device monitoring.
/// Delegates scanning to <see cref="IDeviceScanner"/> and status/alert handling
/// to <see cref="IDeviceStatusService"/>, then fans out updates over SignalR.
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

                var devices = await dbContext.DevicesInfo.ToListAsync(stoppingToken);

                if (devices.Count == 0)
                {
                    _logger.LogDebug("No devices to monitor.");
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                var scanResults = await deviceScanner.ScanDevicesAsync(devices, stoppingToken);
                var newAlerts = await statusService.ProcessScanResultsAsync(devices, scanResults, stoppingToken);

                await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", cancellationToken: stoppingToken);

                var devicesById = devices.ToDictionary(d => d.Id);
                foreach (var alert in newAlerts)
                {
                    devicesById.TryGetValue(alert.DeviceInfoId, out var device);
                    var payload = new
                    {
                        alert.Id,
                        alert.DeviceInfoId,
                        Type = (int)alert.Type,
                        alert.Message,
                        alert.Timestamp,
                        alert.IsRead,
                        Device = device is null ? null : new
                        {
                            device.Id,
                            device.UserDefinedName,
                            device.IpAddress,
                            device.OfficeId
                        }
                    };
                    await _hubContext.Clients.All.SendAsync("AlertCreated", payload, stoppingToken);
                }

                _logger.LogDebug("Monitoring cycle done. Devices={DeviceCount} NewAlerts={AlertCount}",
                    devices.Count, newAlerts.Count);
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

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Device monitoring stopped.");
    }
}
