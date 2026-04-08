using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

public class AlertApiClient(HttpClient httpClient)
{
    public async Task<List<DeviceAlert>> GetAlertsAsync()
        => await httpClient.GetFromJsonAsync<List<DeviceAlert>>("api/alerts") ?? [];

    public async Task<bool> MarkAlertAsReadAsync(int alertId)
        => (await httpClient.PutAsync($"api/alerts/{alertId}/read", null)).IsSuccessStatusCode;

    public async Task<bool> MarkAllAlertsAsReadAsync()
        => (await httpClient.PutAsync("api/alerts/mark-all-as-read", null)).IsSuccessStatusCode;

    public async Task<bool> ClearAllAlertsAsync()
        => (await httpClient.DeleteAsync("api/alerts")).IsSuccessStatusCode;
}