using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example01
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example01";

        protected override void OnModLoad()
        {
            // 1.6 is the minimum needed by this tiny example, not the current API.
            if (!PoliticalWorldAPI.IsCompatible(1, 6))
            {
                LogError("Political World API 1.6+ is required by this example.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 01",
                Version = "1.0.0",
                Author = "Lous12",
                Description = "Minimal external addon example",
                RequiredApiMajor = 1,
                RequiredApiMinor = 6
            }))
            {
                return;
            }

            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
            LogInfo("Example 01 loaded with PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion + ".");
        }
    }
}
