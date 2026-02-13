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

                        // Skanujemy urządzenie
                        var scan = await snmpService.GetDeviceInfoAsync(device.IpAddress);

                        // LOGIKA HYBRYDOWA:
                        // Jeśli PingResponseTimeMs ma wartość, oznacza to, że urządzenie "żyje" w sieci.
                        if (scan.PingResponseTimeMs.HasValue)
                        {
                            device.Status = "Online";
                            device.PingResponseTimeMs = scan.PingResponseTimeMs;

                            // Jeśli dodatkowo SNMP zadziałało (Success), uzupełniamy szczegóły
                            if (scan.Success)
                            {
                                device.SysName = scan.Name;
                                device.SysDescr = scan.Description;
                                device.SysLocation = scan.Location;
                                device.SysContact = scan.Contact;
                                _logger.LogInformation($"SNMP Success dla {device.IpAddress}");
                            }
                            else
                            {
                                // Urządzenie żyje (Ping OK), ale SNMP milczy (np. telefon)
                                // Nie czyścimy starych danych SNMP, jeśli jakieś były, 
                                // lub dopisujemy info o braku danych.
                                if (string.IsNullOrEmpty(device.SysDescr))
                                {
                                    device.SysDescr = "Dostępne przez Ping (SNMP niedostępne)";
                                }
                                _logger.LogWarning($"Urządzenie {device.IpAddress} Online (tylko Ping).");
                            }
                        }
                        else
                        {
                            // Brak odpowiedzi na Ping = Urządzenie jest Offline
                            device.Status = "Offline";
                            device.PingResponseTimeMs = null;
                            _logger.LogWarning($"Urządzenie {device.IpAddress} jest całkowicie Offline.");
                        }

                        device.LastScanned = DateTime.UtcNow;
                    }

                    // Zapisujemy zmiany w bazie
                    await db.SaveChangesAsync(stoppingToken);

                    // SIGNALR: Powiadamiamy frontend o zmianach
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