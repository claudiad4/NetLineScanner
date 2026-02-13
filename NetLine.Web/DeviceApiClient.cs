using NetLine.ApiService.Models;

namespace NetLine.Web.Services;

public class DeviceApiClient(HttpClient httpClient)
{
    public async Task<List<DeviceInfo>> GetDevicesAsync()
        => await httpClient.GetFromJsonAsync<List<DeviceInfo>>("api/devices") ?? [];

    public async Task<bool> AddDeviceAsync(string ip, string label, string type)
    {
        var response = await httpClient.PostAsync($"api/devices?ip={ip}&userLabel={label}&type={type}", null);
        return response.IsSuccessStatusCode;
    }
}