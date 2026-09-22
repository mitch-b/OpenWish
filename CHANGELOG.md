# Changelog

All notable user-facing changes to OpenWish are documented here.

## [0.1.25] - 2026-09-22

### Improved

- Event loading failures are distinct from an empty event list and offer a
  direct retry.
- Event refresh failures keep previously loaded event cards available while
  explaining that the latest update could not be retrieved.
- Pending invitation loading failures remain visible instead of disappearing
  as though no invitations exist.
- Interrupted invitation acceptance safely reconciles on retry while keeping
  the invitation available until acceptance is confirmed.
- Interrupted invitation decline safely reconciles on retry while keeping the
  confirmation controls available until decline is confirmed.

## [0.1.24] - 2026-09-21

### Improved

- Friend requests now distinguish a loading failure from an empty inbox and
  provide a duplicate-safe retry.
- Comments stay unavailable when their current state cannot be loaded, avoiding
  a misleading empty thread and offering a duplicate-safe retry.
- Reservation controls stay hidden until reservation status is known, preventing
  shoppers from acting on stale availability or starting overlapping retries.
- Event wishlists distinguish initial and refresh failures from empty data while
  preserving previously loaded details.
- The mobile navigation toggle now reports whether its controlled menu is open
  or closed to assistive technology.

## [0.1.22] - 2026-09-20

### Improved

- Grid and list views now use the same accessible wishlist-item deletion
  confirmation.
- Delete controls prevent duplicate requests and clearly show when removal is
  in progress.
- Failed deletions stay in the dialog with safe retry guidance.
- Successful deletion updates the current view immediately without resetting
  search, filters, sorting, or layout.
- Repeated deletion requests safely reconcile a previously completed removal,
  while unexpected server responses are reported as failures.

## [0.1.23] - 2026-09-19

### Improved

- Gift Exchange is now the feature name for private name-draw events, making
  the workflow clearer for holidays, birthdays, teams, and family traditions.
- Secret Santa remains available as a familiar, optional style for any gift
  exchange; existing exchanges preserve that style after upgrading.
- Exchange setup adapts its event-name examples, date and budget guidance, and
  assignment language to the selected style.
- Organizers see clearer readiness labels and can make private assignments
  with an explicit confirmation that reflects their exchange style.

## [0.1.21] - 2026-09-19

### Improved

- Wishlist-item saves prevent duplicate requests and clearly show when an add
  or edit is in progress.
- Save failures stay in the item dialog with safe guidance, while stable
  request keys make repeated add attempts idempotent.
- Successful adds and edits update the visible wishlist immediately and
  announce the saved result without bypassing viewer privacy rules.
- Product-link imports validate web addresses and prevent overlapping paste or
  button requests.
- Product imports preserve the entered link and clearly distinguish imported
  details, a usable link without details, and a retryable failure.

## [0.1.20] - 2026-09-18

### Improved

- Wishlist overview now shows the number of lists, saved ideas, and shared
  lists at a glance before people start filtering.
- Wishlists shared by friends now have a clear purpose, route to friend
  management, and a guided empty state when nothing is available yet.
- Browser verification retries one incomplete route render before reporting a
  failure, preventing transient navigation timing from failing CI.

## [0.1.19] - 2026-09-18

### Improved

- Event details distinguish loading failures from loading progress and provide
  clear, duplicate-safe retry and exit actions.
- Event management distinguishes loading failures from loading progress without
  implying that settings were changed, and safely redirects non-organizers.
- Event edits prevent duplicate saves, announce completion, and keep actionable
  failures on the form.
- Participant removal requires confirmation, prevents duplicate requests, and
  safely reconciles interrupted results while keeping participant views in sync.
- Invitation declines require confirmation, explain the consequence, and never
  expose internal failure details.

## [0.1.18] - 2026-09-17

### Improved

- Sign-in explains when a session stays active, warns against using that option
  on shared devices, and groups account-recovery actions.
- Registration shows the complete password requirements before submission and
  provides a direct route back to sign-in.
- Password recovery explains its privacy-preserving result, uses email-friendly
  input, and provides a safe return to sign-in.
- Password reset shows the complete password requirements and names the action's
  outcome clearly.
- Confirmation-email recovery explains its privacy behavior, uses email-friendly
  input, and provides a safe return to sign-in.

## [0.1.17] - 2026-09-16

### Improved

- Two-factor settings show whether protection is on or off and prioritize the
  next useful action.
- Authenticator setup provides a truthful manual-key workflow and a
  mobile-friendly verification-code field.
- Recovery-code replacement explains that existing codes will stop working,
  keeps a safe exit visible, and presents new codes as an accessible list.
- Authenticator reset explains its immediate effect and keeps the current
  authenticator as the first action.
- Turning off 2FA explains the reduced protection, preserves a safe exit, and
  confirms the resulting account state.

## [0.1.16] - 2026-09-15

### Improved

- Account settings navigation stays labelled, reachable, and comfortably sized
  on smaller screens.
- Profile settings explain read-only and optional fields, support phone
  autofill, and name the save action clearly.
- Email settings show confirmation in text and explain when a changed address
  takes effect.
- Password settings connect the complete enforced length and character
  requirements to the new-password field.
- Personal-data actions state their outcomes, while account deletion offers a
  clear safe exit before the irreversible action.

## [0.1.15] - 2026-09-13

### Improved

- Wishlist cards provide explicit, named links for keyboard and screen-reader
  navigation.
- Wishlist settings expose loading progress and offer an actionable retry when
  loading fails.
- Friend-access failures remain distinct from a genuinely empty access list.
- Friend-access controls have clear names, visible labels, readable contrast,
  duplicate-change protection, announced outcomes, and comfortable touch
  targets.
- Event connections distinguish loading failures from having no available
  events and provide safe retry guidance.

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
