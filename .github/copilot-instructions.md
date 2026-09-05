# Copilot Instructions

OpenWish is a .NET 10, self-hosted wishlist and gift-exchange application. It
uses ASP.NET Core Blazor with server rendering plus interactive WebAssembly,
EF Core with PostgreSQL, and .NET Aspire for local orchestration.

## Commands

Run commands from `src/`, which contains `OpenWish.slnx`:

```bash
dotnet build
dotnet format
dotnet test
dotnet run --project OpenWish.AppHost
```

The AppHost starts PostgreSQL, pgAdmin, and `OpenWish.Web`; set its local
PostgreSQL credentials first:

```bash
cd src/OpenWish.AppHost
dotnet user-secrets set Parameters:sqlUser "openwish"
dotnet user-secrets set Parameters:sqlPassword "D0 not use this in prod!"
```

Run one test with:

```bash
dotnet test OpenWish.Shared.Tests/OpenWish.Shared.Tests.csproj \
  --filter "FullyQualifiedName~Namespace.Type.Method"
```

Run the committed browser/API verification from the repository root with
`scripts/verify-e2e.sh`. It uses an isolated Docker Compose stack, synthetic
data, and a Development-only login endpoint; never enable that endpoint in
production.

For EF Core model changes, create migrations from the repository root:

```bash
dotnet ef migrations add <MigrationName> -p src/OpenWish.Data -s src/OpenWish.Web
```

`OpenWish.Web` applies pending migrations at startup only when
`OpenWishSettings:OwnDatabaseUpgrades` is `true`; the Aspire host enables it
for local development.

## Architecture

`OpenWish.AppHost` is the local development entry point. It provisions the
PostgreSQL resource and passes its `OpenWish` connection string to
`OpenWish.Web`.

`OpenWish.Web` is the production host. It owns ASP.NET Core Identity,
authentication, Razor component rendering, API controllers, TLS/proxy setup,
and startup migration execution. It registers application services for
server-rendered components and maps the client assembly for interactive
WebAssembly.

`OpenWish.Web.Client` contains the `InteractiveAuto` UI and HTTP
implementations of shared service interfaces. The client uses the named
`OpenWish.API` `HttpClient`, whose base address is the hosting application.

`OpenWish.Shared` is the server/client boundary: DTOs, request models, and
service interfaces live here. `OpenWish.Application` contains the server-side
implementations, business rules, AutoMapper profile, external product lookup,
email, and activity logic. `OpenWish.Data` owns `ApplicationDbContext`, EF
entities, attributes, and migrations. `OpenWish.ServiceDefaults` adds Aspire
service discovery, resilience, health, logging, and OpenTelemetry defaults.

## Cross-project conventions

- Add a feature that must work after WebAssembly hydration as a vertical
  slice: add shared models and an interface in `OpenWish.Shared`, the
  application implementation and registration in `OpenWish.Application`, a
  controller in `OpenWish.Web`, and an HTTP implementation plus registration
  in `OpenWish.Web.Client`. Keep their method signatures and routes aligned.
- Server application services use `IDbContextFactory<ApplicationDbContext>`;
  create and asynchronously dispose a context within each operation with
  `await using var context = await _contextFactory.CreateDbContextAsync()`.
  Do not inject a long-lived `ApplicationDbContext` into these services.
- Public routes use entity `PublicId` values rather than internal integer
  keys. Preserve authorization and viewer-specific filtering in application
  services. Controllers obtain the authenticated caller through
  `ApiUserContextService`; do not trust a caller-supplied user ID for access
  decisions.
- Most domain records use soft deletion (`Deleted`) and timestamps inherited
  from `BaseEntity`. Queries for active records consistently filter
  `!entity.Deleted`; deletion operations set the flag rather than removing
  rows.
- Register services through each project's `AddOpenWish*Services` extension
  method, rather than adding unrelated registrations directly to `Program.cs`.
  Server-side authentication state and user context have different Razor and
  controller implementations; retain that separation.
- Central package versions are in `src/Directory.Packages.props`. All
  projects target `net10.0` with nullable reference types and preview C#
  enabled by `src/Directory.Build.props`.
- Follow `src/.editorconfig`: file-scoped namespaces, primary constructors
  where suitable, `var` for obvious types, `I`-prefixed interfaces, and
  underscore-prefixed private fields. EF migrations are generated code.
- Read `PRODUCT_DIRECTION.md` and `PLAN.md` before autonomous product work.
  Routine automation selects open issues labeled `autowork`, delivers one
  bounded increment, and includes tests, browser evidence, screenshots, and a
  dated release note.

## Configuration and secrets

Keep credentials out of source control. Local secrets belong in .NET user
secrets; production configuration uses environment variables (for example,
`ConnectionStrings__OpenWish` and
`OpenWishSettings__EmailConfig__SmtpPass`). Optional Google authentication,
SMTP email, OpenAI integration, TLS, and forwarded-proxy settings are
configured through `OpenWish.Web` configuration.