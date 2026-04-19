using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using NetLine.Web.Services;
using NetLine.Domain.Entities;
using NetLine.Web.Components.Shared.Devices;

namespace NetLine.Web.Components.Pages.Devices
{
    public partial class DeviceDetails : ComponentBase, IAsyncDisposable
    {
        [Inject] protected DeviceApiClient ApiClient { get; set; } = default!;
        [Inject] protected AlertApiClient AlertClient { get; set; } = default!;
        [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected DeviceHubClient Hub { get; set; } = default!;

        [Parameter] public int Id { get; set; }

        protected const string NoDataText = "Brak danych od urządzenia";

        protected DeviceInfo? device;
        protected List<DeviceAlert> alerts = [];
        protected List<DeviceMetric> metrics = [];
        protected CurrentUserInfo? user;
        protected bool isAdmin;
        protected bool accessDenied;
        protected string activeTab = "system";
        protected NetworkSubCategory selectedNetworkSub = NetworkSubCategory.All;

        protected record TabDefinition(string Key, string Label, string Icon, int? Badge = null);

        protected IEnumerable<TabDefinition> availableTabs => new List<TabDefinition>
        {
            new("system", "System", "bi-info-square"),
            new(nameof(MonitoringCategory.Cpu), "CPU", "bi-cpu"),
            new(nameof(MonitoringCategory.Memory), "Pamięć", "bi-memory"),
            new(nameof(MonitoringCategory.Network), "Sieć", "bi-diagram-3"),
            new(nameof(MonitoringCategory.Component), "Komponenty", "bi-hdd-stack"),
            new(nameof(MonitoringCategory.Health), "Stan", "bi-heart-pulse"),
            new(nameof(MonitoringCategory.Raw), "Surowe", "bi-braces"),
            new("alerts", "Alerty", "bi-bell", alerts.Count),
        };

        protected List<DeviceMetric> MetricsFor(MonitoringCategory category)
            => metrics.Where(m => m.Category == category).OrderBy(m => m.MetricKey).ToList();

        protected override async Task OnInitializedAsync()
        {
            user = await CurrentUser.GetAsync();
            isAdmin = user?.IsAdmin == true;

            await LoadAsync();

            if (accessDenied) return;

            Hub.OnDeviceStatusUpdated += HandleStatusUpdated;
            Hub.OnAlertCreated += HandleAlertCreated;
            await Hub.StartAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                device = await ApiClient.GetDeviceAsync(Id);

                if (device is null) return;

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

        private async Task HandleStatusUpdated()
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }

        private Task HandleAlertCreated(DeviceAlert alert)
        {
            if (alert.DeviceInfoId != Id) return Task.CompletedTask;
            alerts.Insert(0, alert);
            return InvokeAsync(StateHasChanged);
        }

        public ValueTask DisposeAsync()
        {
            Hub.OnDeviceStatusUpdated -= HandleStatusUpdated;
            Hub.OnAlertCreated -= HandleAlertCreated;
            return ValueTask.CompletedTask;
        }

        protected void GoBack()
        {
            if (device?.OfficeId != null)
                Nav.NavigateTo($"/offices/{device.OfficeId}");
            else if (isAdmin)
                Nav.NavigateTo("/offices");
            else
                Nav.NavigateTo("/devices");
        }

        protected MarkupString RenderOrPlaceholder(string? value, bool small)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new MarkupString($"<span class=\"text-muted fst-italic\">{System.Net.WebUtility.HtmlEncode(NoDataText)}</span>");
            }

            var encoded = System.Net.WebUtility.HtmlEncode(value);
            return new MarkupString(small ? $"<small>{encoded}</small>" : encoded);
        }
    }
}
