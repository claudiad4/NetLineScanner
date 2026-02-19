using NetLine.ApiService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.Domain.Models;          
using NetLine.Application.Interfaces;  
using NetLine.Infrastructure.Data;     

namespace NetLine.ApiService.Services;

//this is our background service that will be running in the background and will be checking the status of the devices every 5 seconds and updating the database and sending the updates to the clients via SignalR
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
        _logger.LogInformation("Device monitoring in progress..");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var snmpService = scope.ServiceProvider.GetRequiredService<ISNMPService>();

                    var devices = await db.DevicesInfo.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);

                        device.PingResponseTimeMs = scan.PingResponseTimeMs;
                        device.LastScanned = DateTime.UtcNow;

                        if (scan.Success)
                        {
                            device.Status = "Online";
                            device.SysName = scan.Name;
                            device.SysDescr = scan.Description;
                            device.SysLocation = scan.Location;
                            device.SysContact = scan.Contact;
                            device.SysUpTime = scan.UpTime;
                            device.SysInterfacesCount = scan.InterfacesCount;
                        }
                        else
                        {
                            
                            device.Status = scan.PingResponseTimeMs.HasValue ? "Limited" : "Offline";

                            // Exception for the testing environment where ping response time is always 1ms, but device is actually offline
                            if (scan.PingResponseTimeMs.HasValue && scan.PingResponseTimeMs.Value == 1)
                            {
                                device.Status = "Offline";
                                device.PingResponseTimeMs = null;
                            }

                            device.SysUpTime = null;

                            if (!scan.PingResponseTimeMs.HasValue)
                            {
                                device.PingResponseTimeMs = null;
                            }
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR: {ex.Message}");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}