using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Components.Shared.Users;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages;

public partial class Users : ComponentBase
{
    [Inject] protected UserApiClient UserClient { get; set; } = default!;
    [Inject] protected OfficeApiClient OfficeClient { get; set; } = default!;
    [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;

    private List<ManagedUser>? users;
    private List<Office>? offices;
    private string? currentUserId;

    private bool showForm;
    private bool isLoading;
    private UserAddForm? addForm;

    protected override async Task OnInitializedAsync()
    {
        var info = await CurrentUser.GetAsync();
        currentUserId = info?.Id;
        await LoadData();
    }

    private async Task LoadData()
    {
        users = await UserClient.GetUsersAsync();
        offices = await OfficeClient.GetOfficesAsync();
    }

    private void ToggleAddForm()
    {
        showForm = !showForm;
        if (showForm)
        {
            addForm?.Reset();
        }
    }

    private void CancelForm() => showForm = false;

    private async Task HandleCreateUser(UserAddForm.UserAddRequest request)
    {
        isLoading = true;
        var (success, error) = await UserClient.CreateUserAsync(
            request.Email, request.Password, request.OfficeId,
            request.Role, request.FirstName, request.LastName);
        if (success)
        {
            showForm = false;
            await LoadData();
        }
        else
        {
            addForm?.SetError(error ?? "Nie udało się utworzyć użytkownika.");
        }
        isLoading = false;
    }

    private async Task HandleSaveUser(UsersTable.UserUpdate update)
    {
        await UserClient.UpdateUserAsync(update.UserId, update.OfficeId, update.Role);
        await LoadData();
    }

    private async Task DeleteUser(string id)
    {
        await UserClient.DeleteUserAsync(id);
        await LoadData();
    }
}
