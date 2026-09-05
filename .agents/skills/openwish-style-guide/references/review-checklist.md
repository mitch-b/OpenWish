# UI review checklist

Review every changed surface at desktop, tablet, and mobile widths.

## Hierarchy and content

- The page's purpose and primary action are obvious in five seconds.
- Headings form one logical hierarchy with no duplicated section titles.
- Copy uses plain, active, sentence-case language.
- Secondary metadata is legible but does not compete with actions.
- Empty states explain the next useful step.

## Components

- Buttons use consistent size, radius, icon spacing, and action wording.
- Cards represent real objects or groups rather than decorative containers.
- Forms keep visible labels, helpful examples, and nearby validation.
- Modals have a title, escape action, focused task, and clear consequence.
- Notifications distinguish unread items without relying on color alone.
- Loading, success, warning, error, disabled, and destructive states are styled.

## Accessibility

- Normal text meets WCAG AA contrast; controls and focus indicators are visible.
- Every interactive element is keyboard reachable and has an accessible name.
- Icon-only controls have `aria-label`; decorative icons are hidden.
- Touch targets are at least 44px where practical.
- Content works at 200% zoom and wraps without clipping.
- Motion is disabled or reduced under `prefers-reduced-motion`.

## Responsive behavior

- Mobile has one compact app header and no horizontal overflow.
- Actions stack or wrap without shrinking labels.
- Tables become cards, scroll safely, or preserve essential labels.
- Long names, emails, URLs, and translated text wrap without overlap.
- Sticky elements do not cover dialogs or content.

## Visual QA

- Run `scripts/verify-e2e.sh`.
- Inspect every file under `.docs/images/walkthrough/` at full size.
- Inspect at least one complete light page and one complete dark page.
- Verify mobile navigation open and closed.
- Verify one populated and one empty state where fixtures permit.
- Verify a modal, notification flyout, form, list, and destructive action.
- Reject generic gradients, emoji chrome, unexplained dead space, repeated
  card-within-card patterns, arbitrary one-off styles, and tiny text.
