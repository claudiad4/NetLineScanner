using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Infrastructure.Services.Scanning;

/// <summary>
/// Nowoczesny silnik skanuj¹cy. 
/// Równolegle uruchamia wszystkie komponenty monitoruj¹ce i zwraca czyst¹ listê metryk.
/// Zlikwidowano wsteczn¹ kompatybilnoœæ z SNMPScanResult.
/// </summary>
public class DeviceScanner : IDeviceScanner
{
    private readonly IReadOnlyList<IMonitoringComponent> _components;
    private readonly ILogger<DeviceScanner> _logger;

    public DeviceScanner(IEnumerable<IMonitoringComponent> components, ILogger<DeviceScanner> logger)
    {
        _components = components.ToList();
        _logger = logger;
    }

    public async Task<IReadOnlyList<ModernDeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken)
    {
        // 1. Zmiana: U¿ywamy bezpiecznej dla w¹tków kolekcji ConcurrentBag zamiast standardowej listy i blokad (lock)
        var scanResults = new ConcurrentBag<DeviceScanResult>();

        await Parallel.ForEachAsync(
            devices,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (device, ct) =>
            {
                var componentResults = await RunComponentsAsync(device, ct);

                // 2. Zmiana: Od razu wrzucamy wynik do worka, bez rêcznego budowania starych struktur
                scanResults.Add(new ModernDeviceScanResult(device.Id, device.IpAddress, componentResults));
            });

        return scanResults.ToList().AsReadOnly();
    }

    private async Task<IReadOnlyList<ComponentResult>> RunComponentsAsync(DeviceInfo device, CancellationToken ct)
    {
        var tasks = _components.Select(async c =>
        {
            try
            {
                return await c.CollectAsync(device, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Component {Component} failed for device {DeviceId}", c.Name, device.Id);
                return ComponentResult.Fail(device.Id, c.Category, c.Name, ex.Message);
            }
        });

        return await Task.WhenAll(tasks);
    }
}

// 3. Zmiana: Definiujemy nowy, ultralekki obiekt wymiany danych u¿ywaj¹c C# Records
public record ModernDeviceScanResult(
    int DeviceId,
    string IpAddress,
    IReadOnlyList<ComponentResult> ComponentResults
)
{
    // Opcjonalny pomocnik: "sp³aszcza" wszystkie metryki ze wszystkich komponentów do jednej listy
    public IEnumerable<ComponentMetric> AllMetrics => ComponentResults.SelectMany(c => c.Metrics);

    // Opcjonalny pomocnik: szybko sprawdza czy jakikolwiek skaner zanotowa³ sukces
    public bool IsOnline => ComponentResults.Any(c => c.Success);
}