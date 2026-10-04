# 08 — World Query + Addon Data

Waits for the public `WorldReady` lifecycle event, requests a capped kingdom snapshot, and stores one addon-owned integer on each reached kingdom.

The important parts are the boundaries:
- the query is capped;
- the addon does not keep the manager collection;
- data is namespaced through `PoliticalWorldAPI.Data` and follows the object's save lifecycle.

Minimum API used: **1.11+**.
