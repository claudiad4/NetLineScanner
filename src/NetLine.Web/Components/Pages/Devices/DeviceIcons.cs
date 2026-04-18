using NetLine.Domain.Entities;

namespace NetLine.Web.Components.Pages.Devices
{
    public static class DeviceIcons
    {
        public static string GetIconForType(string? type) => type switch
        {
            "Computer" => "bi-pc-display",
            "Laptop" => "bi-laptop",
            "Server" => "bi-hdd-network",
            "Switch" => "bi-router",
            "Printer" => "bi-printer",
            "Smartphone" => "bi-phone",
            "Tablet" => "bi-tablet",
            _ => "bi-three-dots"
        };

        public static string GetStatusClass(string? status) => status switch
        {
            "Online" => "bg-success",
            "Limited" => "bg-warning text-dark",
            "Offline" => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetIcon(this AlertType type) => type switch
        {
            AlertType.WentOffline => "bi-x-circle-fill",
            AlertType.CameOnline => "bi-check-circle-fill",
            AlertType.HighLatency => "bi-exclamation-circle-fill",
            AlertType.HighPacketLoss => "bi-reception-0",
            AlertType.HighCpuUsage => "bi-cpu-fill",
            AlertType.HighMemoryUsage => "bi-memory",
            AlertType.InterfaceDown => "bi-ethernet",
            AlertType.ComponentFailure => "bi-tools",
            _ => "bi-info-circle-fill"
        };

        public static string GetColorClass(this AlertType type) => type switch
        {
            AlertType.WentOffline => "text-danger",
            AlertType.CameOnline => "text-success",
            AlertType.HighLatency => "text-warning",
            AlertType.HighPacketLoss => "text-warning",
            AlertType.HighCpuUsage => "text-danger",
            AlertType.HighMemoryUsage => "text-danger",
            AlertType.InterfaceDown => "text-warning",
            AlertType.ComponentFailure => "text-secondary",
            _ => "text-info"
        };
    }
}