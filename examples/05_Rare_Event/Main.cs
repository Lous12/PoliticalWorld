using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example05
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example05";
        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId, Name = "PW SDK Example 05", Version = "1.0.0", Author = "Lous12"
            })) return;

            PoliticalWorldAPI.RegisterRarePoliticalEvent(AddonId,
                new PoliticalWorldAPI.RarePoliticalEventDefinition
                {
                    Id = AddonId + ".low_stability_warning",
                    DisplayName = "Low Stability Warning",
                    Description = "Harmless SDK example: publishes a log-style political event.",
                    CheckIntervalYears = 2,
                    CooldownYears = 10,
                    ChancePermille = 25,
                    Condition = PoliticalWorldAPI.Conditions.StabilityAtMost(35),
                    Handler = kingdom => PoliticalWorldAPI.PublishKingdomEvent(
                        kingdom,
                        "Political tension is rising.",
                        AddonId + ".warning",
                        2f)
                });
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
