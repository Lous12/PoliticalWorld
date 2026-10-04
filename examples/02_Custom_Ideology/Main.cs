using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example02
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example02";

        protected override void OnModLoad()
        {
            // Minimum for this example. Current PoliticalWorldAPI is newer.
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 02",
                Version = "1.0.0",
                Author = "Lous12",
                RequiredApiMajor = 1,
                RequiredApiMinor = 6
            })) return;

            PoliticalWorldAPI.RegisterIdeology(AddonId, new PoliticalWorldAPI.IdeologyDefinition
            {
                Id = AddonId + ".arcane_reformism",
                ParentId = "",
                NameKey = AddonId + ".arcane_reformism",
                HighSupportStability = 1,
                SupportThreshold = 55,
                LowSupportStability = -1,
                DiffusionMultiplier = 1.0f,
                // Zero means this addon ideology will not randomly spawn unless
                // some addon/gameplay logic deliberately introduces it.
                RandomWeight = 0,
                Tags = new[] { "magic", "reformist", "sdk-example" }
            });

            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
