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
