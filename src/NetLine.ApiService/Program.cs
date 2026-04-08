using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NetLine.Infrastructure.Data;
using NetLine.Infrastructure.Services;
using NetLine.Application.Interfaces;
using NetLine.ApiService.Hubs;
using NetLine.ApiService.Services;
using NetLine.ApiService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add DbContext with PostgreSQL
builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

// Add Identity services
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedEmail = false; // To i tak wyłączamy
})

.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add application services
builder.Services.AddSingleton<ISNMPService, SnmpService>();
builder.Services.AddSingleton<IICMPService, ICMPService>();
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

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

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
app.UseStaticFiles();
app.UseAntiforgery();

// Map endpoints
app.MapDefaultEndpoints();
app.MapHub<DeviceHub>("/devicehub");
app.MapGet("/", () => "NetLine API - Monitoring system is ready.");
app.MapDeviceEndpoints();

// Map Identity endpoints
app.MapIdentityApi<IdentityUser>();

app.Run();