using Microsoft.AspNetCore.Components;
using NetLine.Domain.Entities;
using NetLine.Web.Services;

namespace NetLine.Web.Components.Pages.Devices;

public partial class AddDevice : ComponentBase
{
    [Inject] protected DeviceApiClient ApiClient { get; set; } = default!;
    [Inject] protected OfficeApiClient OfficeClient { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Parameter] public int OfficeId { get; set; }

    private Office? office;
    private string ip1 = "";
    private string ip2 = "";
    private string ip3 = "";
    private string ip4 = "";
    private string label = "";
    private string deviceType = "Computer";
    private bool isLoading;
    private bool showSuccessToast;
    private bool isIpInvalid;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        office = await OfficeClient.GetOfficeAsync(OfficeId);
    }

    private async Task Submit()
    {
        isLoading = true;
        errorMessage = null;
        isIpInvalid = false;

        if (!IsValidByte(ip1) || !IsValidByte(ip2) || !IsValidByte(ip3) || !IsValidByte(ip4))
        {
            isIpInvalid = true;
            errorMessage = "Popraw błędy w adresie IP.";
            isLoading = false;
            return;
        }

        string fullIp = $"{ip1}.{ip2}.{ip3}.{ip4}";
        try
        {
            var (success, error) = await ApiClient.AddDeviceAsync(fullIp, label, deviceType, OfficeId);
            if (success)
            {
                showSuccessToast = true;
                isLoading = false;
                StateHasChanged();
                await Task.Delay(2000);
                Nav.NavigateTo($"/offices/{OfficeId}");
            }
            else
            {
                errorMessage = error ?? "Serwer odrzucił zapis (sprawdź czy IP nie jest zajęte).";
                isLoading = false;
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Błąd połączenia: " + ex.Message;
            isLoading = false;
        }
    }

    private void Cancel() => Nav.NavigateTo($"/offices/{OfficeId}");

    private static bool IsValidByte(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return int.TryParse(input, out int result) && result >= 0 && result <= 255;
    }
}
