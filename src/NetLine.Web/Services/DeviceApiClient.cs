using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Application.DTO.Dashboards;

namespace NetLine.Web.Services;

public class DeviceApiClient(HttpClient httpClient)
{
    public async Task<List<DeviceInfo>> GetDevicesAsync()
        => await httpClient.GetFromJsonAsync<List<DeviceInfo>>("api/devices") ?? [];

    public async Task<(bool Success, string? Error)> AddDeviceAsync(string ip, string label, string type, int officeId)
    {
        var request = new AddDeviceRequest(ip, label, officeId, type);
        var response = await httpClient.PostAsJsonAsync("api/devices", request);
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return (true, null);

        if (string.IsNullOrWhiteSpace(body))
            return (false, $"Error: {response.StatusCode}");

        try
        {
            var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(body);
            return (false, json?.GetValueOrDefault("error") ?? body);
        }
        catch
        {
            return (false, body);
        }
    }

    public async Task<List<DeviceAlert>> GetAlertsAsync(int? deviceId = null, int limit = 50)
    {
        var url = deviceId.HasValue
            ? $"api/alerts?deviceId={deviceId}&limit={limit}"
            : $"api/alerts?limit={limit}";
        return await httpClient.GetFromJsonAsync<List<DeviceAlert>>(url) ?? [];
    }

    public async Task<bool> MarkAlertAsReadAsync(int alertId)
        => (await httpClient.PutAsync($"api/alerts/{alertId}/read", null))
           .IsSuccessStatusCode;

    public async Task<List<DeviceInfo>> GetDevicesByOfficeAsync(int officeId)
    => await httpClient.GetFromJsonAsync<List<DeviceInfo>>($"api/devices?officeId={officeId}") ?? [];

    public async Task<DeviceInfo?> GetDeviceAsync(int id)
    => await httpClient.GetFromJsonAsync<DeviceInfo>($"api/devices/{id}");

    public async Task<bool> DeleteDeviceAsync(int id)
    => (await httpClient.DeleteAsync($"api/devices/{id}")).IsSuccessStatusCode;

    public async Task<List<DeviceMetric>> GetLatestMetricsAsync(int id)
        => await httpClient.GetFromJsonAsync<List<DeviceMetric>>($"api/devices/{id}/metrics/latest") ?? [];

    public async Task<bool> UpdateDeviceAsync(int id, string userDefinedName, string deviceType)
    {
        var response = await httpClient.PutAsJsonAsync($"api/devices/{id}", new
        {
            UserDefinedName = userDefinedName,
            DeviceType = deviceType
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<DeviceDashboardDto?> GetDashboardAsync(int id)
        => await httpClient.GetFromJsonAsync<DeviceDashboardDto>($"api/devices/{id}/dashboard");
}
