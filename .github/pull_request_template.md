## User outcome

Describe the user problem solved and the `PRODUCT_DIRECTION.md` value area it
improves.

- Day type: <!-- Polish / Feature / Major feature / Override / Owner request -->
- Journey: <!-- J1-J8 on polish days; delete otherwise -->
- Roadmap: <!-- for example A3; delete when not a roadmap item -->
- Issues: <!-- "Closes #N" for each autowork or polish issue this completes,
  and for a major-feature tracking issue on its final milestone;
  "Part of #N" for an earlier milestone; delete when none -->
- Labels: <!-- auto-improvement plus one of polish, feature, major-feature,
  or override; add autowork for an owner issue -->

## Jank log

<!-- Polish days only: each finding, where it occurs, severity
(blocks / annoys / cosmetic), and whether it was fixed, deferred to an issue,
or left as a nit. Delete this section on other days. -->

## Changes

| Area | Summary |
|---|---|
| Product | |
| Verification | |
| Documentation | |

## Evidence

- UI impact: <!-- yes / no; include visible behavior changes from backend code -->
- Release note:
- Desktop screenshot:
- Mobile screenshot:
- Before-and-after screenshots (polish days):
<!-- UI-impacting PRs must embed real desktop and mobile images on the fields
above: ![Descriptive alt text](https://github.com/user-attachments/assets/UUID).
Capture the affected journey on the latest revision, not unrelated baseline
screens. Artifact links and local paths do not count. Stage review-only files
in ignored pr-assets/ and upload them; never commit them.
See .docs/SCREENSHOTS.md for capture and upload commands. -->
- Browser assertions:
- API assertions:
- Server log result:

## Verification

- [ ] `dotnet format --verify-no-changes`
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `scripts/verify-e2e.sh`
- [ ] UI impact is declared; affected desktop/mobile screenshots are embedded above (or no UI impact)
- [ ] No secrets or real user data are included
- [ ] User-visible behavior is documented in `.docs/releases/`
