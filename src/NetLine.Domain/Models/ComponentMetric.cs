namespace NetLine.Domain.Models;

/// <summary>
/// A single value produced by a monitoring component during one collection cycle.
/// Either <see cref="NumericValue"/> or <see cref="TextValue"/> is populated depending on metric kind.
/// </summary>
public sealed record ComponentMetric(
    string Key,
    double? NumericValue,
    string? TextValue,
    string? Unit = null,
    string? Label = null)
{
    public static ComponentMetric Numeric(string key, double value, string? unit = null, string? label = null)
        => new(key, value, null, unit, label);

    public static ComponentMetric Text(string key, string value, string? label = null)
        => new(key, null, value, null, label);
}
