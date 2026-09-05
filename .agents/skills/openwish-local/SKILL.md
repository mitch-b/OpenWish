---
name: openwish-local
description: Run OpenWish locally for user testing or agent verification without disrupting the developer's own instance or data.
---

# OpenWish local environments

Use this skill whenever someone asks to run OpenWish locally, prepare a manual
test environment, test with demo data, capture screenshots, or verify a
user-visible change.

## Choose the environment

### User review environment

Use the persistent isolated stack at `http://localhost:9090` when the user asks
to test OpenWish themselves.

1. Run `scripts/agent-environment.sh status`.
2. Check `http://localhost:9090/alive`.
3. If both are healthy, use the existing stack. Do not redeploy or reseed it.
4. If it is stopped, run `scripts/agent-environment.sh deploy`.
5. Deploy a changed build only after `dotnet format`, `dotnet build`,
   `dotnet test`, and `scripts/verify-e2e.sh` pass.
6. Never run `seed` or `reset` while the user is testing unless they explicitly
   ask to discard their local changes.

The stack uses Compose project `openwish-agent`, web port `9090`, PostgreSQL
port `55433`, and its own persistent volume. It is separate from Aspire and
must never connect to, stop, seed, or migrate the developer's normal database.

Open `http://localhost:9090/Account/Login` to choose a demo persona:

- AlexDemo: organizer, `playwright-owner@openwish.local`
- JordanDemo: confirmed friend, `playwright-friend@openwish.local`
- CaseyDemo: confirmed friend, `playwright-friend2@openwish.local`
- TaylorDemo: pending invitee, `playwright-guest@openwish.local`

No password is required. Development persona controls are available only when
the app is in Development and `OpenWishSettings__EnableDevelopmentLogin=true`.

The initial fixture includes public and private wishlists, several gift ideas,
a confirmed friend, a pending friend request, an accepted and a pending event
participant, a completed Secret Santa draw, reservations, notifications, and
activity. Use AlexDemo to create another event, invite JordanDemo, paste product
URLs into a wishlist, and exercise the organizer flow.

### Agent verification environment

Use `scripts/verify-e2e.sh` for agent-driven testing and screenshot capture.
It creates a unique ephemeral Compose project with no host ports, resets only
its own synthetic data, runs owner/guest/friend/mobile journeys, writes evidence
under `.docs/images/`, and removes itself afterward.

Do not use the persistent port-9090 stack for destructive automation while the
user may be testing. After automated verification succeeds and the change is
ready for review, promote it with:

```bash
scripts/agent-environment.sh deploy
```

Promotion preserves existing review data. On a brand-new volume, demo data is
seeded automatically. To deliberately restore fixtures, run:

```bash
scripts/agent-environment.sh seed
```

## Commands

```bash
scripts/agent-environment.sh status
scripts/agent-environment.sh logs
scripts/agent-environment.sh deploy
scripts/agent-environment.sh seed
scripts/agent-environment.sh stop
scripts/agent-environment.sh reset
```

`reset` removes the isolated review database volume and is destructive. Use it
only when the user explicitly asks to discard local review data.
