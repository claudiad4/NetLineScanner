using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages;

public partial class DeviceDetails : ComponentBase
{
    private const string NoDataText = "Brak danych od urządzenia";

    [Parameter] public int Id { get; set; }

    private DeviceInfo? device;
    private List<DeviceAlert> alerts = [];
    private List<DeviceMetric> metrics = [];
    private CurrentUserInfo? user;
    private bool isAdmin;
    private bool accessDenied;
    private string activeTab = "system";

    private record TabDefinition(string Key, string Label, string Icon, int? Badge = null);

    private IEnumerable<TabDefinition> availableTabs => new List<TabDefinition>
    {
        new("system", "System", "bi-info-square"),
        new(nameof(MonitoringCategory.Cpu), "CPU", "bi-cpu"),
        new(nameof(MonitoringCategory.Memory), "Pamięć", "bi-memory"),
        new(nameof(MonitoringCategory.Network), "Sieć", "bi-diagram-3"),
        new(nameof(MonitoringCategory.Component), "Komponenty", "bi-hdd-stack"),
        new(nameof(MonitoringCategory.Health), "Stan", "bi-heart-pulse"),
        new(nameof(MonitoringCategory.Power), "Zasilanie", "bi-plug"),
        new(nameof(MonitoringCategory.Raw), "Surowe", "bi-braces"),
        new("alerts", "Alerty", "bi-bell", alerts.Count),
    };

    protected override async Task OnInitializedAsync()
    {
        user = await CurrentUser.GetAsync();
        isAdmin = user?.IsAdmin == true;

        try
        {
            device = await ApiClient.GetDeviceAsync(Id);

            if (device is null)
            {
                return;
            }

            if (!isAdmin && device.OfficeId != user?.OfficeId)
            {
                accessDenied = true;
                device = null;
                return;
            }

            alerts = await AlertClient.GetAlertsAsync(deviceId: Id, limit: 50);
            metrics = await ApiClient.GetLatestMetricsAsync(Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }

    private void GoBack()
    {
        if (device?.OfficeId != null)
            Nav.NavigateTo($"/offices/{device.OfficeId}");
        else if (isAdmin)
            Nav.NavigateTo("/offices");
        else
            Nav.NavigateTo("/devices");
    }

    private static string GetIconForType(string? type) => type switch
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

    private static string GetStatusClass(string? status) => status switch
    {
        "Online" => "bg-success",
        "Limited" => "bg-warning text-dark",
        "Offline" => "bg-danger",
        _ => "bg-secondary"
    };

    private static string GetAlertIcon(AlertType type) => type switch
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

    private static string GetAlertColor(AlertType type) => type switch
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

    private static MarkupString RenderOrPlaceholder(string? value, bool small)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new MarkupString($"<span class=\"text-muted fst-italic\">{System.Net.WebUtility.HtmlEncode(NoDataText)}</span>");
        }

        var encoded = System.Net.WebUtility.HtmlEncode(value);
        return new MarkupString(small ? $"<small>{encoded}</small>" : encoded);
    }
}
