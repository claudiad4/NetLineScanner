using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Components.Shared.Offices;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages.Offices;

public partial class Offices : ComponentBase
{
    [Inject] protected OfficeApiClient ApiClient { get; set; } = default!;
    [Inject] protected CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    private List<Office>? offices;
    private CurrentUserInfo? user;
    private bool isAdmin;

    private Office? editingOffice;
    private bool isLoading;
    private bool showForm;
    private OfficeFormCard? formCard;

    protected override async Task OnInitializedAsync()
    {
        user = await CurrentUser.GetAsync();
        isAdmin = user?.IsAdmin == true;
        await LoadOffices();
    }

    private async Task LoadOffices()
    {
        var all = await ApiClient.GetOfficesAsync();
        if (isAdmin)
        {
            offices = all;
        }
        else if (user?.OfficeId is int officeId)
        {
            offices = all.Where(o => o.Id == officeId).ToList();
        }
        else
        {
            offices = [];
        }
    }

    private void ToggleAddForm()
    {
        if (showForm)
        {
            CancelForm();
        }
        else
        {
            editingOffice = null;
            showForm = true;
        }
    }

    private void StartEdit(Office office)
    {
        editingOffice = office;
        showForm = true;
    }

    private void CancelForm()
    {
        showForm = false;
        editingOffice = null;
    }

    private void OpenOffice(int officeId) => Nav.NavigateTo($"/offices/{officeId}");

    private async Task HandleSave(OfficeFormCard.OfficeFormData data)
    {
        if (!isAdmin) return;

        isLoading = true;

        bool success;
        string? error;
        if (editingOffice is null)
        {
            (success, error) = await ApiClient.AddOfficeAsync(data.Name, data.Location);
        }
        else
        {
            (success, error) = await ApiClient.UpdateOfficeAsync(editingOffice.Id, data.Name, data.Location);
        }

        if (success)
        {
            CancelForm();
            await LoadOffices();
        }
        else
        {
            formCard?.SetError(error ?? "Nie udało się zapisać biura.");
        }

        isLoading = false;
    }

    private async Task DeleteOffice(int id)
    {
        if (!isAdmin) return;
        await ApiClient.DeleteOfficeAsync(id);
        await LoadOffices();
    }
}
