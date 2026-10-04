using System.Collections.Generic;
using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example08
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example08";

        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 11))
            {
                LogError("Political World API 1.11+ is required by this example.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 08",
                Version = "1.0.0",
                Author = "Lous12",
                Description = "Capped world query and addon-owned save data example",
                RequiredApiMajor = 1,
                RequiredApiMinor = 11
            })) return;

            PoliticalWorldAPI.Subscribe(AddonId, PoliticalWorldAPI.Events.WorldReady, OnWorldReady);
        }

        private static void OnWorldReady(PoliticalWorldAPI.PoliticalEventData data)
        {
            // WorldQuery returns a capped snapshot. Do not replace this with an
            // unbounded manager scan just because the test world is small.
            List<Kingdom> kingdoms = PoliticalWorldAPI.WorldQuery.GetKingdoms(64);
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null) continue;

                int seen = PoliticalWorldAPI.Data.GetInt(
                    kingdom,
                    AddonId,
                    "world_ready_seen",
                    0
                );

                PoliticalWorldAPI.Data.SetInt(
                    kingdom,
                    AddonId,
                    "world_ready_seen",
                    seen + 1
                );
            }

            NeoModLoader.services.LogService.LogInfo(
                "[PW SDK Example 08] touched " + kingdoms.Count + " kingdom snapshots."
            );
        }
    }
}
