This application is a C# .NET application that uses the ASP.NET Core framework to allow users to host or collaborate on group wishlist events: 'secret santa'/gift exchange or birthday events for example.

## Code Standards

When using GitHub Copilot as a coding agent (for automated PRs or code suggestions), always run dotnet format and ensure dotnet build succeeds before committing changes.
When using Copilot in VS Code interactive/agent mode (for on-demand code generation or suggestions), running dotnet format and dotnet build is optional and not required after every change.

To format and ensure build:
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
1. If single statement methods, use expression-bodied members where appropriate
1. Use `var` when the type is obvious from the right-hand side of the assignment
1. Use `nameof` for property names in exceptions and logging
1. Use `async`/`await` for asynchronous methods and ensure proper exception handling
1. Maintain existing code structure and organization
1. Use dependency injection patterns where appropriate
1. Document public APIs and complex logic. Suggest changes to the `.docs/` folder when appropriate