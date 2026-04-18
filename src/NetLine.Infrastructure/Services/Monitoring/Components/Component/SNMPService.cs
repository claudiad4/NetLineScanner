using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

// Zmieniono przestrzeń nazw na spójną z resztą komponentów
namespace NetLine.Infrastructure.Services.Monitoring.Components.System;

/// <summary>
/// Pobiera podstawowe informacje systemowe urządzenia za pomocą protokołu SNMP.
/// </summary>
public sealed class SystemInfoComponent : IMonitoringComponent
{
    private readonly ISnmpClient _snmp;
    private readonly ILogger<SystemInfoComponent> _logger;

    public SystemInfoComponent(ISnmpClient snmp, ILogger<SystemInfoComponent> logger)
    {
        _snmp = snmp;
        _logger = logger;
    }

    // Zakładam, że w MonitoringCategory istnieje kategoria "System" (lub użyj właściwej)
    public MonitoringCategory Category => MonitoringCategory.Component;
    public string Name => "Component";

    public async Task<ComponentResult> CollectAsync(DeviceInfo device, CancellationToken cancellationToken)
    {
        var metrics = new List<ComponentMetric>();

        // 1. Zdefiniowanie listy OID
        var oids = new[]
        {
            OIDDictionary.SysDescr,
            OIDDictionary.SysName,
            OIDDictionary.SysLocation,
            OIDDictionary.SysContact,
            OIDDictionary.SysUpTime,
            OIDDictionary.SysInterfacesCount
        };

        // 2. Użycie klienta SNMP wstrzykniętego z zewnątrz
        var snmpData = await _snmp.GetAsync(device.IpAddress, oids, cancellationToken);

        // 3. Obsługa braku odpowiedzi (zastępuje długie bloki try-catch)
        if (snmpData is null || snmpData.Count == 0)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "System OIDs unavailable or SNMP timeout");
        }

        // 4. Mapowanie wyników do listy metryk
        TryAddTextMetric(metrics, snmpData, 0, "sys.descr", "Description");
        TryAddTextMetric(metrics, snmpData, 1, "sys.name", "Name");
        TryAddTextMetric(metrics, snmpData, 2, "sys.location", "Location");
        TryAddTextMetric(metrics, snmpData, 3, "sys.contact", "Contact");
        TryAddTextMetric(metrics, snmpData, 4, "sys.uptime", "UpTime");

        // SysInterfacesCount jest liczbą, więc traktujemy ją inaczej
        if (snmpData.Count > 5 && int.TryParse(snmpData[5]?.Data?.ToString(), out var ifCount))
        {
            metrics.Add(ComponentMetric.Numeric("sys.interfaces_count", ifCount, "count", "Interfaces Count"));
        }

        if (metrics.Count == 0)
        {
            return ComponentResult.Fail(device.Id, Category, Name, "Failed to parse system metrics");
        }

        _logger.LogDebug("SystemInfo component collected {Count} metrics for {Device}", metrics.Count, device.IpAddress);
        return ComponentResult.Ok(device.Id, Category, Name, metrics);
    }

    // Metoda pomocnicza do bezpiecznego dodawania metryk tekstowych
    private static void TryAddTextMetric(List<ComponentMetric> metrics, IList<Lextm.SharpSnmpLib.Variable> data, int index, string key, string label)
    {
        if (data.Count > index && data[index]?.Data != null)
        {
            var textValue = data[index].Data.ToString();

            // Ignorowanie błędów SNMP, gdy dany OID nie jest wspierany na urządzeniu
            if (!string.IsNullOrWhiteSpace(textValue) && textValue != "NoSuchObject" && textValue != "NoSuchInstance")
            {
                metrics.Add(new ComponentMetric(
                    Key: key,
                    NumericValue: 0,
                    TextValue: textValue,
                    Unit: "text",
                    Label: label));
            }
        }
    }
}