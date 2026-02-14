using NetLine.ApiService.Data;
using NetLine.ApiService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace NetLine.ApiService.Services;

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
        _logger.LogInformation("Monitorowanie urządzeń uruchomione (Logika danych historycznych).");

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
                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);
                        device.PingResponseTimeMs = scan.PingResponseTimeMs;
                        device.LastScanned = DateTime.UtcNow;

                        if (scan.Success)
                        {
                            // Aktualizacja wszystkich danych
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
                            // Jeśli SNMP zawiedzie, ustawiamy status na podstawie Pinga
                            device.Status = scan.PingResponseTimeMs.HasValue ? "Limited" : "Offline";

                            // ==================================================================
                            // START WYJĄTKU DO TESTÓW:
                            // Jeżeli SNMP nie działa, ale Ping wynosi dokładnie 1 ms -> Offline
                            // DODATKOWO: Ukrywamy Ping, aby labelka ICMP zgasła na froncie
                            // ==================================================================
                            if (scan.PingResponseTimeMs.HasValue && scan.PingResponseTimeMs.Value == 1)
                            {
                                device.Status = "Offline";
                                device.PingResponseTimeMs = null; // To "zgasi" labelkę ICMP na zielono
                            }
                            // ==================================================================
                            // KONIEC WYJĄTKU

                            // KLUCZOWE: Czyścimy tylko UpTime. 
                            // Dzięki temu wiemy na froncie, że SNMP nie odpowiedziało, 
                            // ale SysName, Location i Contact zostają w bazie jako dane historyczne.
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
                _logger.LogError($"Błąd monitoringu: {ex.Message}");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}