# <img src="src/OpenWish.Web/wwwroot/images/openwish-color.svg" alt="OpenWish logo" height="42" style="vertical-align: middle;"> OpenWish

Shareable wishlists. A web application intended for selfhosting.

* [.NET 10](https://dot.net/)
* Blazor App (Server & Client WebAssembly)
* Entity Framework Core managed data on PostgreSQL
* Docker images [published](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web)

## Developing?

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/mitch-b/OpenWish)

See [DEVELOPING.md](./DEVELOPING.md) for additional details.

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
* Built-in light/dark theme with automatic system preference detection and manual toggle (now a sun/moon slider control)

## AI Described Features

OpenWish is designed to facilitate gift-giving events and wishlists with social features, making it suitable for managing occasions like Secret Santa, birthdays, or holiday gift exchanges.

### Core Features

* User Management:
  * Supports identity/authentication
* Event System:
  * Create and manage events with configurable budgets
  * Support for copying/cloning existing events
  * Track event participants

### Wishlist Features

* Wishlist Management:
  * Users can create multiple wishlists
  * Support for both private and public wishlists
  * Add/remove items to wishlists

### Social Features

* Comment on wishlists
* React to wishlists
* Comment on items
* React to items

### Gift Exchange Features

* Gift Exchange Coordination:
  * Track who is giving gifts to whom
  * Manage purchase intentions
  * Set up custom pairing rules for gift exchanges
  * Support for notifications

## Installation

### Docker Compose

Since OpenWish depends on an external datasource (PostgreSQL), if you don't already have a PostgreSQL instance to use, you can run an instance alongside the OpenWish application using Docker Compose:

```yaml
services:
  sql:
    image: postgres:18
    container_name: openwish-postgres
    environment:
      POSTGRES_USER: "openwish"
      POSTGRES_PASSWORD: "YourStrong!Passw0rd"
      POSTGRES_DB: "OpenWish"
    volumes:
      - openwish-data:/var/lib/postgresql
    ports:
      - 5432:5432

  web:
    image: ghcr.io/mitch-b/openwish-web:latest
    container_name: openwish-web
    environment:
      - TZ=America/Chicago
      - ConnectionStrings__OpenWish=Server=sql;Port=5432;Database=OpenWish;User Id=openwish;Password=YourStrong!Passw0rd;
      - OpenWishSettings__OwnDatabaseUpgrades=true
    ports:
      - 5001:8080
    depends_on:
      - sql

volumes:
  openwish-data:
```

See [package versions](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web/versions) for published tags. Recommended to use `{year}{month}` tags (ie. `202601`) for managing upgrades.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

