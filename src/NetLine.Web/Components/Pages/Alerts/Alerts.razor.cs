using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages.Alerts;

public partial class Alerts : ComponentBase, IAsyncDisposable
{
    [Inject] protected AlertApiClient ApiClient { get; set; } = default!;
    [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] protected DeviceHubClient Hub { get; set; } = default!;

    private List<DeviceAlert>? alerts;
    private AlertType? selectedFilter;
    private string? errorMessage;
    private bool isLoading;
    private CurrentUserInfo? user;
    private bool isAdmin;
    private int? scopedOfficeId;

    private IEnumerable<DeviceAlert> filteredAlerts
        => selectedFilter == null
            ? (alerts ?? [])
            : (alerts?.Where(a => a.Type == selectedFilter) ?? []);

    protected override async Task OnInitializedAsync()
    {
        user = await CurrentUser.GetAsync();
        isAdmin = user?.IsAdmin == true;
        scopedOfficeId = isAdmin ? null : user?.OfficeId;

        await LoadAlerts();

        Hub.OnDeviceStatusUpdated += HandleStatusUpdated;
        Hub.OnAlertCreated += HandleAlertCreated;
        await Hub.StartAsync();
    }

    private Task SetFilter(AlertType? type)
    {
        selectedFilter = type;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ClearError()
    {
        errorMessage = null;
        StateHasChanged();
    }

    private async Task HandleStatusUpdated()
    {
        await LoadAlerts();
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleAlertCreated(DeviceAlert alert)
    {
        if (isAdmin || alert.Device?.OfficeId == scopedOfficeId)
        {
            await LoadAlerts();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAlerts()
    {
        try
        {
            isLoading = true;
            if (!isAdmin && scopedOfficeId is null)
            {
                alerts = [];
                errorMessage = null;
                await InvokeAsync(StateHasChanged);
                return;
            }
            alerts = await ApiClient.GetAlertsAsync(officeId: scopedOfficeId);
            errorMessage = null;
            await InvokeAsync(StateHasChanged);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Błąd połączenia: {ex.Message}";
            alerts = [];
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            errorMessage = $"Błąd ładowania alertów: {ex.Message}";
            alerts = [];
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task MarkAsRead(int alertId)
    {
        try
        {
            isLoading = true;
            var success = await ApiClient.MarkAlertAsReadAsync(alertId);
            if (success)
            {
                await LoadAlerts();
            }
            else
            {
                errorMessage = "Nie udało się oznaczyć alertu jako przeczytany";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Błąd przy oznaczaniu alertu: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task MarkAllAsRead()
    {
        try
        {
            isLoading = true;
            var success = await ApiClient.MarkAllAlertsAsReadAsync(scopedOfficeId);
            if (success)
            {
                await LoadAlerts();
            }
            else
            {
                errorMessage = "Nie udało się oznaczyć wszystkich alertów jako przeczytane";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Błąd przy oznaczaniu alertów: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ClearAllAlerts()
    {
        try
        {
            isLoading = true;
            var success = await ApiClient.ClearAllAlertsAsync(scopedOfficeId);
            if (success)
            {
                await LoadAlerts();
            }
            else
            {
                errorMessage = "Nie udało się wyczyścić alertów";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Błąd przy czyszczeniu alertów: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        Hub.OnDeviceStatusUpdated -= HandleStatusUpdated;
        Hub.OnAlertCreated -= HandleAlertCreated;
        return ValueTask.CompletedTask;
    }
}
