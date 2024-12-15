var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.OpenWish_ApiService>("apiservice");

builder.AddProject<Projects.OpenWish_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
