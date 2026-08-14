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
        private static void UpdatePoliticalCrises()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int k = 0; k < kingdoms.Count; k++)
            {
                Kingdom kingdom = kingdoms[k];

                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                int cooldown = GetKingdomIntData(
                    kingdom,
                    CrisisCooldownDataKey,
                    0
                );

                if (cooldown > 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        CrisisCooldownDataKey,
                        cooldown - 1
                    );
                }

                if (
                    GetKingdomIntData(
                        kingdom,
                        CrisisInitializedDataKey,
                        0
                    ) == 0
                )
                {
                    SetKingdomIntData(
                        kingdom,
                        CrisisInitializedDataKey,
                        1
                    );
                    continue;
                }

                if (!IsPoliticalCrisisActive(kingdom))
                {
                    TryStartPoliticalCrisis(kingdom);
                    continue;
                }

                UpdateActivePoliticalCrisis(kingdom);
            }
        }

        private static void TryStartPoliticalCrisis(Kingdom kingdom)
        {
            if (
                kingdom == null ||
                GetKingdomIntData(kingdom, CrisisCooldownDataKey, 0) > 0
            )
            {
                return;
            }

            string ideology;
            int support;
            int radicalism;
            string leaderName;

            GetLeadingPoliticalMovement(
                kingdom,
                out ideology,
                out support,
                out radicalism,
                out leaderName
            );

            int stability = GetNationalStability(kingdom);

            if (
                !IsValidIdeology(ideology) ||
                support < CrisisFormationSupportThreshold ||
                radicalism < CrisisFormationRadicalismThreshold ||
                stability > CrisisFormationStabilityCeiling
            )
            {
                return;
            }

            string demand = ChooseCrisisDemand(
                kingdom,
                ideology,
                support,
                radicalism
            );

            int pressure = CalculateCrisisPressureTarget(
                kingdom,
                ideology,
                support,
                radicalism
            );

            SetKingdomIntData(kingdom, CrisisActiveDataKey, 1);
            SetKingdomStringData(kingdom, CrisisIdeologyDataKey, ideology);
            SetKingdomStringData(kingdom, CrisisDemandDataKey, demand);
            SetKingdomIntData(kingdom, CrisisPressureDataKey, pressure);
            SetKingdomIntData(kingdom, CrisisStageDataKey, 1);
            SetKingdomIntData(kingdom, CrisisAgeDataKey, 0);
            SetKingdomIntData(kingdom, CrisisResponseMadeDataKey, 0);

            PoliticalWorldAPI.InternalEmitCoreEvent(
                eventId: PoliticalWorldAPI.Events.PoliticalCrisisStarted,
                kingdom: kingdom,
                oldValue: ideology,
                newValue: demand,
                newNumber: pressure,
                eventKey: "crisis_started_" + GetMovementKeySuffix(ideology),
                category: "political-crisis",
                year: GetWorldYearSafe()
            );

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_crisis_started"),
                    GetWorldObjectDisplayName(kingdom),
                    GetIdeologyName(ideology),
                    GetCrisisDemandName(demand)
                ),
                kingdom,
                null,
                null,
                GetIdeologyIconPath(ideology),
                "crisis_started_" + GetMovementKeySuffix(ideology),
                40f
            );
        }

        private static void UpdateActivePoliticalCrisis(Kingdom kingdom)
        {
            string ideology = GetPoliticalCrisisIdeology(kingdom);
            string demand = GetKingdomStringData(
                kingdom,
                CrisisDemandDataKey,
                ""
            );

            if (!IsValidIdeology(ideology))
            {
                EndPoliticalCrisis(kingdom, false);
                return;
            }

            string suffix = GetMovementKeySuffix(ideology);
            int movementActive = GetKingdomIntData(
                kingdom,
                MovementActivePrefix + suffix,
                0
            );
            int support = GetKingdomIdeologySupport(kingdom, ideology);
            int radicalism = GetKingdomIntData(
                kingdom,
                MovementRadicalismPrefix + suffix,
                0
            );

            if (
                movementActive == 0 ||
                support <= MovementDissolutionThreshold ||
                radicalism < 35 ||
                IsCrisisDemandAlreadySatisfied(kingdom, ideology, demand)
            )
            {
                EndPoliticalCrisis(kingdom, true);
                return;
            }

            int age = GetKingdomIntData(
                kingdom,
                CrisisAgeDataKey,
                0
            ) + 1;
            SetKingdomIntData(kingdom, CrisisAgeDataKey, age);

            int currentPressure = GetPoliticalCrisisPressure(kingdom);
            int targetPressure = CalculateCrisisPressureTarget(
                kingdom,
                ideology,
                support,
                radicalism
            );
            int nextPressure = MoveTowardsInt(
                currentPressure,
                targetPressure,
                7
            );
            SetKingdomIntData(
                kingdom,
                CrisisPressureDataKey,
                nextPressure
            );

            int responseMade = GetKingdomIntData(
                kingdom,
                CrisisResponseMadeDataKey,
                0
            );

            if (responseMade == 0 && age >= 2)
            {
                if (
                    UnityEngine.Random.value <
                    CalculateCrisisAcceptanceChance(
                        kingdom,
                        ideology,
                        demand,
                        support,
                        radicalism,
                        nextPressure
                    )
                )
                {
                    AcceptPoliticalCrisisDemand(
                        kingdom,
                        ideology,
                        demand,
                        suffix
                    );
                    return;
                }

                SetKingdomIntData(
                    kingdom,
                    CrisisResponseMadeDataKey,
                    1
                );
                SetKingdomIntData(kingdom, CrisisStageDataKey, 2);
                SetKingdomIntData(
                    kingdom,
                    CrisisPressureDataKey,
                    ClampInt(nextPressure + 12, 0, 100)
                );
                SetNationalStability(
                    kingdom,
                    GetNationalStability(kingdom) - 5
                );
                SetKingdomIntData(
                    kingdom,
                    MovementRadicalismPrefix + suffix,
                    ClampInt(radicalism + 8, 0, 100)
                );

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_crisis_rejected"),
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyName(ideology)
                    ),
                    kingdom,
                    null,
                    null,
                    GetIdeologyIconPath(ideology),
                    "crisis_rejected_" + suffix,
                    45f
                );
                return;
            }

            int stage = GetKingdomIntData(
                kingdom,
                CrisisStageDataKey,
                1
            );

            if (
                responseMade != 0 &&
                stage < 3 &&
                nextPressure >= CrisisSeverePressureThreshold
            )
            {
                SetKingdomIntData(kingdom, CrisisStageDataKey, 3);
                SetNationalStability(
                    kingdom,
                    GetNationalStability(kingdom) - 4
                );

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_crisis_escalated"),
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyName(ideology)
                    ),
                    kingdom,
                    null,
                    null,
                    GetIdeologyIconPath(ideology),
                    "crisis_escalated_" + suffix,
                    60f
                );
            }

            if (
                responseMade != 0 &&
                GetKingdomIntData(kingdom, CrisisStageDataKey, 1) >= 3 &&
                age >= CrisisRegimeChangeMinAge &&
                GetPoliticalCrisisPressure(kingdom) >= CrisisRegimeChangePressureThreshold
            )
            {
                if (TryResolveCriticalPoliticalCrisis(
                    kingdom,
                    ideology,
                    support,
                    GetKingdomIntData(
                        kingdom,
                        MovementRadicalismPrefix + suffix,
                        radicalism
                    ),
                    GetPoliticalCrisisPressure(kingdom),
                    suffix
                ))
                {
                    return;
                }
            }

            // Если движение постепенно теряет почву под ногами, кризис
            // может закончиться без переворота или революции.
            if (
                responseMade != 0 &&
                age >= 8 &&
                nextPressure <= 35
            )
            {
                EndPoliticalCrisis(kingdom, true);
            }
        }

        private static bool TryResolveCriticalPoliticalCrisis(
            Kingdom kingdom,
            string ideology,
            int support,
            int radicalism,
            int pressure,
            string suffix
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology)
            )
            {
                return false;
            }

            string stateIdeology = GetStateIdeology(kingdom);
            int hostility = IsValidIdeology(stateIdeology)
                ? GetIdeologyHostility(ideology, stateIdeology)
                : 50;
            int stability = GetNationalStability(kingdom);

            bool peacefulTransition =
                support >= 45 &&
                radicalism < 78 &&
                hostility <= 55 &&
                stability >= 30;

            bool coup = !peacefulTransition && (
                ideology == FascismIdeologyId ||
                ideology == MonarchismIdeologyId ||
                ideology == ConservatismIdeologyId ||
                GetPoliticalTrait(GetLivingRuler(kingdom)) == MilitaristTraitId
            ) &&
                radicalism >= 68 &&
                support >= 28;

            string outcome;
            if (peacefulTransition)
            {
                outcome = "peaceful";
            }
            else if (coup)
            {
                outcome = "coup";
            }
            else
            {
                outcome = "revolution";
            }

            ApplyMovementRegimeChange(
                kingdom,
                ideology,
                outcome,
                support,
                radicalism,
                pressure,
                suffix
            );

            return true;
        }

        private static void ApplyMovementRegimeChange(
            Kingdom kingdom,
            string ideology,
            string outcome,
            int support,
            int radicalism,
            int pressure,
            string suffix
        )
        {
            if (kingdom == null || !IsValidIdeology(ideology))
            {
                return;
            }

            string kingdomName = GetWorldObjectDisplayName(kingdom);
            string ideologyName = GetIdeologyName(ideology);
            Actor movementLeader = FindMovementLeaderActor(kingdom, ideology);

            SetStateIdeology(kingdom, ideology);
            SetKingdomCourse(
                kingdom,
                GetPreferredCourseForIdeology(ideology, outcome)
            );

            bool rulerChanged = false;
            if (outcome == "coup" || outcome == "revolution")
            {
                rulerChanged = TryInstallMovementRuler(
                    kingdom,
                    movementLeader
                );
            }

            Actor ruler = GetLivingRuler(kingdom);
            if (ruler != null)
            {
                AssignPoliticalTrait(
                    ruler,
                    GetPreferredRulerTraitForIdeology(ideology, outcome)
                );
            }

            if (outcome == "peaceful")
            {
                SetNationalStability(
                    kingdom,
                    GetNationalStability(kingdom) + 10
                );
                AdjustCitiesAfterRegimeChange(kingdom, ideology, 5, -2);
            }
            else if (outcome == "coup")
            {
                SetNationalStability(
                    kingdom,
                    GetNationalStability(kingdom) - 8
                );
                AdjustCitiesAfterRegimeChange(kingdom, ideology, 1, -7);
            }
            else
            {
                SetNationalStability(
                    kingdom,
                    Math.Max(18, GetNationalStability(kingdom) - 15)
                );
                AdjustCitiesAfterRegimeChange(kingdom, ideology, 4, -11);
            }

            SetKingdomIntData(
                kingdom,
                MovementRadicalismPrefix + suffix,
                ClampInt(radicalism - (outcome == "peaceful" ? 35 : 20), 0, 100)
            );
            SetKingdomIntData(kingdom, MovementActivePrefix + suffix, 0);
            SetKingdomIntData(kingdom, MovementRadicalizedPrefix + suffix, 0);

            string eventText;
            string eventKey;
            if (outcome == "peaceful")
            {
                eventText = string.Format(
                    LM.Get("ukiol_event_peaceful_transition"),
                    kingdomName,
                    ideologyName
                );
                eventKey = "peaceful_transition_" + suffix;
            }
            else if (outcome == "coup")
            {
                eventText = string.Format(
                    LM.Get("ukiol_event_coup"),
                    kingdomName,
                    ideologyName,
                    rulerChanged
                        ? GetWorldObjectDisplayName(GetLivingRuler(kingdom))
                        : LM.Get("ukiol_event_regime_changed")
                );
                eventKey = "coup_" + suffix;
            }
            else
            {
                eventText = string.Format(
                    LM.Get("ukiol_event_revolution"),
                    kingdomName,
                    ideologyName,
                    rulerChanged
                        ? GetWorldObjectDisplayName(GetLivingRuler(kingdom))
                        : LM.Get("ukiol_event_regime_changed")
                );
                eventKey = "revolution_" + suffix;
            }

            PublishPoliticalEvent(
                eventText,
                kingdom,
                null,
                movementLeader,
                GetIdeologyIconPath(ideology),
                eventKey,
                90f
            );

            ClearPoliticalCrisisData(kingdom);
            SetKingdomIntData(
                kingdom,
                CrisisCooldownDataKey,
                CrisisCooldownTicks + 4
            );
        }

        private static void AdjustCitiesAfterRegimeChange(
            Kingdom kingdom,
            string ideology,
            int supporterDelta,
            int oppositionDelta
        )
        {
            List<City> cities = GetCitiesSafe(kingdom);

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (city == null)
                {
                    continue;
                }

                int support = GetCityIdeologySupport(city, ideology);
                int delta = support >= 35
                    ? supporterDelta
                    : oppositionDelta;

                SetLocalStability(
                    city,
                    GetLocalStability(city) + delta
                );
            }
        }

        private static string GetPreferredCourseForIdeology(
            string ideology,
            string outcome
        )
        {
            if (
                ideology == FascismIdeologyId ||
                ideology == MonarchismIdeologyId
            )
            {
                return MilitaristTraitId;
            }

            if (
                ideology == LiberalismIdeologyId ||
                ideology == DemocracyIdeologyId ||
                ideology == SocialismIdeologyId ||
                ideology == CommunismIdeologyId ||
                ideology == AnarchismIdeologyId ||
                ideology == SyndicalismIdeologyId
            )
            {
                return ReformerTraitId;
            }

            if (outcome == "coup")
            {
                return MilitaristTraitId;
            }

            return DiplomatTraitId;
        }

        private static string GetPreferredRulerTraitForIdeology(
            string ideology,
            string outcome
        )
        {
            if (
                outcome == "coup" ||
                ideology == FascismIdeologyId ||
                ideology == MonarchismIdeologyId
            )
            {
                return MilitaristTraitId;
            }

            if (
                ideology == LiberalismIdeologyId ||
                ideology == DemocracyIdeologyId ||
                ideology == SocialismIdeologyId
            )
            {
                return ReformerTraitId;
            }

            return DiplomatTraitId;
        }

        private static Actor FindMovementLeaderActor(
            Kingdom kingdom,
            string ideology
        )
        {
            if (kingdom == null || !IsValidIdeology(ideology))
            {
                return null;
            }

            Actor best = null;
            float bestScore = -1f;
            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (int c = 0; c < cities.Count && inspected < 240; c++)
            {
                List<Actor> units = GetCityUnitsSafe(cities[c]);

                for (int i = 0; i < units.Count && inspected < 240; i++)
                {
                    Actor actor = units[i];
                    inspected++;

                    if (
                        actor == null ||
                        !actor.isAlive() ||
                        GetCitizenIdeology(actor) != ideology
                    )
                    {
                        continue;
                    }

                    float score = GetCitizenIdeologyConviction(actor);
                    try
                    {
                        if (actor.stats != null)
                        {
                            score += actor.stats[S.stewardship] * 0.6f;
                            score += actor.stats[S.diplomacy] * 0.5f;
                            score += actor.stats[S.warfare] * 0.5f;
                        }
                    }
                    catch
                    {
                    }

                    if (score > bestScore)
                    {
                        best = actor;
                        bestScore = score;
                    }
                }
            }

            return best;
        }

        private static bool TryInstallMovementRuler(
            Kingdom kingdom,
            Actor candidate
        )
        {
            if (
                kingdom == null ||
                candidate == null ||
                !candidate.isAlive()
            )
            {
                return false;
            }

            if (GetLivingRuler(kingdom) == candidate)
            {
                return true;
            }

            string[] methodNames =
            {
                "setKing",
                "setNewKing",
                "setKingActor",
                "changeKing",
                "makeKing"
            };

            for (int nameIndex = 0; nameIndex < methodNames.Length; nameIndex++)
            {
                try
                {
                    MethodInfo[] methods = kingdom.GetType().GetMethods(MemberFlags);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.Name != methodNames[nameIndex])
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length < 1 || parameters.Length > 2)
                        {
                            continue;
                        }

                        if (!parameters[0].ParameterType.IsAssignableFrom(typeof(Actor)))
                        {
                            continue;
                        }

                        object[] args = parameters.Length == 1
                            ? new object[] { candidate }
                            : new object[]
                            {
                                candidate,
                                parameters[1].ParameterType == typeof(bool)
                                    ? (object)false
                                    : GetDefaultValue(parameters[1].ParameterType)
                            };

                        method.Invoke(kingdom, args);

                        if (GetLivingRuler(kingdom) == candidate)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == null || !type.IsValueType)
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private static string ChooseCrisisDemand(
            Kingdom kingdom,
            string ideology,
            int support,
            int radicalism
        )
        {
            string course = GetKingdomCourse(kingdom);

            if (
                (ideology == LiberalismIdeologyId ||
                 ideology == DemocracyIdeologyId) &&
                course != ReformerTraitId
            )
            {
                return CrisisDemandReformsId;
            }

            if (
                ideology == SocialismIdeologyId &&
                radicalism < 72 &&
                course != ReformerTraitId
            )
            {
                return CrisisDemandReformsId;
            }

            if (
                ideology == FascismIdeologyId &&
                (support < 34 || radicalism < 75) &&
                course != MilitaristTraitId
            )
            {
                return CrisisDemandMilitarizationId;
            }

            return CrisisDemandIdeologyId;
        }

        private static int CalculateCrisisPressureTarget(
            Kingdom kingdom,
            string ideology,
            int support,
            int radicalism
        )
        {
            if (kingdom == null || !IsValidIdeology(ideology))
            {
                return 0;
            }

            int stability = GetNationalStability(kingdom);
            float value =
                12f +
                Math.Max(0, support - 15) * 1.15f +
                Math.Max(0, radicalism - 40) * 0.75f +
                Math.Max(0, 55 - stability) * 0.80f;

            string stateIdeology = GetStateIdeology(kingdom);
            if (IsValidIdeology(stateIdeology))
            {
                value += GetIdeologyHostility(
                    ideology,
                    stateIdeology
                ) * 0.18f;
            }

            return ClampInt((int)Math.Round(value), 0, 100);
        }

        private static float CalculateCrisisAcceptanceChance(
            Kingdom kingdom,
            string ideology,
            string demand,
            int support,
            int radicalism,
            int pressure
        )
        {
            float chance = 0.22f;
            int stability = GetNationalStability(kingdom);

            if (support >= 35) chance += 0.14f;
            if (support >= 45) chance += 0.10f;
            if (pressure >= 55) chance += 0.12f;
            if (stability < 35) chance += 0.15f;
            if (stability > 70) chance -= 0.10f;

            if (demand == CrisisDemandIdeologyId)
            {
                chance -= 0.14f;
            }

            string stateIdeology = GetStateIdeology(kingdom);
            if (IsValidIdeology(stateIdeology))
            {
                int hostility = GetIdeologyHostility(
                    ideology,
                    stateIdeology
                );
                chance += (50f - hostility) / 100f * 0.18f;
            }

            Actor ruler = GetLivingRuler(kingdom);
            if (ruler != null)
            {
                if (ruler.hasTrait(ReformerTraitId))
                {
                    chance += demand == CrisisDemandReformsId
                        ? 0.25f
                        : 0.05f;
                }
                else if (ruler.hasTrait(DiplomatTraitId))
                {
                    chance += demand == CrisisDemandReformsId
                        ? 0.18f
                        : 0.08f;
                }
                else if (ruler.hasTrait(MilitaristTraitId))
                {
                    if (demand == CrisisDemandMilitarizationId)
                    {
                        chance += 0.22f;
                    }
                    else
                    {
                        chance -= 0.20f;
                    }
                }
            }

            if (radicalism >= 85)
            {
                chance -= 0.08f;
            }

            return Mathf.Clamp(chance, 0.05f, 0.85f);
        }

        private static void AcceptPoliticalCrisisDemand(
            Kingdom kingdom,
            string ideology,
            string demand,
            string suffix
        )
        {
            if (kingdom == null)
            {
                return;
            }

            if (demand == CrisisDemandReformsId)
            {
                SetKingdomCourse(kingdom, ReformerTraitId);
            }
            else if (demand == CrisisDemandMilitarizationId)
            {
                SetKingdomCourse(kingdom, MilitaristTraitId);
            }
            else if (demand == CrisisDemandIdeologyId)
            {
                SetStateIdeology(kingdom, ideology);
            }

            int radicalism = GetKingdomIntData(
                kingdom,
                MovementRadicalismPrefix + suffix,
                0
            );
            SetKingdomIntData(
                kingdom,
                MovementRadicalismPrefix + suffix,
                ClampInt(radicalism - 25, 0, 100)
            );
            SetNationalStability(
                kingdom,
                GetNationalStability(kingdom) + 6
            );

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_crisis_accepted"),
                    GetWorldObjectDisplayName(kingdom),
                    GetIdeologyName(ideology),
                    GetCrisisDemandName(demand)
                ),
                kingdom,
                null,
                null,
                GetIdeologyIconPath(ideology),
                "crisis_accepted_" + suffix,
                45f
            );

            ClearPoliticalCrisisData(kingdom);
            SetKingdomIntData(
                kingdom,
                CrisisCooldownDataKey,
                CrisisCooldownTicks
            );
        }

        private static void EndPoliticalCrisis(
            Kingdom kingdom,
            bool publishEvent
        )
        {
            if (kingdom == null)
            {
                return;
            }

            bool wasActive = IsPoliticalCrisisActive(kingdom);
            string ideology = GetPoliticalCrisisIdeology(kingdom);
            string demand = GetKingdomStringData(
                kingdom,
                CrisisDemandDataKey,
                ""
            );
            int pressure = GetPoliticalCrisisPressure(kingdom);

            if (wasActive)
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    eventId: PoliticalWorldAPI.Events.PoliticalCrisisEnded,
                    kingdom: kingdom,
                    oldValue: ideology ?? "",
                    newValue: demand,
                    oldNumber: pressure,
                    newNumber: 0,
                    eventKey: "crisis_ended_" + GetMovementKeySuffix(ideology),
                    category: "political-crisis",
                    year: GetWorldYearSafe()
                );
            }

            if (publishEvent && IsValidIdeology(ideology))
            {
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_crisis_ended"),
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyName(ideology)
                    ),
                    kingdom,
                    null,
                    null,
                    GetIdeologyIconPath(ideology),
                    "crisis_ended_" + GetMovementKeySuffix(ideology),
                    45f
                );
            }

            ClearPoliticalCrisisData(kingdom);
            SetKingdomIntData(
                kingdom,
                CrisisCooldownDataKey,
                CrisisCooldownTicks
            );
        }

        private static void ClearPoliticalCrisisData(Kingdom kingdom)
        {
            SetKingdomIntData(kingdom, CrisisActiveDataKey, 0);
            SetKingdomStringData(kingdom, CrisisIdeologyDataKey, "");
            SetKingdomStringData(kingdom, CrisisDemandDataKey, "");
            SetKingdomIntData(kingdom, CrisisPressureDataKey, 0);
            SetKingdomIntData(kingdom, CrisisStageDataKey, 0);
            SetKingdomIntData(kingdom, CrisisAgeDataKey, 0);
            SetKingdomIntData(kingdom, CrisisResponseMadeDataKey, 0);
        }

        private static bool IsPoliticalCrisisActive(Kingdom kingdom)
        {
            return GetKingdomIntData(
                kingdom,
                CrisisActiveDataKey,
                0
            ) != 0;
        }

        private static string GetPoliticalCrisisIdeology(Kingdom kingdom)
        {
            if (!IsPoliticalCrisisActive(kingdom))
            {
                return null;
            }

            string ideology = GetKingdomStringData(
                kingdom,
                CrisisIdeologyDataKey,
                ""
            );
            return IsValidIdeology(ideology) ? ideology : null;
        }

        private static int GetPoliticalCrisisPressure(Kingdom kingdom)
        {
            if (!IsPoliticalCrisisActive(kingdom))
            {
                return 0;
            }

            return ClampInt(
                GetKingdomIntData(
                    kingdom,
                    CrisisPressureDataKey,
                    0
                ),
                0,
                100
            );
        }

        private static bool IsCrisisDemandAlreadySatisfied(
            Kingdom kingdom,
            string ideology,
            string demand
        )
        {
            if (kingdom == null)
            {
                return true;
            }

            if (demand == CrisisDemandReformsId)
            {
                return GetKingdomCourse(kingdom) == ReformerTraitId;
            }

            if (demand == CrisisDemandMilitarizationId)
            {
                return GetKingdomCourse(kingdom) == MilitaristTraitId;
            }

            if (demand == CrisisDemandIdeologyId)
            {
                return GetStateIdeology(kingdom) == ideology;
            }

            return false;
        }

        private static string FormatPoliticalCrisis(
            string ideology,
            int pressure
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_crisis_none");
            }

            return GetIdeologyName(ideology) + " — " + pressure + "%";
        }

        private static string GetCrisisDemandName(string demand)
        {
            switch (demand)
            {
                case CrisisDemandReformsId:
                    return LM.Get("ukiol_crisis_demand_reforms");
                case CrisisDemandMilitarizationId:
                    return LM.Get("ukiol_crisis_demand_militarization");
                case CrisisDemandIdeologyId:
                    return LM.Get("ukiol_crisis_demand_ideology");
                default:
                    return LM.Get("ukiol_crisis_none");
            }
        }

        private static string GetCrisisColor(int pressure)
        {
            if (pressure <= 0) return "#72E58A";
            if (pressure < 35) return "#E8D36A";
            if (pressure < 55) return "#FFC14D";
            if (pressure < CrisisSeverePressureThreshold) return "#FF853A";
            return "#FF3A3A";
        }

        private static void EnsureMovementLeaderName(
            Kingdom kingdom,
            string ideology,
            string leaderKey
        )
        {
            if (kingdom == null || !IsValidIdeology(ideology))
            {
                return;
            }

            Actor best = null;
            int bestConviction = -1;
            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (int c = 0; c < cities.Count && inspected < 120; c++)
            {
                List<Actor> units = GetCityUnitsSafe(cities[c]);

                for (int i = 0; i < units.Count && inspected < 120; i++)
                {
                    Actor actor = units[i];
                    inspected++;

                    if (
                        actor == null ||
                        !actor.isAlive() ||
                        GetCitizenIdeology(actor) != ideology
                    )
                    {
                        continue;
                    }

                    int conviction = GetCitizenIdeologyConviction(actor);
                    if (conviction > bestConviction)
                    {
                        best = actor;
                        bestConviction = conviction;
                    }
                }
            }

            if (best != null)
            {
                SetKingdomStringData(
                    kingdom,
                    leaderKey,
                    GetWorldObjectDisplayName(best)
                );
            }
        }

    }
}
