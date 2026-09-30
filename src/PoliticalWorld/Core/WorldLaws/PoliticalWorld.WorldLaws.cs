using System;
using NeoModLoader.api;

namespace Lous12.PoliticalWorld
{
    public partial class Main : BasicMod<Main>
    {
        private const string PoliticalWorldLawGroupId =
            "ukiol_political_world";
        private const string PoliticalRebellionsWorldLawId =
            "ukiol_world_law_political_rebellions";

        private static WorldLawAsset _politicalRebellionsWorldLaw;

        private static void CreatePoliticalWorldLaws()
        {
            try
            {
                if (AssetManager.world_law_groups.get(PoliticalWorldLawGroupId) == null)
                {
                    AssetManager.world_law_groups.add(
                        new WorldLawGroupAsset
                        {
                            id = PoliticalWorldLawGroupId,
                            name = "ukiol_world_laws_tab_political_world",
                            color = "#C9A7FF"
                        }
                    );
                }

                _politicalRebellionsWorldLaw =
                    AssetManager.world_laws_library.get(
                        PoliticalRebellionsWorldLawId
                    );

                if (_politicalRebellionsWorldLaw == null)
                {
                    _politicalRebellionsWorldLaw =
                        AssetManager.world_laws_library.add(
                            new WorldLawAsset
                            {
                                id = PoliticalRebellionsWorldLawId,
                                group_id = PoliticalWorldLawGroupId,
                                icon_path = PoliticsIconPath,
                                default_state = true
                            }
                        );
                }
            }
            catch (Exception exception)
            {
                // The law is a sandbox/QoL control, so failure to register it
                // must never prevent the core political simulation from loading.
                _politicalRebellionsWorldLaw = null;
                LogWarning(
                    "Could not register Political World world laws: " +
                    exception.Message
                );
            }
        }

        private static bool ArePoliticalRebellionsEnabled()
        {
            // Fail open: older/unsupported WorldBox builds, early bootstrap and
            // saves without the custom option retain Political World's previous
            // behaviour instead of silently disabling rebellions.
            if (World.world == null || World.world.world_laws == null)
            {
                return true;
            }

            try
            {
                PlayerOptionData option;
                if (
                    World.world.world_laws.dict != null &&
                    World.world.world_laws.dict.TryGetValue(
                        PoliticalRebellionsWorldLawId,
                        out option
                    ) &&
                    option != null
                )
                {
                    return option.boolVal;
                }

                return _politicalRebellionsWorldLaw == null ||
                    _politicalRebellionsWorldLaw.default_state;
            }
            catch
            {
                return true;
            }
        }
    }
}
