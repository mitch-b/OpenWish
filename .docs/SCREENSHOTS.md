# Screenshot and browser evidence

Use screenshots as supporting evidence for a tested user journey, not as the
only proof that a change works.

## Permanent documentation

Commit only screenshots that explain the current product in `README.md` or
long-lived documentation. Store them under `.docs/images/`, use descriptive
kebab-case names, remove superseded images, and optimize them before commit.
The walkthrough captures in `.docs/images/walkthrough/`, including the
README's compact `readme-*.png` views, are viewport-bound views of the
synthetic multi-user scenario, not full-page captures. Keep them concise and
focused on one useful state so the README reads as a product story rather than
an evidence archive.

## Pull requests

Upload review-only screenshots as GitHub user attachments and embed the
generated URL in the pull request body. This keeps evidence available with the
review without permanently increasing every clone of the repository.

Use the ignored repository-root `pr-assets/` directory as the local staging
area, not an asset branch. Never force-add its contents. The `screenshots` CI
check rejects committed `pr-assets/` files and requires embedded images in both
the `Desktop screenshot:` and `Mobile screenshot:` PR fields when web markup,
code-behind, styles, scripts, localized copy, fonts, or images change. Deleted
and renamed UI files count too. It reruns when the PR body is edited.

Declare `- UI impact: yes` for any user-visible change, including changes
implemented only in application services or other backend code. Non-UI changes
can declare `- UI impact: no` and use `N/A` in the screenshot fields. Detected
UI source changes still require images even if declared non-UI. This is a
conservative evidence check, not visual verification: reviewers must inspect
the images for relevance and freshness.

Capture the affected journey at desktop and mobile widths on the latest PR
revision using synthetic data. Include loading, empty, validation, or dialog
states when they change. For polish, include before/after views. Keep captures
viewport-bound and focused; unrelated dashboard screenshots do not demonstrate
a changed form. Give every embedded image descriptive alt text. CI artifact
links are useful for logs and assertion results but do not render images in
the PR and do not satisfy the screenshot requirement.

### Upload and embed

With GitHub CLI 2.99.0 or newer, first create the PR using the repository
template. Put local image references directly on its screenshot fields, then
attach those files to the existing PR:

```markdown
- UI impact: yes
- Desktop screenshot: ![Updated wishlist on desktop](./pr-assets/desktop.png)
- Mobile screenshot: ![Updated wishlist on mobile](./pr-assets/mobile.png)
```

```bash
gh pr edit <PR-number> --repo mitch-b/OpenWish \
  --attach ./pr-assets/desktop.png --attach ./pr-assets/mobile.png
gh pr view <PR-number> --repo mitch-b/OpenWish --json body --jq .body
```

The attachment-only edit preserves the body and replaces matching local image
references with GitHub-hosted URLs. Check that **both** references now contain
`https://github.com/user-attachments/assets/` and no local image paths remain.
If an upload partly fails, retry only the failed file, not the whole command.
Do not merge until the `screenshots`, `dotnet`, and `browser` checks are green
on the latest revision.

If the CLI is older or its credential cannot upload attachments, use the
GitHub PR web editor to upload the images and place the generated Markdown on
the same fields. Do not substitute a local path, an expiring Actions artifact,
or a committed evidence archive.

## Releases

Attach larger evidence bundles to the GitHub release when they need to remain
available after the pull request. Do not use an orphan branch as an asset
store: it has unclear retention, bypasses the normal documentation review
path, and can leave broken links after force-pushes or cleanup.

## Capture workflow

Run `scripts/verify-e2e.sh`. The script starts an isolated verification stack,
runs committed multi-user Playwright assertions, writes machine-readable
results to ignored `pr-assets/verification/`, and generates walkthrough
captures in ignored `pr-assets/walkthrough/`. The `browser-evidence` Actions
artifact contains both directories and is retained for 14 days; uploaded PR
images remain with the review independently of that retention.

To intentionally refresh permanent documentation instead:

```bash
OPENWISH_WALKTHROUGH_DIR="$PWD/.docs/images/walkthrough" scripts/verify-e2e.sh
```

Commit only the documentation images needed for that update, not the review
bundle. `OPENWISH_EVIDENCE_DIR` and `OPENWISH_WALKTHROUGH_DIR` can also select
other absolute workspace-local output directories.
