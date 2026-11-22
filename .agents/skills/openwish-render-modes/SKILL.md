---
name: openwish-render-modes
description: Choose or benchmark Blazor render modes and WebAssembly performance in OpenWish. Use for InteractiveServer, InteractiveAuto, static SSR, SignalR circuits, hydration, browser startup, and render-boundary decisions.
---

# OpenWish render modes

Choose the smallest **independent interactive boundary**, not the fastest
renderer for each Razor file. Most components either render statically or
inherit their parent's renderer. A child doesn't need a render mode just
because it handles clicks. Mixing explicit Server and WASM in the same
interactive tree isn't supported; introducing new sibling boundaries can
affect Auto's renderer selection across the page.

See [the initial benchmark](baseline.md) for component-level measurements.
On the initial run (app revision `918e511`, three samples per cell), Server
won warm hard-navigation readiness on all 18 boundaries. Auto's cold mobile
startup matched Server at the median, but it downloaded about 2.5 MiB of
framework resources in the background. Cached Auto took 4.1 s to become
interactive on simulated mobile versus 0.72 s for Server. There was no
material local-click advantage under the tested conditions. Don't treat a
cached download as a cached running runtime or infer in-app navigation,
high-RTT interaction, or concurrent server costs from these results.

**Current decision:** all interactive boundaries use `InteractiveServer`, the
measured winner. WebAssembly stays registered in `Program.cs` so the benchmark
can force it, but no production component selects it, so browsers do not
download the runtime. Keep the HTTP client services and `PublicId` routes
working so a future Auto or WASM experiment stays correct. Change the policy
only after a new benchmark shows a clear win on the measured journey.

| Component type | Default | When to reconsider |
| --- | --- | --- |
| App shell, layouts, passive markup | Static SSR | Add an island only for behavior that requires it. A static page can still boot an interactive layout island. |
| Account, sign-in/out, recovery, consent | Static SSR/HTTP forms | Keep Identity, cookies, redirects, antiforgery, and request-bound `HttpContext` on the server. Don't move them into a circuit or WASM to chase a timing result. |
| Wishlist, dashboard, event, friends, gift plan and other interactive routes | `InteractiveServer` | Compare cold and warm navigation, real actions, and load before changing a boundary. Keep both server and browser service paths working. |
| Child forms, cards, dialogs, filtering and coordination controls | Inherit | Consider a separate island only across a static parent and serializable boundary with a specific measured benefit. |
| Theme toggle | Server today; candidate for plain JS | It only needs browser state. Compare a behavior-equivalent JS prototype, including system theme and persistence, on a route that would otherwise stay static. |
| Notification flyout | Server today | Use the same policy as the routes: a Server layout island pins Auto siblings to Server. Measure open-to-data, and check whether fetch-on-open or a lighter island would save work before rewriting. |

Auto can render via Server on a cold load while fetching the WASM bundle in
the background. On a later load it may pick cached WASM; it also prefers
reusing an available renderer. A mounted component does not switch renderers.
An unconditionally server-interactive component in the layout can effectively
pin Auto siblings to Server. WASM still needs authenticated server APIs for
data and mutations. Keep internal entity IDs off the wire, use `PublicId`,
and audit persisted state as browser-readable data.

## Benchmark

From the repo root, with Docker running:

```bash
scripts/benchmark-render-modes.sh
# Quicker check:
BENCHMARK_SAMPLES=1 BENCHMARK_PROFILES=desktop scripts/benchmark-render-modes.sh
```

This starts an isolated verification stack with synthetic data; never use
the local review stack or production. It instruments a temporary copy of
tracked app sources, uses the same Release image for all three policies,
forces each policy across the existing interactive roots together, and records the
actual renderer. No runtime override ships with the app.

`pr-assets/render-benchmark/` contains `components.json` (all Razor UI
components in the two Web projects), raw samples, and `summary.md`. Each
interactive route is measured with a fresh browser context and with a
primed cache. The two layout roots are measured on the dashboard. Server
and database are warmed separately. The mobile profile is Chromium with
a Pixel 7 viewport, 4x CPU slowdown, 100 ms simulated HTTP latency, and
10 Mbps down / 2 Mbps up; it is not a real phone or a controlled measurement
of WebSocket latency.

Read navigation-to-first-interactive-render, synthetic click-to-DOM, and
framework bytes together. The probe measures the renderer, not when every
list finishes loading or how long a save takes. Framework bytes are browser
resource-transfer counts, not total traffic or SignalR bytes. Browser service
workers are disabled for repeatability. No static clone is timed for an
interactive feature: a page with dead buttons isn't a valid comparison.
Forced Server is a policy comparison, not a byte-for-byte historical build.

Use the median and spread for decisions; small runs do not establish tail
latency. Before changing production policy, time representative read-only and
mutation journeys on real devices, and measure CPU, memory and open circuits
at representative concurrency. Keep the choice that preserves UX and wins
on the measured journey, not the one that merely moves more code to a client.
