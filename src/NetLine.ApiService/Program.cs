using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NetLine.Infrastructure.Data;
using NetLine.ApiService.Hubs;
using NetLine.ApiService.Services;
using NetLine.ApiService.Endpoints;
using NetLine.Application.Interfaces.Scanning;
using NetLine.Application.Interfaces.Monitoring;
using NetLine.Application.Interfaces.Alerts;
using NetLine.Application.Interfaces.Devices;
using NetLine.Infrastructure.Services.Scanning;
using NetLine.Infrastructure.Services.Alerts;
using NetLine.Infrastructure.Services.Monitoring;
using NetLine.Infrastructure.Services.Monitoring.Snmp;
using NetLine.Infrastructure;
using NetLine.Infrastructure.Identity;
using NetLine.Infrastructure.Services.Monitoring.Components.CPU;
using NetLine.Infrastructure.Services.Monitoring.Components.Memory;
using NetLine.Infrastructure.Services.Monitoring.Components.Network;
using NetLine.Infrastructure.Services.Monitoring.Components.System;
using NetLine.Infrastructure.Services.Monitoring.Components.Component;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add DbContext with PostgreSQL
builder.AddNpgsqlDbContext<AppDbContext>("NetLineDB");

// Add Identity services with roles
builder.Services.AddIdentityApiEndpoints<AppUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add application services
builder.Services.AddSingleton<ISNMPService, SnmpService>();
builder.Services.AddSingleton<IICMPService, ICMPService>();
builder.Services.AddSingleton<ISnmpClient, SnmpClient>();

// Monitoring components — ordered to match the user-facing taxonomy
builder.Services.AddSingleton<IMonitoringComponent, SystemComponent>();
builder.Services.AddSingleton<IMonitoringComponent, CpuComponent>();
builder.Services.AddSingleton<IMonitoringComponent, MemoryComponent>();
builder.Services.AddSingleton<IMonitoringComponent, NetworkInterfaceComponent>();
builder.Services.AddSingleton<IMonitoringComponent, PingComponent>();
builder.Services.AddSingleton<IMonitoringComponent, PortScanComponent>();
builder.Services.AddSingleton<IMonitoringComponent, DnsComponent>();

builder.Services.AddScoped<IDeviceScanner, DeviceScanner>();
builder.Services.AddScoped<IDeviceStatusService, DeviceStatusService>();
builder.Services.AddScoped<IDeviceManager, DeviceManager>();
builder.Services.AddHostedService<DeviceMonitorService>();

// Add API services
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAntiforgery();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations and seed roles/admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
await IdentitySeeder.SeedAsync(app.Services);

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(err => err.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = ex is InvalidOperationException ? 400 : 500;
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message });
}));

app.UseStaticFiles();
app.UseAntiforgery();

// Map endpoints
app.MapDefaultEndpoints();
app.MapHub<DeviceHub>("/devicehub");
app.MapGet("/", () => "NetLine API - Monitoring system is ready.");
app.MapDeviceEndpoints();
app.MapOfficeEndpoints();
app.MapUserEndpoints();
app.MapAlertEndpoints();

// Map Identity endpoints
app.MapIdentityApi<AppUser>();

app.Run();
