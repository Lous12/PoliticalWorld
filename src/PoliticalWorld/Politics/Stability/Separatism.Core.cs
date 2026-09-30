using System;
using System.Collections.Generic;
using NeoModLoader.General;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // 1.10.0-dev8 separatism simulation. The displayed separatism number is
        // still derived from existing political state, but a city now remembers
        // whether that pressure has crystallized into an actual movement.
        private const string SeparatistStageDataKey = "ukiol_separatist_stage";
        private const string SeparatistStageOwnerDataKey = "ukiol_separatist_stage_owner";

        private const int SeparatistStageNone = 0;
        private const int SeparatistStageMovement = 1;
        private const int SeparatistStageAutonomy = 2;
        private const int SeparatistStageSecession = 3;

        private const int SeparatistMovementThreshold = 45;
        private const int SeparatistAutonomyThreshold = 65;
        private const int SeparatistSecessionThreshold = 80;
        private const int SeparatistSecessionLoyaltyCeiling = 35;

        private static int GetCitySeparatistStage(City city)
        {
            if (city == null) return SeparatistStageNone;
            return ClampInt(
                GetCityIntData(city, SeparatistStageDataKey, SeparatistStageNone),
                SeparatistStageNone,
                SeparatistStageSecession
            );
        }

        private static string GetCitySeparatistStageId(City city)
        {
            int stage = GetCitySeparatistStage(city);
            if (stage >= SeparatistStageSecession) return "secession_crisis";
            if (stage >= SeparatistStageAutonomy) return "autonomy_campaign";
            if (stage >= SeparatistStageMovement) return "separatist_movement";
            return "none";
        }

        private static int DetermineNextSeparatistStage(
            int currentStage,
            int sentiment,
            int loyalty
        )
        {
            // Hysteresis is deliberate. Without it, a city hovering around a
            // threshold would spam movement-start/movement-end events every
            // stability tick.
            switch (currentStage)
            {
                case SeparatistStageNone:
                    return sentiment >= SeparatistMovementThreshold
                        ? SeparatistStageMovement
                        : SeparatistStageNone;

                case SeparatistStageMovement:
                    if (sentiment >= SeparatistAutonomyThreshold)
                    {
                        return SeparatistStageAutonomy;
                    }
                    return sentiment <= 25
                        ? SeparatistStageNone
                        : SeparatistStageMovement;

                case SeparatistStageAutonomy:
                    if (
                        sentiment >= SeparatistSecessionThreshold &&
                        loyalty <= SeparatistSecessionLoyaltyCeiling
                    )
                    {
                        return SeparatistStageSecession;
                    }
                    return sentiment < 50
                        ? SeparatistStageMovement
                        : SeparatistStageAutonomy;

                case SeparatistStageSecession:
                    if (sentiment < 65 || loyalty > 50)
                    {
                        return SeparatistStageAutonomy;
                    }
                    return SeparatistStageSecession;
            }

            return SeparatistStageNone;
        }

        private static void ProcessSeparatistMovements()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int kingdomIndex = 0; kingdomIndex < kingdoms.Count; kingdomIndex++)
            {
                Kingdom kingdom = kingdoms[kingdomIndex];
                if (kingdom == null || kingdom.data == null) continue;

                List<City> cities = GetCitiesSafe(kingdom);
                if (cities.Count <= 1) continue;

                City capital = GetMemberValue(kingdom, "capital", "_capital") as City;
                string ownerIdentity = GetStableObjectIdentity(kingdom);

                for (int cityIndex = 0; cityIndex < cities.Count; cityIndex++)
                {
                    City city = cities[cityIndex];
                    if (city == null || city.data == null || city == capital) continue;
                    if (GetKingdomFromObject(city) != kingdom) continue;

                    // Ownership changes dissolve the old state's movement. The
                    // new state gets to generate its own separatist history.
                    string stageOwner = GetCityStringData(
                        city,
                        SeparatistStageOwnerDataKey,
                        ""
                    );
                    if (
                        !string.IsNullOrEmpty(stageOwner) &&
                        !string.IsNullOrEmpty(ownerIdentity) &&
                        !string.Equals(stageOwner, ownerIdentity, StringComparison.Ordinal)
                    )
                    {
                        SetCityIntData(city, SeparatistStageDataKey, SeparatistStageNone);
                    }
                    if (!string.IsNullOrEmpty(ownerIdentity))
                    {
                        SetCityStringData(city, SeparatistStageOwnerDataKey, ownerIdentity);
                    }

                    int sentiment = ScenarioBridge.GetSettlementSeparatistSentiment(city);
                    int loyalty = ScenarioBridge.GetSettlementGovernmentLoyalty(city);
                    int currentStage = GetCitySeparatistStage(city);
                    int nextStage = DetermineNextSeparatistStage(
                        currentStage,
                        sentiment,
                        loyalty
                    );

                    if (nextStage == currentStage) continue;

                    SetCityIntData(city, SeparatistStageDataKey, nextStage);

                    if (nextStage > currentStage)
                    {
                        ApplySeparatistEscalationEffects(
                            city,
                            kingdom,
                            nextStage
                        );
                        PublishSeparatistStageEvent(
                            city,
                            kingdom,
                            currentStage,
                            nextStage,
                            sentiment,
                            loyalty
                        );
                    }
                    else if (nextStage == SeparatistStageNone)
                    {
                        PublishSeparatistResolutionEvent(
                            city,
                            kingdom,
                            currentStage,
                            sentiment,
                            loyalty
                        );
                    }
                }
            }
        }

        private static void ApplySeparatistEscalationEffects(
            City city,
            Kingdom kingdom,
            int stage
        )
        {
            if (city == null || kingdom == null) return;

            if (stage == SeparatistStageAutonomy)
            {
                SetLocalStability(
                    city,
                    ClampInt(GetLocalStability(city) - 3, 0, 100)
                );
            }
            else if (stage >= SeparatistStageSecession)
            {
                SetLocalStability(
                    city,
                    ClampInt(GetLocalStability(city) - 7, 0, 100)
                );
                SetNationalStability(
                    kingdom,
                    ClampInt(GetNationalStability(kingdom) - 2, 0, 100)
                );
            }
        }

        private static void PublishSeparatistStageEvent(
            City city,
            Kingdom kingdom,
            int oldStage,
            int newStage,
            int sentiment,
            int loyalty
        )
        {
            string eventId;
            string textKey;
            string eventKey;

            if (newStage >= SeparatistStageSecession)
            {
                eventId = PoliticalWorldAPI.Events.SettlementSecessionCrisisStarted;
                textKey = "ukiol_event_separatist_secession_crisis";
                eventKey = "separatist_secession_" + GetStableObjectIdentity(city);
            }
            else if (newStage >= SeparatistStageAutonomy)
            {
                eventId = PoliticalWorldAPI.Events.SettlementAutonomyDemanded;
                textKey = "ukiol_event_separatist_autonomy";
                eventKey = "separatist_autonomy_" + GetStableObjectIdentity(city);
            }
            else
            {
                eventId = PoliticalWorldAPI.Events.SettlementSeparatistMovementStarted;
                textKey = "ukiol_event_separatist_movement";
                eventKey = "separatist_movement_" + GetStableObjectIdentity(city);
            }

            string text = string.Format(
                LM.Get(textKey),
                GetWorldObjectDisplayName(city),
                GetWorldObjectDisplayName(kingdom),
                sentiment,
                loyalty
            );

            Dictionary<string, string> payload = new Dictionary<string, string>()
            {
                { "separatism", sentiment.ToString() },
                { "loyalty", loyalty.ToString() },
                { "stage", GetCitySeparatistStageId(city) }
            };

            PoliticalWorldAPI.InternalEmitCoreEvent(
                eventId: eventId,
                kingdom: kingdom,
                oldNumber: oldStage,
                newNumber: newStage,
                text: text,
                eventKey: eventKey,
                category: "settlement-separatism",
                year: GetWorldYearSafe(),
                payload: payload,
                city: city
            );

            PublishPoliticalEvent(
                text,
                kingdom,
                city,
                null,
                SocietyIconPath,
                eventKey,
                newStage >= SeparatistStageSecession ? 60f : 35f
            );
        }

        private static void PublishSeparatistResolutionEvent(
            City city,
            Kingdom kingdom,
            int oldStage,
            int sentiment,
            int loyalty
        )
        {
            string eventKey =
                "separatist_resolved_" + GetStableObjectIdentity(city);
            string text = string.Format(
                LM.Get("ukiol_event_separatist_resolved"),
                GetWorldObjectDisplayName(city),
                GetWorldObjectDisplayName(kingdom)
            );

            Dictionary<string, string> payload = new Dictionary<string, string>()
            {
                { "separatism", sentiment.ToString() },
                { "loyalty", loyalty.ToString() },
                { "stage", "none" }
            };

            PoliticalWorldAPI.InternalEmitCoreEvent(
                eventId: PoliticalWorldAPI.Events.SettlementSeparatismEnded,
                kingdom: kingdom,
                oldNumber: oldStage,
                newNumber: SeparatistStageNone,
                text: text,
                eventKey: eventKey,
                category: "settlement-separatism",
                year: GetWorldYearSafe(),
                payload: payload,
                city: city
            );

            PublishPoliticalEvent(
                text,
                kingdom,
                city,
                null,
                SocietyIconPath,
                eventKey,
                20f
            );
        }

        private static int GetSeparatistLocalStabilityPenalty(City city)
        {
            int stage = GetCitySeparatistStage(city);
            if (stage >= SeparatistStageSecession) return 10;
            if (stage >= SeparatistStageAutonomy) return 5;
            if (stage >= SeparatistStageMovement) return 2;
            return 0;
        }

        private static int GetSeparatistRebellionThreshold(City city)
        {
            int stage = GetCitySeparatistStage(city);
            if (stage >= SeparatistStageSecession) return 35;
            if (stage >= SeparatistStageAutonomy) return 25;
            return RebellionThreshold;
        }

        private static float ApplySeparatistRebellionPressure(
            City city,
            float chance
        )
        {
            if (city == null) return chance;

            int stage = GetCitySeparatistStage(city);
            int sentiment = ScenarioBridge.GetSettlementSeparatistSentiment(city);
            int loyalty = ScenarioBridge.GetSettlementGovernmentLoyalty(city);

            if (stage >= SeparatistStageSecession)
            {
                chance += 0.28f;
            }
            else if (stage >= SeparatistStageAutonomy)
            {
                chance += 0.12f;
            }
            else if (stage >= SeparatistStageMovement)
            {
                chance += 0.04f;
            }

            if (sentiment > 60)
            {
                chance += Math.Min(0.12f, (sentiment - 60) * 0.003f);
            }
            if (loyalty < 25)
            {
                chance += 0.06f;
            }

            return Math.Max(0.01f, Math.Min(1f, chance));
        }

        private static bool CanCityJoinSeparatistRebellion(City city)
        {
            if (city == null) return false;

            int local = GetLocalStability(city);
            int stage = GetCitySeparatistStage(city);

            if (local <= RebellionJoinThreshold) return true;
            if (stage >= SeparatistStageSecession && local <= 35) return true;
            if (stage >= SeparatistStageAutonomy && local <= 28) return true;
            return false;
        }
    }
}
