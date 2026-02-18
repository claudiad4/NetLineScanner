namespace NetLine.ApiService.Endpoints;
using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Infrastructure.Data;

    public static class DeviceEndpoints
    {
        // To jest metoda rozszerzająca (Extension Method)
        public static void MapDeviceEndpoints(this WebApplication app)
        {
            // EKRAN 1: Skanowanie IP (zanim dodamy do bazy)
            app.MapGet("/api/scan/{ip}", async (string ip, ISNMPService snmp) =>
            {
                var result = await snmp.GetDeviceInfoAsync(ip);

                if (result.Success)
                {
                    return Results.Ok(result);
                }

                return Results.NotFound(new
                {
                    error = "Urządzenie nie odpowiedziało",
                    details = result.ErrorMessage
                });
            })
            .WithName("ScanDevice"); // Dobra praktyka: nazywanie endpointów

            // EKRAN 2: Lista urządzeń zapisanych w bazie
            app.MapGet("/api/devices", async (AppDbContext db) =>
            {
                return await db.DevicesInfo.ToListAsync();
            })
            .WithName("GetDevices");

            // EKRAN 1: Zapisywanie urządzenia do bazy
            app.MapPost("/api/devices", async (string ip, string userLabel, string type, AppDbContext db, ISNMPService snmp) =>
            {
                // 1. Sprawdź, czy IP już istnieje
                var existing = await db.DevicesInfo.AnyAsync(d => d.IpAddress == ip);
                if (existing) return Results.BadRequest("Urządzenie o tym IP już jest w bazie!");

                // 2. Automatyczne skanowanie SNMP przed zapisem
                var scan = await snmp.GetDeviceInfoAsync(ip);

                // 3. Tworzenie obiektu
                var newDevice = new DeviceInfo
                {
                    IpAddress = ip,
                    UserDefinedName = userLabel,
                    DeviceType = type,
                    Status = scan.Success ? "Online" : "Offline",
                    PingResponseTimeMs = scan.PingResponseTimeMs,
                    SysName = scan.Name ?? "Brak nazwy",
                    SysDescr = scan.Description ?? "Brak opisu",
                    SysLocation = scan.Location ?? "Nieznana",
                    SysContact = scan.Contact ?? "Brak kontaktu",
                    LastScanned = DateTime.UtcNow
                };

                db.DevicesInfo.Add(newDevice);
                await db.SaveChangesAsync();

                return Results.Created($"/api/devices/{newDevice.Id}", newDevice);
            })
            .WithName("AddDevice");
        }
    }
