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
        private static void InstallPoliticalMapPatches()
        {
            if (_harmony == null)
            {
                return;
            }

            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(MetaTypeExtensions),
                    nameof(MetaTypeExtensions.AsString)
                );
                MethodInfo prefix = typeof(Main).GetMethod(
                    nameof(PoliticalMapMetaTypeAsStringPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (target != null && prefix != null)
                {
                    _harmony.Patch(
                        target,
                        prefix: new HarmonyMethod(prefix)
                    );
                    _politicalMapMetaStringPatchInstalled = true;
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not patch MetaTypeExtensions.AsString for political map: " +
                    exception.Message
                );
            }

            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(Zones),
                    nameof(Zones.getCurrentMapBorderMode)
                );
                MethodInfo prefix = typeof(Main).GetMethod(
                    nameof(PoliticalMapBorderModePrefix),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (target != null && prefix != null)
                {
                    _harmony.Patch(
                        target,
                        prefix: new HarmonyMethod(prefix)
                    );
                    _politicalMapBorderModePatchInstalled = true;
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not patch Zones.getCurrentMapBorderMode for political map: " +
                    exception.Message
                );
            }
        }

        // v1.6.0-dev7.3.1: the zone layer already knows the correct
        // party/ideology/tension, but the vanilla City nameplate asset only
        // renders the settlement/kingdom name. Patch NameplateText.setText
        // instead of replacing the whole nameplate system: this keeps
        // vanilla positioning, overlap handling and automatic width
        // calculation, while adding the active political value to the text.
        private static void InstallPoliticalMapNameplateTextPatch()
        {
            if (_harmony == null)
            {
                return;
            }

            try
            {
                MethodInfo target = null;
                MethodInfo[] methods =
                    typeof(NameplateText).GetMethods(MemberFlags);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo candidate = methods[i];
                    if (
                        candidate != null &&
                        candidate.Name == "setText" &&
                        candidate.GetParameters().Length == 3
                    )
                    {
                        ParameterInfo[] parameters =
                            candidate.GetParameters();

                        if (
                            parameters.Length > 0 &&
                            parameters[0].ParameterType == typeof(string)
                        )
                        {
                            target = candidate;
                            break;
                        }
                    }
                }

                MethodInfo prefix = typeof(Main).GetMethod(
                    nameof(PoliticalMapNameplateSetTextPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (target == null || prefix == null)
                {
                    LogWarning(
                        "Political Layer nameplate patch target was not found."
                    );
                    return;
                }

                _harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(prefix)
                );
                _politicalMapNameplateTextPatchInstalled = true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not patch Political Layer nameplate text: " +
                    exception.Message
                );
            }
        }

        private static void PoliticalMapNameplateSetTextPrefix(
            NameplateText __instance,
            ref string __0
        )
        {
            try
            {
                if (
                    __instance == null ||
                    string.IsNullOrEmpty(__0) ||
                    !IsPoliticalMapLayerActive(false)
                )
                {
                    return;
                }

                object nanoObject = GetMemberValue(
                    __instance,
                    "nano_object",
                    "_nano_object"
                );

                City city = nanoObject as City;

                if (city == null)
                {
                    Kingdom kingdom = nanoObject as Kingdom;
                    if (kingdom != null)
                    {
                        city = GetPoliticalMapRepresentativeCity(
                            kingdom
                        );
                    }
                }

                if (
                    city == null ||
                    city.data == null ||
                    city.isRekt()
                )
                {
                    return;
                }

                int mode = GetPoliticalMapMode();
                PoliticalMapMetaObject political =
                    GetPoliticalMapCityVisual(city, mode);

                if (
                    political == null ||
                    political.data == null ||
                    string.IsNullOrWhiteSpace(political.data.name)
                )
                {
                    return;
                }

                string politicalText =
                    political.data.name.Trim();

                // setText() recalculates the vanilla background width after
                // this prefix runs, so a single-line suffix is safer and
                // cleaner than manually resizing or adding a second UI text.
                string suffix = " \u2022 " + politicalText;

                if (
                    __0.EndsWith(
                        suffix,
                        StringComparison.Ordinal
                    )
                )
                {
                    return;
                }

                __0 += suffix;
            }
            catch
            {
                // Political labels are presentation-only and must never
                // interfere with vanilla nameplate rendering.
            }
        }

        private static City GetPoliticalMapRepresentativeCity(
            Kingdom pKingdom
        )
        {
            if (pKingdom == null || pKingdom.data == null)
            {
                return null;
            }

            City capital = GetMemberValue(
                pKingdom,
                "capital",
                "_capital"
            ) as City;

            if (
                capital != null &&
                capital.data != null &&
                !capital.isRekt()
            )
            {
                return capital;
            }

            List<City> cities = GetCitiesSafe(pKingdom);
            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (
                    city != null &&
                    city.data != null &&
                    !city.isRekt()
                )
                {
                    return city;
                }
            }

            return null;
        }

        private static bool PoliticalMapMetaTypeAsStringPrefix(
            MetaType pType,
            ref string __result
        )
        {
            if (pType != PoliticalMapMetaType)
            {
                return true;
            }

            __result = PoliticalMapMetaId;
            return false;
        }

        private static bool PoliticalMapBorderModePrefix(
            bool pCheckOnlyOption,
            ref MetaType __result
        )
        {
            if (!IsPoliticalMapLayerActive(pCheckOnlyOption))
            {
                return true;
            }

            __result = PoliticalMapMetaType;
            return false;
        }

        private static bool IsPoliticalMapLayerActive(bool pCheckOnlyOption)
        {
            try
            {
                if (_politicalMapMetaAsset != null)
                {
                    return _politicalMapMetaAsset.isActive(pCheckOnlyOption);
                }
            }
            catch
            {
            }

            try
            {
                PlayerOptionData data;
                return PlayerConfig.dict.TryGetValue(
                    PoliticalMapOptionId,
                    out data
                ) && data != null && data.boolVal;
            }
            catch
            {
                return false;
            }
        }

    }
}
