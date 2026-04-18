using Lextm.SharpSnmpLib;

namespace NetLine.Application.Interfaces.Monitoring;

/// <summary>
/// Async SNMP Get/Walk primitives used by monitoring components. Abstracts the
/// concrete SharpSnmpLib-based client so components can be unit-tested in isolation.
/// </summary>
public interface ISnmpClient
{
    Task<IReadOnlyList<Variable>?> GetAsync(string ipAddress, IEnumerable<string> oids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Variable>> WalkAsync(string ipAddress, string rootOid, CancellationToken cancellationToken = default);
}
