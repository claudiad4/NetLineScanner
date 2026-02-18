using NetLine.ApiService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
// --- NOWE USINGI ---
using NetLine.Domain.Models;           // Widzi modele
using NetLine.Application.Interfaces;  // Widzi interfejs ISnmpService
using NetLine.Infrastructure.Data;     // Widzi AppDbContext

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

                    // --- ZMIANA: Pobieramy INTERFEJS, nie konkretną klasę ---
                    var snmpService = scope.ServiceProvider.GetRequiredService<ISNMPService>();

                    var devices = await db.DevicesInfo.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        // Tutaj wywołujemy metodę z interfejsu - kod wygląda tak samo, 
                        // ale pod spodem działa nowa architektura
                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);

                        device.PingResponseTimeMs = scan.PingResponseTimeMs;
                        device.LastScanned = DateTime.UtcNow;

                        if (scan.Success)
                        {
                            // Aktualizacja danych SNMP
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
                            // Logika offline/limited
                            device.Status = scan.PingResponseTimeMs.HasValue ? "Limited" : "Offline";

                            // Specjalny wyjątek dla 1ms (localhost/test)
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
                _logger.LogError($"Błąd monitoringu: {ex.Message}");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}