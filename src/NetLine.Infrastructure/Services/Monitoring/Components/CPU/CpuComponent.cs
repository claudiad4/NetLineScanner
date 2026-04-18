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
        var metrics = new List<ComponentMetric>();

        var loadTable = await _snmp.WalkAsync(device.IpAddress, OIDDictionary.HrProcessorLoadTable, cancellationToken);
        if (loadTable.Count > 0)
        {
            var values = new List<double>();
            var coreIndex = 0;
            foreach (var v in loadTable)
            {
                if (int.TryParse(v.Data.ToString(), out var load))
                {
                    values.Add(load);
                    metrics.Add(ComponentMetric.Numeric($"cpu.core_{coreIndex}", load, "%", $"Core {coreIndex}"));
                    coreIndex++;
                }
            }

            if (values.Count > 0)
            {
                metrics.Add(ComponentMetric.Numeric("cpu.usage_avg", values.Average(), "%", "Average CPU"));
                metrics.Add(ComponentMetric.Numeric("cpu.core_count", values.Count, "count", "Cores"));
            }
        }

        var ucd = await _snmp.GetAsync(device.IpAddress,
            new[] { OIDDictionary.SsCpuUser, OIDDictionary.SsCpuSystem, OIDDictionary.SsCpuIdle },
            cancellationToken);

        if (ucd is not null)
        {
            TryAddPercent(metrics, "cpu.user", ucd[0].Data.ToString(), "User");
            TryAddPercent(metrics, "cpu.system", ucd[1].Data.ToString(), "System");
            TryAddPercent(metrics, "cpu.idle", ucd[2].Data.ToString(), "Idle");

            if (!metrics.Any(m => m.Key == "cpu.usage_avg")
                && double.TryParse(ucd[2].Data.ToString(), out var idle))
            {
                metrics.Add(ComponentMetric.Numeric("cpu.usage_avg", Math.Max(0, 100 - idle), "%", "Average CPU"));
            }
        }

        if (metrics.Count == 0)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "No CPU metrics available");
        }

        _logger.LogDebug("CPU component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    private static void TryAddPercent(List<ComponentMetric> metrics, string key, string? raw, string label)
    {
        if (int.TryParse(raw, out var value))
        {
            metrics.Add(ComponentMetric.Numeric(key, value, "%", label));
        }
    }
}
