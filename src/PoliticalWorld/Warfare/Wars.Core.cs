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
        // Step 9E FAST: declaration, exhaustion, war score, negotiated peace and rebellion-war bridge, moved unchanged from Main.cs.

        private static void UpdatePendingWarDeclarations()
        {
            if (PendingWarDeclarations.Count == 0)
            {
                return;
            }

            List<string> ready = new List<string>();
            foreach (
                KeyValuePair<string, PendingWarDeclaration> pair
                in PendingWarDeclarations
            )
            {
                PendingWarDeclaration pending = pair.Value;
                if (pending == null || Time.time >= pending.ExecuteAt)
                {
                    ready.Add(pair.Key);
                }
            }

            for (int i = 0; i < ready.Count; i++)
            {
                string key = ready[i];
                PendingWarDeclaration pending;
                if (!PendingWarDeclarations.TryGetValue(key, out pending))
                {
                    continue;
                }

                PendingWarDeclarations.Remove(key);

                if (
                    pending == null ||
                    pending.Attacker == null ||
                    pending.Defender == null ||
                    pending.StartMethod == null ||
                    pending.Diplomacy == null
                )
                {
                    ClearWarPreparationState(
                        pending == null ? null : pending.Attacker
                    );
                    if (pending != null && pending.FromDiplomaticCrisis)
                    {
                        ClearDiplomaticCrisisData(pending.Attacker);
                        ClearDiplomaticCrisisData(pending.Defender);
                    }
                    continue;
                }

                List<Kingdom> kingdoms = GetKingdomsSafe();
                if (
                    !kingdoms.Contains(pending.Attacker) ||
                    !kingdoms.Contains(pending.Defender)
                )
                {
                    ClearWarPreparationState(pending.Attacker);
                    continue;
                }

                try
                {
                    _allowPoliticalWarStart = true;
                    pending.StartMethod.Invoke(
                        pending.Diplomacy,
                        pending.Args
                    );

                    WarPairNextRuntimeStartTime[key] =
                        Time.time + 2f;

                    int attackerDeclarationShock =
                        GetIdeologyWarDeclarationShock(pending.Attacker);
                    if (pending.SurpriseAttack)
                    {
                        attackerDeclarationShock +=
                            SurpriseAttackAttackerExhaustion;
                    }
                    AddWarExhaustion(
                        pending.Attacker,
                        attackerDeclarationShock
                    );
                    int defenderDeclarationShock =
                        Math.Max(
                            1,
                            GetIdeologyWarDeclarationShock(pending.Defender) / 2
                        );
                    if (pending.SurpriseAttack)
                    {
                        defenderDeclarationShock +=
                            SurpriseAttackDefenderExhaustion;
                    }
                    AddWarExhaustion(
                        pending.Defender,
                        defenderDeclarationShock
                    );

                    InitializeActivePoliticalWar(
                        pending.Attacker,
                        pending.Defender,
                        pending.CasusBelli,
                        pending.DeclaredYear
                    );

                    if (!pending.FromBlocCollectiveDefense)
                    {
                        ActivateInternationalBlocCollectiveDefense(pending);
                    }

                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_war_hostilities_began"),
                            GetWorldObjectDisplayName(pending.Attacker),
                            GetWorldObjectDisplayName(pending.Defender)
                        ),
                        pending.Attacker,
                        null,
                        GetLivingRuler(pending.Attacker),
                        MilitaristIconPath,
                        "war_hostilities_" + key,
                        15f
                    );
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "Delayed vanilla war start failed: " +
                        exception.Message
                    );
                }
                finally
                {
                    _allowPoliticalWarStart = false;
                    ClearWarPreparationState(pending.Attacker);
                    if (pending.FromDiplomaticCrisis)
                    {
                        ClearDiplomaticCrisisData(pending.Attacker);
                        ClearDiplomaticCrisisData(pending.Defender);
                    }
                }
            }
        }

        private static void WarPeacePostfix(object[] __args)
        {
            try
            {
                List<Kingdom> kingdoms = ExtractWarKingdoms(__args);
                if (kingdoms.Count < 2)
                {
                    return;
                }

                Kingdom first = kingdoms[0];
                Kingdom second = kingdoms[1];

                // Some diplomacy methods with peace-like names are helpers
                // that do not actually end the conflict. Only register a
                // truce after vanilla no longer reports this pair at war.
                if (IsKingdomPairAtWarSafe(first, second))
                {
                    return;
                }

                int untilYear = GetWorldYearSafe() + WarTruceYears;
                SetPairTruceUntilYear(first, second, untilYear);
                SetPairTruceUntilYear(second, first, untilYear);
                SetKingdomIntData(
                    first,
                    WarLatestTruceUntilYearDataKey,
                    untilYear
                );
                SetKingdomIntData(
                    second,
                    WarLatestTruceUntilYearDataKey,
                    untilYear
                );

                FinalizeActivePoliticalWar(first, second);

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_war_truce_signed"),
                        GetWorldObjectDisplayName(first),
                        GetWorldObjectDisplayName(second),
                        untilYear
                    ),
                    first,
                    null,
                    null,
                    DiplomatIconPath,
                    "war_truce_" + GetWarPairKey(first, second),
                    20f
                );
            }
            catch
            {
                // Peace/truce integration is optional. Vanilla peace must
                // never fail because Political World could not inspect args.
            }
        }

        private static void UpdateWarDiplomacyFoundation()
        {
            int currentYear = GetWorldYearSafe();
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null)
                {
                    continue;
                }

                int lastYear = GetKingdomIntData(
                    kingdom,
                    WarExhaustionLastYearDataKey,
                    -1
                );

                if (lastYear < 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        WarExhaustionLastYearDataKey,
                        currentYear
                    );
                }
                else if (currentYear > lastYear)
                {
                    int elapsed = Math.Min(10, currentYear - lastYear);
                    int exhaustion = ClampInt(
                        GetKingdomIntData(
                            kingdom,
                            WarExhaustionDataKey,
                            0
                        ),
                        0,
                        100
                    );

                    if (IsKingdomAtWarSafe(kingdom))
                    {
                        exhaustion = ClampInt(
                            exhaustion +
                                GetIdeologyWarExhaustionPerYear(kingdom) * elapsed,
                            0,
                            100
                        );
                    }
                    else
                    {
                        exhaustion = ClampInt(
                            exhaustion -
                                WarExhaustionPeaceRecoveryPerYear * elapsed,
                            0,
                            100
                        );
                    }

                    SetKingdomIntData(
                        kingdom,
                        WarExhaustionDataKey,
                        exhaustion
                    );
                    SetKingdomIntData(
                        kingdom,
                        WarExhaustionLastYearDataKey,
                        currentYear
                    );
                }

                // A save can occur in the short declaration phase. Runtime
                // MethodInfo/Kingdom references are intentionally not
                // serialized, so clear any stale UI marker after reload.
                if (
                    GetKingdomIntData(
                        kingdom,
                        WarPreparationStateDataKey,
                        0
                    ) != 0 &&
                    !HasRuntimePendingDeclaration(kingdom)
                )
                {
                    ClearWarPreparationState(kingdom);
                }
            }

            UpdateActiveWarPeaceLogic(currentYear);
        }

        private static void InitializeActivePoliticalWar(
            Kingdom attacker,
            Kingdom defender,
            string casusBelli,
            int startYear
        )
        {
            if (attacker == null || defender == null || attacker == defender)
            {
                return;
            }

            ActiveWarSimulation war = new ActiveWarSimulation();
            war.Attacker = attacker;
            war.Defender = defender;
            war.PairKey = GetWarPairKey(attacker, defender);
            war.CasusBelli = string.IsNullOrEmpty(casusBelli)
                ? "strategic_rivalry"
                : casusBelli;
            war.StartYear = startYear;
            war.AttackerStartCities = Math.Max(1, GetCitiesSafe(attacker).Count);
            war.DefenderStartCities = Math.Max(1, GetCitiesSafe(defender).Count);
            war.AttackerStartPopulation = Math.Max(1, GetKingdomPopulationSafe(attacker));
            war.DefenderStartPopulation = Math.Max(1, GetKingdomPopulationSafe(defender));
            war.LastCalculatedYear = -1;
            war.LastWarScore = 0;
            ActiveWarSimulations[war.PairKey] = war;

            PersistActiveWarSide(
                attacker,
                defender,
                1,
                startYear,
                war.AttackerStartCities,
                war.AttackerStartPopulation,
                war.CasusBelli
            );
            PersistActiveWarSide(
                defender,
                attacker,
                2,
                startYear,
                war.DefenderStartCities,
                war.DefenderStartPopulation,
                war.CasusBelli
            );
        }

        private static void PersistActiveWarSide(
            Kingdom kingdom,
            Kingdom other,
            int role,
            int startYear,
            int startCities,
            int startPopulation,
            string casusBelli
        )
        {
            if (kingdom == null || other == null)
            {
                return;
            }

            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairActiveDataPrefix, other),
                1
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairRoleDataPrefix, other),
                role
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairStartYearDataPrefix, other),
                startYear
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairStartCitiesDataPrefix, other),
                Math.Max(1, startCities)
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairStartPopulationDataPrefix, other),
                Math.Max(1, startPopulation)
            );
            SetKingdomStringData(
                kingdom,
                GetPairWarDataKey(WarPairCasusBelliDataPrefix, other),
                casusBelli ?? "strategic_rivalry"
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairNegotiationDataPrefix, other),
                0
            );
        }

        private static string GetPairWarDataKey(
            string prefix,
            Kingdom other
        )
        {
            return prefix +
                StablePartyHash(GetStableObjectIdentity(other)).ToString();
        }

        private static int GetKingdomPopulationSafe(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 0;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            long total = 0;
            for (int i = 0; i < cities.Count; i++)
            {
                total += Math.Max(0, GetCityPopulationSafe(cities[i]));
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)total;
        }

        private static void UpdateActiveWarPeaceLogic(int currentYear)
        {
            List<ActiveWarSimulation> activeWars = GetActivePoliticalWarsSafe();
            HashSet<string> activeKeys = new HashSet<string>();

            for (int i = 0; i < activeWars.Count; i++)
            {
                ActiveWarSimulation war = activeWars[i];
                if (war == null || war.Attacker == null || war.Defender == null)
                {
                    continue;
                }

                activeKeys.Add(war.PairKey);
                if (war.LastCalculatedYear == currentYear)
                {
                    continue;
                }

                war.LastCalculatedYear = currentYear;
                war.LastWarScore = CalculateWarScore(war, currentYear);
                ActiveWarSimulations[war.PairKey] = war;

                string peaceReason;
                if (ShouldOpenPeaceNegotiations(war, currentYear, out peaceReason))
                {
                    SchedulePeaceAgreement(war, peaceReason);
                }
            }

            List<string> stale = new List<string>();
            foreach (
                KeyValuePair<string, ActiveWarSimulation> pair
                in ActiveWarSimulations
            )
            {
                if (!activeKeys.Contains(pair.Key))
                {
                    stale.Add(pair.Key);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                ActiveWarSimulation war;
                if (ActiveWarSimulations.TryGetValue(stale[i], out war))
                {
                    if (war != null)
                    {
                        ClearActiveWarSide(war.Attacker, war.Defender);
                        ClearActiveWarSide(war.Defender, war.Attacker);
                    }
                }
                ActiveWarSimulations.Remove(stale[i]);
                PendingPeaceAgreements.Remove(stale[i]);
            }
        }

        private static List<ActiveWarSimulation> GetActivePoliticalWarsSafe()
        {
            List<ActiveWarSimulation> result = new List<ActiveWarSimulation>();
            object diplomacy = GetMemberValue(
                World.world,
                "diplomacy",
                "_diplomacy"
            );
            if (diplomacy == null)
            {
                return result;
            }

            object wars = GetMemberValue(
                diplomacy,
                "wars",
                "_wars",
                "list_wars",
                "wars_active",
                "active_wars"
            );
            IEnumerable enumerable = wars as IEnumerable;
            if (enumerable == null)
            {
                return result;
            }

            foreach (object warObject in enumerable)
            {
                if (warObject == null)
                {
                    continue;
                }

                List<Kingdom> members = ExtractWarKingdoms(
                    new object[] { warObject }
                );
                if (members.Count < 2)
                {
                    continue;
                }

                Kingdom first = members[0];
                Kingdom second = members[1];
                ActiveWarSimulation simulation = GetOrRebuildWarSimulation(
                    first,
                    second,
                    warObject
                );
                if (simulation != null)
                {
                    simulation.VanillaWarObject = warObject;
                    result.Add(simulation);
                }
            }

            return result;
        }

        private static ActiveWarSimulation GetOrRebuildWarSimulation(
            Kingdom first,
            Kingdom second,
            object vanillaWarObject
        )
        {
            if (first == null || second == null || first == second)
            {
                return null;
            }

            string pairKey = GetWarPairKey(first, second);
            ActiveWarSimulation existing;
            if (ActiveWarSimulations.TryGetValue(pairKey, out existing))
            {
                existing.VanillaWarObject = vanillaWarObject;
                return existing;
            }

            int firstRole = GetKingdomIntData(
                first,
                GetPairWarDataKey(WarPairRoleDataPrefix, second),
                0
            );
            int secondRole = GetKingdomIntData(
                second,
                GetPairWarDataKey(WarPairRoleDataPrefix, first),
                0
            );

            Kingdom attacker = firstRole == 1 || secondRole == 2
                ? first
                : second;
            Kingdom defender = attacker == first ? second : first;

            int startYear = GetKingdomIntData(
                attacker,
                GetPairWarDataKey(WarPairStartYearDataPrefix, defender),
                GetWorldYearSafe()
            );
            int attackerCities = GetKingdomIntData(
                attacker,
                GetPairWarDataKey(WarPairStartCitiesDataPrefix, defender),
                Math.Max(1, GetCitiesSafe(attacker).Count)
            );
            int defenderCities = GetKingdomIntData(
                defender,
                GetPairWarDataKey(WarPairStartCitiesDataPrefix, attacker),
                Math.Max(1, GetCitiesSafe(defender).Count)
            );
            int attackerPopulation = GetKingdomIntData(
                attacker,
                GetPairWarDataKey(WarPairStartPopulationDataPrefix, defender),
                Math.Max(1, GetKingdomPopulationSafe(attacker))
            );
            int defenderPopulation = GetKingdomIntData(
                defender,
                GetPairWarDataKey(WarPairStartPopulationDataPrefix, attacker),
                Math.Max(1, GetKingdomPopulationSafe(defender))
            );
            string casus = GetKingdomStringData(
                attacker,
                GetPairWarDataKey(WarPairCasusBelliDataPrefix, defender),
                "strategic_rivalry"
            );

            ActiveWarSimulation rebuilt = new ActiveWarSimulation();
            rebuilt.Attacker = attacker;
            rebuilt.Defender = defender;
            rebuilt.PairKey = pairKey;
            rebuilt.CasusBelli = string.IsNullOrEmpty(casus)
                ? "strategic_rivalry"
                : casus;
            rebuilt.StartYear = startYear;
            rebuilt.AttackerStartCities = Math.Max(1, attackerCities);
            rebuilt.DefenderStartCities = Math.Max(1, defenderCities);
            rebuilt.AttackerStartPopulation = Math.Max(1, attackerPopulation);
            rebuilt.DefenderStartPopulation = Math.Max(1, defenderPopulation);
            rebuilt.VanillaWarObject = vanillaWarObject;
            rebuilt.LastCalculatedYear = -1;
            rebuilt.LastWarScore = 0;
            ActiveWarSimulations[pairKey] = rebuilt;

            // Vanilla wars that existed before installing dev8 have no
            // persisted attacker role. Record the inferred baseline now so
            // the next save/load keeps a stable simulation.
            PersistActiveWarSide(
                attacker,
                defender,
                1,
                rebuilt.StartYear,
                rebuilt.AttackerStartCities,
                rebuilt.AttackerStartPopulation,
                rebuilt.CasusBelli
            );
            PersistActiveWarSide(
                defender,
                attacker,
                2,
                rebuilt.StartYear,
                rebuilt.DefenderStartCities,
                rebuilt.DefenderStartPopulation,
                rebuilt.CasusBelli
            );

            return rebuilt;
        }

        private static int CalculateWarScore(
            ActiveWarSimulation war,
            int currentYear
        )
        {
            if (war == null || war.Attacker == null || war.Defender == null)
            {
                return 0;
            }

            int attackerCities = GetCitiesSafe(war.Attacker).Count;
            int defenderCities = GetCitiesSafe(war.Defender).Count;
            int attackerPopulation = Math.Max(0, GetKingdomPopulationSafe(war.Attacker));
            int defenderPopulation = Math.Max(0, GetKingdomPopulationSafe(war.Defender));

            float defenderCityLoss = 1f -
                Math.Min(1f, defenderCities / (float)Math.Max(1, war.DefenderStartCities));
            float attackerCityLoss = 1f -
                Math.Min(1f, attackerCities / (float)Math.Max(1, war.AttackerStartCities));
            float defenderPopulationLoss = 1f -
                Math.Min(1f, defenderPopulation / (float)Math.Max(1, war.DefenderStartPopulation));
            float attackerPopulationLoss = 1f -
                Math.Min(1f, attackerPopulation / (float)Math.Max(1, war.AttackerStartPopulation));

            int attackerExhaustion = ClampInt(
                GetKingdomIntData(war.Attacker, WarExhaustionDataKey, 0),
                0,
                100
            );
            int defenderExhaustion = ClampInt(
                GetKingdomIntData(war.Defender, WarExhaustionDataKey, 0),
                0,
                100
            );

            float score = 0f;
            score += defenderCityLoss * 55f;
            score -= attackerCityLoss * 55f;
            score += defenderPopulationLoss * 20f;
            score -= attackerPopulationLoss * 20f;
            score += (defenderExhaustion - attackerExhaustion) * 0.20f;

            int duration = Math.Max(0, currentYear - war.StartYear);
            if (duration >= 4)
            {
                score += Math.Min(8f, (duration - 3) * 0.75f);
            }

            return ClampInt((int)Math.Round(score), -100, 100);
        }

        private static bool ShouldOpenPeaceNegotiations(
            ActiveWarSimulation war,
            int currentYear,
            out string reason
        )
        {
            reason = "";
            if (war == null || war.Attacker == null || war.Defender == null)
            {
                return false;
            }

            if (PendingPeaceAgreements.ContainsKey(war.PairKey))
            {
                return false;
            }

            float nextAttempt;
            if (
                WarPairNextPeaceAttemptTime.TryGetValue(war.PairKey, out nextAttempt) &&
                Time.time < nextAttempt
            )
            {
                return false;
            }

            int duration = Math.Max(0, currentYear - war.StartYear);
            int score = war.LastWarScore;
            int attackerExhaustion = ClampInt(
                GetKingdomIntData(war.Attacker, WarExhaustionDataKey, 0),
                0,
                100
            );
            int defenderExhaustion = ClampInt(
                GetKingdomIntData(war.Defender, WarExhaustionDataKey, 0),
                0,
                100
            );
            int attackerCities = GetCitiesSafe(war.Attacker).Count;
            int defenderCities = GetCitiesSafe(war.Defender).Count;

            if (attackerCities <= 0 || defenderCities <= 0)
            {
                reason = "state_collapse";
                return true;
            }

            if (duration < WarMinimumPeaceYears)
            {
                return false;
            }

            if (score >= WarMajorVictoryScore)
            {
                reason = "attacker_major_victory";
                return true;
            }
            if (score <= -WarMajorVictoryScore)
            {
                reason = "defender_major_victory";
                return true;
            }

            bool conquestGoal =
                war.CasusBelli == "conquest" ||
                war.CasusBelli == "expansion";
            bool defenderLostCity =
                defenderCities < war.DefenderStartCities;
            if (
                conquestGoal &&
                duration >= WarGoalPeaceYears &&
                defenderLostCity &&
                score >= WarGoalSatisfiedScore
            )
            {
                reason = "war_goal_achieved";
                return true;
            }

            if (
                defenderExhaustion >= WarCriticalExhaustion &&
                score >= 5
            )
            {
                reason = "defender_exhausted";
                return true;
            }
            if (
                attackerExhaustion >= WarCriticalExhaustion &&
                score <= 10
            )
            {
                reason = "attacker_exhausted";
                return true;
            }
            if (
                attackerExhaustion >= WarMutualExhaustion &&
                defenderExhaustion >= WarMutualExhaustion &&
                duration >= 2
            )
            {
                reason = "mutual_exhaustion";
                return true;
            }
            if (
                duration >= WarLongWarYears &&
                Math.Abs(score) < WarMajorVictoryScore
            )
            {
                reason = "long_war"
                    ;
                return true;
            }

            return false;
        }

        private static void SchedulePeaceAgreement(
            ActiveWarSimulation war,
            string reason
        )
        {
            if (
                war == null ||
                war.Attacker == null ||
                war.Defender == null ||
                PendingPeaceAgreements.ContainsKey(war.PairKey)
            )
            {
                return;
            }

            PendingPeaceAgreement pending = new PendingPeaceAgreement();
            pending.War = war;
            pending.PairKey = war.PairKey;
            pending.Reason = reason ?? "mutual_exhaustion";
            pending.ExecuteAt = Time.time + WarPeaceNegotiationSeconds;
            PendingPeaceAgreements[war.PairKey] = pending;

            SetKingdomIntData(
                war.Attacker,
                GetPairWarDataKey(WarPairNegotiationDataPrefix, war.Defender),
                1
            );
            SetKingdomIntData(
                war.Defender,
                GetPairWarDataKey(WarPairNegotiationDataPrefix, war.Attacker),
                1
            );

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_peace_negotiations"),
                    GetWorldObjectDisplayName(war.Attacker),
                    GetWorldObjectDisplayName(war.Defender),
                    GetPeaceReasonName(pending.Reason),
                    war.LastWarScore
                ),
                war.Attacker,
                null,
                null,
                DiplomatIconPath,
                "peace_negotiations_" + war.PairKey,
                15f
            );
        }

        private static void UpdatePendingPeaceAgreements()
        {
            if (PendingPeaceAgreements.Count == 0)
            {
                return;
            }

            List<string> ready = new List<string>();
            foreach (
                KeyValuePair<string, PendingPeaceAgreement> pair
                in PendingPeaceAgreements
            )
            {
                if (pair.Value == null || Time.time >= pair.Value.ExecuteAt)
                {
                    ready.Add(pair.Key);
                }
            }

            for (int i = 0; i < ready.Count; i++)
            {
                string pairKey = ready[i];
                PendingPeaceAgreement pending;
                if (!PendingPeaceAgreements.TryGetValue(pairKey, out pending))
                {
                    continue;
                }
                PendingPeaceAgreements.Remove(pairKey);

                if (
                    pending == null ||
                    pending.War == null ||
                    pending.War.Attacker == null ||
                    pending.War.Defender == null
                )
                {
                    continue;
                }

                ActiveWarSimulation war = pending.War;
                if (!IsKingdomPairAtWarSafe(war.Attacker, war.Defender))
                {
                    FinalizeActivePoliticalWar(war.Attacker, war.Defender);
                    continue;
                }

                bool ended = TryInvokeVanillaPeace(
                    war.Attacker,
                    war.Defender,
                    war.VanillaWarObject
                );

                if (!ended)
                {
                    WarPairNextPeaceAttemptTime[pairKey] = Time.time + 8f;
                    SetKingdomIntData(
                        war.Attacker,
                        GetPairWarDataKey(WarPairNegotiationDataPrefix, war.Defender),
                        0
                    );
                    SetKingdomIntData(
                        war.Defender,
                        GetPairWarDataKey(WarPairNegotiationDataPrefix, war.Attacker),
                        0
                    );
                    LogWarning(
                        "Political peace could not find a compatible vanilla peace method for " +
                        GetWorldObjectDisplayName(war.Attacker) + " / " +
                        GetWorldObjectDisplayName(war.Defender) + ". Vanilla war continues."
                    );
                    LogWarDiplomacyCandidates(
                        _patchedDiplomacyInstance == null
                            ? null
                            : _patchedDiplomacyInstance.GetType()
                    );
                }
            }
        }

        private static bool TryInvokeVanillaPeace(
            Kingdom first,
            Kingdom second,
            object knownWarObject
        )
        {
            object diplomacy = _patchedDiplomacyInstance ?? GetMemberValue(
                World.world,
                "diplomacy",
                "_diplomacy"
            );
            if (diplomacy == null || first == null || second == null)
            {
                return false;
            }

            object warObject = knownWarObject ?? FindActiveWarObjectForPair(first, second);
            string[] preferredNames = new string[]
            {
                "endWar",
                "makePeace",
                "stopWar",
                "finishWar",
                "endConflict",
                "removeWar"
            };

            MethodInfo[] methods = diplomacy.GetType().GetMethods(MemberFlags);
            for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
            {
                string wanted = preferredNames[nameIndex];
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != wanted)
                    {
                        continue;
                    }

                    object[] invocationArgs;
                    if (!TryBuildPeaceInvocationArgs(
                        method,
                        first,
                        second,
                        warObject,
                        out invocationArgs
                    ))
                    {
                        continue;
                    }

                    try
                    {
                        method.Invoke(diplomacy, invocationArgs);
                        if (!IsKingdomPairAtWarSafe(first, second))
                        {
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        LogWarning(
                            "Peace method " + method.Name + " failed: " +
                            exception.Message
                        );
                    }
                }
            }

            if (warObject != null)
            {
                MethodInfo[] warMethods = warObject.GetType().GetMethods(MemberFlags);
                string[] warMethodNames = new string[]
                {
                    "end",
                    "finish",
                    "stop",
                    "makePeace"
                };
                for (int n = 0; n < warMethodNames.Length; n++)
                {
                    for (int i = 0; i < warMethods.Length; i++)
                    {
                        MethodInfo method = warMethods[i];
                        if (
                            method == null ||
                            method.Name != warMethodNames[n] ||
                            method.GetParameters().Length != 0
                        )
                        {
                            continue;
                        }
                        try
                        {
                            method.Invoke(warObject, null);
                            if (!IsKingdomPairAtWarSafe(first, second))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryBuildPeaceInvocationArgs(
            MethodInfo method,
            Kingdom first,
            Kingdom second,
            object warObject,
            out object[] invocationArgs
        )
        {
            invocationArgs = null;
            if (method == null)
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            object[] values = new object[parameters.Length];
            int kingdomIndex = 0;
            bool usedWar = false;
            bool usedKingdom = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type type = parameter.ParameterType;

                if (typeof(Kingdom).IsAssignableFrom(type))
                {
                    values[i] = kingdomIndex == 0 ? first : second;
                    kingdomIndex++;
                    usedKingdom = true;
                    continue;
                }

                if (
                    warObject != null &&
                    type.IsInstanceOfType(warObject)
                )
                {
                    values[i] = warObject;
                    usedWar = true;
                    continue;
                }

                if (parameter.IsOptional)
                {
                    values[i] = parameter.DefaultValue;
                    continue;
                }
                if (type == typeof(bool))
                {
                    values[i] = false;
                    continue;
                }
                if (type == typeof(int))
                {
                    values[i] = 0;
                    continue;
                }
                if (type == typeof(float))
                {
                    values[i] = 0f;
                    continue;
                }
                if (type == typeof(double))
                {
                    values[i] = 0d;
                    continue;
                }
                if (type == typeof(string))
                {
                    values[i] = "";
                    continue;
                }
                if (type.IsEnum)
                {
                    Array enumValues = Enum.GetValues(type);
                    values[i] = enumValues.Length > 0
                        ? enumValues.GetValue(0)
                        : Activator.CreateInstance(type);
                    continue;
                }
                if (!type.IsValueType)
                {
                    values[i] = null;
                    continue;
                }

                return false;
            }

            if (!usedWar && kingdomIndex < 2 && !usedKingdom)
            {
                return false;
            }
            if (usedKingdom && kingdomIndex == 1 && !usedWar)
            {
                return false;
            }

            invocationArgs = values;
            return true;
        }

        private static object FindActiveWarObjectForPair(
            Kingdom first,
            Kingdom second
        )
        {
            object diplomacy = _patchedDiplomacyInstance ?? GetMemberValue(
                World.world,
                "diplomacy",
                "_diplomacy"
            );
            object wars = GetMemberValue(
                diplomacy,
                "wars",
                "_wars",
                "list_wars",
                "wars_active",
                "active_wars"
            );
            IEnumerable enumerable = wars as IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            foreach (object war in enumerable)
            {
                List<Kingdom> members = ExtractWarKingdoms(
                    new object[] { war }
                );
                if (members.Contains(first) && members.Contains(second))
                {
                    return war;
                }
            }
            return null;
        }

        private static bool IsKingdomPairAtWarSafe(
            Kingdom first,
            Kingdom second
        )
        {
            return FindActiveWarObjectForPair(first, second) != null;
        }

        private static void FinalizeActivePoliticalWar(
            Kingdom first,
            Kingdom second
        )
        {
            if (first == null || second == null)
            {
                return;
            }

            string pairKey = GetWarPairKey(first, second);
            ActiveWarSimulations.Remove(pairKey);
            PendingPeaceAgreements.Remove(pairKey);
            WarPairNextPeaceAttemptTime.Remove(pairKey);
            ClearActiveWarSide(first, second);
            ClearActiveWarSide(second, first);
        }

        private static void ClearActiveWarSide(
            Kingdom kingdom,
            Kingdom other
        )
        {
            if (kingdom == null || other == null)
            {
                return;
            }

            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairActiveDataPrefix, other),
                0
            );
            SetKingdomIntData(
                kingdom,
                GetPairWarDataKey(WarPairNegotiationDataPrefix, other),
                0
            );
        }

        private static ActiveWarSimulation GetVisibleActiveWarForKingdom(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return null;
            }

            List<ActiveWarSimulation> wars = GetActivePoliticalWarsSafe();
            for (int i = 0; i < wars.Count; i++)
            {
                ActiveWarSimulation war = wars[i];
                if (
                    war != null &&
                    (war.Attacker == kingdom || war.Defender == kingdom)
                )
                {
                    war.LastWarScore = CalculateWarScore(
                        war,
                        GetWorldYearSafe()
                    );
                    return war;
                }
            }
            return null;
        }

        private static int GetWarScoreForKingdom(
            ActiveWarSimulation war,
            Kingdom kingdom
        )
        {
            if (war == null || kingdom == null)
            {
                return 0;
            }
            return war.Defender == kingdom
                ? -war.LastWarScore
                : war.LastWarScore;
        }

        private static string FormatWarScore(int score)
        {
            score = ClampInt(score, -100, 100);
            return score > 0
                ? "+" + score.ToString()
                : score.ToString();
        }

        private static Color GetWarScoreColor(int score)
        {
            if (score >= 25)
            {
                return new Color(0.48f, 0.88f, 0.55f, 1f);
            }
            if (score <= -25)
            {
                return new Color(0.95f, 0.36f, 0.30f, 1f);
            }
            return new Color(0.95f, 0.75f, 0.35f, 1f);
        }

        private static string GetPeaceReasonName(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                reason = "mutual_exhaustion";
            }
            string key = "ukiol_peace_reason_" + reason;
            string localized = LM.Get(key);
            return string.IsNullOrEmpty(localized) || localized == key
                ? reason
                : localized;
        }

        private static bool HasRuntimePendingDeclaration(Kingdom attacker)
        {
            if (attacker == null)
            {
                return false;
            }

            foreach (
                KeyValuePair<string, PendingWarDeclaration> pair
                in PendingWarDeclarations
            )
            {
                if (
                    pair.Value != null &&
                    pair.Value.Attacker == attacker
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractDirectWarKingdoms(
            object[] args,
            out Kingdom attacker,
            out Kingdom defender
        )
        {
            attacker = null;
            defender = null;

            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                Kingdom kingdom = args[i] as Kingdom;
                if (kingdom == null)
                {
                    continue;
                }

                if (attacker == null)
                {
                    attacker = kingdom;
                }
                else if (kingdom != attacker)
                {
                    defender = kingdom;
                    break;
                }
            }

            return attacker != null && defender != null;
        }

        private static List<Kingdom> ExtractWarKingdoms(object[] args)
        {
            List<Kingdom> result = new List<Kingdom>();
            if (args == null)
            {
                return result;
            }

            for (int i = 0; i < args.Length; i++)
            {
                object arg = args[i];
                AddUniqueWarKingdom(result, arg as Kingdom);

                if (arg == null || result.Count >= 2)
                {
                    continue;
                }

                string[] memberNames = new string[]
                {
                    "kingdom_1",
                    "kingdom_2",
                    "kingdom1",
                    "kingdom2",
                    "attacker",
                    "defender",
                    "main_attacker",
                    "main_defender",
                    "kingdom_attacker",
                    "kingdom_defender",
                    "kingdomAttack",
                    "kingdomDefend"
                };

                for (int m = 0; m < memberNames.Length; m++)
                {
                    AddUniqueWarKingdom(
                        result,
                        GetMemberValue(arg, memberNames[m]) as Kingdom
                    );
                    if (result.Count >= 2)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static void AddUniqueWarKingdom(
            List<Kingdom> result,
            Kingdom kingdom
        )
        {
            if (
                result != null &&
                kingdom != null &&
                !result.Contains(kingdom)
            )
            {
                result.Add(kingdom);
            }
        }

        private static bool IsInternalCivilWarType(object warType)
        {
            if (warType == null)
            {
                return false;
            }

            string id = "";
            object idValue = GetMemberValue(
                warType,
                "id",
                "_id",
                "name"
            );
            if (idValue != null)
            {
                id = idValue.ToString().ToLowerInvariant();
            }

            return
                id.Contains("inspire") ||
                id.Contains("rebel") ||
                id.Contains("civil");
        }

        private static object[] CloneObjectArray(object[] source)
        {
            if (source == null)
            {
                return new object[0];
            }

            object[] result = new object[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static string GetWarPairKey(
            Kingdom first,
            Kingdom second
        )
        {
            string firstId = GetStableObjectIdentity(first);
            string secondId = GetStableObjectIdentity(second);

            if (string.CompareOrdinal(firstId, secondId) <= 0)
            {
                return firstId + "|" + secondId;
            }

            return secondId + "|" + firstId;
        }

        private static string GetPairTruceDataKey(Kingdom other)
        {
            return WarPairTruceDataPrefix +
                StablePartyHash(GetStableObjectIdentity(other)).ToString();
        }

        private static int GetPairTruceUntilYear(
            Kingdom kingdom,
            Kingdom other
        )
        {
            if (kingdom == null || other == null)
            {
                return 0;
            }

            return GetKingdomIntData(
                kingdom,
                GetPairTruceDataKey(other),
                0
            );
        }

        private static void SetPairTruceUntilYear(
            Kingdom kingdom,
            Kingdom other,
            int untilYear
        )
        {
            if (kingdom == null || other == null)
            {
                return;
            }

            SetKingdomIntData(
                kingdom,
                GetPairTruceDataKey(other),
                Math.Max(0, untilYear)
            );
        }

        private static float GetWarPreparationSeconds(Kingdom attacker)
        {
            float seconds = WarPreparationSeconds;
            IdeologyBehaviorProfile behavior = GetIdeologyBehaviorProfile(attacker);

            if (behavior.Militarism >= 80) seconds -= 0.75f;
            else if (behavior.Militarism <= 25) seconds += 1.25f;
            if (behavior.Stateless) seconds += 0.75f;

            string course = GetKingdomCourse(attacker);
            if (course == MilitaristTraitId) seconds -= 1.25f;
            else if (course == DiplomatTraitId) seconds += 1.5f;

            string blocType = GetKingdomStringData(
                attacker,
                InternationalBlocTypeDataKey,
                ""
            );
            int blocIntegration = GetKingdomIntData(
                attacker,
                InternationalBlocIntegrationDataKey,
                0
            );
            if (blocIntegration >= 35)
            {
                if (blocType == "military_political") seconds -= 0.45f;
                else if (blocType == "defensive") seconds -= 0.20f;
            }

            return Math.Max(1.5f, seconds);
        }

        private static string DetermineBasicCasusBelli(
            Kingdom attacker,
            Kingdom defender
        )
        {
            string course = GetKingdomCourse(attacker);
            string ideology = GetStateIdeology(attacker);
            string defenderIdeology = GetStateIdeology(defender);

            if (course == MilitaristTraitId)
            {
                return "conquest";
            }
            if (ideology == FascismIdeologyId)
            {
                return "expansion";
            }
            if (
                IsValidIdeology(ideology) &&
                IsValidIdeology(defenderIdeology) &&
                ideology != defenderIdeology
            )
            {
                return "ideological_rivalry";
            }
            return "strategic_rivalry";
        }

        private static string GetCasusBelliName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "strategic_rivalry";
            }

            string key = "ukiol_casus_belli_" + id;
            string localized = LM.Get(key);
            return string.IsNullOrEmpty(localized) || localized == key
                ? id
                : localized;
        }

        private static void AddWarExhaustion(
            Kingdom kingdom,
            int amount
        )
        {
            if (kingdom == null)
            {
                return;
            }

            int current = GetKingdomIntData(
                kingdom,
                WarExhaustionDataKey,
                0
            );
            SetKingdomIntData(
                kingdom,
                WarExhaustionDataKey,
                ClampInt(current + amount, 0, 100)
            );
        }

        private static void ClearWarPreparationState(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return;
            }

            SetKingdomIntData(
                kingdom,
                WarPreparationStateDataKey,
                0
            );
            SetKingdomStringData(
                kingdom,
                WarPreparationTargetNameDataKey,
                ""
            );
            SetKingdomStringData(
                kingdom,
                WarPreparationCasusBelliDataKey,
                ""
            );
        }

        private static bool IsKingdomAtWarSafe(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return false;
            }

            string[] methodNames = new string[]
            {
                "isAtWar",
                "hasWars",
                "hasEnemies"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo method = FindMethodByNameAndParameterCount(
                    kingdom.GetType(),
                    methodNames[i],
                    0
                );
                if (method == null || method.ReturnType != typeof(bool))
                {
                    continue;
                }

                try
                {
                    return (bool)method.Invoke(kingdom, null);
                }
                catch
                {
                }
            }

            object collection = GetMemberValue(
                kingdom,
                "wars",
                "_wars",
                "enemies",
                "_enemies",
                "enemy_kingdoms",
                "_enemy_kingdoms"
            );
            IEnumerable enumerable = collection as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    if (item != null)
                    {
                        return true;
                    }
                }
            }

            object diplomacy = GetMemberValue(
                World.world,
                "diplomacy",
                "_diplomacy"
            );
            object wars = GetMemberValue(
                diplomacy,
                "wars",
                "_wars",
                "list_wars",
                "wars_active",
                "active_wars"
            );
            IEnumerable warEnumerable = wars as IEnumerable;
            if (warEnumerable != null)
            {
                foreach (object war in warEnumerable)
                {
                    if (war == null)
                    {
                        continue;
                    }
                    List<Kingdom> members = ExtractWarKingdoms(
                        new object[] { war }
                    );
                    if (members.Contains(kingdom))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string GetWarDiplomacyStatusText(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return LM.Get("ukiol_war_status_peace");
            }

            if (
                GetKingdomIntData(
                    kingdom,
                    WarPreparationStateDataKey,
                    0
                ) != 0
            )
            {
                string target = GetKingdomStringData(
                    kingdom,
                    WarPreparationTargetNameDataKey,
                    "?"
                );
                string cb = GetKingdomStringData(
                    kingdom,
                    WarPreparationCasusBelliDataKey,
                    "strategic_rivalry"
                );
                return string.Format(
                    LM.Get("ukiol_war_status_declared"),
                    target,
                    GetCasusBelliName(cb)
                );
            }

            if (IsKingdomAtWarSafe(kingdom))
            {
                return LM.Get("ukiol_war_status_active");
            }

            return LM.Get("ukiol_war_status_peace");
        }

        private static Color GetWarDiplomacyStatusColor(Kingdom kingdom)
        {
            if (
                kingdom != null &&
                GetKingdomIntData(
                    kingdom,
                    WarPreparationStateDataKey,
                    0
                ) != 0
            )
            {
                return new Color(0.97f, 0.72f, 0.28f, 1f);
            }

            if (IsKingdomAtWarSafe(kingdom))
            {
                return new Color(0.95f, 0.35f, 0.30f, 1f);
            }

            return new Color(0.46f, 0.88f, 0.58f, 1f);
        }

        private static Color GetWarExhaustionColor(int exhaustion)
        {
            exhaustion = ClampInt(exhaustion, 0, 100);
            if (exhaustion >= 75)
            {
                return new Color(0.95f, 0.36f, 0.30f, 1f);
            }
            if (exhaustion >= 40)
            {
                return new Color(0.96f, 0.70f, 0.28f, 1f);
            }
            return new Color(0.50f, 0.86f, 0.56f, 1f);
        }

        private static void TryStartRebellionWar(
            Kingdom originalKingdom,
            Kingdom rebelKingdom
        )
        {
            try
            {
                object diplomacy = GetMemberValue(
                    World.world,
                    "diplomacy",
                    "_diplomacy"
                );

                if (diplomacy == null)
                {
                    return;
                }

                Type warTypeLibrary =
                    typeof(World).Assembly.GetType(
                        "WarTypeLibrary"
                    );

                if (warTypeLibrary == null)
                {
                    return;
                }

                object inspireAsset = null;

                FieldInfo inspireField =
                    warTypeLibrary.GetField(
                        "inspire",
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (inspireField != null)
                {
                    inspireAsset =
                        inspireField.GetValue(null);
                }

                if (inspireAsset == null)
                {
                    PropertyInfo inspireProperty =
                        warTypeLibrary.GetProperty(
                            "inspire",
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic
                        );

                    if (inspireProperty != null)
                    {
                        inspireAsset =
                            inspireProperty.GetValue(
                                null,
                                null
                            );
                    }
                }

                if (inspireAsset == null)
                {
                    return;
                }

                MethodInfo startWarMethod =
                    FindMethodByNameAndParameterCount(
                        diplomacy.GetType(),
                        "startWar",
                        4
                    );

                if (startWarMethod == null)
                {
                    return;
                }

                try
                {
                    _allowPoliticalWarStart = true;
                    startWarMethod.Invoke(
                        diplomacy,
                        new object[]
                        {
                            originalKingdom,
                            rebelKingdom,
                            inspireAsset,
                            false
                        }
                    );
                }
                finally
                {
                    _allowPoliticalWarStart = false;
                }
            }
            catch
            {
                // Новое государство уже существует.
                // Если WorldBox поменяет API войн, мод не падает.
            }
        }
    }
}
