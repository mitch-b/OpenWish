# 📃 OpenWish

Shareable wishlists. A web application intended for selfhosting.

* [.NET 9](https://dot.net/)
* Blazor Server App (user interface)
* Entity Framework Core managed data

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/mitch-b/OpenWish)

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

## AI Described Features

OpenWish is designed to facilitate gift-giving events and wishlists with social features, making it suitable for managing occasions like Secret Santa, birthdays, or holiday gift exchanges.

**Core Features**

* User Management: 
  * Supports identity/authentication
* Event System:
  * Create and manage events with configurable budgets
  * Support for copying/cloning existing events
  * Track event participants

**Wishlist Features**

* Wishlist Management:
  * Users can create multiple wishlists
  * Support for both private and public wishlists
  * Add/remove items to wishlists

**Social Features**

* Comment on wishlists
* React to wishlists
* Comment on items
* React to items

**Gift Exchange Features**

* Gift Exchange Coordination:
  * Track who is giving gifts to whom
  * Manage purchase intentions
  * Set up custom pairing rules for gift exchanges
  * Support for notifications

## Installation

### Docker Compose

Since OpenWish is built to be a multi-component solution, docker compose can be the easiest way to get up and running!

```yaml
services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong!Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - openwish-data:/var/opt/mssql

  web:
    build: ./src/OpenWish.Web
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sql;Database=OpenWish;User Id=sa;Password=YourStrong!Passw0rd;
    ports:
      - "5001:80"
    depends_on:
      - sql

volumes:
  openwish-data:
```

## Local Development

### Secrets Management

```bash
cd src/OpenWish
dotnet tool install --global dotnet-ef
dotnet ef database update

dotnet user-secrets set EmailConfig:SmtpUser myuser
dotnet user-secrets set EmailConfig:SmtpPass mypass
dotnet user-secrets set EmailConfig:SmtpHost my-smtp.host.com
dotnet user-secrets set EmailConfig:SmtpPort 587
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
