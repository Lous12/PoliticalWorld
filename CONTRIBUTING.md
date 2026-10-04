# Contributing to Political World

Contributions are welcome: code, documentation, examples, translations, bug reports, API proposals, compatibility fixes, forks and AI-assisted patches.

## Before coding

Read:

- `AGENTS.md`
- `ARCHITECTURE.md`
- `KNOWN_RISKS.md`
- `DEVELOPMENT.md`

For a large mechanic or public API redesign, open an issue/discussion first.

## Code rules

- Keep `Main.cs` tiny.
- Put new code in the narrowest fitting module.
- Do not mass-rename legacy `ukiol_*` identifiers.
- Prefer existing WorldBox/NML systems over duplicate frameworks.
- Prefer event-driven/staggered work over permanent heavy polling.
- Public addon features belong behind `PoliticalWorldAPI`.
- Comments should explain traps and reasons, not obvious syntax.

## AI-assisted contributions

AI help is allowed.

Do not submit a giant generated rewrite you did not test.

For AI-assisted changes:

- make sure the assistant read the actual source;
- do not invent API methods;
- do not bypass missing addon API with reflection;
- keep the diff focused;
- provide runtime test evidence.

## Public API changes

Preserve API 1.x compatibility when practical, add capability flags for optional systems, validate addon-owned IDs/state, and update both RU and EN docs.

## Testing

For runtime changes include WorldBox/NML/PW versions, reproduction steps, relevant `Player.log` lines and save/load results when persistence is involved.

## Forks

Forks are welcome under the MIT License.

Please clearly mark a fork as unofficial so players do not confuse it with the main Political World release.

If your fork fixes something useful, a focused PR back to the main project is welcome.
