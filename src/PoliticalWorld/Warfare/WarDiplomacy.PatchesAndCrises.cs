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
        // Step 9E FAST: vanilla diplomacy interception and diplomatic-crisis pipeline, moved unchanged from Main.cs.

        private static void EnsureWarDiplomacyPatches()
        {
            if (_harmony == null || World.world == null)
            {
                return;
            }

            object diplomacy = GetMemberValue(
                World.world,
                "diplomacy",
                "_diplomacy"
            );

            if (diplomacy == null)
            {
                return;
            }

            // Harmony patches are installed on the DiplomacyManager type, but
            // the concrete manager instance is owned by one world/save. Always
            // refresh it before the fast installed-patches return path.
            _patchedDiplomacyInstance = diplomacy;

            if (
                _warStartPatchInstalled &&
                _warPeacePatchInstalled
            )
            {
                return;
            }

            if (Time.unscaledTime < _nextWarPatchAttemptTime)
            {
                return;
            }

            _nextWarPatchAttemptTime = Time.unscaledTime + 5f;

            if (!_warStartPatchInstalled)
            {
                _warStartPatchInstalled =
                    TryInstallWarStartPatches(diplomacy.GetType());

                if (!_warStartPatchInstalled)
                {
                    LogWarDiplomacyCandidates(diplomacy.GetType());
                }
            }

            if (!_warPeacePatchInstalled)
            {
                _warPeacePatchInstalled =
                    TryInstallWarPeacePatches(diplomacy.GetType());
            }
        }

        private static bool TryInstallWarStartPatches(Type diplomacyType)
        {
            if (_harmony == null || diplomacyType == null)
            {
                return false;
            }

            MethodInfo prefix = typeof(Main).GetMethod(
                nameof(WarStartPrefix),
                BindingFlags.Static | BindingFlags.NonPublic
            );
            MethodInfo postfix = typeof(Main).GetMethod(
                nameof(WarStartPostfix),
                BindingFlags.Static | BindingFlags.NonPublic
            );
            if (prefix == null || postfix == null)
            {
                return false;
            }

            MethodInfo[] methods = diplomacyType.GetMethods(MemberFlags);
            bool installedAny = false;
            MethodInfo preferred = null;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != "startWar")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (
                    parameters.Length < 2 ||
                    !typeof(Kingdom).IsAssignableFrom(parameters[0].ParameterType) ||
                    !typeof(Kingdom).IsAssignableFrom(parameters[1].ParameterType)
                )
                {
                    continue;
                }

                string patchKey =
                    diplomacyType.FullName + "|" +
                    method.Name + "|" +
                    parameters.Length + "|" +
                    method.ToString();

                if (!PatchedWarStartMethods.Contains(patchKey))
                {
                    try
                    {
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(prefix),
                            postfix: new HarmonyMethod(postfix)
                        );
                        PatchedWarStartMethods.Add(patchKey);
                        LogInfo(
                            "War diplomacy patch: intercepted " +
                            diplomacyType.Name + "." +
                            method.Name + " params=" +
                            parameters.Length
                        );
                    }
                    catch (Exception exception)
                    {
                        LogWarning(
                            "War start overload patch failed (params=" +
                            parameters.Length + "): " +
                            exception.Message
                        );
                        continue;
                    }
                }

                installedAny = true;
                if (preferred == null || parameters.Length == 4)
                {
                    preferred = method;
                }
            }

            if (preferred != null)
            {
                _patchedWarStartMethod = preferred;
            }

            return installedAny;
        }

        private static MethodInfo FindWarStartMethod(Type diplomacyType)
        {
            if (diplomacyType == null)
            {
                return null;
            }

            MethodInfo[] methods = diplomacyType.GetMethods(MemberFlags);
            MethodInfo fallback = null;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != "startWar")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 2)
                {
                    continue;
                }

                if (
                    typeof(Kingdom).IsAssignableFrom(parameters[0].ParameterType) &&
                    typeof(Kingdom).IsAssignableFrom(parameters[1].ParameterType)
                )
                {
                    if (parameters.Length == 4)
                    {
                        return method;
                    }
                    fallback = method;
                }
            }

            return fallback;
        }

        private static bool TryInstallWarPeacePatches(Type diplomacyType)
        {
            if (_harmony == null || diplomacyType == null)
            {
                return false;
            }

            MethodInfo postfix = typeof(Main).GetMethod(
                nameof(WarPeacePostfix),
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

            if (postfix == null)
            {
                return false;
            }

            string[] names = new string[]
            {
                "endWar",
                "stopWar",
                "finishWar",
                "makePeace",
                "endConflict",
                "removeWar"
            };

            MethodInfo[] methods = diplomacyType.GetMethods(MemberFlags);
            bool installedAny = false;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                bool nameMatch = false;
                for (int n = 0; n < names.Length; n++)
                {
                    if (method.Name == names[n])
                    {
                        nameMatch = true;
                        break;
                    }
                }

                if (!nameMatch)
                {
                    continue;
                }

                string patchKey =
                    diplomacyType.FullName + "|" +
                    method.Name + "|" +
                    method.GetParameters().Length;

                if (PatchedWarPeaceMethods.Contains(patchKey))
                {
                    installedAny = true;
                    continue;
                }

                try
                {
                    _harmony.Patch(
                        method,
                        postfix: new HarmonyMethod(postfix)
                    );
                    PatchedWarPeaceMethods.Add(patchKey);
                    installedAny = true;
                    LogInfo(
                        "War peace patch: " +
                        diplomacyType.Name + "." +
                        method.Name + " params=" +
                        method.GetParameters().Length
                    );
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "War peace patch failed for " +
                        method.Name + ": " +
                        exception.Message
                    );
                }
            }

            return installedAny;
        }

        private static void LogWarDiplomacyCandidates(Type diplomacyType)
        {
            if (diplomacyType == null)
            {
                return;
            }

            try
            {
                MethodInfo[] methods = diplomacyType.GetMethods(MemberFlags);
                int logged = 0;

                for (int i = 0; i < methods.Length && logged < 30; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null)
                    {
                        continue;
                    }

                    string lower = method.Name.ToLowerInvariant();
                    if (
                        lower.Contains("war") ||
                        lower.Contains("peace") ||
                        lower.Contains("enemy")
                    )
                    {
                        LogInfo(
                            "Diplomacy candidate: " +
                            diplomacyType.Name + "." +
                            method.Name + " params=" +
                            method.GetParameters().Length +
                            " returns=" + method.ReturnType.Name
                        );
                        logged++;
                    }
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Diplomacy candidate scan failed: " +
                    exception.Message
                );
            }
        }

        private static bool WarStartPrefix(
            object __instance,
            object[] __args,
            MethodBase __originalMethod
        )
        {
            try
            {
                if (_allowPoliticalWarStart)
                {
                    return true;
                }

                // The player used vanilla's force-war power. Do not turn an
                // explicit god-power order into a diplomatic crisis or a nice
                // little concession agreement. They clicked WAR; give WAR.
                if (
                    IsForcedPlayerWarType(__args) ||
                    IsExplicitPlayerWarPowerCall()
                )
                {
                    return true;
                }

                // Native pause must also cover Harmony entry points, which can
                // be reached independently of Political World's Update loop.
                if (IsPoliticalSimulationPaused())
                {
                    return false;
                }

                Kingdom attacker;
                Kingdom defender;
                if (!TryExtractDirectWarKingdoms(
                    __args,
                    out attacker,
                    out defender
                ))
                {
                    return true;
                }

                if (attacker == null || defender == null || attacker == defender)
                {
                    return true;
                }

                // Civil/inspire wars created by Political World's internal
                // rebellion pipeline must stay immediate. Their political
                // preparation happens before the rebel kingdom is spawned.
                object warType =
                    __args != null && __args.Length >= 3
                        ? __args[2]
                        : null;
                if (IsInternalCivilWarType(warType))
                {
                    return true;
                }

                string pairKey = GetWarPairKey(attacker, defender);
                float runtimeCooldown;
                if (
                    WarPairNextRuntimeStartTime.TryGetValue(
                        pairKey,
                        out runtimeCooldown
                    ) &&
                    Time.time < runtimeCooldown
                )
                {
                    return false;
                }

                if (
                    PendingWarDeclarations.ContainsKey(pairKey) ||
                    PendingDiplomaticCrises.ContainsKey(pairKey)
                )
                {
                    return false;
                }

                int worldYear = GetWorldYearSafe();
                int truceUntil = GetPairTruceUntilYear(
                    attacker,
                    defender
                );
                if (truceUntil > worldYear)
                {
                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_war_blocked_by_truce"),
                            GetWorldObjectDisplayName(attacker),
                            GetWorldObjectDisplayName(defender),
                            truceUntil
                        ),
                        attacker,
                        null,
                        null,
                        DiplomatIconPath,
                        "war_truce_blocked_" + pairKey,
                        45f
                    );
                    return false;
                }

                if (HandleInternationalBlocInternalWarAttempt(
                    attacker,
                    defender
                ))
                {
                    WarPairNextRuntimeStartTime[pairKey] = Time.time + 15f;
                    return false;
                }

                if (ShouldIdeologyBlockAggressiveWar(attacker))
                {
                    WarPairNextRuntimeStartTime[pairKey] = Time.time + 15f;
                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_war_blocked_by_pacifism"),
                            GetWorldObjectDisplayName(attacker),
                            GetWorldObjectDisplayName(defender)
                        ),
                        attacker,
                        null,
                        GetLivingRuler(attacker),
                        DiplomatIconPath,
                        "war_pacifism_blocked_" + pairKey,
                        45f
                    );
                    return false;
                }

                string casusBelli = DetermineBasicCasusBelli(
                    attacker,
                    defender
                );

                if (ShouldLaunchSurpriseAttack(attacker, defender, casusBelli))
                {
                    PendingWarDeclaration surprise = new PendingWarDeclaration();
                    surprise.Attacker = attacker;
                    surprise.Defender = defender;
                    surprise.Diplomacy = __instance;
                    surprise.StartMethod =
                        (__originalMethod as MethodInfo) ??
                        _patchedWarStartMethod;
                    surprise.Args = CloneObjectArray(__args);
                    surprise.ExecuteAt = Time.time + 0.65f;
                    surprise.CasusBelli = casusBelli;
                    surprise.PairKey = pairKey;
                    surprise.DeclaredYear = worldYear;
                    surprise.SurpriseAttack = true;
                    PendingWarDeclarations[pairKey] = surprise;

                    ChangeDiplomaticReputation(attacker, -12);
                    SetKingdomIntData(attacker, WarPreparationStateDataKey, 2);
                    SetKingdomStringData(
                        attacker,
                        WarPreparationTargetNameDataKey,
                        GetWorldObjectDisplayName(defender)
                    );
                    SetKingdomStringData(
                        attacker,
                        WarPreparationCasusBelliDataKey,
                        casusBelli
                    );
                    SetKingdomIntData(
                        attacker,
                        WarLastDeclarationYearDataKey,
                        worldYear
                    );

                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_surprise_attack"),
                            GetWorldObjectDisplayName(attacker),
                            GetWorldObjectDisplayName(defender)
                        ),
                        attacker,
                        null,
                        GetLivingRuler(attacker),
                        MilitaristIconPath,
                        "war_surprise_" + pairKey,
                        20f
                    );
                    return false;
                }

                PendingDiplomaticCrisis crisis = new PendingDiplomaticCrisis();
                crisis.Attacker = attacker;
                crisis.Defender = defender;
                crisis.Diplomacy = __instance;
                crisis.StartMethod =
                    (__originalMethod as MethodInfo) ??
                    _patchedWarStartMethod;
                crisis.Args = CloneObjectArray(__args);
                crisis.PairKey = pairKey;
                crisis.CasusBelli = casusBelli;
                crisis.Demand = DetermineDiplomaticDemand(casusBelli);
                crisis.Stage = 1;
                crisis.Tension = CalculateDiplomaticCrisisTension(
                    attacker,
                    defender,
                    casusBelli
                );
                crisis.StartedYear = worldYear;
                crisis.StageEndsAt = Time.time + DiplomaticCrisisOpeningSeconds;
                PendingDiplomaticCrises[pairKey] = crisis;

                SetDiplomaticCrisisMirror(crisis, 1);
                WarPairNextRuntimeStartTime[pairKey] =
                    Time.time + DiplomaticCrisisRuntimeCooldownSeconds;

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_diplomatic_crisis_started"),
                        GetWorldObjectDisplayName(attacker),
                        GetWorldObjectDisplayName(defender),
                        GetDiplomaticDemandName(crisis.Demand)
                    ),
                    attacker,
                    null,
                    GetLivingRuler(attacker),
                    DiplomatIconPath,
                    "diplomatic_crisis_" + pairKey,
                    20f
                );

                return false;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "War declaration interception failed safely: " +
                    exception.Message
                );
                return true;
            }
        }

        private static void UpdatePendingDiplomaticCrises()
        {
            if (PendingDiplomaticCrises.Count > 0)
            {
                List<string> ready = new List<string>();
                foreach (
                    KeyValuePair<string, PendingDiplomaticCrisis> pair
                    in PendingDiplomaticCrises
                )
                {
                    PendingDiplomaticCrisis crisis = pair.Value;
                    if (crisis == null || Time.time >= crisis.StageEndsAt)
                    {
                        ready.Add(pair.Key);
                    }
                }

                for (int i = 0; i < ready.Count; i++)
                {
                    string pairKey = ready[i];
                    PendingDiplomaticCrisis crisis;
                    if (!PendingDiplomaticCrises.TryGetValue(pairKey, out crisis))
                    {
                        continue;
                    }

                    if (
                        crisis == null ||
                        crisis.Attacker == null ||
                        crisis.Defender == null ||
                        crisis.StartMethod == null ||
                        crisis.Diplomacy == null
                    )
                    {
                        PendingDiplomaticCrises.Remove(pairKey);
                        ClearDiplomaticCrisisData(crisis == null ? null : crisis.Attacker);
                        ClearDiplomaticCrisisData(crisis == null ? null : crisis.Defender);
                        continue;
                    }

                    List<Kingdom> kingdoms = GetKingdomsSafe();
                    if (
                        !kingdoms.Contains(crisis.Attacker) ||
                        !kingdoms.Contains(crisis.Defender)
                    )
                    {
                        PendingDiplomaticCrises.Remove(pairKey);
                        ClearDiplomaticCrisisData(crisis.Attacker);
                        ClearDiplomaticCrisisData(crisis.Defender);
                        continue;
                    }

                    if (crisis.Stage <= 1)
                    {
                        crisis.Stage = 2;
                        crisis.StageEndsAt = Time.time + DiplomaticUltimatumSeconds;
                        crisis.Tension = ClampInt(crisis.Tension + 12, 0, 100);
                        PendingDiplomaticCrises[pairKey] = crisis;
                        SetDiplomaticCrisisMirror(crisis, 2);

                        PublishPoliticalEvent(
                            string.Format(
                                LM.Get("ukiol_event_diplomatic_ultimatum"),
                                GetWorldObjectDisplayName(crisis.Attacker),
                                GetWorldObjectDisplayName(crisis.Defender),
                                GetDiplomaticDemandName(crisis.Demand)
                            ),
                            crisis.Attacker,
                            null,
                            GetLivingRuler(crisis.Attacker),
                            DiplomatIconPath,
                            "diplomatic_ultimatum_" + pairKey,
                            20f
                        );
                        continue;
                    }

                    if (ShouldDefenderAcceptDiplomaticDemand(crisis))
                    {
                        PendingDiplomaticCrises.Remove(pairKey);
                        ApplyDiplomaticSettlement(crisis);
                        continue;
                    }

                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_diplomatic_refused"),
                            GetWorldObjectDisplayName(crisis.Defender),
                            GetWorldObjectDisplayName(crisis.Attacker)
                        ),
                        crisis.Defender,
                        null,
                        GetLivingRuler(crisis.Defender),
                        DiplomatIconPath,
                        "diplomatic_refused_" + pairKey,
                        20f
                    );

                    // Intended declaration flow: once the defender rejects
                    // the ultimatum, the diplomatic crisis escalates into war.
                    // Do not roll a second attacker back-down chance here: it
                    // made an explicit declaration sometimes disappear after a
                    // visible refusal, which looked like a broken war tool.
                    PendingDiplomaticCrises.Remove(pairKey);
                    ScheduleWarFromDiplomaticCrisis(crisis);
                }
            }

            CleanupStaleDiplomaticCrisisData();
        }

        private static void ScheduleWarFromDiplomaticCrisis(
            PendingDiplomaticCrisis crisis
        )
        {
            if (crisis == null)
            {
                return;
            }

            PendingWarDeclaration pending = new PendingWarDeclaration();
            pending.Attacker = crisis.Attacker;
            pending.Defender = crisis.Defender;
            pending.Diplomacy = crisis.Diplomacy;
            pending.StartMethod = crisis.StartMethod;
            pending.Args = CloneObjectArray(crisis.Args);
            pending.ExecuteAt =
                Time.time + GetWarPreparationSeconds(crisis.Attacker);
            pending.CasusBelli = crisis.CasusBelli;
            pending.PairKey = crisis.PairKey;
            pending.DeclaredYear = GetWorldYearSafe();
            pending.FromDiplomaticCrisis = true;
            PendingWarDeclarations[crisis.PairKey] = pending;

            SetDiplomaticCrisisMirror(crisis, 3);
            SetKingdomIntData(crisis.Attacker, WarPreparationStateDataKey, 1);
            SetKingdomStringData(
                crisis.Attacker,
                WarPreparationTargetNameDataKey,
                GetWorldObjectDisplayName(crisis.Defender)
            );
            SetKingdomStringData(
                crisis.Attacker,
                WarPreparationCasusBelliDataKey,
                crisis.CasusBelli
            );
            SetKingdomIntData(
                crisis.Attacker,
                WarLastDeclarationYearDataKey,
                GetWorldYearSafe()
            );

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_diplomatic_escalation"),
                    GetWorldObjectDisplayName(crisis.Attacker),
                    GetWorldObjectDisplayName(crisis.Defender),
                    GetCasusBelliName(crisis.CasusBelli)
                ),
                crisis.Attacker,
                null,
                GetLivingRuler(crisis.Attacker),
                MilitaristIconPath,
                "diplomatic_escalation_" + crisis.PairKey,
                20f,
                new List<string>
                {
                    string.Format(
                        LM.Get(
                            "ukiol_chronicle_detail_war_rejected_demand"
                        ),
                        GetDiplomaticDemandName(crisis.Demand)
                    ),
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_war_casus_belli"),
                        GetCasusBelliName(crisis.CasusBelli)
                    )
                },
                new List<string>
                {
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_war_preparation"),
                        GetWorldObjectDisplayName(crisis.Defender)
                    )
                }
            );
        }

        private static void ApplyDiplomaticSettlement(
            PendingDiplomaticCrisis crisis
        )
        {
            if (
                crisis == null ||
                crisis.Attacker == null ||
                crisis.Defender == null
            )
            {
                return;
            }

            int attackerGain = 2;
            int defenderLoss = 2;
            if (crisis.Demand == "border_guarantees")
            {
                attackerGain = 3;
                defenderLoss = 4;
            }
            else if (crisis.Demand == "strategic_withdrawal")
            {
                attackerGain = 3;
                defenderLoss = 3;
                AddWarExhaustion(crisis.Defender, 2);
            }
            else if (crisis.Demand == "economic_compensation")
            {
                attackerGain = 2;
                defenderLoss = 2;
                ApplyDiplomaticEconomicCompensation(
                    crisis.Attacker,
                    crisis.Defender
                );
            }
            else if (crisis.Demand == "political_guarantees")
            {
                ChangeDiplomaticReputation(crisis.Defender, 2);
            }

            SetNationalStability(
                crisis.Attacker,
                GetNationalStability(crisis.Attacker) + attackerGain
            );
            SetNationalStability(
                crisis.Defender,
                GetNationalStability(crisis.Defender) - defenderLoss
            );
            ChangeDiplomaticReputation(crisis.Attacker, 2);

            int untilYear =
                GetWorldYearSafe() + DiplomaticSettlementTruceYears;
            SetPairTruceUntilYear(
                crisis.Attacker,
                crisis.Defender,
                untilYear
            );
            SetPairTruceUntilYear(
                crisis.Defender,
                crisis.Attacker,
                untilYear
            );
            SetKingdomIntData(
                crisis.Attacker,
                WarLatestTruceUntilYearDataKey,
                untilYear
            );
            SetKingdomIntData(
                crisis.Defender,
                WarLatestTruceUntilYearDataKey,
                untilYear
            );

            ClearDiplomaticCrisisData(crisis.Attacker);
            ClearDiplomaticCrisisData(crisis.Defender);
            WarPairNextRuntimeStartTime[crisis.PairKey] =
                Time.time + DiplomaticCrisisRuntimeCooldownSeconds;

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_diplomatic_accepted"),
                    GetWorldObjectDisplayName(crisis.Defender),
                    GetWorldObjectDisplayName(crisis.Attacker),
                    GetDiplomaticDemandName(crisis.Demand),
                    untilYear
                ),
                crisis.Defender,
                null,
                GetLivingRuler(crisis.Defender),
                DiplomatIconPath,
                "diplomatic_accepted_" + crisis.PairKey,
                25f
            );
        }

        private static void ApplyDiplomaticEconomicCompensation(
            Kingdom receiver,
            Kingdom payer
        )
        {
            List<City> receiverCities = GetCitiesSafe(receiver);
            List<City> payerCities = GetCitiesSafe(payer);
            int transfers = Math.Min(
                4,
                Math.Min(receiverCities.Count, payerCities.Count)
            );
            for (int i = 0; i < transfers; i++)
            {
                TryChangeCityResource(payerCities[i], "gold", -1);
                TryChangeCityResource(receiverCities[i], "gold", 1);
            }
        }

        private static bool ShouldDefenderAcceptDiplomaticDemand(
            PendingDiplomaticCrisis crisis
        )
        {
            if (
                crisis == null ||
                crisis.Attacker == null ||
                crisis.Defender == null
            )
            {
                return false;
            }

            int attackerPower = CalculateDiplomaticPower(crisis.Attacker);
            int defenderPower = CalculateDiplomaticPower(crisis.Defender);
            IdeologyBehaviorProfile defenderBehavior =
                GetIdeologyBehaviorProfile(crisis.Defender);
            int defenderExhaustion = ClampInt(
                GetKingdomIntData(
                    crisis.Defender,
                    WarExhaustionDataKey,
                    0
                ),
                0,
                100
            );
            int defenderStability = GetNationalStability(crisis.Defender);

            int chance = 22;
            chance += ClampInt(
                (attackerPower - defenderPower) / 25,
                -24,
                34
            );
            chance += defenderExhaustion / 3;
            chance += Math.Max(0, 50 - defenderStability) / 2;
            chance += crisis.Tension / 6;
            chance -= defenderBehavior.Militarism / 5;
            if (GetKingdomCourse(crisis.Defender) == DiplomatTraitId)
            {
                chance += 12;
            }
            if (GetKingdomCourse(crisis.Defender) == MilitaristTraitId)
            {
                chance -= 12;
            }

            chance = ClampInt(chance, 6, 88);
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        private static bool ShouldAttackerBackDownAfterRefusal(
            PendingDiplomaticCrisis crisis
        )
        {
            if (crisis == null || crisis.Attacker == null)
            {
                return true;
            }

            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(crisis.Attacker);
            int reform = GetKingdomIntData(
                crisis.Attacker,
                IdeologyReformPressureDataKey,
                0
            );
            int radical = GetKingdomIntData(
                crisis.Attacker,
                IdeologyRadicalizationPressureDataKey,
                0
            );

            int chance = 18;
            chance += behavior.Pluralism / 3;
            chance += reform / 5;
            chance -= behavior.Militarism / 3;
            chance -= radical / 5;
            chance -= crisis.Tension / 8;
            if (GetKingdomCourse(crisis.Attacker) == DiplomatTraitId)
            {
                chance += 20;
            }
            if (GetKingdomCourse(crisis.Attacker) == MilitaristTraitId)
            {
                chance -= 18;
            }

            chance = ClampInt(chance, 4, 70);
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        private static bool ShouldLaunchSurpriseAttack(
            Kingdom attacker,
            Kingdom defender,
            string casusBelli
        )
        {
            if (attacker == null || defender == null)
            {
                return false;
            }

            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(attacker);
            int radical = GetKingdomIntData(
                attacker,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            int exhaustion = ClampInt(
                GetKingdomIntData(attacker, WarExhaustionDataKey, 0),
                0,
                100
            );
            if (exhaustion >= 70)
            {
                return false;
            }

            int chance = 3;
            chance += behavior.Militarism / 5;
            chance += radical / 8;
            chance -= behavior.Pluralism / 10;
            if (casusBelli == "conquest" || casusBelli == "expansion")
            {
                chance += 5;
            }
            string course = GetKingdomCourse(attacker);
            if (course == MilitaristTraitId)
            {
                chance += 8;
            }
            else if (course == DiplomatTraitId)
            {
                chance -= 12;
            }

            chance = ClampInt(chance, 1, 32);
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        private static int CalculateDiplomaticPower(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 0;
            }

            int cities = GetCitiesSafe(kingdom).Count;
            int population = GetKingdomPopulationSafe(kingdom);
            int stability = GetNationalStability(kingdom);
            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(kingdom);

            long score = 40L;
            score += cities * 70L;
            score += Math.Min(5000, population) / 8L;
            score += stability * 4L;
            score += behavior.Militarism * 2L;
            if (GetKingdomCourse(kingdom) == MilitaristTraitId)
            {
                score += 90L;
            }

            return ClampInt(
                score > int.MaxValue ? int.MaxValue : (int)score,
                0,
                int.MaxValue
            );
        }

        private static int CalculateDiplomaticCrisisTension(
            Kingdom attacker,
            Kingdom defender,
            string casusBelli
        )
        {
            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(attacker);
            int radical = GetKingdomIntData(
                attacker,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            int score = 22;
            score += behavior.Militarism / 3;
            score += radical / 4;
            score += Math.Max(0, 50 - behavior.Pluralism) / 4;
            if (casusBelli == "conquest") score += 14;
            else if (casusBelli == "expansion") score += 10;
            else if (casusBelli == "ideological_confrontation") score += 8;

            int powerDifference =
                CalculateDiplomaticPower(attacker) -
                CalculateDiplomaticPower(defender);
            if (powerDifference > 0)
            {
                score += Math.Min(12, powerDifference / 60);
            }

            return ClampInt(score, 15, 92);
        }

        private static string DetermineDiplomaticDemand(string casusBelli)
        {
            if (casusBelli == "conquest")
            {
                return "border_guarantees";
            }
            if (casusBelli == "expansion")
            {
                return "strategic_withdrawal";
            }
            if (casusBelli == "ideological_confrontation")
            {
                return "political_guarantees";
            }
            return "economic_compensation";
        }

        private static string GetDiplomaticDemandName(string demand)
        {
            string key = "ukiol_diplomatic_demand_" +
                (string.IsNullOrEmpty(demand)
                    ? "political_guarantees"
                    : demand);
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key
                ? demand
                : value;
        }

        private static string GetDiplomaticCrisisStageName(int stage)
        {
            string key = "ukiol_diplomatic_stage_" +
                ClampInt(stage, 1, 3).ToString();
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key
                ? stage.ToString()
                : value;
        }

        private static void SetDiplomaticCrisisMirror(
            PendingDiplomaticCrisis crisis,
            int stage
        )
        {
            if (
                crisis == null ||
                crisis.Attacker == null ||
                crisis.Defender == null
            )
            {
                return;
            }

            SetDiplomaticCrisisDataForKingdom(
                crisis.Attacker,
                crisis.Defender,
                crisis.Demand,
                stage,
                crisis.Tension,
                1
            );
            SetDiplomaticCrisisDataForKingdom(
                crisis.Defender,
                crisis.Attacker,
                crisis.Demand,
                stage,
                crisis.Tension,
                2
            );
        }

        private static void SetDiplomaticCrisisDataForKingdom(
            Kingdom kingdom,
            Kingdom counterpart,
            string demand,
            int stage,
            int tension,
            int role
        )
        {
            if (kingdom == null)
            {
                return;
            }

            SetKingdomIntData(kingdom, DiplomaticCrisisActiveDataKey, 1);
            SetKingdomStringData(
                kingdom,
                DiplomaticCrisisCounterpartDataKey,
                GetWorldObjectDisplayName(counterpart)
            );
            SetKingdomStringData(
                kingdom,
                DiplomaticCrisisDemandDataKey,
                demand ?? "political_guarantees"
            );
            SetKingdomIntData(
                kingdom,
                DiplomaticCrisisStageDataKey,
                ClampInt(stage, 1, 3)
            );
            SetKingdomIntData(
                kingdom,
                DiplomaticCrisisTensionDataKey,
                ClampInt(tension, 0, 100)
            );
            SetKingdomIntData(
                kingdom,
                DiplomaticCrisisRoleDataKey,
                role
            );
        }

        private static void ClearDiplomaticCrisisData(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return;
            }
            SetKingdomIntData(kingdom, DiplomaticCrisisActiveDataKey, 0);
            SetKingdomStringData(kingdom, DiplomaticCrisisCounterpartDataKey, "");
            SetKingdomStringData(kingdom, DiplomaticCrisisDemandDataKey, "");
            SetKingdomIntData(kingdom, DiplomaticCrisisStageDataKey, 0);
            SetKingdomIntData(kingdom, DiplomaticCrisisTensionDataKey, 0);
            SetKingdomIntData(kingdom, DiplomaticCrisisRoleDataKey, 0);
        }

        private static bool IsKingdomInActiveDiplomaticSequence(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return false;
            }

            foreach (
                KeyValuePair<string, PendingDiplomaticCrisis> pair
                in PendingDiplomaticCrises
            )
            {
                PendingDiplomaticCrisis crisis = pair.Value;
                if (
                    crisis != null &&
                    (crisis.Attacker == kingdom || crisis.Defender == kingdom)
                )
                {
                    return true;
                }
            }

            foreach (
                KeyValuePair<string, PendingWarDeclaration> pair
                in PendingWarDeclarations
            )
            {
                PendingWarDeclaration war = pair.Value;
                if (
                    war != null &&
                    war.FromDiplomaticCrisis &&
                    (war.Attacker == kingdom || war.Defender == kingdom)
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static void CleanupStaleDiplomaticCrisisData()
        {
            if (Time.time < _nextDiplomaticCrisisCleanupTime)
            {
                return;
            }
            _nextDiplomaticCrisisCleanupTime = Time.time + 2f;

            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (
                    kingdom != null &&
                    GetKingdomIntData(
                        kingdom,
                        DiplomaticCrisisActiveDataKey,
                        0
                    ) != 0 &&
                    !IsKingdomInActiveDiplomaticSequence(kingdom)
                )
                {
                    ClearDiplomaticCrisisData(kingdom);
                }
            }
        }

        private static int GetDiplomaticReputation(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 50;
            }

            int stored = GetKingdomIntData(
                kingdom,
                DiplomaticReputationDataKey,
                50
            );
            return ClampInt(stored, 0, 100);
        }

        private static void ChangeDiplomaticReputation(
            Kingdom kingdom,
            int delta
        )
        {
            if (kingdom == null)
            {
                return;
            }
            SetKingdomIntData(
                kingdom,
                DiplomaticReputationDataKey,
                ClampInt(GetDiplomaticReputation(kingdom) + delta, 0, 100)
            );
        }

        private static Color GetDiplomaticReputationColor(int reputation)
        {
            int value = ClampInt(reputation, 0, 100);
            if (value >= 70)
            {
                return new Color(0.48f, 0.88f, 0.66f, 1f);
            }
            if (value >= 40)
            {
                return new Color(0.72f, 0.83f, 0.93f, 1f);
            }
            return new Color(0.95f, 0.46f, 0.39f, 1f);
        }
    }
}
