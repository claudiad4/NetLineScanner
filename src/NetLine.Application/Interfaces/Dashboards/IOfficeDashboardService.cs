using NetLine.Application.DTO.Dashboards;

namespace NetLine.Application.Interfaces.Dashboards;

public interface IOfficeDashboardService
{
    Task<OfficeDashboardDto> GetOfficeDashboardAsync(int officeId, CancellationToken cancellationToken = default);
}
