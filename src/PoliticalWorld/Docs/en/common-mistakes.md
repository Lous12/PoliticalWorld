# Common mistakes

## The addon is inside PoliticalWorld
NML recursively compiles `.cs`, so this causes duplicate/conflicting types. The addon must be a separate sibling mod folder.

## Missing Dependencies
The addon `mod.json` should contain `"Dependencies": ["Lous12.PoliticalWorld"]`; otherwise compile order/assembly references are not guaranteed.

## Old public identity is used
The public identity is now `Lous12.PoliticalWorld` and namespace `Lous12.PoliticalWorld`. Internal `ukiol_*` values are legacy save/content IDs only.

## Content ID is not owned by the addon
Use `AddonId + ".something"`. Validation intentionally rejects IDs owned by someone else.

## RegisterAddon was not called first
Register the addon before content and subscriptions.

## Permanent Update scans the world
Use Event Bus for political transitions and Rare Political Event Registry for rare kingdom-level effects.

## Shared tag is used as private state
Use `AddAddonKingdomTag` and addon data for internal state.

## The mod reaches into Main/ScenarioBridge
Those are internal implementation details. If the public feature is missing, record a missing capability instead.

## Only a screenshot is provided after a failure
Prefer the full `Player.log` or at least the complete compile error: file, line, error code, and message.
