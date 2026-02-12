using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Hubs;
using NetLine.ApiService.Models;
using NetLine.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REJESTRACJA US£UG (Dependency Injection) ---
builder.AddServiceDefaults();

// Rejestracja bazy danych Postgres
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

// Rejestracja Twojego nowego serwisu SNMP
builder.Services.AddSingleton<SnmpService>();
builder.Services.AddHostedService<DeviceMonitorService>();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync(); // To stworzy tabelê deviceinfo automatycznie
}

// --- 2. KONFIGURACJA ŒRODOWISKA ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

// --- 3. ENDPOINTY (Logika Twoich ekranów) ---
app.MapHub<DeviceHub>("/devicehub");

// Testowy endpoint powitalny
app.MapGet("/", () => "NetLine API - System Monitorowania SNMP gotowy.");

// EKRAN 1: Skanowanie IP (zanim dodamy do bazy)
// U¿ycie: GET /api/scan/127.0.0.1
app.MapGet("/api/scan/{ip}", async (string ip, SnmpService snmp) =>
{
    var result = await snmp.GetDeviceInfoAsync(ip);

    if (result.Success)
    {
        return Results.Ok(result);
    }

    return Results.NotFound(new
    {
        error = "Urz¹dzenie nie odpowiedzia³o",
        details = result.ErrorMessage
    });
});

// EKRAN 2: Lista urz¹dzeñ zapisanych w bazie
app.MapGet("/api/devices", async (AppDbContext db) =>
{
    return await db.DevicesInfo.ToListAsync();
});

// EKRAN 1: Zapisywanie urz¹dzenia do bazy (KROK NA PRZYSZ£OŒÆ)
app.MapPost("/api/devices", async (string ip, string userLabel, string type, AppDbContext db, SnmpService snmp) =>
{
    // 1. SprawdŸ, czy IP ju¿ istnieje
    var existing = await db.DevicesInfo.AnyAsync(d => d.IpAddress == ip);
    if (existing) return Results.BadRequest("Urz¹dzenie o tym IP ju¿ jest w bazie!");

    // 2. Automatyczne skanowanie SNMP przed zapisem
    var scan = await snmp.GetDeviceInfoAsync(ip);

    // 3. Tworzenie obiektu na podstawie skanu i danych od u¿ytkownika
    var newDevice = new DeviceInfo
    {
        IpAddress = ip,
        UserDefinedName = userLabel,
        DeviceType = type,
        Status = scan.Success ? "Online" : "Offline",
        PingResponseTimeMs = scan.PingResponseTimeMs,

        // Dane pobrane automatycznie z SNMP
        SysName = scan.Name ?? "Brak nazwy",
        SysDescr = scan.Description ?? "Brak opisu",
        SysLocation = scan.Location ?? "Nieznana",
        SysContact = scan.Contact ?? "Brak kontaktu",

        LastScanned = DateTime.UtcNow
    };

    db.DevicesInfo.Add(newDevice);
    await db.SaveChangesAsync();

    return Results.Created($"/api/devices/{newDevice.Id}", newDevice);
});

app.Run();