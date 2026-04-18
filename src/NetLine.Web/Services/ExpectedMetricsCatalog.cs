using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

public sealed record ExpectedMetric(
    MonitoringCategory Category,
    string Key,
    string Label,
    string? Unit = null);

/// <summary>
/// Canonical list of parameters the scanner tries to collect from a device.
/// The device details view uses it to render every expected row, falling back
/// to "unavailable, no data from device" when the device did not return a value.
/// Dynamic per-index metrics (CPU cores, interfaces) are not listed here and
/// are shown only when reported by the device.
/// </summary>
public static class ExpectedMetricsCatalog
{
    public static readonly IReadOnlyList<ExpectedMetric> All = new ExpectedMetric[]
    {
        new(MonitoringCategory.System, "system.descr", "Description"),
        new(MonitoringCategory.System, "system.name", "Host name"),
        new(MonitoringCategory.System, "system.location", "Location"),
        new(MonitoringCategory.System, "system.contact", "Contact"),
        new(MonitoringCategory.System, "system.uptime", "Uptime"),
        new(MonitoringCategory.System, "system.os_family", "OS family"),
        new(MonitoringCategory.System, "system.os_version", "OS version"),
        new(MonitoringCategory.System, "system.users", "Logged users", "count"),
        new(MonitoringCategory.System, "system.processes", "Processes", "count"),
        new(MonitoringCategory.System, "system.load_1", "Load 1 min", "load"),
        new(MonitoringCategory.System, "system.load_5", "Load 5 min", "load"),
        new(MonitoringCategory.System, "system.load_15", "Load 15 min", "load"),

        new(MonitoringCategory.Cpu, "cpu.usage_avg", "Average CPU", "%"),
        new(MonitoringCategory.Cpu, "cpu.core_count", "Cores", "count"),
        new(MonitoringCategory.Cpu, "cpu.user", "User", "%"),
        new(MonitoringCategory.Cpu, "cpu.system", "System", "%"),
        new(MonitoringCategory.Cpu, "cpu.idle", "Idle", "%"),

        new(MonitoringCategory.Memory, "memory.total_mb", "Total RAM", "MB"),
        new(MonitoringCategory.Memory, "memory.free_mb", "Free RAM", "MB"),
        new(MonitoringCategory.Memory, "memory.used_mb", "Used RAM", "MB"),
        new(MonitoringCategory.Memory, "memory.usage_pct", "Usage %", "%"),

        new(MonitoringCategory.Network, "ping.probes", "Probes sent", "count"),
        new(MonitoringCategory.Network, "ping.failed", "Failed probes", "count"),
        new(MonitoringCategory.Network, "ping.loss_pct", "Packet loss", "%"),
        new(MonitoringCategory.Network, "ping.rtt_avg_ms", "Avg RTT", "ms"),
        new(MonitoringCategory.Network, "ping.rtt_min_ms", "Min RTT", "ms"),
        new(MonitoringCategory.Network, "ping.rtt_max_ms", "Max RTT", "ms"),
        new(MonitoringCategory.Network, "ping.jitter_ms", "Jitter", "ms"),
        new(MonitoringCategory.Network, "ping.reachable", "Reachable"),

        new(MonitoringCategory.Network, "dns.resolved", "DNS resolved"),
        new(MonitoringCategory.Network, "dns.hostname", "Hostname"),
        new(MonitoringCategory.Network, "dns.response_ms", "DNS response", "ms"),

        new(MonitoringCategory.Network, "port.21.open", "FTP (21)", "state"),
        new(MonitoringCategory.Network, "port.22.open", "SSH (22)", "state"),
        new(MonitoringCategory.Network, "port.23.open", "Telnet (23)", "state"),
        new(MonitoringCategory.Network, "port.25.open", "SMTP (25)", "state"),
        new(MonitoringCategory.Network, "port.53.open", "DNS (53)", "state"),
        new(MonitoringCategory.Network, "port.80.open", "HTTP (80)", "state"),
        new(MonitoringCategory.Network, "port.110.open", "POP3 (110)", "state"),
        new(MonitoringCategory.Network, "port.139.open", "NetBIOS (139)", "state"),
        new(MonitoringCategory.Network, "port.143.open", "IMAP (143)", "state"),
        new(MonitoringCategory.Network, "port.443.open", "HTTPS (443)", "state"),
        new(MonitoringCategory.Network, "port.445.open", "SMB (445)", "state"),
        new(MonitoringCategory.Network, "port.3306.open", "MySQL (3306)", "state"),
        new(MonitoringCategory.Network, "port.3389.open", "RDP (3389)", "state"),
        new(MonitoringCategory.Network, "port.5432.open", "PostgreSQL (5432)", "state"),
        new(MonitoringCategory.Network, "port.8080.open", "HTTP-Alt (8080)", "state"),
        new(MonitoringCategory.Network, "port.open_count", "Open ports", "count"),

        new(MonitoringCategory.Network, "net.if.total", "Interfaces", "count"),
        new(MonitoringCategory.Network, "net.if.up", "Interfaces up", "count"),
        new(MonitoringCategory.Network, "net.if.down", "Interfaces down", "count"),
    };

    public static readonly HashSet<string> AllKeys =
        new(All.Select(e => e.Key), StringComparer.Ordinal);
}
