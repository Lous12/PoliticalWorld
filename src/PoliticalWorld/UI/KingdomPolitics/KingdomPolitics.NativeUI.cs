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
        private static void KingdomStatsRowsPostfix(
            KingdomWindow __instance
        )
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                Kingdom kingdom = __instance.meta_object;

                if (kingdom == null)
                {
                    kingdom = GetMemberValue(
                        __instance,
                        "kingdom",
                        "_kingdom"
                    ) as Kingdom;
                }

                if (kingdom == null)
                {
                    return;
                }

                EnsureNativeKingdomPoliticsUi(__instance);

                int windowId = __instance.GetInstanceID();

                // v1.3.6.7: Politics no longer destroys/hides the vanilla
                // stats content. The political page is an embedded panel
                // inside the same KingdomWindow. If WorldBox itself asks to
                // rebuild the normal stats page, treat that as leaving
                // Politics and hide our embedded panel.
                if (
                    _kingdomPoliticsWindows.Contains(windowId) &&
                    !_openingNativeKingdomPolitics
                )
                {
                    _kingdomPoliticsWindows.Remove(windowId);
                    HideNativeKingdomPoliticsPanel(__instance);
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not render native kingdom politics UI: " +
                    exception.Message
                );
            }
        }

        private static void CityStatsRowsPostfix(
            CityWindow __instance
        )
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                City city = __instance.meta_object;

                if (city == null)
                {
                    city = GetMemberValue(
                        __instance,
                        "city",
                        "_city"
                    ) as City;
                }

                if (city == null)
                {
                    return;
                }

                EnsureNativeCityPoliticsUi(__instance);

                int windowId = __instance.GetInstanceID();
                if (
                    _cityPoliticsWindows.Contains(windowId) &&
                    !_openingNativeCityPolitics
                )
                {
                    _cityPoliticsWindows.Remove(windowId);
                    _cityPoliticsAddonPageIds.Remove(windowId);
                    HideNativeCityPoliticsPanel(__instance);
                }

                // 1.10.0-dev3: keep the vanilla settlement page readable.
                // The old build dumped every political metric into the normal
                // stat list and eventually turned the window into a debug log.
                // Only the two useful at-a-glance rows stay here; the full
                // local political state now lives in the dedicated Politics
                // page.
                int stability = GetLocalStability(city);
                int target = CalculateLocalStabilityTarget(city);

                ShowCityStatRowWithCustomIcon(
                    __instance,
                    "ukiol_local_stability_label",
                    (object)FormatStabilityValue(
                        stability,
                        target,
                        true
                    ),
                    GetStabilityColor(stability),
                    OverviewIconPath,
                    "iconLeaders"
                );

                string dominantIdeology;
                int dominantSupport;
                int ideologicalTension;

                GetCityIdeologyOverview(
                    city,
                    out dominantIdeology,
                    out dominantSupport,
                    out ideologicalTension
                );

                ShowCityStatRowWithCustomIcon(
                    __instance,
                    "ukiol_city_ideology_label",
                    (object)FormatCityPoliticalIdentity(
                        city,
                        dominantIdeology,
                        dominantSupport
                    ),
                    "#E8D36A",
                    GetIdeologyIconPath(dominantIdeology),
                    "iconLeaders"
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not render settlement Politics entry point: " +
                    exception.Message
                );
            }
        }

        private static KingdomWindow[] GetCachedKingdomWindows()
        {
            bool needsRefresh =
                _cachedKingdomWindows == null ||
                _cachedKingdomWindows.Length == 0 ||
                Time.unscaledTime >= _nextKingdomWindowCacheRefreshTime;

            if (!needsRefresh)
            {
                return _cachedKingdomWindows;
            }

            try
            {
                KingdomWindow[] found =
                    Resources.FindObjectsOfTypeAll<KingdomWindow>();
                _cachedKingdomWindows = found ?? new KingdomWindow[0];
            }
            catch
            {
                _cachedKingdomWindows = new KingdomWindow[0];
            }

            _nextKingdomWindowCacheRefreshTime =
                Time.unscaledTime + KingdomWindowCacheRefreshInterval;
            return _cachedKingdomWindows;
        }

        private static void HandleKingdomPoliticsRailExitClick()
        {
            try
            {
                if (!Input.GetMouseButtonDown(0))
                {
                    return;
                }

                if (_kingdomPoliticsWindows.Count == 0)
                {
                    return;
                }

                KingdomWindow[] windows =
                    GetCachedKingdomWindows();
                if (windows == null)
                {
                    return;
                }

                KingdomWindow activeWindow = null;
                for (int i = 0; i < windows.Length; i++)
                {
                    KingdomWindow window = windows[i];
                    if (
                        window == null ||
                        window.gameObject == null ||
                        !window.gameObject.activeInHierarchy ||
                        !IsKingdomWindowFullyOpen(window)
                    )
                    {
                        continue;
                    }

                    int id = window.GetInstanceID();
                    if (_kingdomPoliticsWindows.Contains(id))
                    {
                        activeWindow = window;
                        break;
                    }
                }

                if (activeWindow == null)
                {
                    return;
                }

                Vector2 pointer = Input.mousePosition;

                // Never interpret a click on our own Politics tab as an exit.
                if (
                    _kingdomPoliticsOverlayButton != null &&
                    _kingdomPoliticsOverlayButton.gameObject != null
                )
                {
                    RectTransform politicsButtonRect =
                        _kingdomPoliticsOverlayButton.GetComponent<RectTransform>();
                    if (
                        politicsButtonRect != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(
                            politicsButtonRect,
                            pointer,
                            null
                        )
                    )
                    {
                        return;
                    }
                }

                RectTransform windowRect =
                    activeWindow.GetComponent<RectTransform>();
                if (windowRect == null)
                {
                    return;
                }

                Canvas sourceCanvas =
                    activeWindow.GetComponentInParent<Canvas>();
                Camera uiCamera = null;
                if (
                    sourceCanvas != null &&
                    sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                )
                {
                    uiCamera = sourceCanvas.worldCamera;
                }

                Vector3[] corners = new Vector3[4];
                windowRect.GetWorldCorners(corners);
                Vector2 bl = RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    corners[0]
                );
                Vector2 tl = RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    corners[1]
                );

                float left = Mathf.Min(bl.x, tl.x);
                float bottom = Mathf.Min(bl.y, tl.y);
                float top = Mathf.Max(bl.y, tl.y);

                // The vanilla vertical rail sits just outside the window's
                // left edge. Keep this detector narrow so map clicks and the
                // Political World tab itself are unaffected.
                float railLeft = left - 82f;
                float railRight = left + 10f;

                if (
                    pointer.x < railLeft ||
                    pointer.x > railRight ||
                    pointer.y < bottom ||
                    pointer.y > top
                )
                {
                    return;
                }

                int windowId = activeWindow.GetInstanceID();
                _kingdomPoliticsWindows.Remove(windowId);
                _kingdomPoliticsSelectedPartyIds.Remove(windowId);
                _kingdomPoliticsSelectedPartyKingdomIds.Remove(windowId);
                HideNativeKingdomPoliticsPanel(activeWindow);
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not handle Politics side-rail exit click: " +
                    exception.Message
                );
            }
        }

        private static void RefreshActiveKingdomPoliticsTabs()
        {
            try
            {
                // v1.3.8.4: do NOT hide and immediately re-enable the injected
                // side tab on every refresh. Repeated SetActive(false/true)
                // could interrupt Unity Selectable/Graphic state and made the
                // frame appear to vanish intermittently. We only hide it when
                // there is no fully opened KingdomWindow.
                KingdomWindow[] windows =
                    GetCachedKingdomWindows();

                if (windows == null)
                {
                    HidePoliticsSideButton();
                    RestoreAllKingdomPoliticsHostScrolls();
                    _kingdomPoliticsWindows.Clear();
                    return;
                }

                KingdomWindow activeWindow = null;
                for (int i = 0; i < windows.Length; i++)
                {
                    KingdomWindow window = windows[i];
                    if (
                        window == null ||
                        window.gameObject == null ||
                        !window.gameObject.activeInHierarchy ||
                        !IsKingdomWindowFullyOpen(window)
                    )
                    {
                        continue;
                    }

                    activeWindow = window;
                    break;
                }

                if (activeWindow == null)
                {
                    HidePoliticsSideButton();
                    RestoreAllKingdomPoliticsHostScrolls();
                    _kingdomPoliticsWindows.Clear();
                    return;
                }

                RestoreInactiveKingdomPoliticsHostScrolls(
                    activeWindow.GetInstanceID()
                );

                // First use WorldBox's actual tab system. Because the cloned
                // tab is parented to the same vanilla tab rail it naturally
                // follows the window animation and disappears with its
                // parent; no screen-space tracking is required.
                if (TryEnsureTrueNativeKingdomPoliticsTab(activeWindow))
                {
                    return;
                }

                // Fallback only: our proven overlay must wait until the
                // kingdom sheet stops moving.
                if (!IsKingdomWindowStableForPolitics(activeWindow))
                {
                    HidePoliticsSideButton();
                    return;
                }

                EnsureNativeKingdomPoliticsUi(activeWindow);
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not refresh native kingdom Politics tabs: " +
                    exception.Message
                );
            }
        }

        private static bool IsKingdomWindowStableForPolitics(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null || pWindow.gameObject == null ||
                !pWindow.gameObject.activeInHierarchy ||
                !IsKingdomWindowFullyOpen(pWindow))
            {
                return false;
            }

            RectTransform rect = pWindow.GetComponent<RectTransform>();
            if (rect == null)
            {
                return true;
            }

            Canvas canvas = pWindow.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(
                camera, corners[0]
            );
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(
                camera, corners[1]
            );
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(
                camera, corners[2]
            );

            Rect screenRect = new Rect(
                Mathf.Min(bl.x, tl.x),
                Mathf.Min(bl.y, tl.y),
                Mathf.Abs(tr.x - tl.x),
                Mathf.Abs(tl.y - bl.y)
            );

            int windowId = pWindow.GetInstanceID();
            if (_politicsStableWindowId != windowId ||
                !_politicsHasLastWindowScreenRect)
            {
                _politicsStableWindowId = windowId;
                _politicsLastWindowScreenRect = screenRect;
                _politicsHasLastWindowScreenRect = true;
                _politicsWindowStableSince = Time.unscaledTime;
                return false;
            }

            float delta = Mathf.Max(
                Mathf.Abs(screenRect.x - _politicsLastWindowScreenRect.x),
                Mathf.Abs(screenRect.y - _politicsLastWindowScreenRect.y),
                Mathf.Abs(screenRect.width - _politicsLastWindowScreenRect.width),
                Mathf.Abs(screenRect.height - _politicsLastWindowScreenRect.height)
            );

            _politicsLastWindowScreenRect = screenRect;

            if (delta > 1.25f)
            {
                _politicsWindowStableSince = Time.unscaledTime;
                return false;
            }

            if (_politicsWindowStableSince < 0f)
            {
                _politicsWindowStableSince = Time.unscaledTime;
                return false;
            }

            return Time.unscaledTime - _politicsWindowStableSince >= 0.10f;
        }

        private static void ResetPoliticsWindowStabilityTracking()
        {
            _politicsStableWindowId = -1;
            _politicsHasLastWindowScreenRect = false;
            _politicsWindowStableSince = -1f;
        }

        private static void HidePoliticsSideButton()
        {
            // A real WindowMetaTab is a child of the ScrollWindow and follows
            // the vanilla window lifecycle automatically. Never toggle its
            // activeSelf from the overlay lifecycle code.
            if (
                _kingdomPoliticsTrueNativeTab != null &&
                _kingdomPoliticsTrueNativeTab.gameObject != null
            )
            {
                return;
            }

            if (
                _kingdomPoliticsOverlayButton != null &&
                _kingdomPoliticsOverlayButton.gameObject != null
            )
            {
                _kingdomPoliticsOverlayButton.gameObject.SetActive(false);
            }
        }

        private static bool IsKingdomWindowFullyOpen(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null || pWindow.gameObject == null)
            {
                return false;
            }

            RectTransform rect = pWindow.GetComponent<RectTransform>();
            if (rect == null)
            {
                return pWindow.gameObject.activeInHierarchy;
            }

            Canvas canvas = pWindow.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (
                canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
            )
            {
                camera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[0]
            );
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[1]
            );
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[2]
            );

            float width = Mathf.Abs(tr.x - tl.x);
            float height = Mathf.Abs(tl.y - bl.y);

            // The full kingdom sheet occupies most of the screen vertically.
            // WorldBox also keeps a compact kingdom inspector along the
            // bottom; do not show the Politics overlay button for that mode.
            return width >= 300f && height >= Screen.height * 0.68f;
        }

        private static int GetNativeKingdomPoliticsPage(int pWindowId)
        {
            int page;
            if (!_kingdomPoliticsPages.TryGetValue(pWindowId, out page))
            {
                page = NativePoliticsPageOverview;
                _kingdomPoliticsPages[pWindowId] = page;
            }

            return Mathf.Clamp(
                page,
                NativePoliticsPageOverview,
                NativePoliticsPageHistory
            );
        }

        private static bool TryEnsureTrueNativeKingdomPoliticsTab(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null || pWindow.gameObject == null)
            {
                return false;
            }

            GameObject createdClone = null;

            try
            {
                ScrollWindow scrollWindow =
                    pWindow.GetComponentInParent<ScrollWindow>();
                WindowMetaTabButtonsContainer container =
                    scrollWindow != null ? scrollWindow.tabs : null;

                if (container == null)
                {
                    return false;
                }

                // Ensure WorldBox has populated its private tab registry before
                // we append our cloned tab.
                container.init();

                int windowId = pWindow.GetInstanceID();

                if (
                    _kingdomPoliticsTrueNativeTab != null &&
                    _kingdomPoliticsTrueNativeTab.gameObject != null &&
                    _kingdomPoliticsTrueNativeContainer == container &&
                    _kingdomPoliticsTrueNativeWindowId == windowId
                )
                {
                    _kingdomPoliticsTrueNativeTab.gameObject.SetActive(true);
                    _kingdomPoliticsOverlayButton =
                        _kingdomPoliticsTrueNativeTab.GetComponent<Button>();
                    return true;
                }

                WindowMetaTab[] existingTabs =
                    container.GetComponentsInChildren<WindowMetaTab>(true);

                for (int i = 0; i < existingTabs.Length; i++)
                {
                    WindowMetaTab existing = existingTabs[i];
                    if (
                        existing != null &&
                        existing.gameObject != null &&
                        existing.gameObject.name == NativePoliticsButtonName
                    )
                    {
                        ConfigureTrueNativePoliticsTab(
                            pWindow,
                            container,
                            existing
                        );
                        return true;
                    }
                }

                WindowMetaTab donor = container.tab_default;
                if (donor == null)
                {
                    for (int i = 0; i < existingTabs.Length; i++)
                    {
                        if (
                            existingTabs[i] != null &&
                            existingTabs[i].gameObject != null &&
                            existingTabs[i].gameObject.activeSelf
                        )
                        {
                            donor = existingTabs[i];
                            break;
                        }
                    }
                }

                if (
                    donor == null ||
                    donor.gameObject == null ||
                    donor.transform.parent == null
                )
                {
                    return false;
                }

                // Clone the WHOLE vanilla WindowMetaTab object. This keeps
                // the exact RectTransform, CanvasGroup, Image, Button,
                // TipButton, hover/click animation and child icon hierarchy.
                GameObject clone = UnityEngine.Object.Instantiate(
                    donor.gameObject,
                    donor.transform.parent,
                    false
                );
                createdClone = clone;
                clone.name = NativePoliticsButtonName;

                // Put Politics immediately before the donor. The donor's
                // native layout parent determines position/spacing.
                clone.transform.SetSiblingIndex(
                    donor.transform.GetSiblingIndex()
                );

                WindowMetaTab politicsTab =
                    clone.GetComponent<WindowMetaTab>();
                if (politicsTab == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return false;
                }

                ConfigureTrueNativePoliticsTab(
                    pWindow,
                    container,
                    politicsTab
                );
                createdClone = null;

                LogInfo(
                    "Kingdom Politics TRUE native WindowMetaTab created; " +
                    "donor=" + donor.gameObject.name +
                    ", parent=" + donor.transform.parent.name
                );
                return true;
            }
            catch (Exception exception)
            {
                if (createdClone != null)
                {
                    UnityEngine.Object.Destroy(createdClone);
                }

                LogWarning(
                    "True native Kingdom Politics tab unavailable; " +
                    "falling back to overlay: " + exception.Message
                );
                return false;
            }
        }

        private static void ConfigureTrueNativePoliticsTab(
            KingdomWindow pWindow,
            WindowMetaTabButtonsContainer pContainer,
            WindowMetaTab pTab
        )
        {
            if (pWindow == null || pContainer == null || pTab == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();

            // A cloned tab inherits the donor's serialized content/action.
            // Clear both before registering Political World.
            pTab.tab_elements.Clear();
            pTab.tab_action = new WindowMetaTabEvent();

            SetTrueNativeWindowMetaTabContainer(
                pTab,
                pContainer
            );
            RegisterTrueNativeWindowMetaTab(
                pContainer,
                pTab
            );

            TipButton tip = pTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = "ukiol_native_politics_tab_name";
                tip.textOnClickDescription =
                    "ukiol_native_politics_tab_desc";
                tip.text_description_2 = "";

                FieldInfo worldTipField =
                    typeof(WindowMetaTab).GetField(
                        "_worldtip_text",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );
                if (worldTipField != null)
                {
                    worldTipField.SetValue(
                        pTab,
                        pTab.getWorldTipText()
                    );
                }
            }

            // Start in the exact vanilla unselected state. From this point
            // WindowMetaTabButtonsContainer swaps this sprite itself.
            Image tabBackground = pTab.GetComponent<Image>();
            if (tabBackground != null)
            {
                Sprite offSprite = SpriteTextureLoader.getSprite(
                    "ui/tab_button_vertical"
                );
                if (offSprite != null)
                {
                    tabBackground.sprite = offSprite;
                }
            }

            ReplaceTrueNativePoliticsTabIcon(
                pTab,
                PoliticsIconPath
            );

            pTab.tab_action.AddListener(
                delegate(WindowMetaTab clickedTab)
                {
                    if (
                        pWindow == null ||
                        pWindow.gameObject == null ||
                        !pWindow.gameObject.activeInHierarchy
                    )
                    {
                        return;
                    }

                    _kingdomPoliticsWindows.Add(windowId);
                    if (!_kingdomPoliticsPages.ContainsKey(windowId))
                    {
                        _kingdomPoliticsPages[windowId] =
                            NativePoliticsPageOverview;
                    }

                    try
                    {
                        _openingNativeKingdomPolitics = true;
                        ShowNativeKingdomPoliticsPanel(
                            pWindow,
                            GetNativeKingdomPoliticsPage(windowId)
                        );
                        SyncTrueNativePoliticsTabContent(
                            pWindow
                        );
                        pContainer.showTab(clickedTab);
                    }
                    finally
                    {
                        _openingNativeKingdomPolitics = false;
                    }
                }
            );

            pTab.gameObject.SetActive(true);
            _kingdomPoliticsTrueNativeTab = pTab;
            _kingdomPoliticsTrueNativeContainer = pContainer;
            _kingdomPoliticsTrueNativeWindowId = windowId;
            _kingdomPoliticsOverlayButton = pTab.GetComponent<Button>();
        }

        private static void SetTrueNativeWindowMetaTabContainer(
            WindowMetaTab pTab,
            WindowMetaTabButtonsContainer pContainer
        )
        {
            FieldInfo field = typeof(WindowMetaTab).GetField(
                "container",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (field != null)
            {
                field.SetValue(pTab, pContainer);
            }
        }

        private static void RegisterTrueNativeWindowMetaTab(
            WindowMetaTabButtonsContainer pContainer,
            WindowMetaTab pTab
        )
        {
            FieldInfo tabsField =
                typeof(WindowMetaTabButtonsContainer).GetField(
                    "_tabs",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (tabsField == null)
            {
                throw new MissingFieldException(
                    "WindowMetaTabButtonsContainer._tabs"
                );
            }

            List<WindowMetaTab> tabs =
                tabsField.GetValue(pContainer) as List<WindowMetaTab>;

            if (tabs == null)
            {
                throw new InvalidOperationException(
                    "WindowMetaTabButtonsContainer._tabs is null"
                );
            }

            if (!tabs.Contains(pTab))
            {
                tabs.Add(pTab);
            }
        }

        private static void ReplaceTrueNativePoliticsTabIcon(
            WindowMetaTab pTab,
            string pIconPath
        )
        {
            if (pTab == null || string.IsNullOrEmpty(pIconPath))
            {
                return;
            }

            Sprite sprite = SpriteTextureLoader.getSprite(pIconPath);
            if (sprite == null)
            {
                return;
            }

            Image rootImage = pTab.GetComponent<Image>();
            Image[] images = pTab.GetComponentsInChildren<Image>(true);
            Image best = null;
            float bestArea = -1f;

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image == rootImage)
                {
                    continue;
                }

                RectTransform rect = image.rectTransform;
                if (rect == null)
                {
                    continue;
                }

                float width = Mathf.Abs(rect.rect.width);
                float height = Mathf.Abs(rect.rect.height);
                float area = width * height;

                if (
                    width >= 8f &&
                    height >= 8f &&
                    area > bestArea
                )
                {
                    best = image;
                    bestArea = area;
                }
            }

            if (best != null)
            {
                best.sprite = sprite;
                best.color = Color.white;
                best.enabled = true;
                best.preserveAspect = true;
            }
        }

        private static void SyncTrueNativePoliticsTabContent(
            KingdomWindow pWindow
        )
        {
            if (
                pWindow == null ||
                _kingdomPoliticsTrueNativeTab == null ||
                _kingdomPoliticsTrueNativeWindowId !=
                    pWindow.GetInstanceID()
            )
            {
                return;
            }

            Transform panel = null;
            RectTransform[] rects =
                pWindow.GetComponentsInChildren<RectTransform>(true);

            for (int i = 0; i < rects.Length; i++)
            {
                if (
                    rects[i] != null &&
                    rects[i].gameObject.name == NativePoliticsPanelName
                )
                {
                    panel = rects[i];
                    break;
                }
            }

            _kingdomPoliticsTrueNativeTab.tab_elements.Clear();
            if (panel != null)
            {
                _kingdomPoliticsTrueNativeTab.tab_elements.Add(panel);
            }
        }

        private static void EnsureNativeKingdomPoliticsUi(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return;
            }

            // Preferred path: WorldBox's own WindowMetaTab system. Only if
            // that cannot be resolved on this game build do we enter the
            // legacy overlay fallback below.
            if (TryEnsureTrueNativeKingdomPoliticsTab(pWindow))
            {
                return;
            }

            try
            {
                Button existing = FindButtonByName(
                    pWindow,
                    NativePoliticsButtonName
                );

                if (existing != null)
                {
                    _kingdomPoliticsOverlayButton = existing;
                    PositionAnchoredPoliticsOverlayButton(
                        existing,
                        pWindow
                    );
                    ApplySpriteToButton(
                        existing,
                        PoliticsIconPath
                    );
                    EnsurePoliticsSideButtonVisuals(existing);
                    return;
                }

                // 1.3.6.4: the visible kingdom side tabs in WorldBox 0.51.2
                // are not ordinary UnityEngine.UI.Button objects. Previous
                // versions therefore locked onto unrelated Button groups
                // such as content_info_left. Discover the rail by its
                // RectTransforms instead and create our own clickable tab
                // inside the same native parent.
                List<RectTransform> nativeSideRects =
                    FindLikelyKingdomSideTabRects(pWindow);

                if (nativeSideRects.Count == 0)
                {
                    Button anchoredFallback =
                        CreateAnchoredPoliticsSideButton(pWindow);

                    if (anchoredFallback != null)
                    {
                        _kingdomPoliticsOverlayButton = anchoredFallback;
                        ApplySpriteToButton(
                            anchoredFallback,
                            PoliticsIconPath
                        );
                        EnsurePoliticsSideButtonVisuals(anchoredFallback);
                        LogInfo(
                            "Kingdom Politics anchored fallback tab created."
                        );
                    }
                    return;
                }

                RectTransform donorRect = ChooseNativeTabVisualDonor(
                    nativeSideRects
                );

                if (donorRect == null || donorRect.parent == null)
                {
                    Button anchoredFallback =
                        CreateAnchoredPoliticsSideButton(pWindow);
                    if (anchoredFallback != null)
                    {
                        _kingdomPoliticsOverlayButton = anchoredFallback;
                        ApplySpriteToButton(
                            anchoredFallback,
                            PoliticsIconPath
                        );
                        EnsurePoliticsSideButtonVisuals(anchoredFallback);
                        LogInfo(
                            "Kingdom Politics anchored fallback tab created after donor miss."
                        );
                    }
                    return;
                }

                Button politicsButton = CreateNativePoliticsSideButton(
                    pWindow,
                    donorRect,
                    nativeSideRects
                );

                if (politicsButton == null)
                {
                    return;
                }

                ApplySpriteToButton(
                    politicsButton,
                    PoliticsIconPath
                );
                EnsurePoliticsSideButtonVisuals(politicsButton);

                LogInfo(
                    "Kingdom Politics native tab created under " +
                    donorRect.parent.name + " using donor " +
                    donorRect.gameObject.name
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not create native Politics side tab: " +
                    exception
                );
            }
        }

        private static Button FindButtonByName(
            Component pRoot,
            string pName
        )
        {
            if (string.IsNullOrEmpty(pName))
            {
                return null;
            }

            // v1.3.6.2: side tabs in WorldBox 0.51.2 are not guaranteed to
            // be descendants of KingdomWindow. Search all loaded UI buttons
            // for our exact injected name instead of relying on hierarchy.
            Button[] allButtons =
                Resources.FindObjectsOfTypeAll<Button>();

            if (allButtons == null)
            {
                return null;
            }

            for (int i = 0; i < allButtons.Length; i++)
            {
                Button button = allButtons[i];
                if (
                    button != null &&
                    button.gameObject != null &&
                    button.gameObject.name == pName
                )
                {
                    return button;
                }
            }

            return null;
        }

        private static Transform GetKingdomWindowUiSearchRoot(
            KingdomWindow pWindow
        )
        {
            // Retained for compatibility with the rest of the UI code.
            // Side-tab discovery itself no longer depends on this hierarchy.
            return pWindow != null ? pWindow.transform : null;
        }

        private static List<Button> FindLikelyKingdomSideTabButtons(
            KingdomWindow pWindow
        )
        {
            List<Button> result = new List<Button>();
            List<RectTransform> rects =
                FindLikelyKingdomSideTabRects(pWindow);

            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i] == null)
                {
                    continue;
                }

                Button button = rects[i].GetComponent<Button>();
                if (button != null && !result.Contains(button))
                {
                    result.Add(button);
                }
            }

            return result;
        }

        private static List<RectTransform> FindLikelyKingdomSideTabRects(
            KingdomWindow pWindow
        )
        {
            List<RectTransform> result = new List<RectTransform>();
            if (pWindow == null)
            {
                return result;
            }

            RectTransform windowRect =
                pWindow.GetComponent<RectTransform>();
            if (windowRect == null)
            {
                return result;
            }

            Vector3[] corners = new Vector3[4];
            windowRect.GetWorldCorners(corners);
            float left = Mathf.Min(corners[0].x, corners[1].x);
            float right = Mathf.Max(corners[2].x, corners[3].x);
            float bottom = Mathf.Min(corners[0].y, corners[3].y);
            float top = Mathf.Max(corners[1].y, corners[2].y);
            float windowHeight = Mathf.Max(1f, top - bottom);

            RectTransform[] rects =
                Resources.FindObjectsOfTypeAll<RectTransform>();

            if (rects == null)
            {
                return result;
            }

            // The actual side rail sits immediately outside the left edge of
            // the kingdom window. Keep this band deliberately narrow so we do
            // not select the "content_info_left" statistics icons that were
            // reported at x ~= left - 179 in the 1.3.6.3 diagnostic.
            float minX = left - 105f;
            float maxX = left - 8f;
            float minY = bottom + 15f;
            float maxY = top - 55f;

            List<RectTransform> candidates = new List<RectTransform>();

            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (
                    rect == null ||
                    rect.gameObject == null ||
                    rect.gameObject.name == NativePoliticsButtonName ||
                    !rect.gameObject.activeInHierarchy
                )
                {
                    continue;
                }

                Vector3 pos = rect.position;
                if (
                    pos.x < minX || pos.x > maxX ||
                    pos.y < minY || pos.y > maxY
                )
                {
                    continue;
                }

                float w = Mathf.Abs(rect.rect.width);
                float h = Mathf.Abs(rect.rect.height);

                // Native side-tab roots are compact. This also removes long
                // labels and large container panels while still allowing
                // scaled UI themes.
                if (
                    w < 24f || h < 24f ||
                    w > 105f || h > 105f
                )
                {
                    continue;
                }

                if (rect.parent == null)
                {
                    continue;
                }

                candidates.Add(rect);
            }

            Dictionary<Transform, List<RectTransform>> groups =
                new Dictionary<Transform, List<RectTransform>>();

            for (int i = 0; i < candidates.Count; i++)
            {
                RectTransform rect = candidates[i];
                Transform parent = rect.parent;

                List<RectTransform> group;
                if (!groups.TryGetValue(parent, out group))
                {
                    group = new List<RectTransform>();
                    groups[parent] = group;
                }

                group.Add(rect);
            }

            Transform bestParent = null;
            List<RectTransform> bestGroup = null;
            float bestScore = float.MinValue;
            float expectedX = left - 48f;

            foreach (
                KeyValuePair<Transform, List<RectTransform>> pair
                in groups
            )
            {
                List<RectTransform> group = pair.Value;
                if (group == null || group.Count < 3)
                {
                    continue;
                }

                float minGroupX = float.MaxValue;
                float maxGroupX = float.MinValue;
                float minGroupY = float.MaxValue;
                float maxGroupY = float.MinValue;
                float sumX = 0f;
                int visibleCount = 0;
                int imageCount = 0;

                for (int i = 0; i < group.Count; i++)
                {
                    RectTransform rect = group[i];
                    if (rect == null)
                    {
                        continue;
                    }

                    Vector3 pos = rect.position;
                    minGroupX = Mathf.Min(minGroupX, pos.x);
                    maxGroupX = Mathf.Max(maxGroupX, pos.x);
                    minGroupY = Mathf.Min(minGroupY, pos.y);
                    maxGroupY = Mathf.Max(maxGroupY, pos.y);
                    sumX += pos.x;
                    visibleCount++;

                    if (
                        rect.GetComponent<Image>() != null ||
                        rect.GetComponentInChildren<Image>(true) != null
                    )
                    {
                        imageCount++;
                    }
                }

                if (visibleCount < 3)
                {
                    continue;
                }

                float xSpread = maxGroupX - minGroupX;
                float ySpread = maxGroupY - minGroupY;
                float avgX = sumX / visibleCount;

                if (xSpread > 26f || ySpread < 80f)
                {
                    continue;
                }

                float score =
                    visibleCount * 1200f +
                    imageCount * 250f +
                    ySpread * 2.5f -
                    Mathf.Abs(avgX - expectedX) * 18f -
                    xSpread * 30f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestParent = pair.Key;
                    bestGroup = group;
                }
            }

            if (bestGroup != null)
            {
                result.AddRange(bestGroup);
            }

            result.Sort(
                delegate(RectTransform a, RectTransform b)
                {
                    if (a == null || b == null)
                    {
                        return 0;
                    }

                    return b.position.y.CompareTo(a.position.y);
                }
            );

            int windowId = pWindow.GetInstanceID();
            if (!_kingdomPoliticsUiScanLogged.Contains(windowId))
            {
                _kingdomPoliticsUiScanLogged.Add(windowId);

                string sample = string.Empty;
                int sampleCount = Mathf.Min(12, result.Count);
                for (int i = 0; i < sampleCount; i++)
                {
                    RectTransform rect = result[i];
                    if (rect == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(sample))
                    {
                        sample += ", ";
                    }

                    string componentTypes = string.Empty;
                    Component[] components = rect.GetComponents<Component>();
                    for (
                        int componentIndex = 0;
                        componentIndex < components.Length;
                        componentIndex++
                    )
                    {
                        Component component = components[componentIndex];
                        if (component == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(componentTypes))
                        {
                            componentTypes += "|";
                        }

                        componentTypes += component.GetType().Name;
                    }

                    sample += rect.gameObject.name + "/" +
                        rect.parent.name + "@" +
                        Mathf.RoundToInt(rect.position.x) + ":" +
                        Mathf.RoundToInt(rect.position.y) +
                        "[" + componentTypes + "]";
                }

                LogInfo(
                    "Kingdom Politics UI scan v4: rects=" +
                    rects.Length +
                    ", candidates=" + candidates.Count +
                    ", rail=" + result.Count +
                    ", parent=" +
                    (bestParent != null ? bestParent.name : "<none>") +
                    ", windowRect=" +
                    Mathf.RoundToInt(left) + "," +
                    Mathf.RoundToInt(bottom) + ".." +
                    Mathf.RoundToInt(right) + "," +
                    Mathf.RoundToInt(top) +
                    (string.IsNullOrEmpty(sample)
                        ? string.Empty
                        : ", sample=[" + sample + "]")
                );
            }

            return result;
        }

        private static RectTransform ChooseNativeTabVisualDonor(
            List<RectTransform> pRects
        )
        {
            if (pRects == null || pRects.Count == 0)
            {
                return null;
            }

            RectTransform best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < pRects.Count; i++)
            {
                RectTransform rect = pRects[i];
                if (rect == null)
                {
                    continue;
                }

                float score = 0f;

                Image rootImage = rect.GetComponent<Image>();
                Image childImage =
                    rect.GetComponentInChildren<Image>(true);

                if (rootImage != null)
                {
                    score += 100f;
                }

                if (childImage != null)
                {
                    score += 40f;
                }

                if (rect.childCount > 0)
                {
                    score += 20f;
                }

                float w = Mathf.Abs(rect.rect.width);
                float h = Mathf.Abs(rect.rect.height);
                score -= Mathf.Abs(w - h) * 0.5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = rect;
                }
            }

            return best ?? pRects[0];
        }

        private static Button CreateAnchoredPoliticsSideButton(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null || pWindow.gameObject == null)
            {
                return null;
            }

            RectTransform windowRect =
                pWindow.GetComponent<RectTransform>();
            if (windowRect == null)
            {
                return null;
            }

            // 1.3.6.6: do not rely on the internal WorldBox window hierarchy.
            // The previous build proved that the object was being created,
            // but it could still be invisible because the shared "windows"
            // canvas controls its own draw order. A dedicated overlay canvas
            // guarantees that the tab is rendered above the vanilla window.
            GameObject overlayObject = new GameObject(
                "ukiol_kingdom_politics_overlay_canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            Canvas overlayCanvas = overlayObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 32000;

            CanvasScaler scaler = overlayObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            GameObject gameObject = new GameObject(
                NativePoliticsButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline)
            );
            gameObject.transform.SetParent(overlayObject.transform, false);

            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(54f, 54f);

            Image background = gameObject.GetComponent<Image>();
            Sprite frameSprite =
                SpriteTextureLoader.getSprite("ukiol/icons/tab_frame");
            if (frameSprite != null)
            {
                background.sprite = frameSprite;
                background.color = Color.white;
                background.preserveAspect = true;
            }
            else
            {
                // Deliberately visible diagnostic fallback if the frame asset
                // somehow fails to load.
                background.color = new Color(0.37f, 0.30f, 0.16f, 1f);
            }
            background.raycastTarget = true;

            Outline outline = gameObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.07f, 0.05f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(gameObject.transform, false);

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.13f, 0.13f);
            iconRect.anchorMax = new Vector2(0.87f, 0.87f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.localScale = Vector3.one;

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite =
                SpriteTextureLoader.getSprite(PoliticsIconPath);
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.72f, 1f);
            colors.pressedColor = new Color(0.92f, 0.55f, 0.36f, 1f);
            colors.selectedColor = new Color(0.93f, 0.42f, 0.31f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                delegate
                {
                    OpenNativeKingdomPolitics(pWindow);
                }
            );

            PositionAnchoredPoliticsOverlayButton(button, pWindow);
            gameObject.SetActive(true);

            _kingdomPoliticsOverlayButton = button;

            LogInfo(
                "Kingdom Politics guaranteed overlay tab created; canvas=" +
                overlayCanvas.renderMode +
                ", sortingOrder=" + overlayCanvas.sortingOrder
            );

            return button;
        }

        private static void PositionAnchoredPoliticsOverlayButton(
            Button pButton,
            KingdomWindow pWindow
        )
        {
            if (
                pButton == null ||
                pWindow == null ||
                pWindow.gameObject == null
            )
            {
                return;
            }

            RectTransform windowRect =
                pWindow.GetComponent<RectTransform>();
            RectTransform buttonRect =
                pButton.GetComponent<RectTransform>();
            if (windowRect == null || buttonRect == null)
            {
                return;
            }

            Canvas sourceCanvas = pWindow.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (
                sourceCanvas != null &&
                sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            )
            {
                uiCamera = sourceCanvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            windowRect.GetWorldCorners(corners);
            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[0]
            );
            Vector2 screenTL = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[1]
            );
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[2]
            );

            float left = Mathf.Min(screenBL.x, screenTL.x);
            float bottom = screenBL.y;
            float top = Mathf.Max(screenTL.y, screenTR.y);
            float windowHeight = Mathf.Max(1f, top - bottom);

            float size = Mathf.Clamp(windowHeight * 0.062f, 48f, 54f);

            // v1.3.8.2: the guaranteed overlay tab used to be a few pixels
            // wider / farther left than WorldBox's real side tabs. Visually
            // that made Politics look like a detached button instead of part
            // of the kingdom-window rail. Read the currently visible vanilla
            // rail geometry and mirror its center, size and frame sprite.
            float screenX = left - size * 0.78f;
            Image nativeTabDonor;
            float nativeTabX;
            float nativeTabSize;
            if (
                TryGetVisibleNativeKingdomSideTabGeometry(
                    pWindow,
                    out nativeTabDonor,
                    out nativeTabX,
                    out nativeTabSize
                )
            )
            {
                screenX = nativeTabX;
                size = nativeTabSize;

                // Geometry comes from the native rail, but the Politics frame
                // itself stays on our stable tab_frame sprite. Some native
                // donor Images are state-dependent children; copying their
                // sprite/color here could leave our root frame transparent.
            }

            buttonRect.sizeDelta = new Vector2(size, size);

            // Y remains controlled by our proven overlay placement. Only the
            // rail's horizontal geometry is inherited, so opening Politics
            // cannot disturb WorldBox's own tabs or their layout.
            float screenY = bottom + windowHeight * 0.31f;
            buttonRect.anchoredPosition = new Vector2(screenX, screenY);

            pButton.gameObject.SetActive(true);
            EnsurePoliticsSideButtonVisuals(pButton);

            int windowId = pWindow.GetInstanceID();
            if (!_kingdomPoliticsUiScanLogged.Contains(windowId + 9000000))
            {
                _kingdomPoliticsUiScanLogged.Add(windowId + 9000000);
                LogInfo(
                    "Kingdom Politics overlay screen position: x=" +
                    Mathf.RoundToInt(screenX) +
                    ", y=" + Mathf.RoundToInt(screenY) +
                    ", size=" + Mathf.RoundToInt(size) +
                    ", screen=" + Screen.width + "x" + Screen.height
                );
            }
        }

        private static bool TryGetVisibleNativeKingdomSideTabGeometry(
            KingdomWindow pWindow,
            out Image pDonor,
            out float pScreenX,
            out float pSize
        )
        {
            pDonor = null;
            pScreenX = 0f;
            pSize = 0f;

            if (pWindow == null || pWindow.gameObject == null)
            {
                return false;
            }

            RectTransform windowRect =
                pWindow.GetComponent<RectTransform>();
            if (windowRect == null)
            {
                return false;
            }

            Canvas windowCanvas = pWindow.GetComponentInParent<Canvas>();
            Camera windowCamera = null;
            if (
                windowCanvas != null &&
                windowCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            )
            {
                windowCamera = windowCanvas.worldCamera;
            }

            Vector3[] windowCorners = new Vector3[4];
            windowRect.GetWorldCorners(windowCorners);
            Vector2 windowBL = RectTransformUtility.WorldToScreenPoint(
                windowCamera,
                windowCorners[0]
            );
            Vector2 windowTL = RectTransformUtility.WorldToScreenPoint(
                windowCamera,
                windowCorners[1]
            );
            Vector2 windowTR = RectTransformUtility.WorldToScreenPoint(
                windowCamera,
                windowCorners[2]
            );

            float left = Mathf.Min(windowBL.x, windowTL.x);
            float bottom = windowBL.y;
            float top = Mathf.Max(windowTL.y, windowTR.y);

            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            if (images == null || images.Length == 0)
            {
                return false;
            }

            List<Image> candidates = new List<Image>();
            List<float> candidateXs = new List<float>();
            List<float> candidateSizes = new List<float>();

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (
                    image == null ||
                    image.gameObject == null ||
                    !image.gameObject.activeInHierarchy ||
                    image.sprite == null ||
                    image.gameObject.name == NativePoliticsButtonName ||
                    image.transform.IsChildOf(
                        _kingdomPoliticsOverlayButton != null
                            ? _kingdomPoliticsOverlayButton.transform
                            : null
                    )
                )
                {
                    continue;
                }

                RectTransform rect = image.rectTransform;
                if (rect == null)
                {
                    continue;
                }

                Canvas canvas = image.GetComponentInParent<Canvas>();
                Camera camera = null;
                if (
                    canvas != null &&
                    canvas.renderMode != RenderMode.ScreenSpaceOverlay
                )
                {
                    camera = canvas.worldCamera;
                }

                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector2 bl = RectTransformUtility.WorldToScreenPoint(
                    camera, corners[0]
                );
                Vector2 tl = RectTransformUtility.WorldToScreenPoint(
                    camera, corners[1]
                );
                Vector2 tr = RectTransformUtility.WorldToScreenPoint(
                    camera, corners[2]
                );

                float width = Mathf.Abs(tr.x - tl.x);
                float height = Mathf.Abs(tl.y - bl.y);
                if (
                    width < 42f || height < 42f ||
                    width > 72f || height > 72f ||
                    Mathf.Abs(width - height) > 9f
                )
                {
                    continue;
                }

                float centerX = (tl.x + tr.x) * 0.5f;
                float centerY = (bl.y + tl.y) * 0.5f;
                if (
                    centerX < left - 95f ||
                    centerX > left - 8f ||
                    centerY < bottom + 15f ||
                    centerY > top - 35f
                )
                {
                    continue;
                }

                candidates.Add(image);
                candidateXs.Add(centerX);
                candidateSizes.Add((width + height) * 0.5f);
            }

            if (candidates.Count < 3)
            {
                return false;
            }

            int bestIndex = -1;
            float bestScore = float.MinValue;
            float expectedX = left - 40f;

            for (int i = 0; i < candidates.Count; i++)
            {
                int sameRailCount = 0;
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (Mathf.Abs(candidateXs[j] - candidateXs[i]) <= 5f)
                    {
                        sameRailCount++;
                    }
                }

                float score =
                    sameRailCount * 1000f -
                    Mathf.Abs(candidateXs[i] - expectedX) * 12f -
                    Mathf.Abs(candidateSizes[i] - 52f) * 5f;

                if (candidates[i].transform.childCount > 0)
                {
                    score += 100f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            List<float> railXs = new List<float>();
            List<float> railSizes = new List<float>();
            float bestX = candidateXs[bestIndex];

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Mathf.Abs(candidateXs[i] - bestX) <= 5f)
                {
                    railXs.Add(candidateXs[i]);
                    railSizes.Add(candidateSizes[i]);
                }
            }

            if (railXs.Count < 3)
            {
                return false;
            }

            railXs.Sort();
            railSizes.Sort();
            pScreenX = railXs[railXs.Count / 2];
            pSize = Mathf.Clamp(
                railSizes[railSizes.Count / 2],
                46f,
                58f
            );
            pDonor = candidates[bestIndex];
            return true;
        }

        private static Button CreateNativePoliticsSideButton(
            KingdomWindow pWindow,
            RectTransform pDonorRect,
            List<RectTransform> pNativeRects
        )
        {
            if (
                pWindow == null ||
                pDonorRect == null ||
                pDonorRect.parent == null
            )
            {
                return null;
            }

            GameObject gameObject = new GameObject(
                NativePoliticsButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            gameObject.transform.SetParent(
                pDonorRect.parent,
                false
            );

            RectTransform rect =
                gameObject.GetComponent<RectTransform>();

            rect.anchorMin = pDonorRect.anchorMin;
            rect.anchorMax = pDonorRect.anchorMax;
            rect.pivot = pDonorRect.pivot;
            rect.sizeDelta = pDonorRect.sizeDelta;
            rect.localScale = pDonorRect.localScale;
            rect.localRotation = pDonorRect.localRotation;

            Image background = gameObject.GetComponent<Image>();
            Image donorImage = pDonorRect.GetComponent<Image>();

            if (donorImage != null)
            {
                background.sprite = donorImage.sprite;
                background.overrideSprite = donorImage.overrideSprite;
                background.color = donorImage.color;
                background.material = donorImage.material;
                background.type = donorImage.type;
                background.preserveAspect = donorImage.preserveAspect;
                background.fillCenter = donorImage.fillCenter;
                background.raycastTarget = true;
            }
            else
            {
                // A neutral native-like plate. The actual mod icon is rendered
                // by a dedicated child Image below.
                background.color = new Color(
                    0.28f,
                    0.31f,
                    0.25f,
                    0.98f
                );
            }

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(gameObject.transform, false);

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.16f, 0.16f);
            iconRect.anchorMax = new Vector2(0.84f, 0.84f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.localScale = Vector3.one;

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite =
                SpriteTextureLoader.getSprite(PoliticsIconPath);
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                delegate
                {
                    OpenNativeKingdomPolitics(pWindow);
                }
            );

            PositionNativePoliticsRect(
                rect,
                pNativeRects
            );

            gameObject.transform.SetAsLastSibling();
            gameObject.SetActive(true);

            return button;
        }

        private static void PositionNativePoliticsRect(
            RectTransform pRect,
            List<RectTransform> pNativeRects
        )
        {
            if (
                pRect == null ||
                pNativeRects == null ||
                pNativeRects.Count == 0
            )
            {
                return;
            }

            Transform parent = pRect.parent;

            if (
                parent.GetComponent<VerticalLayoutGroup>() != null ||
                parent.GetComponent<GridLayoutGroup>() != null
            )
            {
                pRect.SetAsLastSibling();
                return;
            }

            List<float> ys = new List<float>();
            float averageX = 0f;
            float averageZ = 0f;
            int count = 0;

            for (int i = 0; i < pNativeRects.Count; i++)
            {
                RectTransform rect = pNativeRects[i];
                if (rect == null)
                {
                    continue;
                }

                Vector3 pos = rect.position;
                ys.Add(pos.y);
                averageX += pos.x;
                averageZ += pos.z;
                count++;
            }

            if (count == 0)
            {
                return;
            }

            averageX /= count;
            averageZ /= count;
            ys.Sort();
            ys.Reverse();

            float fallbackSpacing =
                Mathf.Max(42f, Mathf.Abs(pRect.rect.height) + 4f);
            float spacing = fallbackSpacing;

            List<float> gaps = new List<float>();
            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float gap = ys[i] - ys[i + 1];
                if (gap > 15f && gap < 110f)
                {
                    gaps.Add(gap);
                }
            }

            if (gaps.Count > 0)
            {
                gaps.Sort();
                spacing = gaps[gaps.Count / 2];
            }

            float targetY = ys[ys.Count - 1] - spacing;

            // If vanilla leaves a visibly larger gap inside the rail, prefer
            // filling that gap instead of extending the rail downward.
            float largestGap = 0f;
            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float gap = ys[i] - ys[i + 1];
                if (
                    gap > largestGap &&
                    gap >= spacing * 1.65f
                )
                {
                    largestGap = gap;
                    targetY = (ys[i] + ys[i + 1]) * 0.5f;
                }
            }

            pRect.position = new Vector3(
                averageX,
                targetY,
                averageZ
            );
        }

        private static void HookNativeKingdomTabExitButtons(
            KingdomWindow pWindow,
            List<Button> pButtons
        )
        {
            if (pWindow == null || pButtons == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();

            for (int i = 0; i < pButtons.Count; i++)
            {
                Button button = pButtons[i];
                if (button == null)
                {
                    continue;
                }

                int buttonId = button.GetInstanceID();
                if (_kingdomTabButtonsHooked.Contains(buttonId))
                {
                    continue;
                }

                _kingdomTabButtonsHooked.Add(buttonId);

                button.onClick.AddListener(
                    delegate
                    {
                        if (_openingNativeKingdomPolitics)
                        {
                            return;
                        }

                        _kingdomPoliticsWindows.Remove(windowId);
                    }
                );
            }
        }

        private static void PositionNativePoliticsSideButton(
            Button pButton,
            List<Button> pNativeButtons
        )
        {
            if (
                pButton == null ||
                pNativeButtons == null ||
                pNativeButtons.Count == 0
            )
            {
                return;
            }

            RectTransform rect =
                pButton.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            if (
                pButton.transform.parent.GetComponent<VerticalLayoutGroup>() != null ||
                pButton.transform.parent.GetComponent<GridLayoutGroup>() != null
            )
            {
                pButton.transform.SetAsLastSibling();
                return;
            }

            List<float> ys = new List<float>();
            float averageX = 0f;
            float averageZ = 0f;
            for (int i = 0; i < pNativeButtons.Count; i++)
            {
                if (pNativeButtons[i] == null)
                {
                    continue;
                }

                Vector3 pos = pNativeButtons[i].transform.position;
                ys.Add(pos.y);
                averageX += pos.x;
                averageZ += pos.z;
            }

            if (ys.Count == 0)
            {
                return;
            }

            averageX /= ys.Count;
            averageZ /= ys.Count;
            ys.Sort();
            ys.Reverse();

            float buttonHeight = Mathf.Max(32f, Mathf.Abs(rect.rect.height));
            float normalSpacing = buttonHeight + 4f;
            float smallestReasonableGap = float.MaxValue;

            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float gap = Mathf.Abs(ys[i] - ys[i + 1]);
                if (gap >= buttonHeight * 0.75f && gap < smallestReasonableGap)
                {
                    smallestReasonableGap = gap;
                }
            }

            if (smallestReasonableGap < float.MaxValue)
            {
                normalSpacing = smallestReasonableGap;
            }

            float targetY = ys[ys.Count - 1] - normalSpacing;
            float largestGap = 0f;

            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float gap = ys[i] - ys[i + 1];
                if (gap > largestGap && gap >= normalSpacing * 1.55f)
                {
                    largestGap = gap;
                    targetY = (ys[i] + ys[i + 1]) * 0.5f;
                }
            }

            rect.position = new Vector3(
                averageX,
                targetY,
                averageZ
            );
        }

        private static void ApplySpriteToButton(
            Button pButton,
            string pSpritePath
        )
        {
            if (
                pButton == null ||
                string.IsNullOrEmpty(pSpritePath)
            )
            {
                return;
            }

            Sprite sprite = SpriteTextureLoader.getSprite(pSpritePath);
            if (sprite == null)
            {
                return;
            }

            Image[] images =
                pButton.GetComponentsInChildren<Image>(true);
            Image best = null;
            float bestArea = -1f;

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (
                    image == null ||
                    image.gameObject == pButton.gameObject
                )
                {
                    continue;
                }

                RectTransform r = image.rectTransform;
                if (r == null)
                {
                    continue;
                }

                float w = Mathf.Abs(r.rect.width);
                float h = Mathf.Abs(r.rect.height);
                float area = w * h;

                if (
                    w >= 12f && h >= 12f &&
                    w <= 64f && h <= 64f &&
                    area > bestArea
                )
                {
                    best = image;
                    bestArea = area;
                }
            }

            if (best == null)
            {
                best = pButton.GetComponent<Image>();
            }

            if (best != null)
            {
                best.sprite = sprite;
                best.color = Color.white;
                best.preserveAspect = true;
                best.enabled = true;
            }
        }

        private static void EnsurePoliticsSideButtonVisuals(
            Button pButton
        )
        {
            if (pButton == null || pButton.gameObject == null)
            {
                return;
            }

            Image background = pButton.GetComponent<Image>();
            if (background != null)
            {
                Sprite frameSprite = SpriteTextureLoader.getSprite(
                    "ukiol/icons/tab_frame"
                );
                if (frameSprite != null)
                {
                    background.sprite = frameSprite;
                    background.overrideSprite = null;
                    background.type = Image.Type.Simple;
                    background.preserveAspect = true;
                }

                background.color = Color.white;
                background.enabled = true;
                background.raycastTarget = true;
                if (background.canvasRenderer != null)
                {
                    background.canvasRenderer.SetAlpha(1f);
                }
                pButton.targetGraphic = background;
            }

            Transform iconTransform = pButton.transform.Find("icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    Sprite politicsSprite = SpriteTextureLoader.getSprite(
                        PoliticsIconPath
                    );
                    if (politicsSprite != null)
                    {
                        icon.sprite = politicsSprite;
                    }
                    icon.color = Color.white;
                    icon.enabled = true;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    if (icon.canvasRenderer != null)
                    {
                        icon.canvasRenderer.SetAlpha(1f);
                    }
                }
            }

            pButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = pButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.72f, 1f);
            colors.pressedColor = new Color(0.92f, 0.55f, 0.36f, 1f);
            colors.selectedColor = new Color(0.93f, 0.42f, 0.31f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            pButton.colors = colors;
            pButton.interactable = true;
        }

        private static void OpenNativeKingdomPolitics(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _kingdomPoliticsWindows.Add(windowId);

            if (!_kingdomPoliticsPages.ContainsKey(windowId))
            {
                _kingdomPoliticsPages[windowId] =
                    NativePoliticsPageOverview;
            }

            try
            {
                _openingNativeKingdomPolitics = true;
                ShowNativeKingdomPoliticsPanel(
                    pWindow,
                    GetNativeKingdomPoliticsPage(windowId)
                );
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not open embedded Politics page: " +
                    exception
                );
            }
            finally
            {
                _openingNativeKingdomPolitics = false;
            }
        }

        private static void SelectNativeKingdomPoliticsPage(
            KingdomWindow pWindow,
            int pPage
        )
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _kingdomPoliticsWindows.Add(windowId);
            _kingdomPoliticsPages[windowId] = Mathf.Clamp(
                pPage,
                NativePoliticsPageOverview,
                NativePoliticsPageHistory
            );
            _kingdomPoliticsAddonPageIds.Remove(windowId);

            // Clicking any top Politics page is an explicit navigation action.
            // In particular, clicking Parties while a profile is open returns
            // to the normal party list.
            _kingdomPoliticsSelectedPartyIds.Remove(windowId);
            _kingdomPoliticsSelectedPartyKingdomIds.Remove(windowId);

            OpenNativeKingdomPolitics(pWindow);
        }

        private static void SelectNativeKingdomAddonPoliticsPage(
            KingdomWindow pWindow,
            string pPageId
        )
        {
            if (pWindow == null || string.IsNullOrEmpty(pPageId))
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _kingdomPoliticsWindows.Add(windowId);
            _kingdomPoliticsAddonPageIds[windowId] = pPageId;
            _kingdomPoliticsSelectedPartyIds.Remove(windowId);
            _kingdomPoliticsSelectedPartyKingdomIds.Remove(windowId);
            OpenNativeKingdomPolitics(pWindow);
        }

        private static void OpenNativeKingdomPartyProfile(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            string pPartyId
        )
        {
            if (
                pWindow == null ||
                pKingdom == null ||
                string.IsNullOrEmpty(pPartyId)
            )
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _kingdomPoliticsWindows.Add(windowId);
            _kingdomPoliticsPages[windowId] =
                NativePoliticsPageParties;
            _kingdomPoliticsSelectedPartyIds[windowId] = pPartyId;
            _kingdomPoliticsSelectedPartyKingdomIds[windowId] =
                GetStableObjectIdentity(pKingdom);

            OpenNativeKingdomPolitics(pWindow);
        }

        private static void CloseNativeKingdomPartyProfile(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _kingdomPoliticsSelectedPartyIds.Remove(windowId);
            _kingdomPoliticsSelectedPartyKingdomIds.Remove(windowId);
            _kingdomPoliticsPages[windowId] =
                NativePoliticsPageParties;

            OpenNativeKingdomPolitics(pWindow);
        }

        private static void HideNativeKingdomPoliticsPanel(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return;
            }

            Transform panel = null;
            RectTransform[] rects =
                pWindow.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (
                    rects[i] != null &&
                    rects[i].gameObject.name == NativePoliticsPanelName
                )
                {
                    panel = rects[i];
                    break;
                }
            }

            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            RestoreKingdomPoliticsHostScroll(pWindow);
        }

        private static void RestoreKingdomPoliticsHostScroll(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            ScrollRect hostScroll;
            if (
                !_kingdomPoliticsHostScrolls.TryGetValue(
                    windowId,
                    out hostScroll
                ) ||
                hostScroll == null
            )
            {
                _kingdomPoliticsHostScrolls.Remove(windowId);
                _kingdomPoliticsHostScrollEnabled.Remove(windowId);
                return;
            }

            bool wasEnabled = true;
            _kingdomPoliticsHostScrollEnabled.TryGetValue(
                windowId,
                out wasEnabled
            );

            hostScroll.enabled = wasEnabled;
            _kingdomPoliticsHostScrolls.Remove(windowId);
            _kingdomPoliticsHostScrollEnabled.Remove(windowId);
        }

        private static void RestoreAllKingdomPoliticsHostScrolls()
        {
            if (_kingdomPoliticsHostScrolls.Count == 0)
            {
                return;
            }

            List<int> ids = new List<int>(
                _kingdomPoliticsHostScrolls.Keys
            );
            for (int i = 0; i < ids.Count; i++)
            {
                int windowId = ids[i];
                ScrollRect hostScroll;
                if (
                    !_kingdomPoliticsHostScrolls.TryGetValue(
                        windowId,
                        out hostScroll
                    ) ||
                    hostScroll == null
                )
                {
                    continue;
                }

                bool wasEnabled = true;
                _kingdomPoliticsHostScrollEnabled.TryGetValue(
                    windowId,
                    out wasEnabled
                );
                hostScroll.enabled = wasEnabled;
            }

            _kingdomPoliticsHostScrolls.Clear();
            _kingdomPoliticsHostScrollEnabled.Clear();
        }

        private static void RestoreInactiveKingdomPoliticsHostScrolls(
            int pActiveWindowId
        )
        {
            if (_kingdomPoliticsHostScrolls.Count == 0)
            {
                return;
            }

            List<int> ids = new List<int>(
                _kingdomPoliticsHostScrolls.Keys
            );
            for (int i = 0; i < ids.Count; i++)
            {
                int windowId = ids[i];
                if (windowId == pActiveWindowId)
                {
                    continue;
                }

                ScrollRect hostScroll;
                if (
                    _kingdomPoliticsHostScrolls.TryGetValue(
                        windowId,
                        out hostScroll
                    ) &&
                    hostScroll != null
                )
                {
                    bool wasEnabled = true;
                    _kingdomPoliticsHostScrollEnabled.TryGetValue(
                        windowId,
                        out wasEnabled
                    );
                    hostScroll.enabled = wasEnabled;
                }

                _kingdomPoliticsHostScrolls.Remove(windowId);
                _kingdomPoliticsHostScrollEnabled.Remove(windowId);
            }
        }

        private static void ShowNativeKingdomPoliticsPanel(
            KingdomWindow pWindow,
            int pPage
        )
        {
            if (pWindow == null)
            {
                return;
            }

            Kingdom kingdom = pWindow.meta_object;
            if (kingdom == null)
            {
                kingdom = GetMemberValue(
                    pWindow,
                    "kingdom",
                    "_kingdom"
                ) as Kingdom;
            }

            if (kingdom == null)
            {
                return;
            }

            ScrollRect hostScroll = FindActiveKingdomStatsScroll(pWindow);
            if (hostScroll == null)
            {
                LogWarning(
                    "Could not open Politics panel: active kingdom ScrollRect was not found."
                );
                return;
            }

            Transform host = hostScroll.viewport != null
                ? hostScroll.viewport
                : hostScroll.transform;

            Transform old = null;
            for (int i = 0; i < host.childCount; i++)
            {
                Transform child = host.GetChild(i);
                if (
                    child != null &&
                    child.gameObject.name == NativePoliticsPanelName
                )
                {
                    old = child;
                    break;
                }
            }

            if (old != null)
            {
                // Deactivate first so the old inner ScrollRect immediately
                // unregisters from the shared vanilla scrollbar before the
                // new Politics page is created.
                old.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(old.gameObject);
            }

            GameObject panelObject = new GameObject(
                NativePoliticsPanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            panelObject.transform.SetParent(host, false);
            panelObject.transform.SetAsLastSibling();

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            Image panelBackground = panelObject.GetComponent<Image>();
            // v1.3.6.8: stay close to the muted olive/stone tone used by
            // vanilla kingdom pages instead of looking like a separate card.
            panelBackground.color = new Color(
                0.235f,
                0.252f,
                0.215f,
                1f
            );
            panelBackground.raycastTarget = true;

            GameObject barObject = new GameObject(
                NativePoliticsPageBarName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup)
            );
            barObject.transform.SetParent(panelObject.transform, false);

            RectTransform barRect =
                barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.offsetMin = new Vector2(8f, -48f);
            barRect.offsetMax = new Vector2(-8f, -6f);

            Image barBackground = barObject.GetComponent<Image>();
            barBackground.color = new Color(
                0.185f,
                0.200f,
                0.170f,
                0.78f
            );
            barBackground.raycastTarget = false;

            HorizontalLayoutGroup bar =
                barObject.GetComponent<HorizontalLayoutGroup>();
            bar.childAlignment = TextAnchor.MiddleCenter;
            bar.padding = new RectOffset(6, 6, 2, 2);
            bar.spacing = 4f;
            bar.childControlWidth = false;
            bar.childControlHeight = false;
            bar.childForceExpandWidth = false;
            bar.childForceExpandHeight = false;

            int politicsWindowId = pWindow.GetInstanceID();
            string addonPageId = "";
            _kingdomPoliticsAddonPageIds.TryGetValue(
                politicsWindowId,
                out addonPageId
            );

            string[] icons =
            {
                OverviewIconPath,
                PartiesIconPath,
                SocietyIconPath,
                LawsIconPath,
                HistoryIconPath
            };

            for (int page = 0; page < icons.Length; page++)
            {
                int capturedPage = page;
                Button pageButton = CreateEmbeddedPoliticsPageButton(
                    barObject.transform,
                    icons[page],
                    string.IsNullOrEmpty(addonPageId) && page == pPage
                );
                if (pageButton != null)
                {
                    pageButton.onClick.AddListener(
                        delegate
                        {
                            SelectNativeKingdomPoliticsPage(
                                pWindow,
                                capturedPage
                            );
                        }
                    );
                }
            }

            List<PoliticalWorldAPI.PoliticsPageInfo> addonPages =
                PoliticalWorldAPI.UI.GetPoliticsPages(kingdom);
            for (int i = 0; i < addonPages.Count; i++)
            {
                PoliticalWorldAPI.PoliticsPageInfo page = addonPages[i];
                if (page == null)
                {
                    continue;
                }

                string capturedId = page.Id;
                string iconPath = string.IsNullOrEmpty(page.IconPath)
                    ? PoliticsIconPath
                    : page.IconPath;
                Button pageButton = CreateEmbeddedPoliticsPageButton(
                    barObject.transform,
                    iconPath,
                    string.Equals(addonPageId, page.Id, StringComparison.Ordinal)
                );
                if (pageButton != null)
                {
                    pageButton.onClick.AddListener(
                        delegate
                        {
                            SelectNativeKingdomAddonPoliticsPage(
                                pWindow,
                                capturedId
                            );
                        }
                    );
                }
            }

            GameObject viewportObject = new GameObject(
                "ukiol_politics_content_viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect)
            );
            viewportObject.transform.SetParent(panelObject.transform, false);

            RectTransform viewportRect =
                viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -54f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject(
                "ukiol_politics_content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect =
                contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup column =
                contentObject.GetComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperCenter;
            column.spacing = 2f;
            column.padding = new RectOffset(2, 2, 2, 6);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect politicsScroll =
                viewportObject.GetComponent<ScrollRect>();
            politicsScroll.viewport = viewportRect;
            politicsScroll.content = contentRect;
            politicsScroll.horizontal = false;
            politicsScroll.vertical = true;
            politicsScroll.movementType = ScrollRect.MovementType.Clamped;
            politicsScroll.scrollSensitivity = 22f;

            // v1.3.7.2: the red scrollbar visible at the right belongs to
            // WorldBox's outer KingdomWindow ScrollRect. Mouse-wheel input
            // was reaching our inner ScrollRect, but dragging that bar was
            // still controlling the outer (covered) page. Temporarily
            // disable the outer ScrollRect and bind its existing Scrollbar
            // to the Politics ScrollRect instead.
            if (!_kingdomPoliticsHostScrolls.ContainsKey(politicsWindowId))
            {
                _kingdomPoliticsHostScrolls[politicsWindowId] = hostScroll;
                _kingdomPoliticsHostScrollEnabled[politicsWindowId] =
                    hostScroll.enabled;
            }

            Scrollbar sharedVerticalScrollbar = hostScroll.verticalScrollbar;
            if (sharedVerticalScrollbar != null)
            {
                hostScroll.enabled = false;

                // ScrollRect subscribes to scrollbar callbacks in OnEnable,
                // so cycle this component after assigning the scrollbar.
                politicsScroll.enabled = false;
                politicsScroll.verticalScrollbar = sharedVerticalScrollbar;
                politicsScroll.verticalScrollbarVisibility =
                    ScrollRect.ScrollbarVisibility.Permanent;
                politicsScroll.enabled = true;
                sharedVerticalScrollbar.gameObject.SetActive(true);
            }

            Text textTemplate = FindPoliticsTextTemplate(pWindow);
            bool renderedAddon = false;
            if (!string.IsNullOrEmpty(addonPageId))
            {
                for (int i = 0; i < addonPages.Count; i++)
                {
                    PoliticalWorldAPI.PoliticsPageInfo page = addonPages[i];
                    if (
                        page == null ||
                        !string.Equals(page.Id, addonPageId, StringComparison.Ordinal)
                    )
                    {
                        continue;
                    }

                    renderedAddon = PoliticalWorldAPI.UI.RenderPoliticsPage(
                        page,
                        new PoliticalWorldAPI.PoliticsPageContext
                        {
                            TargetKind = PoliticalWorldAPI.InspectorTargetKind.Kingdom,
                            KingdomWindow = pWindow,
                            Kingdom = kingdom,
                            Content = contentObject.transform,
                            TextTemplate = textTemplate
                        }
                    );
                    break;
                }
            }

            if (!renderedAddon)
            {
                if (!string.IsNullOrEmpty(addonPageId))
                {
                    _kingdomPoliticsAddonPageIds.Remove(politicsWindowId);
                }
                RenderEmbeddedPoliticsPage(
                    pWindow,
                    kingdom,
                    pPage,
                    contentObject.transform,
                    textTemplate
                );
            }

            panelObject.SetActive(true);
            politicsScroll.verticalNormalizedPosition = 1f;

            // Keep the real WindowMetaTab's content reference pointed at the
            // newest panel (page changes rebuild the panel object).
            SyncTrueNativePoliticsTabContent(pWindow);

            LogInfo(
                "Kingdom Politics embedded panel shown: page=" + pPage +
                ", host=" + host.gameObject.name
            );
        }

        private static Text FindPoliticsTextTemplate(
            Component pWindow
        )
        {
            if (pWindow == null)
            {
                return null;
            }

            Text[] texts = pWindow.GetComponentsInChildren<Text>(true);
            Text best = null;
            float bestScore = -1f;

            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || text.font == null)
                {
                    continue;
                }

                float score = 0f;
                if (text.fontSize >= 12 && text.fontSize <= 24)
                {
                    score += 10f;
                }
                if (text.gameObject.activeInHierarchy)
                {
                    score += 5f;
                }
                score += Mathf.Min(5f, text.fontSize * 0.1f);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = text;
                }
            }

            return best;
        }

        private static Button CreateEmbeddedPoliticsPageButton(
            Transform pParent,
            string pIconPath,
            bool pSelected
        )
        {
            if (pParent == null)
            {
                return null;
            }

            GameObject buttonObject = new GameObject(
                "ukiol_embedded_politics_page_button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            buttonObject.transform.SetParent(pParent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(36f, 36f);

            LayoutElement layout =
                buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = 36f;
            layout.preferredWidth = 36f;
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;

            Image background = buttonObject.GetComponent<Image>();
            Sprite frame = SpriteTextureLoader.getSprite(
                "ukiol/icons/tab_frame"
            );
            if (frame != null)
            {
                background.sprite = frame;
                background.preserveAspect = true;
                background.color = pSelected
                    ? new Color(1f, 0.72f, 0.40f, 1f)
                    : new Color(0.82f, 0.82f, 0.76f, 1f);
            }
            else
            {
                background.color = pSelected
                    ? new Color(0.55f, 0.32f, 0.16f, 1f)
                    : new Color(0.22f, 0.22f, 0.18f, 1f);
            }

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(buttonObject.transform, false);
            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.14f, 0.14f);
            iconRect.anchorMax = new Vector2(0.86f, 0.86f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(pIconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = new Color(1f, 0.95f, 0.78f, 1f);
            buttonColors.pressedColor = new Color(0.92f, 0.66f, 0.38f, 1f);
            buttonColors.selectedColor = new Color(1f, 0.82f, 0.52f, 1f);
            buttonColors.disabledColor = new Color(0.55f, 0.55f, 0.50f, 0.65f);
            buttonColors.colorMultiplier = 1f;
            button.colors = buttonColors;
            return button;
        }

        private static void RenderEmbeddedPoliticsPage(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            int pPage,
            Transform pContent,
            Text pTextTemplate
        )
        {
            if (pKingdom == null || pContent == null)
            {
                return;
            }

            if (pPage == NativePoliticsPageParties)
            {
                AddEmbeddedPoliticsTitle(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_native_politics_parties_title"),
                    PartiesIconPath
                );

                AddEmbeddedPartyCreateButton(
                    pWindow,
                    pKingdom,
                    pContent,
                    pTextTemplate
                );

                List<PartyOverviewEntry> parties =
                    GetPoliticalPartyOverviewEntries(pKingdom);

                int profileWindowId = pWindow == null
                    ? 0
                    : pWindow.GetInstanceID();
                string selectedPartyId = "";
                string selectedKingdomId = "";
                _kingdomPoliticsSelectedPartyIds.TryGetValue(
                    profileWindowId,
                    out selectedPartyId
                );
                _kingdomPoliticsSelectedPartyKingdomIds.TryGetValue(
                    profileWindowId,
                    out selectedKingdomId
                );

                string currentKingdomId =
                    GetStableObjectIdentity(pKingdom);
                if (
                    !string.IsNullOrEmpty(selectedPartyId) &&
                    selectedKingdomId == currentKingdomId
                )
                {
                    PartyOverviewEntry selectedParty = null;
                    for (int i = 0; i < parties.Count; i++)
                    {
                        if (
                            parties[i] != null &&
                            parties[i].Id == selectedPartyId
                        )
                        {
                            selectedParty = parties[i];
                            break;
                        }
                    }

                    if (selectedParty != null)
                    {
                        RenderEmbeddedPartyProfile(
                            pWindow,
                            pKingdom,
                            pContent,
                            pTextTemplate,
                            selectedParty
                        );
                        return;
                    }
                }

                // Stale selection (different kingdom, deleted party, etc.).
                _kingdomPoliticsSelectedPartyIds.Remove(profileWindowId);
                _kingdomPoliticsSelectedPartyKingdomIds.Remove(
                    profileWindowId
                );

                if (parties.Count > 0)
                {
                    AddEmbeddedPartySummaryRow(
                        pContent,
                        pTextTemplate,
                        parties
                    );
                }

                if (parties.Count == 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_native_politics_status_label"),
                        LM.Get("ukiol_party_none"),
                        PartiesIconPath,
                        new Color(0.75f, 0.75f, 0.75f, 1f)
                    );
                    return;
                }

                int maxRows = parties.Count;
                for (int i = 0; i < maxRows; i++)
                {
                    PartyOverviewEntry party = parties[i];
                    AddEmbeddedPartyRow(
                        pWindow,
                        pKingdom,
                        pContent,
                        pTextTemplate,
                        party,
                        i + 1
                    );
                }

                if (parties.Count > maxRows)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_native_politics_status_label"),
                        string.Format(
                            LM.Get("ukiol_party_more_count"),
                            parties.Count - maxRows
                        ),
                        PartiesIconPath,
                        new Color(0.75f, 0.75f, 0.75f, 1f)
                    );
                }
                return;
            }

            if (pPage == NativePoliticsPageSociety)
            {
                AddEmbeddedPoliticsTitle(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_native_politics_society_title"),
                    SocietyIconPath
                );

                List<SocietyCityEntry> societyCities =
                    GetSocietyCityEntries(pKingdom);
                List<PartyOverviewEntry> societyParties =
                    GetPoliticalPartyOverviewEntries(pKingdom);

                AddEmbeddedSocietySummaryRow(
                    pContent,
                    pTextTemplate,
                    societyCities
                );

                List<PartyOverviewEntry> strongholdParties =
                    new List<PartyOverviewEntry>();
                for (int i = 0; i < societyParties.Count; i++)
                {
                    PartyOverviewEntry candidate = societyParties[i];
                    if (
                        candidate != null &&
                        candidate.StrongholdSupport >=
                            PartyStrongholdMinimumSupport &&
                        !string.IsNullOrEmpty(
                            candidate.StrongholdCityName
                        )
                    )
                    {
                        strongholdParties.Add(candidate);
                    }
                }

                if (strongholdParties.Count > 0)
                {
                    AddEmbeddedPoliticsSectionTitle(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_society_strongholds_title"),
                        PartiesIconPath
                    );

                    int strongholdRows = Math.Min(
                        5,
                        strongholdParties.Count
                    );

                    for (int i = 0; i < strongholdRows; i++)
                    {
                        PartyOverviewEntry party = strongholdParties[i];

                        AddEmbeddedPoliticsRow(
                            pContent,
                            pTextTemplate,
                            party.Name,
                            string.Format(
                                LM.Get("ukiol_society_stronghold_value"),
                                party.StrongholdCityName,
                                party.StrongholdSupport
                            ),
                            GetIdeologyIconPath(party.Ideology),
                            GetPartyIdentityColor(
                                party.Ideology,
                                party.ColorSeed
                            )
                        );
                    }
                }

                AddEmbeddedPoliticsSectionTitle(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_society_regions_title"),
                    SocietyIconPath
                );

                if (societyCities.Count == 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_native_politics_status_label"),
                        LM.Get("ukiol_society_no_cities"),
                        SocietyIconPath,
                        new Color(0.75f, 0.75f, 0.75f, 1f)
                    );
                    return;
                }

                for (int i = 0; i < societyCities.Count; i++)
                {
                    AddEmbeddedSocietyCityRow(
                        pContent,
                        pTextTemplate,
                        societyCities[i]
                    );
                }
                return;
            }

            if (pPage == NativePoliticsPageLaws)
            {
                AddEmbeddedPoliticsTitle(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_native_politics_laws_title"),
                    LawsIconPath
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_native_politics_status_label"),
                    LM.Get("ukiol_native_politics_laws_placeholder"),
                    LawsIconPath,
                    new Color(0.75f, 0.75f, 0.75f, 1f)
                );
                return;
            }

            if (pPage == NativePoliticsPageHistory)
            {
                AddEmbeddedPoliticsTitle(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_native_politics_history_title"),
                    HistoryIconPath
                );

                List<ElectionHistoryEntry> elections =
                    LoadElectionHistory(pKingdom);
                List<LeadershipHistoryEntry> leadership =
                    LoadLeadershipHistory(pKingdom);

                if (elections.Count == 0 && leadership.Count == 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_native_politics_status_label"),
                        LM.Get("ukiol_political_history_empty"),
                        HistoryIconPath,
                        new Color(0.75f, 0.75f, 0.75f, 1f)
                    );
                    return;
                }

                if (leadership.Count > 0)
                {
                    AddEmbeddedPoliticsSectionTitle(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_leadership_history_title"),
                        OverviewIconPath
                    );
                    for (int i = leadership.Count - 1; i >= 0; i--)
                    {
                        LeadershipHistoryEntry entry = leadership[i];
                        AddEmbeddedPoliticsRow(
                            pContent,
                            pTextTemplate,
                            string.Format(
                                LM.Get("ukiol_election_history_year"),
                                entry.Year
                            ),
                            string.Format(
                                LM.Get("ukiol_leadership_history_result"),
                                GetLeadershipRoleName(entry.Role),
                                entry.Name,
                                GetLeadershipTitleName(entry.Title)
                            ),
                            OverviewIconPath,
                            new Color(0.83f, 0.79f, 0.68f, 1f)
                        );
                    }
                }

                if (elections.Count > 0)
                {
                    AddEmbeddedPoliticsSectionTitle(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_election_history_title"),
                        PartiesIconPath
                    );

                    for (int i = elections.Count - 1; i >= 0; i--)
                    {
                        ElectionHistoryEntry election = elections[i];
                        AddEmbeddedPoliticsRow(
                            pContent,
                            pTextTemplate,
                            string.Format(
                                LM.Get("ukiol_election_history_year"),
                                election.Year
                            ),
                            string.Format(
                                LM.Get("ukiol_election_history_result"),
                                election.WinnerPartyName,
                                election.WinnerSupport
                            ),
                            GetIdeologyIconPath(election.WinnerIdeology),
                            GetElectionPartyColor(
                                pKingdom,
                                election.WinnerPartyId,
                                election.WinnerIdeology
                            )
                        );
                    }
                }
                return;
            }

            AddEmbeddedPoliticsTitle(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_native_politics_overview_title"),
                OverviewIconPath
            );

            string course = GetKingdomCourse(pKingdom);
            string courseText = LM.Get("ukiol_state_course_none");
            if (course == ReformerTraitId)
            {
                courseText = LM.Get("ukiol_state_course_reformer");
            }
            else if (course == MilitaristTraitId)
            {
                courseText = LM.Get("ukiol_state_course_militarist");
            }
            else if (course == DiplomatTraitId)
            {
                courseText = LM.Get("ukiol_state_course_diplomat");
            }

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_state_course_label"),
                courseText,
                GetCourseIconPath(course),
                new Color(0.30f, 1f, 0.30f, 1f)
            );

            string governmentForm = GetGovernmentForm(pKingdom);
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_government_form_label"),
                GetGovernmentPublicName(pKingdom),
                OverviewIconPath,
                GetGovernmentFormColor(governmentForm)
            );

            string politicalSystem = GetPoliticalSystem(pKingdom);
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_political_system_label"),
                GetPoliticalSystemName(politicalSystem),
                OverviewIconPath,
                GetPoliticalSystemColor(politicalSystem)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_head_of_state_label"),
                FormatLeadershipOffice(
                    pKingdom,
                    HeadOfStateNameDataKey,
                    HeadOfStateTitleDataKey,
                    HeadOfStateSinceYearDataKey
                ),
                OverviewIconPath,
                new Color(0.86f, 0.79f, 0.59f, 1f)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_head_of_government_label"),
                FormatLeadershipOffice(
                    pKingdom,
                    HeadOfGovernmentNameDataKey,
                    HeadOfGovernmentTitleDataKey,
                    HeadOfGovernmentSinceYearDataKey
                ),
                OverviewIconPath,
                new Color(0.72f, 0.85f, 0.91f, 1f)
            );

            if (GetKingdomIntData(pKingdom, LeadershipCrisisDataKey, 0) != 0)
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_leadership_status_label"),
                    string.Format(
                        LM.Get("ukiol_leadership_crisis_value"),
                        Math.Max(
                            0,
                            GetWorldYearSafe() - GetKingdomIntData(
                                pKingdom,
                                LeadershipCrisisSinceYearDataKey,
                                GetWorldYearSafe()
                            )
                        )
                    ),
                    HistoryIconPath,
                    new Color(0.95f, 0.45f, 0.38f, 1f)
                );
            }

            int leaderTenureYears = Math.Max(
                0,
                GetKingdomIntData(
                    pKingdom,
                    LeaderTenureYearsDataKey,
                    0
                )
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_leader_tenure_label"),
                string.Format(
                    LM.Get("ukiol_leader_tenure_value"),
                    leaderTenureYears
                ),
                OverviewIconPath,
                new Color(0.82f, 0.80f, 0.66f, 1f)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_government_elections_label"),
                GetGovernmentElectionStatusText(
                    pKingdom,
                    governmentForm
                ),
                PartiesIconPath,
                GetPoliticalSystemColor(politicalSystem)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_war_status_label"),
                GetWarDiplomacyStatusText(pKingdom),
                MilitaristIconPath,
                GetWarDiplomacyStatusColor(pKingdom)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_diplomatic_reputation_label"),
                FormatIdeologyBehaviorPercent(GetDiplomaticReputation(pKingdom)),
                DiplomatIconPath,
                GetDiplomaticReputationColor(GetDiplomaticReputation(pKingdom))
            );

            string visibleBlocId = GetKingdomStringData(
                pKingdom,
                InternationalBlocIdDataKey,
                ""
            );
            if (!string.IsNullOrEmpty(visibleBlocId))
            {
                string visibleBlocType = GetKingdomStringData(
                    pKingdom,
                    InternationalBlocTypeDataKey,
                    "commonwealth"
                );
                Color visibleBlocColor = GetInternationalBlocColor(
                    visibleBlocType
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_name_label"),
                    GetKingdomStringData(
                        pKingdom,
                        InternationalBlocNameDataKey,
                        "?"
                    ),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_type_label"),
                    GetInternationalBlocTypeName(visibleBlocType),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_leader_label"),
                    GetKingdomStringData(
                        pKingdom,
                        InternationalBlocLeaderNameDataKey,
                        "?"
                    ),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_members_label"),
                    GetKingdomIntData(
                        pKingdom,
                        InternationalBlocMemberCountDataKey,
                        0
                    ).ToString(),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_unity_label"),
                    FormatIdeologyBehaviorPercent(
                        GetKingdomIntData(
                            pKingdom,
                            InternationalBlocUnityDataKey,
                            0
                        )
                    ),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_bloc_integration_label"),
                    FormatIdeologyBehaviorPercent(
                        GetKingdomIntData(
                            pKingdom,
                            InternationalBlocIntegrationDataKey,
                            0
                        )
                    ),
                    DiplomatIconPath,
                    visibleBlocColor
                );
                int nextSummitYear = GetKingdomIntData(
                    pKingdom, InternationalSummitNextYearDataKey, 0
                );
                if (nextSummitYear > 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_next_label"),
                        nextSummitYear.ToString(),
                        DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                    );
                }
                int lastSummitYear = GetKingdomIntData(
                    pKingdom, InternationalSummitLastYearDataKey, 0
                );
                if (lastSummitYear > 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_last_label"),
                        lastSummitYear.ToString() + " — " +
                            GetInternationalSummitResultName(
                                GetKingdomStringData(pKingdom, InternationalSummitLastResultDataKey, "none")
                            ),
                        DiplomatIconPath, new Color(0.72f, 0.83f, 0.93f, 1f)
                    );
                }
                if (GetKingdomIntData(pKingdom, InternationalSummitActiveDataKey, 0) != 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_stage_label"),
                        GetInternationalSummitStageName(
                            GetKingdomIntData(pKingdom, InternationalSummitStageDataKey, 1)
                        ),
                        DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                    );
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_host_label"),
                        GetKingdomStringData(pKingdom, InternationalSummitHostCityDataKey, "?") +
                            " (" + GetKingdomStringData(pKingdom, InternationalSummitHostKingdomDataKey, "?") + ")",
                        DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                    );
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_agenda_label"),
                        GetInternationalSummitAgendaName(
                            GetKingdomStringData(pKingdom, InternationalSummitAgendaDataKey, "common_declaration")
                        ),
                        DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                    );
                    AddEmbeddedPoliticsRow(
                        pContent, pTextTemplate,
                        LM.Get("ukiol_summit_attendees_label"),
                        GetKingdomIntData(pKingdom, InternationalSummitAttendeesDataKey, 0).ToString() +
                            "/" + GetKingdomIntData(pKingdom, InternationalBlocMemberCountDataKey, 0).ToString(),
                        DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                    );
                    if (GetKingdomIntData(pKingdom, InternationalSummitStageDataKey, 1) >= 3)
                    {
                        AddEmbeddedPoliticsRow(
                            pContent, pTextTemplate,
                            LM.Get("ukiol_summit_votes_label"),
                            GetKingdomIntData(pKingdom, InternationalSummitYesVotesDataKey, 0).ToString() +
                                " / " + GetKingdomIntData(pKingdom, InternationalSummitNoVotesDataKey, 0).ToString(),
                            DiplomatIconPath, new Color(0.78f, 0.72f, 0.95f, 1f)
                        );
                    }
                }
            }

            if (GetKingdomIntData(pKingdom, DiplomaticCrisisActiveDataKey, 0) != 0)
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_diplomatic_crisis_label"),
                    GetKingdomStringData(pKingdom, DiplomaticCrisisCounterpartDataKey, "?"),
                    DiplomatIconPath,
                    new Color(0.95f, 0.68f, 0.30f, 1f)
                );
                string diplomaticDemand = GetKingdomStringData(
                    pKingdom,
                    DiplomaticCrisisDemandDataKey,
                    "political_guarantees"
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_diplomatic_demand_label"),
                    GetDiplomaticDemandName(diplomaticDemand),
                    DiplomatIconPath,
                    new Color(0.95f, 0.78f, 0.40f, 1f)
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_diplomatic_stage_label"),
                    GetDiplomaticCrisisStageName(
                        GetKingdomIntData(pKingdom, DiplomaticCrisisStageDataKey, 1)
                    ),
                    DiplomatIconPath,
                    new Color(0.92f, 0.62f, 0.38f, 1f)
                );
                int diplomaticTension = ClampInt(
                    GetKingdomIntData(pKingdom, DiplomaticCrisisTensionDataKey, 0),
                    0,
                    100
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_diplomatic_tension_label"),
                    FormatIdeologyBehaviorPercent(diplomaticTension),
                    DiplomatIconPath,
                    GetWarExhaustionColor(diplomaticTension)
                );
            }

            ActiveWarSimulation visibleWar = GetVisibleActiveWarForKingdom(
                pKingdom
            );
            if (visibleWar != null)
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_war_goal_label"),
                    GetCasusBelliName(visibleWar.CasusBelli),
                    MilitaristIconPath,
                    new Color(0.95f, 0.64f, 0.28f, 1f)
                );

                int visibleScore = GetWarScoreForKingdom(
                    visibleWar,
                    pKingdom
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_war_score_label"),
                    FormatWarScore(visibleScore),
                    MilitaristIconPath,
                    GetWarScoreColor(visibleScore)
                );
            }

            int warExhaustion = ClampInt(
                GetKingdomIntData(pKingdom, WarExhaustionDataKey, 0),
                0,
                100
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_war_exhaustion_label"),
                string.Format(
                    LM.Get("ukiol_war_exhaustion_value"),
                    warExhaustion
                ),
                MilitaristIconPath,
                GetWarExhaustionColor(warExhaustion)
            );

            int truceUntil = GetKingdomIntData(
                pKingdom,
                WarLatestTruceUntilYearDataKey,
                0
            );
            if (truceUntil > GetWorldYearSafe())
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_war_truce_label"),
                    string.Format(
                        LM.Get("ukiol_war_truce_value"),
                        truceUntil
                    ),
                    DiplomatIconPath,
                    new Color(0.54f, 0.82f, 0.96f, 1f)
                );
            }

            if (
                politicalSystem == PoliticalSystemSovietId ||
                politicalSystem == PoliticalSystemSovietOnePartyId
            )
            {
                int delegates = GetKingdomIntData(
                    pKingdom,
                    CouncilDelegateCountDataKey,
                    0
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_council_label"),
                    delegates > 0
                        ? string.Format(
                            LM.Get("ukiol_council_value"),
                            delegates
                        )
                        : LM.Get("ukiol_council_forming"),
                    PartiesIconPath,
                    GetPoliticalSystemColor(politicalSystem)
                );

                if (delegates > 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_council_delegates_label"),
                        FormatCouncilDelegateSummary(pKingdom),
                        PartiesIconPath,
                        GetPoliticalSystemColor(politicalSystem)
                    );
                }

                string councilParty = GetKingdomStringData(
                    pKingdom,
                    CouncilDominantPartyNameDataKey,
                    ""
                );
                if (!string.IsNullOrEmpty(councilParty))
                {
                    int councilSupport = ClampInt(
                        GetKingdomIntData(
                            pKingdom,
                            CouncilDominantPartySupportDataKey,
                            0
                        ),
                        0,
                        100
                    );
                    string councilIdeology = GetKingdomStringData(
                        pKingdom,
                        CouncilDominantPartyIdeologyDataKey,
                        ""
                    );
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_council_dominant_label"),
                        string.Format(
                            LM.Get("ukiol_council_dominant_value"),
                            councilParty,
                            councilSupport
                        ),
                        GetIdeologyIconPath(councilIdeology),
                        GetElectionPartyColor(
                            pKingdom,
                            GetKingdomStringData(
                                pKingdom,
                                CouncilDominantPartyIdDataKey,
                                ""
                            ),
                            councilIdeology
                        )
                    );
                }
            }

            if (UsesCommunistPartyCongress(pKingdom, politicalSystem))
            {
                int committeeSize = GetKingdomIntData(
                    pKingdom,
                    CentralCommitteeSizeDataKey,
                    0
                );
                int nextCongress = GetKingdomIntData(
                    pKingdom,
                    PartyCongressNextYearDataKey,
                    0
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_party_congress_label"),
                    nextCongress > 0
                        ? string.Format(
                            LM.Get("ukiol_party_congress_value"),
                            nextCongress,
                            Math.Max(0, nextCongress - GetWorldYearSafe())
                        )
                        : LM.Get("ukiol_party_congress_forming"),
                    PartiesIconPath,
                    new Color(0.90f, 0.59f, 0.43f, 1f)
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_central_committee_label"),
                    committeeSize > 0
                        ? string.Format(
                            LM.Get("ukiol_central_committee_value"),
                            committeeSize
                        )
                        : LM.Get("ukiol_central_committee_forming"),
                    PartiesIconPath,
                    new Color(0.90f, 0.59f, 0.43f, 1f)
                );

                string generalSecretary = GetKingdomStringData(
                    pKingdom,
                    GeneralSecretaryNameDataKey,
                    ""
                );
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_general_secretary_label"),
                    !string.IsNullOrEmpty(generalSecretary)
                        ? generalSecretary
                        : LM.Get("ukiol_general_secretary_none"),
                    PartiesIconPath,
                    new Color(0.94f, 0.66f, 0.48f, 1f)
                );

                if (committeeSize > 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_politburo_label"),
                        FormatPolitburoSummary(pKingdom),
                        PartiesIconPath,
                        new Color(0.92f, 0.62f, 0.45f, 1f)
                    );
                }

                if (committeeSize > 0)
                {
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_central_committee_members_label"),
                        FormatCentralCommitteeMemberSummary(pKingdom),
                        PartiesIconPath,
                        new Color(0.88f, 0.72f, 0.57f, 1f)
                    );
                    AddEmbeddedPoliticsRow(
                        pContent,
                        pTextTemplate,
                        LM.Get("ukiol_party_internal_currents_label"),
                        FormatPartyInternalCurrents(pKingdom),
                        PartiesIconPath,
                        new Color(0.82f, 0.69f, 0.59f, 1f)
                    );
                }
            }

            string rulingPartyName = GetKingdomStringData(
                pKingdom,
                ElectionRulingPartyNameDataKey,
                ""
            );
            string rulingPartyIdeology = GetKingdomStringData(
                pKingdom,
                ElectionRulingPartyIdeologyDataKey,
                ""
            );
            int rulingPartySupport = ClampInt(
                GetKingdomIntData(
                    pKingdom,
                    ElectionRulingPartySupportDataKey,
                    0
                ),
                0,
                100
            );

            if (!string.IsNullOrEmpty(rulingPartyName))
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_election_ruling_party_label"),
                    string.Format(
                        LM.Get("ukiol_election_ruling_party_value"),
                        rulingPartyName,
                        rulingPartySupport
                    ),
                    GetIdeologyIconPath(rulingPartyIdeology),
                    GetElectionPartyColor(
                        pKingdom,
                        GetKingdomStringData(
                            pKingdom,
                            ElectionRulingPartyIdDataKey,
                            ""
                        ),
                        rulingPartyIdeology
                    )
                );
            }

            string winningCoalitionName = GetKingdomStringData(
                pKingdom,
                ElectionWinningCoalitionNameDataKey,
                ""
            );
            int winningCoalitionSupport = ClampInt(
                GetKingdomIntData(
                    pKingdom,
                    ElectionWinningCoalitionSupportDataKey,
                    0
                ),
                0,
                100
            );
            if (!string.IsNullOrEmpty(winningCoalitionName))
            {
                AddEmbeddedPoliticsRow(
                    pContent,
                    pTextTemplate,
                    LM.Get("ukiol_election_coalition_label"),
                    string.Format(
                        LM.Get("ukiol_election_coalition_value"),
                        winningCoalitionName,
                        winningCoalitionSupport
                    ),
                    PartiesIconPath,
                    new Color(0.78f, 0.72f, 0.9f, 1f)
                );
            }

            string ideology = GetStateIdeology(pKingdom);
            int ideologySupport = GetKingdomIdeologySupport(
                pKingdom,
                ideology
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_state_ideology_label"),
                FormatStateIdeology(ideology, ideologySupport),
                GetIdeologyIconPath(ideology),
                ParseHtmlColor(GetIdeologySupportColor(ideologySupport))
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_state_ideology_current_label"),
                GetIdeologyCurrentName(GetStateIdeologyCurrent(pKingdom)),
                GetIdeologyIconPath(ideology),
                new Color(0.91f, 0.83f, 0.42f, 1f)
            );

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_ideology_tree_path_label"),
                GetKingdomIdeologyTreePath(pKingdom),
                GetIdeologyIconPath(ideology),
                new Color(0.72f, 0.83f, 0.93f, 1f)
            );

            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(pKingdom);
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_behavior_market_label"),
                FormatIdeologyBehaviorPercent(behavior.Market),
                OverviewIconPath, new Color(0.95f, 0.80f, 0.35f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_behavior_welfare_label"),
                FormatIdeologyBehaviorPercent(behavior.Welfare),
                OverviewIconPath, new Color(0.45f, 0.90f, 0.55f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_behavior_centralization_label"),
                FormatIdeologyBehaviorPercent(behavior.Centralization),
                OverviewIconPath, new Color(0.95f, 0.62f, 0.35f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_behavior_pluralism_label"),
                FormatIdeologyBehaviorPercent(behavior.Pluralism),
                OverviewIconPath, new Color(0.45f, 0.82f, 0.95f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_behavior_militarism_label"),
                FormatIdeologyBehaviorPercent(behavior.Militarism),
                MilitaristIconPath, new Color(0.95f, 0.48f, 0.42f, 1f)
            );

            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_ideology_economy_effect_label"),
                GetIdeologyEconomyEffectSummary(pKingdom),
                OverviewIconPath, new Color(0.91f, 0.83f, 0.42f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_ideology_stability_effect_label"),
                FormatSignedPoliticalValue(
                    GetIdeologyBehaviorStabilityModifier(pKingdom)
                ),
                OverviewIconPath, new Color(0.45f, 0.90f, 0.55f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_ideology_army_effect_label"),
                FormatSignedPoliticalPercent(
                    GetIdeologyArmyLimitEffectPercent(pKingdom)
                ),
                MilitaristIconPath, new Color(0.95f, 0.48f, 0.42f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_ideology_war_exhaustion_effect_label"),
                "+" + GetIdeologyWarExhaustionPerYear(pKingdom).ToString() + "/" +
                    LM.Get("ukiol_year_short"),
                MilitaristIconPath, new Color(0.95f, 0.70f, 0.44f, 1f)
            );

            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_reform_pressure_label"),
                FormatIdeologyBehaviorPercent(
                    GetKingdomIntData(
                        pKingdom,
                        IdeologyReformPressureDataKey,
                        0
                    )
                ),
                ReformerIconPath, new Color(0.45f, 0.90f, 0.72f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent, pTextTemplate,
                LM.Get("ukiol_radicalization_pressure_label"),
                FormatIdeologyBehaviorPercent(
                    GetKingdomIntData(
                        pKingdom,
                        IdeologyRadicalizationPressureDataKey,
                        0
                    )
                ),
                MilitaristIconPath, new Color(0.95f, 0.50f, 0.45f, 1f)
            );

            int stability = GetNationalStability(pKingdom);
            int target = CalculateNationalStabilityTarget(pKingdom);
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_national_stability_label"),
                FormatStabilityValue(stability, target, true),
                OverviewIconPath,
                ParseHtmlColor(GetStabilityColor(stability))
            );

            string movementIdeology;
            int movementSupport;
            int movementRadicalism;
            string movementLeader;
            GetLeadingPoliticalMovement(
                pKingdom,
                out movementIdeology,
                out movementSupport,
                out movementRadicalism,
                out movementLeader
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_political_movement_label"),
                FormatPoliticalMovement(
                    movementIdeology,
                    movementSupport,
                    movementLeader
                ),
                GetIdeologyIconPath(movementIdeology),
                ParseHtmlColor(GetMovementColor(movementRadicalism))
            );

            string leadingIdeology;
            string leadingName;
            string leadingLeader;
            int leadingSupport;
            int leadingRadicalism;
            GetLeadingPoliticalParty(
                pKingdom,
                out leadingIdeology,
                out leadingName,
                out leadingLeader,
                out leadingSupport,
                out leadingRadicalism
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_leading_party_label"),
                FormatPartyWithSupport(leadingName, leadingSupport),
                PartiesIconPath,
                ParseHtmlColor(GetMovementColor(leadingRadicalism))
            );

            int crisisPressure = GetPoliticalCrisisPressure(pKingdom);
            string crisisIdeology = GetPoliticalCrisisIdeology(pKingdom);
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_political_crisis_label"),
                FormatPoliticalCrisis(crisisIdeology, crisisPressure),
                GetIdeologyIconPath(crisisIdeology),
                ParseHtmlColor(GetCrisisColor(crisisPressure))
            );
        }

        private static void RenderEmbeddedPartyProfile(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            Transform pContent,
            Text pTextTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (
                pWindow == null ||
                pKingdom == null ||
                pContent == null ||
                pParty == null
            )
            {
                return;
            }

            AddEmbeddedPartyProfileBackButton(
                pWindow,
                pContent,
                pTextTemplate
            );

            AddEmbeddedPartyProfileHeader(
                pContent,
                pTextTemplate,
                pParty
            );

            AddEmbeddedPartyRenameButton(
                pWindow,
                pKingdom,
                pContent,
                pTextTemplate,
                pParty
            );

            AddEmbeddedPoliticsSectionTitle(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_info_title"),
                OverviewIconPath
            );

            string unknown = LM.Get("ukiol_party_profile_unknown");
            string leader = string.IsNullOrEmpty(pParty.Leader)
                ? unknown
                : pParty.Leader;
            string founder = string.IsNullOrEmpty(pParty.Founder)
                ? unknown
                : pParty.Founder;
            string founded = pParty.FoundedYear > 0
                ? pParty.FoundedYear.ToString()
                : LM.Get("ukiol_party_profile_founded_legacy");
            string origin = string.IsNullOrEmpty(pParty.OriginCityName)
                ? unknown
                : pParty.OriginCityName;
            // Old 1.3.x saves can contain a legacy parent id without a
            // preserved parent name. Treat that as an independent/unknown
            // historical origin instead of showing the misleading
            // "Unknown party" label. All new splits persist both fields.
            string parent =
                string.IsNullOrEmpty(pParty.ParentPartyId) ||
                string.IsNullOrEmpty(pParty.ParentPartyName)
                    ? LM.Get("ukiol_party_profile_no_parent")
                    : pParty.ParentPartyName;

            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_leader"),
                leader,
                PartiesIconPath,
                Color.white
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_founder"),
                founder,
                PartiesIconPath,
                new Color(0.86f, 0.82f, 0.68f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_founded"),
                founded,
                HistoryIconPath,
                new Color(0.84f, 0.84f, 0.78f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_origin"),
                origin,
                SocietyIconPath,
                new Color(0.82f, 0.88f, 0.72f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_parent"),
                parent,
                PartiesIconPath,
                new Color(0.78f, 0.84f, 0.92f, 1f)
            );

            AddEmbeddedPoliticsSectionTitle(
                pContent,
                pTextTemplate,
                LM.Get("ukiol_party_profile_traits_title"),
                PartiesIconPath
            );
            AddEmbeddedPartyProfileTraitsCard(
                pContent,
                pTextTemplate,
                pParty.Traits
            );

            AddEmbeddedPartyProfileRegionalSections(
                pKingdom,
                pContent,
                pTextTemplate,
                pParty
            );

            AddEmbeddedPartySupportHistorySection(
                pContent,
                pTextTemplate,
                pParty
            );

            AddEmbeddedPartyProfileHistorySection(
                pContent,
                pTextTemplate,
                pParty
            );
        }

        private static void AddEmbeddedPartyCreateButton(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            Transform pParent,
            Text pTemplate
        )
        {
            if (
                pWindow == null ||
                pKingdom == null ||
                pParent == null
            )
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_party_create_button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            Sprite bgSprite = SpriteTextureLoader.getSprite(
                "ui/special/windowInnerSliced"
            );
            if (bgSprite != null)
            {
                background.sprite = bgSprite;
                background.type = Image.Type.Sliced;
            }
            background.color = new Color(0.14f, 0.27f, 0.18f, 0.96f);
            background.raycastTarget = true;

            Text text = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "create_party_text",
                "+  " + LM.Get("ukiol_party_create_button"),
                new Color(0.78f, 0.94f, 0.72f, 1f),
                TextAnchor.MiddleCenter,
                11
            );
            if (text != null)
            {
                RectTransform r = text.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(8f, 1f);
                r.offsetMax = new Vector2(-8f, -1f);
                text.fontStyle = FontStyle.Bold;
            }

            Button button = rowObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 1f, 0.86f, 1f);
            colors.pressedColor = new Color(0.68f, 0.86f, 0.64f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(
                delegate
                {
                    PartyEditorWindow.OpenForCreate(
                        pWindow,
                        pKingdom
                    );
                }
            );
        }

        private static void AddEmbeddedPartyRenameButton(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (
                pWindow == null ||
                pKingdom == null ||
                pParent == null ||
                pParty == null ||
                string.IsNullOrEmpty(pParty.Id)
            )
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_party_profile_editor",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            Sprite bgSprite = SpriteTextureLoader.getSprite(
                "ui/special/windowInnerSliced"
            );
            if (bgSprite != null)
            {
                background.sprite = bgSprite;
                background.type = Image.Type.Sliced;
            }
            background.color = new Color(0.17f, 0.20f, 0.17f, 0.96f);
            background.raycastTarget = true;

            Text text = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "editor_text",
                "✎  " + LM.Get("ukiol_party_editor_button"),
                new Color(0.94f, 0.84f, 0.46f, 1f),
                TextAnchor.MiddleCenter,
                11
            );
            if (text != null)
            {
                RectTransform r = text.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(8f, 1f);
                r.offsetMax = new Vector2(-8f, -1f);
                text.fontStyle = FontStyle.Bold;
            }

            Button button = rowObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.78f, 1f);
            colors.pressedColor = new Color(0.88f, 0.72f, 0.48f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            string capturedPartyId = pParty.Id;
            button.onClick.AddListener(
                delegate
                {
                    PartyEditorWindow.OpenForParty(
                        pWindow,
                        pKingdom,
                        capturedPartyId
                    );
                }
            );
        }

        private static void AddEmbeddedPartyProfileBackButton(
            KingdomWindow pWindow,
            Transform pParent,
            Text pTemplate
        )
        {
            if (pWindow == null || pParent == null)
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_party_profile_back",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.165f, 0.178f, 0.151f, 0.92f);
            background.raycastTarget = true;

            Text text = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "back_text",
                "←  " + LM.Get("ukiol_party_profile_back"),
                new Color(0.94f, 0.84f, 0.46f, 1f),
                TextAnchor.MiddleLeft,
                11
            );
            if (text != null)
            {
                RectTransform r = text.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(10f, 1f);
                r.offsetMax = new Vector2(-8f, -1f);
                text.fontStyle = FontStyle.Bold;
            }

            Button button = rowObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.78f, 1f);
            colors.pressedColor = new Color(0.88f, 0.72f, 0.48f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(
                delegate
                {
                    CloseNativeKingdomPartyProfile(pWindow);
                }
            );
        }

        private static void AddEmbeddedPartyProfileHeader(
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (pParent == null || pParty == null)
            {
                return;
            }

            const float rowHeight = 126f;
            Color ideologyColor = GetPartyIdentityColor(
                pParty.Ideology,
                pParty.ColorSeed
            );

            GameObject rowObject = new GameObject(
                "ukiol_party_profile_header",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = rowHeight;
            layout.preferredHeight = rowHeight;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.195f, 0.211f, 0.180f, 0.96f);
            background.raycastTarget = false;

            GameObject stripeObject = new GameObject(
                "profile_stripe",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            stripeObject.transform.SetParent(rowObject.transform, false);
            RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(5f, 0f);
            Image stripe = stripeObject.GetComponent<Image>();
            stripe.color = ideologyColor;
            stripe.raycastTarget = false;

            GameObject iconPlate = new GameObject(
                "profile_icon_plate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            iconPlate.transform.SetParent(rowObject.transform, false);
            RectTransform plateRect = iconPlate.GetComponent<RectTransform>();
            plateRect.anchorMin = new Vector2(0f, 1f);
            plateRect.anchorMax = new Vector2(0f, 1f);
            plateRect.pivot = new Vector2(0f, 1f);
            plateRect.anchoredPosition = new Vector2(14f, -15f);
            plateRect.sizeDelta = new Vector2(58f, 58f);
            Image plate = iconPlate.GetComponent<Image>();
            plate.color = new Color(
                ideologyColor.r * 0.34f + 0.03f,
                ideologyColor.g * 0.34f + 0.03f,
                ideologyColor.b * 0.34f + 0.03f,
                0.82f
            );
            plate.raycastTarget = true;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(iconPlate.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(
                GetIdeologyIconPath(pParty.Ideology)
            );
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Button secretButton = iconPlate.GetComponent<Button>();
            if (secretButton != null)
            {
                secretButton.targetGraphic = plate;
                secretButton.transition = Selectable.Transition.None;
                int secretClicks = 0;
                bool secretRevealed = false;
                secretButton.onClick.AddListener(
                    delegate
                    {
                        if (secretRevealed)
                        {
                            return;
                        }

                        secretClicks++;
                        if (secretClicks < HiddenPortraitClickCount)
                        {
                            return;
                        }

                        Sprite hiddenPortrait =
                            SpriteTextureLoader.getSprite(
                                HiddenPortraitResourcePath
                            );
                        if (hiddenPortrait == null)
                        {
                            return;
                        }

                        secretRevealed = true;
                        icon.sprite = hiddenPortrait;
                        iconRect.anchorMin = new Vector2(0.03f, 0.03f);
                        iconRect.anchorMax = new Vector2(0.97f, 0.97f);
                        plate.color = new Color(0.14f, 0.16f, 0.14f, 0.96f);

                        Text infinity = CreateEmbeddedPoliticsText(
                            iconPlate.transform,
                            pTemplate,
                            "portrait_infinity",
                            "∞",
                            new Color(1f, 0.86f, 0.35f, 1f),
                            TextAnchor.LowerRight,
                            13
                        );
                        if (infinity != null)
                        {
                            RectTransform ir = infinity.rectTransform;
                            ir.anchorMin = Vector2.zero;
                            ir.anchorMax = Vector2.one;
                            ir.offsetMin = new Vector2(2f, 1f);
                            ir.offsetMax = new Vector2(-3f, -1f);
                            infinity.fontStyle = FontStyle.Bold;
                            infinity.raycastTarget = false;
                        }

                        try
                        {
                            SoundBox.click();
                        }
                        catch
                        {
                            // Easter egg audio must never affect the profile.
                        }
                    }
                );
            }

            Text name = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "profile_party_name",
                pParty.Name,
                Color.white,
                TextAnchor.MiddleLeft,
                17
            );
            if (name != null)
            {
                RectTransform r = name.rectTransform;
                // Custom names can be much longer than generated ones. Give
                // the title the full header width and up to two lines instead
                // of competing with the support percentage on the same row.
                r.anchorMin = new Vector2(0f, 0.70f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(84f, 1f);
                r.offsetMax = new Vector2(-10f, -3f);
                name.fontStyle = FontStyle.Bold;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.resizeTextMinSize = 8;
                name.resizeTextMaxSize = 16;
            }

            Text support = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "profile_party_support",
                FormatPartySupportWithTrend(pParty),
                ideologyColor,
                TextAnchor.MiddleRight,
                21
            );
            if (support != null)
            {
                RectTransform r = support.rectTransform;
                r.anchorMin = new Vector2(0.72f, 0.48f);
                r.anchorMax = new Vector2(1f, 0.70f);
                r.pivot = new Vector2(1f, 0.5f);
                r.offsetMin = new Vector2(2f, 0f);
                r.offsetMax = new Vector2(-10f, 0f);
                support.fontStyle = FontStyle.Bold;
                support.resizeTextMinSize = 14;
                support.resizeTextMaxSize = 21;
            }

            Text ideology = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "profile_party_ideology",
                GetIdeologyName(pParty.Ideology),
                ideologyColor,
                TextAnchor.MiddleLeft,
                12
            );
            if (ideology != null)
            {
                RectTransform r = ideology.rectTransform;
                r.anchorMin = new Vector2(0f, 0.47f);
                r.anchorMax = new Vector2(0.72f, 0.70f);
                r.offsetMin = new Vector2(84f, 0f);
                r.offsetMax = new Vector2(-4f, 0f);
                ideology.resizeTextMinSize = 8;
                ideology.resizeTextMaxSize = 12;
                ideology.fontStyle = FontStyle.Bold;
            }

            string profile = FormatPartyStoredProfile(
                pParty.Position,
                pParty.Strategy,
                pParty.ForeignStance
            );
            Text profileText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "profile_party_profile",
                profile,
                new Color(0.76f, 0.84f, 0.91f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (profileText != null)
            {
                RectTransform r = profileText.rectTransform;
                r.anchorMin = new Vector2(0f, 0.18f);
                r.anchorMax = new Vector2(1f, 0.47f);
                r.offsetMin = new Vector2(84f, 0f);
                r.offsetMax = new Vector2(-10f, 0f);
                profileText.resizeTextMinSize = 8;
                profileText.resizeTextMaxSize = 10;
            }

            AddEmbeddedPartySupportBar(
                rowObject.transform,
                pParty.Support,
                ideologyColor
            );
        }


        private static void AddEmbeddedPartyProfileRegionalSections(
            Kingdom pKingdom,
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (
                pKingdom == null ||
                pParent == null ||
                pParty == null
            )
            {
                return;
            }

            List<PoliticalParty> runtimeParties =
                GetPoliticalPartiesReadOnly(pKingdom);
            PoliticalParty runtimeParty =
                FindPoliticalPartyById(
                    runtimeParties,
                    pParty.Id
                );

            AddEmbeddedPoliticsSectionTitle(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_position_title"),
                OverviewIconPath
            );

            if (runtimeParty == null)
            {
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_status"),
                    GetPartyProfileStatusText(pParty.Support),
                    PartiesIconPath,
                    GetPartyProfileStatusColor(pParty.Support)
                );
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_national_support"),
                    pParty.Support + "%",
                    GetIdeologyIconPath(pParty.Ideology),
                    GetPartyIdentityColor(pParty.Ideology, pParty.ColorSeed)
                );
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_support_5y"),
                    FormatPartySupportTrendValue(pParty),
                    HistoryIconPath,
                    GetPartySupportTrendColor(pParty)
                );
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_cities_first"),
                    "0 / 0",
                    SocietyIconPath,
                    new Color(0.76f, 0.78f, 0.73f, 1f)
                );
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_stronghold"),
                    LM.Get("ukiol_party_profile_no_stronghold"),
                    SocietyIconPath,
                    new Color(0.70f, 0.71f, 0.67f, 1f)
                );
                return;
            }

            List<PartyRegionSupportEntry> regions =
                BuildPartyRegionSupportEntries(
                    pKingdom,
                    runtimeParties,
                    runtimeParty
                );

            int leadingCities =
                CountPartyLeadingCities(
                    pKingdom,
                    runtimeParties,
                    runtimeParty
                );
            int totalCities = GetCitiesSafe(pKingdom).Count;

            PartyRegionSupportEntry strongest =
                regions.Count > 0
                    ? regions[0]
                    : null;

            string strongholdText =
                strongest != null &&
                strongest.Support >= PartyStrongholdMinimumSupport
                    ? strongest.Name + " — " +
                        strongest.Support + "%"
                    : LM.Get("ukiol_party_profile_no_stronghold");

            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_status"),
                GetPartyProfileStatusText(pParty.Support),
                PartiesIconPath,
                GetPartyProfileStatusColor(pParty.Support)
            );
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_national_support"),
                pParty.Support + "%",
                GetIdeologyIconPath(pParty.Ideology),
                GetPartyIdentityColor(pParty.Ideology, pParty.ColorSeed)
            );
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_support_5y"),
                FormatPartySupportTrendValue(pParty),
                HistoryIconPath,
                GetPartySupportTrendColor(pParty)
            );
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_cities_first"),
                leadingCities + " / " + totalCities,
                SocietyIconPath,
                new Color(0.80f, 0.84f, 0.72f, 1f)
            );
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_stronghold"),
                strongholdText,
                SocietyIconPath,
                strongest != null &&
                    strongest.Support >= PartyStrongholdMinimumSupport
                    ? GetPartyIdentityColor(pParty.Ideology, pParty.ColorSeed)
                    : new Color(0.70f, 0.71f, 0.67f, 1f)
            );

            string originValue =
                BuildPartyOriginSupportText(
                    pKingdom,
                    runtimeParties,
                    runtimeParty,
                    regions
                );
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_origin_support"),
                originValue,
                SocietyIconPath,
                new Color(0.82f, 0.88f, 0.72f, 1f)
            );

            AddEmbeddedPoliticsSectionTitle(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_regions_title"),
                SocietyIconPath
            );

            int shown = 0;
            for (int i = 0; i < regions.Count; i++)
            {
                PartyRegionSupportEntry region = regions[i];
                if (region == null || region.Support <= 0)
                {
                    continue;
                }

                AddEmbeddedPartyRegionSupportRow(
                    pParent,
                    pTemplate,
                    region,
                    pParty.Ideology,
                    pParty.ColorSeed
                );
                shown++;

                if (shown >= 5)
                {
                    break;
                }
            }

            if (shown == 0)
            {
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_regions_title"),
                    LM.Get("ukiol_party_profile_no_regions"),
                    SocietyIconPath,
                    new Color(0.70f, 0.71f, 0.67f, 1f)
                );
            }
        }


        private static void AddEmbeddedPartySupportHistorySection(
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (pParent == null || pParty == null)
            {
                return;
            }

            AddEmbeddedPoliticsSectionTitle(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_support_history_title"),
                HistoryIconPath
            );

            if (
                pParty.SupportHistory == null ||
                pParty.SupportHistory.Count == 0
            )
            {
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_support_history_title"),
                    LM.Get("ukiol_party_profile_support_no_trend"),
                    HistoryIconPath,
                    new Color(0.70f, 0.71f, 0.67f, 1f)
                );
                return;
            }

            int shown = 0;
            for (
                int i = pParty.SupportHistory.Count - 1;
                i >= 0 && shown < 6;
                i--
            )
            {
                PartySupportHistoryEntry entry =
                    pParty.SupportHistory[i];
                if (entry == null || entry.Year <= 0)
                {
                    continue;
                }
                AddEmbeddedPartySupportHistoryRow(
                    pParent,
                    pTemplate,
                    entry,
                    pParty.Ideology,
                    pParty.ColorSeed
                );
                shown++;
            }
        }

        private static void AddEmbeddedPartySupportHistoryRow(
            Transform pParent,
            Text pTemplate,
            PartySupportHistoryEntry pEntry,
            string pIdeology,
            int pColorSeed
        )
        {
            if (pParent == null || pEntry == null)
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_party_support_history_row",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 34f;
            layout.preferredHeight = 34f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(
                0.185f,
                0.198f,
                0.174f,
                0.86f
            );
            background.raycastTarget = false;

            Text year = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_support_history_year",
                pEntry.Year.ToString(),
                new Color(0.82f, 0.82f, 0.76f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (year != null)
            {
                RectTransform r = year.rectTransform;
                r.anchorMin = new Vector2(0f, 0.42f);
                r.anchorMax = new Vector2(0.55f, 1f);
                r.offsetMin = new Vector2(12f, 0f);
                r.offsetMax = new Vector2(-4f, -1f);
            }

            Text support = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_support_history_support",
                ClampInt(pEntry.Support, 0, 100) + "%",
                GetPartyIdentityColor(pIdeology, pColorSeed),
                TextAnchor.MiddleRight,
                11
            );
            if (support != null)
            {
                RectTransform r = support.rectTransform;
                r.anchorMin = new Vector2(0.55f, 0.42f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(4f, 0f);
                r.offsetMax = new Vector2(-12f, -1f);
                support.fontStyle = FontStyle.Bold;
            }

            GameObject barBackgroundObject = new GameObject(
                "party_support_history_bar_bg",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            barBackgroundObject.transform.SetParent(
                rowObject.transform,
                false
            );
            RectTransform bgRect =
                barBackgroundObject.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.offsetMin = new Vector2(12f, 5f);
            bgRect.offsetMax = new Vector2(-12f, 9f);
            Image bgImage = barBackgroundObject.GetComponent<Image>();
            bgImage.color = new Color(0.09f, 0.10f, 0.08f, 0.85f);
            bgImage.raycastTarget = false;

            GameObject barObject = new GameObject(
                "party_support_history_bar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            barObject.transform.SetParent(
                barBackgroundObject.transform,
                false
            );
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            float normalized = ClampInt(
                pEntry.Support,
                0,
                100
            ) / 100f;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(normalized, 1f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            Image barImage = barObject.GetComponent<Image>();
            barImage.color = GetPartyIdentityColor(pIdeology, pColorSeed);
            barImage.raycastTarget = false;
        }

        private static void AddEmbeddedPartyProfileHistorySection(
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty
        )
        {
            if (pParent == null || pParty == null)
            {
                return;
            }

            AddEmbeddedPoliticsSectionTitle(
                pParent,
                pTemplate,
                LM.Get("ukiol_party_profile_history_title"),
                HistoryIconPath
            );

            if (pParty.History == null || pParty.History.Count == 0)
            {
                AddEmbeddedPoliticsRow(
                    pParent,
                    pTemplate,
                    LM.Get("ukiol_party_profile_history_title"),
                    LM.Get("ukiol_party_profile_history_empty"),
                    HistoryIconPath,
                    new Color(0.70f, 0.71f, 0.67f, 1f)
                );
                return;
            }

            int shown = 0;
            for (
                int i = pParty.History.Count - 1;
                i >= 0 && shown < MaxPartyHistoryEntries;
                i--
            )
            {
                PartyHistoryEntry entry = pParty.History[i];
                if (entry == null)
                {
                    continue;
                }

                AddEmbeddedPartyHistoryRow(
                    pParent,
                    pTemplate,
                    entry
                );
                shown++;
            }
        }

        private static void AddEmbeddedPartyHistoryRow(
            Transform pParent,
            Text pTemplate,
            PartyHistoryEntry pEntry
        )
        {
            if (pParent == null || pEntry == null)
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_party_history_row",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(
                0.185f,
                0.198f,
                0.174f,
                0.88f
            );
            background.raycastTarget = false;

            string yearText = pEntry.Year > 0
                ? pEntry.Year.ToString()
                : "—";
            Text year = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_history_year",
                yearText,
                new Color(0.86f, 0.75f, 0.34f, 1f),
                TextAnchor.MiddleCenter,
                10
            );
            if (year != null)
            {
                RectTransform r = year.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(8f, 0f);
                r.sizeDelta = new Vector2(48f, 0f);
                year.fontStyle = FontStyle.Bold;
            }

            Text eventText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_history_event",
                FormatPartyHistoryEvent(pEntry),
                new Color(0.84f, 0.85f, 0.80f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (eventText != null)
            {
                RectTransform r = eventText.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(62f, 4f);
                r.offsetMax = new Vector2(-8f, -4f);
                eventText.horizontalOverflow = HorizontalWrapMode.Wrap;
                eventText.verticalOverflow = VerticalWrapMode.Truncate;
                eventText.resizeTextForBestFit = true;
                eventText.resizeTextMinSize = 8;
                eventText.resizeTextMaxSize = 10;
            }
        }

        private static string FormatPartyHistoryEvent(
            PartyHistoryEntry pEntry
        )
        {
            if (pEntry == null)
            {
                return "";
            }

            string value = pEntry.Value ?? "";
            string extra = pEntry.Extra ?? "";

            switch (pEntry.Type)
            {
                case PartyHistoryFounded:
                    return LM.Get("ukiol_party_history_founded");
                case PartyHistoryFirstLeader:
                    return string.Format(
                        LM.Get("ukiol_party_history_first_leader"),
                        value
                    );
                case PartyHistoryLeaderChanged:
                    if (string.IsNullOrEmpty(extra))
                    {
                        return string.Format(
                            LM.Get("ukiol_party_history_leader_new"),
                            value
                        );
                    }
                    return string.Format(
                        LM.Get("ukiol_party_history_leader_changed"),
                        extra,
                        value
                    );
                case PartyHistorySplitFrom:
                    return string.Format(
                        LM.Get("ukiol_party_history_split_from"),
                        value
                    );
                case PartyHistorySplitChild:
                    return string.Format(
                        LM.Get("ukiol_party_history_split_child"),
                        value
                    );
                case PartyHistoryTraitGained:
                    return string.Format(
                        LM.Get("ukiol_party_history_trait_gained"),
                        GetPartyTraitLocalizedName(value)
                    );
                case PartyHistoryTraitLost:
                    return string.Format(
                        LM.Get("ukiol_party_history_trait_lost"),
                        GetPartyTraitLocalizedName(value)
                    );
                case PartyHistoryBecameLeading:
                    return LM.Get("ukiol_party_history_became_leading");
                case PartyHistoryLostLeading:
                    return LM.Get("ukiol_party_history_lost_leading");
                case PartyHistorySuccession:
                    return string.Format(
                        LM.Get("ukiol_party_history_succession"),
                        value
                    );
                case PartyHistoryTrackingStarted:
                    return LM.Get("ukiol_party_history_tracking_started");
                default:
                    return LM.Get("ukiol_party_history_unknown");
            }
        }

        private static PoliticalParty FindPoliticalPartyById(
            List<PoliticalParty> pParties,
            string pPartyId
        )
        {
            if (
                pParties == null ||
                string.IsNullOrEmpty(pPartyId)
            )
            {
                return null;
            }

            for (int i = 0; i < pParties.Count; i++)
            {
                PoliticalParty party = pParties[i];
                if (
                    party != null &&
                    party.Id == pPartyId
                )
                {
                    return party;
                }
            }

            return null;
        }

        private static List<PartyRegionSupportEntry>
            BuildPartyRegionSupportEntries(
                Kingdom pKingdom,
                List<PoliticalParty> pParties,
                PoliticalParty pParty
            )
        {
            List<PartyRegionSupportEntry> result =
                new List<PartyRegionSupportEntry>();

            if (
                pKingdom == null ||
                pParties == null ||
                pParty == null
            )
            {
                return result;
            }

            List<City> cities = GetCitiesSafe(pKingdom);

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (city == null)
                {
                    continue;
                }

                string cityId = GetStableObjectIdentity(city);
                int support = GetPartyCitySupport(
                    pKingdom,
                    city,
                    pParty,
                    pParties
                );

                result.Add(
                    new PartyRegionSupportEntry()
                    {
                        CityId = cityId,
                        Name = GetWorldObjectDisplayName(city),
                        Support = support,
                        Population = Math.Max(
                            1,
                            GetCityPopulationSafe(city)
                        ),
                        IsOrigin =
                            !string.IsNullOrEmpty(pParty.OriginCityId) &&
                            pParty.OriginCityId == cityId
                    }
                );
            }

            result.Sort(
                delegate(
                    PartyRegionSupportEntry a,
                    PartyRegionSupportEntry b
                )
                {
                    int supportCompare =
                        b.Support.CompareTo(a.Support);
                    if (supportCompare != 0)
                    {
                        return supportCompare;
                    }

                    int populationCompare =
                        b.Population.CompareTo(a.Population);
                    if (populationCompare != 0)
                    {
                        return populationCompare;
                    }

                    return string.Compare(
                        a.Name,
                        b.Name,
                        StringComparison.Ordinal
                    );
                }
            );

            return result;
        }

        private static int CountPartyLeadingCities(
            Kingdom pKingdom,
            List<PoliticalParty> pParties,
            PoliticalParty pParty
        )
        {
            if (
                pKingdom == null ||
                pParties == null ||
                pParty == null
            )
            {
                return 0;
            }

            int count = 0;
            List<City> cities = GetCitiesSafe(pKingdom);

            for (int c = 0; c < cities.Count; c++)
            {
                City city = cities[c];
                if (city == null)
                {
                    continue;
                }

                int targetSupport = GetPartyCitySupport(
                    pKingdom,
                    city,
                    pParty,
                    pParties
                );
                if (targetSupport <= 0)
                {
                    continue;
                }

                int bestSupport = 0;
                for (int p = 0; p < pParties.Count; p++)
                {
                    PoliticalParty candidate = pParties[p];
                    if (candidate == null)
                    {
                        continue;
                    }

                    int candidateSupport =
                        GetPartyCitySupport(
                            pKingdom,
                            city,
                            candidate,
                            pParties
                        );
                    if (candidateSupport > bestSupport)
                    {
                        bestSupport = candidateSupport;
                    }
                }

                // A tie at the top still means the party shares first place
                // in this city. This avoids an arbitrary list order deciding
                // which tied party is counted as "No. 1".
                if (targetSupport >= bestSupport)
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildPartyOriginSupportText(
            Kingdom pKingdom,
            List<PoliticalParty> pParties,
            PoliticalParty pParty,
            List<PartyRegionSupportEntry> pRegions
        )
        {
            if (
                pParty == null ||
                string.IsNullOrEmpty(pParty.OriginCityName)
            )
            {
                return LM.Get("ukiol_party_profile_unknown");
            }

            if (pRegions != null)
            {
                for (int i = 0; i < pRegions.Count; i++)
                {
                    PartyRegionSupportEntry region = pRegions[i];
                    if (
                        region != null &&
                        region.IsOrigin
                    )
                    {
                        return region.Name + " — " +
                            region.Support + "%";
                    }
                }
            }

            // The historical homeland can survive in party metadata even
            // after that city leaves the kingdom. Keep showing the homeland
            // instead of silently replacing it with the current stronghold.
            return pParty.OriginCityName + " — " +
                LM.Get("ukiol_party_profile_origin_outside");
        }

        private static string GetPartyProfileStatusText(
            int pSupport
        )
        {
            if (pSupport >= 50)
            {
                return LM.Get(
                    "ukiol_party_profile_status_dominant"
                );
            }
            if (pSupport >= 30)
            {
                return LM.Get(
                    "ukiol_party_profile_status_major"
                );
            }
            if (pSupport >= 15)
            {
                return LM.Get(
                    "ukiol_party_profile_status_medium"
                );
            }
            if (pSupport >= 5)
            {
                return LM.Get(
                    "ukiol_party_profile_status_small"
                );
            }

            return LM.Get(
                "ukiol_party_profile_status_marginal"
            );
        }

        private static Color GetPartyProfileStatusColor(
            int pSupport
        )
        {
            if (pSupport >= 50)
            {
                return new Color(0.55f, 0.95f, 0.55f, 1f);
            }
            if (pSupport >= 30)
            {
                return new Color(0.86f, 0.88f, 0.52f, 1f);
            }
            if (pSupport >= 15)
            {
                return new Color(0.91f, 0.76f, 0.43f, 1f);
            }
            if (pSupport >= 5)
            {
                return new Color(0.76f, 0.78f, 0.73f, 1f);
            }

            return new Color(0.62f, 0.63f, 0.60f, 1f);
        }

        private static void AddEmbeddedPartyRegionSupportRow(
            Transform pParent,
            Text pTemplate,
            PartyRegionSupportEntry pRegion,
            string pIdeology,
            int pColorSeed
        )
        {
            if (
                pParent == null ||
                pRegion == null
            )
            {
                return;
            }

            const float rowHeight = 42f;
            Color ideologyColor =
                GetPartyIdentityColor(pIdeology, pColorSeed);

            GameObject rowObject = new GameObject(
                "ukiol_party_profile_region",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = rowHeight;
            layout.preferredHeight = rowHeight;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            bool evenRow = (pParent.childCount % 2) == 0;
            background.color = evenRow
                ? new Color(0.207f, 0.222f, 0.190f, 0.90f)
                : new Color(0.192f, 0.208f, 0.178f, 0.90f);
            background.raycastTarget = false;

            GameObject stripeObject = new GameObject(
                "region_stripe",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            stripeObject.transform.SetParent(rowObject.transform, false);
            RectTransform stripeRect =
                stripeObject.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(4f, 0f);
            Image stripe = stripeObject.GetComponent<Image>();
            stripe.color = ideologyColor;
            stripe.raycastTarget = false;

            string nameText = pRegion.Name;
            if (pRegion.IsOrigin)
            {
                nameText += LM.Get(
                    "ukiol_party_profile_region_origin_suffix"
                );
            }

            Text name = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "region_name",
                nameText,
                Color.white,
                TextAnchor.MiddleLeft,
                11
            );
            if (name != null)
            {
                RectTransform r = name.rectTransform;
                r.anchorMin = new Vector2(0f, 0.42f);
                r.anchorMax = new Vector2(0.75f, 1f);
                r.offsetMin = new Vector2(12f, 0f);
                r.offsetMax = new Vector2(-4f, -1f);
                name.fontStyle = FontStyle.Bold;
                name.resizeTextMinSize = 8;
                name.resizeTextMaxSize = 11;
            }

            Text support = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "region_support",
                pRegion.Support + "%",
                ideologyColor,
                TextAnchor.MiddleRight,
                12
            );
            if (support != null)
            {
                RectTransform r = support.rectTransform;
                r.anchorMin = new Vector2(0.74f, 0.42f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(2f, 0f);
                r.offsetMax = new Vector2(-10f, -1f);
                support.fontStyle = FontStyle.Bold;
            }

            GameObject barBg = new GameObject(
                "region_support_bg",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            barBg.transform.SetParent(rowObject.transform, false);
            RectTransform bgRect =
                barBg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.offsetMin = new Vector2(12f, 8f);
            bgRect.offsetMax = new Vector2(-10f, 13f);
            Image bg = barBg.GetComponent<Image>();
            bg.color = new Color(0.11f, 0.12f, 0.10f, 0.90f);
            bg.raycastTarget = false;

            GameObject fill = new GameObject(
                "region_support_fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            fill.transform.SetParent(barBg.transform, false);
            RectTransform fillRect =
                fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(
                Mathf.Clamp01(pRegion.Support / 100f),
                1f
            );
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = ideologyColor;
            fillImage.raycastTarget = false;
        }

        private static void AddEmbeddedPartyProfileTraitsCard(
            Transform pParent,
            Text pTemplate,
            List<string> pTraits
        )
        {
            if (pParent == null)
            {
                return;
            }

            GameObject card = new GameObject(
                "ukiol_party_profile_traits",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            card.transform.SetParent(pParent, false);

            LayoutElement layout = card.GetComponent<LayoutElement>();
            int traitCount = pTraits == null
                ? 0
                : Mathf.Min(pTraits.Count, MaxPartyTraits);
            float cardHeight = traitCount <= 0
                ? 42f
                : 14f + traitCount * 30f;
            layout.minHeight = cardHeight;
            layout.preferredHeight = cardHeight;
            layout.flexibleHeight = 0f;

            Image background = card.GetComponent<Image>();
            background.color = new Color(0.196f, 0.212f, 0.181f, 0.82f);
            background.raycastTarget = false;

            AddEmbeddedPartyProfileTraitRows(
                card.transform,
                pTemplate,
                pTraits
            );
        }

        // The compact chips used in the party list intentionally share one
        // horizontal band. That works in a small card, but long localized
        // labels can visually collide in the full profile. The profile gets
        // a dedicated stacked layout: one readable trait per row.
        private static void AddEmbeddedPartyProfileTraitRows(
            Transform pParent,
            Text pTemplate,
            List<string> pTraits
        )
        {
            if (pParent == null)
            {
                return;
            }

            List<string> traits = pTraits == null
                ? new List<string>()
                : pTraits;

            if (traits.Count == 0)
            {
                Text emptyText = CreateEmbeddedPoliticsText(
                    pParent,
                    pTemplate,
                    "profile_traits_empty",
                    LM.Get("ukiol_party_traits_none"),
                    new Color(0.64f, 0.65f, 0.61f, 1f),
                    TextAnchor.MiddleCenter,
                    9
                );
                if (emptyText != null)
                {
                    RectTransform r = emptyText.rectTransform;
                    r.anchorMin = Vector2.zero;
                    r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(12f, 5f);
                    r.offsetMax = new Vector2(-12f, -5f);
                }
                return;
            }

            int count = Mathf.Min(traits.Count, MaxPartyTraits);
            for (int i = 0; i < count; i++)
            {
                string trait = traits[i];
                GameObject chip = new GameObject(
                    "profile_trait_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );
                chip.transform.SetParent(pParent, false);

                RectTransform chipRect = chip.GetComponent<RectTransform>();
                chipRect.anchorMin = new Vector2(0f, 1f);
                chipRect.anchorMax = new Vector2(1f, 1f);
                chipRect.pivot = new Vector2(0.5f, 1f);
                chipRect.anchoredPosition = new Vector2(0f, -7f - i * 30f);
                chipRect.sizeDelta = new Vector2(-24f, 25f);

                Image chipBackground = chip.GetComponent<Image>();
                chipBackground.color = GetPartyTraitChipColor(trait);
                chipBackground.raycastTarget = false;

                Text chipText = CreateEmbeddedPoliticsText(
                    chip.transform,
                    pTemplate,
                    "text",
                    GetPartyTraitLocalizedName(trait),
                    GetPartyTraitChipTextColor(trait),
                    TextAnchor.MiddleCenter,
                    10
                );
                if (chipText != null)
                {
                    RectTransform r = chipText.rectTransform;
                    r.anchorMin = Vector2.zero;
                    r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(6f, 1f);
                    r.offsetMax = new Vector2(-6f, -1f);
                    chipText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    chipText.verticalOverflow = VerticalWrapMode.Truncate;
                    chipText.resizeTextForBestFit = true;
                    chipText.resizeTextMinSize = 8;
                    chipText.resizeTextMaxSize = 10;
                }
            }
        }

        private static void AddEmbeddedPartySummaryRow(
            Transform pParent,
            Text pTemplate,
            List<PartyOverviewEntry> pParties
        )
        {
            if (
                pParent == null ||
                pParties == null ||
                pParties.Count == 0
            )
            {
                return;
            }

            PartyOverviewEntry leading = pParties[0];

            GameObject rowObject = new GameObject(
                "ukiol_politics_party_summary",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            // The leading party may have a player-defined 48-character name.
            // Keep two lines available so the summary does not clip it.
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.176f, 0.192f, 0.162f, 0.88f);
            background.raycastTarget = false;

            Text countText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_count",
                string.Format(
                    LM.Get("ukiol_party_active_count"),
                    pParties.Count
                ),
                new Color(0.77f, 0.79f, 0.74f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (countText != null)
            {
                RectTransform r = countText.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(0.28f, 1f);
                r.offsetMin = new Vector2(9f, 1f);
                r.offsetMax = new Vector2(-3f, -1f);
                countText.resizeTextMinSize = 8;
                countText.resizeTextMaxSize = 10;
            }

            string leadingText = string.Format(
                LM.Get("ukiol_party_leading_short"),
                leading.Name
            );
            Text leaderText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_leading",
                leadingText,
                new Color(0.90f, 0.82f, 0.44f, 1f),
                TextAnchor.MiddleRight,
                10
            );
            if (leaderText != null)
            {
                RectTransform r = leaderText.rectTransform;
                r.anchorMin = new Vector2(0.28f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(3f, 1f);
                r.offsetMax = new Vector2(-9f, -1f);
                leaderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                leaderText.verticalOverflow = VerticalWrapMode.Truncate;
                leaderText.resizeTextForBestFit = true;
                leaderText.resizeTextMinSize = 7;
                leaderText.resizeTextMaxSize = 10;
            }
        }

        private static void AddEmbeddedPartyRow(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            Transform pParent,
            Text pTemplate,
            PartyOverviewEntry pParty,
            int pRank
        )
        {
            if (pParent == null || pParty == null)
            {
                return;
            }

            // v1.3.8.4: the party card is intentionally compact and split into
            // clear native-like information bands: identity, leader, political
            // profile, traits, then the thin support bar.
            const float rowHeight = 112f;

            GameObject rowObject = new GameObject(
                "ukiol_politics_party_card",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, rowHeight);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = rowHeight;
            layout.preferredHeight = rowHeight;
            layout.flexibleHeight = 0f;

            Image rowBackground = rowObject.GetComponent<Image>();
            bool evenRow = (pParent.childCount % 2) == 0;
            rowBackground.color = pRank == 1
                ? new Color(0.225f, 0.232f, 0.186f, 0.94f)
                : (evenRow
                    ? new Color(0.203f, 0.220f, 0.187f, 0.90f)
                    : new Color(0.190f, 0.206f, 0.176f, 0.90f));
            rowBackground.raycastTarget = true;

            Button cardButton = rowObject.GetComponent<Button>();
            cardButton.targetGraphic = rowBackground;
            ColorBlock cardColors = cardButton.colors;
            cardColors.normalColor = Color.white;
            cardColors.highlightedColor = new Color(1f, 1f, 0.92f, 1f);
            cardColors.pressedColor = new Color(0.88f, 0.86f, 0.72f, 1f);
            cardColors.selectedColor = Color.white;
            cardColors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);
            cardColors.colorMultiplier = 1f;
            cardButton.colors = cardColors;
            string capturedPartyId = pParty.Id;
            cardButton.onClick.AddListener(
                delegate
                {
                    OpenNativeKingdomPartyProfile(
                        pWindow,
                        pKingdom,
                        capturedPartyId
                    );
                }
            );

            Color ideologyColor = GetPartyIdentityColor(
                pParty.Ideology,
                pParty.ColorSeed
            );

            GameObject stripeObject = new GameObject(
                "party_stripe",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            stripeObject.transform.SetParent(rowObject.transform, false);
            RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(4f, 0f);
            stripeObject.GetComponent<Image>().color = ideologyColor;
            stripeObject.GetComponent<Image>().raycastTarget = false;

            GameObject iconPlate = new GameObject(
                "party_icon_plate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconPlate.transform.SetParent(rowObject.transform, false);
            RectTransform plateRect = iconPlate.GetComponent<RectTransform>();
            plateRect.anchorMin = new Vector2(0f, 1f);
            plateRect.anchorMax = new Vector2(0f, 1f);
            plateRect.pivot = new Vector2(0f, 1f);
            plateRect.anchoredPosition = new Vector2(12f, -12f);
            plateRect.sizeDelta = new Vector2(40f, 40f);
            Image plateImage = iconPlate.GetComponent<Image>();
            plateImage.color = new Color(
                ideologyColor.r * 0.32f + 0.035f,
                ideologyColor.g * 0.32f + 0.035f,
                ideologyColor.b * 0.32f + 0.035f,
                0.78f
            );
            plateImage.raycastTarget = false;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(iconPlate.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(
                GetIdeologyIconPath(pParty.Ideology)
            );
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Rank is now physically attached to the icon instead of floating
            // underneath it as a separate label.
            GameObject rankPlate = new GameObject(
                "party_rank_plate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            rankPlate.transform.SetParent(iconPlate.transform, false);
            RectTransform rankPlateRect = rankPlate.GetComponent<RectTransform>();
            rankPlateRect.anchorMin = new Vector2(1f, 0f);
            rankPlateRect.anchorMax = new Vector2(1f, 0f);
            rankPlateRect.pivot = new Vector2(1f, 0f);
            rankPlateRect.anchoredPosition = new Vector2(0f, 0f);
            rankPlateRect.sizeDelta = new Vector2(16f, 12f);
            Image rankPlateImage = rankPlate.GetComponent<Image>();
            rankPlateImage.color = new Color(0.10f, 0.11f, 0.09f, 0.94f);
            rankPlateImage.raycastTarget = false;

            Text rankBadge = CreateEmbeddedPoliticsText(
                rankPlate.transform,
                pTemplate,
                "party_rank",
                pRank.ToString(),
                new Color(0.93f, 0.83f, 0.45f, 1f),
                TextAnchor.MiddleCenter,
                8
            );
            if (rankBadge != null)
            {
                RectTransform r = rankBadge.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                rankBadge.resizeTextForBestFit = true;
                rankBadge.resizeTextMinSize = 7;
                rankBadge.resizeTextMaxSize = 8;
            }

            Text nameText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_name",
                pParty.Name,
                Color.white,
                TextAnchor.MiddleLeft,
                14
            );
            if (nameText != null)
            {
                RectTransform r = nameText.rectTransform;
                r.anchorMin = new Vector2(0f, 0.70f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(60f, 0f);
                r.offsetMax = new Vector2(-76f, -3f);
                nameText.fontStyle = FontStyle.Bold;
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 7;
                nameText.resizeTextMaxSize = 14;
            }

            Text supportText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_support",
                pParty.Support + "%",
                ideologyColor,
                TextAnchor.MiddleRight,
                17
            );
            if (supportText != null)
            {
                RectTransform r = supportText.rectTransform;
                r.anchorMin = new Vector2(1f, 0.75f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, 0.5f);
                r.anchoredPosition = new Vector2(-9f, -1f);
                r.sizeDelta = new Vector2(64f, 0f);
                supportText.fontStyle = FontStyle.Bold;
                supportText.resizeTextMinSize = 12;
                supportText.resizeTextMaxSize = 17;
            }

            string leader = string.IsNullOrEmpty(pParty.Leader)
                ? LM.Get("ukiol_party_leader_unknown")
                : pParty.Leader;
            string founded = pParty.FoundedYear > 0
                ? string.Format(
                    LM.Get("ukiol_party_founded_short"),
                    pParty.FoundedYear
                )
                : "";

            Text leaderText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_leader",
                LM.Get("ukiol_party_leader_label") + ": " + leader,
                new Color(0.79f, 0.81f, 0.76f, 1f),
                TextAnchor.MiddleLeft,
                11
            );
            if (leaderText != null)
            {
                RectTransform r = leaderText.rectTransform;
                r.anchorMin = new Vector2(0f, 0.52f);
                r.anchorMax = new Vector2(0.78f, 0.70f);
                r.offsetMin = new Vector2(60f, 0f);
                r.offsetMax = new Vector2(-3f, 0f);
                leaderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                leaderText.verticalOverflow = VerticalWrapMode.Truncate;
                leaderText.resizeTextForBestFit = true;
                leaderText.resizeTextMinSize = 9;
                leaderText.resizeTextMaxSize = 11;
            }

            if (!string.IsNullOrEmpty(founded))
            {
                Text foundedText = CreateEmbeddedPoliticsText(
                    rowObject.transform,
                    pTemplate,
                    "party_founded",
                    founded,
                    new Color(0.62f, 0.65f, 0.60f, 1f),
                    TextAnchor.MiddleRight,
                    8
                );
                if (foundedText != null)
                {
                    RectTransform r = foundedText.rectTransform;
                    r.anchorMin = new Vector2(0.76f, 0.52f);
                    r.anchorMax = new Vector2(1f, 0.70f);
                    r.offsetMin = new Vector2(3f, 0f);
                    r.offsetMax = new Vector2(-9f, 0f);
                    foundedText.resizeTextMinSize = 7;
                    foundedText.resizeTextMaxSize = 8;
                }
            }

            // Ideology and party profile are deliberately separate now. This
            // prevents one long "ideology · position · strategy · stance" line
            // from reading like a single overloaded field.
            Text ideologyText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_ideology",
                GetIdeologyName(pParty.Ideology),
                ideologyColor,
                TextAnchor.MiddleLeft,
                11
            );
            if (ideologyText != null)
            {
                RectTransform r = ideologyText.rectTransform;
                r.anchorMin = new Vector2(0f, 0.32f);
                r.anchorMax = new Vector2(0.58f, 0.52f);
                r.offsetMin = new Vector2(60f, 0f);
                r.offsetMax = new Vector2(-3f, 0f);
                ideologyText.fontStyle = FontStyle.Bold;
                ideologyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                ideologyText.verticalOverflow = VerticalWrapMode.Truncate;
                ideologyText.resizeTextForBestFit = true;
                ideologyText.resizeTextMinSize = 8;
                ideologyText.resizeTextMaxSize = 11;
            }

            string profile = FormatPartyStoredProfile(
                pParty.Position,
                pParty.Strategy,
                pParty.ForeignStance
            );
            string stackedProfile = string.IsNullOrEmpty(profile)
                ? profile
                : profile.Replace(" · ", "\n");
            Text profileText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "party_profile",
                stackedProfile,
                new Color(0.73f, 0.80f, 0.86f, 1f),
                TextAnchor.MiddleRight,
                9
            );
            if (profileText != null)
            {
                RectTransform r = profileText.rectTransform;
                r.anchorMin = new Vector2(0.56f, 0.29f);
                r.anchorMax = new Vector2(1f, 0.54f);
                r.offsetMin = new Vector2(3f, 0f);
                r.offsetMax = new Vector2(-9f, 0f);
                profileText.horizontalOverflow = HorizontalWrapMode.Wrap;
                profileText.verticalOverflow = VerticalWrapMode.Truncate;
                profileText.resizeTextForBestFit = true;
                profileText.resizeTextMinSize = 8;
                profileText.resizeTextMaxSize = 9;
            }

            AddEmbeddedPartyTraitChips(
                rowObject.transform,
                pTemplate,
                pParty.Traits
            );

            AddEmbeddedPartySupportBar(
                rowObject.transform,
                pParty.Support,
                ideologyColor
            );
        }

        private static void AddEmbeddedPartyTraitChips(
            Transform pParent,
            Text pTemplate,
            List<string> pTraits
        )
        {
            if (pParent == null)
            {
                return;
            }

            List<string> traits = pTraits == null
                ? new List<string>()
                : pTraits;

            if (traits.Count == 0)
            {
                Text emptyText = CreateEmbeddedPoliticsText(
                    pParent,
                    pTemplate,
                    "party_traits_empty",
                    LM.Get("ukiol_party_traits_none"),
                    new Color(0.64f, 0.65f, 0.61f, 1f),
                    TextAnchor.MiddleLeft,
                    8
                );
                if (emptyText != null)
                {
                    RectTransform r = emptyText.rectTransform;
                    r.anchorMin = new Vector2(0f, 0.10f);
                    r.anchorMax = new Vector2(1f, 0.32f);
                    r.offsetMin = new Vector2(60f, 0f);
                    r.offsetMax = new Vector2(-9f, 0f);
                    emptyText.resizeTextMinSize = 7;
                    emptyText.resizeTextMaxSize = 8;
                }
                return;
            }

            int count = Mathf.Min(traits.Count, MaxPartyTraits);
            float gap = 3f;

            // Equal-width chips were the reason long Russian labels such as
            // "Милитаризированная" wrapped. Give each chip a width proportional
            // to the localized label instead.
            float[] weights = new float[count];
            float totalWeight = 0f;
            for (int i = 0; i < count; i++)
            {
                string localized = GetPartyTraitLocalizedName(traits[i]);
                float weight = Mathf.Clamp(
                    string.IsNullOrEmpty(localized) ? 8f : localized.Length,
                    8f,
                    22f
                );
                weights[i] = weight;
                totalWeight += weight;
            }
            if (totalWeight <= 0f)
            {
                totalWeight = count;
            }

            GameObject area = new GameObject(
                "party_traits_area",
                typeof(RectTransform)
            );
            area.transform.SetParent(pParent, false);
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0f, 0.08f);
            areaRect.anchorMax = new Vector2(1f, 0.29f);
            areaRect.offsetMin = new Vector2(60f, 1f);
            areaRect.offsetMax = new Vector2(-9f, -1f);

            float cursor = 0f;
            for (int i = 0; i < count; i++)
            {
                string trait = traits[i];
                float minX = cursor / totalWeight;
                cursor += weights[i];
                float maxX = cursor / totalWeight;

                GameObject chip = new GameObject(
                    "party_trait_chip_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(RectMask2D)
                );
                chip.transform.SetParent(area.transform, false);
                RectTransform chipRect = chip.GetComponent<RectTransform>();
                chipRect.anchorMin = new Vector2(minX, 0f);
                chipRect.anchorMax = new Vector2(maxX, 1f);
                chipRect.offsetMin = new Vector2(
                    i == 0 ? 0f : gap * 0.5f,
                    0f
                );
                chipRect.offsetMax = new Vector2(
                    i == count - 1 ? 0f : -gap * 0.5f,
                    0f
                );

                Image chipBackground = chip.GetComponent<Image>();
                chipBackground.color = GetPartyTraitChipColor(trait);
                chipBackground.raycastTarget = false;

                Text chipText = CreateEmbeddedPoliticsText(
                    chip.transform,
                    pTemplate,
                    "text",
                    GetPartyTraitLocalizedName(trait),
                    GetPartyTraitChipTextColor(trait),
                    TextAnchor.MiddleCenter,
                    8
                );
                if (chipText != null)
                {
                    RectTransform r = chipText.rectTransform;
                    r.anchorMin = Vector2.zero;
                    r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(2f, 0f);
                    r.offsetMax = new Vector2(-2f, 0f);
                    // Never allow long localized trait names to draw over
                    // neighbouring chips. Wrap enables best-fit to respect the
                    // chip width; RectMask2D above is the final hard boundary.
                    chipText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    chipText.verticalOverflow = VerticalWrapMode.Truncate;
                    chipText.resizeTextForBestFit = true;
                    chipText.resizeTextMinSize = 5;
                    chipText.resizeTextMaxSize = 8;
                }
            }
        }

        private static int NormalizePartyColorSeed(
            int pSeed
        )
        {
            if (PartyColorVariantCount <= 0)
            {
                return 0;
            }

            int seed = pSeed % PartyColorVariantCount;
            if (seed < 0)
            {
                seed += PartyColorVariantCount;
            }

            return seed;
        }

        private static HashSet<int> GetUsedPartyColorSeeds(
            Kingdom pKingdom,
            string pIdeology,
            int pIgnoreSlot
        )
        {
            HashSet<int> used = new HashSet<int>();

            if (
                pKingdom == null ||
                pKingdom.data == null ||
                !IsValidIdeology(pIdeology)
            )
            {
                return used;
            }

            int count = ClampInt(
                GetKingdomIntData(
                    pKingdom,
                    PartySlotCountDataKey,
                    0
                ),
                0,
                MaxPoliticalParties
            );

            for (int slot = 0; slot < count; slot++)
            {
                if (slot == pIgnoreSlot)
                {
                    continue;
                }

                int active = GetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2ActivePrefix,
                        slot
                    ),
                    0
                );
                if (active == 0)
                {
                    continue;
                }

                string ideology = GetKingdomStringData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2IdeologyPrefix,
                        slot
                    ),
                    ""
                );
                if (ideology != pIdeology)
                {
                    continue;
                }

                int seed = GetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2ColorSeedPrefix,
                        slot
                    ),
                    -1
                );
                if (
                    seed >= 0 &&
                    seed < PartyColorVariantCount
                )
                {
                    used.Add(seed);
                }
            }

            return used;
        }

        private static int ChooseNewPartyColorSeed(
            Kingdom pKingdom,
            string pIdeology,
            string pPartyId,
            int pIgnoreSlot
        )
        {
            HashSet<int> used = GetUsedPartyColorSeeds(
                pKingdom,
                pIdeology,
                pIgnoreSlot
            );

            int preferred = NormalizePartyColorSeed(
                StablePartyHash(
                    (pPartyId ?? "") + "|party_color"
                )
            );

            for (int step = 0;
                step < PartyColorVariantCount;
                step++)
            {
                int candidate = NormalizePartyColorSeed(
                    preferred + step
                );
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }

            return preferred;
        }

        private static int ChooseRelatedPartyColorSeed(
            Kingdom pKingdom,
            string pIdeology,
            int pParentSeed,
            int pIgnoreSlot
        )
        {
            HashSet<int> used = GetUsedPartyColorSeeds(
                pKingdom,
                pIdeology,
                pIgnoreSlot
            );
            int parent = NormalizePartyColorSeed(pParentSeed);

            int[] offsets = new int[]
            {
                1, -1, 2, -2, 3, -3,
                4, -4, 5, -5, 6
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                int candidate = NormalizePartyColorSeed(
                    parent + offsets[i]
                );
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }

            return ChooseNewPartyColorSeed(
                pKingdom,
                pIdeology,
                "split|" + parent,
                pIgnoreSlot
            );
        }

        private static Color GetPartyIdentityColor(
            string pIdeology,
            int pColorSeed
        )
        {
            Color baseColor = GetEmbeddedPartyIdeologyColor(
                pIdeology
            );

            float hue;
            float saturation;
            float value;
            Color.RGBToHSV(
                baseColor,
                out hue,
                out saturation,
                out value
            );

            int seed = NormalizePartyColorSeed(pColorSeed);

            // Neutral ideologies (for example anarchism's grey base) remain
            // neutral instead of suddenly turning bright red/blue just because
            // HSV hue is undefined at zero saturation.
            if (saturation < 0.16f)
            {
                float neutralValueScale =
                    (seed % 4 == 0)
                        ? 0.82f
                        : (seed % 4 == 1)
                            ? 0.94f
                            : (seed % 4 == 2)
                                ? 1.05f
                                : 0.88f;

                value = Mathf.Clamp(
                    value * neutralValueScale,
                    0.48f,
                    0.90f
                );
                saturation = (seed % 3) * 0.035f;

                if (saturation > 0f)
                {
                    hue = (seed % 2 == 0)
                        ? 0.10f
                        : 0.58f;
                }

                Color neutralResult = Color.HSVToRGB(
                    hue,
                    saturation,
                    value
                );
                neutralResult.a = 1f;
                return neutralResult;
            }

            float hueOffset = 0f;
            switch (seed)
            {
                case 1:
                    hueOffset = 0.028f;
                    break;
                case 2:
                    hueOffset = -0.028f;
                    break;
                case 3:
                    hueOffset = 0.056f;
                    break;
                case 4:
                    hueOffset = -0.056f;
                    break;
                case 5:
                    hueOffset = 0.085f;
                    break;
                case 6:
                    hueOffset = -0.085f;
                    break;
                case 7:
                    hueOffset = 0.115f;
                    break;
                case 8:
                    hueOffset = -0.115f;
                    break;
                case 9:
                    hueOffset = 0.145f;
                    break;
                case 10:
                    hueOffset = -0.145f;
                    break;
                case 11:
                    hueOffset = 0.175f;
                    break;
            }

            hue = Mathf.Repeat(
                hue + hueOffset,
                1f
            );

            // Variants stay recognizably inside the same ideological family
            // while remaining visibly different in the party list.
            float saturationScale =
                (seed % 3 == 0)
                    ? 0.92f
                    : (seed % 3 == 1)
                        ? 1.06f
                        : 1.00f;
            float valueScale =
                (seed % 4 == 0)
                    ? 0.92f
                    : (seed % 4 == 1)
                        ? 1.05f
                        : (seed % 4 == 2)
                            ? 0.98f
                            : 1.01f;

            saturation = Mathf.Clamp(
                saturation * saturationScale,
                0.32f,
                0.96f
            );
            value = Mathf.Clamp(
                value * valueScale,
                0.58f,
                1f
            );

            Color result = Color.HSVToRGB(
                hue,
                saturation,
                value
            );
            result.a = 1f;
            return result;
        }

        private static Color GetEmbeddedPartyIdeologyColor(
            string pIdeology
        )
        {
            if (pIdeology == MonarchismIdeologyId)
            {
                return new Color(0.82f, 0.64f, 0.28f, 1f);
            }
            if (pIdeology == ConservatismIdeologyId)
            {
                return new Color(0.42f, 0.58f, 0.92f, 1f);
            }
            if (pIdeology == LiberalismIdeologyId)
            {
                return new Color(0.34f, 0.72f, 1f, 1f);
            }
            if (pIdeology == DemocracyIdeologyId)
            {
                return new Color(0.35f, 0.85f, 0.90f, 1f);
            }
            if (pIdeology == SocialismIdeologyId)
            {
                return new Color(0.93f, 0.44f, 0.48f, 1f);
            }
            if (pIdeology == CommunismIdeologyId)
            {
                return new Color(0.93f, 0.27f, 0.22f, 1f);
            }
            if (pIdeology == FascismIdeologyId)
            {
                return new Color(0.88f, 0.56f, 0.20f, 1f);
            }
            if (pIdeology == AnarchismIdeologyId)
            {
                return new Color(0.72f, 0.72f, 0.72f, 1f);
            }
            if (pIdeology == SyndicalismIdeologyId)
            {
                return new Color(0.92f, 0.56f, 0.26f, 1f);
            }

            return new Color(0.78f, 0.80f, 0.74f, 1f);
        }

        private static Color GetPartyTraitChipColor(string pTrait)
        {
            if (
                pTrait == PartyTraitCorrupt ||
                pTrait == PartyTraitSplintered ||
                pTrait == PartyTraitFactional
            )
            {
                return new Color(0.34f, 0.18f, 0.16f, 0.90f);
            }

            if (
                pTrait == PartyTraitRevolutionary ||
                pTrait == PartyTraitMilitarized ||
                pTrait == PartyTraitPopulist
            )
            {
                return new Color(0.31f, 0.25f, 0.12f, 0.90f);
            }

            return new Color(0.17f, 0.25f, 0.29f, 0.90f);
        }

        private static Color GetPartyTraitChipTextColor(string pTrait)
        {
            if (
                pTrait == PartyTraitCorrupt ||
                pTrait == PartyTraitSplintered ||
                pTrait == PartyTraitFactional
            )
            {
                return new Color(1f, 0.68f, 0.58f, 1f);
            }

            if (
                pTrait == PartyTraitRevolutionary ||
                pTrait == PartyTraitMilitarized ||
                pTrait == PartyTraitPopulist
            )
            {
                return new Color(0.98f, 0.84f, 0.48f, 1f);
            }

            return new Color(0.68f, 0.84f, 0.96f, 1f);
        }

        private static void AddEmbeddedPartySupportBar(
            Transform pParent,
            int pSupport,
            Color pColor
        )
        {
            if (pParent == null)
            {
                return;
            }

            GameObject background = new GameObject(
                "party_support_bar_bg",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            background.transform.SetParent(pParent, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.offsetMin = new Vector2(64f, 4f);
            bgRect.offsetMax = new Vector2(-10f, 9f);
            Image bgImage = background.GetComponent<Image>();
            bgImage.color = new Color(0.12f, 0.13f, 0.11f, 0.88f);
            bgImage.raycastTarget = false;

            GameObject fill = new GameObject(
                "party_support_bar_fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            fill.transform.SetParent(background.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            float ratio = Mathf.Clamp01(pSupport / 100f);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = pColor;
            fillImage.raycastTarget = false;
        }

        private static void AddEmbeddedSocietySummaryRow(
            Transform pParent,
            Text pTemplate,
            List<SocietyCityEntry> pCities
        )
        {
            if (pParent == null)
            {
                return;
            }

            int count = pCities == null ? 0 : pCities.Count;
            int stabilityTotal = 0;

            if (pCities != null)
            {
                for (int i = 0; i < pCities.Count; i++)
                {
                    stabilityTotal += pCities[i].Stability;
                }
            }

            int averageStability = count <= 0
                ? 0
                : (int)Math.Round(
                    stabilityTotal / (double)count
                );

            GameObject rowObject = new GameObject(
                "ukiol_politics_society_summary",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 26f;
            layout.preferredHeight = 26f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.176f, 0.192f, 0.162f, 0.88f);
            background.raycastTarget = false;

            Text countText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "society_city_count",
                string.Format(
                    LM.Get("ukiol_society_city_count"),
                    count
                ),
                new Color(0.78f, 0.80f, 0.75f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (countText != null)
            {
                RectTransform r = countText.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(0.42f, 1f);
                r.offsetMin = new Vector2(9f, 1f);
                r.offsetMax = new Vector2(-3f, -1f);
            }

            Text stabilityText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "society_average_stability",
                string.Format(
                    LM.Get("ukiol_society_average_stability"),
                    averageStability
                ),
                ParseHtmlColor(GetStabilityColor(averageStability)),
                TextAnchor.MiddleRight,
                10
            );
            if (stabilityText != null)
            {
                RectTransform r = stabilityText.rectTransform;
                r.anchorMin = new Vector2(0.42f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(3f, 1f);
                r.offsetMax = new Vector2(-9f, -1f);
                stabilityText.resizeTextForBestFit = true;
                stabilityText.resizeTextMinSize = 8;
                stabilityText.resizeTextMaxSize = 10;
            }
        }

        private static void AddEmbeddedPoliticsSectionTitle(
            Transform pParent,
            Text pTemplate,
            string pText,
            string pIconPath
        )
        {
            if (pParent == null)
            {
                return;
            }

            GameObject rowObject = new GameObject(
                "ukiol_politics_section_title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 26f;
            layout.preferredHeight = 26f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.145f, 0.157f, 0.136f, 0.76f);
            background.raycastTarget = false;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(rowObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(17f, 17f);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(pIconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text title = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "section_title",
                pText,
                new Color(0.90f, 0.82f, 0.45f, 1f),
                TextAnchor.MiddleLeft,
                11
            );
            if (title != null)
            {
                RectTransform r = title.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(31f, 1f);
                r.offsetMax = new Vector2(-8f, -1f);
                title.fontStyle = FontStyle.Bold;
            }
        }

        private static void AddEmbeddedSocietyCityRow(
            Transform pParent,
            Text pTemplate,
            SocietyCityEntry pCity
        )
        {
            if (pParent == null || pCity == null)
            {
                return;
            }

            const float rowHeight = 82f;

            GameObject rowObject = new GameObject(
                "ukiol_politics_society_city",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = rowHeight;
            layout.preferredHeight = rowHeight;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            bool evenRow = (pParent.childCount % 2) == 0;
            background.color = evenRow
                ? new Color(0.207f, 0.222f, 0.190f, 0.90f)
                : new Color(0.192f, 0.208f, 0.178f, 0.90f);
            background.raycastTarget = false;

            Color ideologyColor = GetEmbeddedPartyIdeologyColor(
                pCity.DominantIdeology
            );

            GameObject stripeObject = new GameObject(
                "city_stripe",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            stripeObject.transform.SetParent(rowObject.transform, false);
            RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(4f, 0f);
            Image stripe = stripeObject.GetComponent<Image>();
            stripe.color = ideologyColor;
            stripe.raycastTarget = false;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(rowObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(12f, -12f);
            iconRect.sizeDelta = new Vector2(30f, 30f);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(
                GetIdeologyIconPath(pCity.DominantIdeology)
            );
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            string cityName = pCity.Name;
            if (pCity.IsCapital)
            {
                cityName += LM.Get("ukiol_society_capital_suffix");
            }

            Text nameText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "city_name",
                cityName,
                Color.white,
                TextAnchor.MiddleLeft,
                13
            );
            if (nameText != null)
            {
                RectTransform r = nameText.rectTransform;
                r.anchorMin = new Vector2(0f, 0.72f);
                r.anchorMax = new Vector2(0.72f, 1f);
                r.offsetMin = new Vector2(52f, 0f);
                r.offsetMax = new Vector2(-4f, -3f);
                nameText.fontStyle = FontStyle.Bold;
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 9;
                nameText.resizeTextMaxSize = 13;
            }

            Text stabilityText = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "city_stability",
                string.Format(
                    LM.Get("ukiol_society_stability_short"),
                    pCity.Stability
                ),
                ParseHtmlColor(GetStabilityColor(pCity.Stability)),
                TextAnchor.MiddleRight,
                9
            );
            if (stabilityText != null)
            {
                RectTransform r = stabilityText.rectTransform;
                r.anchorMin = new Vector2(0.70f, 0.72f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(2f, 0f);
                r.offsetMax = new Vector2(-9f, -3f);
                stabilityText.resizeTextForBestFit = true;
                stabilityText.resizeTextMinSize = 7;
                stabilityText.resizeTextMaxSize = 9;
            }

            string ideologyText = FormatCityPoliticalIdentity(
                pCity.City,
                pCity.DominantIdeology,
                pCity.DominantSupport
            );
            Text ideologyLabel = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "city_ideology",
                ideologyText,
                ideologyColor,
                TextAnchor.MiddleLeft,
                10
            );
            if (ideologyLabel != null)
            {
                RectTransform r = ideologyLabel.rectTransform;
                r.anchorMin = new Vector2(0f, 0.47f);
                r.anchorMax = new Vector2(1f, 0.72f);
                r.offsetMin = new Vector2(52f, 0f);
                r.offsetMax = new Vector2(-9f, 0f);
                ideologyLabel.resizeTextForBestFit = true;
                ideologyLabel.resizeTextMinSize = 8;
                ideologyLabel.resizeTextMaxSize = 10;
            }

            string partyText = pCity.PartySupport > 0
                ? string.Format(
                    LM.Get("ukiol_society_party_short"),
                    pCity.PartyName,
                    pCity.PartySupport
                )
                : LM.Get("ukiol_society_party_none");
            Text partyLabel = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "city_party",
                partyText,
                pCity.PartySupport > 0
                    ? GetEmbeddedPartyIdeologyColor(pCity.PartyIdeology)
                    : new Color(0.68f, 0.69f, 0.65f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (partyLabel != null)
            {
                RectTransform r = partyLabel.rectTransform;
                r.anchorMin = new Vector2(0f, 0.23f);
                r.anchorMax = new Vector2(1f, 0.47f);
                r.offsetMin = new Vector2(52f, 0f);
                r.offsetMax = new Vector2(-9f, 0f);
                partyLabel.resizeTextForBestFit = true;
                partyLabel.resizeTextMinSize = 7;
                partyLabel.resizeTextMaxSize = 10;
            }

            string traditionText = IsValidIdeology(pCity.TraditionIdeology)
                ? string.Format(
                    LM.Get("ukiol_society_tradition_short"),
                    GetIdeologyName(pCity.TraditionIdeology),
                    pCity.TraditionStrength
                )
                : LM.Get("ukiol_ideology_none");
            Text traditionLabel = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "city_tradition",
                traditionText,
                new Color(0.79f, 0.71f, 0.48f, 1f),
                TextAnchor.MiddleLeft,
                9
            );
            if (traditionLabel != null)
            {
                RectTransform r = traditionLabel.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 0.23f);
                r.offsetMin = new Vector2(52f, 0f);
                r.offsetMax = new Vector2(-9f, 1f);
                traditionLabel.resizeTextForBestFit = true;
                traditionLabel.resizeTextMinSize = 7;
                traditionLabel.resizeTextMaxSize = 9;
            }
        }

        private static void AddEmbeddedPoliticsTitle(
            Transform pParent,
            Text pTemplate,
            string pText,
            string pIconPath
        )
        {
            AddEmbeddedPoliticsRow(
                pParent,
                pTemplate,
                pText,
                "",
                pIconPath,
                new Color(0.91f, 0.83f, 0.42f, 1f),
                true
            );
        }

        private static void AddEmbeddedPoliticsRow(
            Transform pParent,
            Text pTemplate,
            string pLabel,
            string pValue,
            string pIconPath,
            Color pValueColor,
            bool pTitle = false
        )
        {
            if (pParent == null)
            {
                return;
            }

            GameObject rowObject = new GameObject(
                pTitle ? "ukiol_politics_title_row" : "ukiol_politics_row",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = pTitle ? 38f : 34f;
            layout.preferredHeight = pTitle ? 38f : 34f;
            layout.flexibleHeight = 0f;

            Image rowBackground = rowObject.GetComponent<Image>();
            // Alternate the stat rows very slightly, like the vanilla entity
            // lists, so long party/ideology lists remain readable without
            // turning into heavy custom cards.
            bool evenRow = (pParent.childCount % 2) == 0;
            rowBackground.color = pTitle
                ? new Color(0.175f, 0.190f, 0.160f, 0.96f)
                : (evenRow
                    ? new Color(0.220f, 0.238f, 0.202f, 0.78f)
                    : new Color(0.205f, 0.222f, 0.188f, 0.78f));
            rowBackground.raycastTarget = false;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(rowObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconRect.sizeDelta = new Vector2(
                pTitle ? 24f : 21f,
                pTitle ? 24f : 21f
            );

            Image icon = iconObject.GetComponent<Image>();
            Sprite iconSprite = string.IsNullOrEmpty(pIconPath)
                ? null
                : SpriteTextureLoader.getSprite(pIconPath);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            // An Image without a sprite is rendered as a white rectangle.
            // Empty political rows should simply have no icon instead.
            icon.enabled = iconSprite != null;

            Text label = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "label",
                pLabel,
                pTitle
                    ? new Color(0.94f, 0.84f, 0.46f, 1f)
                    : new Color(0.80f, 0.81f, 0.77f, 1f),
                TextAnchor.MiddleLeft,
                pTitle ? 15 : 12
            );
            if (label != null)
            {
                RectTransform r = label.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(pTitle ? 1f : 0.52f, 1f);
                r.offsetMin = new Vector2(34f, 1f);
                r.offsetMax = new Vector2(-5f, -1f);
            }

            if (!pTitle)
            {
                Text value = CreateEmbeddedPoliticsText(
                    rowObject.transform,
                    pTemplate,
                    "value",
                    pValue,
                    pValueColor,
                    TextAnchor.MiddleRight,
                    12
                );
                if (value != null)
                {
                    RectTransform r = value.rectTransform;
                    r.anchorMin = new Vector2(0.50f, 0f);
                    r.anchorMax = new Vector2(1f, 1f);
                    r.offsetMin = new Vector2(5f, 1f);
                    r.offsetMax = new Vector2(-7f, -1f);
                }
            }
        }

        private static Text CreateEmbeddedPoliticsText(
            Transform pParent,
            Text pTemplate,
            string pName,
            string pText,
            Color pColor,
            TextAnchor pAlignment,
            int pFontSize
        )
        {
            GameObject textObject = new GameObject(
                pName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );
            textObject.transform.SetParent(pParent, false);

            Text text = textObject.GetComponent<Text>();
            if (pTemplate != null)
            {
                text.font = pTemplate.font;
                text.fontStyle = pTemplate.fontStyle;
                text.material = pTemplate.material;
            }
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>(
                    "Arial.ttf"
                );
            }
            text.text = pText ?? "";
            text.color = pColor;
            text.alignment = pAlignment;
            text.fontSize = pFontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = pFontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            return text;
        }

        private static Color ParseHtmlColor(string pColor)
        {
            Color color;
            if (
                !string.IsNullOrEmpty(pColor) &&
                ColorUtility.TryParseHtmlString(pColor, out color)
            )
            {
                return color;
            }

            return Color.white;
        }

        private static ScrollRect FindActiveKingdomStatsScroll(
            KingdomWindow pWindow
        )
        {
            if (pWindow == null)
            {
                return null;
            }

            ScrollRect[] scrolls =
                pWindow.GetComponentsInChildren<ScrollRect>(true);
            ScrollRect best = null;
            float bestArea = 0f;

            for (int i = 0; i < scrolls.Length; i++)
            {
                ScrollRect scroll = scrolls[i];
                if (
                    scroll == null ||
                    scroll.content == null ||
                    !scroll.gameObject.activeInHierarchy
                )
                {
                    continue;
                }

                RectTransform rect =
                    scroll.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                float area =
                    Mathf.Abs(rect.rect.width * rect.rect.height);

                if (area > bestArea)
                {
                    bestArea = area;
                    best = scroll;
                }
            }

            return best;
        }

        private static void ClearNativeKingdomStatsContent(
            ScrollRect pScroll
        )
        {
            if (pScroll == null || pScroll.content == null)
            {
                return;
            }

            for (int i = 0; i < pScroll.content.childCount; i++)
            {
                Transform child = pScroll.content.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private static void RenderNativeKingdomPoliticsPage(
            KingdomWindow pWindow,
            Kingdom pKingdom,
            int pPage
        )
        {
            if (pWindow == null || pKingdom == null)
            {
                return;
            }

            ScrollRect scroll = FindActiveKingdomStatsScroll(pWindow);
            if (scroll != null)
            {
                ClearNativeKingdomStatsContent(scroll);
                CreateNativePoliticsPageBar(
                    pWindow,
                    scroll.content,
                    pPage
                );
            }

            if (pPage == NativePoliticsPageParties)
            {
                RenderNativePoliticsParties(pWindow, pKingdom);
            }
            else if (pPage == NativePoliticsPageSociety)
            {
                RenderNativePoliticsSociety(pWindow, pKingdom);
            }
            else if (pPage == NativePoliticsPageLaws)
            {
                RenderNativePoliticsPlaceholder(
                    pWindow,
                    "ukiol_native_politics_laws_title",
                    "ukiol_native_politics_laws_placeholder",
                    LawsIconPath
                );
            }
            else if (pPage == NativePoliticsPageHistory)
            {
                RenderNativePoliticsPlaceholder(
                    pWindow,
                    "ukiol_native_politics_history_title",
                    "ukiol_native_politics_history_placeholder",
                    HistoryIconPath
                );
            }
            else
            {
                RenderNativePoliticsOverview(pWindow, pKingdom);
            }

            if (scroll != null && scroll.content != null)
            {
                Transform pageBar = scroll.content.Find(
                    NativePoliticsPageBarName
                );
                if (pageBar != null)
                {
                    pageBar.SetAsFirstSibling();
                }

                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static void CreateNativePoliticsPageBar(
            KingdomWindow pWindow,
            RectTransform pContent,
            int pSelectedPage
        )
        {
            if (pWindow == null || pContent == null)
            {
                return;
            }

            Transform old = pContent.Find(NativePoliticsPageBarName);
            if (old != null)
            {
                UnityEngine.Object.Destroy(old.gameObject);
            }

            GameObject barObject = new GameObject(
                NativePoliticsPageBarName,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup)
            );
            barObject.transform.SetParent(pContent, false);

            RectTransform barRect =
                barObject.GetComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(0f, 48f);

            LayoutElement layout =
                barObject.GetComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;
            layout.flexibleHeight = 0f;

            HorizontalLayoutGroup row =
                barObject.GetComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 5f;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.padding = new RectOffset(4, 4, 2, 2);

            Button donor = null;
            List<Button> sideButtons =
                FindLikelyKingdomSideTabButtons(pWindow);
            if (sideButtons.Count > 0)
            {
                donor = sideButtons[0];
            }

            string[] icons =
            {
                OverviewIconPath,
                PartiesIconPath,
                SocietyIconPath,
                LawsIconPath,
                HistoryIconPath
            };

            for (int page = 0; page < icons.Length; page++)
            {
                Button button = CreateNativePoliticsPageButton(
                    donor,
                    barObject.transform,
                    icons[page],
                    page == pSelectedPage
                );

                if (button == null)
                {
                    continue;
                }

                int capturedPage = page;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(
                    delegate
                    {
                        SelectNativeKingdomPoliticsPage(
                            pWindow,
                            capturedPage
                        );
                    }
                );
            }

            barObject.transform.SetAsFirstSibling();
            barObject.SetActive(true);
        }

        private static Button CreateNativePoliticsPageButton(
            Button pDonor,
            Transform pParent,
            string pIconPath,
            bool pSelected
        )
        {
            if (pParent == null)
            {
                return null;
            }

            GameObject gameObject;
            if (pDonor != null)
            {
                gameObject = UnityEngine.Object.Instantiate(
                    pDonor.gameObject,
                    pParent
                );
            }
            else
            {
                gameObject = new GameObject(
                    "ukiol_native_page_button",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement)
                );
            }

            gameObject.name = "ukiol_native_page_button";
            gameObject.SetActive(true);

            RectTransform rect =
                gameObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(42f, 42f);
            }

            LayoutElement layout =
                gameObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<LayoutElement>();
            }
            layout.minWidth = 42f;
            layout.preferredWidth = 42f;
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;

            Button button = gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            ApplySpriteToButton(button, pIconPath);

            if (pSelected)
            {
                Image background = gameObject.GetComponent<Image>();
                if (background != null)
                {
                    background.color = new Color(
                        1f,
                        0.55f,
                        0.35f,
                        1f
                    );
                }
            }

            return button;
        }

        private static void RenderNativePoliticsOverview(
            KingdomWindow pWindow,
            Kingdom pKingdom
        )
        {
            string course = GetKingdomCourse(pKingdom);
            string courseText = LM.Get("ukiol_state_course_none");
            if (course == ReformerTraitId)
            {
                courseText = LM.Get("ukiol_state_course_reformer");
            }
            else if (course == MilitaristTraitId)
            {
                courseText = LM.Get("ukiol_state_course_militarist");
            }
            else if (course == DiplomatTraitId)
            {
                courseText = LM.Get("ukiol_state_course_diplomat");
            }

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_native_politics_overview_title",
                (object)LM.Get("ukiol_native_politics_overview_value"),
                "#E8D36A",
                OverviewIconPath,
                "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_state_course_label",
                (object)courseText,
                "#43FF43",
                GetCourseIconPath(course),
                "iconKings"
            );

            string ideology = GetStateIdeology(pKingdom);
            int ideologySupport =
                GetKingdomIdeologySupport(pKingdom, ideology);

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_state_ideology_label",
                (object)FormatStateIdeology(
                    ideology,
                    ideologySupport
                ),
                GetIdeologySupportColor(ideologySupport),
                GetIdeologyIconPath(ideology),
                "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_state_ideology_current_label",
                (object)GetIdeologyCurrentName(
                    GetStateIdeologyCurrent(pKingdom)
                ),
                "#E8D36A",
                GetIdeologyIconPath(ideology),
                "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_ideology_tree_path_label",
                (object)GetKingdomIdeologyTreePath(pKingdom),
                "#B8D4ED",
                GetIdeologyIconPath(ideology),
                "iconKings"
            );

            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(pKingdom);
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_behavior_market_label",
                (object)FormatIdeologyBehaviorPercent(behavior.Market),
                "#F2CC59", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_behavior_welfare_label",
                (object)FormatIdeologyBehaviorPercent(behavior.Welfare),
                "#73E68C", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_behavior_centralization_label",
                (object)FormatIdeologyBehaviorPercent(behavior.Centralization),
                "#F29E59", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_behavior_pluralism_label",
                (object)FormatIdeologyBehaviorPercent(behavior.Pluralism),
                "#73D1F2", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_behavior_militarism_label",
                (object)FormatIdeologyBehaviorPercent(behavior.Militarism),
                "#F2796B", MilitaristIconPath, "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_ideology_economy_effect_label",
                (object)GetIdeologyEconomyEffectSummary(pKingdom),
                "#E8D36A", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_ideology_stability_effect_label",
                (object)FormatSignedPoliticalValue(
                    GetIdeologyBehaviorStabilityModifier(pKingdom)
                ),
                "#73E68C", OverviewIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_ideology_army_effect_label",
                (object)FormatSignedPoliticalPercent(
                    GetIdeologyArmyLimitEffectPercent(pKingdom)
                ),
                "#F2796B", MilitaristIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_ideology_war_exhaustion_effect_label",
                (object)("+" + GetIdeologyWarExhaustionPerYear(pKingdom).ToString() + "/" +
                    LM.Get("ukiol_year_short")),
                "#F2B36F", MilitaristIconPath, "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_reform_pressure_label",
                (object)FormatIdeologyBehaviorPercent(
                    GetKingdomIntData(pKingdom, IdeologyReformPressureDataKey, 0)
                ),
                "#73E6B8", ReformerIconPath, "iconKings"
            );
            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_radicalization_pressure_label",
                (object)FormatIdeologyBehaviorPercent(
                    GetKingdomIntData(pKingdom, IdeologyRadicalizationPressureDataKey, 0)
                ),
                "#F27F73", MilitaristIconPath, "iconKings"
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow, "ukiol_diplomatic_reputation_label",
                (object)FormatIdeologyBehaviorPercent(
                    GetDiplomaticReputation(pKingdom)
                ),
                "#B8D4ED", DiplomatIconPath, "iconKings"
            );

            string nativeBlocId = GetKingdomStringData(
                pKingdom,
                InternationalBlocIdDataKey,
                ""
            );
            if (!string.IsNullOrEmpty(nativeBlocId))
            {
                string nativeBlocType = GetKingdomStringData(
                    pKingdom,
                    InternationalBlocTypeDataKey,
                    "commonwealth"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_name_label",
                    (object)GetKingdomStringData(
                        pKingdom,
                        InternationalBlocNameDataKey,
                        "?"
                    ),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_type_label",
                    (object)GetInternationalBlocTypeName(nativeBlocType),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_leader_label",
                    (object)GetKingdomStringData(
                        pKingdom,
                        InternationalBlocLeaderNameDataKey,
                        "?"
                    ),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_members_label",
                    (object)GetKingdomIntData(
                        pKingdom,
                        InternationalBlocMemberCountDataKey,
                        0
                    ),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_unity_label",
                    (object)FormatIdeologyBehaviorPercent(
                        GetKingdomIntData(
                            pKingdom,
                            InternationalBlocUnityDataKey,
                            0
                        )
                    ),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_bloc_integration_label",
                    (object)FormatIdeologyBehaviorPercent(
                        GetKingdomIntData(
                            pKingdom,
                            InternationalBlocIntegrationDataKey,
                            0
                        )
                    ),
                    "#9FC7EE", DiplomatIconPath, "iconKings"
                );
                int nextSummitYear = GetKingdomIntData(
                    pKingdom, InternationalSummitNextYearDataKey, 0
                );
                if (nextSummitYear > 0)
                {
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_next_label",
                        (object)nextSummitYear,
                        "#C7B7F2", DiplomatIconPath, "iconKings"
                    );
                }
                int lastSummitYear = GetKingdomIntData(
                    pKingdom, InternationalSummitLastYearDataKey, 0
                );
                if (lastSummitYear > 0)
                {
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_last_label",
                        (object)(lastSummitYear.ToString() + " — " +
                            GetInternationalSummitResultName(
                                GetKingdomStringData(pKingdom, InternationalSummitLastResultDataKey, "none")
                            )),
                        "#B8D4ED", DiplomatIconPath, "iconKings"
                    );
                }
                if (GetKingdomIntData(pKingdom, InternationalSummitActiveDataKey, 0) != 0)
                {
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_stage_label",
                        (object)GetInternationalSummitStageName(
                            GetKingdomIntData(pKingdom, InternationalSummitStageDataKey, 1)
                        ),
                        "#C7B7F2", DiplomatIconPath, "iconKings"
                    );
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_host_label",
                        (object)(GetKingdomStringData(pKingdom, InternationalSummitHostCityDataKey, "?") +
                            " (" + GetKingdomStringData(pKingdom, InternationalSummitHostKingdomDataKey, "?") + ")"),
                        "#C7B7F2", DiplomatIconPath, "iconKings"
                    );
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_agenda_label",
                        (object)GetInternationalSummitAgendaName(
                            GetKingdomStringData(pKingdom, InternationalSummitAgendaDataKey, "common_declaration")
                        ),
                        "#C7B7F2", DiplomatIconPath, "iconKings"
                    );
                    ShowKingdomStatRowWithCustomIcon(
                        pWindow, "ukiol_summit_attendees_label",
                        (object)(GetKingdomIntData(pKingdom, InternationalSummitAttendeesDataKey, 0).ToString() +
                            "/" + GetKingdomIntData(pKingdom, InternationalBlocMemberCountDataKey, 0).ToString()),
                        "#C7B7F2", DiplomatIconPath, "iconKings"
                    );
                    if (GetKingdomIntData(pKingdom, InternationalSummitStageDataKey, 1) >= 3)
                    {
                        ShowKingdomStatRowWithCustomIcon(
                            pWindow, "ukiol_summit_votes_label",
                            (object)(GetKingdomIntData(pKingdom, InternationalSummitYesVotesDataKey, 0).ToString() +
                                " / " + GetKingdomIntData(pKingdom, InternationalSummitNoVotesDataKey, 0).ToString()),
                            "#C7B7F2", DiplomatIconPath, "iconKings"
                        );
                    }
                }
            }
            if (GetKingdomIntData(pKingdom, DiplomaticCrisisActiveDataKey, 0) != 0)
            {
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_diplomatic_crisis_label",
                    (object)GetKingdomStringData(
                        pKingdom,
                        DiplomaticCrisisCounterpartDataKey,
                        "?"
                    ),
                    "#F2AD4D", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_diplomatic_demand_label",
                    (object)GetDiplomaticDemandName(
                        GetKingdomStringData(
                            pKingdom,
                            DiplomaticCrisisDemandDataKey,
                            "political_guarantees"
                        )
                    ),
                    "#F2C766", DiplomatIconPath, "iconKings"
                );
                ShowKingdomStatRowWithCustomIcon(
                    pWindow, "ukiol_diplomatic_stage_label",
                    (object)GetDiplomaticCrisisStageName(
                        GetKingdomIntData(
                            pKingdom,
                            DiplomaticCrisisStageDataKey,
                            1
                        )
                    ),
                    "#ED9E61", DiplomatIconPath, "iconKings"
                );
            }

            int stability = GetNationalStability(pKingdom);
            int target = CalculateNationalStabilityTarget(pKingdom);
            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_national_stability_label",
                (object)FormatStabilityValue(stability, target, true),
                GetStabilityColor(stability),
                OverviewIconPath,
                "iconClock"
            );

            string movementIdeology;
            int movementSupport;
            int movementRadicalism;
            string movementLeader;
            GetLeadingPoliticalMovement(
                pKingdom,
                out movementIdeology,
                out movementSupport,
                out movementRadicalism,
                out movementLeader
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_political_movement_label",
                (object)FormatPoliticalMovement(
                    movementIdeology,
                    movementSupport,
                    movementLeader
                ),
                GetMovementColor(movementRadicalism),
                GetIdeologyIconPath(movementIdeology),
                "iconKings"
            );

            string leadingIdeology;
            string leadingName;
            string leadingLeader;
            int leadingSupport;
            int leadingRadicalism;
            GetLeadingPoliticalParty(
                pKingdom,
                out leadingIdeology,
                out leadingName,
                out leadingLeader,
                out leadingSupport,
                out leadingRadicalism
            );

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_leading_party_label",
                (object)FormatPartyWithSupport(
                    leadingName,
                    leadingSupport
                ),
                GetMovementColor(leadingRadicalism),
                PartiesIconPath,
                "iconKings"
            );

            int crisisPressure = GetPoliticalCrisisPressure(pKingdom);
            string crisisIdeology = GetPoliticalCrisisIdeology(pKingdom);
            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_political_crisis_label",
                (object)FormatPoliticalCrisis(
                    crisisIdeology,
                    crisisPressure
                ),
                GetCrisisColor(crisisPressure),
                GetIdeologyIconPath(crisisIdeology),
                "iconClock"
            );
        }

        private static void RenderNativePoliticsParties(
            KingdomWindow pWindow,
            Kingdom pKingdom
        )
        {
            List<PartyOverviewEntry> parties =
                GetPoliticalPartyOverviewEntries(pKingdom);

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_native_politics_parties_title",
                (object)parties.Count,
                "#E8D36A",
                PartiesIconPath,
                "iconKings"
            );

            if (parties.Count == 0)
            {
                ShowKingdomStatRowWithCustomIcon(
                    pWindow,
                    "ukiol_native_politics_status_label",
                    (object)LM.Get("ukiol_party_none"),
                    "#BEBEBE",
                    PartiesIconPath,
                    "iconKings"
                );
                return;
            }

            int maxRows = Math.Min(9, parties.Count);
            for (int i = 0; i < maxRows; i++)
            {
                PartyOverviewEntry party = parties[i];
                string labelKey = "ukiol_party_rank_" + (i + 1);

                ShowKingdomStatRowWithCustomIcon(
                    pWindow,
                    labelKey,
                    (object)FormatPartyWithSupport(
                        party.Name,
                        party.Support
                    ),
                    GetMovementColor(party.Radicalism),
                    GetIdeologyIconPath(party.Ideology),
                    "iconKings"
                );
            }
        }

        private static void RenderNativePoliticsSociety(
            KingdomWindow pWindow,
            Kingdom pKingdom
        )
        {
            List<SocietyCityEntry> cities =
                GetSocietyCityEntries(pKingdom);

            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                "ukiol_native_politics_society_title",
                (object)string.Format(
                    LM.Get("ukiol_society_city_count"),
                    cities.Count
                ),
                "#E8D36A",
                SocietyIconPath,
                "iconKings"
            );

            int maxRows = Math.Min(9, cities.Count);
            for (int i = 0; i < maxRows; i++)
            {
                SocietyCityEntry city = cities[i];
                string value = FormatCityPoliticalIdentity(
                    city.City,
                    city.DominantIdeology,
                    city.DominantSupport
                );

                ShowKingdomStatRowWithCustomIcon(
                    pWindow,
                    city.Name,
                    (object)value,
                    GetIdeologySupportColor(city.DominantSupport),
                    GetIdeologyIconPath(city.DominantIdeology),
                    "iconKings"
                );
            }
        }

        private static void RenderNativePoliticsPlaceholder(
            KingdomWindow pWindow,
            string pTitleKey,
            string pPlaceholderKey,
            string pIconPath
        )
        {
            ShowKingdomStatRowWithCustomIcon(
                pWindow,
                pTitleKey,
                (object)LM.Get(pPlaceholderKey),
                "#BEBEBE",
                pIconPath,
                "iconKings"
            );
        }

        private static void ShowKingdomStatRowWithCustomIcon(
            KingdomWindow pWindow,
            string pLabel,
            object pValue,
            string pColor,
            string pCustomIconPath,
            string pFallbackIconPath
        )
        {
            Dictionary<int, bool> imageStates =
                CaptureUiImageStates(pWindow);
            Dictionary<int, bool> textStates =
                CaptureUiTextStates(pWindow);

            pWindow.showStatRow(
                pLabel,
                pValue,
                pColor,
                pIconPath: pFallbackIconPath
            );

            ApplyCustomIconToLatestStatRow(
                pWindow,
                imageStates,
                pCustomIconPath,
                pFallbackIconPath
            );
            ApplyCompactTextToLatestStatRow(
                pWindow,
                textStates
            );
        }

        private static void ShowCityStatRowWithCustomIcon(
            CityWindow pWindow,
            string pLabel,
            object pValue,
            string pColor,
            string pCustomIconPath,
            string pFallbackIconPath
        )
        {
            Dictionary<int, bool> imageStates =
                CaptureUiImageStates(pWindow);

            pWindow.showStatRow(
                pLabel,
                pValue,
                pColor,
                pIconPath: pFallbackIconPath
            );

            ApplyCustomIconToLatestStatRow(
                pWindow,
                imageStates,
                pCustomIconPath,
                pFallbackIconPath
            );
        }

        private static Dictionary<int, bool> CaptureUiImageStates(
            Component pRoot
        )
        {
            Dictionary<int, bool> states =
                new Dictionary<int, bool>();

            if (pRoot == null)
            {
                return states;
            }

            try
            {
                UnityEngine.UI.Image[] images =
                    pRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);

                for (int i = 0; i < images.Length; i++)
                {
                    UnityEngine.UI.Image image = images[i];

                    if (image == null)
                    {
                        continue;
                    }

                    states[image.GetInstanceID()] =
                        image.gameObject.activeInHierarchy;
                }
            }
            catch
            {
            }

            return states;
        }

        private static Dictionary<int, bool> CaptureUiTextStates(
            Component pRoot
        )
        {
            Dictionary<int, bool> states =
                new Dictionary<int, bool>();

            if (pRoot == null)
            {
                return states;
            }

            try
            {
                Text[] texts =
                    pRoot.GetComponentsInChildren<Text>(true);

                for (int i = 0; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    states[text.GetInstanceID()] =
                        text.gameObject.activeInHierarchy;
                }
            }
            catch
            {
            }

            return states;
        }

        private static void ApplyCompactTextToLatestStatRow(
            Component pRoot,
            Dictionary<int, bool> pBeforeStates
        )
        {
            if (pRoot == null)
            {
                return;
            }

            try
            {
                Text[] texts =
                    pRoot.GetComponentsInChildren<Text>(true);
                List<Text> latestTexts = new List<Text>();
                int maxPreparedLines = 1;

                for (int i = 0; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (
                        text == null ||
                        !text.gameObject.activeInHierarchy
                    )
                    {
                        continue;
                    }

                    bool wasActive = false;
                    bool existedBefore =
                        pBeforeStates != null &&
                        pBeforeStates.TryGetValue(
                            text.GetInstanceID(),
                            out wasActive
                        );

                    if (existedBefore && wasActive)
                    {
                        continue;
                    }

                    int originalSize = text.fontSize;
                    if (originalSize <= 0)
                    {
                        continue;
                    }

                    bool leftAligned =
                        text.alignment == TextAnchor.UpperLeft ||
                        text.alignment == TextAnchor.MiddleLeft ||
                        text.alignment == TextAnchor.LowerLeft;
                    bool rightAligned =
                        text.alignment == TextAnchor.UpperRight ||
                        text.alignment == TextAnchor.MiddleRight ||
                        text.alignment == TextAnchor.LowerRight;

                    string visibleText = text.text ?? "";
                    string preparedText = PreparePoliticsStatText(
                        visibleText,
                        leftAligned,
                        rightAligned
                    );
                    text.text = preparedText;

                    int preparedLines = CountPoliticsTextLines(
                        preparedText
                    );
                    if (preparedLines > maxPreparedLines)
                    {
                        maxPreparedLines = preparedLines;
                    }

                    int textLength = visibleText.Length;
                    int longestWord = GetPoliticsLongestWordLength(
                        visibleText
                    );

                    // dev9.4: wrap explicitly at whitespace and shrink
                    // enough for long Russian words so Unity never needs
                    // to cut a word in half.
                    float sizeFactor = 0.86f;
                    if (textLength >= 34)
                    {
                        sizeFactor = 0.72f;
                    }
                    else if (textLength >= 26)
                    {
                        sizeFactor = 0.76f;
                    }
                    else if (textLength >= 19)
                    {
                        sizeFactor = 0.81f;
                    }

                    if (leftAligned && longestWord >= 14)
                    {
                        sizeFactor = Mathf.Min(sizeFactor, 0.72f);
                    }
                    else if (rightAligned && longestWord >= 16)
                    {
                        sizeFactor = Mathf.Min(sizeFactor, 0.76f);
                    }

                    int compactSize = Mathf.Max(
                        7,
                        Mathf.RoundToInt(originalSize * sizeFactor)
                    );

                    if (compactSize >= originalSize)
                    {
                        compactSize = Mathf.Max(7, originalSize - 1);
                    }

                    text.fontSize = compactSize;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = Mathf.Max(
                        7,
                        compactSize - 3
                    );
                    text.resizeTextMaxSize = compactSize;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.lineSpacing = 0.86f;

                    RectTransform rect = text.rectTransform;
                    if (rect != null)
                    {
                        if (leftAligned)
                        {
                            Vector2 anchorMax = rect.anchorMax;
                            if (anchorMax.x < 0.56f)
                            {
                                anchorMax.x = 0.56f;
                                rect.anchorMax = anchorMax;
                            }

                            Vector2 offsetMin = rect.offsetMin;
                            Vector2 offsetMax = rect.offsetMax;
                            offsetMin.x = Mathf.Max(
                                0f,
                                offsetMin.x - 7f
                            );
                            offsetMax.x = Mathf.Max(
                                offsetMax.x,
                                -1f
                            );
                            rect.offsetMin = offsetMin;
                            rect.offsetMax = offsetMax;
                        }
                        else if (rightAligned)
                        {
                            Vector2 anchorMin = rect.anchorMin;
                            if (anchorMin.x > 0.44f)
                            {
                                anchorMin.x = 0.44f;
                                rect.anchorMin = anchorMin;
                            }

                            Vector2 offsetMin = rect.offsetMin;
                            Vector2 offsetMax = rect.offsetMax;
                            offsetMin.x = Mathf.Min(
                                offsetMin.x,
                                1f
                            );
                            offsetMax.x = Mathf.Min(
                                0f,
                                offsetMax.x + 7f
                            );
                            rect.offsetMin = offsetMin;
                            rect.offsetMax = offsetMax;
                        }
                    }

                    latestTexts.Add(text);
                }

                if (latestTexts.Count > 0 && maxPreparedLines >= 3)
                {
                    ExpandPoliticsStatRowHeight(
                        pRoot,
                        latestTexts,
                        maxPreparedLines
                    );
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not apply politics adaptive-row fix: " +
                    exception.Message
                );
            }
        }

        private static string PreparePoliticsStatText(
            string pText,
            bool pLeftAligned,
            bool pRightAligned
        )
        {
            string text = (pText ?? "")
                .Replace("\r", " " )
                .Replace("\n", " " )
                .Trim();

            if (text.Length == 0)
            {
                return text;
            }

            if (
                pRightAligned &&
                text.IndexOf(" → ", StringComparison.Ordinal) >= 0
            )
            {
                text = text.Replace(" → ", " →\n");
            }

            int maxChars = pLeftAligned ? 16 : 21;
            int maxLines = pLeftAligned ? 2 : 3;

            return WrapPoliticsTextByWords(
                text,
                maxChars,
                maxLines
            );
        }

        private static string WrapPoliticsTextByWords(
            string pText,
            int pMaxChars,
            int pMaxLines
        )
        {
            if (string.IsNullOrEmpty(pText))
            {
                return "";
            }

            string[] forcedLines = pText.Split('\n');
            List<string> result = new List<string>();

            for (int f = 0; f < forcedLines.Length; f++)
            {
                string part = (forcedLines[f] ?? "").Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                string[] words = part.Split(
                    new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                );
                string line = "";

                for (int w = 0; w < words.Length; w++)
                {
                    string word = words[w];
                    string candidate = line.Length == 0
                        ? word
                        : line + " " + word;

                    bool canCreateLine =
                        result.Count < Math.Max(1, pMaxLines - 1);

                    if (
                        line.Length > 0 &&
                        candidate.Length > pMaxChars &&
                        canCreateLine
                    )
                    {
                        result.Add(line);
                        line = word;
                    }
                    else
                    {
                        line = candidate;
                    }
                }

                if (line.Length > 0)
                {
                    if (result.Count < pMaxLines)
                    {
                        result.Add(line);
                    }
                    else
                    {
                        result[result.Count - 1] =
                            result[result.Count - 1] + " " + line;
                    }
                }
            }

            while (result.Count > pMaxLines)
            {
                int last = result.Count - 1;
                result[last - 1] =
                    result[last - 1] + " " + result[last];
                result.RemoveAt(last);
            }

            return string.Join("\n", result.ToArray());
        }

        private static int CountPoliticsTextLines(string pText)
        {
            if (string.IsNullOrEmpty(pText))
            {
                return 1;
            }

            int lines = 1;
            for (int i = 0; i < pText.Length; i++)
            {
                if (pText[i] == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }

        private static int GetPoliticsLongestWordLength(string pText)
        {
            if (string.IsNullOrEmpty(pText))
            {
                return 0;
            }

            string normalized = pText
                .Replace("\r", " " )
                .Replace("\n", " " )
                .Replace("→", " " )
                .Replace("—", " " );
            string[] words = normalized.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

            int longest = 0;
            for (int i = 0; i < words.Length; i++)
            {
                int length = words[i].Trim().Length;
                if (length > longest)
                {
                    longest = length;
                }
            }

            return longest;
        }

        private static void ExpandPoliticsStatRowHeight(
            Component pRoot,
            List<Text> pTexts,
            int pLineCount
        )
        {
            if (
                pRoot == null ||
                pTexts == null ||
                pTexts.Count == 0 ||
                pLineCount < 3
            )
            {
                return;
            }

            Transform rootTransform = pRoot.transform;
            Transform candidate = pTexts[0] == null
                ? null
                : pTexts[0].transform.parent;

            while (
                candidate != null &&
                candidate != rootTransform
            )
            {
                bool containsAll = true;
                for (int i = 0; i < pTexts.Count; i++)
                {
                    Text text = pTexts[i];
                    if (
                        text == null ||
                        !text.transform.IsChildOf(candidate)
                    )
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                {
                    break;
                }

                candidate = candidate.parent;
            }

            if (
                candidate == null ||
                candidate == rootTransform
            )
            {
                return;
            }

            RectTransform rowRect =
                candidate as RectTransform;
            if (rowRect == null)
            {
                rowRect = candidate.GetComponent<RectTransform>();
            }

            float currentHeight = rowRect == null
                ? 36f
                : Mathf.Max(36f, rowRect.rect.height);
            float desiredHeight = Mathf.Max(
                currentHeight,
                48f + (pLineCount - 3) * 10f
            );

            if (currentHeight > 120f)
            {
                return;
            }

            LayoutElement layout =
                candidate.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = candidate.gameObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = Mathf.Max(
                layout.minHeight,
                desiredHeight
            );
            layout.preferredHeight = Mathf.Max(
                layout.preferredHeight,
                desiredHeight
            );
            layout.flexibleHeight = 0f;
        }

        private static void ApplyCustomIconToLatestStatRow(
            Component pRoot,
            Dictionary<int, bool> pBeforeStates,
            string pCustomIconPath,
            string pFallbackIconPath
        )
        {
            if (
                pRoot == null ||
                string.IsNullOrEmpty(pCustomIconPath)
            )
            {
                return;
            }

            try
            {
                Sprite customSprite =
                    SpriteTextureLoader.getSprite(pCustomIconPath);

                if (customSprite == null)
                {
                    return;
                }

                UnityEngine.UI.Image[] images =
                    pRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);

                UnityEngine.UI.Image candidate = null;
                UnityEngine.UI.Image fallbackCandidate = null;
                float fallbackY = float.MaxValue;
                string fallbackLeaf = GetIconLeafName(pFallbackIconPath);

                for (int i = 0; i < images.Length; i++)
                {
                    UnityEngine.UI.Image image = images[i];

                    if (
                        image == null ||
                        !image.gameObject.activeInHierarchy
                    )
                    {
                        continue;
                    }

                    bool wasActive = false;
                    bool existedBefore =
                        pBeforeStates != null &&
                        pBeforeStates.TryGetValue(
                            image.GetInstanceID(),
                            out wasActive
                        );

                    bool isNewOrActivated =
                        !existedBefore || !wasActive;

                    bool fallbackMatches =
                        ImageUsesIcon(
                            image,
                            fallbackLeaf
                        );

                    if (
                        isNewOrActivated &&
                        fallbackMatches
                    )
                    {
                        candidate = image;
                        break;
                    }

                    if (
                        isNewOrActivated &&
                        candidate == null &&
                        IsLikelySmallUiIcon(image)
                    )
                    {
                        candidate = image;
                    }

                    if (fallbackMatches)
                    {
                        float y = image.transform.position.y;

                        if (y < fallbackY)
                        {
                            fallbackY = y;
                            fallbackCandidate = image;
                        }
                    }
                }

                if (candidate == null)
                {
                    candidate = fallbackCandidate;
                }

                if (candidate == null)
                {
                    return;
                }

                candidate.sprite = customSprite;
                candidate.color = Color.white;
                candidate.preserveAspect = true;
                candidate.enabled = true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not apply custom politics stat icon: " +
                    exception.Message
                );
            }
        }

        private static bool ImageUsesIcon(
            UnityEngine.UI.Image pImage,
            string pIconLeaf
        )
        {
            if (
                pImage == null ||
                pImage.sprite == null ||
                string.IsNullOrEmpty(pIconLeaf)
            )
            {
                return false;
            }

            string spriteName = pImage.sprite.name;

            if (string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            return spriteName.IndexOf(
                pIconLeaf,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

        private static bool IsLikelySmallUiIcon(
            UnityEngine.UI.Image pImage
        )
        {
            if (pImage == null)
            {
                return false;
            }

            RectTransform rect =
                pImage.rectTransform;

            if (rect == null)
            {
                return false;
            }

            float width = Mathf.Abs(rect.rect.width);
            float height = Mathf.Abs(rect.rect.height);

            return
                width > 0f &&
                height > 0f &&
                width <= 48f &&
                height <= 48f;
        }

        private static string GetIconLeafName(
            string pIconPath
        )
        {
            if (string.IsNullOrEmpty(pIconPath))
            {
                return string.Empty;
            }

            int slash = pIconPath.LastIndexOf('/');

            if (
                slash >= 0 &&
                slash + 1 < pIconPath.Length
            )
            {
                return pIconPath.Substring(slash + 1);
            }

            return pIconPath;
        }

        private static string GetCourseIconPath(
            string pCourse
        )
        {
            if (pCourse == ReformerTraitId)
            {
                return ReformerIconPath;
            }

            if (pCourse == MilitaristTraitId)
            {
                return MilitaristIconPath;
            }

            if (pCourse == DiplomatTraitId)
            {
                return DiplomatIconPath;
            }

            return ReformerIconPath;
        }

    }
}
