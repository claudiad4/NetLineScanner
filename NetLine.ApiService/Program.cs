using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using NetLine.ApiService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REJESTRACJA US£UG (Dependency Injection) ---
builder.AddServiceDefaults();

// Rejestracja bazy danych Postgres
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

// Rejestracja Twojego nowego serwisu SNMP
builder.Services.AddSingleton<SnmpService>();
builder.Services.AddHostedService<DeviceMonitorService>();

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
app.MapPost("/api/devices", async (DeviceInfo device, AppDbContext db) =>
{
    db.DevicesInfo.Add(device);
    await db.SaveChangesAsync();
    return Results.Created($"/api/devices/{device.Id}", device);
});

app.Run();