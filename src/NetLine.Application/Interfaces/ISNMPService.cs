using NetLine.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Application.Interfaces
{
    public interface ISNMPService
    {
        Task<SNMPScanResult> GetDeviceInfoAsync(string ipAddress);
    }
}
