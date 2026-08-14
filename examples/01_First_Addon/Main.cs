using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example01
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example01";
        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
            PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId, Name = "PW SDK Example 01", Version = "1.0.0", Author = "Lous12",
                Description = "Minimal external addon example"
            });
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
            LogInfo("Example 01 loaded.");
        }
    }
}
