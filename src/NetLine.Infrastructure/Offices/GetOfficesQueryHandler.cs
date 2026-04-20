using Microsoft.EntityFrameworkCore;
using NetLine.Application.DTO;
using NetLine.Application.Interfaces.Identity;
using NetLine.Application.Offices.Queries;
using NetLine.Infrastructure.Data;

namespace NetLine.Infrastructure.Offices;

public class GetOfficesQueryHandler : IGetOfficesQueryHandler
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOfficesQueryHandler(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OfficeDto>> HandleAsync(
        GetOfficesQuery query,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Array.Empty<OfficeDto>();

        IQueryable<Domain.Entities.Office> source = _db.Offices.AsNoTracking();

        if (_currentUser.IsGlobalAdmin)
        {
            // full access — no filter
        }
        else if (_currentUser.IsOfficeAdmin)
        {
            var managed = await _currentUser.GetManagedOfficeIdsAsync(ct);
            if (managed.Count == 0)
                return Array.Empty<OfficeDto>();

            source = source.Where(o => managed.Contains(o.Id));
        }
        else if (_currentUser.IsUser)
        {
            var officeId = _currentUser.OfficeId;
            if (officeId is null)
                return Array.Empty<OfficeDto>();

            source = source.Where(o => o.Id == officeId);
        }
        else
        {
            return Array.Empty<OfficeDto>();
        }

        return await source
            .OrderBy(o => o.Name)
            .Select(o => new OfficeDto(o.Id, o.Name, o.Location, o.CreatedAt))
            .ToListAsync(ct);
    }
}
