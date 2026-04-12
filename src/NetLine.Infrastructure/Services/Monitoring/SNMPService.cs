using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;
using NetLine.Domain.Models;
using NetLine.Domain.Entities;
using NetLine.Application.Interfaces.Monitoring;

namespace NetLine.Infrastructure.Services.Monitoring;

public class SnmpService : ISNMPService
{
    private readonly ILogger<SnmpService> _logger;

    public SnmpService(ILogger<SnmpService> logger)
    {
        _logger = logger;
    }

    public async Task<SNMPScanResult> GetDeviceInfoAsync(string ipAddress)
    {
        var result = new SNMPScanResult();
        _logger.LogInformation("Starting SNMP scan for IP: {IpAddress}", ipAddress);

        try
        {
            var variables = new List<Variable>
            {
                new Variable(new ObjectIdentifier(OIDDictionary.SysDescr)),
                new Variable(new ObjectIdentifier(OIDDictionary.SysName)),
                new Variable(new ObjectIdentifier(OIDDictionary.SysLocation)),
                new Variable(new ObjectIdentifier(OIDDictionary.SysContact)),
                new Variable(new ObjectIdentifier(OIDDictionary.SysUpTime)),
                new Variable(new ObjectIdentifier(OIDDictionary.SysInterfacesCount))
            };

          
            var snmpData = await Task.Run(() => Messenger.Get(
                VersionCode.V2,
                new IPEndPoint(IPAddress.Parse(ipAddress), 161),
                new OctetString("public"),
                variables,
                2000));

            if (snmpData != null && snmpData.Count > 0)
            {
                result.Success = true;
                result.Description = snmpData[0].Data.ToString();
                result.Name = snmpData[1].Data.ToString();
                result.Location = snmpData[2].Data.ToString();
                result.Contact = snmpData[3].Data.ToString();
                result.UpTime = snmpData[4].Data.ToString();

                if (int.TryParse(snmpData[5].Data.ToString(), out var ifCount))
                {
                    result.InterfacesCount = ifCount;
                }

                _logger.LogInformation("SNMP scan successful for {IpAddress}", ipAddress);
            }
        }
        catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
        {
            _logger.LogWarning("SNMP timeout for {IpAddress}.", ipAddress);
            result.Success = false;
            result.ErrorMessage = "SNMP Timeout";
        }

        catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
        {
            _logger.LogInformation("SNMP connection refused by {IpAddress} (Agent is probably off).", ipAddress);
            result.Success = false;
            result.ErrorMessage = "SNMP Agent Offline";
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP error for {IpAddress}", ipAddress);
            result.Success = false;
            result.ErrorMessage = "SNMP ERROR";
        }

        return result;
    }
}