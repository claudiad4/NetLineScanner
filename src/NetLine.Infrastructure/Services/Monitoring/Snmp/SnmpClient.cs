using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using NetLine.Application.Interfaces.Monitoring;

namespace NetLine.Infrastructure.Services.Monitoring.Snmp;

/// <summary>
/// Lightweight wrapper around SharpSnmpLib that exposes async Get/Walk primitives
/// and uniform error handling for monitoring components.
/// </summary>
public sealed class SnmpClient : ISnmpClient
{
    private const int DefaultTimeoutMs = 2000;
    private const int DefaultPort = 161;

    private readonly string _community;

    public SnmpClient(string community = "public")
    {
        _community = community;
    }

    public async Task<IReadOnlyList<Variable>?> GetAsync(string ipAddress, IEnumerable<string> oids, CancellationToken cancellationToken = default)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), DefaultPort);
        var community = new OctetString(_community);

        try
        {
            var result = await Task.Run(() => Messenger.Get(
                VersionCode.V2,
                endpoint,
                community,
                variables,
                DefaultTimeoutMs), cancellationToken);

            return result.ToList();
        }
        catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Variable>> WalkAsync(string ipAddress, string rootOid, CancellationToken cancellationToken = default)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), DefaultPort);
        var community = new OctetString(_community);
        var results = new List<Variable>();

        try
        {
            await Task.Run(() => Messenger.Walk(
                VersionCode.V2,
                endpoint,
                community,
                new ObjectIdentifier(rootOid),
                results,
                DefaultTimeoutMs,
                WalkMode.WithinSubtree), cancellationToken);
        }
        catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
        {
            return Array.Empty<Variable>();
        }
        catch (SocketException)
        {
            return Array.Empty<Variable>();
        }

        return results;
    }
}
