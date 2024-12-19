# Development Guide

## EntityFramework Core Changes

The EFCore context is found in the [OpenWish.Data](./src/OpenWish.Data) project as a reference, but the [OpenWish.ApiService](./src/OpenWish.ApiService) project owns running the Migrations on Startup. 

### Add Migration

After adjusting EF models and you want to stage a new DB migration, run:

```bash
cd src/OpenWish.ApiService
dotnet ef migrations add Initial --project ../OpenWish.Data/OpenWish.Data.csproj
```

> Note: may need `dotnet tool install --global dotnet-ef`