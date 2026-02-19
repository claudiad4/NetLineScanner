using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services;

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
        _logger.LogInformation("Starting scanning for IP: {IpAddress}", ipAddress);

        // --- 1. PING (IMCP) ---
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);

            if (reply != null && reply.Status == IPStatus.Success)
            {
                result.PingResponseTimeMs = reply.RoundtripTime == 0 ? 1 : reply.RoundtripTime;
                _logger.LogDebug("Ping OK: {Ms}ms", result.PingResponseTimeMs);
            }
            else
            {
                _logger.LogWarning("Device {IpAddress} is not responding to PING (Offline).", ipAddress);
                result.Success = false;
                result.ErrorMessage = "Device is Offline.";
                return result; 
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace("ERROR: {IpAddress}: {Msg}", ipAddress, ex.Message);        }

        // --- 2. SNMP  ---
        try
        {
            var variables = new List<Variable>
            {
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.1.0")), // Descr
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.5.0")), // Name
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.6.0")), // Location
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.4.0")), // Contact
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.1.3.0")), // UpTime 
                new Variable(new ObjectIdentifier(".1.3.6.1.2.1.2.1.0"))  // IfNumber 

                //we can add more OIDs HERE
            };

            var snmpData = await Task.Run(() => Messenger.Get(
                VersionCode.V2, //SNMP version 2c
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

                _logger.LogInformation("Scanning was successful for {IpAddress}", ipAddress);
            }
        }
        catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
        {
            _logger.LogWarning("Timeout SNMP for {IpAddress}. Device is online but SNMP is not working.", ipAddress);
            result.Success = false;
            result.ErrorMessage = "No answer from SNMP (Timeout).";
        }
        catch (Exception ex)
        {
            _logger.LogError("SNMP CRITICAL ERROR {IpAddress}: {Message}", ipAddress, ex.Message);
            result.Success = false;
            result.ErrorMessage = "SNMP ERROR";
        }

        return result;
    }
}