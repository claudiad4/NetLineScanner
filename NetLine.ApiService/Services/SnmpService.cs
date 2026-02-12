using System.Net;
using System.Net.NetworkInformation;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace NetLine.ApiService.Services;

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
    public async Task<SnmpScanResult> GetDeviceInfoAsync(string ipAddress)
    {
        var result = new SnmpScanResult();

        try
        {
            // --- 1. POMIAR PING ---
            try
            {
                using var ping = new Ping();
                // Zwiększamy timeout do 2 sekund i dodajemy mały bufor
                var reply = await ping.SendPingAsync(ipAddress, 2000);

                if (reply != null && reply.Status == IPStatus.Success)
                {
                    result.PingResponseTimeMs = reply.RoundtripTime == 0 ? 1 : reply.RoundtripTime;
                    // Czasami 127.0.0.1 zwraca 0ms, co baza może mylić z błędem, ustawiamy min. 1ms
                }
                else
                {
                    result.PingResponseTimeMs = null;
                }
            }
            catch
            {
                result.PingResponseTimeMs = null;
            }

            // --- 2. POBIERANIE DANYCH SNMP ---
            var variables = new List<Variable>
            {
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.1.0")), // Opis
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.5.0")), // Nazwa
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.6.0")), // Lokalizacja
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.4.0"))  // Kontakt
            };

            // Wykonujemy GET SNMP (Timeout 2000ms)
            var snmpData = await Task.Run(() => Messenger.Get(
                VersionCode.V2,
                new IPEndPoint(IPAddress.Parse(ipAddress), 161),
                new OctetString("public"),
                variables,
                2000));

            // Jeśli doszliśmy tutaj, SNMP odpowiedziało
            result.Success = true;
            result.Description = snmpData[0].Data.ToString();
            result.Name = snmpData[1].Data.ToString();
            result.Location = snmpData[2].Data.ToString();
            result.Contact = snmpData[3].Data.ToString();
        }
        catch (Exception ex)
        {
            // Jeśli złapiemy błąd (np. Timeout), ustawiamy Success na false
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}