# Самостоятельный NeoModLoader-мод

Не каждый проект должен зависеть от Political World. В SDK есть `WorldBox-NeoMod-Starter`, который зависит только от NML.

Минимальный входной класс:

```csharp
using NeoModLoader.api;

namespace YourName.MyWorldBoxMod
{
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            LogInfo("Loaded.");
        }
    }
}
```

`BasicMod<T>` — официальный базовый класс NML для простого мода. NML вызывает `OnModLoad`, затем обычный Unity lifecycle; сам `BasicMod` также создаёт feature manager и загружает `Locales`.

Для UI начинайте с NML feature API, описанного в `nml-ui-recipes.md`.

Political World API подключайте только если ваш мод действительно хочет интеграцию с политикой. Не делайте ненужную hard dependency.
