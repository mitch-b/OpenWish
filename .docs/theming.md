# Theming (Light & Dark Mode)

OpenWish supports a built-in light and dark theme.

## How It Works

* CSS variables defined in `wwwroot/theme.css` control all palette values.
* An inline script in `Components/App.razor` applies the stored or system-preferred theme before styles load to prevent a flash of incorrect theme.
* `theme.js` exposes `theme.getPreferred()`, `theme.apply(themeName)` and `theme.toggle()` helpers and persists the choice in both `localStorage` and a cookie (`ow_theme`).
* The toggle UI lives in `Components/Layout/MainLayout.razor` via the `ThemeToggle` component.

## Extending

Add new semantic colors to both light and dark blocks in `theme.css` and then reference them using `var(--color-your-token)` in component `.razor.css` files.

 
### Semantic Tokens Added

The following tokens exist for both light and dark themes:

| Token | Purpose |
|-------|---------|
| `--color-success-*` | Success states (background/border/text) |
| `--color-warning-*` | Warning states |
| `--color-info-*` | Informational states |

Helper classes (`.badge-success`, `.badge-warning`, `.badge-info`) are available in `app.css` for quick inline usage.

## Accessibility

Current palette choices target WCAG AA contrast for body text and interactive elements. If you introduce new tokens, validate contrast (e.g. using browser dev tools or external contrast checkers) especially for text < 18px.

## Server Awareness

A cookie is set for potential future server-side rendering adjustments. Currently the server does not branch logic based on the theme.

## Fallbacks

If anything fails, the app defaults to light mode.
