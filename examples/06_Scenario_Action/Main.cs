using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example06
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example06";
        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId, Name = "PW SDK Example 06", Version = "1.0.0", Author = "Lous12"
            })) return;

            PoliticalWorldAPI.RegisterAction(AddonId, new PoliticalWorldAPI.ActionDefinition
            {
                Id = AddonId + ".stabilize",
                Category = "sdk-example",
                DisplayName = "SDK: +5 stability",
                Description = "Explicit action for Scenario Tools-style integration.",
                SortOrder = 100,
                Condition = PoliticalWorldAPI.Conditions.StabilityAtMost(95),
                Handler = kingdom => PoliticalWorldAPI.ChangeKingdomStability(kingdom, 5)
            });
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
