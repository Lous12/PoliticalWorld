# NeoModLoader UI Recipes

This section is useful for both Political World addons and standalone NML mods.

## What NML already provides

Current NeoModLoader includes feature classes such as:

- `ModPowerTabFeature` — produces a `PowersTab`; subclasses implement `PositionButton(PowerButton)`.
- `ModButtonFeature<TPowersTabFeature>` — produces a `PowerButton`, requires the tab feature, and asks the tab to position the button.
- `ModWindowButtonFeature<TWindowFeature, TPowersTabFeature>` — a button for a `ScrollWindow`; it requires both window and tab features and uses `SpritePath` plus `WindowOpenAction`.
- `ModGodPowerButtonFeature` — a specialized path for God Power buttons.

NML source:

- https://github.com/WorldBoxOpenMods/ModLoader/tree/master/api/features
- https://github.com/WorldBoxOpenMods/ModLoader/blob/master/api/features/ModPowerTabFeature.cs
- https://github.com/WorldBoxOpenMods/ModLoader/blob/master/api/features/ModWindowButtonFeature.cs

## Recipe: separate tab

1. Create a feature inheriting `ModPowerTabFeature`.
2. Create/return the `PowersTab` through the `ModObjectFeature` implementation.
3. Implement `PositionButton` so every button follows one layout policy.
4. Implement buttons as separate features instead of repeatedly calling `GameObject.Find` every frame.

## Recipe: button opens a window

When the window is a `ScrollWindow`, prefer `ModWindowButtonFeature`: NML already provides the relationship between the window feature, tab feature, sprite, and click action.

## Performance

- do not search UI objects with `GameObject.Find` in every `Update`;
- refresh expensive lists only while the window is open or when data actually changes;
- cache references to created elements;
- avoid Harmony when the NML feature API already solves the task;
- use scrolling for large lists instead of an unbounded map overlay.

## Political World visual language

Related addons are encouraged to keep the vanilla/NML language: calm dark panels, vanilla-like frames, small pixel icons, and no huge bright overlays. This is a design recommendation, not an API requirement.
