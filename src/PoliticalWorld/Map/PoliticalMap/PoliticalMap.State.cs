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
        // v1.6.0-dev7: native political map layer. The custom MetaType follows
        // the same pattern used by current WorldBox mods: a real MetaTypeAsset
        // owns zone drawing while a multi-toggle GodPower switches the three
        // political views through WorldBox's normal X/Z layer controls.
        private const string PoliticalMapPowerId = "ukiol_political_map_layer";
        private const string PoliticalMapMetaId = "ukiol_political_map";
        private const string PoliticalMapOptionId = "map_ukiol_political_map_layer";
        private const MetaType PoliticalMapMetaType = (MetaType)260;
        private const int PoliticalMapModeParties = 0;
        private const int PoliticalMapModeIdeologies = 1;
        private const int PoliticalMapModeTension = 2;
        private const float PoliticalMapVisualRefreshInterval = 1.5f;
        private const int PoliticalMapMetaCacheLimit = 1024;

        private static bool _politicalMapMetaStringPatchInstalled;
        private static bool _politicalMapBorderModePatchInstalled;
        private static bool _politicalMapNameplateTextPatchInstalled;
        private static MetaTypeAsset _politicalMapMetaAsset;
        private static readonly Dictionary<string, PoliticalMapMetaObject>
            PoliticalMapMetaCache = new Dictionary<string, PoliticalMapMetaObject>();
        private static readonly Dictionary<string, ColorAsset>
            PoliticalMapColorCache = new Dictionary<string, ColorAsset>();
        private static readonly Dictionary<City, PoliticalMapMetaObject>
            PoliticalMapCityVisualCache = new Dictionary<City, PoliticalMapMetaObject>();
        private static readonly Dictionary<Kingdom, List<PoliticalParty>>
            PoliticalMapPartyCache = new Dictionary<Kingdom, List<PoliticalParty>>();
        private static int _politicalMapCachedMode = -1;
        private static float _politicalMapVisualCacheExpiresAt;

        // v1.6.0-dev7.2: keep the Politics-tab map-layer button visually
        // identical to vanilla multi-mode layer buttons. WorldBox's
        // "subspecies_layer" prefab exposes one main ToggleIcon plus three
        // small mode indicators (toggle_0/1/2) under the button.
        private static PowerButton _politicalMapToolbarButton;
        private static int _politicalMapToolbarVisualMode = -1;
        private static bool _politicalMapToolbarVisualActive;
        private static bool _politicalMapToolbarVisualInitialized;

        private sealed class PoliticalMapMetaObject : MetaObject<MetaObjectData>
        {
            private readonly ColorAsset _color;
            private readonly ActorAsset _actorAsset;

            public PoliticalMapMetaObject(
                long pId,
                string pName,
                ColorAsset pColor
            )
            {
                _color = pColor;
                _actorAsset = AssetManager.actor_library.get("human");
                data = new MetaObjectData
                {
                    id = pId,
                    name = pName ?? "",
                    created_time = 0
                };
                setHash(
                    (PoliticalMapMetaType.GetHashCode() * 397) ^
                    pId.GetHashCode()
                );
                if (_color != null)
                {
                    _color.initColor();
                }
            }

            public override MetaType meta_type
            {
                get { return PoliticalMapMetaType; }
            }

            public override BaseSystemManager manager
            {
                get { return null; }
            }

            public override ColorLibrary getColorLibrary()
            {
                return AssetManager.kingdom_colors_library;
            }

            public override ColorAsset getColor()
            {
                if (_color != null)
                {
                    return _color;
                }

                ColorAsset fallback =
                    ColorAsset.tryMakeNewColorAsset("#777777");
                if (fallback != null)
                {
                    fallback.initColor();
                }
                return fallback;
            }

            public override ActorAsset getActorAsset()
            {
                return _actorAsset ??
                    AssetManager.actor_library.get("human");
            }

            public override bool hasCities()
            {
                return false;
            }

            public override IEnumerable<City> getCities()
            {
                return new City[0];
            }

            public override bool hasKingdoms()
            {
                return false;
            }

            public override IEnumerable<Kingdom> getKingdoms()
            {
                return new Kingdom[0];
            }

            public override Sprite getTopicSprite()
            {
                return getSpriteIcon();
            }

            public override void Dispose()
            {
            }
        }

    }
}
