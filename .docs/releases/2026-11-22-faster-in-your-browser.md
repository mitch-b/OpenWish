# Faster in your browser - November 22, 2026

OpenWish now runs its pages in your browser with WebAssembly once the app is
cached, instead of keeping a live server connection for every page.

- **Runs in your browser.** The dashboard and notifications panel no longer pin
  every page to the server, so later visits respond without a round trip for
  each click.
- **Same behavior either way.** Reordering ideas, reservations, comments,
  invitations, friend requests, and gift exchange rules work the same whether a
  page runs on the server or in the browser.
- **No loading flash.** Event management and invitations keep the data shown
  while the page loads instead of flashing a loading state.

No schema changes.
