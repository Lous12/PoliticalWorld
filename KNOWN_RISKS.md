# Known risks — read before touching the weird parts

This is not a bug list. It is a map of areas where "small cleanup" can cause large breakage.

## 1. Legacy `ukiol_*` IDs

These identifiers are old and ugly. Some are also part of save compatibility.

Do not mass-rename them. If you want to replace one, first prove where it is stored and design a migration.

## 2. Persistence and save/load

Typical failure modes:

- duplicated state after load;
- state silently reset to defaults;
- runtime references restored as if they were stable IDs;
- simulation firing during load and mutating the world before restore finishes.

Any persistence change needs both a fresh-world test and an existing-save roundtrip.

## 3. Dynamic country names

Country naming may involve base/manual country name, ideology/government formatting, rank/title progression, localization, cached names and update triggers.

A visible political name must not accidentally become the next canonical base and then be wrapped again.

Repro tests should include manual rename + ideology change + rank change.

## 4. Native UI tabs

Risks include another mod creating its own native tab, multiple panels sharing one window, selecting the wrong active ScrollRect, stale tab state and repeated switching breaking something that worked once.

Stress-test repeated switching, reopening the window and changing the selected kingdom/city.

## 5. Alliance / bloc synchronization

Loading an existing world must not accidentally create/destroy alliances or trigger autonomous bloc changes before restore is complete.

## 6. War and diplomacy patches

Harmony patches around war start/diplomacy are tied closely to WorldBox internals. After a WorldBox update, verify method signatures.

## 7. Public API 1.x

Before changing a public method, search the docs/examples, consider existing addons and prefer additive capability changes.

## 8. Performance

Avoid per-frame full-world scans, repeated reflection in hot loops, rebuilding UI every refresh tick and recalculating expensive derived data when a cached/event-driven path exists.

## 9. Localization

Missing translation should degrade gracefully. Do not make a new language key mandatory for core logic to function.

## 10. "Cleanup"

The most dangerous sentence in this repository is:

> this looks redundant, let's delete it.

Before deleting weird compatibility code, find out what save/event/UI path reaches it.
