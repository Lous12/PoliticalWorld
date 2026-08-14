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
        private static GodPower CreateBasePower(
            string powerId,
            PowerActionWithID clickAction
        )
        {
            return new GodPower()
            {
                id = powerId,
                name = powerId,
                rank = PowerRank.Rank0_free,
                type = PowerActionType.PowerSpecial,
                click_action = clickAction,
                requires_premium = false,
                tester_enabled = true,
                track_activity = true
            };
        }

        private static void CreateReformerPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    ReformerPowerId,
                    new PowerActionWithID(
                        GiveReformerToKingdomRuler
                    )
                )
            );
        }

        private static void CreateMilitaristPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    MilitaristPowerId,
                    new PowerActionWithID(
                        GiveMilitaristToKingdomRuler
                    )
                )
            );
        }

        private static void CreateDiplomatPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    DiplomatPowerId,
                    new PowerActionWithID(
                        GiveDiplomatToKingdomRuler
                    )
                )
            );
        }

        private static void CreateReformerCoursePower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    ReformerCoursePowerId,
                    new PowerActionWithID(
                        SetReformerCourse
                    )
                )
            );
        }

        private static void CreateMilitaristCoursePower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    MilitaristCoursePowerId,
                    new PowerActionWithID(
                        SetMilitaristCourse
                    )
                )
            );
        }

        private static void CreateDiplomatCoursePower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    DiplomatCoursePowerId,
                    new PowerActionWithID(
                        SetDiplomatCourse
                    )
                )
            );
        }

        private static void CreateStabilizeCityPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    StabilizeCityPowerId,
                    new PowerActionWithID(
                        StabilizeCity
                    )
                )
            );
        }

        private static void CreateDestabilizeCityPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    DestabilizeCityPowerId,
                    new PowerActionWithID(
                        DestabilizeCity
                    )
                )
            );
        }

        private static void CreateEncourageReformPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    EncourageReformPowerId,
                    new PowerActionWithID(EncourageReform)
                )
            );
        }

        private static void CreateIncreaseRadicalizationPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    IncreaseRadicalizationPowerId,
                    new PowerActionWithID(IncreaseRadicalization)
                )
            );
        }

        private static void CreateReduceRadicalizationPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    ReduceRadicalizationPowerId,
                    new PowerActionWithID(ReduceRadicalization)
                )
            );
        }

        private static void CreatePoliticalOverviewPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    PoliticalOverviewPowerId,
                    new PowerActionWithID(
                        OpenPoliticalOverview
                    )
                )
            );
        }

        private static void CreatePoliticalOverviewWindow()
        {
            try
            {
                PoliticalOverviewWindow.CreateWindow(
                    PoliticalOverviewWindowId,
                    "ukiol_political_overview_title"
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create Political Overview window: " +
                    exception.Message
                );
            }
        }

        private static void CreateMonarchismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                MonarchismPowerId,
                new PowerActionWithID(SetMonarchismIdeology)
            ));
        }

        private static void CreateConservatismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                ConservatismPowerId,
                new PowerActionWithID(SetConservatismIdeology)
            ));
        }

        private static void CreateLiberalismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                LiberalismPowerId,
                new PowerActionWithID(SetLiberalismIdeology)
            ));
        }

        private static void CreateDemocracyPower()
        {
            AssetManager.powers.add(CreateBasePower(
                DemocracyPowerId,
                new PowerActionWithID(SetDemocracyIdeology)
            ));
        }

        private static void CreateSocialismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                SocialismPowerId,
                new PowerActionWithID(SetSocialismIdeology)
            ));
        }

        private static void CreateCommunismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CommunismPowerId,
                new PowerActionWithID(SetCommunismIdeology)
            ));
        }

        private static void CreateFascismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                FascismPowerId,
                new PowerActionWithID(SetFascismIdeology)
            ));
        }

        private static void CreateAnarchismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                AnarchismPowerId,
                new PowerActionWithID(SetAnarchismIdeology)
            ));
        }

        private static void CreateSyndicalismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                SyndicalismPowerId,
                new PowerActionWithID(SetSyndicalismIdeology)
            ));
        }

        private static void CreateCityMonarchismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityMonarchismPowerId,
                new PowerActionWithID(SetCityMonarchismIdeology)
            ));
        }

        private static void CreateCityConservatismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityConservatismPowerId,
                new PowerActionWithID(SetCityConservatismIdeology)
            ));
        }

        private static void CreateCityLiberalismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityLiberalismPowerId,
                new PowerActionWithID(SetCityLiberalismIdeology)
            ));
        }

        private static void CreateCityDemocracyPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityDemocracyPowerId,
                new PowerActionWithID(SetCityDemocracyIdeology)
            ));
        }

        private static void CreateCitySocialismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CitySocialismPowerId,
                new PowerActionWithID(SetCitySocialismIdeology)
            ));
        }

        private static void CreateCityCommunismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityCommunismPowerId,
                new PowerActionWithID(SetCityCommunismIdeology)
            ));
        }

        private static void CreateCityFascismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityFascismPowerId,
                new PowerActionWithID(SetCityFascismIdeology)
            ));
        }

        private static void CreateCityAnarchismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CityAnarchismPowerId,
                new PowerActionWithID(SetCityAnarchismIdeology)
            ));
        }

        private static void CreateCitySyndicalismPower()
        {
            AssetManager.powers.add(CreateBasePower(
                CitySyndicalismPowerId,
                new PowerActionWithID(SetCitySyndicalismIdeology)
            ));
        }

        private static void CreateStateIdeologyEditorPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    StateIdeologyEditorPowerId,
                    new PowerActionWithID(OpenStateIdeologyEditor)
                )
            );
        }

        private static void CreateCityIdeologyEditorPower()
        {
            AssetManager.powers.add(
                CreateBasePower(
                    CityIdeologyEditorPowerId,
                    new PowerActionWithID(OpenCityIdeologyEditor)
                )
            );
        }

        private static void CreateIdeologyEditorWindow()
        {
            try
            {
                IdeologyEditorWindow.CreateWindow(
                    IdeologyEditorWindowId,
                    "ukiol_ideology_editor_title"
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create Ideology Editor window: " +
                    exception.Message
                );
            }
        }

        private static void CreatePartyRenameWindow()
        {
            try
            {
                PartyRenameWindow.CreateWindow(
                    PartyRenameWindowId,
                    "ukiol_party_rename_title"
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create Party Rename window: " +
                    exception.Message
                );
            }
        }

        private static void CreatePoliticsTab()
        {
            _politicsTab = TabManager.CreateTab(
                "UkiolPolitics",
                "tab_ukiol_politics",
                "hotkey_tip_tab_other",
                SpriteTextureLoader.getSprite(
                    PoliticsIconPath
                )
            );

            // WorldBox 0.50.5+ lays toolbar elements out by direct-child
            // sibling order. Do not use SetLayout()/UpdateLayout() here:
            // NeoModLoader's legacy grouped-layout helper still positions
            // elements through localPosition and creates all _line objects
            // after the buttons, which makes the separators collect at the
            // far right on WorldBox 0.51.2.
            //
            // The stable pattern is the one used by current PowerBox:
            // append buttons in display order and insert a clone of the
            // vanilla main-tab _line prefab exactly where a section ends.
            // Never resize/reposition the separator manually.

            // Keep vanilla pause/play + speed controls as their own section.
            AddPoliticsNativeSeparator("utility_to_traits");

            // Political traits.
            AddPoliticsToolbarButton(ReformerPowerId, ReformerIconPath);
            AddPoliticsToolbarButton(DiplomatPowerId, DiplomatIconPath);
            AddPoliticsToolbarButton(MilitaristPowerId, MilitaristIconPath);
            AddPoliticsNativeSeparator("traits_to_courses");

            // State course.
            AddPoliticsToolbarButton(ReformerCoursePowerId, ReformerIconPath);
            AddPoliticsToolbarButton(DiplomatCoursePowerId, DiplomatIconPath);
            AddPoliticsToolbarButton(MilitaristCoursePowerId, MilitaristIconPath);
            AddPoliticsNativeSeparator("courses_to_state_ideologies");

            // Ideology editors. The state editor can assign any node in the
            // complete ideology tree. The settlement editor uses the same tree
            // while keeping citizen ideology storage compatible with the root-
            // based local support simulation.
            // Both ideology editors belong to one visual group. With the
            // current sibling-order toolbar this places them as the top/bottom
            // pair in one column instead of two isolated one-button sections.
            AddPoliticsToolbarButton(StateIdeologyEditorPowerId, PoliticsIconPath);
            AddPoliticsToolbarButton(CityIdeologyEditorPowerId, SocietyIconPath);
            AddPoliticsNativeSeparator("ideology_editors_to_pressure_controls");

            // Political pressure controls. They operate on the same persisted
            // reform/radicalization pressures used by the automatic ideology
            // evolution system, so player intervention remains part of the
            // normal simulation instead of bypassing it.
            AddPoliticsToolbarButton(EncourageReformPowerId, ReformerIconPath);
            AddPoliticsToolbarButton(IncreaseRadicalizationPowerId, MilitaristIconPath);
            AddPoliticsToolbarButton(ReduceRadicalizationPowerId, DiplomatIconPath);
            AddPoliticsNativeSeparator("pressure_controls_to_map_layer");

            // Political map. It is a true multi-toggle zone layer. The
            // button uses the vanilla "subspecies_layer" visual so its three
            // mode indicators are visible underneath; X/Z still cycles
            // Parties, Ideologies and Political Tension.
            AddPoliticsToolbarToggleButton(PoliticalMapPowerId, OverviewIconPath);
            AddPoliticsNativeSeparator("map_layer_to_stability");

            // Stability tools.
            AddPoliticsToolbarButton(StabilizeCityPowerId, DiplomatIconPath);
            AddPoliticsToolbarButton(DestabilizeCityPowerId, MilitaristIconPath);

            RefreshPoliticsTabButtonNavigation();
        }

        private static void AddPoliticsToolbarButton(
            string pPowerId,
            string pIconPath
        )
        {
            if (_politicsTab == null)
            {
                return;
            }

            PowerButton button = PowerButtonCreator.CreateGodPowerButton(
                pPowerId,
                SpriteTextureLoader.getSprite(pIconPath)
            );

            if (button == null)
            {
                LogWarning("Could not create Politics toolbar button: " + pPowerId);
                return;
            }

            // AddButtonToTab is the current NeoModLoader path for WorldBox's
            // sibling-order toolbar system. It appends the button as a direct
            // child and records it in PowersTab._power_buttons.
            PowerButtonCreator.AddButtonToTab(button, _politicsTab);
        }

        private static void AddPoliticsToolbarToggleButton(
            string pPowerId,
            string pIconPath
        )
        {
            if (_politicsTab == null)
            {
                return;
            }

            PowerButton button = null;

            // Current WorldBox/NML's generic CreateToggleButton clones
            // "map_kings_leaders", which only has the single on/off marker.
            // Vanilla multi-mode layers use "subspecies_layer"; cloning that
            // prefab gives us the three small mode markers under the icon.
            try
            {
                PowerButton prefab =
                    ResourcesFinder.FindResource<PowerButton>(
                        "subspecies_layer"
                    );

                if (prefab != null)
                {
                    bool wasActive = prefab.gameObject.activeSelf;
                    if (wasActive)
                    {
                        prefab.gameObject.SetActive(false);
                    }

                    button = UnityEngine.Object.Instantiate(prefab);

                    if (wasActive)
                    {
                        prefab.gameObject.SetActive(true);
                    }

                    Sprite sprite = SpriteTextureLoader.getSprite(
                        pIconPath
                    );

                    button.name = pPowerId;
                    button.icon.sprite = sprite;
                    button.icon.overrideSprite = sprite;
                    button.open_window_id = null;
                    button.type = PowerButtonType.Special;
                    button.transform.localScale = Vector3.one;
                    button.gameObject.SetActive(true);
                    button.init();
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create vanilla multi-toggle Political Layer button: " +
                    exception.Message
                );
                button = null;
            }

            // Compatibility fallback for installations where the vanilla
            // multi-mode prefab was renamed by another mod/game build.
            if (button == null)
            {
                button = PowerButtonCreator.CreateToggleButton(
                    pPowerId,
                    SpriteTextureLoader.getSprite(pIconPath),
                    null,
                    default(Vector2),
                    true
                );
            }

            if (button == null)
            {
                LogWarning(
                    "Could not create Politics toolbar toggle button: " +
                    pPowerId
                );
                return;
            }

            PowerButtonCreator.AddButtonToTab(button, _politicsTab);

            if (pPowerId == PoliticalMapPowerId)
            {
                _politicalMapToolbarButton = button;
                _politicalMapToolbarVisualInitialized = false;
                SyncPoliticalMapToolbarModeIndicators(true);
            }
        }

        private static void SyncPoliticalMapToolbarModeIndicators(
            bool pForce = false
        )
        {
            PowerButton button = _politicalMapToolbarButton;
            if (
                button == null ||
                button.gameObject == null
            )
            {
                return;
            }

            bool active = IsPoliticalMapLayerActive(false);
            int mode = GetPoliticalMapMode();

            if (
                !pForce &&
                _politicalMapToolbarVisualInitialized &&
                active == _politicalMapToolbarVisualActive &&
                mode == _politicalMapToolbarVisualMode
            )
            {
                return;
            }

            _politicalMapToolbarVisualInitialized = true;
            _politicalMapToolbarVisualActive = active;
            _politicalMapToolbarVisualMode = mode;

            try
            {
                Transform mainToggle = button.transform.Find(
                    "ToggleIcon"
                );
                if (mainToggle != null)
                {
                    ToggleIcon icon =
                        mainToggle.GetComponent<ToggleIcon>();
                    if (icon != null)
                    {
                        icon.updateIcon(active);
                    }
                }

                // Vanilla's three markers are intentionally rotated relative
                // to the option index: mode 0 -> toggle_1, mode 1 -> toggle_2,
                // mode 2 -> toggle_0. This is the same mapping used by the
                // game's multi-mode layer prefab.
                for (
                    int optionIndex = PoliticalMapModeParties;
                    optionIndex <= PoliticalMapModeTension;
                    optionIndex++
                )
                {
                    int childIndex =
                        optionIndex + 1 > PoliticalMapModeTension
                            ? 0
                            : optionIndex + 1;

                    Transform marker = button.transform.Find(
                        "toggle_" + childIndex
                    );
                    if (marker == null)
                    {
                        continue;
                    }

                    ToggleIcon markerIcon =
                        marker.GetComponent<ToggleIcon>();
                    if (markerIcon == null)
                    {
                        continue;
                    }

                    markerIcon.updateIconMultiToggle(
                        active,
                        active && mode == optionIndex
                    );
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not sync Political Layer mode indicators: " +
                    exception.Message
                );
            }
        }

        private static void AddPoliticsNativeSeparator(string pBoundaryName)
        {
            if (_politicsTab == null || _politicsTab.transform == null)
            {
                return;
            }

            try
            {
                GameObject linePrefab = FindVanillaTabSeparatorPrefab();
                if (linePrefab == null)
                {
                    LogWarning(
                        "Could not find vanilla _line prefab for Politics boundary: " +
                        pBoundaryName
                    );
                    return;
                }

                // IMPORTANT: append the native object directly into the tab and
                // leave all of its transform/RectTransform values untouched.
                // WorldBox decides the visible position from sibling order.
                GameObject separator = UnityEngine.Object.Instantiate(
                    linePrefab,
                    _politicsTab.transform
                );
                separator.name = "_line_ukiol_" + pBoundaryName;
                separator.SetActive(true);

                Image separatorImage = separator.GetComponent<Image>();
                if (separatorImage != null)
                {
                    separatorImage.enabled = true;
                    separatorImage.raycastTarget = false;
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not add native Politics separator " +
                    pBoundaryName + ": " + exception.Message
                );
            }
        }

        private static GameObject FindVanillaTabSeparatorPrefab()
        {
            // First prefer the exact same source used by PowerBox: the _line
            // object from the vanilla main tab. This avoids accidentally
            // cloning a modified separator from another modded tab.
            try
            {
                GameObject canvasContainer = GameObject.Find("/Canvas Container Main");
                if (canvasContainer != null)
                {
                    Transform current = canvasContainer.transform;
                    string[] pathParts = new string[]
                    {
                        "Canvas - UI/General",
                        "CanvasBottom",
                        "BottomElements",
                        "BottomElementsMover",
                        "CanvasScrollView",
                        "Scroll View",
                        "Viewport",
                        "Content",
                        "Power Tabs",
                        "main",
                        "_line"
                    };

                    for (int i = 0; i < pathParts.Length && current != null; i++)
                    {
                        current = FindDirectOrRecursiveChild(current, pathParts[i]);
                    }

                    if (current != null && current.gameObject != null)
                    {
                        return current.gameObject;
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to the resource lookup below.
            }

            return ResourcesFinder.FindResource<GameObject>("_line");
        }

        private static Transform FindDirectOrRecursiveChild(
            Transform pRoot,
            string pName
        )
        {
            if (pRoot == null || string.IsNullOrEmpty(pName))
            {
                return null;
            }

            Transform direct = pRoot.Find(pName);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < pRoot.childCount; i++)
            {
                Transform child = pRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(child.name, pName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindDirectOrRecursiveChild(child, pName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void RefreshPoliticsTabButtonNavigation()
        {
            if (_politicsTab == null)
            {
                return;
            }

            _politicsTab._power_buttons.Clear();
            foreach (PowerButton powerButton in _politicsTab.GetComponentsInChildren<PowerButton>())
            {
                if (
                    powerButton != null &&
                    powerButton.rect_transform != null
                )
                {
                    _politicsTab._power_buttons.Add(powerButton);
                }
            }

            foreach (PowerButton powerButton in _politicsTab._power_buttons)
            {
                powerButton.findNeighbours(_politicsTab._power_buttons);
            }
        }

        private static bool OpenStateIdeologyEditor(
            WorldTile pTile,
            string pPowerId
        )
        {
            try
            {
                if (
                    pTile == null ||
                    pTile.zone == null ||
                    pTile.zone.city == null ||
                    pTile.zone.city.kingdom == null
                )
                {
                    return false;
                }

                IdeologyEditorWindow.OpenForKingdom(
                    pTile.zone.city.kingdom,
                    pTile
                );
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not open state Ideology Editor: " +
                    exception.Message
                );
                return false;
            }
        }

        private static bool OpenCityIdeologyEditor(
            WorldTile pTile,
            string pPowerId
        )
        {
            try
            {
                if (
                    pTile == null ||
                    pTile.zone == null ||
                    pTile.zone.city == null
                )
                {
                    return false;
                }

                IdeologyEditorWindow.OpenForCity(
                    pTile.zone.city,
                    pTile
                );
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not open settlement Ideology Editor: " +
                    exception.Message
                );
                return false;
            }
        }

        private static bool OpenPoliticalOverview(
            WorldTile pTile,
            string pPowerId
        )
        {
            try
            {
                if (
                    pTile == null ||
                    pTile.zone == null ||
                    pTile.zone.city == null ||
                    pTile.zone.city.kingdom == null
                )
                {
                    return false;
                }

                PoliticalOverviewWindow.OpenForKingdom(
                    pTile.zone.city.kingdom
                );
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not open Political Overview: " +
                    exception.Message
                );
                return false;
            }
        }

        private static bool GiveReformerToKingdomRuler(
            WorldTile pTile,
            string pPowerId
        )
        {
            return GiveTraitToKingdomRuler(
                pTile,
                ReformerTraitId
            );
        }

        private static bool GiveMilitaristToKingdomRuler(
            WorldTile pTile,
            string pPowerId
        )
        {
            return GiveTraitToKingdomRuler(
                pTile,
                MilitaristTraitId
            );
        }

        private static bool GiveDiplomatToKingdomRuler(
            WorldTile pTile,
            string pPowerId
        )
        {
            return GiveTraitToKingdomRuler(
                pTile,
                DiplomatTraitId
            );
        }

        private static bool SetReformerCourse(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetCourseForKingdom(
                pTile,
                ReformerTraitId
            );
        }

        private static bool SetMilitaristCourse(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetCourseForKingdom(
                pTile,
                MilitaristTraitId
            );
        }

        private static bool SetDiplomatCourse(
            WorldTile pTile,
            string pPowerId
        )
        {
            return SetCourseForKingdom(
                pTile,
                DiplomatTraitId
            );
        }

        private static bool StabilizeCity(
            WorldTile pTile,
            string pPowerId
        )
        {
            return ChangeSelectedCityStability(
                pTile,
                25,
                5
            );
        }

        private static bool DestabilizeCity(
            WorldTile pTile,
            string pPowerId
        )
        {
            return ChangeSelectedCityStability(
                pTile,
                -25,
                -5
            );
        }

        private static bool ChangeSelectedCityStability(
            WorldTile pTile,
            int localDelta,
            int nationalDelta
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

            City city = pTile.zone.city;
            Kingdom kingdom =
                GetKingdomFromObject(city);

            if (kingdom == null)
            {
                return false;
            }

            SetLocalStability(
                city,
                GetLocalStability(city) + localDelta
            );

            SetNationalStability(
                kingdom,
                GetNationalStability(kingdom) +
                    nationalDelta
            );

            EffectsLibrary.spawnAtTile(
                localDelta >= 0
                    ? "fx_positive_effect"
                    : "fx_bad_place",
                pTile,
                0.5f
            );

            return true;
        }

        private static bool SetCourseForKingdom(
            WorldTile pTile,
            string course
        )
        {
            if (pTile == null)
            {
                return false;
            }

            Kingdom kingdom = pTile.zone.city?.kingdom;

            if (
                kingdom == null ||
                !SetKingdomCourse(kingdom, course)
            )
            {
                EffectsLibrary.spawnAtTile(
                    "fx_bad_place",
                    pTile,
                    0.25f
                );

                return false;
            }

            Actor ruler = GetLivingRuler(kingdom);

            if (ruler != null)
            {
                ruler.startShake(
                    0.3f,
                    0.1f,
                    true,
                    true
                );

                ruler.startColorEffect(
                    ActorColorEffect.White
                );
            }

            EffectsLibrary.spawnAtTile(
                "fx_positive_effect",
                pTile,
                0.5f
            );

            return true;
        }

        private static bool GiveTraitToKingdomRuler(
            WorldTile pTile,
            string traitId
        )
        {
            if (pTile == null)
            {
                return false;
            }

            Kingdom kingdom = pTile.zone.city?.kingdom;
            Actor ruler = GetLivingRuler(kingdom);

            if (ruler == null)
            {
                EffectsLibrary.spawnAtTile(
                    "fx_bad_place",
                    pTile,
                    0.25f
                );

                return false;
            }

            AssignPoliticalTrait(ruler, traitId);

            ruler.startShake(
                0.3f,
                0.1f,
                true,
                true
            );

            ruler.startColorEffect(
                ActorColorEffect.White
            );

            EffectsLibrary.spawnAtTile(
                "fx_positive_effect",
                pTile,
                0.5f
            );

            return true;
        }

        private static void AssignPoliticalTrait(
            Actor ruler,
            string traitId
        )
        {
            if (ruler == null)
            {
                return;
            }

            RemovePoliticalTraits(ruler);
            ruler.addTrait(traitId);
            ruler.setStatsDirty();
        }

        private static void RemovePoliticalTraits(Actor ruler)
        {
            if (ruler.hasTrait(ReformerTraitId))
            {
                ruler.removeTrait(ReformerTraitId);
            }

            if (ruler.hasTrait(MilitaristTraitId))
            {
                ruler.removeTrait(MilitaristTraitId);
            }

            if (ruler.hasTrait(DiplomatTraitId))
            {
                ruler.removeTrait(DiplomatTraitId);
            }
        }    }
}
