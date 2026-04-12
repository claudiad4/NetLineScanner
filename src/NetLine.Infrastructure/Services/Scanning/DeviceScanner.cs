using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Scanning;

public class DeviceScanner : IDeviceScanner
{
    private readonly ISNMPService _snmpService;
    private readonly IICMPService _icmpService;
    private readonly ILogger<DeviceScanner> _logger;

    public DeviceScanner(
        ISNMPService snmpService,
        IICMPService icmpService,
        ILogger<DeviceScanner> logger)
    {
        _snmpService = snmpService;
        _icmpService = icmpService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken)
    {
        var deviceList = devices.ToList();
        var scanResults = new List<DeviceScanResult>();

        // Using Parallel.ForEachAsync to prevent socket exhaustion on large device lists
        await Parallel.ForEachAsync(
            deviceList,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (device, ct) =>
            {
                try
                {
                    var pingTask = _icmpService.GetPingResponseTimeAsync(device.IpAddress);
                    var snmpTask = _snmpService.GetDeviceInfoAsync(device.IpAddress);

                    // Wait for both operations for this device
                    await Task.WhenAll(pingTask, snmpTask);

                    var pingTime = await pingTask;
                    var snmpData = await snmpTask;

                    var result = new DeviceScanResult(
                        device.Id,
                        device.IpAddress,
                        pingTime,
                        snmpData);

                    lock (scanResults)
                    {
                        scanResults.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to scan device {DeviceId} at {IpAddress}",
                        device.Id,
                        device.IpAddress);

                    // Return offline result on exception
                    var offlineResult = new DeviceScanResult(
                        device.Id,
                        device.IpAddress,
                        null,
                        new SNMPScanResult { Success = false, ErrorMessage = ex.Message });

                    lock (scanResults)
                    {
                        scanResults.Add(offlineResult);
                    }
                }
            });

        return scanResults.AsReadOnly();
    }
}