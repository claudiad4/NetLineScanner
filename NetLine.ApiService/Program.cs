using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Aspire wstrzyknie connection string do "deviceinfo"
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

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

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Sukces: Migracja bazy danych zakoñczona pomyœlnie!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"B³¹d podczas migracji: {ex.Message}");
    }
}

// Endpoint do pobierania listy
app.MapGet("/devices", async (AppDbContext db) => await db.DevicesBasicInfo.ToListAsync());

// Endpoint do skanowania (uruchamia ping i zapisuje do bazy)
app.MapGet("/scan/{ipAddress}", async (string ipAddress, AppDbContext db) =>
{
    using var ping = new Ping();
    try
    {
        // Wysy³amy sygna³ do urz¹dzenia
        var reply = await ping.SendPingAsync(ipAddress, 1000);
        var isOnline = (reply.Status == IPStatus.Success);

        // Tworzymy obiekt urz¹dzenia do zapisu
        var device = new DeviceBasicInfo
        {
            IpAddress = ipAddress,
            Status = isOnline ? "Online" : "Offline",
            DeviceType = "Zeskanowane",
            UniqueIdOrName = $"Skan-{DateTime.Now:HHmm}-{ipAddress}"
        };

        db.DevicesBasicInfo.Add(device);
        await db.SaveChangesAsync();

        return Results.Ok(new { Status = device.Status, Info = "Zapisano w bazie!" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"B³¹d skanowania: {ex.Message}");
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
