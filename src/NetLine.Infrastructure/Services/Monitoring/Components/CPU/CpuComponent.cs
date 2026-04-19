using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Monitoring.Components.CPU;
/// </summary>
public sealed class CpuComponent : IMonitoringComponent
{
    private readonly ISnmpClient _snmp;
    private readonly ILogger<CpuComponent> _logger;
    public CpuComponent(ISnmpClient snmp, ILogger<CpuComponent> logger)
    {
        _snmp = snmp;
        _logger = logger;
    }

    public MonitoringCategory Category => MonitoringCategory.Cpu;
    public string Name => "CPU";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        double? usageAvg = null;

        var loadTable = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.HrProcessorLoadTable, cancellationToken);
        if (loadTable.Count > 0)
        {
            var values = new List<double>();
            foreach (var v in loadTable)
            {
                if (int.TryParse(v.Data.ToString(), out var load))
                {
                    values.Add(load);
                }
            }

            if (values.Count > 0)
            {
                usageAvg = values.Average();
            }
        }

        if (usageAvg is null)
        {
            var ucd = await _snmp.GetAsync(device.IpAddress, new[] { OIDDictionary.SsCpuIdle }, cancellationToken);
            if (ucd is not null && double.TryParse(ucd[0].Data.ToString(), out var idle))
            {
                usageAvg = Math.Max(0, 100 - idle);
            }
        }

        if (usageAvg is null)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "No CPU metrics available");
        }

        var metrics = new List<ComponentMetric>
        {
            ComponentMetric.Numeric("cpu.usage_avg", Math.Round(usageAvg.Value, 2), "%", "Average CPU")
        };

        _logger.LogDebug("CPU component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }
}
