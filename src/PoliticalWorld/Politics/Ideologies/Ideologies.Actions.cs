using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.api;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using NeoModLoader.General.UI.Window;
using NeoModLoader.General.UI.Prefabs;
using strings;
using UnityEngine;
using UnityEngine.UI;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // Step 9D FAST: ideology sandbox actions and reform/radicalization controls moved unchanged.
        private static bool SetMonarchismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                MonarchismIdeologyId
            );
        }

        private static bool SetConservatismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                ConservatismIdeologyId
            );
        }

        private static bool SetLiberalismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                LiberalismIdeologyId
            );
        }

        private static bool SetDemocracyIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                DemocracyIdeologyId
            );
        }

        private static bool SetSocialismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                SocialismIdeologyId
            );
        }

        private static bool SetCommunismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                CommunismIdeologyId
            );
        }

        private static bool SetFascismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                FascismIdeologyId
            );
        }

        private static bool SetAnarchismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                AnarchismIdeologyId
            );
        }

        private static bool SetSyndicalismIdeology(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetIdeologyForKingdom(
                pTile,
                SyndicalismIdeologyId
            );
        }

        private static bool SetCityMonarchismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, MonarchismIdeologyId);
        }

        private static bool SetCityConservatismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, ConservatismIdeologyId);
        }

        private static bool SetCityLiberalismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, LiberalismIdeologyId);
        }

        private static bool SetCityDemocracyIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, DemocracyIdeologyId);
        }

        private static bool SetCitySocialismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, SocialismIdeologyId);
        }

        private static bool SetCityCommunismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, CommunismIdeologyId);
        }

        private static bool SetCityFascismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, FascismIdeologyId);
        }

        private static bool SetCityAnarchismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, AnarchismIdeologyId);
        }

        private static bool SetCitySyndicalismIdeology(WorldTile pTile, string pPowerId)
        {
            return SetIdeologyForCity(pTile, SyndicalismIdeologyId);
        }

        private static bool SetIdeologyForCity(
            WorldTile pTile,
            string ideology,
            string selectedNodeId = null
        )
        {
            if (
                pTile == null ||
                pTile.zone == null ||
                pTile.zone.city == null ||
                !IsValidIdeology(ideology)
            )
            {
                return false;
            }

            IdeologyNode selectedNode = GetIdeologyNode(
                string.IsNullOrEmpty(selectedNodeId)
                    ? ideology
                    : selectedNodeId
            );
            if (
                selectedNode == null ||
                selectedNode.RootIdeologyId != ideology
            )
            {
                selectedNode = GetIdeologyNode(ideology);
            }

            City city = pTile.zone.city;
            Kingdom kingdom = GetKingdomFromObject(city);
            List<Actor> units = GetCityUnitsSafe(city);
            int changed = 0;

            for (int i = 0; i < units.Count; i++)
            {
                Actor actor = units[i];
                if (
                    actor == null ||
                    actor.data == null ||
                    !actor.isAlive()
                )
                {
                    continue;
                }

                SetCitizenIdeology(actor, ideology);
                SetCitizenIdeologyConviction(
                    actor,
                    UnityEngine.Random.Range(65, 86)
                );
                changed++;
            }

            if (changed <= 0)
            {
                EffectsLibrary.spawnAtTile(
                    "fx_bad_place",
                    pTile,
                    0.25f
                );
                return false;
            }

            // Citizens keep root ideology IDs because city support, movements,
            // parties and diffusion are still root-based. Preserve the exact
            // branch selected in dev2 as settlement political identity so the
            // next city-politics stage can consume it without a save migration.
            SetCityStringData(
                city,
                CityIdeologyCurrentDataKey,
                selectedNode != null && selectedNode.Tier > 0
                    ? selectedNode.Id
                    : ""
            );

            // Give the manual sandbox edit a persistent local legacy so the
            // city's normal diffusion model does not immediately forget it.
            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string candidate = IdeologyIds[i];
                SetCityIntData(
                    city,
                    PoliticalMemoryPrefix + GetMovementKeySuffix(candidate),
                    candidate == ideology ? 82 : 8
                );
            }
            SetCityIntData(
                city,
                PoliticalMemoryLastYearDataKey,
                GetWorldYearSafe()
            );

            EffectsLibrary.spawnAtTile(
                "fx_positive_effect",
                pTile,
                0.5f
            );

            string displayName = selectedNode != null && selectedNode.Tier > 0
                ? GetIdeologyCurrentName(selectedNode.Id)
                : GetIdeologyName(ideology);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_city_ideology_edited"),
                    GetWorldObjectDisplayName(city),
                    displayName
                ),
                kingdom,
                city,
                null,
                GetIdeologyIconPath(ideology),
                "city_ideology_edited_" +
                    (selectedNode != null ? selectedNode.Id : ideology),
                10f
            );

            return true;
        }

        private static bool SetIdeologyForKingdom(
            WorldTile pTile,
            string ideology,
            string selectedNodeId = null
        )
        {
            if (
                pTile == null ||
                pTile.zone == null ||
                pTile.zone.city == null ||
                !IsValidIdeology(ideology)
            )
            {
                return false;
            }

            Kingdom kingdom = pTile.zone.city.kingdom;
            if (kingdom == null)
            {
                return false;
            }

            IdeologyNode selectedNode = GetIdeologyNode(
                string.IsNullOrEmpty(selectedNodeId)
                    ? ideology
                    : selectedNodeId
            );
            if (
                selectedNode == null ||
                selectedNode.RootIdeologyId != ideology
            )
            {
                selectedNode = GetIdeologyNode(ideology);
            }

            string previousIdeology = GetStateIdeology(kingdom);
            string previousCurrent = GetStateIdeologyCurrent(kingdom);

            if (!SetStateIdeology(kingdom, ideology))
            {
                EffectsLibrary.spawnAtTile(
                    "fx_bad_place",
                    pTile,
                    0.25f
                );
                return false;
            }

            if (selectedNode != null && selectedNode.Tier > 0)
            {
                SetStateIdeologyCurrent(kingdom, selectedNode.Id);
                ResetIdeologyCurrentCandidate(kingdom);
            }
            else
            {
                ResetStateIdeologyCurrent(kingdom);
            }

            // A direct sandbox edit is intentional. Clear accumulated automatic
            // evolution pressure so the simulation does not instantly undo the
            // player's choice on the next ideology tick.
            SetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                0
            );
            SetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            SetKingdomStringData(
                kingdom,
                IdeologyEvolutionDirectionDataKey,
                ""
            );

            EffectsLibrary.spawnAtTile(
                "fx_positive_effect",
                pTile,
                0.5f
            );

            string currentNow = GetStateIdeologyCurrent(kingdom);
            if (
                previousIdeology != ideology ||
                previousCurrent != currentNow
            )
            {
                string displayName = selectedNode != null && selectedNode.Tier > 0
                    ? GetIdeologyCurrentName(selectedNode.Id)
                    : GetIdeologyName(ideology);

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_state_ideology_changed"),
                        GetWorldObjectDisplayName(kingdom),
                        displayName
                    ),
                    kingdom,
                    pTile.zone.city,
                    null,
                    GetIdeologyIconPath(ideology),
                    "state_ideology_changed_" +
                        (selectedNode != null ? selectedNode.Id : ideology),
                    15f
                );
            }

            Actor ruler = GetLivingRuler(kingdom);

            if (ruler != null)
            {
                ruler.startShake(0.3f, 0.1f, true, true);
                ruler.startColorEffect(
                    ActorColorEffect.White
                );
            }

            return true;
        }

        private static bool EncourageReform(
            WorldTile pTile,
            string pPowerId
        )
        {
            return AdjustIdeologyEvolutionPressures(
                pTile,
                35,
                -10,
                true
            );
        }

        private static bool IncreaseRadicalization(
            WorldTile pTile,
            string pPowerId
        )
        {
            return AdjustIdeologyEvolutionPressures(
                pTile,
                -10,
                35,
                false
            );
        }

        private static bool ReduceRadicalization(
            WorldTile pTile,
            string pPowerId
        )
        {
            return AdjustIdeologyEvolutionPressures(
                pTile,
                10,
                -40,
                true
            );
        }

        private static bool AdjustIdeologyEvolutionPressures(
            WorldTile pTile,
            int reformDelta,
            int radicalizationDelta,
            bool positiveEffect
        )
        {
            if (
                pTile == null ||
                pTile.zone == null ||
                pTile.zone.city == null
            )
            {
                return false;
            }

            Kingdom kingdom = GetKingdomFromObject(pTile.zone.city);
            if (kingdom == null || kingdom.data == null)
            {
                return false;
            }

            int reform = ClampInt(
                GetKingdomIntData(
                    kingdom,
                    IdeologyReformPressureDataKey,
                    0
                ) + reformDelta,
                0,
                100
            );
            int radicalization = ClampInt(
                GetKingdomIntData(
                    kingdom,
                    IdeologyRadicalizationPressureDataKey,
                    0
                ) + radicalizationDelta,
                0,
                100
            );

            SetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                reform
            );
            SetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                radicalization
            );

            // Do not let momentum accumulated for the previous automatic
            // branch candidate fire immediately after a deliberate player
            // intervention. The normal resolver will choose a new candidate
            // from the adjusted pressures on the next ideology tick.
            ResetIdeologyCurrentCandidate(kingdom);

            string direction = "stable";
            if (
                radicalization >= reform + 15 &&
                radicalization >= 45
            )
            {
                direction = "radicalization";
            }
            else if (
                reform >= radicalization + 15 &&
                reform >= 45
            )
            {
                direction = "reform";
            }
            else if (Math.Max(reform, radicalization) >= 50)
            {
                direction = "contested";
            }

            SetKingdomStringData(
                kingdom,
                IdeologyEvolutionDirectionDataKey,
                direction
            );

            EffectsLibrary.spawnAtTile(
                positiveEffect
                    ? "fx_positive_effect"
                    : "fx_bad_place",
                pTile,
                0.5f
            );

            return true;
        }

    }
}
