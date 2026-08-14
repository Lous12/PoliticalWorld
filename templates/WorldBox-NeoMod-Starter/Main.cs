using NeoModLoader.api;

namespace YourName.MyWorldBoxMod
{
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            LogInfo("Standalone NeoMod loaded.");
        }
    }
}
