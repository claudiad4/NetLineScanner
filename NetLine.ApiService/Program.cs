using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// --- KONFIGURACJA ---
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");
builder.Services.AddHttpClient();

var app = builder.Build();

// --- INICJALIZACJA BAZY (Tworzy tabele, jeœli ich nie ma) ---
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
    catch { }
}

// --- ENDPOINTY ---

app.MapGet("/", () => "API NetLine dzia³a. Wywo³aj /scan-my-home, aby przeszukaæ sieæ.");

// Endpoint do podgl¹du wszystkich zapisanych urz¹dzeñ
app.MapGet("/devices", async (AppDbContext db) => await db.DevicesBasicInfo.ToListAsync());

// G£ÓWNY SKANER
app.MapGet("/scan-my-home", async (AppDbContext db) =>
{
    // 1. POBIERANIE NAZWY SIECI WIFI (SSID)
    string ssid = "Nie wykryto (Ethernet)";
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        var match = Regex.Match(output, @"^\s+SSID\s+:\s+(.*)$", RegexOptions.Multiline);
        if (match.Success) ssid = match.Groups[1].Value.Trim();
    }
    catch { }

    // 2. IDENTYFIKACJA TWOJEGO IP I PREFIKSU SIECI
    var hostName = Dns.GetHostName();
    var hostEntry = await Dns.GetHostEntryAsync(hostName);
    var myIp = hostEntry.AddressList
        .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

    if (myIp == null) return Results.Problem("Nie wykryto adresu IP hosta.");

    string ipString = myIp.ToString();
    string networkPrefix = ipString.Substring(0, ipString.LastIndexOf('.') + 1);

    // Oznaczamy wszystkie urz¹dzenia w bazie jako Offline przed skanowaniem
    var allDevices = await db.DevicesBasicInfo.ToListAsync();
    foreach (var d in allDevices) d.Status = "Offline";

    int foundCount = 0;

    // 3. SKANOWANIE ZAKRESU (od .1 do .30 - najbezpieczniejszy zakres domowy)
    for (int i = 1; i <= 30; i++)
    {
        string testIp = networkPrefix + i;

        using var ping = new Ping();
        try
        {
            // Próbujemy "dopingowaæ" urz¹dzenie 
            var reply = await ping.SendPingAsync(testIp, 100);

            if (reply.Status == IPStatus.Success)
            {
                //  Próbujemy pobraæ jego nazwê sieciow¹
                string displayName = $"URZADZENIE-{i}";
                try
                {
                    var entry = await Dns.GetHostEntryAsync(testIp);
                    displayName = entry.HostName.Split('.')[0].ToUpper();
                }
                catch { /* Brak nazwy DNS - zostanie URZADZENIE-X */ }

                // Jeœli to nasze IP, dodajmy dopisek
                if (testIp == ipString) displayName = $"{hostName} (MÓJ HOST)";

                // 4. ZAPIS LUB AKTUALIZACJA W BAZIE
                var existing = await db.DevicesBasicInfo.FirstOrDefaultAsync(d => d.IpAddress == testIp);

                if (existing == null)
                {
                    db.DevicesBasicInfo.Add(new DeviceBasicInfo
                    {
                        IpAddress = testIp,
                        UniqueIdOrName = displayName,
                        DeviceType = testIp.EndsWith(".1") ? "Router" : "Wykryte",
                        Status = "Online"
                    });
                }
                else
                {
                    existing.UniqueIdOrName = displayName;
                    existing.Status = "Online";
                }
                foundCount++;
            }
        }
        catch { /* Ignorujemy b³êdy dla danego IP */ }
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        NazwaSieci = ssid,
        TwojeIP = ipString,
        ZakresSkanowania = $"{networkPrefix}1 - {networkPrefix}30",
        ZnalezionoUrzadzen = foundCount,
        Status = "Baza danych zaktualizowana"
    });
});

app.MapDefaultEndpoints();
app.Run();