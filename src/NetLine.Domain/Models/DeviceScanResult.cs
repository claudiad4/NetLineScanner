using NetLine.Domain.Entities;

namespace NetLine.Domain.Models;

/// <summary>
/// Nowoczesny, niemutowalny obiekt przechowuj¹cy wynik skanowania urz¹dzenia.
/// Zawiera wy³¹cznie listê rezultatów z poszczególnych wtyczek (komponentów).
/// </summary>
public record DeviceScanResult(
    int DeviceId,
    string IpAddress,
    IReadOnlyList<ComponentResult> ComponentResults
)
{
    /// <summary>
    /// Pomocnicza w³aœciwoœæ: Sp³aszcza wszystkie metryki ze wszystkich komponentów do jednej, p³askiej listy.
    /// Idealne do szybkiego zapisu w bazie danych!
    /// </summary>
    public IEnumerable<ComponentMetric> AllMetrics => ComponentResults.SelectMany(c => c.Metrics);

    /// <summary>
    /// Pomocnicza w³aœciwoœæ: Jeœli jakikolwiek komponent odniós³ sukces (np. Ping, Skaner Portów, SNMP),
    /// uznajemy, ¿e urz¹dzenie "¿yje" w sieci.
    /// </summary>
    public bool IsOnline => ComponentResults.Any(c => c.Success);
}