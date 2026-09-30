using System;
using NeoModLoader.General;
using NeoModLoader.General.UI.Window;
using strings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private sealed class PoliticalChronicleWindow
            : SingleAutoLayoutWindow<PoliticalChronicleWindow>
        {
            private const int MaxVisibleEntries = 120;
            private const string FilterAll = "all";
            private const string FilterPolitics = "politics";
            private const string FilterParties = "parties";
            private const string FilterIdeology = "ideology";
            private const string FilterCrises = "crises";
            private const string FilterWar = "war";
            private const string FilterInternational = "international";

            private static string _filter = FilterAll;
            private static bool _majorOnly;
            private static long _selectedEntryId;

            protected override void Init()
            {
            }

            public override void OnNormalEnable()
            {
                base.OnNormalEnable();
                _selectedEntryId = 0;
                RefreshContent();
                ResetScrollToTop();
            }

            public static void RefreshAfterPersistenceLoad()
            {
                if (Instance == null)
                {
                    return;
                }

                Instance.RefreshContent();
            }

            public static void ResetWorldSelection()
            {
                _selectedEntryId = 0;
            }

            public static void Open()
            {
                if (Instance == null)
                {
                    return;
                }

                _selectedEntryId = 0;
                Instance.RefreshContent();
                ScrollWindow.showWindow(WindowId);
                Instance.ResetScrollToTop();
            }

            private void RefreshContent()
            {
                if (ContentTransform == null)
                {
                    return;
                }

                ClearContent();

                if (_selectedEntryId > 0)
                {
                    PoliticalChronicleEntry selected =
                        FindChronicleEntry(_selectedEntryId);
                    if (selected != null)
                    {
                        AddChronicleDetailView(selected);
                        return;
                    }

                    _selectedEntryId = 0;
                }

                AddFilterBar();

                int matching = CountMatchingEntries();
                AddSummaryCard(matching);

                int shown = 0;
                for (
                    int i = PoliticalChronicleEntries.Count - 1;
                    i >= 0 && shown < MaxVisibleEntries;
                    i--
                )
                {
                    PoliticalChronicleEntry entry =
                        PoliticalChronicleEntries[i];
                    if (!MatchesFilter(entry))
                    {
                        continue;
                    }

                    AddChronicleCard(entry);
                    shown++;
                }

                if (shown == 0)
                {
                    AddMessageCard(
                        LM.Get("ukiol_chronicle_empty")
                    );
                }
            }

            private int CountMatchingEntries()
            {
                int count = 0;
                for (int i = 0; i < PoliticalChronicleEntries.Count; i++)
                {
                    if (MatchesFilter(PoliticalChronicleEntries[i]))
                    {
                        count++;
                    }
                }
                return count;
            }

            private bool MatchesFilter(PoliticalChronicleEntry entry)
            {
                if (entry == null)
                {
                    return false;
                }

                if (
                    _majorOnly &&
                    entry.Importance < PoliticalChronicleImportance.Major
                )
                {
                    return false;
                }

                if (_filter == FilterAll)
                {
                    return true;
                }

                return entry.Category == _filter;
            }

            private void AddFilterBar()
            {
                GameObject row = CreateCard(
                    "ChronicleFilters",
                    94f,
                    new Color(0.07f, 0.08f, 0.09f, 0.96f)
                );

                // Row 1: general + domestic categories.
                AddEmbeddedButton(
                    row.transform,
                    "All",
                    LM.Get("ukiol_chronicle_filter_all"),
                    4f,
                    3f,
                    45f,
                    25f,
                    FilterButtonColor(FilterAll),
                    delegate { SetFilter(FilterAll); }
                );
                AddEmbeddedButton(
                    row.transform,
                    "Politics",
                    LM.Get("ukiol_chronicle_filter_politics"),
                    52f,
                    3f,
                    45f,
                    25f,
                    FilterButtonColor(FilterPolitics),
                    delegate { SetFilter(FilterPolitics); }
                );
                AddEmbeddedButton(
                    row.transform,
                    "Parties",
                    LM.Get("ukiol_chronicle_filter_parties"),
                    100f,
                    3f,
                    45f,
                    25f,
                    FilterButtonColor(FilterParties),
                    delegate { SetFilter(FilterParties); }
                );
                AddEmbeddedButton(
                    row.transform,
                    "Ideology",
                    LM.Get("ukiol_chronicle_filter_ideology"),
                    148f,
                    3f,
                    48f,
                    25f,
                    FilterButtonColor(FilterIdeology),
                    delegate { SetFilter(FilterIdeology); }
                );

                // Row 2: conflicts and foreign affairs.
                AddEmbeddedButton(
                    row.transform,
                    "Crises",
                    LM.Get("ukiol_chronicle_filter_crises"),
                    4f,
                    33f,
                    58f,
                    25f,
                    FilterButtonColor(FilterCrises),
                    delegate { SetFilter(FilterCrises); }
                );
                AddEmbeddedButton(
                    row.transform,
                    "War",
                    LM.Get("ukiol_chronicle_filter_war"),
                    65f,
                    33f,
                    45f,
                    25f,
                    FilterButtonColor(FilterWar),
                    delegate { SetFilter(FilterWar); }
                );
                AddEmbeddedButton(
                    row.transform,
                    "International",
                    LM.Get("ukiol_chronicle_filter_international"),
                    113f,
                    33f,
                    83f,
                    25f,
                    FilterButtonColor(FilterInternational),
                    delegate { SetFilter(FilterInternational); }
                );

                // Row 3: display options.
                AddEmbeddedButton(
                    row.transform,
                    "MajorOnly",
                    _majorOnly
                        ? LM.Get("ukiol_chronicle_major_on")
                        : LM.Get("ukiol_chronicle_major_off"),
                    4f,
                    63f,
                    94f,
                    25f,
                    _majorOnly
                        ? new Color(0.28f, 0.34f, 0.48f, 0.98f)
                        : new Color(0.14f, 0.16f, 0.18f, 0.96f),
                    delegate
                    {
                        _majorOnly = !_majorOnly;
                        RefreshContent();
                        ResetScrollToTop();
                    }
                );

                AddEmbeddedButton(
                    row.transform,
                    "Refresh",
                    LM.Get("ukiol_chronicle_refresh"),
                    102f,
                    63f,
                    94f,
                    25f,
                    new Color(0.14f, 0.16f, 0.18f, 0.96f),
                    delegate
                    {
                        RefreshContent();
                        ResetScrollToTop();
                    }
                );
            }

            private void SetFilter(string filter)
            {
                _filter = filter ?? FilterAll;
                RefreshContent();
                ResetScrollToTop();
            }

            private Color FilterButtonColor(string filter)
            {
                return _filter == filter
                    ? new Color(0.25f, 0.29f, 0.40f, 0.98f)
                    : new Color(0.14f, 0.16f, 0.18f, 0.96f);
            }

            private void AddSummaryCard(int matching)
            {
                GameObject card = CreateCard(
                    "ChronicleSummary",
                    34f,
                    new Color(0.11f, 0.12f, 0.13f, 0.94f)
                );

                string text = string.Format(
                    LM.Get("ukiol_chronicle_stored_filtered"),
                    matching,
                    PoliticalChronicleEntries.Count
                );
                AddCardText(
                    card.transform,
                    "Summary",
                    text,
                    10,
                    FontStyle.Normal,
                    new Color(0.73f, 0.77f, 0.79f, 1f),
                    8f,
                    4f,
                    8f,
                    26f,
                    TextAnchor.MiddleLeft,
                    true
                );
            }

            private void AddChronicleCard(PoliticalChronicleEntry entry)
            {
                Color panel = new Color(0.11f, 0.12f, 0.13f, 0.95f);
                Color accent = GetImportanceColor(entry.Importance);

                GameObject card = CreateCard(
                    "ChronicleEntry_" + entry.Id,
                    96f,
                    panel
                );
                MakeChronicleCardClickable(card, entry.Id);

                GameObject stripe = new GameObject(
                    "ImportanceStripe",
                    typeof(RectTransform),
                    typeof(Image)
                );
                stripe.transform.SetParent(card.transform, false);
                RectTransform stripeRect = stripe.GetComponent<RectTransform>();
                stripeRect.anchorMin = new Vector2(0f, 0f);
                stripeRect.anchorMax = new Vector2(0f, 1f);
                stripeRect.pivot = new Vector2(0f, 0.5f);
                stripeRect.anchoredPosition = Vector2.zero;
                stripeRect.sizeDelta = new Vector2(4f, 0f);
                Image stripeImage = stripe.GetComponent<Image>();
                stripeImage.color = accent;
                stripeImage.raycastTarget = false;

                AddChronicleIcon(card.transform, entry);

                string meta = string.Format(
                    LM.Get("ukiol_chronicle_entry_meta"),
                    entry.Year,
                    GetCategoryLabel(entry.Category),
                    GetImportanceLabel(entry.Importance)
                );

                AddCardText(
                    card.transform,
                    "Meta",
                    meta,
                    8,
                    FontStyle.Normal,
                    accent,
                    43f,
                    4f,
                    8f,
                    15f,
                    TextAnchor.MiddleLeft,
                    true
                );

                string title = BuildChronicleEntryTitle(entry);
                AddCardText(
                    card.transform,
                    "Title",
                    title,
                    10,
                    FontStyle.Bold,
                    Color.white,
                    43f,
                    19f,
                    8f,
                    20f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "Text",
                    entry.Text,
                    9,
                    FontStyle.Normal,
                    new Color(0.90f, 0.92f, 0.93f, 1f),
                    10f,
                    43f,
                    8f,
                    34f,
                    TextAnchor.UpperLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "OpenDetails",
                    LM.Get("ukiol_chronicle_open_details"),
                    8,
                    FontStyle.Bold,
                    accent,
                    10f,
                    78f,
                    8f,
                    13f,
                    TextAnchor.MiddleRight,
                    true
                );
            }

            private string BuildChronicleEntryTitle(
                PoliticalChronicleEntry entry
            )
            {
                string title = GetChronicleEventTitle(entry);
                string subject = GetChronicleSubject(entry);
                if (!string.IsNullOrEmpty(subject))
                {
                    title = string.Format(
                        LM.Get("ukiol_chronicle_title_with_subject"),
                        title,
                        subject
                    );
                }

                return title;
            }

            private void MakeChronicleCardClickable(
                GameObject card,
                long entryId
            )
            {
                if (card == null)
                {
                    return;
                }

                Button button = card.AddComponent<Button>();
                button.targetGraphic = card.GetComponent<Image>();
                button.navigation = new Navigation
                {
                    mode = Navigation.Mode.None
                };

                long capturedId = entryId;
                button.onClick.AddListener(
                    delegate
                    {
                        _selectedEntryId = capturedId;
                        RefreshContent();
                        ResetScrollToTop();
                    }
                );
            }

            private PoliticalChronicleEntry FindChronicleEntry(long entryId)
            {
                for (int i = PoliticalChronicleEntries.Count - 1; i >= 0; i--)
                {
                    PoliticalChronicleEntry entry = PoliticalChronicleEntries[i];
                    if (entry != null && entry.Id == entryId)
                    {
                        return entry;
                    }
                }

                return null;
            }

            private void AddChronicleDetailView(
                PoliticalChronicleEntry entry
            )
            {
                AddChronicleDetailNavigation();
                AddChronicleDetailHeader(entry);
                AddChronicleDescriptionCard(entry);
                AddChronicleParticipantsCard(entry);
                AddChronicleDetailListCard(
                    "DetailCauses",
                    LM.Get("ukiol_chronicle_causes"),
                    entry.Causes,
                    LM.Get("ukiol_chronicle_detail_no_causes"),
                    new Color(0.95f, 0.76f, 0.42f, 1f)
                );
                AddChronicleDetailListCard(
                    "DetailConsequences",
                    LM.Get("ukiol_chronicle_consequences"),
                    entry.Consequences,
                    LM.Get("ukiol_chronicle_detail_no_consequences"),
                    new Color(0.58f, 0.82f, 0.63f, 1f)
                );
            }

            private void AddChronicleDetailNavigation()
            {
                GameObject card = CreateCard(
                    "ChronicleDetailNavigation",
                    36f,
                    new Color(0.07f, 0.08f, 0.09f, 0.96f)
                );

                AddEmbeddedButton(
                    card.transform,
                    "Back",
                    LM.Get("ukiol_chronicle_detail_back"),
                    4f,
                    5f,
                    58f,
                    26f,
                    new Color(0.14f, 0.16f, 0.18f, 0.96f),
                    delegate
                    {
                        _selectedEntryId = 0;
                        RefreshContent();
                        ResetScrollToTop();
                    }
                );

                AddCardText(
                    card.transform,
                    "DetailTitle",
                    LM.Get("ukiol_chronicle_detail_title"),
                    10,
                    FontStyle.Bold,
                    Color.white,
                    68f,
                    5f,
                    8f,
                    26f,
                    TextAnchor.MiddleLeft,
                    true
                );
            }

            private void AddChronicleDetailHeader(
                PoliticalChronicleEntry entry
            )
            {
                Color accent = GetImportanceColor(entry.Importance);
                GameObject card = CreateCard(
                    "ChronicleDetailHeader",
                    74f,
                    new Color(0.11f, 0.12f, 0.13f, 0.95f)
                );

                GameObject stripe = new GameObject(
                    "ImportanceStripe",
                    typeof(RectTransform),
                    typeof(Image)
                );
                stripe.transform.SetParent(card.transform, false);
                RectTransform stripeRect = stripe.GetComponent<RectTransform>();
                stripeRect.anchorMin = new Vector2(0f, 0f);
                stripeRect.anchorMax = new Vector2(0f, 1f);
                stripeRect.pivot = new Vector2(0f, 0.5f);
                stripeRect.anchoredPosition = Vector2.zero;
                stripeRect.sizeDelta = new Vector2(4f, 0f);
                Image stripeImage = stripe.GetComponent<Image>();
                stripeImage.color = accent;
                stripeImage.raycastTarget = false;

                AddChronicleIcon(card.transform, entry);

                string meta = string.Format(
                    LM.Get("ukiol_chronicle_entry_meta"),
                    entry.Year,
                    GetCategoryLabel(entry.Category),
                    GetImportanceLabel(entry.Importance)
                );
                AddCardText(
                    card.transform,
                    "Meta",
                    meta,
                    8,
                    FontStyle.Normal,
                    accent,
                    43f,
                    7f,
                    8f,
                    15f,
                    TextAnchor.MiddleLeft,
                    true
                );
                AddCardText(
                    card.transform,
                    "Title",
                    BuildChronicleEntryTitle(entry),
                    11,
                    FontStyle.Bold,
                    Color.white,
                    43f,
                    23f,
                    8f,
                    38f,
                    TextAnchor.UpperLeft,
                    true
                );
            }

            private void AddChronicleDescriptionCard(
                PoliticalChronicleEntry entry
            )
            {
                GameObject card = CreateCard(
                    "ChronicleDetailDescription",
                    82f,
                    new Color(0.11f, 0.12f, 0.13f, 0.95f)
                );
                AddCardText(
                    card.transform,
                    "Header",
                    LM.Get("ukiol_chronicle_detail_what_happened"),
                    9,
                    FontStyle.Bold,
                    new Color(0.72f, 0.82f, 0.95f, 1f),
                    9f,
                    6f,
                    8f,
                    18f,
                    TextAnchor.MiddleLeft,
                    true
                );
                AddCardText(
                    card.transform,
                    "Body",
                    entry == null ? "" : entry.Text,
                    9,
                    FontStyle.Normal,
                    new Color(0.90f, 0.92f, 0.93f, 1f),
                    10f,
                    27f,
                    8f,
                    48f,
                    TextAnchor.UpperLeft,
                    true
                );
            }

            private void AddChronicleParticipantsCard(
                PoliticalChronicleEntry entry
            )
            {
                if (entry == null)
                {
                    return;
                }

                System.Collections.Generic.List<string> participants =
                    new System.Collections.Generic.List<string>();

                if (!string.IsNullOrEmpty(entry.KingdomName))
                {
                    participants.Add(
                        LM.Get("ukiol_chronicle_detail_kingdom") + ": " +
                        entry.KingdomName
                    );
                }
                if (!string.IsNullOrEmpty(entry.CityName))
                {
                    participants.Add(
                        LM.Get("ukiol_chronicle_detail_city") + ": " +
                        entry.CityName
                    );
                }
                if (!string.IsNullOrEmpty(entry.ActorName))
                {
                    participants.Add(
                        LM.Get("ukiol_chronicle_detail_actor") + ": " +
                        entry.ActorName
                    );
                }

                if (participants.Count == 0)
                {
                    return;
                }

                float height = 30f + participants.Count * 15f;
                GameObject card = CreateCard(
                    "ChronicleDetailParticipants",
                    height,
                    new Color(0.11f, 0.12f, 0.13f, 0.95f)
                );
                AddCardText(
                    card.transform,
                    "Header",
                    LM.Get("ukiol_chronicle_detail_participants"),
                    9,
                    FontStyle.Bold,
                    new Color(0.72f, 0.82f, 0.95f, 1f),
                    9f,
                    5f,
                    8f,
                    17f,
                    TextAnchor.MiddleLeft,
                    true
                );
                AddCardText(
                    card.transform,
                    "Body",
                    BuildChronicleDetailText(participants),
                    8,
                    FontStyle.Normal,
                    new Color(0.84f, 0.87f, 0.89f, 1f),
                    11f,
                    23f,
                    8f,
                    Math.Max(15f, participants.Count * 15f),
                    TextAnchor.UpperLeft,
                    true
                );
            }

            private void AddChronicleDetailListCard(
                string name,
                string header,
                System.Collections.Generic.List<string> details,
                string emptyText,
                Color headerColor
            )
            {
                int count = details == null ? 0 : details.Count;
                float bodyHeight = count == 0
                    ? 22f
                    : Math.Max(20f, count * 16f);
                float height = 31f + bodyHeight;

                GameObject card = CreateCard(
                    name,
                    height,
                    new Color(0.11f, 0.12f, 0.13f, 0.95f)
                );
                AddCardText(
                    card.transform,
                    "Header",
                    header,
                    9,
                    FontStyle.Bold,
                    headerColor,
                    9f,
                    5f,
                    8f,
                    18f,
                    TextAnchor.MiddleLeft,
                    true
                );

                string body = count == 0
                    ? emptyText
                    : BuildChronicleDetailText(details);
                AddCardText(
                    card.transform,
                    "Body",
                    body,
                    8,
                    count == 0 ? FontStyle.Italic : FontStyle.Normal,
                    new Color(0.84f, 0.87f, 0.89f, 1f),
                    11f,
                    25f,
                    8f,
                    bodyHeight,
                    TextAnchor.UpperLeft,
                    true
                );
            }

            private float GetChronicleDetailSectionHeight(
                System.Collections.Generic.List<string> details
            )
            {
                if (details == null || details.Count == 0)
                {
                    return 0f;
                }

                return 17f + details.Count * 13f;
            }

            private float AddChronicleDetailSection(
                Transform parent,
                string name,
                string header,
                System.Collections.Generic.List<string> details,
                float top,
                Color headerColor
            )
            {
                if (details == null || details.Count == 0)
                {
                    return top;
                }

                AddCardText(
                    parent,
                    name + "Header",
                    header,
                    8,
                    FontStyle.Bold,
                    headerColor,
                    10f,
                    top,
                    8f,
                    14f,
                    TextAnchor.MiddleLeft,
                    true
                );

                string body = BuildChronicleDetailText(details);
                float bodyHeight = Math.Max(13f, details.Count * 13f);
                AddCardText(
                    parent,
                    name + "Body",
                    body,
                    8,
                    FontStyle.Normal,
                    new Color(0.82f, 0.85f, 0.87f, 1f),
                    12f,
                    top + 14f,
                    8f,
                    bodyHeight,
                    TextAnchor.UpperLeft,
                    true
                );

                return top + 17f + bodyHeight;
            }

            private string BuildChronicleDetailText(
                System.Collections.Generic.List<string> details
            )
            {
                if (details == null || details.Count == 0)
                {
                    return "";
                }

                System.Text.StringBuilder builder =
                    new System.Text.StringBuilder();

                for (int i = 0; i < details.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("\n");
                    }

                    builder.Append("• ");
                    builder.Append(details[i]);
                }

                return builder.ToString();
            }

            private void AddChronicleIcon(
                Transform parent,
                PoliticalChronicleEntry entry
            )
            {
                if (entry == null || string.IsNullOrEmpty(entry.IconPath))
                {
                    return;
                }

                Sprite sprite = SpriteTextureLoader.getSprite(entry.IconPath);
                if (sprite == null)
                {
                    return;
                }

                GameObject obj = new GameObject(
                    "EventIcon",
                    typeof(RectTransform),
                    typeof(Image)
                );
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(10f, -7f);
                rect.sizeDelta = new Vector2(27f, 27f);

                Image image = obj.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
            }

            private string GetChronicleSubject(PoliticalChronicleEntry entry)
            {
                if (entry == null)
                {
                    return "";
                }

                if (!string.IsNullOrEmpty(entry.KingdomName))
                {
                    return entry.KingdomName;
                }
                if (!string.IsNullOrEmpty(entry.CityName))
                {
                    return entry.CityName;
                }
                if (!string.IsNullOrEmpty(entry.ActorName))
                {
                    return entry.ActorName;
                }

                return "";
            }

            private string GetChronicleEventTitle(
                PoliticalChronicleEntry entry
            )
            {
                string key = entry == null ? "" : (entry.EventKey ?? "");

                if (key.StartsWith("election_"))
                    return LM.Get("ukiol_chronicle_title_election");
                if (
                    key.StartsWith("party_") ||
                    key.StartsWith("movement_") ||
                    key.StartsWith("ideological_party_split_") ||
                    key.StartsWith("party_congress")
                )
                    return LM.Get("ukiol_chronicle_title_party");
                if (key.StartsWith("revolution_"))
                    return LM.Get("ukiol_chronicle_title_revolution");
                if (key.StartsWith("coup_"))
                    return LM.Get("ukiol_chronicle_title_coup");
                if (key.StartsWith("peaceful_transition_"))
                    return LM.Get("ukiol_chronicle_title_transition");
                if (key.StartsWith("crisis_"))
                    return LM.Get("ukiol_chronicle_title_crisis");
                if (key.StartsWith("rebellion_"))
                    return LM.Get("ukiol_chronicle_title_rebellion");
                if (key.StartsWith("diplomatic_"))
                    return LM.Get("ukiol_chronicle_title_diplomacy");
                if (key.StartsWith("war_"))
                    return LM.Get("ukiol_chronicle_title_war");
                if (key.StartsWith("peace_"))
                    return LM.Get("ukiol_chronicle_title_peace");
                if (key.StartsWith("bloc_"))
                    return LM.Get("ukiol_chronicle_title_bloc");
                if (key.StartsWith("summit_"))
                    return LM.Get("ukiol_chronicle_title_summit");
                if (
                    key.StartsWith("ideology_") ||
                    key.StartsWith("current_changed_") ||
                    key.StartsWith("state_ideology_") ||
                    key.StartsWith("city_ideology_")
                )
                    return LM.Get("ukiol_chronicle_title_ideology");
                if (key.StartsWith("government_"))
                    return LM.Get("ukiol_chronicle_title_government");
                if (
                    key.StartsWith("leadership_") ||
                    key.StartsWith("political_succession_") ||
                    key.StartsWith("general_secretary_")
                )
                    return LM.Get("ukiol_chronicle_title_leadership");

                return GetCategoryLabel(entry == null ? "" : entry.Category);
            }

            private string GetCategoryLabel(string category)
            {
                if (category == ChronicleCategoryWar)
                    return LM.Get("ukiol_chronicle_category_war");
                if (category == ChronicleCategoryInternational)
                    return LM.Get("ukiol_chronicle_category_international");
                if (category == ChronicleCategoryParties)
                    return LM.Get("ukiol_chronicle_category_parties");
                if (category == ChronicleCategoryIdeology)
                    return LM.Get("ukiol_chronicle_category_ideology");
                if (category == ChronicleCategoryCrises)
                    return LM.Get("ukiol_chronicle_category_crises");
                return LM.Get("ukiol_chronicle_category_politics");
            }

            private string GetImportanceLabel(
                PoliticalChronicleImportance importance
            )
            {
                if (importance == PoliticalChronicleImportance.Historic)
                    return LM.Get("ukiol_chronicle_importance_historic");
                if (importance == PoliticalChronicleImportance.Major)
                    return LM.Get("ukiol_chronicle_importance_major");
                if (importance == PoliticalChronicleImportance.Low)
                    return LM.Get("ukiol_chronicle_importance_low");
                return LM.Get("ukiol_chronicle_importance_normal");
            }

            private Color GetImportanceColor(
                PoliticalChronicleImportance importance
            )
            {
                if (importance == PoliticalChronicleImportance.Historic)
                    return new Color(1f, 0.70f, 0.24f, 1f);
                if (importance == PoliticalChronicleImportance.Major)
                    return new Color(0.55f, 0.78f, 1f, 1f);
                if (importance == PoliticalChronicleImportance.Low)
                    return new Color(0.58f, 0.61f, 0.63f, 1f);
                return new Color(0.78f, 0.82f, 0.84f, 1f);
            }

            private GameObject CreateCard(string name, float height, Color color)
            {
                GameObject card = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image)
                );
                card.transform.SetParent(ContentTransform, false);
                card.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(200f, height);
                LayoutElement layout = card.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f;
                layout.minWidth = 180f;
                layout.preferredHeight = height;
                layout.minHeight = height;

                Image image = card.GetComponent<Image>();
                Sprite background = SpriteTextureLoader.getSprite(
                    "ui/special/windowInnerSliced"
                );
                if (background != null)
                {
                    image.sprite = background;
                    image.type = Image.Type.Sliced;
                }
                image.color = color;
                return card;
            }

            private void AddMessageCard(string text)
            {
                GameObject card = CreateCard(
                    "ChronicleEmpty",
                    48f,
                    new Color(0.11f, 0.12f, 0.13f, 0.94f)
                );
                AddCardText(
                    card.transform,
                    "Message",
                    text,
                    10,
                    FontStyle.Italic,
                    new Color(0.73f, 0.77f, 0.79f, 1f),
                    8f,
                    5f,
                    8f,
                    38f,
                    TextAnchor.MiddleCenter,
                    true
                );
            }

            private void AddCardText(
                Transform parent,
                string name,
                string value,
                int fontSize,
                FontStyle style,
                Color color,
                float left,
                float top,
                float right,
                float height,
                TextAnchor alignment,
                bool bestFit
            )
            {
                GameObject obj = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Text)
                );
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(left, -top - height);
                rect.offsetMax = new Vector2(-right, -top);

                Text text = obj.GetComponent<Text>();
                OT.InitializeCommonText(text);
                text.text = value ?? "";
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.color = color;
                text.alignment = alignment;
                text.resizeTextForBestFit = bestFit;
                text.resizeTextMinSize = 7;
                text.resizeTextMaxSize = fontSize;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.supportRichText = false;
                text.raycastTarget = false;
            }

            private void AddEmbeddedButton(
                Transform parent,
                string name,
                string label,
                float x,
                float y,
                float width,
                float height,
                Color color,
                UnityAction action
            )
            {
                GameObject obj = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button)
                );
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -y);
                rect.sizeDelta = new Vector2(width, height);

                Image image = obj.GetComponent<Image>();
                Sprite background = SpriteTextureLoader.getSprite(
                    "ui/special/windowInnerSliced"
                );
                if (background != null)
                {
                    image.sprite = background;
                    image.type = Image.Type.Sliced;
                }
                image.color = color;

                Button button = obj.GetComponent<Button>();
                button.targetGraphic = image;
                button.navigation = new Navigation
                {
                    mode = Navigation.Mode.None
                };
                if (action != null)
                {
                    button.onClick.AddListener(action);
                }

                GameObject textObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(Text)
                );
                textObject.transform.SetParent(obj.transform, false);
                RectTransform textRect =
                    textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2f, 1f);
                textRect.offsetMax = new Vector2(-2f, -1f);

                Text text = textObject.GetComponent<Text>();
                OT.InitializeCommonText(text);
                text.text = label ?? "";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.fontSize = 9;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 6;
                text.resizeTextMaxSize = 9;
                text.raycastTarget = false;
            }

            private void ClearContent()
            {
                for (
                    int i = ContentTransform.childCount - 1;
                    i >= 0;
                    i--
                )
                {
                    Transform child = ContentTransform.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            private void ResetScrollToTop()
            {
                try
                {
                    Canvas.ForceUpdateCanvases();
                    if (
                        ScrollWindowComponent != null &&
                        ScrollWindowComponent.transform_scrollRect != null
                    )
                    {
                        ScrollRect scroll = ScrollWindowComponent
                            .transform_scrollRect
                            .GetComponent<ScrollRect>();
                        if (scroll != null)
                        {
                            scroll.verticalNormalizedPosition = 1f;
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
