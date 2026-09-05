# OpenWish design system

## Visual language

OpenWish uses a calm deep-ink foundation with violet for primary actions and a
berry accent for warmth. The single expressive motif is a softly lit gift or
ribbon form. Everything else stays quiet and functional.

## Tokens

Use variables from `src/OpenWish.Web/wwwroot/theme.css`.

| Purpose | Token |
|---|---|
| App canvas | `--color-bg` |
| Primary content surface | `--color-surface` |
| Grouped or inset surface | `--color-surface-alt` |
| Elevated surface | `--color-surface-raised` |
| Hover surface | `--color-surface-hover` |
| Primary text | `--color-text` |
| Supporting text | `--color-text-muted` |
| Tertiary text | `--color-text-subtle` |
| Brand action | `--color-primary-bg` |
| Brand tint | `--color-primary-soft` |
| Warm accent | `--color-accent` |
| Warm accent tint | `--color-accent-soft` |
| Borders | `--color-border`, `--color-border-muted` |
| Elevation | `--shadow-sm`, `--shadow-md`, `--shadow-lg` |

Do not add hex colors to component styles unless the color belongs to a fixed
illustration or must contrast against a known branded background.

## Typography

- Family: self-hosted Manrope, with Segoe UI as fallback.
- Page title: 2–3.4rem, weight 700, tight line height.
- Section title: 1.1–1.35rem, weight 700.
- Body: 1rem, line-height 1.5–1.65.
- Supporting text: 0.82–0.9rem, never below 0.75rem.
- Labels: sentence case, weight 700. Do not simulate hierarchy with all caps.

## Layout

- Main content max-width: 1240px.
- Desktop shell: 232px navigation rail plus fluid content.
- Page padding: 32px desktop, 16px mobile.
- Standard gap: 16px; dense gap: 8px; section gap: 32–40px.
- Mobile breakpoint: 640px for the shell, 768px for content reflow.
- Tablet layouts must be intentionally tested between 768px and 1200px.

## Components

### Buttons

- Primary: one per local decision area.
- Secondary: neutral outline or quiet surface.
- Destructive: danger color and explicit verb.
- Icon and text use a 6–8px gap. Icon-only buttons require `aria-label`.
- Standard controls are at least 42px high; primary mobile controls are 44px.

### Cards and sections

- Use cards for independently actionable objects such as a wishlist or event.
- Use unshadowed inset surfaces for facts, statuses, and grouped rows.
- Do not nest multiple equally elevated cards.
- Radius: 16px panels, 12–13px controls and inset groups, pill only for status.

### Forms

- Labels stay visible; placeholders are examples, not labels.
- Help text follows the field and explains constraints before submission.
- Related controls share a group and action buttons align with the fields.
- Validation is adjacent, specific, and announced with semantic status markup.

### Navigation

- Active route uses a violet wash plus a left indicator.
- Every destination has a consistent line icon and visible text.
- Account and utility actions are visually separated from primary navigation.
- Mobile uses one header row; never stack separate navigation and utility bars.

### Status and feedback

- Success, warning, information, and danger use semantic background, border, and
  text tokens.
- Pair color with text and, where helpful, an icon.
- Notifications show title, concise message, timestamp, and contextual actions.
- Unread state uses a structural indicator, not only a color change.

### Dialogs

- Use the shared backdrop, radius, and elevation.
- Keep headers and footers visually quiet.
- Order actions from safe/secondary to primary/destructive.
- Destructive confirmations state the object, impact, and permanence.

## Iconography

Use `<OpenWishIcon Name="..." />`, backed by
`wwwroot/icons/bootstrap-icons.svg`. Add only required symbols to the optimized
sprite and preserve `.docs/licenses/Bootstrap-Icons-LICENSE.txt`.

User-selected emoji may remain as wishlist content. Emoji must not be used for
navigation, actions, status, or generic decoration.
