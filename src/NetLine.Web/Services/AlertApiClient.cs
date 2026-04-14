using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

public class AlertApiClient(HttpClient httpClient)
{
    public async Task<List<DeviceAlert>> GetAlertsAsync(int? officeId = null, int? deviceId = null, int? limit = null)
    {
        var query = new List<string>();
        if (officeId.HasValue) query.Add($"officeId={officeId}");
        if (deviceId.HasValue) query.Add($"deviceId={deviceId}");
        if (limit.HasValue) query.Add($"limit={limit}");
        var suffix = query.Count == 0 ? "" : "?" + string.Join("&", query);
        return await httpClient.GetFromJsonAsync<List<DeviceAlert>>($"api/alerts{suffix}") ?? [];
    }

    public async Task<bool> MarkAlertAsReadAsync(int alertId)
        => (await httpClient.PutAsync($"api/alerts/{alertId}/read", null)).IsSuccessStatusCode;

    public async Task<bool> MarkAllAlertsAsReadAsync(int? officeId = null)
    {
        var url = officeId.HasValue
            ? $"api/alerts/mark-all-as-read?officeId={officeId}"
            : "api/alerts/mark-all-as-read";
        return (await httpClient.PutAsync(url, null)).IsSuccessStatusCode;
    }

    public async Task<bool> ClearAllAlertsAsync(int? officeId = null)
    {
        var url = officeId.HasValue
            ? $"api/alerts?officeId={officeId}"
            : "api/alerts";
        return (await httpClient.DeleteAsync(url)).IsSuccessStatusCode;
    }
}
