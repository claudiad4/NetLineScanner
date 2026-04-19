namespace NetLine.Domain.Entities;

/// <summary>
/// Functional category a monitoring component belongs to.
/// Mirrors the user-facing taxonomy (CPU, Memory, Network, ...).
/// </summary>
public enum MonitoringCategory
{
    Component,
    Cpu,
    Health,
    Memory,
    Network,
    Raw,
    System
}

/// <summary>
/// How often a monitoring component should run.
/// Light = cheap probes (ping), Medium = SNMP queries, Heavy = expensive scans (port scan).
/// </summary>
public enum ScanFrequency
{
    Light,
    Medium,
    Heavy
}

/// <summary>
/// Single source of truth for tier intervals, shared by the scanner (to decide
/// what is due) and the device entity (to compute NextScanAt for the UI).
/// </summary>
public static class ScanIntervals
{
    public static readonly TimeSpan Light = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Heavy = TimeSpan.FromHours(6);

    public static TimeSpan For(ScanFrequency frequency) => frequency switch
    {
        ScanFrequency.Light => Light,
        ScanFrequency.Medium => Medium,
        ScanFrequency.Heavy => Heavy,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
    };
}
