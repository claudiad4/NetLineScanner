using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Components.Shared.Offices;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages.Offices;

public partial class OfficesDetails : ComponentBase, IAsyncDisposable
{
    [Inject] protected OfficeApiClient OfficeClient { get; set; } = default!;
    [Inject] protected DeviceApiClient DeviceClient { get; set; } = default!;
    [Inject] protected AlertApiClient AlertClient { get; set; } = default!;
    [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected DeviceHubClient Hub { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private Office? office;
    private List<DeviceInfo>? devices;
    private List<DeviceAlert>? alerts;
    private int? expandedDeviceId;

    private bool showForm;
    private bool showOfficeEdit;
    private DeviceInfo? editingDevice;

    private OfficeEditForm? officeEditForm;

    private CurrentUserInfo? user;
    private bool isAdmin;
    private bool accessDenied;

    protected override async Task OnInitializedAsync()
    {
        user = await CurrentUser.GetAsync();
        isAdmin = user?.IsAdmin == true;

        if (!isAdmin && user?.OfficeId != Id)
        {
            accessDenied = true;
            return;
        }

        await LoadData();

        Hub.OnDeviceStatusUpdated += HandleStatusUpdated;
        await Hub.StartAsync();
    }

    private void ToggleDetails(int id)
        => expandedDeviceId = (expandedDeviceId == id) ? null : id;

    private void EditDevice(DeviceInfo device)
    {
        if (!isAdmin) return;
        editingDevice = device;
        showForm = true;
    }

    private async Task DeleteDevice(int id)
    {
        if (!isAdmin) return;
        await DeviceClient.DeleteDeviceAsync(id);
        await LoadData();
    }

    private void ViewDeviceCharts(int deviceId)
        => Nav.NavigateTo($"/devices/{deviceId}");

    private async Task OnDeviceFormSuccess()
    {
        showForm = false;
        editingDevice = null;
        await LoadData();
    }

    private Task OnDeviceFormCancel()
    {
        showForm = false;
        editingDevice = null;
        return Task.CompletedTask;
    }

    private void ToggleOfficeEditForm()
    {
        if (!isAdmin || office is null) return;
        showOfficeEdit = !showOfficeEdit;
    }

    private Task HandleOfficeEditCancel()
    {
        showOfficeEdit = false;
        return Task.CompletedTask;
    }

    private async Task HandleOfficeSave((string Name, string? Location) edit)
    {
        if (!isAdmin) return;

        var (success, error) = await OfficeClient.UpdateOfficeAsync(Id, edit.Name, edit.Location);
        if (success)
        {
            showOfficeEdit = false;
            await LoadData();
        }
        else
        {
            officeEditForm?.SetError(error ?? "Nie udało się zapisać biura.");
        }
    }

    private async Task HandleStatusUpdated()
    {
        await LoadData();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadData()
    {
        try
        {
            office = await OfficeClient.GetOfficeAsync(Id);
            devices = await DeviceClient.GetDevicesByOfficeAsync(Id);
            alerts = await AlertClient.GetAlertsAsync(officeId: Id, limit: 50);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }

    public ValueTask DisposeAsync()
    {
        Hub.OnDeviceStatusUpdated -= HandleStatusUpdated;
        return ValueTask.CompletedTask;
    }
}
