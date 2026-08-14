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
    public partial class Main : BasicMod<Main>
    {
        private static void InstallSafePatches()
        {
            try
            {
                _harmony = new Harmony(
                    "fluttershy.politicalworld.politics.v1392"
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Harmony initialization failed: " +
                    exception.Message
                );

                return;
            }

            _armyLimitPatchInstalled = TryInstallPostfix(
                typeof(City),
                new string[]
                {
                    // WorldBox 0.51.2 build 719: this is the actual final
                    // multiplier used by City army capacity. Keep the older
                    // names below as compatibility fallbacks for old builds.
                    "getArmyMaxMultiplier",
                    "getArmyMaxTotalPercentage",
                    "getArmyMaxPercentage",
                    "getArmyMaxTotalPercent",
                    "getArmyLimitPercentage",
                    "getArmyLimitPercent"
                },
                nameof(ArmyLimitPostfix)
            );

            if (!_armyLimitPatchInstalled)
            {
                LogArmyPatchCandidates();
            }

            _soldierStatsPatchInstalled = TryInstallPostfix(
                typeof(Actor),
                new string[]
                {
                    "updateStats"
                },
                nameof(SoldierStatsPostfix)
            );

            _kingdomWindowPatchInstalled = TryInstallPostfix(
                typeof(KingdomWindow),
                new string[]
                {
                    "showStatsRows"
                },
                nameof(KingdomStatsRowsPostfix)
            );

            _cityWindowPatchInstalled = TryInstallPostfix(
                typeof(CityWindow),
                new string[]
                {
                    "showStatsRows"
                },
                nameof(CityStatsRowsPostfix)
            );

            _loyaltyBridgePatchInstalled =
                TryInstallPostfixWithParameterCount(
                    typeof(City),
                    new string[]
                    {
                        "getLoyalty"
                    },
                    1,
                    nameof(CityLoyaltyPostfix)
                );

            InstallPoliticalMapPatches();
            InstallPoliticalMapNameplateTextPatch();

            LogInfo(
                "Politics patches: army limit=" +
                _armyLimitPatchInstalled +
                ", soldier stats=" +
                _soldierStatsPatchInstalled +
                ", kingdom window=" +
                _kingdomWindowPatchInstalled +
                ", city window=" +
                _cityWindowPatchInstalled +
                ", loyalty bridge=" +
                _loyaltyBridgePatchInstalled +
                ", political map meta=" +
                _politicalMapMetaStringPatchInstalled +
                ", political map zones=" +
                _politicalMapBorderModePatchInstalled +
                ", political map labels=" +
                _politicalMapNameplateTextPatchInstalled
            );
        }

        private static void LogArmyPatchCandidates()
        {
            try
            {
                MethodInfo[] methods = typeof(City).GetMethods(MemberFlags);
                int logged = 0;

                for (int i = 0; i < methods.Length && logged < 24; i++)
                {
                    MethodInfo method = methods[i];

                    if (
                        method == null ||
                        method.Name.IndexOf(
                            "army",
                            StringComparison.OrdinalIgnoreCase
                        ) < 0
                    )
                    {
                        continue;
                    }

                    LogInfo(
                        "Army patch candidate: City." +
                        method.Name +
                        " params=" +
                        method.GetParameters().Length +
                        " returns=" +
                        method.ReturnType.Name
                    );
                    logged++;
                }

                if (logged == 0)
                {
                    LogInfo(
                        "Army patch candidate scan: no City methods containing 'army'."
                    );
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Army patch candidate scan failed: " +
                    exception.Message
                );
            }
        }

        private static bool TryInstallPostfix(
            Type targetType,
            string[] targetMethodNames,
            string postfixMethodName
        )
        {
            if (_harmony == null || targetType == null)
            {
                return false;
            }

            MethodInfo postfix = typeof(Main).GetMethod(
                postfixMethodName,
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

            if (postfix == null)
            {
                LogWarning(
                    "Postfix method not found: " +
                    postfixMethodName
                );

                return false;
            }

            MethodInfo target = null;
            MethodInfo[] methods = targetType.GetMethods(MemberFlags);

            for (int nameIndex = 0;
                nameIndex < targetMethodNames.Length;
                nameIndex++)
            {
                string targetName =
                    targetMethodNames[nameIndex];

                for (int methodIndex = 0;
                    methodIndex < methods.Length;
                    methodIndex++)
                {
                    MethodInfo candidate = methods[methodIndex];

                    if (
                        candidate.Name == targetName &&
                        candidate.GetParameters().Length == 0
                    )
                    {
                        target = candidate;
                        break;
                    }
                }

                if (target != null)
                {
                    break;
                }
            }

            if (target == null)
            {
                LogWarning(
                    "Target method was not found on " +
                    targetType.Name +
                    " for postfix " +
                    postfixMethodName
                );

                return false;
            }

            try
            {
                _harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(postfix)
                );

                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not patch " +
                    targetType.Name +
                    "." +
                    target.Name +
                    ": " +
                    exception.Message
                );

                return false;
            }
        }

        private static bool TryInstallPostfixWithParameterCount(
            Type targetType,
            string[] targetMethodNames,
            int parameterCount,
            string postfixMethodName
        )
        {
            if (_harmony == null || targetType == null)
            {
                return false;
            }

            MethodInfo postfix = typeof(Main).GetMethod(
                postfixMethodName,
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

            if (postfix == null)
            {
                return false;
            }

            MethodInfo target = null;
            MethodInfo[] methods = targetType.GetMethods(MemberFlags);

            for (int nameIndex = 0;
                nameIndex < targetMethodNames.Length;
                nameIndex++)
            {
                string targetName = targetMethodNames[nameIndex];

                for (int methodIndex = 0;
                    methodIndex < methods.Length;
                    methodIndex++)
                {
                    MethodInfo candidate = methods[methodIndex];

                    if (
                        candidate.Name == targetName &&
                        candidate.GetParameters().Length == parameterCount
                    )
                    {
                        target = candidate;
                        break;
                    }
                }

                if (target != null)
                {
                    break;
                }
            }

            if (target == null)
            {
                LogWarning(
                    "Target method was not found on " +
                    targetType.Name +
                    " for postfix " +
                    postfixMethodName
                );

                return false;
            }

            try
            {
                _harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(postfix)
                );

                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not patch " +
                    targetType.Name +
                    "." +
                    target.Name +
                    ": " +
                    exception.Message
                );

                return false;
            }
        }

        private static void ArmyLimitPostfix(
            City __instance,
            ref float __result
        )
        {
            try
            {
                Kingdom kingdom = GetKingdomFromObject(
                    __instance
                );

                string course = GetKingdomCourse(kingdom);

                if (course == MilitaristTraitId)
                {
                    __result += MilitaristArmyLimitBonus;
                }
                else if (course == DiplomatTraitId)
                {
                    __result -= DiplomatArmyLimitPenalty;

                    if (__result < 0.05f)
                    {
                        __result = 0.05f;
                    }
                }
                else if (course == ReformerTraitId)
                {
                    __result += ReformerArmyLimitBonus;
                }

                if (kingdom != null)
                {
                    __result += GetCachedArmyLimitPoliticalDelta(kingdom);

                    if (__result < 0.05f) __result = 0.05f;
                }
            }
            catch
            {
                // Политический бонус не должен ломать
                // основной расчёт армии WorldBox.
            }
        }

        private static float GetCachedArmyLimitPoliticalDelta(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return 0f;
            }

            float until;
            float cached;
            if (
                CachedArmyLimitPoliticalDeltaUntil.TryGetValue(kingdom, out until) &&
                Time.unscaledTime < until &&
                CachedArmyLimitPoliticalDelta.TryGetValue(kingdom, out cached)
            )
            {
                return cached;
            }

            float delta = 0f;
            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(kingdom);
            string ideology = GetStateIdeology(kingdom);
            int support = IsValidIdeology(ideology)
                ? GetKingdomIdeologySupport(kingdom, ideology)
                : 50;
            float behaviorStrength =
                Math.Max(0f, Math.Min(1f, support / 100f));
            delta +=
                (behavior.Militarism - 50) * 0.0015f * behaviorStrength;

            string blocType = GetKingdomStringData(
                kingdom,
                InternationalBlocTypeDataKey,
                ""
            );
            int blocIntegration = GetKingdomIntData(
                kingdom,
                InternationalBlocIntegrationDataKey,
                0
            );
            if (blocIntegration >= 30)
            {
                if (blocType == "defensive")
                {
                    delta += 0.02f;
                }
                else if (blocType == "military_political")
                {
                    delta += 0.04f;
                }
            }

            CachedArmyLimitPoliticalDelta[kingdom] = delta;
            CachedArmyLimitPoliticalDeltaUntil[kingdom] =
                Time.unscaledTime + ArmyLimitModifierCacheSeconds;
            return delta;
        }

        private static void SoldierStatsPostfix(
            Actor __instance
        )
        {
            try
            {
                Actor actor = __instance;

                if (
                    actor == null ||
                    !actor.isAlive() ||
                    !actor.hasArmy() ||
                    actor.stats == null
                )
                {
                    return;
                }

                Kingdom kingdom = GetKingdomFromObject(actor);
                string course = GetKingdomCourse(kingdom);

                if (course != MilitaristTraitId)
                {
                    return;
                }

                actor.stats[S.damage] +=
                    MilitaristSoldierDamageBonus;

                actor.stats[S.speed] +=
                    MilitaristSoldierSpeedBonus;

                actor.stats[S.armor] +=
                    MilitaristSoldierArmorBonus;

                actor.stats.normalize();
            }
            catch
            {
                // Ошибка дополнительного бонуса не должна
                // прерывать стандартный пересчёт характеристик.
            }
        }
    }
}
