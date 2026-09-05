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

var web = builder.AddProject<Projects.OpenWish_Web>("openwish-web")
    .WithEnvironment("OpenWishSettings__OwnDatabaseUpgrades", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

foreach (var settingName in new[]
{
    "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
    "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
    "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL",
    "OTEL_EXPORTER_OTLP_METRICS_PROTOCOL"
})
{
    if (builder.Configuration[settingName] is { Length: > 0 } settingValue)
    {
        web.WithEnvironment(settingName, settingValue);
    }
}

foreach (var (configurationKey, environmentName) in new[]
{
    ("Authentication:Google:ClientId", "Authentication__Google__ClientId"),
    ("Authentication:Google:ClientSecret", "Authentication__Google__ClientSecret")
})
{
    if (builder.Configuration[configurationKey] is { Length: > 0 } settingValue)
    {
        web.WithEnvironment(environmentName, settingValue);
    }
}

builder.Build().Run();