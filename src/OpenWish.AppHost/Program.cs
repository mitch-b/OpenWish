var builder = DistributedApplication.CreateBuilder(args);

var sqlHostName = "sql";
var sqlDatabaseName = "OpenWish";

// set in user secrets! check DEVELOPING.md
var sqlUser = builder.AddParameter("sqlUser", secret: true);
var sqlPassword = builder.AddParameter("sqlPassword", secret: true);

var sql = builder.AddPostgres(sqlHostName, sqlUser, sqlPassword)
    .WithPgAdmin()
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase(sqlDatabaseName);

builder.AddProject<Projects.OpenWish_Web>("openwish-web")
    .WithEnvironment("OpenWishSettings__OwnDatabaseUpgrades", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
