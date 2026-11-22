# Faster interactive pages - November 22, 2026

OpenWish now uses the fastest measured way to make each page interactive. In
a benchmark of every interactive page, a live server connection was ready in
about 0.1 seconds on desktop and under 1.1 seconds on a simulated phone, while
switching to browser WebAssembly took several seconds and downloaded about
2.5 MB of runtime files.

- **Ready sooner.** Pages respond to clicks quickly, including on a repeat
  visit, without downloading the browser runtime in the background.
- **More reliable actions.** Reordering ideas, reservations, comments,
  invitations, friend requests, and gift exchange rules use the same public
  identifiers everywhere, so they keep working wherever a page runs.
- **No loading flash.** Event management and invitations keep the data shown
  while the page becomes interactive.

No schema changes.
