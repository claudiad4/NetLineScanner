using NetLine.Web;
using NetLine.Web.Components;
using NetLine.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure.Data;
using NetLine.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection") ?? throw new InvalidOperationException("Connection string 'AppDbContextConnection' not found.");;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

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

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityUserAccessor>();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<IdentityUser>, IdentityNoOpEmailSender>();

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

app.MapAdditionalIdentityEndpoints();;

app.Run();