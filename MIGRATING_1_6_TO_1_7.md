# Migrating from Political World 1.6 to 1.7

## Players

1. Back up important worlds before testing a beta build.
2. When installing manually, remove old legacy mod folders such as `UkiolFirstMod` before installing `PoliticalWorld`.
3. The project GUID is now `Lous12.PoliticalWorld`.
4. Historical `ukiol_*` gameplay/save identifiers are intentionally retained, so do not rename them in saves or source files.

## Addon developers

Internal/test addons written before the public API identity was finalized must update:

```text
old dependency / namespace: ukiol.PoliticalWorld / legacy namespace
new dependency GUID:        Lous12.PoliticalWorld
new C# namespace:           Lous12.PoliticalWorld
```

Use:

```json
"Dependencies": ["Lous12.PoliticalWorld"]
```

and:

```csharp
using Lous12.PoliticalWorld;
```

Do not migrate old `ukiol_*` content/save IDs just because the author identity changed. They are compatibility IDs, not branding.
