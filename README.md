# 📃 OpenWish

Shareable wishlists. A web application intended for selfhosting.

* [.NET 8](https://dot.net/)
* Blazor [Auto Rendering Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0) (Server + WebAssembly)
* Entity Framework Core

## Screenshots

TODO

## Features

* Users can have their own wishlist and a hidden wishlist
* Users can add items to wishlist (for themselves or others)
  * Simple (Name, description, price)
  * Via online store URL (todo: how?)
* Remove items from wishlist (list owner)
* Mark items as bought (authenticated user)
* Add comments to items (authenticated user)
* Add users and share wishlists (admin user)
* Allow anonymous access (optional feature)

## Installation

### Docker-Compose

TODO

(incl. GitHub actions to publish ghcr image)

### Local Docker

```pwsh
docker build -t openwish:dev .
docker run -it --rm -p 5000:8080 openwish:dev
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
