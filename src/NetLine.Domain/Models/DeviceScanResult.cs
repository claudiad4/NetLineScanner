namespace NetLine.Domain.Models;

/// <summary>
/// Aggregate scan outcome for one device collected in a monitoring cycle.
/// Holds legacy ping/SNMP fields for API back-compat plus a list of
/// per-component results produced by <see cref="NetLine.Application.Interfaces.Monitoring.IMonitoringComponent"/>.
/// </summary>
public sealed record DeviceScanResult(
    int DeviceId,
    string IpAddress,
    long? PingResponseTimeMs,
    SNMPScanResult SnmpData,
    IReadOnlyList<ComponentResult> ComponentResults)
{
    public DeviceScanResult(int deviceId, string ipAddress, long? pingResponseTimeMs, SNMPScanResult snmpData)
        : this(deviceId, ipAddress, pingResponseTimeMs, snmpData, Array.Empty<ComponentResult>()) { }
}
