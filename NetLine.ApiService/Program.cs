using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.EntityFrameworkCore;
using NetLine.ApiService.Data;
using NetLine.ApiService.Models;
using System.Diagnostics;
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
        // Czyœcimy bazê przy ka¿dym starcie, aby pozbyæ siê b³êdnych duplikatów
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Baza zresetowana. Gotowa na unikalne adresy IP.");
    }
    catch (Exception ex) { Console.WriteLine($"B³¹d bazy: {ex.Message}"); }
}

// --- ENDPOINTY ---

app.MapGet("/", () => "API NetLine dzia³a. Wywo³aj /scan-my-home");
app.MapGet("/devices", async (AppDbContext db) => await db.DevicesInfo.ToListAsync());

app.MapGet("/scan-my-home", async (AppDbContext db) =>
{
    var hostName = Dns.GetHostName();
    var hostEntry = await Dns.GetHostEntryAsync(hostName);
    var myIp = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
    if (myIp == null) return Results.Problem("Nie wykryto IP hosta.");

    string ipString = myIp.ToString();
    string networkPrefix = ipString.Substring(0, ipString.LastIndexOf('.') + 1);

    int foundCount = 0;

    // LISTA TWOICH AGENTÓW (127.0.0.1 - 127.0.0.5)
    var targets = new List<string> { "127.0.0.1", "127.0.0.2", "127.0.0.3", "127.0.0.4", "127.0.0.5" };

    // Opcjonalnie: skanowanie sieci lokalnej (pierwsze 10 adresów)
    for (int i = 1; i <= 10; i++) targets.Add(networkPrefix + i);

    foreach (var testIp in targets)
    {
        try
        {
            var snmp = await GetExtendedSnmpData(testIp);

            if (snmp.Success)
            {
                // SZUKAMY URZ¥DZENIA TYLKO PO IP
                var device = await db.DevicesInfo.FirstOrDefaultAsync(d => d.IpAddress == testIp)
                             ?? new DeviceInfo { IpAddress = testIp };

                device.Status = "Online";
                device.SystemVersion = snmp.SysDescr;

                // UNIKALNA NAZWA: Jeœli SNMP zawiedzie, u¿ywamy IP, ¿eby unikn¹æ b³êdu Duplicate Key
                string finalName = snmp.SysName;
                if (string.IsNullOrEmpty(finalName) || finalName.Contains("NoSuchObject"))
                {
                    finalName = $"Agent-{testIp.Replace(".", "_")}"; // Np. Agent-127_0_0_2
                }

                device.UniqueIdOrName = finalName;
                device.DeviceType = "SNMP Agent";
                device.LastScanned = DateTime.UtcNow;
                device.RawLogs = $"Uptime: {snmp.Uptime} | Pobrano z IP: {testIp}";

                if (device.Id == 0) db.DevicesInfo.Add(device);
                foundCount++;
            }
        }
        catch { /* Przeskok w razie b³êdu po³¹czenia */ }
    }

    // Zapisujemy zmiany – teraz bez b³êdu o duplikatach!
    await db.SaveChangesAsync();
    return Results.Ok(new { Status = "Skanowanie zakoñczone", Znaleziono = foundCount });
});

app.MapDefaultEndpoints();
app.Run();

// --- FUNKCJA POBIERAJ¥CA DANE ---
async Task<ExtendedSnmpResult> GetExtendedSnmpData(string ip)
{
    try
    {
        var result = await Task.Run(() => Messenger.Get(
            VersionCode.V2,
            new IPEndPoint(IPAddress.Parse(ip), 161),
            new OctetString("public"),
            new List<Variable> {
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")), // sysDescr
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.5.0")), // sysName
                new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"))  // sysUpTime
            },
            2000)); // Krótszy timeout, ¿eby skanowanie 5 agentów sz³o szybciej

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