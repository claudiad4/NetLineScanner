using NetLine.Application.DTO;

namespace NetLine.Application.Offices.Queries;

public sealed record GetOfficesQuery;

public interface IGetOfficesQueryHandler
{
    Task<IReadOnlyList<OfficeDto>> HandleAsync(
        GetOfficesQuery query,
        CancellationToken ct = default);
}
