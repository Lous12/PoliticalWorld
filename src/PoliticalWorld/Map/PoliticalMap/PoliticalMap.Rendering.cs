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
        private static void CreatePoliticalMapLayer()
        {
            try
            {
                RegisterPoliticalMapOption();
                RegisterPoliticalMapPower();

                try
                {
                    AssetManager.powers.linkAssets();
                }
                catch
                {
                }

                try
                {
                    AssetManager.options_library.linkAssets();
                }
                catch
                {
                }

                RegisterPoliticalMapMetaAsset();

                try
                {
                    AssetManager.meta_type_library.linkAssets();
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "Could not link Political Layer MetaType asset: " +
                        exception.Message
                    );
                }

                RegisterPoliticalMapNameplate();
                InvalidatePoliticalMapVisualCache();
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create Political Layer: " +
                    exception.Message
                );
            }
        }

        private static void RegisterPoliticalMapOption()
        {
            string[] localeOptions = new string[]
            {
                "ukiol_political_map_mode_parties",
                "ukiol_political_map_mode_ideologies",
                "ukiol_political_map_mode_tension"
            };

            OptionAsset option = AssetManager.options_library.get(
                PoliticalMapOptionId
            );

            if (option == null)
            {
                option = AssetManager.options_library.add(
                    new OptionAsset
                    {
                        id = PoliticalMapOptionId,
                        default_bool = false,
                        default_int = PoliticalMapModeParties,
                        max_value = PoliticalMapModeTension,
                        multi_toggle = true,
                        type = OptionType.Bool,
                        locale_options_ids = localeOptions
                    }
                );
            }
            else
            {
                option.default_bool = false;
                option.default_int = PoliticalMapModeParties;
                option.max_value = PoliticalMapModeTension;
                option.multi_toggle = true;
                option.type = OptionType.Bool;
                option.locale_options_ids = localeOptions;
            }

            PlayerOptionData data;
            if (!PlayerConfig.dict.TryGetValue(
                    PoliticalMapOptionId,
                    out data
                ))
            {
                PlayerConfig.instance.data.add(
                    new PlayerOptionData(PoliticalMapOptionId)
                    {
                        boolVal = false,
                        intVal = PoliticalMapModeParties
                    }
                );
            }
            else if (data != null)
            {
                data.intVal = ClampInt(
                    data.intVal,
                    PoliticalMapModeParties,
                    PoliticalMapModeTension
                );
            }
        }

        private static void RegisterPoliticalMapPower()
        {
            GodPower power = AssetManager.powers.get(
                PoliticalMapPowerId
            );

            if (power == null)
            {
                power = AssetManager.powers.add(
                    new GodPower
                    {
                        id = PoliticalMapPowerId,
                        name = PoliticalMapPowerId,
                        path_icon = "ui/icons/iconMap"
                    }
                );
            }

            power.map_modes_switch = true;
            power.multi_toggle = true;
            power.toggle_name = PoliticalMapOptionId;
            power.force_map_mode = (MetaType)0;
            power.unselect_when_window = true;
            power.ignore_cursor_icon = true;
            power.allow_unit_selection = true;
            power.toggle_action = (PowerToggleAction)Delegate.Combine(
                new PowerToggleAction(
                    AssetManager.powers.toggleOptionZone
                ),
                new PowerToggleAction(
                    delegate(string pPowerId)
                    {
                        InvalidatePoliticalMapVisualCache();
                        SyncPoliticalMapToolbarModeIndicators(true);
                    }
                )
            );
        }

        private static void RegisterPoliticalMapMetaAsset()
        {
            MetaTypeAsset asset = AssetManager.meta_type_library.get(
                PoliticalMapMetaId
            );

            if (asset == null)
            {
                asset = new MetaTypeAsset();
                asset.id = PoliticalMapMetaId;
                asset = AssetManager.meta_type_library.add(asset);
            }

            asset.map_mode = PoliticalMapMetaType;
            asset.option_id = PoliticalMapOptionId;
            asset.power_option_zone_id = PoliticalMapPowerId;
            asset.icon_single_path = "ui/icons/iconMap";
            asset.force_zone_when_selected = false;
            asset.set_icon_for_cancel_button = false;

            // v1.6.0-dev7.1: a custom MetaTypeAsset is still expected by
            // WorldBox's ZoneCalculator to expose the normal selection
            // lifecycle delegates. dev7 only supplied drawing delegates, so
            // checkSelectedNanoObject() eventually invoked a null delegate.
            // Reuse the vanilla City meta lifecycle because this political
            // layer is settlement-based; only the visual/meta getters below
            // remain custom.
            MetaTypeAsset cityMeta = MetaTypeLibrary.city;
            if (cityMeta != null)
            {
                asset.window_name = cityMeta.window_name;
                asset.power_tab_id = cityMeta.power_tab_id;
                asset.icon_list = cityMeta.icon_list;
                asset.get_list = cityMeta.get_list;
                asset.get_sorted_list = cityMeta.get_sorted_list;
                asset.custom_sorted_list = cityMeta.custom_sorted_list;
                asset.has_any = cityMeta.has_any;
                asset.get_selected = cityMeta.get_selected;
                asset.set_selected = cityMeta.set_selected;
                asset.get = cityMeta.get;
                asset.window_action_clear = cityMeta.window_action_clear;
                asset.window_history_action_update =
                    cityMeta.window_history_action_update;
                asset.window_history_action_restore =
                    cityMeta.window_history_action_restore;
                asset.selected_tab_action = cityMeta.selected_tab_action;
                asset.selected_tab_action_meta =
                    cityMeta.selected_tab_action_meta;
                asset.click_action_zone = cityMeta.click_action_zone;
                asset.check_unit_has_meta = cityMeta.check_unit_has_meta;
                asset.set_unit_set_meta_for_meta_for_window =
                    cityMeta.set_unit_set_meta_for_meta_for_window;
            }
            else
            {
                LogWarning(
                    "Political Layer: vanilla City MetaType asset is unavailable."
                );
            }

            asset.draw_zones = new MetaZoneDrawAction(
                DrawPoliticalMapZones
            );
            asset.tile_get_metaobject = new MetaZoneGetMeta(
                GetPoliticalMapMetaForZone
            );
            asset.tile_get_metaobject_0 = new MetaZoneGetMetaSimple(
                GetPoliticalPartyMetaForZone
            );
            asset.tile_get_metaobject_1 = new MetaZoneGetMetaSimple(
                GetPoliticalIdeologyMetaForZone
            );
            asset.tile_get_metaobject_2 = new MetaZoneGetMetaSimple(
                GetPoliticalTensionMetaForZone
            );
            asset.check_tile_has_meta = new MetaZoneTooltipAction(
                PoliticalMapTileHasMeta
            );
            asset.check_cursor_tooltip = new MetaZoneTooltipAction(
                PoliticalMapNoTooltip
            );
            asset.check_cursor_highlight = new MetaZoneHighlightAction(
                delegate(
                    MetaTypeAsset pMetaTypeAsset,
                    WorldTile pTile,
                    QuantumSpriteAsset pQAsset
                )
                {
                }
            );
            asset.cursor_tooltip_action = new MetaTooltipShowAction(
                delegate(NanoObject pMeta)
                {
                }
            );

            // linkAssets() normally resolves this from option_id. Assign it
            // directly as well so getZoneOptionState() remains valid even if
            // another mod delays/reorders meta-library linking.
            asset.option_asset = AssetManager.options_library.get(
                PoliticalMapOptionId
            );

            _politicalMapMetaAsset = asset;
        }

        private static void RegisterPoliticalMapNameplate()
        {
            try
            {
                if (AssetManager.nameplates_library == null)
                {
                    return;
                }

                NameplateAsset plate =
                    AssetManager.nameplates_library.get("plate_city");

                if (plate == null)
                {
                    plate = AssetManager.nameplates_library.get(
                        "plate_kingdom"
                    );
                }

                if (plate != null)
                {
                    AssetManager.nameplates_library.map_modes_nameplates[
                        PoliticalMapMetaType
                    ] = plate;
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not register Political Layer nameplates: " +
                    exception.Message
                );
            }
        }

        private static void DrawPoliticalMapZones(
            MetaTypeAsset pAsset
        )
        {
            if (
                pAsset == null ||
                World.world == null ||
                World.world.zone_calculator == null
            )
            {
                return;
            }

            int mode = GetPoliticalMapMode();
            RefreshPoliticalMapVisualCacheIfNeeded(mode);

            MetaZoneGetMetaSimple getter = GetPoliticalMapZoneGetter(
                mode
            );
            if (getter == null)
            {
                return;
            }

            bool drewVisibleZones = false;

            try
            {
                if (World.world.zone_camera != null)
                {
                    List<TileZone> visibleZones =
                        World.world.zone_camera.getVisibleZones();

                    if (visibleZones != null)
                    {
                        drewVisibleZones = true;

                        for (int i = 0; i < visibleZones.Count; i++)
                        {
                            DrawPoliticalMapZone(
                                visibleZones[i],
                                pAsset,
                                getter
                            );
                        }
                    }
                }
            }
            catch
            {
                drewVisibleZones = false;
            }

            // Compatibility fallback if a future WorldBox build changes the
            // visible-zone camera API. This path is deliberately not used on
            // 0.51.2 because it is more expensive on large worlds.
            if (!drewVisibleZones)
            {
                List<Kingdom> kingdoms = GetKingdomsSafe();
                for (int k = 0; k < kingdoms.Count; k++)
                {
                    List<City> cities = GetCitiesSafe(kingdoms[k]);
                    for (int c = 0; c < cities.Count; c++)
                    {
                        City city = cities[c];
                        if (
                            city == null ||
                            city.data == null ||
                            city.isRekt() ||
                            city.zones == null
                        )
                        {
                            continue;
                        }

                        for (int z = 0; z < city.zones.Count; z++)
                        {
                            DrawPoliticalMapZone(
                                city.zones[z],
                                pAsset,
                                getter
                            );
                        }
                    }
                }
            }
        }

        private static void DrawPoliticalMapZone(
            TileZone pZone,
            MetaTypeAsset pAsset,
            MetaZoneGetMetaSimple pGetter
        )
        {
            if (
                pZone == null ||
                pZone.city == null ||
                pZone.city.data == null
            )
            {
                return;
            }

            try
            {
                World.world.zone_calculator.drawBegin();
                World.world.zone_calculator.drawZoneMeta(
                    pZone,
                    pAsset,
                    pGetter
                );
                World.world.zone_calculator.drawEnd(pZone);
            }
            catch
            {
            }
        }

        private static MetaZoneGetMetaSimple GetPoliticalMapZoneGetter(
            int pMode
        )
        {
            switch (pMode)
            {
                case PoliticalMapModeParties:
                    return new MetaZoneGetMetaSimple(
                        GetPoliticalPartyMetaForZone
                    );
                case PoliticalMapModeIdeologies:
                    return new MetaZoneGetMetaSimple(
                        GetPoliticalIdeologyMetaForZone
                    );
                case PoliticalMapModeTension:
                    return new MetaZoneGetMetaSimple(
                        GetPoliticalTensionMetaForZone
                    );
                default:
                    return new MetaZoneGetMetaSimple(
                        GetPoliticalPartyMetaForZone
                    );
            }
        }

        private static IMetaObject GetPoliticalMapMetaForZone(
            TileZone pZone,
            int pZoneOption
        )
        {
            return GetPoliticalMapCityVisual(
                pZone,
                ClampInt(
                    pZoneOption,
                    PoliticalMapModeParties,
                    PoliticalMapModeTension
                )
            );
        }

        private static IMetaObject GetPoliticalPartyMetaForZone(
            TileZone pZone
        )
        {
            return GetPoliticalMapCityVisual(
                pZone,
                PoliticalMapModeParties
            );
        }

        private static IMetaObject GetPoliticalIdeologyMetaForZone(
            TileZone pZone
        )
        {
            return GetPoliticalMapCityVisual(
                pZone,
                PoliticalMapModeIdeologies
            );
        }

        private static IMetaObject GetPoliticalTensionMetaForZone(
            TileZone pZone
        )
        {
            return GetPoliticalMapCityVisual(
                pZone,
                PoliticalMapModeTension
            );
        }

        private static bool PoliticalMapTileHasMeta(
            TileZone pZone,
            MetaTypeAsset pAsset,
            int pZoneOption
        )
        {
            return GetPoliticalMapMetaForZone(
                pZone,
                pZoneOption
            ) != null;
        }

        private static bool PoliticalMapNoTooltip(
            TileZone pZone,
            MetaTypeAsset pAsset,
            int pZoneOption
        )
        {
            return false;
        }

        private static PoliticalMapMetaObject GetPoliticalMapCityVisual(
            TileZone pZone,
            int pMode
        )
        {
            return GetPoliticalMapCityVisual(
                pZone == null ? null : pZone.city,
                pMode
            );
        }

        // dev7.3.1 compile fix: nameplates already give us a City directly,
        // while the zone renderer enters through TileZone. Keep both call
        // paths on the same cache/build logic instead of trying to pass a
        // City to the TileZone overload.
        private static PoliticalMapMetaObject GetPoliticalMapCityVisual(
            City pCity,
            int pMode
        )
        {
            if (
                pCity == null ||
                pCity.data == null ||
                pCity.isRekt()
            )
            {
                return null;
            }

            RefreshPoliticalMapVisualCacheIfNeeded(pMode);

            PoliticalMapMetaObject cached;
            if (PoliticalMapCityVisualCache.TryGetValue(
                    pCity,
                    out cached
                ))
            {
                return cached;
            }

            PoliticalMapMetaObject result = null;

            switch (pMode)
            {
                case PoliticalMapModeParties:
                    result = BuildPoliticalPartyMapVisual(pCity);
                    break;
                case PoliticalMapModeIdeologies:
                    result = BuildPoliticalIdeologyMapVisual(pCity);
                    break;
                case PoliticalMapModeTension:
                    result = BuildPoliticalTensionMapVisual(pCity);
                    break;
            }

            PoliticalMapCityVisualCache[pCity] = result;
            return result;
        }

        private static PoliticalMapMetaObject BuildPoliticalPartyMapVisual(
            City pCity
        )
        {
            Kingdom kingdom = GetKingdomFromObject(pCity);
            if (kingdom == null || kingdom.data == null)
            {
                return null;
            }

            List<PoliticalParty> parties;
            if (!PoliticalMapPartyCache.TryGetValue(
                    kingdom,
                    out parties
                ))
            {
                // Map rendering is presentation-only: avoid GetPoliticalParties
                // here because that function also writes support-history
                // snapshots. Loading the active slots is enough for local
                // support calculations and keeps map drawing side-effect free.
                parties = LoadPoliticalPartiesInternal(
                    kingdom,
                    false
                );
                PoliticalMapPartyCache[kingdom] = parties;
            }

            PoliticalParty best = null;
            int bestSupport = -1;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (party == null || !party.Active)
                {
                    continue;
                }

                int support = GetPartyCitySupport(
                    kingdom,
                    pCity,
                    party,
                    parties
                );

                if (
                    best == null ||
                    support > bestSupport ||
                    (
                        support == bestSupport &&
                        string.CompareOrdinal(
                            party.Id ?? "",
                            best.Id ?? ""
                        ) < 0
                    )
                )
                {
                    best = party;
                    bestSupport = support;
                }
            }

            if (best == null || bestSupport <= 0)
            {
                return GetOrCreatePoliticalMapMeta(
                    PoliticalMapModeParties,
                    "no_party",
                    LM.Get("ukiol_political_map_no_party"),
                    new Color(0.48f, 0.50f, 0.50f, 1f)
                );
            }

            return GetOrCreatePoliticalMapMeta(
                PoliticalMapModeParties,
                "party:" + GetStableObjectIdentity(kingdom) +
                    ":" + (best.Id ?? ""),
                best.Name,
                GetPartyIdentityColor(
                    best.Ideology,
                    best.ColorSeed
                )
            );
        }

        private static PoliticalMapMetaObject BuildPoliticalIdeologyMapVisual(
            City pCity
        )
        {
            string ideology;
            int support;
            int tension;
            GetCityIdeologyOverview(
                pCity,
                out ideology,
                out support,
                out tension
            );

            if (!IsValidIdeology(ideology))
            {
                return GetOrCreatePoliticalMapMeta(
                    PoliticalMapModeIdeologies,
                    "none",
                    LM.Get("ukiol_ideology_none"),
                    new Color(0.48f, 0.50f, 0.50f, 1f)
                );
            }

            string current = GetCityIdeologyCurrent(
                pCity,
                ideology
            );
            string name = !string.IsNullOrEmpty(current)
                ? GetIdeologyCurrentName(current)
                : GetIdeologyName(ideology);

            return GetOrCreatePoliticalMapMeta(
                PoliticalMapModeIdeologies,
                "ideology:" + ideology + ":" +
                    (string.IsNullOrEmpty(current) ? "root" : current),
                name,
                GetEmbeddedPartyIdeologyColor(ideology)
            );
        }

        private static PoliticalMapMetaObject BuildPoliticalTensionMapVisual(
            City pCity
        )
        {
            string ideology;
            int support;
            int tension;
            GetCityIdeologyOverview(
                pCity,
                out ideology,
                out support,
                out tension
            );

            int bucket = ClampInt(
                ((tension + 5) / 10) * 10,
                0,
                100
            );

            Color low = new Color(0.30f, 0.78f, 0.38f, 1f);
            Color medium = new Color(0.95f, 0.73f, 0.25f, 1f);
            Color high = new Color(0.92f, 0.25f, 0.20f, 1f);
            Color color;

            if (bucket <= 50)
            {
                color = Color.Lerp(
                    low,
                    medium,
                    bucket / 50f
                );
            }
            else
            {
                color = Color.Lerp(
                    medium,
                    high,
                    (bucket - 50) / 50f
                );
            }

            string name = string.Format(
                LM.Get("ukiol_political_map_tension_value"),
                tension
            );

            return GetOrCreatePoliticalMapMeta(
                PoliticalMapModeTension,
                "tension:" + tension.ToString(),
                name,
                color
            );
        }

        private static PoliticalMapMetaObject GetOrCreatePoliticalMapMeta(
            int pMode,
            string pKey,
            string pName,
            Color pColor
        )
        {
            string key = pMode.ToString() + "|" + (pKey ?? "");

            PoliticalMapMetaObject meta;
            if (PoliticalMapMetaCache.TryGetValue(key, out meta))
            {
                if (meta.data != null && meta.data.name != (pName ?? ""))
                {
                    meta.data.name = pName ?? "";
                }
                return meta;
            }

            ColorAsset color = GetPoliticalMapColorAsset(pColor);
            meta = new PoliticalMapMetaObject(
                GetPoliticalMapStableId(key),
                pName ?? "",
                color
            );
            PoliticalMapMetaCache[key] = meta;
            return meta;
        }

        private static ColorAsset GetPoliticalMapColorAsset(
            Color pColor
        )
        {
            pColor.a = 1f;
            string hex = "#" + ColorUtility.ToHtmlStringRGB(pColor);

            ColorAsset cached;
            if (PoliticalMapColorCache.TryGetValue(
                    hex,
                    out cached
                ))
            {
                return cached;
            }

            ColorAsset color = ColorAsset.tryMakeNewColorAsset(hex);
            if (color != null)
            {
                color.initColor();
                PoliticalMapColorCache[hex] = color;
            }

            return color;
        }

        private static long GetPoliticalMapStableId(string pKey)
        {
            unchecked
            {
                long hash = 1469598103934665603L;
                string raw = PoliticalMapMetaId + "|" + (pKey ?? "");
                for (int i = 0; i < raw.Length; i++)
                {
                    hash ^= raw[i];
                    hash *= 1099511628211L;
                }
                return hash & long.MaxValue;
            }
        }

        private static int GetPoliticalMapMode()
        {
            try
            {
                if (_politicalMapMetaAsset != null)
                {
                    return ClampInt(
                        _politicalMapMetaAsset.getZoneOptionState(),
                        PoliticalMapModeParties,
                        PoliticalMapModeTension
                    );
                }
            }
            catch
            {
            }

            PlayerOptionData data;
            if (PlayerConfig.dict.TryGetValue(
                    PoliticalMapOptionId,
                    out data
                ) && data != null)
            {
                return ClampInt(
                    data.intVal,
                    PoliticalMapModeParties,
                    PoliticalMapModeTension
                );
            }

            return PoliticalMapModeParties;
        }

        private static void RefreshPoliticalMapVisualCacheIfNeeded(
            int pMode
        )
        {
            if (
                pMode != _politicalMapCachedMode ||
                Time.unscaledTime >= _politicalMapVisualCacheExpiresAt
            )
            {
                PoliticalMapCityVisualCache.Clear();
                PoliticalMapPartyCache.Clear();
                _politicalMapCachedMode = pMode;
                _politicalMapVisualCacheExpiresAt =
                    Time.unscaledTime + PoliticalMapVisualRefreshInterval;
            }
        }

        private static void InvalidatePoliticalMapVisualCache()
        {
            PoliticalMapCityVisualCache.Clear();
            PoliticalMapPartyCache.Clear();
            _politicalMapCachedMode = -1;
            _politicalMapVisualCacheExpiresAt = 0f;
        }

    }
}
