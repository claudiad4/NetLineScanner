using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using System.Net;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

// --- KONFIGURACJA US£UG ---
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo"); //
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- POGODA (Zgodnie z proœb¹ zostawiona bez zmian) ---
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// --- BEZPIECZNA INICJALIZACJA BAZY (Tylko raz!) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // MigrateAsync na³o¿y brakuj¹ce tabele bez przerywania programu
        await context.Database.MigrateAsync();
        Console.WriteLine("Sukces: Baza danych zosta³a zsynchronizowana!");
    }
    catch (Exception ex)
    {
        // Jeœli baza w Dockerze jeszcze wstaje, program wypisze b³¹d zamiast siê zawiesiæ
        Console.WriteLine($"Oczekiwanie na bazê danych: {ex.Message}");
    }
}

// --- ENDPOINTY TWOJEGO SKANERA ---

app.MapGet("/", () => "API NetLine dzia³a. Wywo³aj /scan-my-home aby rozpocz¹æ.");

// Endpoint do pobierania listy z bazy (dla Twojej strony Razor)
app.MapGet("/devices", async (AppDbContext db) => await db.DevicesBasicInfo.ToListAsync());

// INTELIGENTNY SKANER (Sam znajduje IP i typ urz¹dzenia)
app.MapGet("/scan-my-home", async (AppDbContext db) =>
{
    // 1. Automatyczne wykrywanie prefixu Twojej sieci
    var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
    var localIp = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

    if (string.IsNullOrEmpty(localIp)) return Results.BadRequest("B³¹d: Nie wykryto adresu IP w sieci lokalnej.");

    // Tworzy np. "192.168.1" z Twojego "192.168.1.15"
    string networkPrefix = localIp.Substring(0, localIp.LastIndexOf('.'));
    int foundCount = 0;
    using var ping = new Ping();

    // Skanujemy zakres 1-20 (mo¿esz zwiêkszyæ do 254)
    for (int i = 1; i <= 20; i++)
    {
        string testIp = $"{networkPrefix}.{i}";
        try
        {
            var reply = await ping.SendPingAsync(testIp, 200);
            if (reply.Status == IPStatus.Success)
            {
                string deviceName;
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(testIp);
                    deviceName = hostEntry.HostName;
                }
                catch
                {
                    deviceName = $"Urz¹dzenie-{testIp}";
                }

                // 2. Rozpoznawanie typu urz¹dzenia (Device type)
                string type = "Stacja robocza"; // Domyœlny
                string nameLower = deviceName.ToLower();

                if (nameLower.Contains("samsung") || nameLower.Contains("iphone") || nameLower.Contains("phone") || nameLower.Contains("android"))
                    type = "Telefon";
                else if (nameLower.Contains("desktop") || nameLower.Contains("pc") || nameLower.Contains("laptop"))
                    type = "Komputer";
                else if (nameLower.Contains("switch") || nameLower.Contains("router") || nameLower.Contains("tplink") || nameLower.Contains("bridge"))
                    type = "Urz¹dzenie sieciowe";

                var device = new DeviceBasicInfo
                {
                    IpAddress = testIp,
                    Status = "Online",
                    DeviceType = type,
                    UniqueIdOrName = deviceName
                };

                db.DevicesBasicInfo.Add(device);
                foundCount++;
            }
        }
        catch { }
    }

    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        Message = $"Skanowanie zakoñczone! Znaleziono {foundCount} urz¹dzeñ.",
        Network = $"{networkPrefix}.0",
        MyIp = localIp
    });
});

app.MapDefaultEndpoints();
app.Run();

// Model pogody
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}