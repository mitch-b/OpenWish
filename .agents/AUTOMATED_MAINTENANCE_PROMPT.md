# Automated maintenance prompt

Use the following prompt for a scheduled unattended OpenWish improvement:

> Work unattended on one meaningful, production-quality OpenWish increment
> that a person using OpenWish would notice.
> Start from a clean checkout of the repository default branch. Before
> changing code, ensure no other open pull request labeled
> `auto-improvement` exists, then create a unique feature branch. Read
> `PRODUCT_DIRECTION.md` and `PLAN.md`, and follow the daily increment rules
> exactly: handle an override if one exists; otherwise determine whether
> today is a polish day or a feature day from the calendar date. Inspect open
> issues labeled `autowork`, `polish`, and `major-feature`, recent merged pull
> requests and their labels, and the running product. On a polish day, walk
> the selected journey on phone and desktop, write a jank log, and fix the
> most impactful findings in that journey even when no bug has been reported.
> On a feature day, deliver an `autowork` capability, the next milestone of an
> in-progress major feature, a new major feature when the major cadence is
> due, or the next Small or Medium roadmap item. Never submit work listed
> under "What does not count as an increment" as the day's outcome unless an
> exception documented there applies: the owner requested it in an
> `autowork` issue, or an override requires it.
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
> Development-only synthetic login and fixture data. Review the full diff and
> remediate actionable findings. When a persistent local review environment is
> needed, use `scripts/agent-environment.sh deploy`; never point automation at
> the developer's Aspire resources or bypass its verification-first promotion.
>
> Only when every gate passes, commit and push the branch and open a pull
> request to `main`. Apply the `auto-improvement` label and exactly one type
> label: `polish`, `feature`, `major-feature`, or `override` for override
> work. Also apply `autowork` when the increment implements a labeled issue.
> Fill in the pull request template, including the day type, the `Journey:`
> or `Roadmap:` line that later runs use for rotation and cadence, and
> `Closes #N` for each `autowork` or `polish` issue the increment completes
> (`Part of #N` for an unfinished major-feature tracking issue). The PR body
> must state the user outcome, the value area it improves, implementation
> scope, exact verification commands and observed result, release note,
> screenshot paths or GitHub attachment URLs, and the most valuable next
> increment. Only after the pull request links the issues it completes, file
> deferred polish findings and update the major-feature tracking issue as
> `PLAN.md` describes. If any prerequisite or gate fails, leave no new PR and
> report the blocker.

The automation host may adapt checkout paths, unique branch names, and
artifact destinations, but it should not weaken the acceptance gates.
