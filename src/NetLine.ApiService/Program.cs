using Microsoft.EntityFrameworkCore;
using NetLine.Infrastructure.Data;      
using NetLine.Infrastructure.Services;   
using NetLine.Application.Interfaces;   
using NetLine.ApiService.Hubs;           
using NetLine.ApiService.Services;       
using NetLine.ApiService.Endpoints;      

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("deviceinfo");

builder.Services.AddSingleton<ISNMPService, SnmpService>();
builder.Services.AddSingleton<IICMPService, ICMPService>();


builder.Services.AddHostedService<DeviceMonitorService>();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapHub<DeviceHub>("/devicehub"); // SignalR

app.MapGet("/", () => "NetLine API - Monitoring system is ready.");

app.MapDeviceEndpoints();

app.Run();