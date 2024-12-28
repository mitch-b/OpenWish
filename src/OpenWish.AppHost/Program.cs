var builder = DistributedApplication.CreateBuilder(args);

var sqlHostName = "sql";
var sqlDatabaseName = "OpenWish";
// set in user secrets! check DEVELOPING.md
var sqlPassword = builder.AddParameter("sqlPassword", secret: true);

var sql = builder.AddSqlServer(sqlHostName, sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase(sqlDatabaseName);

builder.AddProject<Projects.OpenWish_Server>("openwish-web")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
