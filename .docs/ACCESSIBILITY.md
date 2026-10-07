# OpenWish Accessibility Guide

OpenWish is designed to be accessible to all users, regardless of ability. This guide documents the accessibility features, WCAG compliance, and guidelines for contributors.

## Accessibility Features

### Keyboard Navigation

OpenWish is fully navigable using the keyboard:

- **Tab/Shift+Tab**: Move forward/backward through interactive elements
- **Enter/Space**: Activate buttons, links, and form controls
- **Arrow Keys**: Navigate between tabs, list items, and menus
- **Escape**: Close dialogs and modals
- **Alt+Letter**: Activate menu items (desktop browsers)

#### Wishlist Navigation

- **Arrow Keys** (Left/Right): Navigate between wishlist tabs without scrolling the page
- **Home/End**: Jump to the first/last tab
- **Enter**: Open selected tab
- Focus automatically moves to the tab content when selected

#### Form Controls

- **Tab/Shift+Tab**: Move between form fields
- **Arrow Keys**: Select radio buttons or options in dropdowns
- **Enter**: Submit forms
- **Space**: Check/uncheck checkboxes

### Screen Reader Support

OpenWish includes comprehensive ARIA landmarks and labels for screen readers:

- **Page Structure**: Landmarks identify main navigation, content areas, and complementary sections
- **Form Labels**: All form inputs have associated labels with `<label>` elements or `aria-label` attributes
- **Button Text**: All buttons have descriptive text or `aria-label` attributes
- **Dynamic Content**: Loading states and status updates use `aria-live` regions
- **Dialogs**: Modal dialogs use `role="dialog"` and `aria-labelledby` for title

#### Screen Reader Tips

- **NVDA** (Windows, Free): Use Firefox or Chrome for best compatibility
- **JAWS** (Windows, Commercial): Fully tested and supported
- **VoiceOver** (macOS/iOS): Use Safari for best compatibility
- **TalkBack** (Android): Full mobile accessibility support

### Visual Accessibility

#### Color Contrast

OpenWish meets WCAG AA standards for color contrast:

- **Normal Text**: 4.5:1 contrast ratio
- **Large Text** (18pt+): 3:1 contrast ratio
- **UI Components**: 3:1 contrast ratio for visual elements

#### Focus Indicators

- **Keyboard Focus**: Clear, high-contrast focus rings indicate which element has keyboard focus
- **Visible on All Backgrounds**: Focus indicators are visible on light and dark backgrounds
- **Never Removed**: Focus indicators are never hidden with `outline: none` without replacement

#### Text Sizing

- **Responsive Text**: Text sizes adjust based on viewport size
- **User Zoom**: Users can zoom up to 200% without losing functionality
- **No Small Text Requirement**: Critical information is never presented in text smaller than 14px

#### Motion and Animation

- **Reduced Motion**: Animations are disabled for users who prefer reduced motion (`prefers-reduced-motion: reduce`)
- **Auto-play**: Videos and media never auto-play with sound
- **Flashing Content**: Nothing flashes more than 3 times per second

### Mobile Accessibility

OpenWish is accessible on mobile devices with assistive technologies:

- **Touch Targets**: All interactive elements are at least 44x44px (WCAG 2.1 AAA recommendation)
- **Responsive Design**: Content reflows properly on small screens
- **Zoom Support**: Users can zoom without triggering horizontal scroll
- **Portrait and Landscape**: Works correctly in both orientations

### Color Blindness

OpenWish does not rely on color alone to convey meaning:

- **Status Indicators**: Use both color and icons or text labels
- **Charts and Graphs**: Include data labels or legends
- **Links**: Distinguished by more than color (usually underline or different font weight)

### Cognitive Accessibility

OpenWish is designed to be easy to understand:

- **Clear Language**: Text is written in plain language, avoiding jargon
- **Consistent Navigation**: Menu and link placement is consistent across pages
- **Consistent Terminology**: The same words are used for the same concepts throughout
- **Clear Error Messages**: Errors explain what went wrong and how to fix it
- **Undo/Redo**: Important actions can be undone if needed

## WCAG Conformance

OpenWish conforms to the following accessibility standards:

### Web Content Accessibility Guidelines (WCAG) 2.1

OpenWish aims to conform to **WCAG 2.1 Level AA**:

- **Perceivable**: Information and interface components are presented in ways users can perceive
- **Operable**: Users can navigate and operate the interface using various input methods
- **Understandable**: Text and interface behavior are clear and easy to understand
- **Robust**: Content is compatible with current and future assistive technologies

### Section 508 (US)

OpenWish is designed to comply with US Section 508 accessibility requirements for federal agencies.

### EU EN 301 549

OpenWish follows EU standards for accessible information and communication technology.

## Testing Accessibility

We continuously test OpenWish for accessibility:

- **Automated Testing**: Axe-core integration tests catch common issues
- **Manual Testing**: Team members test with screen readers and keyboard navigation
- **User Testing**: We work with users who have disabilities to identify and fix issues
- **Browser Testing**: Tested on Chrome, Firefox, Safari, and Edge
- **Assistive Technology**: Tested with NVDA, JAWS, VoiceOver, and TalkBack

## Known Limitations

While we strive for full accessibility, some limitations exist:

- **Third-Party Integrations**: Google Sign-In and product image imports may have their own accessibility limitations
- **Complex Tables**: Event pairing rule tables may be challenging with some screen readers (we recommend using the keyboard to navigate)
- **PDF Export**: Exported PDFs may not be fully accessible if the PDF viewer doesn't support all features

## Accessibility Shortcuts

| Platform | Shortcut | Action |
|----------|----------|--------|
| All | `/` | Search wishlists and events (if focus is on main content) |
| All | `?` | Show help (on some pages) |
| Windows | `Alt+Letter` | Activate menu commands |
| macOS | `Option+Letter` | Activate menu commands |

## Contributing to Accessibility

We welcome contributions to improve OpenWish accessibility:

### Reporting Accessibility Issues

If you discover an accessibility problem:

1. **Test with Your Setup**: Document what assistive technology or browser you're using
2. **Describe the Issue**: Explain what you expected to happen vs. what actually happened
3. **Provide Steps to Reproduce**: List the exact steps to encounter the problem
4. **Report on GitHub**: Open an issue at [mitch-b/OpenWish](https://github.com/mitch-b/OpenWish/issues) with the label `accessibility`

### Creating Accessible Features

When building new features, follow these guidelines:

#### Semantic HTML

Use semantic HTML elements to provide structure:

```html
<!-- Good -->
<header>Site header</header>
<nav>Site navigation</nav>
<main>Main content</main>
<aside>Sidebar</aside>
<footer>Footer</footer>

<!-- Good -->
<button>Click me</button>
<a href="...">Link</a>
<input type="text" />

<!-- Avoid -->
<div onclick="...">Click me</div> <!-- Use <button> -->
<span role="link">Link</span> <!-- Use <a> or <button> -->
```

#### ARIA Labels

Provide descriptive labels for all interactive elements:

```html
<!-- Good -->
<button aria-label="Close dialog">×</button>
<input type="text" aria-label="Search wishlists" />

<!-- Avoid -->
<button>×</button> <!-- No label -->
<div role="button">Action</div> <!-- Use semantic HTML -->
```

#### Focus Management

Ensure keyboard focus is visible and logical:

```csharp
// C# Razor component example
@ref="elementReference"

// After showing a dialog, focus the first interactive element:
elementReference?.FocusAsync()
```

#### Color Contrast

Always ensure sufficient contrast:

```css
/* Good - 4.5:1 contrast ratio */
color: #333333;
background-color: #ffffff;

/* Avoid - 2.5:1 contrast ratio (too low) */
color: #666666;
background-color: #ffffff;
```

#### Reduced Motion

Respect user preferences for motion:

```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

#### Testing Your Changes

Before submitting a pull request:

1. **Keyboard Navigation**: Test that all features work with Tab, Arrow keys, and Enter
2. **Screen Reader**: Test with NVDA or JAWS for Windows, VoiceOver for macOS
3. **Color Contrast**: Check with WebAIM Color Contrast Checker or axe DevTools
4. **Mobile**: Test with TalkBack or VoiceOver on a mobile device
5. **Automated Tests**: Run `npm run test:a11y` (if available)

## Resources

### Accessibility Tools

- **axe DevTools**: Browser extension for accessibility testing ([deque.com/axe/devtools](https://www.deque.com/axe/devtools/))
- **WebAIM**: Web accessibility resources ([webaim.org](https://webaim.org/))
- **WCAG 2.1 Guidelines**: Full specification ([w3.org/WAI/WCAG21/](https://www.w3.org/WAI/WCAG21/))
- **ARIA Authoring Practices**: Guidance on using ARIA ([w3.org/WAI/ARIA/apg/](https://www.w3.org/WAI/ARIA/apg/))

### Screen Readers

- **NVDA** (Free, Windows): [nvaccess.org](https://www.nvaccess.org/)
- **JAWS** (Commercial, Windows): [freedomscientific.com/products/software/jaws/](https://www.freedomscientific.com/products/software/jaws/)
- **VoiceOver** (Free, macOS/iOS): Built into Apple devices
- **TalkBack** (Free, Android): Built into Android devices

### Learning Resources

- **WebAIM Articles**: [webaim.org/articles/](https://webaim.org/articles/)
- **Accessibility for Everyone**: [bookshop.org - Accessibility for Everyone](https://bookshop.org/books/accessibility-for-everyone/9781491935959)
- **Inclusive Components**: [inclusive-components.design](https://inclusive-components.design/)

## Accessibility Statement

OpenWish is committed to providing an accessible experience for all users. If you have difficulty accessing any part of OpenWish, please report it to [GitHub Issues](https://github.com/mitch-b/OpenWish/issues) with the `accessibility` label.

We will work to resolve accessibility issues as quickly as possible and welcome feedback to improve the accessibility of OpenWish.

**Last Updated**: October 2026

**Accessibility Status**: In Progress - We are actively working to improve OpenWish accessibility and welcome contributions.
