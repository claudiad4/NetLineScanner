using NetLine.Web;
using NetLine.Web.Components;
using NetLine.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<DeviceApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["services:apiservice:http:0"]
        ?? builder.Configuration["services:apiservice:https:0"]
        ?? "http://localhost:5000");
});

builder.Services.AddHttpClient<AlertApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["services:apiservice:http:0"]
        ?? builder.Configuration["services:apiservice:https:0"]
        ?? "http://localhost:5000");
});

builder.Services.AddHttpClient<OfficeApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["services:apiservice:http:0"]
        ?? builder.Configuration["services:apiservice:https:0"]
        ?? "http://localhost:5000");
});

builder.Services.AddSingleton<AlertNotificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();