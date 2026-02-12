using NetLine.ApiService.Data;
using NetLine.ApiService.Hubs; // Upewnij się, że ten folder istnieje
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace NetLine.ApiService.Services;

public class DeviceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly IHubContext<DeviceHub> _hubContext; // SignalR Hub
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

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
        _logger.LogInformation("Monitorowanie urządzeń uruchomione.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var snmpService = scope.ServiceProvider.GetRequiredService<SnmpService>();

                    var devices = await db.DevicesInfo.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        _logger.LogInformation($"Sprawdzam: {device.IpAddress}");

                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);

                        if (scan.Success)
                        {
                            device.Status = "Online";
                            device.PingResponseTimeMs = scan.PingResponseTimeMs;
                            device.SysName = scan.Name;
                            device.SysDescr = scan.Description;
                            device.SysLocation = scan.Location;
                            device.SysContact = scan.Contact;
                        }
                        else
                        {
                            // Jeśli skanowanie SNMP się nie udało (Success = false)
                            device.Status = "Offline";

                            // POPRAWKA: Wymuszamy null, aby przy statusie Offline nie wisiał stary Ping
                            device.PingResponseTimeMs = null;
                        }

                        device.LastScanned = DateTime.UtcNow;
                    }

                    // Zapisujemy zmiany w bazie
                    await db.SaveChangesAsync(stoppingToken);

                    // SIGNALR: Powiadamiamy wszystkich podłączonych klientów o aktualizacji
                    await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd w pętli monitorującej: {ex.Message}");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}