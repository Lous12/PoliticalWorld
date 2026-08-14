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
        private static int CalculateMovementRadicalismTarget(
            Kingdom kingdom,
            string ideology,
            int support
        )
        {
            if (kingdom == null || !IsValidIdeology(ideology))
            {
                return 0;
            }

            int nationalStability = GetNationalStability(kingdom);
            string stateIdeology = GetStateIdeology(kingdom);
            int stateSupport = IsValidIdeology(stateIdeology)
                ? GetKingdomIdeologySupport(kingdom, stateIdeology)
                : 0;

            float value = 8f + support * 0.55f;

            if (nationalStability < 50)
            {
                value += (50 - nationalStability) * 0.75f;
            }
            else if (nationalStability > 70)
            {
                value -= (nationalStability - 70) * 0.30f;
            }

            if (stateSupport < 40)
            {
                value += (40 - stateSupport) * 0.35f;
            }

            if (IsValidIdeology(stateIdeology))
            {
                int hostility = GetIdeologyHostility(
                    ideology,
                    stateIdeology
                );
                value += hostility * 0.20f;
            }

            // dev12: when a whole society is polarizing, opposition movements
            // radicalize faster. A strong reform wave instead gives legal and
            // moderate strategies more room to work.
            int stateRadicalization = GetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            int stateReform = GetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                0
            );
            value += stateRadicalization * 0.12f;
            value -= stateReform * 0.07f;

            if (
                ideology == CommunismIdeologyId ||
                ideology == FascismIdeologyId ||
                ideology == AnarchismIdeologyId ||
                ideology == SyndicalismIdeologyId
            )
            {
                value += 8f;
            }
            else if (ideology == SocialismIdeologyId)
            {
                value += 4f;
            }

            return ClampInt((int)Math.Round(value), 0, 100);
        }

        private static void GetLeadingPoliticalMovement(
            Kingdom kingdom,
            out string ideology,
            out int support,
            out int radicalism,
            out string leaderName
        )
        {
            ideology = null;
            support = 0;
            radicalism = 0;
            leaderName = "";

            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string candidate = IdeologyIds[i];
                string suffix = GetMovementKeySuffix(candidate);

                if (
                    GetKingdomIntData(
                        kingdom,
                        MovementActivePrefix + suffix,
                        0
                    ) == 0
                )
                {
                    continue;
                }

                int candidateSupport = GetKingdomIdeologySupport(
                    kingdom,
                    candidate
                );

                if (candidateSupport <= support)
                {
                    continue;
                }

                ideology = candidate;
                support = candidateSupport;
                radicalism = GetKingdomIntData(
                    kingdom,
                    MovementRadicalismPrefix + suffix,
                    0
                );
                leaderName = GetKingdomStringData(
                    kingdom,
                    MovementLeaderNamePrefix + suffix,
                    ""
                );
            }
        }

        private static string FormatPoliticalMovement(
            string ideology,
            int support,
            string leaderName
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_movement_none");
            }

            return GetIdeologyName(ideology) + " — " + support + "%";
        }

        private static string FormatMovementRadicalism(int radicalism)
        {
            if (radicalism <= 0)
            {
                return LM.Get("ukiol_movement_none");
            }

            return radicalism + "% — " + GetMovementRadicalismName(radicalism);
        }

        private static string GetMovementRadicalismName(int radicalism)
        {
            if (radicalism >= 80)
            {
                return LM.Get("ukiol_movement_radicalism_extreme");
            }
            if (radicalism >= 65)
            {
                return LM.Get("ukiol_movement_radicalism_radical");
            }
            if (radicalism >= 45)
            {
                return LM.Get("ukiol_movement_radicalism_tense");
            }
            if (radicalism >= 25)
            {
                return LM.Get("ukiol_movement_radicalism_assertive");
            }
            return LM.Get("ukiol_movement_radicalism_peaceful");
        }

        private static string GetMovementColor(int radicalism)
        {
            if (radicalism >= 80) return "#FF3A3A";
            if (radicalism >= 65) return "#FF703A";
            if (radicalism >= 45) return "#FFC14D";
            if (radicalism >= 25) return "#E8D36A";
            return "#72E58A";
        }

        private static string GetMovementKeySuffix(string ideology)
        {
            if (string.IsNullOrEmpty(ideology))
            {
                return "none";
            }

            const string prefix = "ukiol_ideology_";
            return ideology.StartsWith(prefix)
                ? ideology.Substring(prefix.Length)
                : ideology;
        }

    }
}
