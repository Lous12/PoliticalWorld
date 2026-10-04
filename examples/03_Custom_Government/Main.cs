using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example03
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example03";

        protected override void OnModLoad()
        {
            // Minimum for this example. Current PoliticalWorldAPI is newer.
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 03",
                Version = "1.0.0",
                Author = "Lous12",
                RequiredApiMajor = 1,
                RequiredApiMinor = 6
            })) return;

            PoliticalWorldAPI.RegisterGovernment(AddonId, new PoliticalWorldAPI.GovernmentDefinition
            {
                Id = AddonId + ".magocracy",
                NameKey = AddonId + ".magocracy",
                DisplayName = "Magocracy",
                // Custom governments reuse a core mechanical archetype instead
                // of reaching into PW internals and cloning government logic.
                BaseArchetype = PoliticalWorldAPI.GovernmentArchetype.Oligarchy,
                Tags = new[] { "magic", "sdk-example" }
            });

            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
