# OpenWish Maintenance Plan

This plan is the operational companion to `PRODUCT_DIRECTION.md`. It records
the durable sequence an automated maintainer follows; GitHub issues labeled
`autowork` remain the source of feature scope.

## Required Increment

1. Start from a clean, current default branch and ensure no other automated
   maintenance pull request is open.
2. Read `PRODUCT_DIRECTION.md`, then inspect open `autowork` issues and recent
   merged work.
3. State one user outcome and the product-direction principle it advances.
4. Implement one cohesive vertical slice. Do not combine unrelated cleanup.
5. Update tests, documentation, release notes, and screenshots in the same
   branch.
6. Run formatting, build, unit tests, and the committed E2E verification.
7. Review the complete diff, remediate actionable findings, and open a pull
   request only when all evidence is present.

## Acceptance Gates

- `dotnet format --verify-no-changes`
- `dotnet build`
- `dotnet test`
- `scripts/verify-e2e.sh`
- no secrets, real user data, or production credentials in evidence
- no browser console errors, failed API responses, or server exceptions in
  the verified journey
- desktop and mobile screenshots covering the dashboard, wishlists, events,
  friends, notifications, and responsive layouts
- a dated release note under `.docs/releases/`

## Release Evidence

Long-lived product screenshots belong under `.docs/images/` and should be
optimized before commit. Pull-request-only screenshots should use GitHub user
attachments so routine evidence does not increase clone size. Release
artifacts may be attached to the GitHub release.

Do not use an orphan or unrelated branch as an image CDN. Such branches are
easy to break, bypass normal review, and make retention unclear.

## Safe Automation Boundaries

- Never weaken authentication or authorization for verification.
- Development-only test authentication must require both the Development
  environment and explicit configuration.
- Use isolated Docker Compose project names and synthetic data.
- Keep the persistent agent environment isolated from Aspire. Promote to it
  with `scripts/agent-environment.sh deploy` only after the ephemeral
  verification gate passes.
- Prefer additive database changes; do not perform destructive migrations
  without explicit human approval.
- Do not merge when runtime verification or review remediation fails.
