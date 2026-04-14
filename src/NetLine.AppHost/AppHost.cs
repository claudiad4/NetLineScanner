var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var deviceDb = postgres.AddDatabase("NetLineDB");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.NetLine_ApiService>("apiservice")
    .WithReference(deviceDb)
    .WaitFor(deviceDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NetLine_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(deviceDb)
    .WaitFor(deviceDb)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
