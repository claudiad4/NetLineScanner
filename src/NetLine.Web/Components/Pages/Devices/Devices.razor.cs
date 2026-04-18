using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages.Devices;

public partial class Devices : ComponentBase, IAsyncDisposable
{
    [Inject] protected DeviceApiClient ApiClient { get; set; } = default!;
    [Inject] protected AlertNotificationService AlertService { get; set; } = default!;
    [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] protected DeviceHubClient Hub { get; set; } = default!;

    private List<DeviceInfo>? devices;
    private int? expandedDeviceId;

    private CurrentUserInfo? user;
    private bool isAdmin;
    private int? scopedOfficeId;

    protected override async Task OnInitializedAsync()
    {
        user = await CurrentUser.GetAsync();
        isAdmin = user?.IsAdmin == true;
        scopedOfficeId = isAdmin ? null : user?.OfficeId;

        await LoadData();

        Hub.OnDeviceStatusUpdated += HandleStatusUpdated;
        Hub.OnAlertCreated += HandleAlertCreated;
        await Hub.StartAsync();
    }

    private void ToggleDetails(int id)
        => expandedDeviceId = (expandedDeviceId == id) ? null : id;

    private async Task HandleStatusUpdated()
    {
        await LoadData();
        await InvokeAsync(StateHasChanged);
    }

    private Task HandleAlertCreated(DeviceAlert alert)
    {
        if (isAdmin || alert.Device?.OfficeId == user?.OfficeId)
        {
            AlertService.AddNotification(alert);
            return InvokeAsync(StateHasChanged);
        }
        return Task.CompletedTask;
    }

    private async Task LoadData()
    {
        try
        {
            if (isAdmin)
            {
                devices = await ApiClient.GetDevicesAsync();
            }
            else if (user?.OfficeId is int officeId)
            {
                devices = await ApiClient.GetDevicesByOfficeAsync(officeId);
            }
            else
            {
                devices = [];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Hub.OnDeviceStatusUpdated -= HandleStatusUpdated;
        Hub.OnAlertCreated -= HandleAlertCreated;
        await Hub.DisposeAsync();
    }
}
