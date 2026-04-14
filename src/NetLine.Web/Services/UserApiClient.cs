using System.Net.Http.Json;

namespace NetLine.Web.Services;

public class UserApiClient(HttpClient httpClient)
{
    public async Task<List<ManagedUser>> GetUsersAsync()
        => await httpClient.GetFromJsonAsync<List<ManagedUser>>("api/users") ?? [];

    public async Task<(bool Success, string? Error)> CreateUserAsync(
        string email, string password, int? officeId, string role,
        string? firstName = null, string? lastName = null)
    {
        var response = await httpClient.PostAsJsonAsync("api/users", new
        {
            Email = email,
            Password = password,
            OfficeId = officeId,
            Role = role,
            FirstName = firstName,
            LastName = lastName
        });
        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"Error: {response.StatusCode}" : body);
    }

    public async Task<bool> UpdateUserAsync(string id, int? officeId, string? role)
    {
        var response = await httpClient.PutAsJsonAsync($"api/users/{id}", new
        {
            OfficeId = officeId,
            Role = role
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string id)
        => (await httpClient.DeleteAsync($"api/users/{id}")).IsSuccessStatusCode;
}

public record ManagedUser(
    string Id,
    string Email,
    int? OfficeId,
    string? FirstName,
    string? LastName,
    string Role);
