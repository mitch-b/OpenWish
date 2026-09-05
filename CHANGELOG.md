# Changelog

All notable user-facing changes to OpenWish are documented here.

## [0.1.0] - 2026-09-05

### Added

- A public "What's new" page makes release highlights visible from the app.
- Reproducible browser verification now captures desktop and mobile evidence
  for the public product experience.

### Improved

- Core page headings and actions use a shared layout for more consistent
  wishlist, event, and friend workflows.
- Dependency maintenance now covers NuGet, containers, dev containers, and
  GitHub Actions.

### Fixed

- Wishlist mutations and collaboration actions now derive the acting user
  from the authenticated session instead of accepting a caller-supplied
  identity.
- Wishlist item updates now return the updated item consistently to both
  server-rendered and WebAssembly clients.
- Nullable timestamps can now be formatted safely, including in time zones
  whose UTC offset contains partial hours.
