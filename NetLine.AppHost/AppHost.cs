var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(); // opcjonalnie, przydatne do podgl¹du

var deviceDb = postgres.AddDatabase("deviceinfo"); // to jest nazwa bazy

var api = builder.AddProject<Projects.NetLine_ApiService>("apiservice")
    .WithReference(deviceDb);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.NetLine_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NetLine_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
