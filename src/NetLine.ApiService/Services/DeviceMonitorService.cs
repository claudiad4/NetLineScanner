using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Hubs;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;
using NetLine.Infrastructure.Services;

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
        _logger.LogInformation("Device monitoring started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var snmpService = scope.ServiceProvider.GetRequiredService<ISNMPService>();
                var pingService = scope.ServiceProvider.GetRequiredService<IICMPService>();

                // 1. Pobieramy listê urz¹dzeñ
                var devices = await db.DevicesInfo.ToListAsync(stoppingToken);

                // 2. RÓWNOLEG£E SKANOWANIE SIECI
                // Zamiast czekaæ na ka¿de urz¹dzenie po kolei, odpalamy Ping i SNMP dla wszystkich naraz!
                var scanTasks = devices.Select(async device =>
                {
                    var pingTask = pingService.GetPingResponseTimeAsync(device.IpAddress);
                    var snmpTask = snmpService.GetDeviceInfoAsync(device.IpAddress);

                    await Task.WhenAll(pingTask, snmpTask); // Czekamy a¿ oba skany dla TEGO urz¹dzenia siê skoñcz¹

                    return new
                    {
                        Device = device,
                        PingTime = await pingTask,
                        SnmpScan = await snmpTask
                    };
                }).ToList();

                // Czekamy a¿ WSZYSTKIE urz¹dzenia zostan¹ przeskanowane
                var scanResults = await Task.WhenAll(scanTasks);

                // 3. SEKWENCYJNA AKTUALIZACJA BAZY DANYCH (Bezpieczne dla Entity Framework)
                foreach (var result in scanResults)
                {
                    var device = result.Device;
                    var pingTime = result.PingTime;
                    var snmpScan = result.SnmpScan;

                    device.LastScanned = DateTime.UtcNow;

                    device.PingResponseTimeMs = pingTime;

                    // 1. ZAPAMIÊTUJEMY STARY STATUS
                    string oldStatus = device.Status;

                    // 2. USTALAMY NOWY STATUS
                    string newStatus = (pingTime.HasValue || snmpScan.Success) ? "Online" : "Offline";

                    // 3. WYKRYWANIE ZMIANY (Edge Detection)
                    if (oldStatus != newStatus)
                    {
                        var alert = new DeviceAlert
                        {
                            DeviceInfoId = device.Id,
                            Timestamp = DateTime.UtcNow,
                            Type = newStatus == "Offline" ? AlertType.WentOffline : AlertType.CameOnline,
                            Message = newStatus == "Offline"
                                ? $"Urz¹dzenie {device.UserDefinedName} przesta³o odpowiadaæ."
                                : $"Urz¹dzenie {device.UserDefinedName} wróci³o do sieci."
                        };
                        db.DeviceAlerts.Add(alert);
                        _logger.LogInformation("ALERT: {Msg}", alert.Message);
                    }

                    // 4. AKTUALIZUJEMY STATUS URZ¥DZENIA
                    device.Status = newStatus;

                    // Logika dla Statusu Online
                    if (newStatus == "Online")
                    {
                        if (snmpScan.Success)
                        {
                            // SNMP odpowiedzia³o - aktualizujemy dane
                            device.SysName = snmpScan.Name;
                            device.SysDescr = snmpScan.Description;
                            device.SysLocation = snmpScan.Location;
                            device.SysContact = snmpScan.Contact;
                            device.SysUpTime = snmpScan.UpTime;
                            device.SysInterfacesCount = snmpScan.InterfacesCount;
                        }
                        else
                        {
                            // NAPRAWA B£ÊDU: Ping dzia³a, ale SNMP pad³o. 
                            // Czyœcimy stare dane SNMP, ¿eby odznaka w UI zmieni³a siê na czerwon¹.
                            device.SysUpTime = null;
                            device.SysInterfacesCount = null;
                            device.SysName = null;
                            device.SysDescr = null;
                            device.SysLocation = null;
                            device.SysContact = null;
                        }
                    }
                    else
                    {
                        // Absolutna cisza - sprzêt ca³kowicie martwy
                        device.PingResponseTimeMs = null;
                        device.SysUpTime = null;
                        device.SysInterfacesCount = null;
                        device.SysName = null;
                        device.SysDescr = null;
                        device.SysLocation = null;
                        device.SysContact = null;
                    }
                }

                // 4. Zapisujemy zaktualizowane dane i wysy³amy powiadomienie
                await db.SaveChangesAsync(stoppingToken);
                await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during device monitoring.");
            }

            // Odpoczynek przed kolejnym skanem
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}