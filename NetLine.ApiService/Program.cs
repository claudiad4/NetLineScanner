using Microsoft.EntityFrameworkCore;

using NetLine.ApiService.Data;

using NetLine.ApiService.Models;

using System.Net;

using System.Net.NetworkInformation;

using System.Text.RegularExpressions;

using System.Diagnostics;



var builder = WebApplication.CreateBuilder(args);



// --- KONFIGURACJA ---

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

builder.Services.AddOpenApi();

builder.Services.AddHttpClient(); // Potrzebne do sprawdzania producentów w sieci



var app = builder.Build();



// --- INICJALIZACJA BAZY ---

using (var scope = app.Services.CreateScope())

{

    try

    {

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

    }

    catch { /* Baza pewnie ju¿ dzia³a */ }

}



// --- ENDPOINTY ---



app.MapGet("/", () => "API NetLine dzia³a. Wywo³aj /scan-my-home");



app.MapGet("/devices", async (AppDbContext db) => await db.DevicesBasicInfo.ToListAsync());



app.MapGet("/scan-my-home", async (AppDbContext db, HttpClient client) =>

{

    var hostInfo = await Dns.GetHostEntryAsync(Dns.GetHostName());

    var localIp = hostInfo.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

    if (string.IsNullOrEmpty(localIp)) return Results.Problem("Nie wykryto IP.");



    string networkPrefix = localIp.Substring(0, localIp.LastIndexOf('.'));

    int foundCount = 0;



    for (int i = 1; i <= 30; i++)

    {

        string testIp = $"{networkPrefix}.{i}";

        using var ping = new Ping();

        try

        {

            var reply = await ping.SendPingAsync(testIp, 100);

            if (reply.Status == IPStatus.Success)

            {

                // 1. Pobieramy MAC (¿eby zapytaæ o producenta)

                string mac = "Unknown";

                try

                {

                    var psi = new ProcessStartInfo("arp", "-a " + testIp) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };

                    using var process = Process.Start(psi);

                    string output = process.StandardOutput.ReadToEnd();

                    var match = Regex.Match(output, @"([0-9a-fA-F]{2}[:-]){5}([0-9a-fA-F]{2})");

                    if (match.Success) mac = match.Value.ToUpper().Replace("-", ":");

                }

                catch { }



                // 2. Odpytujemy API o producenta (jeœli znamy MAC)

                string manufacturer = "Nieznane";

                if (mac != "Unknown")

                {

                    try

                    {

                        var response = await client.GetAsync($"https://api.macvendors.com/{mac}");

                        if (response.IsSuccessStatusCode) manufacturer = await response.Content.ReadAsStringAsync();

                    }

                    catch { }

                }



                // 3. Próba pobrania nazwy z DNS (np. laptop-home)

                string dnsName = "";

                try

                {

                    var entry = await Dns.GetHostEntryAsync(testIp);

                    dnsName = entry.HostName.Split('.')[0].ToUpper();

                }

                catch { }



                // 4. £¹czymy to w jedn¹ sensown¹ nazwê dla u¿ytkownika

                // Priorytet: Nazwa z DNS > Producent > IP

                string finalDisplayName = !string.IsNullOrEmpty(dnsName) ? dnsName : (manufacturer != "Nieznane" ? manufacturer : $"Urz¹dzenie-{testIp.Split('.').Last()}");



                // 5. Okreœlamy typ (do tabeli)

                string type = "Komputer";

                string check = (finalDisplayName + manufacturer).ToLower();

                if (check.Contains("samsung") || check.Contains("phone") || check.Contains("oppo") || check.Contains("apple")) type = "Telefon";

                else if (check.Contains("tv") || check.Contains("lg") || check.Contains("sony")) type = "Telewizor";

                else if (testIp.EndsWith(".1") || check.Contains("router") || check.Contains("tp-link")) type = "Router";



                // 6. ZAPIS DO BAZY (bez nowych pól, u¿ywamy tych co masz)

                var existing = await db.DevicesBasicInfo.FirstOrDefaultAsync(d => d.IpAddress == testIp);

                if (existing == null)

                {

                    db.DevicesBasicInfo.Add(new DeviceBasicInfo

                    {

                        IpAddress = testIp,

                        UniqueIdOrName = finalDisplayName, // Tu wpadnie np. "OPPO" albo "LG"

                        DeviceType = type,

                        Status = "Online"

                    });

                    foundCount++;

                }

                else

                {

                    existing.UniqueIdOrName = finalDisplayName;

                    existing.DeviceType = type;

                    existing.Status = "Online";

                }



                // Czekamy chwilê, ¿eby nie zablokowali nas za zbyt szybkie pytania o MAC

                await Task.Delay(500);

            }

        }

        catch { }

    }



    await db.SaveChangesAsync();

    return Results.Ok($"Skanowanie zakoñczone. Znaleziono {foundCount} urz¹dzeñ.");

});



app.MapDefaultEndpoints();

app.Run();



record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);