# OpenWish Maintenance Plan

This plan is the operational companion to `PRODUCT_DIRECTION.md`. It records
the durable sequence an automated maintainer follows. Work is chosen by the
daily increment rules in `PRODUCT_DIRECTION.md`: owner-filed `autowork`
issues first, then polish-day jank sweeps or feature-day roadmap items.

## Required Increment

1. Start from a clean, current default branch and ensure no other open pull
   request is labeled `auto-improvement`.
2. Read `PRODUCT_DIRECTION.md`. Check for an override, then determine the day
   type from the calendar date.
3. Gather selection state:
   - open issues labeled `autowork`, `polish`, and `major-feature`;
   - pull requests labeled `polish`, `feature`, and `major-feature` merged in
     the last 30 days, with the `Journey:` or `Roadmap:` line from each body.
4. Select exactly one outcome using the rules for the day type. State the day
   type, the journey or roadmap ID, the user outcome, and the value area from
   `PRODUCT_DIRECTION.md` it improves.
5. Prepare:
   - polish day: walk the journey, capture "before" screenshots, and write
     the jank log;
   - major feature: create or update the `major-feature` tracking issue with
     acceptance criteria and milestones.
6. Implement one cohesive vertical slice. Do not combine unrelated cleanup.
7. Update tests, documentation, release notes, and screenshots in the same
   branch. Extend the committed browser journey for the changed behavior.
8. Run formatting, build, unit tests, and the committed E2E verification.
9. Review the complete diff, remediate actionable findings, and open a pull
   request only when all evidence is present. Apply `auto-improvement` and
   exactly one of `polish`, `feature`, or `major-feature`; also apply
   `autowork` when implementing an `autowork` issue.
10. Afterward, file deferred polish findings (at most three, deduplicated) and
    update or close the major-feature tracking issue.

## Labels

| Label | Applies to | Meaning |
|---|---|---|
| `autowork` | issues | Owner-approved work; outranks self-selected work. |
| `auto-improvement` | pull requests | Opened by the daily automation. |
| `polish` | pull requests, issues | Polish-day pull request, or a deferred jank finding. |
| `feature` | pull requests | Small or Medium feature-day pull request. |
| `major-feature` | pull requests, issues | Major feature milestone, or its tracking issue. |

Create a missing label with `gh label create` rather than skipping it; the
labels are how later runs find the major cadence and journey rotation.

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
- polish days: a before-and-after screenshot pair for each fixed finding at
  the same viewport, and the jank log in the pull request
- feature days: browser assertions for the new capability, including what a
  recipient or non-member must not see
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
