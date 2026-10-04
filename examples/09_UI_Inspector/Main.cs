using System.Collections.Generic;
using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example09
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example09";

        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 12))
            {
                LogError("Political World API 1.12+ is required by this example.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 09",
                Version = "1.0.0",
                Author = "Lous12",
                Description = "Public inspector UI integration example",
                RequiredApiMajor = 1,
                RequiredApiMinor = 12
            })) return;

            PoliticalWorldAPI.UI.RegisterInspectorSection(
                AddonId,
                new PoliticalWorldAPI.InspectorSectionDefinition
                {
                    Id = AddonId + ".kingdom_debug",
                    TargetKind = PoliticalWorldAPI.InspectorTargetKind.Kingdom,
                    DisplayName = "SDK Example",
                    Description = "Read-only fields provided by an addon.",
                    SortOrder = 500,
                    Fields = new List<PoliticalWorldAPI.InspectorFieldDefinition>
                    {
                        new PoliticalWorldAPI.InspectorFieldDefinition
                        {
                            Id = AddonId + ".ideology",
                            DisplayName = "Ideology",
                            SortOrder = 10,
                            ValueProvider = context =>
                            {
                                PoliticalWorldAPI.KingdomState state =
                                    PoliticalWorldAPI.GetKingdomState(context.Kingdom);
                                return state == null ? "?" : state.IdeologyName;
                            }
                        },
                        new PoliticalWorldAPI.InspectorFieldDefinition
                        {
                            Id = AddonId + ".stability",
                            DisplayName = "Stability",
                            SortOrder = 20,
                            ValueProvider = context =>
                            {
                                PoliticalWorldAPI.KingdomState state =
                                    PoliticalWorldAPI.GetKingdomState(context.Kingdom);
                                return state == null ? "?" : state.Stability + "%";
                            }
                        }
                    }
                }
            );

            // PW owns the host UI. No Harmony patch or cloned internal window
            // is needed just to expose a couple of addon fields.
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
