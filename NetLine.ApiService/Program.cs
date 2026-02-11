using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// --- KONFIGURACJA ---
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");
builder.Services.AddHttpClient();

var app = builder.Build();

// --- INICJALIZACJA BAZY ---
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Czyœcimy bazê przy starcie, aby pozbyæ siê duplikatów i b³êdnych wpisów
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Baza danych gotowa. Rozpoczynamy nas³uch 5 agentów.");
    }
    catch (Exception ex) { Console.WriteLine($"B³¹d bazy: {ex.Message}"); }
}

// --- ENDPOINTY ---

app.MapGet("/", () => "API NetLine dzia³a. Wywo³aj /scan-my-home, aby sprawdziæ 5 agentów.");

app.MapGet("/devices", async (AppDbContext db) => await db.DevicesInfo.ToListAsync());

app.MapGet("/scan-my-home", async (AppDbContext db) =>
{
    var hostName = Dns.GetHostName();
    var hostEntry = await Dns.GetHostEntryAsync(hostName);
    var myIp = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
    if (myIp == null) return Results.Problem("Nie wykryto adresu IP.");

    string ipString = myIp.ToString();
    string networkPrefix = ipString.Substring(0, ipString.LastIndexOf('.') + 1);

    int foundCount = 0;

    // LISTA TWOICH 5 AGENTÓW Z SYMULATORA
    var targets = new List<string> {
        "127.0.0.1",
        "127.0.0.2",
        "127.0.0.3",
        "127.0.0.4",
        "127.0.0.5",
        "127.0.0.6"
    };

    // Dodatkowo skanujemy pierwsze 10 adresów w sieci domowej
    for (int i = 1; i <= 10; i++) targets.Add(networkPrefix + i);

    foreach (var testIp in targets)
    {
        try
        {
            // Pobieramy dane sysDescr, sysName i sysUpTime
            var snmp = await GetExtendedSnmpData(testIp);

            if (snmp.Success)
            {
                // Identyfikujemy urz¹dzenie po IP, aby unikn¹æ b³êdów unikalnoœci nazwy
                var device = await db.DevicesInfo.FirstOrDefaultAsync(d => d.IpAddress == testIp)
                             ?? new DeviceInfo { IpAddress = testIp };

                device.Status = "Online";
                device.SystemVersion = snmp.SysDescr;

                // LOGIKA UNIKALNEJ NAZWY: 
                // Jeœli w symulatorze wpisa³aœ "Xiamen-R", to zostanie u¿yte. 
                // Jeœli jest "NoSuchObject", stworzymy nazwê na podstawie IP.
                string agentName = snmp.SysName;
                if (string.IsNullOrEmpty(agentName) || agentName.Contains("NoSuchObject"))
                {
                    agentName = $"Agent-{testIp.Replace(".", "_")}";
                }

                device.UniqueIdOrName = agentName;
                device.DeviceType = "SNMP Agent";
                device.LastScanned = DateTime.UtcNow;
                device.RawLogs = $"Uptime: {snmp.Uptime} | Dane pobrane z portu 161";

                // Opcjonalny Ping dla TTL
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(testIp, 500);
                if (reply.Status == IPStatus.Success)
                {
                    device.Ttl = reply.Options?.Ttl;
                    device.PingResponseTimeMs = reply.RoundtripTime;
                }

                if (device.Id == 0) db.DevicesInfo.Add(device);
                foundCount++;
            }
        }
        catch { /* Ignorujemy b³êdy po³¹czenia dla konkretnego IP */ }
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { Status = "Skanowanie 5 agentów zakoñczone", Znaleziono = foundCount });
});

app.MapDefaultEndpoints();
app.Run();

// --- FUNKCJA POBIERAJ¥CA DANE Z PÓL SYSTEMOWYCH (OID) ---
async Task<ExtendedSnmpResult> GetExtendedSnmpData(string ip)
{
    try
    {
        // Messenger.Get odpytuje dok³adnie o te pola, które widzia³aœ w symulatorze
        var result = await Task.Run(() => Messenger.Get(
            VersionCode.V2,
            new IPEndPoint(IPAddress.Parse(ip), 161),
            new OctetString("public"),
            new List<Variable> {
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")), // sysDescr.0
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.5.0")), // sysName.0
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"))  // sysUpTime.0
            },
            2000)); // Czekamy 2 sekundy na odpowiedŸ

        return new ExtendedSnmpResult
        {
            Success = true,
            SysDescr = result[0].Data.ToString(),
            SysName = result[1].Data.ToString(),
            Uptime = result[2].Data.ToString()
        };
    }
    catch { return new ExtendedSnmpResult { Success = false }; }
}

public class ExtendedSnmpResult
{
    public bool Success { get; set; }
    public string? SysDescr { get; set; }
    public string? SysName { get; set; }
    public string? Uptime { get; set; }
}