using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Alerts;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Services.Alerts;

public class DeviceStatusService : IDeviceStatusService
{
    private readonly ILogger<DeviceStatusService> _logger;
    private readonly AppDbContext _dbContext;

    public DeviceStatusService(ILogger<DeviceStatusService> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task ProcessScanResultsAsync(
        List<DeviceInfo> devices,
        IReadOnlyList<DeviceScanResult> scanResults,
        CancellationToken cancellationToken)
    {
        var scanResultsByDeviceId = scanResults.ToDictionary(r => r.DeviceId);

        foreach (var device in devices)
        {
            if (!scanResultsByDeviceId.TryGetValue(device.Id, out var scanResult))
            {
                _logger.LogWarning("No scan result found for device {DeviceId}", device.Id);
                continue;
            }

            device.LastScanned = DateTime.UtcNow;
            device.PingResponseTimeMs = scanResult.PingResponseTimeMs;

            string oldStatus = device.Status;
            string newStatus = DetermineDeviceStatus(scanResult);

            if (oldStatus != newStatus)
            {
                await CreateStatusChangeAlertAsync(device, oldStatus, newStatus, cancellationToken);
            }

            device.Status = newStatus;
            UpdateSnmpData(device, newStatus, scanResult);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string DetermineDeviceStatus(DeviceScanResult scanResult)
    {
        bool isOnline = scanResult.PingResponseTimeMs.HasValue || scanResult.SnmpData.Success;
        return isOnline ? "Online" : "Offline";
    }

    private async Task CreateStatusChangeAlertAsync(
        DeviceInfo device,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var alertType = newStatus == "Offline" ? AlertType.WentOffline : AlertType.CameOnline;
        var message = newStatus == "Offline"
            ? $"Urz¹dzenie {device.UserDefinedName} przesta³o odpowiadaæ."
            : $"Urz¹dzenie {device.UserDefinedName} wróci³o do sieci.";

        var alert = new DeviceAlert
        {
            DeviceInfoId = device.Id,
            Timestamp = DateTime.UtcNow,
            Type = alertType,
            Message = message
        };

        _dbContext.DeviceAlerts.Add(alert);

        _logger.LogInformation(
            "Status change detected for device {DeviceId}: {OldStatus} -> {NewStatus}. Alert: {Message}",
            device.Id, oldStatus, newStatus, message);

        await Task.CompletedTask;
    }

    private static void UpdateSnmpData(DeviceInfo device, string newStatus, DeviceScanResult scanResult)
    {
        if (newStatus == "Online")
        {
            if (scanResult.SnmpData.Success)
            {
                device.SysName = scanResult.SnmpData.Name;
                device.SysDescr = scanResult.SnmpData.Description;
                device.SysLocation = scanResult.SnmpData.Location;
                device.SysContact = scanResult.SnmpData.Contact;
                device.SysUpTime = scanResult.SnmpData.UpTime;
                device.SysInterfacesCount = scanResult.SnmpData.InterfacesCount;
            }
            else
            {
                ClearSnmpData(device);
            }
        }
        else
        {
            device.PingResponseTimeMs = null;
            ClearSnmpData(device);
        }
    }

    private static void ClearSnmpData(DeviceInfo device)
    {
        device.SysName = null;
        device.SysDescr = null;
        device.SysLocation = null;
        device.SysContact = null;
        device.SysUpTime = null;
        device.SysInterfacesCount = null;
    }
}