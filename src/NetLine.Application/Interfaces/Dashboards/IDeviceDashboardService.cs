using NetLine.Application.DTO.Dashboards;

namespace NetLine.Application.Interfaces.Dashboards;

public interface IDeviceDashboardService
{
    Task<DeviceDashboardDto> GetDeviceDashboardAsync(int deviceInfoId, CancellationToken cancellationToken = default);
}
