# OpenWish browser evidence

Use this workflow for every user-visible change.

1. Run `scripts/verify-e2e.sh` from the repository root.
2. The script builds an immutable Playwright image, starts an isolated Compose
   stack, signs in through the explicitly enabled Development-only endpoint,
   and runs the committed browser assertions.
3. Treat a nonzero exit, browser console error, failed HTTP response, missing
   assertion, server exception, or missing evidence file as a failed release.
4. Inspect `pr-assets/verification/openwish-e2e-result.json` and both
   desktop and mobile screenshots.
5. Keep long-lived screenshots only when they document the current product.
   Stage transient review evidence in ignored `pr-assets/`, upload it as GitHub
   user attachments, and embed affected desktop/mobile images in the PR
   template's screenshot fields. Declare `UI impact: yes`, including backend
   behavior changes. See `.docs/SCREENSHOTS.md` for the enforced PR policy.

Never enable the development login endpoint outside an isolated Development
environment. Never use production credentials or real user data.
