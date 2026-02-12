using NetLine.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace NetLine.ApiService.Services;

public class DeviceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public DeviceMonitorService(IServiceProvider serviceProvider, ILogger<DeviceMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

                        // KLUCZOWA LOGIKA STATUSU
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
                            device.PingResponseTimeMs = scan.PingResponseTimeMs; // Może być null jeśli ping też padł
                        }

                        device.LastScanned = DateTime.UtcNow;
                    }

                    // Zapisujemy wszystkie zmiany w statusach do bazy danych
                    await db.SaveChangesAsync(stoppingToken);
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