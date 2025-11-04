# Theming (Light & Dark Mode)

OpenWish supports a built-in light and dark theme.

## How It Works

* CSS variables defined in `wwwroot/theme.css` control all palette values.
* An inline script in `Components/App.razor` applies the stored or system-preferred theme before styles load to prevent a flash of incorrect theme.
* `theme.js` exposes `theme.getPreferred()`, `theme.apply(themeName)` and `theme.toggle()` helpers and persists the choice in both `localStorage` and a cookie (`ow_theme`).
* The toggle UI lives in `Components/Layout/MainLayout.razor` via the `ThemeToggle` component.

## Extending

Add new semantic colors to both light and dark blocks in `theme.css` and then reference them using `var(--color-your-token)` in component `.razor.css` files.

 
### Semantic Tokens

The following tokens exist for both light and dark themes (each supplies `*-bg`, `*-border`, and `*-text` variants):

| Token Prefix | Purpose |
|--------------|---------|
| `--color-success-*` | Success states |
| `--color-warning-*` | Warning / caution |
| `--color-info-*` | Informational / neutral emphasis |
| `--color-danger-*` | Error / destructive / critical |

Bootstrap subtle/background/border overrides (`--bs-*`) are mapped to these tokens for consistent component theming. Legacy `--color-error-*` remains for backward compatibility; prefer `--color-danger-*` moving forward.

Helper classes (`.badge-success`, `.badge-warning`, `.badge-info`, `.badge-danger`) are available in `app.css` for quick inline usage.

### Theme Persistence Strategies

To ensure the theme persists across Blazor navigation and SPA-like transitions, several resilience mechanisms are in place:

1. Early inline script sets `data-theme` before CSS loads (prevents flash).
2. `theme.js` applies a guard if `data-theme` is missing.
3. `blazor:navigation-end` listener reapplies the persisted theme.
4. History API hooks (`pushState`, `replaceState`, `popstate`) trigger reapplication for client-side route changes.
5. A `MutationObserver` watches for accidental removal of `data-theme` and restores it.

If introducing new rendering flows or replacing the root HTML, retain the early script and include `theme.js` in `<head>` so components can immediately access the API.

## Accessibility

Current palette choices target WCAG AA contrast for body text and interactive elements. If you introduce new tokens, validate contrast (e.g. using browser dev tools or external contrast checkers) especially for text < 18px.

## Server Awareness

A cookie is set for potential future server-side rendering adjustments. Currently the server does not branch logic based on the theme.

## Fallbacks

If anything fails, the app defaults to light mode.
