using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;
using strings;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private static CityWindow[] GetCachedCityWindows()
        {
            bool needsRefresh =
                _cachedCityWindows == null ||
                _cachedCityWindows.Length == 0 ||
                Time.unscaledTime >= _nextCityWindowCacheRefreshTime;

            if (!needsRefresh)
            {
                return _cachedCityWindows;
            }

            try
            {
                CityWindow[] found = Resources.FindObjectsOfTypeAll<CityWindow>();
                _cachedCityWindows = found ?? new CityWindow[0];
            }
            catch
            {
                _cachedCityWindows = new CityWindow[0];
            }

            _nextCityWindowCacheRefreshTime =
                Time.unscaledTime + KingdomWindowCacheRefreshInterval;
            return _cachedCityWindows;
        }

        private static void RefreshActiveCityPoliticsTabs()
        {
            try
            {
                CityWindow[] windows = GetCachedCityWindows();
                CityWindow active = null;

                if (windows != null)
                {
                    for (int i = 0; i < windows.Length; i++)
                    {
                        CityWindow window = windows[i];
                        if (IsCityWindowFullyOpen(window))
                        {
                            active = window;
                            break;
                        }
                    }
                }

                if (active == null)
                {
                    HideCityPoliticsSideButton();
                    RestoreAllCityPoliticsHostScrolls();
                    _cityPoliticsWindows.Clear();
                    _cityPoliticsAddonPageIds.Clear();
                    return;
                }

                RestoreInactiveCityPoliticsHostScrolls(active.GetInstanceID());
                EnsureNativeCityPoliticsUi(active);
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not refresh settlement Politics tab: " +
                    exception.Message
                );
            }
        }

        private static bool IsCityWindowFullyOpen(CityWindow pWindow)
        {
            if (
                pWindow == null ||
                pWindow.gameObject == null ||
                !pWindow.gameObject.activeInHierarchy
            )
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
            if (
                canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
            )
            {
                camera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

            float width = Mathf.Abs(tr.x - tl.x);
            float height = Mathf.Abs(tl.y - bl.y);

            return width >= 280f && height >= Screen.height * 0.55f;
        }

        private static void EnsureNativeCityPoliticsUi(CityWindow pWindow)
        {
            if (pWindow == null || pWindow.gameObject == null)
            {
                return;
            }

            // Preferred path: use the exact same WindowMetaTab system as the
            // native Kingdom Politics page. This lets WorldBox own the tab
            // position, spacing, selected sprite and content visibility, so
            // Settlement Politics does not float on top of another page.
            if (TryEnsureTrueNativeCityPoliticsTab(pWindow))
            {
                DestroyCityPoliticsOverlayButton();
                return;
            }

            // Compatibility fallback for game builds where the native tab
            // container cannot be resolved.
            int windowId = pWindow.GetInstanceID();

            if (
                _cityPoliticsOverlayButton != null &&
                _cityPoliticsOverlayButton.gameObject != null &&
                _cityPoliticsOverlayWindowId == windowId
            )
            {
                PositionCityPoliticsSideButton(_cityPoliticsOverlayButton, pWindow);
                return;
            }

            DestroyCityPoliticsOverlayButton();
            _cityPoliticsOverlayButton = CreateCityPoliticsSideButton(pWindow);
            _cityPoliticsOverlayWindowId = _cityPoliticsOverlayButton == null
                ? -1
                : windowId;
        }

        private static bool TryEnsureTrueNativeCityPoliticsTab(CityWindow pWindow)
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

                container.init();
                int windowId = pWindow.GetInstanceID();

                if (
                    _cityPoliticsTrueNativeTab != null &&
                    _cityPoliticsTrueNativeTab.gameObject != null &&
                    _cityPoliticsTrueNativeContainer == container &&
                    _cityPoliticsTrueNativeWindowId == windowId
                )
                {
                    _cityPoliticsTrueNativeTab.gameObject.SetActive(true);
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
                        existing.gameObject.name == NativeCityPoliticsButtonName
                    )
                    {
                        ConfigureTrueNativeCityPoliticsTab(
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

                // Clone a real vanilla city tab, exactly like Kingdom
                // Politics does. No hand-tuned screen coordinates here.
                GameObject clone = UnityEngine.Object.Instantiate(
                    donor.gameObject,
                    donor.transform.parent,
                    false
                );
                createdClone = clone;
                clone.name = NativeCityPoliticsButtonName;
                clone.transform.SetSiblingIndex(donor.transform.GetSiblingIndex());

                WindowMetaTab politicsTab = clone.GetComponent<WindowMetaTab>();
                if (politicsTab == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return false;
                }

                ConfigureTrueNativeCityPoliticsTab(
                    pWindow,
                    container,
                    politicsTab
                );
                createdClone = null;

                LogInfo(
                    "Settlement Politics TRUE native WindowMetaTab created; " +
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
                    "True native Settlement Politics tab unavailable; " +
                    "falling back to overlay: " + exception.Message
                );
                return false;
            }
        }

        private static void ConfigureTrueNativeCityPoliticsTab(
            CityWindow pWindow,
            WindowMetaTabButtonsContainer pContainer,
            WindowMetaTab pTab
        )
        {
            if (pWindow == null || pContainer == null || pTab == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();

            pTab.tab_elements.Clear();
            pTab.tab_action = new WindowMetaTabEvent();

            SetTrueNativeWindowMetaTabContainer(pTab, pContainer);
            RegisterTrueNativeWindowMetaTab(pContainer, pTab);

            TipButton tip = pTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = "ukiol_native_politics_tab_name";
                tip.textOnClickDescription = "ukiol_native_politics_tab_desc";
                tip.text_description_2 = "";
            }

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

            ReplaceTrueNativePoliticsTabIcon(pTab, PoliticsIconPath);

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

                    _cityPoliticsWindows.Add(windowId);

                    try
                    {
                        _openingNativeCityPolitics = true;
                        ShowNativeCityPoliticsPanel(pWindow);
                        SyncTrueNativeCityPoliticsTabContent(pWindow);
                        pContainer.showTab(clickedTab);
                    }
                    finally
                    {
                        _openingNativeCityPolitics = false;
                    }
                }
            );

            pTab.gameObject.SetActive(true);
            _cityPoliticsTrueNativeTab = pTab;
            _cityPoliticsTrueNativeContainer = pContainer;
            _cityPoliticsTrueNativeWindowId = windowId;
        }

        private static void SyncTrueNativeCityPoliticsTabContent(
            CityWindow pWindow
        )
        {
            if (
                pWindow == null ||
                _cityPoliticsTrueNativeTab == null ||
                _cityPoliticsTrueNativeWindowId != pWindow.GetInstanceID()
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
                    rects[i].gameObject.name == NativeCityPoliticsPanelName
                )
                {
                    panel = rects[i];
                    break;
                }
            }

            _cityPoliticsTrueNativeTab.tab_elements.Clear();
            if (panel != null)
            {
                _cityPoliticsTrueNativeTab.tab_elements.Add(panel);
            }
        }

        private static Button CreateCityPoliticsSideButton(CityWindow pWindow)
        {
            RectTransform windowRect = pWindow == null
                ? null
                : pWindow.GetComponent<RectTransform>();
            if (windowRect == null)
            {
                return null;
            }

            GameObject overlayObject = new GameObject(
                "ukiol_city_politics_overlay_canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            Canvas overlayCanvas = overlayObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 31990;

            CanvasScaler scaler = overlayObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            GameObject buttonObject = new GameObject(
                NativeCityPoliticsButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline)
            );
            buttonObject.transform.SetParent(overlayObject.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(52f, 52f);

            Image background = buttonObject.GetComponent<Image>();
            Sprite frame = SpriteTextureLoader.getSprite("ukiol/icons/tab_frame");
            if (frame != null)
            {
                background.sprite = frame;
                background.preserveAspect = true;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.37f, 0.30f, 0.16f, 1f);
            }
            background.raycastTarget = true;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.07f, 0.05f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            GameObject iconObject = new GameObject(
                "icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(buttonObject.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.13f, 0.13f);
            iconRect.anchorMax = new Vector2(0.87f, 0.87f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = SpriteTextureLoader.getSprite(PoliticsIconPath);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.72f, 1f);
            colors.pressedColor = new Color(0.92f, 0.55f, 0.36f, 1f);
            colors.selectedColor = new Color(0.93f, 0.42f, 0.31f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            button.colors = colors;
            button.onClick.AddListener(delegate { OpenNativeCityPolitics(pWindow); });

            PositionCityPoliticsSideButton(button, pWindow);
            return button;
        }

        private static void PositionCityPoliticsSideButton(
            Button pButton,
            CityWindow pWindow
        )
        {
            if (pButton == null || pWindow == null)
            {
                return;
            }

            RectTransform windowRect = pWindow.GetComponent<RectTransform>();
            RectTransform buttonRect = pButton.GetComponent<RectTransform>();
            if (windowRect == null || buttonRect == null)
            {
                return;
            }

            Canvas sourceCanvas = pWindow.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (
                sourceCanvas != null &&
                sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            )
            {
                camera = sourceCanvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            windowRect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

            float left = Mathf.Min(bl.x, tl.x);
            float bottom = bl.y;
            float top = Mathf.Max(tl.y, tr.y);
            float height = Mathf.Max(1f, top - bottom);
            float size = Mathf.Clamp(height * 0.062f, 48f, 54f);

            buttonRect.sizeDelta = new Vector2(size, size);
            buttonRect.anchoredPosition = new Vector2(
                left - size * 0.78f,
                bottom + height * 0.20f
            );
            pButton.gameObject.SetActive(true);
        }

        private static void HideCityPoliticsSideButton()
        {
            if (
                _cityPoliticsOverlayButton != null &&
                _cityPoliticsOverlayButton.gameObject != null
            )
            {
                _cityPoliticsOverlayButton.gameObject.SetActive(false);
            }
        }

        private static void DestroyCityPoliticsOverlayButton()
        {
            if (
                _cityPoliticsOverlayButton != null &&
                _cityPoliticsOverlayButton.gameObject != null
            )
            {
                Transform canvas = _cityPoliticsOverlayButton.transform.parent;
                if (canvas != null)
                {
                    UnityEngine.Object.Destroy(canvas.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(_cityPoliticsOverlayButton.gameObject);
                }
            }

            _cityPoliticsOverlayButton = null;
            _cityPoliticsOverlayWindowId = -1;
        }

        private static void OpenNativeCityPolitics(CityWindow pWindow)
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            _cityPoliticsWindows.Add(windowId);

            try
            {
                _openingNativeCityPolitics = true;
                ShowNativeCityPoliticsPanel(pWindow);
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not open settlement Politics page: " +
                    exception.Message
                );
            }
            finally
            {
                _openingNativeCityPolitics = false;
            }
        }

        private static void SelectNativeCityAddonPoliticsPage(
            CityWindow pWindow,
            string pPageId
        )
        {
            if (pWindow == null)
            {
                return;
            }

            int windowId = pWindow.GetInstanceID();
            if (string.IsNullOrEmpty(pPageId))
            {
                _cityPoliticsAddonPageIds.Remove(windowId);
            }
            else
            {
                _cityPoliticsAddonPageIds[windowId] = pPageId;
            }
            OpenNativeCityPolitics(pWindow);
        }

        private static void HideNativeCityPoliticsPanel(CityWindow pWindow)
        {
            if (pWindow == null)
            {
                return;
            }

            RectTransform[] rects =
                pWindow.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (
                    rect != null &&
                    rect.gameObject.name == NativeCityPoliticsPanelName
                )
                {
                    rect.gameObject.SetActive(false);
                    break;
                }
            }

            RestoreCityPoliticsHostScroll(pWindow);
        }

        private static void RestoreCityPoliticsHostScroll(CityWindow pWindow)
        {
            if (pWindow == null) return;
            int windowId = pWindow.GetInstanceID();
            ScrollRect host;
            if (!_cityPoliticsHostScrolls.TryGetValue(windowId, out host) || host == null)
            {
                _cityPoliticsHostScrolls.Remove(windowId);
                _cityPoliticsHostScrollEnabled.Remove(windowId);
                return;
            }

            bool enabled = true;
            _cityPoliticsHostScrollEnabled.TryGetValue(windowId, out enabled);
            host.enabled = enabled;
            _cityPoliticsHostScrolls.Remove(windowId);
            _cityPoliticsHostScrollEnabled.Remove(windowId);
        }

        private static void RestoreAllCityPoliticsHostScrolls()
        {
            if (_cityPoliticsHostScrolls.Count == 0) return;
            List<int> ids = new List<int>(_cityPoliticsHostScrolls.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                ScrollRect host;
                if (_cityPoliticsHostScrolls.TryGetValue(ids[i], out host) && host != null)
                {
                    bool enabled = true;
                    _cityPoliticsHostScrollEnabled.TryGetValue(ids[i], out enabled);
                    host.enabled = enabled;
                }
            }
            _cityPoliticsHostScrolls.Clear();
            _cityPoliticsHostScrollEnabled.Clear();
        }

        private static void RestoreInactiveCityPoliticsHostScrolls(int pActiveWindowId)
        {
            if (_cityPoliticsHostScrolls.Count == 0) return;
            List<int> ids = new List<int>(_cityPoliticsHostScrolls.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == pActiveWindowId) continue;
                ScrollRect host;
                if (_cityPoliticsHostScrolls.TryGetValue(ids[i], out host) && host != null)
                {
                    bool enabled = true;
                    _cityPoliticsHostScrollEnabled.TryGetValue(ids[i], out enabled);
                    host.enabled = enabled;
                }
                _cityPoliticsHostScrolls.Remove(ids[i]);
                _cityPoliticsHostScrollEnabled.Remove(ids[i]);
            }
        }

        private static ScrollRect FindActiveCityStatsScroll(CityWindow pWindow)
        {
            if (pWindow == null) return null;
            ScrollRect[] scrolls = pWindow.GetComponentsInChildren<ScrollRect>(true);
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

                RectTransform rect = scroll.GetComponent<RectTransform>();
                if (rect == null) continue;
                float area = Mathf.Abs(rect.rect.width * rect.rect.height);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = scroll;
                }
            }
            return best;
        }

        private static void ShowNativeCityPoliticsPanel(CityWindow pWindow)
        {
            if (pWindow == null) return;

            City city = pWindow.meta_object;
            if (city == null)
            {
                city = GetMemberValue(pWindow, "city", "_city") as City;
            }
            if (city == null) return;

            ScrollRect hostScroll = FindActiveCityStatsScroll(pWindow);
            if (hostScroll == null)
            {
                LogWarning("Could not open settlement Politics: active CityWindow ScrollRect was not found.");
                return;
            }

            Transform host = hostScroll.viewport != null
                ? hostScroll.viewport
                : hostScroll.transform;

            Transform old = null;
            for (int i = 0; i < host.childCount; i++)
            {
                Transform child = host.GetChild(i);
                if (child != null && child.gameObject.name == NativeCityPoliticsPanelName)
                {
                    old = child;
                    break;
                }
            }
            if (old != null)
            {
                old.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(old.gameObject);
            }

            GameObject panelObject = new GameObject(
                NativeCityPoliticsPanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            panelObject.transform.SetParent(host, false);
            panelObject.transform.SetAsLastSibling();

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.235f, 0.252f, 0.215f, 1f);
            background.raycastTarget = true;

            GameObject barObject = new GameObject(
                NativeCityPoliticsPageBarName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup)
            );
            barObject.transform.SetParent(panelObject.transform, false);

            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.offsetMin = new Vector2(8f, -48f);
            barRect.offsetMax = new Vector2(-8f, -6f);

            Image barBackground = barObject.GetComponent<Image>();
            barBackground.color = new Color(0.185f, 0.200f, 0.170f, 0.78f);
            barBackground.raycastTarget = false;

            HorizontalLayoutGroup bar = barObject.GetComponent<HorizontalLayoutGroup>();
            bar.childAlignment = TextAnchor.MiddleCenter;
            bar.padding = new RectOffset(6, 6, 2, 2);
            bar.spacing = 4f;
            bar.childControlWidth = false;
            bar.childControlHeight = false;
            bar.childForceExpandWidth = false;
            bar.childForceExpandHeight = false;

            int windowId = pWindow.GetInstanceID();
            string addonPageId = "";
            _cityPoliticsAddonPageIds.TryGetValue(windowId, out addonPageId);

            Button overviewButton = CreateEmbeddedPoliticsPageButton(
                barObject.transform,
                OverviewIconPath,
                string.IsNullOrEmpty(addonPageId)
            );
            if (overviewButton != null)
            {
                overviewButton.onClick.AddListener(delegate
                {
                    SelectNativeCityAddonPoliticsPage(pWindow, "");
                });
            }

            List<PoliticalWorldAPI.PoliticsPageInfo> addonPages =
                PoliticalWorldAPI.UI.GetPoliticsPages(city);
            for (int i = 0; i < addonPages.Count; i++)
            {
                PoliticalWorldAPI.PoliticsPageInfo page = addonPages[i];
                if (page == null) continue;
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
                    pageButton.onClick.AddListener(delegate
                    {
                        SelectNativeCityAddonPoliticsPage(pWindow, capturedId);
                    });
                }
            }

            GameObject viewportObject = new GameObject(
                "ukiol_city_politics_content_viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect)
            );
            viewportObject.transform.SetParent(panelObject.transform, false);

            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -54f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject(
                "ukiol_city_politics_content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup column = contentObject.GetComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperCenter;
            column.spacing = 2f;
            column.padding = new RectOffset(2, 2, 2, 6);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect politicsScroll = viewportObject.GetComponent<ScrollRect>();
            politicsScroll.viewport = viewportRect;
            politicsScroll.content = contentRect;
            politicsScroll.horizontal = false;
            politicsScroll.vertical = true;
            politicsScroll.movementType = ScrollRect.MovementType.Clamped;
            politicsScroll.scrollSensitivity = 22f;

            if (!_cityPoliticsHostScrolls.ContainsKey(windowId))
            {
                _cityPoliticsHostScrolls[windowId] = hostScroll;
                _cityPoliticsHostScrollEnabled[windowId] = hostScroll.enabled;
            }

            Scrollbar shared = hostScroll.verticalScrollbar;
            if (shared != null)
            {
                hostScroll.enabled = false;
                politicsScroll.enabled = false;
                politicsScroll.verticalScrollbar = shared;
                politicsScroll.verticalScrollbarVisibility =
                    ScrollRect.ScrollbarVisibility.Permanent;
                politicsScroll.enabled = true;
                shared.gameObject.SetActive(true);
            }

            Text template = FindPoliticsTextTemplate(pWindow);
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
                            TargetKind = PoliticalWorldAPI.InspectorTargetKind.City,
                            CityWindow = pWindow,
                            City = city,
                            Kingdom = GetKingdomFromObject(city),
                            Content = contentObject.transform,
                            TextTemplate = template
                        }
                    );
                    break;
                }
            }

            if (!renderedAddon)
            {
                if (!string.IsNullOrEmpty(addonPageId))
                {
                    _cityPoliticsAddonPageIds.Remove(windowId);
                }
                RenderNativeCityPoliticsOverview(city, contentObject.transform, template);
            }

            panelObject.SetActive(true);
            politicsScroll.verticalNormalizedPosition = 1f;
        }

        private static void AddSettlementPoliticsSpacer(
            Transform pParent,
            float pHeight = 7f
        )
        {
            if (pParent == null) return;

            GameObject spacer = new GameObject(
                "ukiol_city_politics_spacer",
                typeof(RectTransform),
                typeof(LayoutElement)
            );
            spacer.transform.SetParent(pParent, false);

            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            layout.minHeight = pHeight;
            layout.preferredHeight = pHeight;
            layout.flexibleHeight = 0f;
        }

        private static void AddSettlementPoliticsSectionTitle(
            Transform pParent,
            Text pTemplate,
            string pText,
            string pIconPath
        )
        {
            if (pParent == null) return;

            GameObject rowObject = new GameObject(
                "ukiol_city_politics_section_title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 24f;
            layout.preferredHeight = 24f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            background.color = new Color(0.145f, 0.157f, 0.136f, 0.80f);
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
            iconRect.sizeDelta = new Vector2(15f, 15f);

            Image icon = iconObject.GetComponent<Image>();
            Sprite iconSprite = string.IsNullOrEmpty(pIconPath)
                ? null
                : SpriteTextureLoader.getSprite(pIconPath);
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text title = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "section_title",
                pText,
                new Color(0.90f, 0.82f, 0.45f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (title != null)
            {
                RectTransform r = title.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(29f, 1f);
                r.offsetMax = new Vector2(-8f, -1f);
                title.fontStyle = FontStyle.Bold;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 8;
                title.resizeTextMaxSize = 10;
            }
        }

        private static void AddSettlementPoliticsRow(
            Transform pParent,
            Text pTemplate,
            string pLabel,
            string pValue,
            string pIconPath,
            Color pValueColor
        )
        {
            if (pParent == null) return;

            GameObject rowObject = new GameObject(
                "ukiol_city_politics_row",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );
            rowObject.transform.SetParent(pParent, false);

            LayoutElement layout = rowObject.GetComponent<LayoutElement>();
            layout.minHeight = 28f;
            layout.preferredHeight = 28f;
            layout.flexibleHeight = 0f;

            Image background = rowObject.GetComponent<Image>();
            bool evenRow = (pParent.childCount % 2) == 0;
            background.color = evenRow
                ? new Color(0.220f, 0.238f, 0.202f, 0.72f)
                : new Color(0.205f, 0.222f, 0.188f, 0.72f);
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
            iconRect.anchoredPosition = new Vector2(7f, 0f);
            iconRect.sizeDelta = new Vector2(17f, 17f);

            Image icon = iconObject.GetComponent<Image>();
            Sprite iconSprite = string.IsNullOrEmpty(pIconPath)
                ? null
                : SpriteTextureLoader.getSprite(pIconPath);
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "label",
                pLabel,
                new Color(0.80f, 0.81f, 0.77f, 1f),
                TextAnchor.MiddleLeft,
                10
            );
            if (label != null)
            {
                RectTransform r = label.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(0.44f, 1f);
                r.offsetMin = new Vector2(31f, 1f);
                r.offsetMax = new Vector2(-3f, -1f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 10;
            }

            Text value = CreateEmbeddedPoliticsText(
                rowObject.transform,
                pTemplate,
                "value",
                pValue,
                pValueColor,
                TextAnchor.MiddleRight,
                10
            );
            if (value != null)
            {
                RectTransform r = value.rectTransform;
                r.anchorMin = new Vector2(0.43f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(4f, 1f);
                r.offsetMax = new Vector2(-7f, -1f);
                value.resizeTextForBestFit = true;
                value.resizeTextMinSize = 8;
                value.resizeTextMaxSize = 10;
            }
        }

        // 1.10.0-dev6: these two derived metrics are intentionally based on
        // political state we already simulate. No hidden "separatism stat" is
        // invented or persisted just to make the UI look clever.
        private static int GetCityGovernmentLoyalty(
            City pCity,
            int pLocalStability,
            int pNationalStability,
            string pStateIdeology,
            int pStateSupport,
            int pDivision,
            int pHostility
        )
        {
            if (pCity == null) return 0;

            string dominantIdeology;
            int dominantSupport;
            int ignoredTension;
            GetCityIdeologyOverview(
                pCity,
                out dominantIdeology,
                out dominantSupport,
                out ignoredTension
            );

            float loyalty =
                pLocalStability * 0.45f +
                pStateSupport * 0.30f +
                pNationalStability * 0.25f -
                pDivision * 0.10f -
                pHostility * 0.08f;

            if (
                IsValidIdeology(pStateIdeology) &&
                IsValidIdeology(dominantIdeology)
            )
            {
                loyalty += string.Equals(
                    pStateIdeology,
                    dominantIdeology,
                    StringComparison.Ordinal
                ) ? 8f : -8f;
            }

            Kingdom kingdom = GetKingdomFromObject(pCity);
            if (kingdom != null)
            {
                City capital = GetMemberValue(
                    kingdom,
                    "capital",
                    "_capital"
                ) as City;
                if (capital == pCity)
                {
                    loyalty += 10f;
                }
            }

            return ClampInt((int)Math.Round(loyalty), 0, 100);
        }

        private static int GetCitySeparatistSentiment(
            City pCity,
            int pLocalStability,
            int pNationalStability,
            string pStateIdeology,
            int pStateSupport,
            int pDivision,
            int pHostility
        )
        {
            if (pCity == null) return 0;

            Kingdom kingdom = GetKingdomFromObject(pCity);
            if (kingdom == null || GetCitiesSafe(kingdom).Count <= 1)
            {
                return 0;
            }

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;
            if (capital == pCity)
            {
                return 0;
            }

            string dominantIdeology;
            int dominantSupport;
            int ignoredTension;
            GetCityIdeologyOverview(
                pCity,
                out dominantIdeology,
                out dominantSupport,
                out ignoredTension
            );

            float sentiment =
                (100 - pLocalStability) * 0.30f +
                (100 - pStateSupport) * 0.25f +
                pDivision * 0.20f +
                pHostility * 0.15f +
                Math.Max(0, 50 - pNationalStability) * 0.20f;

            if (
                IsValidIdeology(pStateIdeology) &&
                IsValidIdeology(dominantIdeology) &&
                !string.Equals(
                    pStateIdeology,
                    dominantIdeology,
                    StringComparison.Ordinal
                )
            )
            {
                sentiment += 10f;
            }

            return ClampInt((int)Math.Round(sentiment), 0, 100);
        }

        private static string FormatCityGovernmentLoyalty(int pValue)
        {
            string key = pValue >= 80
                ? "ukiol_city_politics_loyalty_very_high"
                : pValue >= 65
                    ? "ukiol_city_politics_loyalty_high"
                    : pValue >= 45
                        ? "ukiol_city_politics_loyalty_moderate"
                        : pValue >= 25
                            ? "ukiol_city_politics_loyalty_low"
                            : "ukiol_city_politics_loyalty_very_low";
            return pValue + "% — " + LM.Get(key);
        }

        private static string GetCityGovernmentLoyaltyColor(int pValue)
        {
            if (pValue >= 70) return "#43FF43";
            if (pValue >= 50) return "#D9D64A";
            if (pValue >= 30) return "#FF9C43";
            return "#FF4F4F";
        }

        private static string FormatCitySeparatistSentiment(
            int pValue,
            int pStage
        )
        {
            string key;
            if (pStage >= SeparatistStageSecession)
            {
                key = "ukiol_city_politics_separatism_stage_secession";
            }
            else if (pStage >= SeparatistStageAutonomy)
            {
                key = "ukiol_city_politics_separatism_stage_autonomy";
            }
            else if (pStage >= SeparatistStageMovement)
            {
                key = "ukiol_city_politics_separatism_stage_movement";
            }
            else
            {
                key = pValue <= 5
                    ? "ukiol_city_politics_separatism_none"
                    : pValue <= 25
                        ? "ukiol_city_politics_separatism_low"
                        : pValue <= 50
                            ? "ukiol_city_politics_separatism_moderate"
                            : pValue <= 75
                                ? "ukiol_city_politics_separatism_high"
                                : "ukiol_city_politics_separatism_critical";
            }
            return pValue + "% — " + LM.Get(key);
        }

        private static string GetCitySeparatistSentimentColor(int pValue)
        {
            if (pValue <= 15) return "#43FF43";
            if (pValue <= 35) return "#D9D64A";
            if (pValue <= 60) return "#FF9C43";
            return "#FF4F4F";
        }

        private static void RenderNativeCityPoliticsOverview(
            City pCity,
            Transform pContent,
            Text pTemplate
        )
        {
            if (pCity == null || pContent == null) return;

            AddEmbeddedPoliticsTitle(
                pContent,
                pTemplate,
                LM.Get("ukiol_city_politics_title"),
                PoliticsIconPath
            );

            int stability = GetLocalStability(pCity);
            int target = CalculateLocalStabilityTarget(pCity);

            string dominantIdeology;
            int dominantSupport;
            int ideologicalTension;
            GetCityIdeologyOverview(
                pCity,
                out dominantIdeology,
                out dominantSupport,
                out ideologicalTension
            );

            string memoryIdeology;
            int memoryStrength;
            GetDominantCityPoliticalMemory(
                pCity,
                out memoryIdeology,
                out memoryStrength
            );

            Kingdom kingdom = GetKingdomFromObject(pCity);
            string stateIdeology = GetStateIdeology(kingdom);
            int stateSupport = IsValidIdeology(stateIdeology)
                ? GetCityIdeologySupport(pCity, stateIdeology)
                : 0;
            int hostility = GetCityIdeologicalHostility(pCity);

            // Compact local block: this is the stuff you should be able to
            // read in one glance without scrolling through a tax return.
            AddSettlementPoliticsSpacer(pContent, 4f);
            AddSettlementPoliticsSectionTitle(
                pContent,
                pTemplate,
                LM.Get("ukiol_city_politics_local_section"),
                OverviewIconPath
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_stability"),
                FormatStabilityValue(stability, target, true),
                OverviewIconPath,
                ParseHtmlColor(GetStabilityColor(stability))
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_ideology"),
                FormatCityPoliticalIdentity(pCity, dominantIdeology, dominantSupport),
                GetIdeologyIconPath(dominantIdeology),
                new Color(0.91f, 0.83f, 0.42f, 1f)
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_tradition"),
                FormatCityPoliticalTradition(memoryIdeology, memoryStrength),
                GetIdeologyIconPath(memoryIdeology),
                new Color(0.78f, 0.70f, 0.48f, 1f)
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_division"),
                FormatIdeologicalTension(ideologicalTension),
                SocietyIconPath,
                ParseHtmlColor(GetIdeologicalTensionColor(ideologicalTension))
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_hostility"),
                FormatIdeologicalHostility(hostility),
                GetIdeologyIconPath(dominantIdeology),
                ParseHtmlColor(GetIdeologicalHostilityColor(hostility))
            );

            AddSettlementPoliticsSpacer(pContent);
            AddSettlementPoliticsSectionTitle(
                pContent,
                pTemplate,
                LM.Get("ukiol_city_politics_parties_section"),
                PartiesIconPath
            );

            string partyIdeology;
            string partyName;
            int partySupport;
            int secondSupport;
            GetLeadingCityPoliticalParty(
                pCity,
                out partyIdeology,
                out partyName,
                out partySupport,
                out secondSupport
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_party"),
                FormatCityPartyCompetition(partyName, partySupport, secondSupport),
                PartiesIconPath,
                ParseHtmlColor(GetPartyCompetitionColor(partySupport, secondSupport))
            );

            PoliticalParty stronghold = GetStrongholdPartyForCity(kingdom, pCity);
            if (stronghold != null)
            {
                AddSettlementPoliticsRow(
                    pContent, pTemplate,
                    LM.Get("ukiol_city_politics_compact_stronghold"),
                    FormatPartyWithSupport(stronghold.Name, stronghold.StrongholdSupport),
                    GetIdeologyIconPath(stronghold.Ideology),
                    ParseHtmlColor(GetIdeologySupportColor(stronghold.StrongholdSupport))
                );
            }

            // Keep the national relationship in its own lower block. Local
            // politics first; only then show how much the city agrees with
            // whoever is currently running the country.
            AddSettlementPoliticsSpacer(pContent);
            AddSettlementPoliticsSectionTitle(
                pContent,
                pTemplate,
                LM.Get("ukiol_city_politics_central_section"),
                PoliticsIconPath
            );

            int national = GetNationalStability(kingdom);
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_government_support"),
                FormatCityStateIdeologySupport(stateIdeology, stateSupport),
                GetIdeologyIconPath(stateIdeology),
                ParseHtmlColor(GetIdeologySupportColor(stateSupport))
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_national_stability"),
                national + "%",
                OverviewIconPath,
                ParseHtmlColor(GetStabilityColor(national))
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_state_ideology"),
                IsValidIdeology(stateIdeology) ? GetIdeologyName(stateIdeology) : "—",
                GetIdeologyIconPath(stateIdeology),
                new Color(0.91f, 0.83f, 0.42f, 1f)
            );

            int governmentLoyalty = GetCityGovernmentLoyalty(
                pCity,
                stability,
                national,
                stateIdeology,
                stateSupport,
                ideologicalTension,
                hostility
            );
            int separatistSentiment = GetCitySeparatistSentiment(
                pCity,
                stability,
                national,
                stateIdeology,
                stateSupport,
                ideologicalTension,
                hostility
            );

            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_loyalty"),
                FormatCityGovernmentLoyalty(governmentLoyalty),
                PoliticsIconPath,
                ParseHtmlColor(GetCityGovernmentLoyaltyColor(governmentLoyalty))
            );
            int separatistStage = GetCitySeparatistStage(pCity);
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_separatism"),
                FormatCitySeparatistSentiment(separatistSentiment, separatistStage),
                SocietyIconPath,
                ParseHtmlColor(GetCitySeparatistSentimentColor(separatistSentiment))
            );
            AddSettlementPoliticsRow(
                pContent, pTemplate,
                LM.Get("ukiol_city_politics_compact_rebellion"),
                GetRebellionRiskText(pCity, stability, national),
                SocietyIconPath,
                ParseHtmlColor(GetRebellionRiskColor(pCity, stability, national))
            );

            AddSettlementPoliticsSpacer(pContent, 8f);
        }

    }
}
