var builder = DistributedApplication.CreateBuilder(args);

var sqlHostName = "sql";
var sqlDatabaseName = "OpenWish";
// set in user secrets! check DEVELOPING.md
var sqlPassword = builder.AddParameter("sqlPassword", secret: true);

var sql = builder.AddSqlServer(sqlHostName, sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase(sqlDatabaseName);

var apiService = builder.AddProject<Projects.OpenWish_ApiService>("apiservice")
    .WithReference(db)
    .WithEnvironment("OpenWishSettings__Database__Host", $"{sqlHostName}:32772")
    .WithEnvironment("OpenWishSettings__Database__Name", sqlDatabaseName)
    .WithEnvironment("OpenWishSettings__Database__User", "sa")
    .WithEnvironment("OpenWishSettings__Database__Password", sqlPassword.Resource.Value)
    .WaitFor(db);

builder.AddProject<Projects.OpenWish_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
