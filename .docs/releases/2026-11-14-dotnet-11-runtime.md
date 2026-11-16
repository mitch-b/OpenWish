# A faster, more debuggable .NET foundation - November 14, 2026

OpenWish now targets .NET 11 and ASP.NET Core 11. CoreCLR-only projects opt
into Runtime Async for lower-overhead asynchronous execution and cleaner live
call stacks, while shared and WebAssembly client assemblies remain compatible
with Mono. CI also caches NuGet packages to avoid repeating downloads when
dependencies have not changed.

## Wishlist startup safety

Wishlist grid/list view buttons stay disabled until the page can handle
interactions, preventing an early click from being silently lost on a slow
connection. Browser verification holds activation to cover this transition.
