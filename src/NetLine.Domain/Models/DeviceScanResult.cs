namespace NetLine.Domain.Models;

/// <summary>
/// Represents the scan result for a single device.
/// Contains both ping and SNMP data collected during a monitoring cycle.
/// </summary>
public record DeviceScanResult(
    int DeviceId,
    string IpAddress,
    long? PingResponseTimeMs,
    SNMPScanResult SnmpData);