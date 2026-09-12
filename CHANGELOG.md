# Changelog

All notable user-facing changes to OpenWish are documented here.

## [0.1.14] - 2026-09-12

### Improved

- Event deletion failures are reconciled before retry, so interrupted responses
  cannot repeat a completed deletion.
- Pairing-rule loading failures are distinct from an empty rule list and
  provide a retry action.
- Pairing-rule changes are idempotent, reconcile interrupted responses, prevent
  duplicate requests, and announce success or actionable failure.
- Drawing names and resetting a gift exchange reconcile interrupted responses
  before allowing another action and show safe, actionable guidance.
- Invitation loading, friend selection, and invitation actions expose
  recoverable errors, progress, and completion.

## [0.1.13] - 2026-09-11

### Improved

- Personal wishlist failures now appear as actionable errors instead of an
  empty wishlist collection.
- Friends' wishlist failures are distinct from having no shared wishlists yet.
- Friend invitations prevent duplicate sends and communicate busy,
  authentication, success, and failure states.
- Friend loading and removal provide retryable errors, named actions, accurate
  sharing consequences, duplicate-action protection, and announced completion.
- The personalized dashboard communicates when its content is loading.

## [0.1.12] - 2026-09-10

### Improved

- The add-item action stays in the wishlist flow on phones, remains visibly
  labelled, and no longer covers nearby controls or content.
- Wishlist item edit, delete, filter, sort, and view actions provide
  comfortable mobile touch targets.
- Secret Santa setup actions use full-width mobile controls that are easier to
  reach without disturbing the step hierarchy.
- Comment deletion and reservation release confirmations provide comfortable
  mobile touch targets for both safe and destructive choices.

## [0.1.11] - 2026-09-09

### Improved

- Confirmation dialogs now contain keyboard focus, close with Escape, and
  return focus to the action that opened them.
- Wishlist visibility uses native, mutually exclusive choices with visible
  keyboard focus across supported browsers.
- Wishlist and item creation prevent duplicate submissions and keep actionable
  errors on the form instead of navigating away.
- Secret Santa assignment failures are distinct from an assignment that has
  not been drawn yet and include a retry action.

## [0.1.10] - 2026-09-08

### Improved

- Event cards now use an explicit, named link instead of pointer-only card
  navigation.
- Reserved-item refreshes announce progress and results, while errors, counts,
  and new-tab destinations provide clearer context.
- Friend-request actions name the person involved, prevent duplicate updates,
  and announce success or failure.
- Comment deletion now requires a focused confirmation, explains permanence,
  and communicates progress and completion.
- Reservation cancellation now requires a focused confirmation, explains when
  an item becomes available again, and communicates completion.

## [0.1.9] - 2026-09-07

### Improved

- Wishlist discovery filters as people type, provides a named clear action,
  and announces empty results.
- Friend invitations connect their email guidance, prevent empty submission,
  and announce validation and success feedback.
- The notification button communicates whether its keyboard-friendly modal
  panel is open, keeps background controls unavailable, and restores focus
  when the panel closes.
- Notification deletion names the selected notification, explains permanence,
  keeps keyboard focus contained, and starts on the safe action.
- Notification updates communicate busy, success, and failure states instead
  of silently ignoring unsuccessful actions.

## [0.1.8] - 2026-09-07

### Improved

- Wishlist item search is labelled and announces the number of matching ideas.
- Wishlist filters identify when they are open and which choices are active.
- Wishlist sorting identifies the current ordering choice.
- Grid and list controls identify the current wishlist view.
- Product URL import fields and the item dialog provide clearer labels,
  guidance, disabled states, predictable initial focus, and consistent actions.

## [0.1.7] - 2026-09-07

### Improved

- Event invitations, invitation decisions, event wishlists, and gift-exchange
  pairing rules now provide clearer keyboard and screen-reader context.

## [0.1.6] - 2026-09-07

### Improved

- Wishlist browsing, gift coordination, and event deletion confirmations now
  provide clearer controls and context for keyboard and screen-reader users.

## [0.1.5] - 2026-09-07

### Fixed

- Compiled OpenWish assemblies now report the same version as the published
  release metadata.

## [0.1.4] - 2026-09-07

### Fixed

- Google sign-in can now leave OpenWish without being blocked by the browser's
  form destination policy.

## [0.1.3] - 2026-09-07

### Fixed

- Google sign-in now reliably starts from the account login page.

## [0.1.2] - 2026-09-07

### Improved

- Loading updates now describe the content being retrieved for screen-reader
  users without announcing decorative button spinners.

## [0.1.1] - 2026-09-06

### Improved

- Wishlist product links now identify the item they open for screen-reader
  users and safely open in a new tab.

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
