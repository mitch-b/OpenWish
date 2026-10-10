# OpenWish Product Direction

## Purpose

OpenWish helps families, friends, and communities coordinate thoughtful gift
giving without spoiling surprises or depending on a commercial wishlist
platform.

## Core Promise

A group can move from "what should we give?" to a shared, private, and
coordinated plan with less duplicate effort and fewer awkward conversations.

## Current Phase: Grow What People Feel

The first phase made the existing journeys dependable. Dozens of increments
hardened wishlists, events, invitations, friends, notifications, and their
failure states. That foundation is good enough to build on, and continuing to
spend days on invisible hardening no longer moves the product.

Automated work now alternates between two kinds of day:

- **Polish days** make a core journey feel faster, smoother, and more
  finished, whether or not anyone has reported a bug.
- **Feature days** add capability, and on a regular cadence ship something
  substantial from the [roadmap](#product-roadmap).

Code-health work that a user cannot notice is not a daily increment on its
own; see [What does not count](#what-does-not-count-as-an-increment).

## Where Value Comes From

Every increment should make at least one of these noticeably better for a
real person using OpenWish:

1. **Gift giving** - a shopper knows who they are buying for, what is still
   needed, what they have already committed to, and how it fits their budget,
   without duplicating anyone else's gift.
2. **Wishlist curating** - capturing an idea takes seconds on a phone, and
   lists stay ranked, organized, and current instead of growing stale.
3. **Surprise-preserving coordination** - reservations, comments, purchases,
   and exchange details reveal only what each participant needs to know.
4. **Local friending** - connecting with people you already know (a
   household, family, friend group, or the person across the table) is quick,
   and sharing with that whole group takes one step. Connections stay inside
   the instance and never become a public directory.
5. **Occasions that come back** - birthdays and annual exchanges recur, so
   OpenWish should remember them, remind people, and make next year easier
   than this year.
6. **Feels fast and finished** - every action gives immediate feedback, pages
   do not jump or flash while loading, common tasks take the fewest taps, and
   equivalent actions look and behave the same everywhere.

## Constraints Every Change Respects

These are not sources of new work by themselves. Every change must keep them
true.

- **Small-group trust** - permissions and identity boundaries take priority
  over growth mechanics or public discovery.
- **Accessible, consistent workflows** - keyboard, screen reader, contrast,
  and responsive behavior meet `.docs/ACCESSIBILITY.md` and the
  `openwish-style-guide` skill.
- **Self-hosting confidence** - setup, upgrades, backups, and configuration
  stay understandable to an operator who is not an OpenWish contributor.
  Optional integrations such as SMTP, OpenAI, and Google sign-in degrade
  gracefully when they are not configured.
- **Portable data and integrations** - prefer documented contracts and
  replaceable providers over lock-in to a single store, identity provider, or
  deployment platform.
- **Low-maintenance ownership** - favor boring, observable components and
  additive migrations that automated maintainers can verify safely.

## Product Principles

- Make the next useful action obvious.
- Keep the owner, participant, and gift-buyer perspectives distinct.
- Treat privacy boundaries as product behavior, not implementation detail.
- Prefer one complete vertical slice over several unfinished surfaces.
- Show empty, loading, success, and failure states explicitly.
- Keep public identifiers stable and internal database identifiers private.
- Document configuration and operational behavior alongside the code.
- Require executable evidence for user-visible changes.

## De-emphasize

- Public social feeds, follower counts, global user search, and engagement
  mechanics.
- Advertising, affiliate ranking, or paid placement.
- Marketplace checkout and payment processing, including for group gifts.
- Features that require collecting personal data unrelated to gift
  coordination.
- Broad theming or customization systems ahead of roadmap capability.

## Daily Increment Rules

Each scheduled run delivers at most one pull request with one cohesive
outcome. Use the calendar date in the automation host's time zone.

### 1. Overrides

A security fix, a broken default-branch build or CI workflow, a vulnerable
dependency that Dependabot has not addressed, a data-loss risk, or a
reproduced user-visible regression overrides the day type. Fix it, then stop.
Label the pull request `override`; override work does not count toward the
journey rotation, theme rotation, or major cadence.

### 2. Day type

- **Even-numbered day: Polish day.**
- **Odd-numbered day: Feature day.**

An open `autowork` issue written by the owner beats self-selected work when it
fits the day type: bug and UX issues on polish days, capability issues on
feature days. An `autowork` issue that has waited more than seven days is
taken on the next run regardless of day type.

### 3. Polish days

**Goal:** someone who walked this journey yesterday notices that it feels
better today.

Choose the work in this order:

1. The oldest open `autowork` issue describing a bug or UX problem.
2. The oldest open issue labeled `polish` (a finding deferred by an earlier
   polish day).
3. Otherwise, a jank sweep of one journey from the
   [rotation](#journey-rotation). Pick the journey whose most recent merged
   `polish` pull request is oldest, or one that has never been polished; ties
   go to the lowest journey ID, except for the seasonal rule under the
   [major feature order](#major-feature-order). For the two polish days after
   a `major-feature` pull request merges, sweep that new feature instead.

Run a jank sweep like this:

- Use an isolated running instance with the synthetic personas (AlexDemo,
  JordanDemo, CaseyDemo, and TaylorDemo); see the `openwish-local` skill.
- Walk the journey as each relevant persona at a 390 x 844 phone viewport and
  a 1280 x 800 desktop viewport, in light and dark themes, and once using only
  the keyboard. Capture "before" screenshots of what you intend to change.
- Write a jank log: each finding, where it occurs, and whether it blocks,
  annoys, or is cosmetic.
- Fix the one to five most impactful findings in that journey. Stay within
  the journey so the pull request reads as one improvement.
- File up to three remaining significant findings as issues labeled `polish`
  after checking for duplicates. Leave cosmetic nits in the pull request log.

When there are no bugs, there is still jank. Look for:

- **Loading flashes and layout shift** - prerendered content that disappears
  into a spinner and renders again after WebAssembly starts; spinners shown
  for fast responses; skeletons that do not match the final layout; content
  that moves under the pointer.
- **Dead or slow interactions** - buttons that do nothing until the page is
  interactive; no pressed, busy, or disabled state while a request runs;
  possible double submission; whole-page reloads where an in-place update is
  expected; lost scroll position or focus after a dialog closes.
- **Too many steps** - navigating away and back to finish a task; re-entering
  information OpenWish already knows; dialogs for simple, reversible actions;
  missing sensible defaults, autofocus, or Enter-to-submit.
- **Untruthful state** - counts, badges, or lists that stay stale after an
  action; success messages that do not match the result; optimistic updates
  without rollback.
- **Phone ergonomics** - tap targets under 44 px, horizontal scrolling, the
  on-screen keyboard covering the active field, sticky elements overlapping
  content, long names or prices breaking a layout.
- **Inconsistent language or layout** - different words, icons, or placements
  for the same action; jargon; empty states without a next step; errors
  without a recovery path.

A clean journey is a reason to move to the next journey in the rotation, not
to stop. A polish day ends without a pull request only when an override or a
failed gate blocks it.

### Journey rotation

| ID | Journey | Personas |
|---|---|---|
| J1 | First run: register or sign in, read the dashboard, create a first wishlist, add a first idea | new account, TaylorDemo |
| J2 | Capture and curate: add ideas by link and by note, edit, filter, search, and delete | AlexDemo |
| J3 | Connect: invite a friend, accept, handle friend requests, and open each other's lists | AlexDemo, TaylorDemo, JordanDemo |
| J4 | Shop for someone: browse a friend's list, reserve, comment, mark purchased, release, and confirm the owner sees none of it | JordanDemo, AlexDemo |
| J5 | Organize an exchange: create an event, invite, track responses, set rules, draw names, and review readiness | AlexDemo |
| J6 | Participate in an exchange: accept an invitation, connect a wishlist, see a match, and shop within budget | JordanDemo, CaseyDemo |
| J7 | Stay informed: dashboard next actions, notifications, activity, and What's New | all personas |
| J8 | Account: profile, password, two-factor, theme, and sign out | AlexDemo |

Shipped major features join the rotation as new journeys.

### 4. Feature days

**Goal:** OpenWish can do something useful today that it could not do
yesterday.

Choose the first rule that applies:

1. The oldest open `autowork` issue requesting a capability.
2. An in-progress major feature (an open issue labeled `major-feature`):
   deliver its next milestone.
3. **Major-feature turn:** when no pull request labeled `major-feature` has
   merged in the last **14 days** (the major cadence), start the first item in
   the [major feature order](#major-feature-order) that has no open or closed
   `major-feature` tracking issue.
4. Otherwise ship a **Small** or **Medium** roadmap item. Rotate themes
   A, B, C, D in that order, starting after the theme of the most recently
   merged `feature` pull request whose body contains a `Roadmap:` ID (or with
   A when there is none), and take the first undelivered item in the theme.
   A follow-up recorded on the latest major feature's tracking issue may be
   taken instead when it is more valuable.

Before starting, search merged pull requests and issues for the roadmap ID
(for example `Roadmap: A3`) so an item is never delivered twice. The owner
can change priorities by reordering the roadmap or filing `autowork` issues.

### Major features

A major feature is a capability a person would mention when telling a friend
about OpenWish: a new page or workflow, usually with new data, API, client
service, and browser journey.

- Open a tracking issue titled with its roadmap ID (for example
  `[A1] My gift plan`) and labeled `major-feature`. State the user outcome,
  who may see what, acceptance criteria, and at most three milestones.
- Prefer shipping the whole feature in one pull request when it can pass
  every gate. Otherwise each milestone is a separate feature-day pull request.
- Every milestone must be usable on its own, reachable from navigation, and
  covered by the browser journey. If later milestones never ship, the
  product must still make sense: no placeholder pages, hidden flags, or dead
  controls.
- Only one major feature is in progress at a time. Finish it before starting
  another.
- Use additive migrations only.
- Earlier milestone pull requests reference the tracking issue with
  `Part of #N`. Before the final milestone merges, record worthwhile
  follow-ups on the tracking issue; the final pull request closes it with
  `Closes #N` once its acceptance criteria are proven.

### What does not count as an increment

The following are not acceptable as the outcome of a day:

- nullability annotations, `required` modifiers, XML documentation comments,
  or analyzer-warning cleanup;
- refactors or service extractions that do not change behavior;
- test backfill for behavior the day did not change;
- rate limits, configuration, or logging changes not tied to a reproduced
  problem;
- dependency updates that Dependabot already proposes;
- several unrelated small changes across different journeys or features.

Do this work only when the day's user-visible change requires it in the code
it touches, when the owner files an `autowork` issue for it, or when an
override applies.

### Recording work for later runs

Later runs choose work from the history earlier runs leave behind, so every
automated pull request records:

- exactly one type label: `polish`, `feature`, `major-feature`, or
  `override`, plus `autowork` when it implements an owner issue;
- a `Journey:` line on polish work and a `Roadmap:` line on roadmap work;
- `Closes #N` for each `autowork` or `polish` issue it completes, so merged
  work is never selected again.

Rotations read only pull requests carrying the matching line: the journey
rotation uses the latest merged `polish` pull request for each journey, with
no time limit, and the theme rotation uses the latest merged `feature` pull
request with a `Roadmap:` ID.

## Product Roadmap

The owner curates this list; automation reads it but never edits it. Progress
is tracked in GitHub issues and pull requests, not here.

Sizes:

- **Small** - one screen or component; no schema change.
- **Medium** - several screens or one additive schema change.
- **Major** - see [Major features](#major-features).

### A. Gift giving

- **A1 - My gift plan** (Major). One private page listing every gift the
  shopper has reserved or committed to across friends' wishlists and events,
  grouped by recipient and occasion, with a status (reserved, purchased,
  wrapped, given), totals against each event or recipient budget, and links
  back to each item. Only the shopper sees it.
- **A2 - Group gifts** (Major). A shopper opens an expensive item to
  contributions; others pledge an amount or a share; contributors (never the
  recipient) see pledged versus price and who is coordinating; the
  coordinator marks it purchased. Pledges only, never payments.
- **A3 - Shop within budget** (Small). On a gift-exchange match and a
  friend's wishlist, highlight ideas within the event budget and allow
  sorting and filtering by price.
- **A4 - Gift history** (Medium). After an occasion, purchases become a
  private record of what you gave whom, and reserving something you already
  gave that person shows a gentle warning.
- **A5 - After the occasion** (Medium). Once the occasion date passes, the
  recipient can mark ideas as received, received ideas leave the active list,
  and an optional thank-you reaches the giver. Nothing about givers is
  revealed before the date.

### B. Wishlist curating

- **B1 - Capture from anywhere** (Major). Make OpenWish an installable
  progressive web app with a share target, so sharing a product page from a
  phone creates an idea in a chosen list with link import; add a paste-a-link
  quick add to the dashboard and list pages.
- **B2 - Rank what matters** (Medium). Reorder ideas by drag and drop and by
  keyboard, and pin "most wanted" ideas; shoppers see the owner's order.
- **B3 - Move, copy, and bulk edit** (Medium). Move or copy ideas between
  lists, and select several to change priority, move, or archive them.
- **B4 - Gift profile** (Medium). Optional sizes, favorite colors and brands,
  allergies, and "please don't buy" notes, shown to friends beside the
  owner's lists.
- **B5 - Keep lists fresh** (Small). Ask "still want this?" about old ideas,
  archive instead of delete, and flag imported links that stop resolving.
- **B6 - Lists for people without accounts** (Major). A parent or caregiver
  manages wishlists for children or others without accounts, can add a
  co-manager from their household, and chooses whether managers see
  reservations, because the manager is not the recipient.

### C. Local friending

- **C1 - Circles and households** (Major). Group friends into named circles
  such as Family or Book Club; share a wishlist with a circle and invite a
  whole circle to an event in one step; new circle members gain access
  automatically.
- **C2 - Connect in person** (Medium). A short-lived QR code and join code on
  the friends page lets people in the same room connect in seconds. A phone
  without an account goes through registration and returns to the request.
- **C3 - People you already share with** (Small). Suggest connections among
  co-participants of your events and collaborators on your lists, never from
  an instance-wide directory.
- **C4 - First-run checklist** (Small). A dismissible dashboard checklist for
  new accounts: create a list, add an idea, add a friend, and join or create
  an event.

### D. Occasions

- **D1 - Run it again** (Major). Mark an event as recurring and copy a past
  event with its participants, budget, rules, and connected wishlists. New
  draws avoid last year's pairings by default. Build on the existing
  `Event.IsRecurring` and `Event.CopiedFromEvent` fields.
- **D2 - Reminders and digests** (Major). Opt-in email and in-app reminders
  before a name draw, an occasion, and a friend's birthday, plus a weekly
  digest of new ideas on lists you shop from. Without SMTP, reminders are
  in-app only.
- **D3 - Birthdays** (Medium). Optionally share a birthday (month and day)
  with friends; the dashboard shows upcoming birthdays with a link to that
  person's list.
- **D4 - Exchange reveal** (Small). After the occasion, an organizer can
  reveal who drew whom to the group.

### E. Self-hosted operation

Take these only through the major feature order or an `autowork` issue.

- **E1 - Operator console** (Major). First-run administrator, user list,
  open, invite-only, or closed registration, SMTP test message, and version
  and migration status.
- **E2 - Export your data** (Medium). Download your lists, ideas, and events
  as JSON or CSV, and import a previously exported file.

### Major feature order

1. A1 - My gift plan
2. D1 - Run it again
3. B1 - Capture from anywhere
4. C1 - Circles and households
5. D2 - Reminders and digests
6. A2 - Group gifts
7. B6 - Lists for people without accounts
8. E1 - Operator console

The major feature order front-loads holiday-season value. From September
through December, when most groups plan exchanges, journeys J4 through J6 win
ties in the polish rotation.

## Evidence Before Expansion

Every user-visible increment must include:

- focused automated tests where the changed layer supports them;
- an executable Playwright journey with data-bearing assertions, including
  the privacy boundary (what the recipient or a non-member must not see) for
  coordination features;
- desktop and mobile screenshots captured from the verified application,
  with before-and-after pairs for each polish finding fixed;
- a user-focused release note;
- clean application logs for the exercised journey.

Screenshots demonstrate presentation, but assertions and logs prove behavior.
Do not add product analytics that collect wishlist content, gift ideas,
comments, email addresses, or other personal content.

## Grow Deliberately

New concepts must strengthen gift giving, wishlist curating, coordination,
local friending, recurring occasions, or self-hosted operation. Prefer
extending an existing model and workflow over creating a parallel subsystem.
Remove obsolete paths when a replacement is proven and migration is safe.

## Success Signals

- **Today:** a new self-hosted group can create an account, create and share
  a wishlist or event, and coordinate a gift without administrator
  intervention or accidental disclosure.
- **Next:** a returning family runs this year's exchange from last year's in
  minutes; every shopper sees their whole gift plan at a glance; anyone can
  add an idea from a phone's share sheet; and a household can share with
  everyone it cares about in one step.
