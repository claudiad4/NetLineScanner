var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var deviceDb = postgres.AddDatabase("deviceinfo");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.NetLine_ApiService>("apiservice")
    .WithReference(deviceDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NetLine_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
