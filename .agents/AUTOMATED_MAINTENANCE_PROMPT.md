# Automated maintenance prompt

Use the following prompt for a scheduled unattended OpenWish improvement:

> Work unattended on one meaningful, production-quality OpenWish increment.
> Start from a clean checkout of the repository default branch. Before
> changing code, ensure no other open automated-maintenance pull request
> exists, then create a unique feature branch. Read `PRODUCT_DIRECTION.md`
> and `PLAN.md`. Inspect open GitHub issues labeled `autowork`, recent merged
> pull requests, and the current product. Select one bounded user outcome that
> follows the product decision rule; if there is no labeled issue, perform
> only one high-value maintenance increment allowed by the fallback rule.
> Never modify `PRODUCT_DIRECTION.md` during a routine increment.
>
> Implement a complete vertical slice using existing architecture and
> authorization boundaries. Do not weaken authentication, expose secrets,
> use real user data, perform destructive migrations, or change production
> infrastructure. Add focused tests and update documentation. For a
> user-visible change, run `scripts/bump-version.sh` once with the appropriate
> semantic version level, add a dated note under `.docs/releases/`, and update
> `CHANGELOG.md` and `src/OpenWish.Web/wwwroot/releases.json` consistently.
>
> Run `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`, and
> `scripts/verify-e2e.sh`. The browser test is a hard acceptance gate: require
> authenticated API assertions, data-bearing UI assertions, no browser
> errors, no failed responses, clean web logs, and desktop/mobile screenshots.
> Use only the repository-provided Docker Playwright workflow and its
> Development-only synthetic login. Review the full diff and remediate
> actionable findings.
>
> Only when every gate passes, commit and push the branch and open a pull
> request to `main`. Apply the `autowork` label when the increment implements
> a labeled issue. The PR body must state the user outcome, product-direction
> principle, implementation scope, exact verification commands and observed
> result, release note, screenshot paths or GitHub attachment URLs, and the
> most valuable next increment. If any prerequisite or gate fails, leave no
> new PR and report the blocker.

The automation host may adapt checkout paths, unique branch names, and
artifact destinations, but it should not weaken the acceptance gates.
