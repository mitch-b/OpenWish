# OpenWish Product Direction

## Purpose

OpenWish helps families, friends, and communities coordinate thoughtful gift
giving without spoiling surprises or depending on a commercial wishlist
platform.

## Core Promise

A group can move from "what should we give?" to a shared, private, and
coordinated plan with less duplicate effort and fewer awkward conversations.

## Optimize For

1. **Self-hosting confidence** - setup, upgrades, backups, and configuration
   must be understandable to an operator who is not an OpenWish contributor.
2. **Fast wishlist capture** - adding and organizing gift ideas should remain
   useful on a phone and should not require perfect product data.
3. **Surprise-preserving coordination** - reservations, comments, and exchange
   details must reveal only what each participant needs to know.
4. **Small-group trust** - permissions and identity boundaries take priority
   over growth mechanics or public discovery.
5. **Accessible, consistent workflows** - equivalent actions should use the
   same language, layout, feedback, and keyboard behavior across the app.
6. **Portable data and integrations** - prefer documented contracts and
   replaceable providers over lock-in to a single store, identity provider, or
   deployment platform.
7. **Low-maintenance ownership** - favor boring, observable components and
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

- Public social feeds, follower counts, and engagement mechanics.
- Advertising, affiliate ranking, or paid placement.
- Marketplace checkout and payment processing.
- Features that require collecting personal data unrelated to gift
  coordination.
- Broad customization systems before the core wishlist and event journeys are
  reliable.

## Automated Improvement Decision Rule

Automation may select work only from open GitHub issues labeled `autowork`.
If no such issue exists, it may perform one bounded maintenance increment that
directly improves security, dependency health, accessibility, testability,
documentation, or consistency. It must not invent a large product feature.

For a labeled issue, deliver the smallest complete user outcome described by
the issue. Read linked discussions and existing behavior before coding, avoid
duplicating an open pull request, and leave the issue open when acceptance
criteria cannot be proved.

## Daily Feature Decision Rule

- On odd-numbered calendar days, prefer a user-visible vertical slice from an
  `autowork` issue.
- On even-numbered calendar days, prefer maintenance that removes friction
  from a core workflow or makes future increments safer.
- Security fixes, broken builds, vulnerable dependencies, and regressions
  override the calendar rule.
- Stop without a pull request when no change is valuable, bounded, and
  verifiable.

## Evidence Before Expansion

Every user-visible increment must include:

- focused automated tests where the changed layer supports them;
- an executable Playwright journey with data-bearing assertions;
- desktop and mobile screenshots captured from the verified application;
- a user-focused release note;
- clean application logs for the exercised journey.

Screenshots demonstrate presentation, but assertions and logs prove behavior.
Do not add product analytics that collect wishlist content, gift ideas,
comments, email addresses, or other personal content.

## Keep the Product Small

New concepts must strengthen wishlist creation, event coordination, trusted
sharing, or self-hosted operation. Prefer extending an existing model and
workflow over creating a parallel subsystem. Remove obsolete paths when a
replacement is proven and migration is safe.

## Current Success Signal

A new self-hosted group can create an account, create and share a wishlist or
event, and coordinate a gift without administrator intervention or accidental
disclosure.

## Issue-derived Queue

At the time this document was created, the repository had no open issues
labeled `autowork`. This section is intentionally not a copied backlog:
GitHub issues remain the live source of candidate work.
