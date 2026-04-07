using System.Net.NetworkInformation;
using NetLine.Application.Interfaces;

namespace NetLine.Infrastructure.Services;

public class ICMPService : IICMPService
{
    public async Task<long?> GetPingResponseTimeAsync(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            // Wysyłamy ping z timeoutem 1000ms
            var reply = await ping.SendPingAsync(ipAddress, 1000);

            if (reply != null && reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime == 0 ? 1 : reply.RoundtripTime;
            }
        }
        catch
        {
            // Błędy sieciowe zwracają null
        }
        return null;
    }
}