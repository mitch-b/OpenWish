# Development Guide

## Aspire Secrets

To run a local SQL Server instance, you must give a password for the `sa` account. Use dotnet user secrets for this.

```bash
cd src/OpenWish.AppHost
dotnet user-secrets set Parameters:sqlPassword "D0 not use this in prod!"
```

The secret will be passed into `OpenWish.Web` automatically (well, from Aspire).

## EntityFramework Core Changes

The EFCore context is found in the [OpenWish.Data](./src/OpenWish.Data) project as a reference, but the [OpenWish.Web](./src/OpenWish.Web) project owns running the Migrations on Startup. 

### Add Migration

After adjusting EF models and you want to stage a new DB migration, run:

```bash
cd src/OpenWish.Web
dotnet ef migrations add Initial --project ../OpenWish.Data/OpenWish.Data.csproj
```

> Note: may need `dotnet tool install --global dotnet-ef`