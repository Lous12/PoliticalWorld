# NeoModLoader UI Recipes

Этот раздел полезен и для аддонов Political World, и для самостоятельных NML-модов.

## Что уже предоставляет NML

В текущем NeoModLoader есть feature-классы:

- `ModPowerTabFeature` — feature, создающий `PowersTab`; наследник задаёт `PositionButton(PowerButton)`.
- `ModButtonFeature<TPowersTabFeature>` — создаёт `PowerButton`, требует tab feature и автоматически просит tab разместить кнопку.
- `ModWindowButtonFeature<TWindowFeature, TPowersTabFeature>` — кнопка для `ScrollWindow`; требует window + tab feature, использует `SpritePath` и `WindowOpenAction`.
- `ModGodPowerButtonFeature` — специализированный путь для God Power кнопок.

Исходники NML:

- https://github.com/WorldBoxOpenMods/ModLoader/tree/master/api/features
- https://github.com/WorldBoxOpenMods/ModLoader/blob/master/api/features/ModPowerTabFeature.cs
- https://github.com/WorldBoxOpenMods/ModLoader/blob/master/api/features/ModWindowButtonFeature.cs

## Рецепт: отдельная вкладка

1. Сделайте feature, наследующий `ModPowerTabFeature`.
2. Создайте/верните `PowersTab` в реализации `ModObjectFeature`.
3. Реализуйте `PositionButton`, чтобы все ваши кнопки размещались единообразно.
4. Кнопки оформляйте отдельными features, а не `GameObject.Find` каждый кадр.

## Рецепт: кнопка открывает окно

Если окно — `ScrollWindow`, используйте `ModWindowButtonFeature`: NML уже умеет связать window feature, tab feature, sprite и click action.

## Производительность

- не ищите UI-объекты через `GameObject.Find` в каждом `Update`;
- обновляйте тяжёлые списки только пока окно открыто или когда данные реально изменились;
- кэшируйте ссылки на созданные элементы;
- не ставьте Harmony-патч, если NML feature API уже решает задачу;
- большие списки делайте скроллируемыми, не создавайте бесконечный overlay поверх карты.

## Визуальный стиль Political World

Для связанных аддонов рекомендуется сохранять ванильный/NML-язык: тёмные спокойные панели, ванильные frames, маленькие pixel icons, без огромных ярких overlay. Это рекомендация дизайна, а не требование API.
