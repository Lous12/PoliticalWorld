using System;
using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Window;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private sealed class AddonInspectorWindow : SingleAutoLayoutWindow<AddonInspectorWindow>
        {
            private const string PageKingdom = "kingdom";
            private const string PageCity = "city";
            private const string PageActor = "actor";
            private const string PageFramework = "framework";
            private const int ActorPickerLimit = 10;
            private const int FrameworkAddonLimit = 12;
            private const int FrameworkEventLimit = 8;
            private const int FrameworkIssueLimit = 8;

            private static WorldTile _originTile;
            private static Kingdom _selectedKingdom;
            private static City _selectedCity;
            private static Actor _selectedActor;
            private static string _page = PageCity;
            private static string _status = "";
            private static bool _statusSuccess = true;

            public static void ResetWorldSelection()
            {
                _originTile = null;
                _selectedKingdom = null;
                _selectedCity = null;
                _selectedActor = null;
                _page = PageCity;
                _status = "";
                _statusSuccess = true;
            }

            protected override void Init()
            {
            }

            public override void OnNormalEnable()
            {
                base.OnNormalEnable();
                RefreshContent();
                ResetScrollToTop();
            }

            public static void OpenForTile(WorldTile tile)
            {
                if (tile == null || tile.zone == null || tile.zone.city == null || Instance == null)
                {
                    return;
                }

                _originTile = tile;
                _selectedCity = tile.zone.city;
                _selectedKingdom = _selectedCity.kingdom;
                _selectedActor = null;
                _page = PageCity;
                _status = "";
                _statusSuccess = true;

                Instance.RefreshContent();
                ScrollWindow.showWindow(WindowId);
                Instance.ResetScrollToTop();
            }

            private void RefreshContent()
            {
                if (ContentTransform == null) return;
                ClearContent();
                AddTargetTabs();

                if (!string.IsNullOrEmpty(_status))
                {
                    AddStatusCard(_status, _statusSuccess);
                }

                if (_page == PageKingdom)
                {
                    BuildKingdomPage();
                }
                else if (_page == PageActor)
                {
                    BuildActorPage();
                }
                else if (_page == PageFramework)
                {
                    BuildFrameworkPage();
                }
                else
                {
                    BuildCityPage();
                }
            }

            private void BuildKingdomPage()
            {
                if (_selectedKingdom == null)
                {
                    AddMessageCard("No kingdom selected.");
                    return;
                }

                AddTargetHeader("Kingdom", GetWorldObjectDisplayName(_selectedKingdom));
                RenderSections(PoliticalWorldAPI.UI.GetInspectorSections(_selectedKingdom));
                RenderActions(PoliticalWorldAPI.UI.GetContextActions(_selectedKingdom));
            }

            private void BuildCityPage()
            {
                if (_selectedCity == null)
                {
                    AddMessageCard("No city selected.");
                    return;
                }

                AddTargetHeader("City", GetWorldObjectDisplayName(_selectedCity));
                RenderSections(PoliticalWorldAPI.UI.GetInspectorSections(_selectedCity));
                RenderActions(PoliticalWorldAPI.UI.GetContextActions(_selectedCity));
            }

            private void BuildActorPage()
            {
                if (_selectedCity == null)
                {
                    AddMessageCard("Select a city first.");
                    return;
                }

                List<Actor> actors = PoliticalWorldAPI.WorldQuery.GetActors(_selectedCity, ActorPickerLimit, null);
                if (actors == null || actors.Count == 0)
                {
                    AddMessageCard("No actors found in this city.");
                    return;
                }

                if (_selectedActor == null || !actors.Contains(_selectedActor))
                {
                    _selectedActor = actors[0];
                }

                AddSectionLabel("Actor");
                for (int i = 0; i < actors.Count; i++)
                {
                    Actor actor = actors[i];
                    if (actor == null) continue;
                    Actor captured = actor;
                    bool selected = object.ReferenceEquals(_selectedActor, actor);
                    AddListButton(
                        "ActorPick_" + i,
                        (selected ? "✓ " : "") + GetWorldObjectDisplayName(actor),
                        selected ? SelectedButtonColor() : NormalButtonColor(),
                        true,
                        delegate
                        {
                            _selectedActor = captured;
                            _status = "";
                            RefreshContent();
                        }
                    );
                }

                AddTargetHeader("Selected actor", GetWorldObjectDisplayName(_selectedActor));
                RenderSections(PoliticalWorldAPI.UI.GetInspectorSections(_selectedActor));
                RenderActions(PoliticalWorldAPI.UI.GetContextActions(_selectedActor));
            }

            private void BuildFrameworkPage()
            {
                PoliticalWorldAPI.FrameworkSnapshot snapshot =
                    PoliticalWorldAPI.Ecosystem.GetSnapshot();

                AddTargetHeader(
                    "Framework",
                    "PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion
                );

                if (snapshot == null)
                {
                    AddMessageCard("Framework snapshot is unavailable.");
                    return;
                }

                AddSectionLabel("Framework status");
                GameObject summary = CreateCard(
                    "FrameworkSummary",
                    132f,
                    PanelColor()
                );
                AddCardText(summary.transform, "Addons", "Registered addons", 9, FontStyle.Normal, MutedTextColor(), 9f, 7f, 95f, 19f, TextAnchor.MiddleLeft, true);
                AddCardText(summary.transform, "AddonsV", snapshot.RegisteredAddons.ToString(), 10, FontStyle.Bold, Color.white, 105f, 7f, 9f, 19f, TextAnchor.MiddleRight, true);
                AddCardText(summary.transform, "Content", "Generic content", 9, FontStyle.Normal, MutedTextColor(), 9f, 29f, 95f, 19f, TextAnchor.MiddleLeft, true);
                AddCardText(summary.transform, "ContentV", snapshot.GenericContent + " / " + snapshot.GenericContentTypes + " types", 10, FontStyle.Bold, Color.white, 105f, 29f, 9f, 19f, TextAnchor.MiddleRight, true);
                AddCardText(summary.transform, "UI", "UI registrations", 9, FontStyle.Normal, MutedTextColor(), 9f, 51f, 95f, 19f, TextAnchor.MiddleLeft, true);
                AddCardText(summary.transform, "UIV", (snapshot.InspectorSections + snapshot.ContextActions).ToString(), 10, FontStyle.Bold, Color.white, 105f, 51f, 9f, 19f, TextAnchor.MiddleRight, true);
                AddCardText(summary.transform, "Subs", "Event subscriptions", 9, FontStyle.Normal, MutedTextColor(), 9f, 73f, 95f, 19f, TextAnchor.MiddleLeft, true);
                AddCardText(summary.transform, "SubsV", snapshot.EventSubscriptions.ToString(), 10, FontStyle.Bold, Color.white, 105f, 73f, 9f, 19f, TextAnchor.MiddleRight, true);
                AddCardText(summary.transform, "Events", "Published / callbacks", 9, FontStyle.Normal, MutedTextColor(), 9f, 95f, 95f, 19f, TextAnchor.MiddleLeft, true);
                AddCardText(summary.transform, "EventsV", snapshot.EventsPublished + " / " + snapshot.EventCallbackDeliveries, 10, FontStyle.Bold, Color.white, 105f, 95f, 9f, 19f, TextAnchor.MiddleRight, true);

                AddSectionLabel("Registered addons");
                int addonCount = snapshot.Addons == null ? 0 : snapshot.Addons.Count;
                if (addonCount == 0)
                {
                    AddMessageCard("No addons are registered.");
                }
                else
                {
                    int limit = Math.Min(FrameworkAddonLimit, addonCount);
                    for (int i = 0; i < limit; i++)
                    {
                        PoliticalWorldAPI.AddonEcosystemInfo addon = snapshot.Addons[i];
                        if (addon == null) continue;
                        GameObject card = CreateCard("FrameworkAddon_" + i, 91f, PanelColor());
                        Color stateColor = addon.Compatible
                            ? new Color(0.72f, 0.94f, 0.79f)
                            : new Color(1f, 0.78f, 0.48f);
                        AddCardText(card.transform, "Name", SafeLabel(addon.Name, addon.Id), 11, FontStyle.Bold, Color.white, 9f, 5f, 36f, 22f, TextAnchor.MiddleLeft, true);
                        AddCardText(card.transform, "State", addon.Compatible ? "OK" : "CHECK", 8, FontStyle.Bold, stateColor, 160f, 5f, 7f, 22f, TextAnchor.MiddleRight, true);
                        AddCardText(card.transform, "Id", SafeLabel(addon.Id, "addon"), 8, FontStyle.Normal, MutedTextColor(), 9f, 28f, 9f, 18f, TextAnchor.MiddleLeft, true);
                        AddCardText(card.transform, "Stats", "caps " + addon.ProvidedCapabilities.Length + " • content " + addon.GenericContent + " • UI " + (addon.InspectorSections + addon.ContextActions), 8, FontStyle.Normal, MutedTextColor(), 9f, 48f, 9f, 18f, TextAnchor.MiddleLeft, true);
                        AddCardText(card.transform, "Compat", SafeLabel(addon.CompatibilityMessage, "Compatible"), 8, FontStyle.Normal, stateColor, 9f, 67f, 9f, 18f, TextAnchor.MiddleLeft, true);
                    }
                    if (addonCount > limit)
                    {
                        AddMessageCard("Showing " + limit + " of " + addonCount + " addons.");
                    }
                }

                AddSectionLabel("Event metrics");
                int shownEvents = 0;
                if (snapshot.Events != null)
                {
                    for (int i = 0; i < snapshot.Events.Count && shownEvents < FrameworkEventLimit; i++)
                    {
                        PoliticalWorldAPI.EventMetric metric = snapshot.Events[i];
                        if (metric == null) continue;
                        if (metric.Published <= 0 && metric.CallbackDeliveries <= 0 && metric.Subscribers <= 0) continue;
                        GameObject card = CreateCard("FrameworkEvent_" + shownEvents, 55f, PanelColor());
                        AddCardText(card.transform, "Id", metric.EventId, 8, FontStyle.Bold, Color.white, 9f, 5f, 9f, 19f, TextAnchor.MiddleLeft, true);
                        AddCardText(card.transform, "Stats", "published " + metric.Published + " • callbacks " + metric.CallbackDeliveries + " • subs " + metric.Subscribers, 8, FontStyle.Normal, MutedTextColor(), 9f, 27f, 9f, 19f, TextAnchor.MiddleLeft, true);
                        shownEvents++;
                    }
                }
                if (shownEvents == 0)
                {
                    AddMessageCard("No event activity recorded yet.");
                }

                AddSectionLabel("Framework issues");
                int shownIssues = 0;
                if (snapshot.Issues != null)
                {
                    int start = Math.Max(0, snapshot.Issues.Count - FrameworkIssueLimit);
                    for (int i = start; i < snapshot.Issues.Count; i++)
                    {
                        PoliticalWorldAPI.FrameworkIssue issue = snapshot.Issues[i];
                        if (issue == null) continue;
                        GameObject card = CreateCard("FrameworkIssue_" + shownIssues, 62f, PanelColor());
                        Color issueColor = string.Equals(issue.Level, "ERROR", StringComparison.Ordinal)
                            ? new Color(1f, 0.65f, 0.62f)
                            : new Color(1f, 0.82f, 0.52f);
                        AddCardText(card.transform, "Code", SafeLabel(issue.Code, issue.Level), 8, FontStyle.Bold, issueColor, 9f, 5f, 68f, 18f, TextAnchor.MiddleLeft, true);
                        AddCardText(card.transform, "Owner", SafeLabel(issue.AddonId, "framework"), 7, FontStyle.Normal, MutedTextColor(), 80f, 5f, 9f, 18f, TextAnchor.MiddleRight, true);
                        AddCardText(card.transform, "Message", SafeLabel(issue.Message, "Framework warning"), 8, FontStyle.Normal, Color.white, 9f, 25f, 9f, 29f, TextAnchor.MiddleLeft, true);
                        shownIssues++;
                    }
                }
                if (shownIssues == 0)
                {
                    AddMessageCard("No framework compatibility issues recorded.");
                }
            }

            private void RenderSections(List<PoliticalWorldAPI.InspectorSectionInfo> sections)
            {
                AddSectionLabel("Addon sections");
                if (sections == null || sections.Count == 0)
                {
                    AddMessageCard("No addon inspector sections are registered for this target.");
                    return;
                }

                for (int i = 0; i < sections.Count; i++)
                {
                    PoliticalWorldAPI.InspectorSectionInfo section = sections[i];
                    if (section == null) continue;

                    int fieldCount = section.Fields == null ? 0 : section.Fields.Count;
                    float height = 45f + Math.Min(fieldCount, 8) * 22f;
                    if (!string.IsNullOrEmpty(section.Description)) height += 24f;
                    GameObject card = CreateCard("AddonSection_" + i, height, PanelColor());

                    AddCardText(card.transform, "Title", SafeLabel(section.DisplayName, section.Id), 12, FontStyle.Bold, AccentGold(), 9f, 5f, 46f, 24f, TextAnchor.MiddleLeft, true);
                    AddCardText(card.transform, "Source", SafeLabel(section.Source, "addon"), 8, FontStyle.Normal, MutedTextColor(), 130f, 5f, 7f, 24f, TextAnchor.MiddleRight, true);

                    float top = 30f;
                    if (!string.IsNullOrEmpty(section.Description))
                    {
                        AddCardText(card.transform, "Description", section.Description, 9, FontStyle.Normal, MutedTextColor(), 9f, top, 9f, 22f, TextAnchor.MiddleLeft, true);
                        top += 24f;
                    }

                    if (section.Fields != null)
                    {
                        for (int j = 0; j < section.Fields.Count && j < 8; j++)
                        {
                            PoliticalWorldAPI.InspectorFieldInfo field = section.Fields[j];
                            if (field == null) continue;
                            AddCardText(card.transform, "FieldLabel_" + j, SafeLabel(field.DisplayName, field.Id), 9, FontStyle.Normal, MutedTextColor(), 9f, top, 92f, 20f, TextAnchor.MiddleLeft, true);
                            AddCardText(card.transform, "FieldValue_" + j, field.Value ?? "", 10, FontStyle.Bold, Color.white, 102f, top, 9f, 20f, TextAnchor.MiddleRight, true);
                            top += 22f;
                        }
                    }
                }
            }

            private void RenderActions(List<PoliticalWorldAPI.ContextActionInfo> actions)
            {
                AddSectionLabel("Addon actions");
                if (actions == null || actions.Count == 0)
                {
                    AddMessageCard("No addon actions are registered for this target.");
                    return;
                }

                for (int i = 0; i < actions.Count; i++)
                {
                    PoliticalWorldAPI.ContextActionInfo action = actions[i];
                    if (action == null) continue;
                    string actionId = action.Id;
                    AddListButton(
                        "AddonAction_" + i,
                        SafeLabel(action.DisplayName, action.Id),
                        action.Enabled ? SecondaryButtonColor() : DisabledButtonColor(),
                        action.Enabled,
                        delegate
                        {
                            ExecuteAction(actionId);
                        }
                    );
                }
            }

            private void ExecuteAction(string actionId)
            {
                PoliticalWorldAPI.OperationResult result;
                if (_page == PageKingdom)
                {
                    result = PoliticalWorldAPI.UI.ExecuteContextAction(actionId, _selectedKingdom);
                }
                else if (_page == PageActor)
                {
                    result = PoliticalWorldAPI.UI.ExecuteContextAction(actionId, _selectedActor);
                }
                else
                {
                    result = PoliticalWorldAPI.UI.ExecuteContextAction(actionId, _selectedCity);
                }

                _statusSuccess = result != null && result.Success;
                _status = result == null ? "Action failed." : SafeLabel(result.Message, result.Code);
                RefreshContent();
            }

            private void AddTargetTabs()
            {
                GameObject row = CreateCard("AddonInspectorTabs", 32f, new Color(0.07f, 0.08f, 0.09f, 0.95f));
                AddTabButton(row.transform, "Kingdom", PageKingdom, 3f, 46f);
                AddTabButton(row.transform, "City", PageCity, 52f, 46f);
                AddTabButton(row.transform, "Actor", PageActor, 101f, 46f);
                AddTabButton(row.transform, "Framework", PageFramework, 150f, 47f);
            }

            private void AddTabButton(Transform parent, string label, string page, float x, float width)
            {
                bool selected = string.Equals(_page, page, StringComparison.Ordinal);
                string captured = page;
                AddEmbeddedButton(parent, label + "Tab", label, x, 3f, width, 26f, selected ? SelectedButtonColor() : SecondaryButtonColor(), true, delegate
                {
                    _page = captured;
                    _status = "";
                    RefreshContent();
                    ResetScrollToTop();
                });
            }

            private void AddTargetHeader(string type, string name)
            {
                GameObject card = CreateCard("AddonTargetHeader", 55f, HeaderColor());
                AddCardText(card.transform, "Type", type, 9, FontStyle.Bold, MutedTextColor(), 9f, 5f, 9f, 18f, TextAnchor.MiddleLeft, true);
                AddCardText(card.transform, "Name", name, 14, FontStyle.Bold, Color.white, 9f, 23f, 9f, 25f, TextAnchor.MiddleLeft, true);
            }

            private void AddSectionLabel(string text)
            {
                GameObject obj = new GameObject("SectionLabel", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                obj.transform.SetParent(ContentTransform, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, 25f);
                LayoutElement layout = obj.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f; layout.minWidth = 180f; layout.preferredHeight = 25f; layout.minHeight = 25f;
                Text label = obj.GetComponent<Text>();
                OT.InitializeCommonText(label);
                label.text = text ?? ""; label.alignment = TextAnchor.MiddleLeft; label.color = AccentGold(); label.fontSize = 13; label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 9; label.resizeTextMaxSize = 13;
            }

            private void AddListButton(string name, string label, Color color, bool enabled, UnityAction action)
            {
                GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(ContentTransform, false);
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);
                LayoutElement layout = row.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f; layout.minWidth = 180f; layout.preferredHeight = 30f; layout.minHeight = 30f;
                Image image = row.GetComponent<Image>(); ApplyNativeBackground(image, color);
                Button button = row.GetComponent<Button>(); ConfigureButton(button, image, enabled, action);
                GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(row.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(8f, 1f); textRect.offsetMax = new Vector2(-8f, -1f);
                Text text = textObject.GetComponent<Text>(); OT.InitializeCommonText(text);
                text.text = label ?? ""; text.alignment = TextAnchor.MiddleLeft; text.color = enabled ? Color.white : MutedTextColor(); text.fontSize = 11;
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 8; text.resizeTextMaxSize = 11; text.raycastTarget = false;
            }

            private GameObject CreateCard(string name, float height, Color color)
            {
                GameObject card = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
                card.transform.SetParent(ContentTransform, false);
                card.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, height);
                LayoutElement layout = card.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f; layout.minWidth = 180f; layout.preferredHeight = height; layout.minHeight = height;
                Image image = card.GetComponent<Image>(); ApplyNativeBackground(image, color);
                return card;
            }

            private void AddMessageCard(string text)
            {
                GameObject card = CreateCard("AddonInspectorMessage", 44f, PanelColor());
                AddCardText(card.transform, "Message", text, 9, FontStyle.Italic, MutedTextColor(), 9f, 5f, 9f, 34f, TextAnchor.MiddleCenter, true);
            }

            private void AddStatusCard(string text, bool success)
            {
                Color panel = success ? new Color(0.08f, 0.18f, 0.13f, 0.94f) : new Color(0.22f, 0.08f, 0.08f, 0.94f);
                Color foreground = success ? new Color(0.72f, 0.94f, 0.79f) : new Color(1f, 0.72f, 0.70f);
                GameObject card = CreateCard("AddonInspectorStatus", 38f, panel);
                AddCardText(card.transform, "Status", text, 9, FontStyle.Bold, foreground, 9f, 4f, 9f, 30f, TextAnchor.MiddleCenter, true);
            }

            private void AddCardText(Transform parent, string name, string value, int fontSize, FontStyle style, Color color, float left, float top, float right, float height, TextAnchor alignment, bool bestFit)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
                Text text = obj.GetComponent<Text>(); OT.InitializeCommonText(text);
                text.text = value ?? ""; text.fontSize = fontSize; text.fontStyle = style; text.color = color; text.alignment = alignment;
                text.resizeTextForBestFit = bestFit; text.resizeTextMinSize = 7; text.resizeTextMaxSize = fontSize;
                text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.supportRichText = false; text.raycastTarget = false;
            }

            private void AddEmbeddedButton(Transform parent, string name, string label, float x, float y, float width, float height, Color color, bool enabled, UnityAction action)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
                Image image = obj.GetComponent<Image>(); ApplyNativeBackground(image, enabled ? color : DisabledButtonColor());
                Button button = obj.GetComponent<Button>(); ConfigureButton(button, image, enabled, action);
                GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(obj.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(3f, 1f); textRect.offsetMax = new Vector2(-3f, -1f);
                Text text = textObject.GetComponent<Text>(); OT.InitializeCommonText(text); text.text = label ?? ""; text.alignment = TextAnchor.MiddleCenter; text.color = enabled ? Color.white : MutedTextColor(); text.fontSize = 10;
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 7; text.resizeTextMaxSize = 10; text.raycastTarget = false;
            }

            private void ConfigureButton(Button button, Image image, bool enabled, UnityAction action)
            {
                button.targetGraphic = image; button.interactable = enabled; button.navigation = new Navigation { mode = Navigation.Mode.None };
                ColorBlock colors = button.colors; colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f); colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.70f); button.colors = colors;
                if (enabled && action != null) button.onClick.AddListener(action);
            }

            private void ApplyNativeBackground(Image image, Color color)
            {
                Sprite background = SpriteTextureLoader.getSprite("ui/special/windowInnerSliced");
                if (background != null) { image.sprite = background; image.type = Image.Type.Sliced; }
                image.color = color;
            }

            private void ClearContent()
            {
                for (int i = ContentTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = ContentTransform.GetChild(i);
                    if (child != null) Destroy(child.gameObject);
                }
            }

            private void ResetScrollToTop()
            {
                try
                {
                    Canvas.ForceUpdateCanvases();
                    if (ScrollWindowComponent != null && ScrollWindowComponent.transform_scrollRect != null)
                    {
                        ScrollRect scroll = ScrollWindowComponent.transform_scrollRect.GetComponent<ScrollRect>();
                        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
                    }
                }
                catch { }
            }

            private static string SafeLabel(string preferred, string fallback)
            {
                if (!string.IsNullOrEmpty(preferred)) return preferred;
                if (!string.IsNullOrEmpty(fallback)) return fallback;
                return "—";
            }

            private static Color HeaderColor() { return new Color(0.14f, 0.16f, 0.17f, 0.96f); }
            private static Color PanelColor() { return new Color(0.11f, 0.12f, 0.13f, 0.94f); }
            private static Color NormalButtonColor() { return new Color(0.17f, 0.20f, 0.22f, 0.96f); }
            private static Color SecondaryButtonColor() { return new Color(0.14f, 0.16f, 0.18f, 0.96f); }
            private static Color SelectedButtonColor() { return new Color(0.25f, 0.29f, 0.40f, 0.98f); }
            private static Color DisabledButtonColor() { return new Color(0.12f, 0.12f, 0.12f, 0.70f); }
            private static Color MutedTextColor() { return new Color(0.73f, 0.77f, 0.79f, 1f); }
            private static Color AccentGold() { return new Color(0.45f, 0.72f, 1f, 1f); }
        }
    }
}
