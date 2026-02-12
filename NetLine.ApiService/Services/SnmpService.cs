using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace NetLine.ApiService.Services;

// Klasa pomocnicza, która przechowa wynik skanowania
public class SnmpScanResult
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Contact { get; set; }
    public string? ErrorMessage { get; set; }
    public long? PingResponseTimeMs { get; set; }
}

public class SnmpService
{
    // Adresy OID o które pytamy (standardowe dla większości urządzeń)
    private static readonly ObjectIdentifier OidSysDescr = new(".1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier OidSysContact = new(".1.3.6.1.2.1.1.4.0");
    private static readonly ObjectIdentifier OidSysName = new(".1.3.6.1.2.1.1.5.0");
    private static readonly ObjectIdentifier OidSysLocation = new(".1.3.6.1.2.1.1.6.0");

    public async Task<SnmpScanResult> GetDeviceInfoAsync(string ipAddress, string community = "public")
    {
        try
        {
            // Przygotowujemy listę zadań do wykonania (nasze pytania do urządzenia)
            var variables = new List<Variable>
            {
                new Variable(OidSysDescr),
                new Variable(OidSysName),
                new Variable(OidSysLocation),
                new Variable(OidSysContact)
            };

            // Wykonujemy zapytanie (Messenger.Get) w osobnym wątku, żeby nie blokować aplikacji
            var result = await Task.Run(() => Messenger.Get(
                VersionCode.V2,
                new IPEndPoint(IPAddress.Parse(ipAddress), 161),
                new OctetString(community),
                variables,
                2000 // Czekaj max 2 sekundy
            ));

            return new SnmpScanResult
            {
                Success = true,
                Description = result[0].Data.ToString(),
                Name = result[1].Data.ToString(),
                Location = result[2].Data.ToString(),
                Contact = result[3].Data.ToString()
            };
        }
        catch (Exception ex)
        {
            return new SnmpScanResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}