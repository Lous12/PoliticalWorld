# Частые ошибки

## Аддон лежит внутри PoliticalWorld
NML рекурсивно собирает `.cs`, поэтому вы получите конфликт типов/классов. Аддон должен быть отдельной папкой рядом.

## Забыт Dependencies
В `mod.json` аддона должно быть `"Dependencies": ["Lous12.PoliticalWorld"]`, иначе порядок компиляции/assembly reference не гарантирован.

## Используется старое имя
Новая публичная идентичность — `Lous12.PoliticalWorld` и namespace `Lous12.PoliticalWorld`. `ukiol_*` внутри core — только legacy save/content IDs.

## Контентный ID не принадлежит аддону
Используйте `AddonId + ".something"`. Validation специально отклоняет чужие ID.

## RegisterAddon не вызван первым
Сначала зарегистрируйте аддон, потом контент и подписки.

## Постоянный Update сканирует мир
Для политических переходов используйте Event Bus, для редких kingdom-level эффектов — Rare Political Event Registry.

## Общий тег используется как приватное состояние
Для внутреннего состояния используйте `AddAddonKingdomTag` и addon data.

## Мод лезет в Main/ScenarioBridge
Это internal implementation. Если публичной возможности нет — зафиксируйте missing capability.

## После ошибки присылается только скрин
Лучше полный `Player.log` или хотя бы полный compile error: номер файла, строка, код ошибки, текст.
