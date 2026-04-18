using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

/// <summary>
/// Scoped wrapper around the SignalR <c>/devicehub</c> connection. Pages subscribe
/// to <see cref="OnDeviceStatusUpdated"/> / <see cref="OnAlertCreated"/> instead
/// of building their own <c>HubConnection</c>.
/// </summary>
public sealed class DeviceHubClient : IAsyncDisposable
{
    private readonly IConfiguration _config;
    private HubConnection? _connection;

    public DeviceHubClient(IConfiguration config) => _config = config;

    public event Func<Task>? OnDeviceStatusUpdated;
    public event Func<DeviceAlert, Task>? OnAlertCreated;

    public async Task StartAsync()
    {
        if (_connection is not null) return;

        var apiBaseUrl = _config["services:apiservice:http:0"]
                      ?? _config["services:apiservice:https:0"];
        if (string.IsNullOrEmpty(apiBaseUrl)) return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/devicehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On("DeviceStatusUpdated", async () =>
        {
            if (OnDeviceStatusUpdated is { } handler) await handler.Invoke();
        });

        _connection.On<DeviceAlert>("AlertCreated", async alert =>
        {
            if (OnAlertCreated is { } handler) await handler.Invoke(alert);
        });

        await _connection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
