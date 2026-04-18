using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Options;
using NetLine.Application.Interfaces.Monitoring;

namespace NetLine.Infrastructure.Services.Monitoring.Snmp;

public class SnmpOptions
{
    public const string SectionName = "Snmp";
    public string Community { get; set; } = "public";
    public int TimeoutMs { get; set; } = 2000;
    public int Port { get; set; } = 161;
}

/// <summary>
/// Lightweight wrapper around SharpSnmpLib that exposes async Get/Walk primitives
/// and uniform error handling for monitoring components.
/// </summary>
public sealed class SnmpClient : ISnmpClient
{
    private readonly string _community;
    private readonly int _timeoutMs;
    private readonly int _port;

    public SnmpClient(IOptions<SnmpOptions> options)
    {
        var opt = options.Value;
        _community = string.IsNullOrWhiteSpace(opt.Community) ? "public" : opt.Community;
        _timeoutMs = opt.TimeoutMs > 0 ? opt.TimeoutMs : 2000;
        _port = opt.Port > 0 ? opt.Port : 161;
    }

    public async Task<IReadOnlyList<Variable>?> GetAsync(string ipAddress, IEnumerable<string> oids, CancellationToken cancellationToken = default)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), _port);
        var community = new OctetString(_community);

        try
        {
            var result = await Task.Run(() => Messenger.Get(
                VersionCode.V2,
                endpoint,
                community,
                variables,
                _timeoutMs), cancellationToken);

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
        var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), _port);
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
                _timeoutMs,
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
