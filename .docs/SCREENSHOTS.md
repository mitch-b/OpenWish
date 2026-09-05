# Screenshot and browser evidence

Use screenshots as supporting evidence for a tested user journey, not as the
only proof that a change works.

## Permanent documentation

Commit only screenshots that explain the current product in `README.md` or
long-lived documentation. Store them under `.docs/images/`, use descriptive
kebab-case names, remove superseded images, and optimize them before commit.

## Pull requests

Upload review-only screenshots as GitHub user attachments and embed the
generated URL in the pull request body. This keeps evidence available with the
review without permanently increasing every clone of the repository.

## Releases

Attach larger evidence bundles to the GitHub release when they need to remain
available after the pull request. Do not use an orphan branch as an asset
store: it has unclear retention, bypasses the normal documentation review
path, and can leave broken links after force-pushes or cleanup.

## Capture workflow

Run `scripts/verify-e2e.sh`. The script starts an isolated verification stack,
runs committed Playwright assertions, and writes desktop and mobile images to
`.docs/images/verification/`.
