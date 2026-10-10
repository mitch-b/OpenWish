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

- Release note:
- Desktop screenshot:
- Mobile screenshot:
- Before-and-after screenshots (polish days):
- Browser assertions:
- API assertions:
- Server log result:

## Verification

- [ ] `dotnet format --verify-no-changes`
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `scripts/verify-e2e.sh`
- [ ] No secrets or real user data are included
- [ ] User-visible behavior is documented in `.docs/releases/`
