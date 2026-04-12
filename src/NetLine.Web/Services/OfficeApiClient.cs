using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

public class OfficeApiClient(HttpClient httpClient)
{
    public async Task<List<Office>> GetOfficesAsync()
        => await httpClient.GetFromJsonAsync<List<Office>>("api/offices") ?? [];

    public async Task<(bool Success, string? Error)> AddOfficeAsync(string name, string? location)
    {
        var office = new Office { Name = name, Location = location };
        var response = await httpClient.PostAsJsonAsync("api/offices", office);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"Error: {response.StatusCode}" : body);
    }

    public async Task<bool> DeleteOfficeAsync(int id)
        => (await httpClient.DeleteAsync($"api/offices/{id}")).IsSuccessStatusCode;

    public async Task<Office?> GetOfficeAsync(int id)
    => await httpClient.GetFromJsonAsync<Office>($"api/offices/{id}");
}