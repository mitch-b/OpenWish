# Changelog

All notable user-facing changes to OpenWish are documented here.

## [0.1.0] - 2026-09-05

### Added

- A public "What's new" page makes release highlights visible from the app.
- Reproducible browser verification now captures desktop and mobile evidence
  for the complete product experience across owner, guest, and friend roles.
- A screenshot walkthrough shows prospective users the dashboard, wishlists,
  events, gift exchange, friends, notifications, and mobile layout.
- An isolated agent-managed local environment can be promoted on port 9090
  without changing a developer's Aspire instance or data.

### Improved

- Secret Santa is now the default event path, with seasonal defaults, direct
  post-create setup, a five-step organizer checklist, bulk invitations,
  shareable invite links, one-tap wishlist attachment, and a simpler draw.
- Draw readiness now distinguishes accepted and pending participants, and only
  accepted participants can receive assignments.
- Secret Santa event cards and participant views now surface the next action,
  wishlist readiness, budget, and private shopping guidance on desktop and
  mobile.
- Self-hosted deployments now default to trusted proxy configuration, secure
  production cookies, browser security headers, login lockout, and abuse
  limits for invitation and product-scraping endpoints.
- Google sign-in now sends unconfirmed linked accounts back through email
  confirmation instead of repeatedly asking them to link the same account.
- Notification deletion now updates immediately and reports failures instead
  of closing without feedback.
- Creation forms now focus their first useful field, and wishlists use a
  compact, accessible icon picker instead of a full-width emoji input.
- Product metadata scraping validates every redirect and connection address,
  blocks private networks, and limits response size.
- Core page headings and actions use a shared layout for more consistent
  wishlist, event, and friend workflows.
- Dependency maintenance now covers NuGet, containers, dev containers, and
  GitHub Actions.
- Deterministic Development-only fixtures make collaboration, invitation,
  reservation, and authorization flows reproducible.

### Fixed

- Friend, notification, activity, event, and wishlist APIs now derive the
  acting user from the authenticated session and enforce ownership or
  membership before returning or changing data.
- Anonymous gift reservations no longer expose the reserver to other viewers,
  and wishlist owners receive no reservation details.
- Private wishlists cannot be attached to events or exposed through event
  membership.
- User-supplied links and email content are validated or encoded to prevent
  unsafe navigation and injected markup.
- Invitation registration now clearly shows that the invited email address is
  fixed and requires a new invitation to use another address.
- Wishlist mutations and collaboration actions now derive the acting user
  from the authenticated session instead of accepting a caller-supplied
  identity.
- Wishlist item updates now return the updated item consistently to both
  server-rendered and WebAssembly clients.
- Nullable timestamps can now be formatted safely, including in time zones
  whose UTC offset contains partial hours.
