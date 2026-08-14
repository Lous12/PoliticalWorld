using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace YourName.MyPoliticalAddon
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "YourName.MyPoliticalAddon";

        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 6))
            {
                LogError("Political World API 1.6+ is required.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "My Political World Addon",
                Version = "0.1.0",
                Author = "YourName",
                Description = "Started from Political World SDK 1.6"
            }))
            {
                return;
            }

            // Register ideologies/governments/actions/events here.
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
            LogInfo("Loaded with PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion);
        }
    }
}
