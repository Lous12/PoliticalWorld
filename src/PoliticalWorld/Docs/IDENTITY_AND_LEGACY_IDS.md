# Political World identity and legacy IDs

## Canonical identity (1.7.0 internal foundation)

- Mod / dependency ID: `Lous12.PoliticalWorld`
- Public namespace: `Lous12.PoliticalWorld`
- Public API: `Lous12.PoliticalWorld.PoliticalWorldAPI`
- Author identity: `Lous12`

Third-party addons should depend on `Lous12.PoliticalWorld` and use only the public API.

## Why do some internal IDs still start with `ukiol_`?

Political World existed before the public addon platform. Many gameplay assets, localization keys,
traits, powers, ideology IDs, currents, map IDs, and saved values already use `ukiol_` identifiers.
Those strings are legacy persistent content IDs, not the current author/public namespace.

They are intentionally kept stable to protect existing saves and avoid unnecessary migrations.
Do not use the `ukiol_` prefix for new public addon IDs.

## New addon IDs

Use a globally unique addon namespace, for example:

`someauthor.DragonPolitics`

and content IDs owned by that addon, for example:

`someauthor.DragonPolitics.ideology_dragonism`

The Political World public API will continue moving toward explicit namespaced registries and
safe addon-owned storage so authors do not need to touch legacy internal IDs.
