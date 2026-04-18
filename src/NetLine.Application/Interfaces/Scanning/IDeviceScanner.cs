using NetLine.Domain.Entities;
using NetLine.Domain.Models;

namespace NetLine.Application.Interfaces.Scanning;

/// <summary>
/// Odpowiada za równoleg³e, asynchroniczne skanowanie urz¹dzeñ.
/// Uruchamia wszystkie zarejestrowane komponenty monitoruj¹ce (IMonitoringComponent) 
/// dla ka¿dego urz¹dzenia w tym samym czasie.
/// </summary>
public interface IDeviceScanner
{
    /// <summary>
    /// Skanuje listê urz¹dzeñ, zbieraj¹c od nich uniwersalne metryki.
    /// </summary>
    /// <param name="devices">Lista urz¹dzeñ do przeskanowania</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Lista wyników skanowania zawieraj¹ca zebrane metryki</returns>
    Task<IReadOnlyList<DeviceScanResult>> ScanDevicesAsync(
        IEnumerable<DeviceInfo> devices,
        CancellationToken cancellationToken);
}