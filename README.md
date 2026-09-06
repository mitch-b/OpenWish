# <img src="src/OpenWish.Web/wwwroot/images/openwish-color.svg" alt="OpenWish logo" height="42" style="vertical-align: middle;"> OpenWish

Self-hosted wishlists, gift exchanges, and private gift coordination for
families and groups.

* [.NET 10](https://dot.net/)
* Blazor App (Server & Client WebAssembly)
* Entity Framework Core managed data on PostgreSQL
* Docker images [published](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web)

![OpenWish dashboard showing wishlists, events, and pending requests](.docs/images/walkthrough/home-dashboard.png)

## A walkthrough of OpenWish

### Keep every gift idea organized

Create as many lists as you need, choose private or collaborative visibility,
and add gift ideas with descriptions, prices, purchase links, stores, and
priorities. Friends' public lists stay close at hand without mixing them into
your own.

![Owned wishlists with search, sorting, and visibility controls](.docs/images/walkthrough/wishlists.png)

Each list has focused item search, priority and price filters, grid and list
views, comments, and sharing controls. Gift reservations are visible to the
people coordinating a purchase but hidden from the list owner, preserving the
surprise.

![A detailed wishlist with prioritized items, prices, comments, and an attached event](.docs/images/walkthrough/wishlist-details.png)

### Plan birthdays, holidays, and Secret Santa

Start a Secret Santa with useful holiday defaults, then follow the five-step
organizer checklist to invite your group, collect wishlists, review responses,
and draw names. Bulk email entry and shareable invitation links keep setup
quick even for larger groups.

![Upcoming events and gift exchanges](.docs/images/walkthrough/events.png)

![Guided Secret Santa setup with readiness checks](.docs/images/walkthrough/secret-santa-setup.png)

Invite an entire group at once by pasting email addresses, or choose from
existing friends. OpenWish keeps accepted and pending participants distinct so
organizers always know who will be included in the draw.

![Bulk invitation dialog for a Secret Santa event](.docs/images/walkthrough/invitation-dialog.png)

OpenWish draws only from people who accepted, clearly identifies pending
responses, and reveals each assignment privately. Participants can attach a
wishlist in one step, open their match's ideas, and reserve a gift without
spoiling the surprise.

![Gift exchange assignment, event details, participants, and attached wishlists](.docs/images/walkthrough/event-details.png)

Organizers can update event details, manage invitations, review participant
status, and connect wishlists from one workspace.

![Event management workspace with participants and wishlists](.docs/images/walkthrough/event-management.png)

![Mobile Secret Santa assignment and shopping actions](.docs/images/walkthrough/secret-santa-mobile.png)

### Coordinate with people you trust

Find other OpenWish users, send or respond to friend requests, and control who
can see a list. Comments, reactions, and anonymous reservations let a group
coordinate without revealing the gift to its recipient.

![Friend search, requests, and established connections](.docs/images/walkthrough/friends.png)

Notifications bring invitations and wishlist activity into one place.

![Notifications for event invitations and wishlist activity](.docs/images/walkthrough/notifications.png)

### Use it wherever planning happens

The responsive interface works on phones and desktops. It includes a
light/dark theme with automatic system preference detection and a persistent
manual toggle.

<img src=".docs/images/walkthrough/home-mobile.png" alt="OpenWish dashboard on a mobile screen" width="390">

Dark mode keeps the same clear hierarchy and status visibility in lower-light
environments.

![OpenWish Secret Santa event in dark mode](.docs/images/walkthrough/event-details-dark.png)

Account settings, release highlights on the **What's new** page, and
authenticated access controls round out the hosted experience.

## Developing

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/mitch-b/OpenWish)

See the [development guide](.docs/DEVELOPING.md) for local setup, automated
browser verification, and the isolated agent-managed environment. Read
[`PRODUCT_DIRECTION.md`](PRODUCT_DIRECTION.md) for the principles used to
evaluate contributions.

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

See [package versions](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web/versions) for published tags. Use `{year}{month}` tags (for example, `202601`) to manage upgrades.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
