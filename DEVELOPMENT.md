# Development guide

Current baseline:

- Political World 1.11.0
- PoliticalWorldAPI 1.19.0
- WorldBox 0.51.2 / build 719
- NeoModLoader 1.2.0.1

## Repository map

- `src/PoliticalWorld/` — the actual mod
- `src/PoliticalWorld/API/` — public addon API
- `docs/en/`, `docs/ru/` — public documentation
- `examples/` — API examples
- `templates/` — starter projects
- `.github/` — issue/PR/discussion templates

## Normal workflow

1. Reproduce the problem first.
2. Identify the smallest owning module.
3. Read `KNOWN_RISKS.md` for that area.
4. Make one focused change.
5. Compile under the same WorldBox/NML baseline.
6. Test the exact reproduction.
7. Test the nearest related behavior.
8. Save/load if persistent state is involved.
9. Include useful `Player.log` lines in the PR/report.

## What to test by change type

### Politics / parties / ideology

Test fresh world, existing world, relevant elections/leadership and enough simulation time for delayed logic.

### Dynamic names

Test generated name, manual rename, ideology change, rank/government change and save/load. Test another locale when localization is involved.

### UI

Open/close repeatedly, switch tabs repeatedly, switch selected kingdom/city, and test alongside another mod extending the same vanilla window when possible.

### International / warfare

Test existing alliances/wars on load plus new diplomacy/war flows.

### API

Test both core behavior and a minimal addon using the changed public capability.

## PR style

Keep PRs focused. One naming bug, one API capability, one save migration or one UI conflict is good. Giant mixed rewrites are not.

AI-assisted patches are welcome, but they need the same runtime evidence as human-written code.
