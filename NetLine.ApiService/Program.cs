using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure.Data;       // Baza danych (Infrastructure)
using NetLine.Infrastructure.Services;   // Implementacja SNMP (Infrastructure)
using NetLine.Application.Interfaces;    // Interfejs SNMP (Application)
using NetLine.ApiService.Hubs;           // SignalR Hubs (ApiService)
using NetLine.ApiService.Services;       // Background Service (ApiService)
using NetLine.ApiService.Endpoints;      // Wydzielone Endpointy (ApiService)

var builder = WebApplication.CreateBuilder(args);

// --- 1. REJESTRACJA US£UG (Dependency Injection) ---

// Domyœlne ustawienia Aspire
builder.AddServiceDefaults();

// Baza danych PostgreSQL (korzysta z AppDbContext w Infrastructure)
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

// Rejestracja serwisu SNMP zgodnie z Clean Architecture
// Mówimy: "Gdy potrzebny jest ISnmpService, u¿yj implementacji SnmpService z Infrastructure"
builder.Services.AddSingleton<ISNMPService, SnmpService>();

// Serwis monitoruj¹cy w tle (Hosted Service)
builder.Services.AddHostedService<DeviceMonitorService>();

// Pozosta³e us³ugi (SignalR, Swagger)
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 2. INICJALIZACJA BAZY DANYCH ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Tworzy bazê, jeœli nie istnieje (w produkcji lepiej u¿ywaæ Migracji)
    await context.Database.EnsureCreatedAsync();
}

// --- 3. KONFIGURACJA POTOKU HTTP (Middleware) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

// --- 4. MAPOWANIE ENDPOINTÓW ---

// SignalR Hub
app.MapHub<DeviceHub>("/devicehub");

// Prosty test
app.MapGet("/", () => "NetLine API - System Monitorowania SNMP gotowy.");

// Wszystkie endpointy zwi¹zane z urz¹dzeniami (Scan, Get, Add)
// przeniesione do pliku Endpoints/DeviceEndpoints.cs
app.MapDeviceEndpoints();

app.Run();