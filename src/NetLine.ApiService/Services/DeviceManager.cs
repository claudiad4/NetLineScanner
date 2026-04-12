using Microsoft.EntityFrameworkCore;
using NetLine.Application.Interfaces.Devices;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;

namespace NetLine.ApiService.Services;

public class DeviceManager : IDeviceManager
{
    private readonly AppDbContext _db;
    private readonly ISNMPService _snmp;
    private readonly IICMPService _ping;

    public DeviceManager(AppDbContext db, ISNMPService snmp, IICMPService ping)
    {
        _db = db;
        _snmp = snmp;
        _ping = ping;
    }

    public async Task<IEnumerable<DeviceInfo>> GetAllAsync()
        => await _db.DevicesInfo.ToListAsync();

    public async Task<DeviceScanResult> ScanAsync(string ip)
    {
        var pingMs = await _ping.GetPingResponseTimeAsync(ip);
        var snmpResult = await _snmp.GetDeviceInfoAsync(ip);

        return new DeviceScanResult(0, ip, pingMs, snmpResult);
    }

    public async Task<DeviceInfo> AddAsync(AddDeviceRequest request)
    {
        var exists = await _db.DevicesInfo.AnyAsync(d => d.IpAddress == request.Ip);
        if (exists)
            throw new InvalidOperationException("This IP is already in the database.");

        try
        {
            var pingMs = await _ping.GetPingResponseTimeAsync(request.Ip);
            var snmpScan = await _snmp.GetDeviceInfoAsync(request.Ip);

            var status = pingMs.HasValue
                ? (snmpScan.Success ? "Online" : "Limited")
                : "Offline";

            var device = new DeviceInfo
            {
                IpAddress = request.Ip,
                UserDefinedName = request.UserLabel,
                DeviceType = request.Type,
                Status = status,
                PingResponseTimeMs = pingMs,
                SysName = snmpScan.Name,
                SysDescr = snmpScan.Description,
                SysLocation = snmpScan.Location,
                SysContact = snmpScan.Contact,
                SysUpTime = snmpScan.UpTime,
                SysInterfacesCount = snmpScan.InterfacesCount,
                LastScanned = DateTime.UtcNow
            };

            _db.DevicesInfo.Add(device);
            await _db.SaveChangesAsync();

            return device;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to add device at {request.Ip}. The device may not be reachable or SNMP may not be enabled. Details: {ex.Message}",
                ex);
        }
    }
}