using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System;
using Lextm.SharpSnmpLib;
namespace NetLine.Application.Interfaces.Monitoring;

public interface ISnmpClient
{
    Task<IReadOnlyList<Variable>?> GetAsync(string ipAddress, IEnumerable<string> oids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Variable>> WalkAsync(string ipAddress, string rootOid, CancellationToken cancellationToken = default);
}
