---
name: openwish-style-guide
description: Design, implement, or review any OpenWish user interface so it follows the product's visual language, interaction patterns, accessibility bar, responsive behavior, and screenshot QA workflow.
---

# OpenWish style guide

Use this skill for every user-visible change in OpenWish, including pages,
components, dialogs, notifications, forms, copy, loading states, and
responsive behavior.

## Required foundation

Before designing or changing UI, read
[`../frontend-design/SKILL.md`](../frontend-design/SKILL.md). It is the
upstream design-thinking foundation vendored from Anthropic under Apache 2.0.
Apply its process first: ground the work in this product, choose a deliberate
direction, avoid generic generated-UI patterns, and critique screenshots.

Then use this skill for OpenWish-specific decisions. Read
[`references/design-system.md`](references/design-system.md) for tokens and
components and [`references/review-checklist.md`](references/review-checklist.md)
before declaring the work complete.

## Product character

OpenWish is a private coordination space for thoughtful giving. It should feel:

- **Quietly magical**, not childish. Use one restrained ribbon/gift motif and
  crisp line icons; do not use emoji as interface chrome.
- **Warm but capable.** It handles emotionally meaningful moments and private
  data, so pair friendly language with visibly dependable controls.
- **Fast to understand.** The primary action and current status must be obvious
  within seconds, especially for Secret Santa organizers and invitees.
- **Calm under complexity.** Progressive disclosure should keep rules,
  invitations, reservations, and destructive actions from competing with the
  user's next step.

## Mandatory workflow

1. Inventory every affected state: loading, empty, populated, success, warning,
   validation, error, disabled, destructive confirmation, light, dark, mobile,
   tablet, desktop, keyboard focus, and long-content wrapping.
2. Identify the page's single primary job and put that action first. Secondary
   actions must be quieter; destructive actions belong behind confirmation.
3. Reuse the tokens and component patterns in the reference. Do not introduce
   one-off colors, shadows, radii, typography, or icon systems.
4. Use `OpenWishIcon` and the local Bootstrap Icons sprite for interface icons.
   An icon supplements a visible label; it does not replace one unless the
   control has an explicit accessible name.
5. Keep interface copy concise, active, sentence case, and consistent. Name the
   same action the same way before and after it runs.
6. Implement semantic HTML and keyboard behavior before visual polish.
7. Run `dotnet format`, `dotnet build --no-restore`, `dotnet test --no-build`,
   and `scripts/verify-e2e.sh`.
8. Inspect every generated screenshot at full size. Compare information
   hierarchy, alignment, density, contrast, wrapping, and interaction states.
   Fix issues and regenerate evidence until the page reads clearly without
   explanation.

## Design rules

- Use Manrope from the self-hosted font asset. Never add remote font requests.
- Use the deep-ink shell, violet primary, and berry accent defined in
  `theme.css`. Reserve gradients for the branded dashboard moment or a subtle
  accent, never as a default page-header treatment.
- Prefer open page composition and grouped sections over a stack of identical
  “SaaS cards.” Borders communicate grouping; shadows communicate elevation.
- Use an 8px spacing rhythm with deliberate 4px exceptions for compact
  metadata. Keep touch targets at least 44px where practical.
- Keep body copy below 72 characters per line and avoid tiny text. Supporting
  text is normally 0.82–0.9rem; never shrink essential information to solve a
  layout problem.
- Use sentence case. Avoid all-caps labels, novelty punctuation, marketing
  filler, and implementation terminology.
- Use status colors only for status. Never rely on color alone; include text or
  an icon and maintain WCAG AA contrast in both themes.
- Prefer a single clear page title, a one-sentence purpose, and one dominant
  action. Do not repeat headings inside nested cards.
- Use modals only for focused choices or confirmation. Give them a clear title,
  concise consequence, safe escape, initial focus, and a specific action label.
- Empty states explain what is absent and offer the next useful action.
- Loading states preserve layout where possible; all spinners have accessible
  status text. Errors say what happened and what the user can do next.
- Motion responds to user action and respects `prefers-reduced-motion`.

## OpenWish UX priorities

1. A Secret Santa organizer should understand the next step immediately.
2. An invitee should accept and contribute ideas with minimal navigation.
3. A shopper should see their match, budget, wishlist, and privacy promise
   before administrative information.
4. Wishlist owners should add and organize ideas faster than they can in a
   notes app.
5. Privacy language should reassure without exposing identities or overwhelming
   the task.

## Sources

This skill incorporates practices from the vendored Anthropic frontend-design
skill, WCAG 2.2 quick-reference guidance, Nielsen Norman Group usability
heuristics, and public agent-skill examples cataloged by
`hueyexe/frontend-agent-skills` and `finfin/awesome-frontend-skills`. Those
sources informed the process; this file contains the project-specific rules.
