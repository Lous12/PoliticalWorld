# Contributing to Political World

Contributions are welcome: code, documentation, examples, translations, bug reports, API proposals and compatibility fixes.

## Before coding

- Read `ARCHITECTURE.md`.
- Search existing issues/PRs before duplicating work.
- For a large mechanic or public API redesign, open an issue first.

## Code rules

- Keep `Main.cs` as a tiny entry point.
- Put new code in the narrowest fitting module.
- Do not mass-rename legacy `ukiol_*` identifiers.
- Prefer existing WorldBox/NML systems over duplicate frameworks.
- Prefer event-driven/staggered work over permanent heavy polling.
- Public addon features belong behind `PoliticalWorldAPI`; do not make addons consume internals.
- A third-party callback must not be able to crash the entire API pipeline when safe isolation is possible.

## Public API changes

- Preserve API 1.x compatibility.
- Add capability flags for optional systems.
- Validate addon-owned IDs and state.
- Update both RU and EN documentation.
- Add or update a minimal example when introducing a new API pattern.

## Testing

For runtime changes, include:
- WorldBox version/build;
- NeoModLoader version;
- Political World version;
- what you tested;
- relevant `Player.log` lines when fixing an error.

Structural refactors should avoid changing gameplay behavior in the same change whenever possible.

## Pull requests

Keep PRs focused. Explain what changed, why, compatibility risk, and how it was tested. Screenshots are welcome for UI changes.
