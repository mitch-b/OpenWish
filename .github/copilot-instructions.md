This application is a C# .NET application that uses the ASP.NET Core framework to allow users to host or collaborate on group wishlist events: 'secret santa'/gift exchange or birthday events for example.

## Code Standards

### Required Before Each Commit (when implementing new features or fixing bugs)
- Change working directory to `{root}/src`
- Run `dotnet format` before committing any changes to ensure proper code formatting
- This will run format on all files to maintain consistent style based on `.editorconfig` settings
- Ensure `dotnet build` succeeds

### Development Flow
- Build: `dotnet build`

## Repository Structure 
- `src/OpenWish.AppHost/`: Aspire dashboard runtime, main run target for local development
- `src/OpenWish.Application/`: All application logic with services that are consumed by the Web app
- `src/OpenWish.Data/`: EF Core assets
- `src/OpenWish.ServiceDefaults/`: Part of Aspire bootstrap for logging and telemetry needs
- `src/OpenWish.Shared/`: Shared assets between ASP.NET Core Blazor Server UI and Client Web Assembly UI
- `src/OpenWish.Web/`: ASP.NET Core Blazor Server web application - all authentication or server rendering assets should be here
- `src/OpenWish.Web.Client/`: Contains code that can run under WebAssembly for clients to have a more seamless native-like feel using the application
- `.docs`: Contains any useful documentation for running, developing, or end-user documentation.

## Key Guidelines
1. Follow C# and ASP.NET Core best practices and idiomatic patterns
2. Maintain existing code structure and organization
3. Use dependency injection patterns where appropriate
5. Document public APIs and complex logic. Suggest changes to the `.docs/` folder when appropriate