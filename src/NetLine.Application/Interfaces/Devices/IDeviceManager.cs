using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Application.Interfaces.Devices
{
    public interface IDeviceManager
    {
        Task<IEnumerable<DeviceInfo>> GetAllAsync();
        Task<DeviceInfo> AddAsync(AddDeviceRequest request);
        Task<DeviceScanResult> ScanAsync(string ip);
        Task<DeviceScanResult> ScanNowAsync(int deviceId, CancellationToken cancellationToken);
    }
}
