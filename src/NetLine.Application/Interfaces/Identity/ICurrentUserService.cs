namespace NetLine.Application.Interfaces.Identity;

/// <summary>
/// Application-level abstraction over the authenticated principal.
/// Implemented in Infrastructure (reads from ClaimsPrincipal / DB).
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }

    bool IsGlobalAdmin { get; }
    bool IsOfficeAdmin { get; }
    bool IsUser { get; }

    /// <summary>Office assigned to a regular User (null otherwise).</summary>
    int? OfficeId { get; }

    /// <summary>Offices managed by an OfficeAdmin (empty otherwise).</summary>
    Task<IReadOnlyCollection<int>> GetManagedOfficeIdsAsync(CancellationToken ct = default);
}
