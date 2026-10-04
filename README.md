<p align="center">
  <img src="docs/assets/hero-banner.png" alt="Political World banner" width="100%">
</p>

<h1 align="center">Political World</h1>
<p align="center"><strong>Politics mod for WorldBox • Public API • Open development</strong></p>

<p align="center">
  <a href="README_RU.md">Русский</a> ·
  <a href="https://lous12.github.io/PoliticalWorld/">Website</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a> ·
  <a href="https://discord.gg/kYH5GadndE">Discord</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/issues">Issues</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/pulls">Pull Requests</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.11.0-2563eb">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.19.0-8b5cf6">
  <img alt="WorldBox target" src="https://img.shields.io/badge/WorldBox%20build-719-0ea5e9">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NML-1.2.0.1-10b981">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-22c55e">
</p>

> **Play politics. Build anything.**

Political World is a politics simulation mod for WorldBox and an addon framework exposed through `PoliticalWorldAPI`.

## Current status

- **Political World:** 1.11.0
- **Public API:** 1.19.0
- **Target game build:** 719
- **NeoModLoader:** 1.2.0.1
- **Development status:** active large feature development is currently on a break
- **Current focus:** maintenance, critical fixes, repository cleanup, documentation and community development

## 1.11 highlights

- Monarchy rank progression from smaller titles up to Kingdom and Empire.
- Republican rulers now change correctly when another party wins an election.
- Internal party factions and dynamic party splits.
- Splinter parties can move toward nearby ideologies or, in severe crises, make rarer major ideological breaks.
- Political alliances / electoral blocs that can combine support in elections and later collapse.
- Improved long-term party generation.

Community credits for 1.11: **@Asriel**, **@Mauro**, **@Mars**.

## Quick links

- [Project website](https://lous12.github.io/PoliticalWorld/)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869)
- [Discord](https://discord.gg/kYH5GadndE)
- [Getting Started](docs/en/GETTING_STARTED.md)
- [API 1.19 Reference](docs/en/API_REFERENCE_1_19.md)
- [Examples](examples/README.md)
- [Addon template](templates/PoliticalWorld-Addon-Template)
- [Community Addons](https://lous12.github.io/PoliticalWorld/en/community-addons.html)
- [Issues](https://github.com/Lous12/PoliticalWorld/issues)
- [Pull Requests](https://github.com/Lous12/PoliticalWorld/pulls)
- [Forks](https://github.com/Lous12/PoliticalWorld/forks)

## For players

Political World adds ideologies, parties, governments, elections, political stability, separatism, political crises, international blocs, summits, war preparation, political map modes, dynamic country names, settlement politics and long-term party evolution.

## For creators

PoliticalWorldAPI 1.19 includes addon registration, capability discovery, localization fallback, data and tags, event hooks, rare political events, actions, ideology and government registries, party and country access, warfare helpers, UI integration, world lifecycle access, release helpers and diagnostics.

The public API is split across partial source files under `src/PoliticalWorld/API/`. The source is the canonical contract when prose documentation is incomplete.

Start with:

- [AI_START_HERE.md](AI_START_HERE.md)
- [AGENTS.md](AGENTS.md)
- [API source map](src/PoliticalWorld/API/README.md)
- [Examples](examples/README.md)

## Open source, forks and contributions

Political World is open source under the MIT License. Forks, focused patches, PRs, experiments, documentation fixes and AI-assisted contributions are welcome.

Core contributors should read:

- [AGENTS.md](AGENTS.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [KNOWN_RISKS.md](KNOWN_RISKS.md)
- [DEVELOPMENT.md](DEVELOPMENT.md)

Forks should clearly identify themselves as unofficial and keep the original MIT notice. Official Political World releases still come from the main project maintained by Lous12.

## Community

Discord is the easiest place for normal discussion, testing, ideas and addon experiments. GitHub Issues/PRs are better for reproducible bugs and code changes.

## Support the project

Political World, the Public API, source code and documentation remain free. Support is completely optional.

- [DonationAlerts](https://www.donationalerts.com/r/lous12)
- **USDT — TRC20 / TRON:** `TAooa2bwstvhrSPnTaDZjBNGHZ1j5zDB4p`
- **USDT — TON:** `UQCppGv_A8uf07Ws_zyPw_U7XRnhafM2TDd1ABR1DQrfGA73`

> Check the network before sending. USDT on TRC20/TRON and USDT on TON are different networks.

## License

Released under the [MIT License](LICENSE).
