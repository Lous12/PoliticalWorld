using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example04
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example04";

        protected override void OnModLoad()
        {
            // Minimum for this example. Current PoliticalWorldAPI is newer.
            if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 04",
                Version = "1.0.0",
                Author = "Lous12",
                RequiredApiMajor = 1,
                RequiredApiMinor = 6
            })) return;

            // Prefer event subscriptions over polling the entire world in Update().
            PoliticalWorldAPI.Subscribe(AddonId, PoliticalWorldAPI.Events.GovernmentChanged, OnEvent);
            PoliticalWorldAPI.Subscribe(AddonId, PoliticalWorldAPI.Events.ElectionFinished, OnEvent);
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }

        private static void OnEvent(PoliticalWorldAPI.PoliticalEventData data)
        {
            if (data == null) return;

            NeoModLoader.services.LogService.LogInfo(
                "[PW SDK Example 04] " + data.EventId + " / " + data.KingdomName
            );
        }
    }
}
