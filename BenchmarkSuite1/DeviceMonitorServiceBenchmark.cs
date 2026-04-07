using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetLine.ApiService.Hubs;
using NetLine.ApiService.Services;
using NetLine.Application.Interfaces;
using NetLine.Domain.Entities;
using NetLine.Domain.Models;
using NetLine.Infrastructure.Data;
using NetLine.Infrastructure.Services;
using Microsoft.VSDiagnostics;

namespace NetLine.ApiService.Benchmarks;
[SimpleJob(warmupCount: 1, targetCount: 3, invocationCount: 1)]
[CPUUsageDiagnoser]
public class DeviceMonitorServiceBenchmark
{
    private IServiceProvider _serviceProvider;
    private IHubContext<DeviceHub> _hubContext;
    private ILogger<DeviceMonitorService> _logger;
    private CancellationTokenSource _cts;
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        // Add DbContext with in-memory database
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("BenchmarkDb"));
        // Add mock services
        services.AddSingleton<IICMPService>(new MockICMPService());
        services.AddSingleton<ISNMPService>(new MockSNMPService());
        services.AddSignalR();
        services.AddSingleton(typeof(IHubContext<>), typeof(MockHubContext<>));
        var serviceProvider = services.BuildServiceProvider();
        // Seed the database with test devices
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            if (!db.DevicesInfo.Any())
            {
                for (int i = 0; i < 10; i++)
                {
                    db.DevicesInfo.Add(new DeviceInfo { Id = Guid.NewGuid(), IpAddress = $"192.168.1.{100 + i}", Status = "Unknown", LastScanned = DateTime.UtcNow });
                }

                db.SaveChanges();
            }
        }

        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<DeviceMonitorService>>();
        _hubContext = serviceProvider.GetRequiredService<IHubContext<DeviceHub>>();
        _cts = new CancellationTokenSource();
    }

    [Benchmark]
    public async Task MonitorDevices()
    {
        var service = new DeviceMonitorService(_serviceProvider, _logger, _hubContext);
        await service.StartAsync(_cts.Token);
        await Task.Delay(100);
        await service.StopAsync(_cts.Token);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cts?.Dispose();
    }

    private class MockICMPService : IICMPService
    {
        public async Task<long?> GetPingResponseTimeAsync(string ipAddress)
        {
            await Task.Delay(10);
            return 50;
        }
    }

    private class MockSNMPService : ISNMPService
    {
        public async Task<SNMPScanResult> GetDeviceInfoAsync(string ipAddress)
        {
            await Task.Delay(20);
            return new SNMPScanResult
            {
                Success = true,
                Name = "Device",
                Description = "Test Device",
                Location = "Test",
                Contact = "Test",
                UpTime = "1000000",
                InterfacesCount = 4
            };
        }
    }

    private class MockHubContext<T> : IHubContext<T> where T : Hub
    {
        public IHubClients<T> Clients => new MockHubClients<T>();
        public IGroupManager GroupManager => new MockGroupManager();
    }

    private class MockHubClients<T> : IHubClients<T> where T : Hub
    {
        public IClientProxy All => new MockClientProxy();

        public IClientProxy AllExcept(params string[] excludedConnectionIds) => new MockClientProxy();
        public IClientProxy Caller => new MockClientProxy();
        public IClientProxy Others => new MockClientProxy();

        public IClientProxy OthersInGroup(string groupName) => new MockClientProxy();
        public IClientProxy Group(string groupName) => new MockClientProxy();
        public IClientProxy GroupExcept(string groupName, params string[] excludedConnectionIds) => new MockClientProxy();
        public IClientProxy User(string userId) => new MockClientProxy();
        public IClientProxy Users(IList<string> userIds) => new MockClientProxy();
        public IClientProxy Client(string connectionId) => new MockClientProxy();
        public IClientProxy Clients(IList<string> connectionIds) => new MockClientProxy();
    }

    private class MockClientProxy : IClientProxy
    {
        public async Task SendCoreAsync(string method, object? [] args, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }
    }

    private class MockGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}