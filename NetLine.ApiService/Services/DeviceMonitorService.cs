using NetLine.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace NetLine.ApiService.Services;

public class DeviceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Czas co jaki odświeżamy

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

                    // 1. Pobierz wszystkie urządzenia z bazy
                    var devices = await db.DevicesInfo.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        _logger.LogInformation($"Sprawdzam urządzenie: {device.IpAddress}");

                        // 2. Wykonaj skanowanie tym samym serwisem, którego używaliśmy w Swaggerze
                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);

                        // 3. Zaktualizuj dane w bazie
                        device.Status = scan.Success ? "Online" : "Offline";
                        device.PingResponseTimeMs = scan.PingResponseTimeMs;
                        device.LastScanned = DateTime.UtcNow;

                        // Jeśli SNMP odpowiedziało, możemy też odświeżyć dane systemowe
                        if (scan.Success)
                        {
                            device.SysName = scan.Name;
                            device.SysDescr = scan.Description;
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd w pętli monitorującej: {ex.Message}");
            }

            // Czekaj 30 sekund przed kolejną pętlą
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}