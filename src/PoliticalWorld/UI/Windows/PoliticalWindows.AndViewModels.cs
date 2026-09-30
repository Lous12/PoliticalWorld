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
        private sealed class SocietyCityEntry
        {
            public City City;
            public string Name;
            public int Population;
            public bool IsCapital;
            public int Stability;
            public string DominantIdeology;
            public int DominantSupport;
            public int IdeologicalTension;
            public string TraditionIdeology;
            public int TraditionStrength;
            public string PartyIdeology;
            public string PartyName;
            public int PartySupport;
            public int PartySecondSupport;
        }

        private static List<SocietyCityEntry> GetSocietyCityEntries(
            Kingdom kingdom
        )
        {
            List<SocietyCityEntry> result =
                new List<SocietyCityEntry>();

            if (kingdom == null)
            {
                return result;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            List<PoliticalParty> parties =
                GetPoliticalPartiesReadOnly(kingdom);
            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;
            string stateIdeology = GetStateIdeology(kingdom);

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (city == null)
                {
                    continue;
                }

                string dominantIdeology;
                int dominantSupport;
                int tension;
                GetCityIdeologyOverview(
                    city,
                    out dominantIdeology,
                    out dominantSupport,
                    out tension
                );

                string traditionIdeology;
                int traditionStrength;
                GetDominantCityPoliticalMemory(
                    city,
                    out traditionIdeology,
                    out traditionStrength
                );

                string partyIdeology;
                string partyName;
                int partySupport;
                int partySecondSupport;
                GetLeadingCityPoliticalPartyFromList(
                    kingdom,
                    city,
                    parties,
                    out partyIdeology,
                    out partyName,
                    out partySupport,
                    out partySecondSupport
                );

                result.Add(
                    new SocietyCityEntry()
                    {
                        City = city,
                        Name = GetWorldObjectDisplayName(city),
                        Population = GetCityPopulationSafe(city),
                        IsCapital = capital == city,
                        Stability = GetLocalStability(city),
                        DominantIdeology = dominantIdeology,
                        DominantSupport = dominantSupport,
                        IdeologicalTension = tension,
                        TraditionIdeology = traditionIdeology,
                        TraditionStrength = traditionStrength,
                        PartyIdeology = partyIdeology,
                        PartyName = partyName,
                        PartySupport = partySupport,
                        PartySecondSupport = partySecondSupport
                    }
                );
            }

            result.Sort(
                delegate(
                    SocietyCityEntry a,
                    SocietyCityEntry b
                )
                {
                    if (a.IsCapital != b.IsCapital)
                    {
                        return a.IsCapital ? -1 : 1;
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

        private sealed class CouncilDelegateMember
        {
            public string Identity;
            public string Name;
            public string CityName;
            public Actor Actor;
        }

        private sealed class CouncilDelegateCandidate
        {
            public Actor Actor;
            public string Identity;
            public string Name;
            public string CityName;
            public float Score;
        }

        private sealed class PartyLeadershipMember
        {
            public string Identity;
            public string Name;
            public string Current;
            public Actor Actor;
        }

        private sealed class PartyLeadershipCandidate
        {
            public Actor Actor;
            public string Identity;
            public string Name;
            public string Current;
            public float Score;
        }

        private sealed class LeadershipCandidate
        {
            public Actor Actor;
            public string Identity;
            public string Name;
            public string Title;
        }

        private sealed class LeadershipHistoryEntry
        {
            public int Year;
            public string Role;
            public string Name;
            public string Title;
        }

        private sealed class PoliticalParty
        {
            public int Slot;
            public bool Active;
            public string Id;
            public string Ideology;
            public int NameVariant;
            public int NameStyle;
            public string Name;
            public string LeaderIdentity;
            public string LeaderName;
            public Actor LeaderActor;
            public string FounderIdentity;
            public string FounderName;
            public int FoundedYear;
            public int Radicalism;
            public string Position;
            public string Strategy;
            public string ForeignStance;
            public int SupportBias;
            public int Support;
            public int ColorSeed;
            public List<string> Traits;
            public string OriginCityId;
            public string OriginCityName;
            public string ParentPartyId;
            public string ParentPartyName;
            public List<PartyHistoryEntry> History;
            public List<PartySupportHistoryEntry> SupportHistory;
            public string StrongholdCityId;
            public string StrongholdCityName;
            public int StrongholdSupport;
        }

        private sealed class ElectionHistoryEntry
        {
            public int Year;
            public string WinnerPartyId;
            public string WinnerPartyName;
            public string WinnerIdeology;
            public int WinnerSupport;
        }

        private sealed class PartyOverviewEntry
        {
            public string Id;
            public string Ideology;
            public string Name;
            public string Leader;
            public string Founder;
            public int FoundedYear;
            public int Support;
            public int Radicalism;
            public int ColorSeed;
            public string Position;
            public string Strategy;
            public string ForeignStance;
            public List<string> Traits;
            public string TraitsText;
            public string OriginCityId;
            public string OriginCityName;
            public string ParentPartyId;
            public string ParentPartyName;
            public List<PartyHistoryEntry> History;
            public List<PartySupportHistoryEntry> SupportHistory;
            public string StrongholdCityId;
            public string StrongholdCityName;
            public int StrongholdSupport;
        }


        private sealed class PartyHistoryEntry
        {
            public int Year;
            public string Type;
            public string Value;
            public string Extra;
        }

        private sealed class PartySupportHistoryEntry
        {
            public int Year;
            public int Support;
        }

        private sealed class PartyRegionSupportEntry
        {
            public string CityId;
            public string Name;
            public int Support;
            public int Population;
            public bool IsOrigin;
        }

        private static List<PartyOverviewEntry>
            GetPoliticalPartyOverviewEntries(
                Kingdom kingdom
            )
        {
            List<PartyOverviewEntry> result =
                new List<PartyOverviewEntry>();
            List<PoliticalParty> parties =
                GetPoliticalPartiesReadOnly(kingdom);

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                result.Add(
                    new PartyOverviewEntry()
                    {
                        Id = party.Id,
                        Ideology = party.Ideology,
                        Name = party.Name,
                        Leader = party.LeaderName,
                        Founder = party.FounderName,
                        FoundedYear = party.FoundedYear,
                        Support = party.Support,
                        Radicalism = party.Radicalism,
                        ColorSeed = party.ColorSeed,
                        Position = party.Position,
                        Strategy = party.Strategy,
                        ForeignStance = party.ForeignStance,
                        Traits = party.Traits == null
                            ? new List<string>()
                            : new List<string>(party.Traits),
                        TraitsText = FormatPartyTraits(
                            party.Traits
                        ),
                        OriginCityId = party.OriginCityId,
                        OriginCityName = party.OriginCityName,
                        ParentPartyId = party.ParentPartyId,
                        ParentPartyName = party.ParentPartyName,
                        History = ClonePartyHistory(party.History),
                        SupportHistory = ClonePartySupportHistory(
                            party.SupportHistory
                        ),
                        StrongholdCityId = party.StrongholdCityId,
                        StrongholdCityName = party.StrongholdCityName,
                        StrongholdSupport = party.StrongholdSupport
                    }
                );
            }

            result.Sort(
                delegate(
                    PartyOverviewEntry a,
                    PartyOverviewEntry b
                )
                {
                    int supportCompare =
                        b.Support.CompareTo(a.Support);

                    if (supportCompare != 0)
                    {
                        return supportCompare;
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

        private sealed class PoliticalOverviewWindow
            : SingleAutoLayoutWindow<PoliticalOverviewWindow>
        {
            private static Kingdom _selectedKingdom;
            private static Font _font;

            public static void ResetWorldSelection()
            {
                _selectedKingdom = null;
            }

            protected override void Init()
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>(
                        "Arial.ttf"
                    );
                }
            }

            public override void OnNormalEnable()
            {
                base.OnNormalEnable();
                RefreshContent();
                ResetScrollToTop();
            }

            public static void OpenForKingdom(Kingdom kingdom)
            {
                if (kingdom == null || Instance == null)
                {
                    return;
                }

                _selectedKingdom = kingdom;
                Instance.RefreshContent();
                ScrollWindow.showWindow(WindowId);
                Instance.ResetScrollToTop();
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
                    // Cosmetic only. Never break the window because of scroll state.
                }
            }

            private void RefreshContent()
            {
                if (ContentTransform == null)
                {
                    return;
                }

                for (int i = ContentTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = ContentTransform.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }

                Kingdom kingdom = _selectedKingdom;
                if (kingdom == null)
                {
                    AddMessageCard(
                        LM.Get("ukiol_political_overview_no_kingdom")
                    );
                    return;
                }

                string course = GetKingdomCourse(kingdom);
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

                string ideology = GetStateIdeology(kingdom);
                string current = GetStateIdeologyCurrent(kingdom);

                AddKingdomHeader(
                    GetWorldObjectDisplayName(kingdom),
                    GetIdeologyName(ideology),
                    GetNationalStability(kingdom)
                );

                AddSectionLabel(
                    LM.Get("ukiol_political_overview_state_header")
                );

                AddStateCard(
                    courseText,
                    GetIdeologyName(ideology),
                    GetIdeologyCurrentName(current),
                    GetNationalStability(kingdom)
                );

                AddSectionLabel(
                    LM.Get("ukiol_political_overview_parties_header")
                );

                List<PartyOverviewEntry> parties =
                    GetPoliticalPartyOverviewEntries(kingdom);

                if (parties.Count == 0)
                {
                    AddMessageCard(LM.Get("ukiol_party_none"));
                    ResetScrollToTop();
                    return;
                }

                for (int i = 0; i < parties.Count; i++)
                {
                    PartyOverviewEntry party = parties[i];
                    AddPartyCard(
                        party,
                        FormatPartyStoredProfile(
                            party.Position,
                            party.Strategy,
                            party.ForeignStance
                        )
                    );
                }

                ResetScrollToTop();
            }

            private void AddKingdomHeader(
                string kingdomName,
                string ideologyName,
                int stability
            )
            {
                GameObject card = CreateCard(
                    "PoliticalOverviewHeader",
                    76f,
                    new Color(0.15f, 0.17f, 0.15f, 0.92f)
                );

                AddCardText(
                    card.transform,
                    "KingdomName",
                    kingdomName,
                    16,
                    FontStyle.Bold,
                    new Color(1f, 0.83f, 0.34f),
                    14f,
                    8f,
                    90f,
                    30f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "KingdomIdeology",
                    ideologyName,
                    11,
                    FontStyle.Normal,
                    new Color(0.82f, 0.86f, 0.82f),
                    14f,
                    38f,
                    90f,
                    24f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "StabilityValue",
                    stability + "%",
                    22,
                    FontStyle.Bold,
                    GetStabilityColor(stability),
                    340f,
                    15f,
                    12f,
                    38f,
                    TextAnchor.MiddleRight,
                    false
                );
            }

            private void AddStateCard(
                string course,
                string ideology,
                string current,
                int stability
            )
            {
                GameObject card = CreateCard(
                    "PoliticalOverviewStateCard",
                    126f,
                    new Color(0.12f, 0.13f, 0.12f, 0.88f)
                );

                AddStateRow(
                    card.transform,
                    8f,
                    LM.Get("ukiol_state_course_label"),
                    course,
                    Color.white
                );
                AddStateRow(
                    card.transform,
                    37f,
                    LM.Get("ukiol_state_ideology_label"),
                    ideology,
                    Color.white
                );
                AddStateRow(
                    card.transform,
                    66f,
                    LM.Get("ukiol_state_ideology_current_label"),
                    current,
                    Color.white
                );
                AddStateRow(
                    card.transform,
                    95f,
                    LM.Get("ukiol_national_stability_label"),
                    stability + "%",
                    GetStabilityColor(stability)
                );
            }

            private void AddStateRow(
                Transform parent,
                float top,
                string label,
                string value,
                Color valueColor
            )
            {
                AddCardText(
                    parent,
                    "StateLabel",
                    label,
                    12,
                    FontStyle.Normal,
                    new Color(0.72f, 0.74f, 0.72f),
                    14f,
                    top,
                    205f,
                    25f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    parent,
                    "StateValue",
                    value,
                    13,
                    FontStyle.Bold,
                    valueColor,
                    215f,
                    top,
                    14f,
                    25f,
                    TextAnchor.MiddleRight,
                    true
                );
            }

            private void AddPartyCard(
                PartyOverviewEntry party,
                string profile
            )
            {
                Color ideologyColor = GetPartyIdentityColor(
                    party.Ideology,
                    party.ColorSeed
                );

                GameObject card = CreateCard(
                    "PoliticalPartyCard",
                    96f,
                    new Color(0.11f, 0.12f, 0.11f, 0.92f)
                );

                GameObject stripe = new GameObject(
                    "IdeologyStripe",
                    typeof(RectTransform),
                    typeof(Image)
                );
                stripe.transform.SetParent(card.transform, false);
                RectTransform stripeRect =
                    stripe.GetComponent<RectTransform>();
                stripeRect.anchorMin = new Vector2(0f, 0f);
                stripeRect.anchorMax = new Vector2(0f, 1f);
                stripeRect.pivot = new Vector2(0f, 0.5f);
                stripeRect.anchoredPosition = Vector2.zero;
                stripeRect.sizeDelta = new Vector2(5f, 0f);
                stripe.GetComponent<Image>().color = ideologyColor;

                GameObject iconObject = new GameObject(
                    "PartyIcon",
                    typeof(RectTransform),
                    typeof(Image)
                );
                iconObject.transform.SetParent(card.transform, false);
                RectTransform iconRect =
                    iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(14f, -14f);
                iconRect.sizeDelta = new Vector2(34f, 34f);

                Image icon = iconObject.GetComponent<Image>();
                try
                {
                    icon.sprite = SpriteTextureLoader.getSprite(
                        GetIdeologyIconPath(party.Ideology)
                    );
                    icon.preserveAspect = true;
                    icon.color = Color.white;
                }
                catch
                {
                    icon.color = ideologyColor;
                }

                AddCardText(
                    card.transform,
                    "PartyName",
                    party.Name,
                    13,
                    FontStyle.Bold,
                    Color.white,
                    58f,
                    8f,
                    82f,
                    28f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "PartySupport",
                    party.Support + "%",
                    17,
                    FontStyle.Bold,
                    ideologyColor,
                    350f,
                    8f,
                    12f,
                    28f,
                    TextAnchor.MiddleRight,
                    false
                );

                string leader = string.IsNullOrEmpty(party.Leader)
                    ? LM.Get("ukiol_party_leader_unknown")
                    : party.Leader;

                AddCardText(
                    card.transform,
                    "PartyLeader",
                    LM.Get("ukiol_party_leader_label") + ": " + leader,
                    11,
                    FontStyle.Normal,
                    new Color(0.88f, 0.78f, 0.52f),
                    58f,
                    37f,
                    12f,
                    20f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddCardText(
                    card.transform,
                    "PartyProfile",
                    GetIdeologyName(party.Ideology) + "  ·  " + profile,
                    11,
                    FontStyle.Normal,
                    new Color(0.73f, 0.82f, 0.95f),
                    58f,
                    57f,
                    12f,
                    20f,
                    TextAnchor.MiddleLeft,
                    true
                );

                AddSupportBar(
                    card.transform,
                    party.Support,
                    ideologyColor
                );
            }

            private void AddSupportBar(
                Transform parent,
                int support,
                Color color
            )
            {
                GameObject background = new GameObject(
                    "SupportBarBackground",
                    typeof(RectTransform),
                    typeof(Image)
                );
                background.transform.SetParent(parent, false);
                RectTransform bgRect =
                    background.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0f, 1f);
                bgRect.anchorMax = new Vector2(1f, 1f);
                bgRect.pivot = new Vector2(0.5f, 1f);
                bgRect.offsetMin = new Vector2(58f, -87f);
                bgRect.offsetMax = new Vector2(-12f, -82f);
                background.GetComponent<Image>().color =
                    new Color(0.25f, 0.26f, 0.25f, 0.8f);

                GameObject fill = new GameObject(
                    "SupportBarFill",
                    typeof(RectTransform),
                    typeof(Image)
                );
                fill.transform.SetParent(background.transform, false);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                float ratio = Mathf.Clamp01(support / 100f);
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(ratio, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fill.GetComponent<Image>().color = color;
            }

            private void AddSectionLabel(string text)
            {
                GameObject row = new GameObject(
                    "PoliticalOverviewSectionLabel",
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Text)
                );
                row.transform.SetParent(ContentTransform, false);

                LayoutElement layout = row.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f;
                layout.minWidth = 180f;
                layout.preferredHeight = 28f;
                layout.minHeight = 28f;

                Text label = row.GetComponent<Text>();
                label.font = _font;
                label.fontSize = 14;
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(1f, 0.72f, 0.30f);
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 14;
                label.text = text ?? "";
            }

            private void AddMessageCard(string text)
            {
                GameObject card = CreateCard(
                    "PoliticalOverviewMessage",
                    62f,
                    new Color(0.12f, 0.13f, 0.12f, 0.88f)
                );

                AddCardText(
                    card.transform,
                    "Message",
                    text,
                    13,
                    FontStyle.Italic,
                    new Color(0.78f, 0.80f, 0.78f),
                    16f,
                    10f,
                    16f,
                    42f,
                    TextAnchor.MiddleCenter,
                    true
                );
            }

            private GameObject CreateCard(
                string name,
                float height,
                Color color
            )
            {
                GameObject card = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image)
                );
                card.transform.SetParent(ContentTransform, false);

                RectTransform rect = card.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, height);

                LayoutElement layout = card.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f;
                layout.minWidth = 180f;
                layout.preferredHeight = height;
                layout.minHeight = height;

                Image image = card.GetComponent<Image>();
                image.color = color;

                return card;
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
                text.font = _font;
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.color = color;
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.supportRichText = false;
                text.resizeTextForBestFit = bestFit;
                text.resizeTextMinSize = 9;
                text.resizeTextMaxSize = fontSize;
                text.text = value ?? "";
            }

            private Color GetStabilityColor(int stability)
            {
                if (stability >= 70)
                {
                    return new Color(0.42f, 0.90f, 0.44f);
                }
                if (stability >= 40)
                {
                    return new Color(0.95f, 0.82f, 0.36f);
                }
                if (stability >= 20)
                {
                    return new Color(1f, 0.58f, 0.24f);
                }
                return new Color(1f, 0.34f, 0.30f);
            }

            private Color GetPartyIdeologyColor(string ideology)
            {
                if (ideology == MonarchismIdeologyId)
                {
                    return new Color(0.72f, 0.48f, 0.95f);
                }
                if (ideology == ConservatismIdeologyId)
                {
                    return new Color(0.76f, 0.62f, 0.38f);
                }
                if (ideology == LiberalismIdeologyId)
                {
                    return new Color(0.32f, 0.68f, 1f);
                }
                if (ideology == DemocracyIdeologyId)
                {
                    return new Color(0.32f, 0.88f, 0.92f);
                }
                if (ideology == SocialismIdeologyId)
                {
                    return new Color(0.96f, 0.46f, 0.50f);
                }
                if (ideology == CommunismIdeologyId)
                {
                    return new Color(0.94f, 0.24f, 0.23f);
                }
                if (ideology == FascismIdeologyId)
                {
                    return new Color(0.84f, 0.50f, 0.20f);
                }
                if (ideology == AnarchismIdeologyId)
                {
                    return new Color(0.72f, 0.72f, 0.72f);
                }
                if (ideology == SyndicalismIdeologyId)
                {
                    return new Color(0.96f, 0.62f, 0.25f);
                }
                return new Color(0.75f, 0.80f, 0.85f);
            }
        }

        private sealed class PartyEditorWindow
            : SingleAutoLayoutWindow<PartyEditorWindow>
        {
            private static KingdomWindow _targetWindow;
            private static Kingdom _targetKingdom;
            private static string _targetPartyId = "";
            private static TextInput _nameInput;
            private static Text _partyLabel;
            private static Text _ideologyLabel;
            private static Text _supportLabel;
            private static Text _colorLabel;
            private static Text _leaderLabel;
            private static Text _traitLabel;
            private static Text _traitSelectionLabel;
            private static Text _statusLabel;
            private static Button _createButton;
            private static Text _createButtonText;
            private static bool _createMode;
            private static int _draftIdeologyIndex;
            private static int _traitCursor;

            private static readonly string[] PartyEditorTraits =
            {
                PartyTraitMass,
                PartyTraitElite,
                PartyTraitDisciplined,
                PartyTraitFactional,
                PartyTraitReformist,
                PartyTraitPopulist,
                PartyTraitRevolutionary,
                PartyTraitMilitarized,
                PartyTraitCorrupt
            };

            public static void ResetWorldSelection()
            {
                _targetWindow = null;
                _targetKingdom = null;
                _targetPartyId = "";
                _createMode = false;
                _draftIdeologyIndex = 0;
                _traitCursor = 0;
            }

            protected override void Init()
            {
                GetLayoutGroup().spacing = 6;
                GetLayoutGroup().padding = new RectOffset(6, 6, 10, 10);

                _partyLabel = CreateWindowText(
                    "PartyName",
                    "",
                    200f,
                    30f,
                    13,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.84f, 0.38f, 1f)
                );

                CreateWindowText(
                    "NameTitle",
                    LM.Get("ukiol_party_editor_name"),
                    200f,
                    20f,
                    10,
                    TextAnchor.MiddleLeft,
                    new Color(0.78f, 0.84f, 0.88f, 1f)
                );

                _nameInput = UnityEngine.Object.Instantiate(
                    TextInput.Prefab,
                    ContentTransform
                );
                _nameInput.transform.localScale = Vector3.one;
                _nameInput.SetSize(new Vector2(200f, 30f));
                _nameInput.input.characterLimit = PartyCustomNameMaxLength;
                _nameInput.input.lineType = InputField.LineType.SingleLine;
                _nameInput.Setup("", delegate(string value) { });

                AddActionButton(
                    "SaveName",
                    LM.Get("ukiol_party_rename_save"),
                    SaveName,
                    new Color(0.18f, 0.34f, 0.18f, 0.96f)
                );
                AddActionButton(
                    "ResetName",
                    LM.Get("ukiol_party_rename_reset"),
                    ResetName,
                    new Color(0.33f, 0.25f, 0.14f, 0.96f)
                );

                _createButton = AddActionButton(
                    "CreateParty",
                    LM.Get("ukiol_party_editor_begin_create"),
                    BeginOrCreateParty,
                    new Color(0.16f, 0.34f, 0.24f, 0.96f)
                );
                if (_createButton != null)
                {
                    _createButtonText =
                        _createButton.GetComponentInChildren<Text>();
                }

                _ideologyLabel = CreateWindowText(
                    "Ideology",
                    "",
                    200f,
                    26f,
                    11,
                    TextAnchor.MiddleCenter,
                    new Color(0.84f, 0.88f, 0.92f, 1f)
                );
                AddActionButton(
                    "PreviousIdeology",
                    "◀  " + LM.Get("ukiol_party_editor_previous_ideology"),
                    PreviousIdeology,
                    new Color(0.18f, 0.22f, 0.30f, 0.96f)
                );
                AddActionButton(
                    "NextIdeology",
                    LM.Get("ukiol_party_editor_next_ideology") + "  ▶",
                    NextIdeology,
                    new Color(0.18f, 0.22f, 0.30f, 0.96f)
                );

                _supportLabel = CreateWindowText(
                    "Support",
                    "",
                    200f,
                    26f,
                    11,
                    TextAnchor.MiddleCenter,
                    new Color(0.78f, 0.90f, 0.72f, 1f)
                );
                AddActionButton(
                    "DecreaseSupport",
                    "−5%  " + LM.Get("ukiol_party_editor_decrease_support"),
                    DecreaseSupport,
                    new Color(0.25f, 0.20f, 0.18f, 0.96f)
                );
                AddActionButton(
                    "IncreaseSupport",
                    "+5%  " + LM.Get("ukiol_party_editor_increase_support"),
                    IncreaseSupport,
                    new Color(0.18f, 0.30f, 0.20f, 0.96f)
                );

                _colorLabel = CreateWindowText(
                    "Color",
                    "",
                    200f,
                    26f,
                    11,
                    TextAnchor.MiddleCenter,
                    Color.white
                );
                AddActionButton(
                    "PreviousColor",
                    "◀  " + LM.Get("ukiol_party_editor_previous_color"),
                    PreviousColor,
                    new Color(0.23f, 0.20f, 0.27f, 0.96f)
                );
                AddActionButton(
                    "NextColor",
                    LM.Get("ukiol_party_editor_next_color") + "  ▶",
                    NextColor,
                    new Color(0.23f, 0.20f, 0.27f, 0.96f)
                );

                _leaderLabel = CreateWindowText(
                    "Leader",
                    "",
                    200f,
                    30f,
                    10,
                    TextAnchor.MiddleCenter,
                    new Color(0.84f, 0.88f, 0.92f, 1f)
                );
                AddActionButton(
                    "PreviousLeader",
                    "◀  " + LM.Get("ukiol_party_editor_previous_leader"),
                    PreviousLeader,
                    new Color(0.22f, 0.24f, 0.18f, 0.96f)
                );
                AddActionButton(
                    "NextLeader",
                    LM.Get("ukiol_party_editor_next_leader") + "  ▶",
                    NextLeader,
                    new Color(0.22f, 0.24f, 0.18f, 0.96f)
                );
                AddActionButton(
                    "AutoLeader",
                    LM.Get("ukiol_party_editor_auto_leader"),
                    AssignLeader,
                    new Color(0.28f, 0.23f, 0.15f, 0.96f)
                );

                _traitLabel = CreateWindowText(
                    "Traits",
                    "",
                    200f,
                    36f,
                    10,
                    TextAnchor.MiddleCenter,
                    new Color(0.86f, 0.82f, 0.72f, 1f)
                );
                _traitSelectionLabel = CreateWindowText(
                    "TraitSelection",
                    "",
                    200f,
                    26f,
                    10,
                    TextAnchor.MiddleCenter,
                    new Color(0.78f, 0.84f, 0.88f, 1f)
                );
                AddActionButton(
                    "PreviousTrait",
                    "◀  " + LM.Get("ukiol_party_editor_previous_trait"),
                    PreviousTrait,
                    new Color(0.24f, 0.20f, 0.28f, 0.96f)
                );
                AddActionButton(
                    "NextTrait",
                    LM.Get("ukiol_party_editor_next_trait") + "  ▶",
                    NextTrait,
                    new Color(0.24f, 0.20f, 0.28f, 0.96f)
                );
                AddActionButton(
                    "ToggleTrait",
                    LM.Get("ukiol_party_editor_toggle_trait"),
                    ToggleTrait,
                    new Color(0.30f, 0.22f, 0.30f, 0.96f)
                );

                _statusLabel = CreateWindowText(
                    "Status",
                    "",
                    200f,
                    42f,
                    10,
                    TextAnchor.MiddleCenter,
                    new Color(0.78f, 0.84f, 0.88f, 1f)
                );
            }

            public static void OpenForParty(
                KingdomWindow window,
                Kingdom kingdom,
                string partyId
            )
            {
                if (
                    Instance == null ||
                    kingdom == null ||
                    string.IsNullOrEmpty(partyId)
                )
                {
                    return;
                }

                _targetWindow = window;
                _targetKingdom = kingdom;
                _targetPartyId = partyId;
                _createMode = false;
                Instance.RefreshFields(false);
                ScrollWindow.showWindow(WindowId);
            }

            public static void OpenForCreate(
                KingdomWindow window,
                Kingdom kingdom
            )
            {
                if (Instance == null || kingdom == null)
                {
                    return;
                }

                _targetWindow = window;
                _targetKingdom = kingdom;
                _targetPartyId = "";
                _createMode = true;
                _draftIdeologyIndex = GetDefaultDraftIdeologyIndex(kingdom);
                if (_nameInput != null)
                {
                    _nameInput.input.text = "";
                }
                Instance.RefreshFields(false);
                ScrollWindow.showWindow(WindowId);
            }

            public override void OnNormalEnable()
            {
                base.OnNormalEnable();
                RefreshFields(false);
            }

            private void RefreshFields(bool preserveStatus)
            {
                UpdateCreateButtonLabel();

                if (_createMode)
                {
                    RefreshCreateFields(preserveStatus);
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    if (_partyLabel != null)
                    {
                        _partyLabel.text = LM.Get("ukiol_party_rename_missing");
                    }
                    if (_nameInput != null)
                    {
                        _nameInput.input.text = "";
                        _nameInput.input.interactable = false;
                    }
                    if (_ideologyLabel != null)
                    {
                        _ideologyLabel.text = "";
                    }
                    if (_supportLabel != null)
                    {
                        _supportLabel.text = "";
                    }
                    if (_colorLabel != null)
                    {
                        _colorLabel.text = "";
                    }
                    if (_leaderLabel != null)
                    {
                        _leaderLabel.text = "";
                    }
                    if (_traitLabel != null)
                    {
                        _traitLabel.text = "";
                    }
                    if (_traitSelectionLabel != null)
                    {
                        _traitSelectionLabel.text = "";
                    }
                    return;
                }

                if (_partyLabel != null)
                {
                    _partyLabel.text = party.Name;
                }
                if (_nameInput != null)
                {
                    _nameInput.input.interactable = true;
                    _nameInput.input.text = party.Name;
                }
                if (_ideologyLabel != null)
                {
                    _ideologyLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_ideology"),
                        GetIdeologyName(party.Ideology)
                    );
                }
                if (_supportLabel != null)
                {
                    _supportLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_support"),
                        ClampInt(party.Support, 0, 100)
                    );
                }
                if (_colorLabel != null)
                {
                    int seed = NormalizePartyColorSeed(party.ColorSeed);
                    _colorLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_color"),
                        seed + 1,
                        PartyColorVariantCount
                    );
                    _colorLabel.color = GetPartyIdentityColor(
                        party.Ideology,
                        seed
                    );
                }
                if (_leaderLabel != null)
                {
                    string leader = string.IsNullOrEmpty(party.LeaderName)
                        ? LM.Get("ukiol_party_leader_unknown")
                        : party.LeaderName;
                    _leaderLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_leader"),
                        leader
                    );
                }
                RefreshTraitFields(party);
                if (!preserveStatus && _statusLabel != null)
                {
                    _statusLabel.text = LM.Get("ukiol_party_editor_hint");
                }
            }

            private static int GetDefaultDraftIdeologyIndex(Kingdom kingdom)
            {
                if (IdeologyIds == null || IdeologyIds.Length == 0)
                {
                    return 0;
                }

                string state = GetStateIdeology(kingdom);
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    if (IdeologyIds[i] == state)
                    {
                        return i;
                    }
                }

                IdeologyNode node = GetIdeologyNode(state);
                if (node != null)
                {
                    for (int i = 0; i < IdeologyIds.Length; i++)
                    {
                        if (IdeologyIds[i] == node.RootIdeologyId)
                        {
                            return i;
                        }
                    }
                }

                return 0;
            }

            private void UpdateCreateButtonLabel()
            {
                if (_createButtonText == null)
                {
                    return;
                }

                _createButtonText.text = _createMode
                    ? LM.Get("ukiol_party_editor_confirm_create")
                    : LM.Get("ukiol_party_editor_begin_create");
            }

            private void RefreshCreateFields(bool preserveStatus)
            {
                if (IdeologyIds == null || IdeologyIds.Length == 0)
                {
                    return;
                }

                if (_draftIdeologyIndex < 0 || _draftIdeologyIndex >= IdeologyIds.Length)
                {
                    _draftIdeologyIndex = 0;
                }

                string ideology = IdeologyIds[_draftIdeologyIndex];
                if (_partyLabel != null)
                {
                    _partyLabel.text = LM.Get("ukiol_party_editor_new_party");
                }
                if (_nameInput != null)
                {
                    _nameInput.input.interactable = true;
                }
                if (_ideologyLabel != null)
                {
                    _ideologyLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_ideology"),
                        GetIdeologyName(ideology)
                    );
                }
                if (_supportLabel != null)
                {
                    _supportLabel.text = LM.Get("ukiol_party_editor_create_support_hint");
                }
                if (_colorLabel != null)
                {
                    _colorLabel.text = LM.Get("ukiol_party_editor_create_color_hint");
                    _colorLabel.color = GetPartyIdentityColor(ideology, 0);
                }
                if (_leaderLabel != null)
                {
                    _leaderLabel.text = LM.Get("ukiol_party_editor_create_leader_hint");
                }
                if (_traitLabel != null)
                {
                    _traitLabel.text = LM.Get("ukiol_party_editor_create_traits_hint");
                }
                if (_traitSelectionLabel != null)
                {
                    _traitSelectionLabel.text = "";
                }
                if (!preserveStatus && _statusLabel != null)
                {
                    _statusLabel.text = LM.Get("ukiol_party_editor_create_hint");
                }
            }

            private void BeginOrCreateParty()
            {
                if (!_createMode)
                {
                    _createMode = true;
                    PoliticalParty current = GetTargetParty();
                    if (current != null && IdeologyIds != null)
                    {
                        for (int i = 0; i < IdeologyIds.Length; i++)
                        {
                            if (IdeologyIds[i] == current.Ideology)
                            {
                                _draftIdeologyIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        _draftIdeologyIndex = GetDefaultDraftIdeologyIndex(_targetKingdom);
                    }
                    _targetPartyId = "";
                    if (_nameInput != null)
                    {
                        _nameInput.input.text = "";
                    }
                    RefreshFields(false);
                    return;
                }

                if (
                    _targetKingdom == null ||
                    IdeologyIds == null ||
                    IdeologyIds.Length == 0
                )
                {
                    return;
                }

                if (_draftIdeologyIndex < 0 || _draftIdeologyIndex >= IdeologyIds.Length)
                {
                    _draftIdeologyIndex = 0;
                }

                string ideology = IdeologyIds[_draftIdeologyIndex];
                string customName = _nameInput == null
                    ? ""
                    : NormalizePartyCustomName(_nameInput.input.text);
                int radicalism = GetKingdomIntData(
                    _targetKingdom,
                    MovementRadicalismPrefix + GetMovementKeySuffix(ideology),
                    20
                );

                string partyId = ScenarioBridge.CreateKingdomParty(
                    _targetKingdom,
                    ideology,
                    radicalism,
                    customName
                );
                if (string.IsNullOrEmpty(partyId))
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_failed"));
                    return;
                }

                _targetPartyId = partyId;
                _createMode = false;

                if (_targetWindow != null)
                {
                    int windowId = _targetWindow.GetInstanceID();
                    _kingdomPoliticsSelectedPartyIds[windowId] = partyId;
                    _kingdomPoliticsSelectedPartyKingdomIds[windowId] =
                        GetStableObjectIdentity(_targetKingdom);
                }

                SetStatus(LM.Get("ukiol_party_editor_created"));
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void RefreshTraitFields(PoliticalParty party)
            {
                if (_traitLabel != null)
                {
                    string formatted = party == null
                        ? ""
                        : FormatPartyTraits(party.Traits);
                    if (string.IsNullOrEmpty(formatted))
                    {
                        formatted = LM.Get("ukiol_party_traits_none");
                    }
                    _traitLabel.text = string.Format(
                        LM.Get("ukiol_party_editor_traits"),
                        formatted
                    );
                }

                if (
                    _traitSelectionLabel == null ||
                    PartyEditorTraits == null ||
                    PartyEditorTraits.Length == 0
                )
                {
                    return;
                }

                if (_traitCursor < 0 || _traitCursor >= PartyEditorTraits.Length)
                {
                    _traitCursor = 0;
                }

                string trait = PartyEditorTraits[_traitCursor];
                bool enabled = party != null && HasPartyTrait(party, trait);
                _traitSelectionLabel.text = string.Format(
                    LM.Get("ukiol_party_editor_trait_selected"),
                    GetPartyTraitLocalizedName(trait),
                    enabled
                        ? LM.Get("ukiol_party_editor_trait_enabled")
                        : LM.Get("ukiol_party_editor_trait_disabled")
                );
            }

            private static PoliticalParty GetTargetParty()
            {
                if (
                    _targetKingdom == null ||
                    string.IsNullOrEmpty(_targetPartyId)
                )
                {
                    return null;
                }

                return FindPoliticalPartyById(
                    GetPoliticalParties(_targetKingdom),
                    _targetPartyId
                );
            }

            private void SaveName()
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_hint"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null || _nameInput == null)
                {
                    return;
                }

                string value = NormalizePartyCustomName(
                    _nameInput.input.text
                );

                if (string.IsNullOrEmpty(value))
                {
                    ResetName();
                    return;
                }

                bool changed = ScenarioBridge.RenameKingdomParty(
                    _targetKingdom,
                    party.Id,
                    value
                );

                SetStatus(
                    changed
                        ? LM.Get("ukiol_party_rename_saved")
                        : LM.Get("ukiol_party_editor_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void ResetName()
            {
                if (_createMode)
                {
                    if (_nameInput != null)
                    {
                        _nameInput.input.text = "";
                    }
                    SetStatus(LM.Get("ukiol_party_editor_create_hint"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                SetKingdomStringData(
                    _targetKingdom,
                    PartySlotKey(
                        PartyV2CustomNamePrefix,
                        party.Slot
                    ),
                    ""
                );

                SetStatus(LM.Get("ukiol_party_rename_reset_done"));
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void PreviousIdeology()
            {
                CycleIdeology(-1);
            }

            private void NextIdeology()
            {
                CycleIdeology(1);
            }

            private void CycleIdeology(int direction)
            {
                if (IdeologyIds == null || IdeologyIds.Length == 0)
                {
                    return;
                }

                if (_createMode)
                {
                    _draftIdeologyIndex =
                        (_draftIdeologyIndex + direction) % IdeologyIds.Length;
                    if (_draftIdeologyIndex < 0)
                    {
                        _draftIdeologyIndex += IdeologyIds.Length;
                    }
                    RefreshFields(true);
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                int index = 0;
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    if (IdeologyIds[i] == party.Ideology)
                    {
                        index = i;
                        break;
                    }
                }

                int next = (index + direction) % IdeologyIds.Length;
                if (next < 0)
                {
                    next += IdeologyIds.Length;
                }

                string ideology = IdeologyIds[next];
                bool changed = ScenarioBridge.SetKingdomPartyIdeologyPreservingSupport(
                    _targetKingdom,
                    party.Id,
                    ideology
                );

                SetStatus(
                    changed
                        ? string.Format(
                            LM.Get("ukiol_party_editor_ideology_changed"),
                            GetIdeologyName(ideology)
                        )
                        : LM.Get("ukiol_party_editor_ideology_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void DecreaseSupport()
            {
                AdjustSupport(-5);
            }

            private void IncreaseSupport()
            {
                AdjustSupport(5);
            }

            private void AdjustSupport(int delta)
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_first"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                int requested = ClampInt(
                    party.Support + delta,
                    0,
                    100
                );
                bool changed = ScenarioBridge.SetKingdomPartySupport(
                    _targetKingdom,
                    party.Id,
                    requested
                );

                PoliticalParty updated = GetTargetParty();
                int actual = updated == null
                    ? requested
                    : ClampInt(updated.Support, 0, 100);

                SetStatus(
                    changed
                        ? string.Format(
                            LM.Get("ukiol_party_editor_support_changed"),
                            actual
                        )
                        : LM.Get("ukiol_party_editor_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void PreviousColor()
            {
                CycleColor(-1);
            }

            private void NextColor()
            {
                CycleColor(1);
            }

            private void CycleColor(int direction)
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_first"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                int next = NormalizePartyColorSeed(
                    party.ColorSeed + direction
                );
                bool changed = ScenarioBridge.SetKingdomPartyColorSeed(
                    _targetKingdom,
                    party.Id,
                    next
                );

                SetStatus(
                    changed
                        ? LM.Get("ukiol_party_editor_color_changed")
                        : LM.Get("ukiol_party_editor_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void PreviousLeader()
            {
                CycleLeader(-1);
            }

            private void NextLeader()
            {
                CycleLeader(1);
            }

            private void CycleLeader(int direction)
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_first"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                List<Actor> candidates = GetPartyEditorLeaderCandidates(
                    _targetKingdom,
                    party
                );
                if (candidates.Count == 0)
                {
                    SetStatus(LM.Get("ukiol_party_editor_leader_failed"));
                    return;
                }

                int current = -1;
                string currentIdentity = party.LeaderIdentity ?? "";
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (GetStableObjectIdentity(candidates[i]) == currentIdentity)
                    {
                        current = i;
                        break;
                    }
                }

                int next;
                if (current < 0)
                {
                    next = direction < 0 ? candidates.Count - 1 : 0;
                }
                else
                {
                    next = (current + direction) % candidates.Count;
                    if (next < 0)
                    {
                        next += candidates.Count;
                    }
                }

                Actor actor = candidates[next];
                bool changed = ScenarioBridge.SetKingdomPartyLeader(
                    _targetKingdom,
                    party.Id,
                    actor
                );
                SetStatus(
                    changed
                        ? string.Format(
                            LM.Get("ukiol_party_editor_leader_changed_to"),
                            GetWorldObjectDisplayName(actor)
                        )
                        : LM.Get("ukiol_party_editor_leader_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private static List<Actor> GetPartyEditorLeaderCandidates(
                Kingdom kingdom,
                PoliticalParty party
            )
            {
                List<Actor> result = new List<Actor>();
                if (kingdom == null || party == null)
                {
                    return result;
                }

                HashSet<string> reserved =
                    ScenarioBridge.GetReservedPartyLeaderIdentities(kingdom, party.Id);
                HashSet<string> seen = new HashSet<string>();
                List<Actor> units = GetKingdomUnitsSafe(kingdom);

                for (int i = 0; i < units.Count; i++)
                {
                    Actor actor = units[i];
                    if (
                        actor == null ||
                        actor.data == null ||
                        !actor.isAlive() ||
                        GetCitizenIdeology(actor) != party.Ideology
                    )
                    {
                        continue;
                    }

                    string identity = GetStableObjectIdentity(actor);
                    if (
                        string.IsNullOrEmpty(identity) ||
                        reserved.Contains(identity) ||
                        !seen.Add(identity)
                    )
                    {
                        continue;
                    }

                    result.Add(actor);
                    if (result.Count >= 128)
                    {
                        break;
                    }
                }

                result.Sort(
                    delegate(Actor left, Actor right)
                    {
                        string leftName = GetWorldObjectDisplayName(left) ?? "";
                        string rightName = GetWorldObjectDisplayName(right) ?? "";
                        int compare = string.Compare(
                            leftName,
                            rightName,
                            StringComparison.OrdinalIgnoreCase
                        );
                        if (compare != 0)
                        {
                            return compare;
                        }
                        return string.Compare(
                            GetStableObjectIdentity(left),
                            GetStableObjectIdentity(right),
                            StringComparison.Ordinal
                        );
                    }
                );

                return result;
            }

            private void PreviousTrait()
            {
                CycleTrait(-1);
            }

            private void NextTrait()
            {
                CycleTrait(1);
            }

            private void CycleTrait(int direction)
            {
                if (
                    PartyEditorTraits == null ||
                    PartyEditorTraits.Length == 0
                )
                {
                    return;
                }

                _traitCursor = (_traitCursor + direction) % PartyEditorTraits.Length;
                if (_traitCursor < 0)
                {
                    _traitCursor += PartyEditorTraits.Length;
                }
                RefreshTraitFields(GetTargetParty());
            }

            private void ToggleTrait()
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_first"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (
                    party == null ||
                    PartyEditorTraits == null ||
                    PartyEditorTraits.Length == 0
                )
                {
                    return;
                }

                if (_traitCursor < 0 || _traitCursor >= PartyEditorTraits.Length)
                {
                    _traitCursor = 0;
                }

                string trait = PartyEditorTraits[_traitCursor];
                bool changed;
                if (HasPartyTrait(party, trait))
                {
                    changed = RemovePartyTrait(party, trait);
                }
                else
                {
                    RemoveConflictingEditorTrait(party, trait);
                    changed = TryAddPartyTrait(party, trait);
                    if (!changed && party.Traits != null && party.Traits.Count >= MaxPartyTraits)
                    {
                        SetStatus(
                            string.Format(
                                LM.Get("ukiol_party_editor_trait_limit"),
                                MaxPartyTraits
                            )
                        );
                        RefreshTraitFields(party);
                        return;
                    }
                }

                if (changed)
                {
                    SavePartyTraits(_targetKingdom, party);
                    SetStatus(
                        string.Format(
                            LM.Get("ukiol_party_editor_trait_changed"),
                            GetPartyTraitLocalizedName(trait)
                        )
                    );
                    RefreshPartyProfile();
                }
                else
                {
                    SetStatus(LM.Get("ukiol_party_editor_failed"));
                }
                RefreshFields(true);
            }

            private static void RemoveConflictingEditorTrait(
                PoliticalParty party,
                string trait
            )
            {
                if (party == null)
                {
                    return;
                }

                if (trait == PartyTraitMass)
                {
                    RemovePartyTrait(party, PartyTraitElite);
                }
                else if (trait == PartyTraitElite)
                {
                    RemovePartyTrait(party, PartyTraitMass);
                }
                else if (trait == PartyTraitDisciplined)
                {
                    RemovePartyTrait(party, PartyTraitFactional);
                }
                else if (trait == PartyTraitFactional)
                {
                    RemovePartyTrait(party, PartyTraitDisciplined);
                }
                else if (trait == PartyTraitReformist)
                {
                    RemovePartyTrait(party, PartyTraitRevolutionary);
                }
                else if (trait == PartyTraitRevolutionary)
                {
                    RemovePartyTrait(party, PartyTraitReformist);
                }
            }

            private void AssignLeader()
            {
                if (_createMode)
                {
                    SetStatus(LM.Get("ukiol_party_editor_create_first"));
                    return;
                }

                PoliticalParty party = GetTargetParty();
                if (party == null)
                {
                    return;
                }

                bool changed = ScenarioBridge.AssignBestKingdomPartyLeader(
                    _targetKingdom,
                    party.Id
                );
                SetStatus(
                    changed
                        ? LM.Get("ukiol_party_editor_leader_changed")
                        : LM.Get("ukiol_party_editor_leader_failed")
                );
                RefreshPartyProfile();
                RefreshFields(true);
            }

            private void SetStatus(string value)
            {
                if (_statusLabel != null)
                {
                    _statusLabel.text = value ?? "";
                }
            }

            private void RefreshPartyProfile()
            {
                try
                {
                    if (
                        _targetWindow != null &&
                        _targetWindow.gameObject != null &&
                        _targetWindow.gameObject.activeInHierarchy
                    )
                    {
                        ShowNativeKingdomPoliticsPanel(
                            _targetWindow,
                            NativePoliticsPageParties
                        );
                    }
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "Could not refresh party profile after edit: " +
                        exception.Message
                    );
                }
            }

            private Text CreateWindowText(
                string objectName,
                string value,
                float width,
                float height,
                int fontSize,
                TextAnchor anchor,
                Color color
            )
            {
                GameObject obj = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Text),
                    typeof(LayoutElement)
                );
                obj.transform.SetParent(ContentTransform, false);

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(width, height);

                LayoutElement layout = obj.GetComponent<LayoutElement>();
                layout.preferredWidth = width;
                layout.preferredHeight = height;
                layout.minHeight = height;

                Text text = obj.GetComponent<Text>();
                OT.InitializeCommonText(text);
                text.text = value;
                text.alignment = anchor;
                text.color = color;
                text.fontSize = fontSize;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = fontSize;
                return text;
            }

            private Button AddActionButton(
                string objectName,
                string label,
                UnityEngine.Events.UnityAction action,
                Color color
            )
            {
                GameObject row = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement)
                );
                row.transform.SetParent(ContentTransform, false);
                row.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(200f, 30f);

                LayoutElement layout = row.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f;
                layout.preferredHeight = 30f;
                layout.minHeight = 30f;

                Image image = row.GetComponent<Image>();
                Sprite background = SpriteTextureLoader.getSprite(
                    "ui/special/windowInnerSliced"
                );
                if (background != null)
                {
                    image.sprite = background;
                    image.type = Image.Type.Sliced;
                }
                image.color = color;

                Button button = row.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(action);

                GameObject textObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(Text)
                );
                textObject.transform.SetParent(row.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(6f, 1f);
                textRect.offsetMax = new Vector2(-6f, -1f);

                Text text = textObject.GetComponent<Text>();
                OT.InitializeCommonText(text);
                text.text = label;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.fontSize = 11;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = 11;
                return button;
            }
        }

        private static string NormalizePartyCustomName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            string cleaned = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            if (cleaned.Length > PartyCustomNameMaxLength)
            {
                cleaned = cleaned.Substring(0, PartyCustomNameMaxLength);
            }

            return cleaned;
        }

        private sealed class IdeologyEditorWindow
            : SingleAutoLayoutWindow<IdeologyEditorWindow>
        {
            private enum EditorTargetMode
            {
                Kingdom,
                City
            }

            private static EditorTargetMode _targetMode;
            private static Kingdom _targetKingdom;
            private static City _targetCity;
            private static WorldTile _targetTile;
            private static Font _font;
            private static string _statusText = "";

            public static void ResetWorldSelection()
            {
                _targetKingdom = null;
                _targetCity = null;
                _targetTile = null;
                _statusText = "";
            }

            protected override void Init()
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>(
                        "Arial.ttf"
                    );
                }

                GetLayoutGroup().spacing = 4;
                GetLayoutGroup().padding = new RectOffset(5, 5, 8, 10);
            }

            public override void OnNormalEnable()
            {
                base.OnNormalEnable();
                RefreshContent();
                ResetScrollToTop();
            }

            public static void OpenForKingdom(
                Kingdom kingdom,
                WorldTile tile
            )
            {
                if (
                    kingdom == null ||
                    tile == null ||
                    Instance == null
                )
                {
                    return;
                }

                _targetMode = EditorTargetMode.Kingdom;
                _targetKingdom = kingdom;
                _targetCity = null;
                _targetTile = tile;
                _statusText = "";
                Instance.RefreshContent();
                ScrollWindow.showWindow(WindowId);
                Instance.ResetScrollToTop();
            }

            public static void OpenForCity(
                City city,
                WorldTile tile
            )
            {
                if (
                    city == null ||
                    tile == null ||
                    Instance == null
                )
                {
                    return;
                }

                _targetMode = EditorTargetMode.City;
                _targetKingdom = GetKingdomFromObject(city);
                _targetCity = city;
                _targetTile = tile;
                _statusText = "";
                Instance.RefreshContent();
                ScrollWindow.showWindow(WindowId);
                Instance.ResetScrollToTop();
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

            private void RefreshContent()
            {
                if (ContentTransform == null)
                {
                    return;
                }

                for (int i = ContentTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = ContentTransform.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }

                EnsureIdeologyRegistry();

                if (!TargetIsAvailable())
                {
                    AddInfoCard(
                        LM.Get("ukiol_ideology_editor_no_target"),
                        new Color(0.34f, 0.16f, 0.16f, 0.95f),
                        new Color(1f, 0.62f, 0.56f)
                    );
                    return;
                }

                string targetName = _targetMode == EditorTargetMode.Kingdom
                    ? GetWorldObjectDisplayName(_targetKingdom)
                    : GetWorldObjectDisplayName(_targetCity);
                string targetType = _targetMode == EditorTargetMode.Kingdom
                    ? LM.Get("ukiol_ideology_editor_target_state")
                    : LM.Get("ukiol_ideology_editor_target_city");
                string currentNodeId = GetCurrentTargetNodeId();
                string currentName = GetNodeDisplayName(currentNodeId);

                AddHeaderCard(
                    targetType,
                    targetName,
                    currentName
                );

                string hint = _targetMode == EditorTargetMode.Kingdom
                    ? LM.Get("ukiol_ideology_editor_hint_state")
                    : LM.Get("ukiol_ideology_editor_hint_city");
                AddInfoCard(
                    hint,
                    new Color(0.11f, 0.13f, 0.15f, 0.94f),
                    new Color(0.80f, 0.84f, 0.88f)
                );

                if (!string.IsNullOrEmpty(_statusText))
                {
                    AddInfoCard(
                        _statusText,
                        new Color(0.10f, 0.22f, 0.12f, 0.95f),
                        new Color(0.52f, 0.92f, 0.56f)
                    );
                }

                // v1.6.0-dev4: make the ideology mechanics visible directly
                // in the editor.  The Politics overview already exposes the
                // same state-level calculations; this compact card lets the
                // player see what the currently selected ideology actually
                // does without leaving the editor.
                AddCurrentEffectsCard(currentNodeId);

                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string rootId = IdeologyIds[i];
                    AddIdeologyNodeRecursive(
                        rootId,
                        0,
                        currentNodeId
                    );
                }
            }

            private bool TargetIsAvailable()
            {
                if (_targetTile == null)
                {
                    return false;
                }

                if (_targetMode == EditorTargetMode.Kingdom)
                {
                    return
                        _targetKingdom != null &&
                        _targetKingdom.data != null;
                }

                return
                    _targetCity != null &&
                    _targetCity.data != null;
            }

            private string GetCurrentTargetNodeId()
            {
                if (_targetMode == EditorTargetMode.Kingdom)
                {
                    string stateCurrent = GetStateIdeologyCurrent(_targetKingdom);
                    if (!string.IsNullOrEmpty(stateCurrent))
                    {
                        return stateCurrent;
                    }
                    return GetStateIdeology(_targetKingdom);
                }

                string dominantIdeology;
                int dominantSupport;
                int tension;
                GetCityIdeologyOverview(
                    _targetCity,
                    out dominantIdeology,
                    out dominantSupport,
                    out tension
                );

                string cityCurrent = GetCityStringData(
                    _targetCity,
                    CityIdeologyCurrentDataKey,
                    ""
                );
                IdeologyNode node = GetIdeologyNode(cityCurrent);
                if (
                    node != null &&
                    node.Tier > 0 &&
                    node.RootIdeologyId == dominantIdeology
                )
                {
                    return cityCurrent;
                }

                return dominantIdeology;
            }

            private void AddIdeologyNodeRecursive(
                string nodeId,
                int depth,
                string currentNodeId
            )
            {
                if (depth > IdeologyRegistryMaxDepth)
                {
                    return;
                }

                IdeologyNode node = GetIdeologyNode(nodeId);
                if (node == null)
                {
                    return;
                }

                AddIdeologyButton(
                    node,
                    depth,
                    node.Id == currentNodeId
                );

                List<string> children;
                if (
                    !IdeologyChildrenRegistry.TryGetValue(
                        node.Id,
                        out children
                    ) ||
                    children == null
                )
                {
                    return;
                }

                for (int i = 0; i < children.Count; i++)
                {
                    AddIdeologyNodeRecursive(
                        children[i],
                        depth + 1,
                        currentNodeId
                    );
                }
            }

            private void AddIdeologyButton(
                IdeologyNode node,
                int depth,
                bool isSelected
            )
            {
                float height = depth == 0 ? 34f : 28f;
                GameObject row = new GameObject(
                    "IdeologyEditor_" + node.Id,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image),
                    typeof(Button)
                );
                row.transform.SetParent(ContentTransform, false);

                RectTransform rect = row.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, height);

                LayoutElement layout = row.GetComponent<LayoutElement>();
                layout.preferredWidth = 200f;
                layout.minWidth = 180f;
                layout.preferredHeight = height;
                layout.minHeight = height;

                Color rootColor = GetRootColor(node.RootIdeologyId);
                Image image = row.GetComponent<Image>();
                Sprite background = SpriteTextureLoader.getSprite(
                    "ui/special/windowInnerSliced"
                );
                if (background != null)
                {
                    image.sprite = background;
                    image.type = Image.Type.Sliced;
                }

                if (isSelected)
                {
                    image.color = new Color(
                        Math.Min(1f, rootColor.r + 0.16f),
                        Math.Min(1f, rootColor.g + 0.16f),
                        Math.Min(1f, rootColor.b + 0.16f),
                        0.96f
                    );
                }
                else if (depth == 0)
                {
                    image.color = new Color(
                        rootColor.r,
                        rootColor.g,
                        rootColor.b,
                        0.82f
                    );
                }
                else
                {
                    image.color = new Color(
                        0.12f + rootColor.r * 0.10f,
                        0.13f + rootColor.g * 0.10f,
                        0.14f + rootColor.b * 0.10f,
                        0.94f
                    );
                }

                Button button = row.GetComponent<Button>();
                button.targetGraphic = image;
                button.navigation = new Navigation()
                {
                    mode = Navigation.Mode.None
                };
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
                colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
                button.colors = colors;

                string capturedNodeId = node.Id;
                button.onClick.AddListener(
                    delegate
                    {
                        ApplyNode(capturedNodeId);
                    }
                );

                GameObject textObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(Text)
                );
                textObject.transform.SetParent(row.transform, false);

                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.offsetMin = new Vector2(
                    10f + depth * 12f,
                    1f
                );
                textRect.offsetMax = new Vector2(-10f, -1f);

                Text text = textObject.GetComponent<Text>();
                text.font = _font;
                text.fontSize = depth == 0 ? 13 : 11;
                text.fontStyle = depth == 0
                    ? FontStyle.Bold
                    : FontStyle.Normal;
                text.alignment = TextAnchor.MiddleLeft;
                text.color = depth == 0
                    ? Color.white
                    : new Color(0.90f, 0.92f, 0.94f);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = depth == 0 ? 13 : 11;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
                text.text =
                    (isSelected ? "* " : "") +
                    GetNodeDisplayName(node.Id);
            }

            private void ApplyNode(string nodeId)
            {
                if (!TargetIsAvailable())
                {
                    _statusText = LM.Get(
                        "ukiol_ideology_editor_no_target"
                    );
                    RefreshContent();
                    return;
                }

                IdeologyNode node = GetIdeologyNode(nodeId);
                if (node == null || !IsValidIdeology(node.RootIdeologyId))
                {
                    return;
                }

                bool applied;
                if (_targetMode == EditorTargetMode.Kingdom)
                {
                    applied = SetIdeologyForKingdom(
                        _targetTile,
                        node.RootIdeologyId,
                        node.Id
                    );
                }
                else
                {
                    applied = SetIdeologyForCity(
                        _targetTile,
                        node.RootIdeologyId,
                        node.Id
                    );
                }

                if (applied)
                {
                    _statusText = string.Format(
                        LM.Get("ukiol_ideology_editor_applied"),
                        GetNodeDisplayName(node.Id)
                    );
                }

                RefreshContent();
            }

            private string GetNodeDisplayName(string nodeId)
            {
                IdeologyNode node = GetIdeologyNode(nodeId);
                if (node == null)
                {
                    return LM.Get("ukiol_ideology_none");
                }

                return node.Tier > 0
                    ? GetIdeologyCurrentName(node.Id)
                    : GetIdeologyName(node.Id);
            }

            private void AddHeaderCard(
                string targetType,
                string targetName,
                string currentName
            )
            {
                GameObject card = CreateSimpleCard(
                    "IdeologyEditorHeader",
                    70f,
                    new Color(0.14f, 0.16f, 0.18f, 0.97f)
                );

                AddSimpleText(
                    card.transform,
                    targetType + ": " + targetName,
                    15,
                    FontStyle.Bold,
                    new Color(1f, 0.82f, 0.34f),
                    10f,
                    7f,
                    10f,
                    27f
                );
                AddSimpleText(
                    card.transform,
                    string.Format(
                        LM.Get("ukiol_ideology_editor_current"),
                        currentName
                    ),
                    12,
                    FontStyle.Normal,
                    new Color(0.84f, 0.88f, 0.92f),
                    10f,
                    38f,
                    10f,
                    25f
                );
            }

            private void AddCurrentEffectsCard(string currentNodeId)
            {
                IdeologyNode node = GetIdeologyNode(currentNodeId);
                if (node == null)
                {
                    return;
                }

                IdeologyBehaviorProfile behavior =
                    GetIdeologyBehaviorProfile(currentNodeId);
                bool isKingdom =
                    _targetMode == EditorTargetMode.Kingdom &&
                    _targetKingdom != null;

                List<string> special = new List<string>();
                if (behavior.Pacifist)
                    special.Add(LM.Get("ukiol_ideology_editor_special_pacifist"));
                if (behavior.Stateless)
                    special.Add(LM.Get("ukiol_ideology_editor_special_stateless"));
                if (behavior.Primitivist)
                    special.Add(LM.Get("ukiol_ideology_editor_special_primitivist"));

                float cardHeight = isKingdom ? 232f : 190f;
                if (special.Count > 0)
                {
                    cardHeight += 24f;
                }

                GameObject card = CreateSimpleCard(
                    "IdeologyEditorEffects",
                    cardHeight,
                    new Color(0.09f, 0.16f, 0.20f, 0.97f)
                );

                Color titleColor = Color.Lerp(
                    GetRootColor(node.RootIdeologyId),
                    Color.white,
                    0.50f
                );
                AddSimpleText(
                    card.transform,
                    string.Format(
                        LM.Get("ukiol_ideology_editor_effects_title"),
                        GetNodeDisplayName(currentNodeId)
                    ),
                    11,
                    FontStyle.Bold,
                    titleColor,
                    8f,
                    5f,
                    8f,
                    28f
                );

                float y = 34f;
                AddEffectSectionHeader(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_profile_header"),
                    y
                );
                y += 17f;

                AddEffectRow(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_market_short"),
                    FormatIdeologyBehaviorPercent(behavior.Market),
                    y,
                    new Color(0.88f, 0.91f, 0.95f)
                );
                y += 16f;
                AddEffectRow(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_welfare_short"),
                    FormatIdeologyBehaviorPercent(behavior.Welfare),
                    y,
                    new Color(0.88f, 0.91f, 0.95f)
                );
                y += 16f;
                AddEffectRow(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_centralization_short"),
                    FormatIdeologyBehaviorPercent(behavior.Centralization),
                    y,
                    new Color(0.88f, 0.91f, 0.95f)
                );
                y += 16f;
                AddEffectRow(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_pluralism_short"),
                    FormatIdeologyBehaviorPercent(behavior.Pluralism),
                    y,
                    new Color(0.88f, 0.91f, 0.95f)
                );
                y += 16f;
                AddEffectRow(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_militarism_short"),
                    FormatIdeologyBehaviorPercent(behavior.Militarism),
                    y,
                    new Color(0.88f, 0.91f, 0.95f)
                );
                y += 19f;

                AddEffectSectionHeader(
                    card.transform,
                    LM.Get("ukiol_ideology_editor_game_effects_header"),
                    y
                );
                y += 17f;

                if (isKingdom)
                {
                    AddWideEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_economy_short") + ": " +
                        GetIdeologyEconomyEffectSummaryCompact(_targetKingdom),
                        y,
                        new Color(0.84f, 0.89f, 0.93f),
                        25f
                    );
                    y += 27f;

                    int stability =
                        GetIdeologyBehaviorStabilityModifier(_targetKingdom);
                    AddEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_stability_short"),
                        FormatSignedPoliticalValue(stability),
                        y,
                        GetSignedEffectColor(stability)
                    );
                    y += 16f;

                    float army = GetIdeologyArmyLimitEffectPercent(_targetKingdom);
                    AddEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_army_short"),
                        FormatSignedPoliticalPercent(army),
                        y,
                        GetSignedEffectColor(army)
                    );
                    y += 16f;

                    int exhaustion =
                        GetIdeologyWarExhaustionPerYear(_targetKingdom);
                    AddEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_exhaustion_short"),
                        "+" + exhaustion.ToString() + "/" +
                        LM.Get("ukiol_year_short"),
                        y,
                        new Color(0.95f, 0.68f, 0.36f)
                    );
                    y += 18f;
                }
                else
                {
                    AddEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_diffusion_short"),
                        "x" + node.DiffusionMultiplier.ToString("0.00"),
                        y,
                        new Color(0.88f, 0.91f, 0.95f)
                    );
                    y += 16f;

                    int localStability = 0;
                    if (node.Tier > 0)
                    {
                        int support = 100;
                        if (_targetCity != null)
                        {
                            support = GetCityIdeologySupport(
                                _targetCity,
                                node.RootIdeologyId
                            );
                        }
                        localStability = GetIdeologyCurrentStabilityModifier(
                            currentNodeId,
                            support
                        );
                    }
                    AddEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_stability_short"),
                        FormatSignedPoliticalValue(localStability),
                        y,
                        GetSignedEffectColor(localStability)
                    );
                    y += 18f;
                }

                if (special.Count > 0)
                {
                    AddWideEffectRow(
                        card.transform,
                        LM.Get("ukiol_ideology_editor_special_label") + ": " +
                        string.Join(", ", special.ToArray()),
                        y,
                        new Color(0.92f, 0.80f, 0.48f),
                        22f
                    );
                }
            }

            private string GetIdeologyEconomyEffectSummaryCompact(Kingdom kingdom)
            {
                if (kingdom == null)
                {
                    return LM.Get("ukiol_ideology_editor_economy_none_short");
                }

                IdeologyBehaviorProfile behavior =
                    GetIdeologyBehaviorProfile(kingdom);
                int ideologySupport = GetKingdomIdeologySupport(
                    kingdom,
                    GetStateIdeology(kingdom)
                );

                if (ideologySupport < 45)
                {
                    return LM.Get("ukiol_ideology_editor_economy_inactive_short");
                }

                int gold = 0;
                int bread = 0;
                if (behavior.Market >= 70) gold += 1;
                else if (behavior.Primitivist) gold -= 1;
                if (behavior.Welfare >= 70) bread += 1;
                if (behavior.Market <= 25 && behavior.Centralization >= 65) gold -= 1;

                List<string> pieces = new List<string>();
                if (gold != 0)
                {
                    pieces.Add(string.Format(
                        LM.Get("ukiol_ideology_effect_gold_city"),
                        FormatSignedPoliticalValue(gold)
                    ));
                }
                if (bread != 0)
                {
                    pieces.Add(string.Format(
                        LM.Get("ukiol_ideology_effect_bread_city"),
                        FormatSignedPoliticalValue(bread)
                    ));
                }

                return pieces.Count == 0
                    ? LM.Get("ukiol_ideology_editor_economy_none_short")
                    : string.Join(", ", pieces.ToArray());
            }

            private Color GetSignedEffectColor(float value)
            {
                if (value > 0.0001f)
                    return new Color(0.42f, 0.92f, 0.50f);
                if (value < -0.0001f)
                    return new Color(1.00f, 0.42f, 0.40f);
                return new Color(0.74f, 0.78f, 0.82f);
            }

            private void AddEffectSectionHeader(
                Transform parent,
                string value,
                float top
            )
            {
                AddSimpleText(
                    parent,
                    value,
                    9,
                    FontStyle.Bold,
                    new Color(1f, 0.72f, 0.28f),
                    8f,
                    top,
                    8f,
                    14f
                );
            }

            private void AddEffectRow(
                Transform parent,
                string label,
                string value,
                float top,
                Color valueColor
            )
            {
                GameObject labelObj = new GameObject(
                    "EffectLabel",
                    typeof(RectTransform),
                    typeof(Text)
                );
                labelObj.transform.SetParent(parent, false);
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.offsetMin = new Vector2(8f, -top - 15f);
                labelRect.offsetMax = new Vector2(-62f, -top);

                Text labelText = labelObj.GetComponent<Text>();
                labelText.font = _font;
                labelText.fontSize = 9;
                labelText.fontStyle = FontStyle.Normal;
                labelText.color = new Color(0.76f, 0.82f, 0.87f);
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.resizeTextForBestFit = true;
                labelText.resizeTextMinSize = 8;
                labelText.resizeTextMaxSize = 9;
                labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
                labelText.verticalOverflow = VerticalWrapMode.Truncate;
                labelText.raycastTarget = false;
                labelText.text = label ?? "";

                GameObject valueObj = new GameObject(
                    "EffectValue",
                    typeof(RectTransform),
                    typeof(Text)
                );
                valueObj.transform.SetParent(parent, false);
                RectTransform valueRect = valueObj.GetComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0f, 1f);
                valueRect.anchorMax = new Vector2(1f, 1f);
                valueRect.pivot = new Vector2(0.5f, 1f);
                valueRect.offsetMin = new Vector2(138f, -top - 15f);
                valueRect.offsetMax = new Vector2(-8f, -top);

                Text valueText = valueObj.GetComponent<Text>();
                valueText.font = _font;
                valueText.fontSize = 9;
                valueText.fontStyle = FontStyle.Bold;
                valueText.color = valueColor;
                valueText.alignment = TextAnchor.MiddleRight;
                valueText.resizeTextForBestFit = true;
                valueText.resizeTextMinSize = 8;
                valueText.resizeTextMaxSize = 9;
                valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                valueText.verticalOverflow = VerticalWrapMode.Truncate;
                valueText.raycastTarget = false;
                valueText.text = value ?? "";
            }

            private void AddWideEffectRow(
                Transform parent,
                string value,
                float top,
                Color color,
                float height
            )
            {
                AddSimpleText(
                    parent,
                    value,
                    9,
                    FontStyle.Normal,
                    color,
                    8f,
                    top,
                    8f,
                    height
                );
            }

            private void AddInfoCard(
                string value,
                Color backgroundColor,
                Color textColor
            )
            {
                int length = string.IsNullOrEmpty(value) ? 0 : value.Length;
                float cardHeight = length > 120 ? 82f : (length > 72 ? 64f : 46f);
                GameObject card = CreateSimpleCard(
                    "IdeologyEditorInfo",
                    cardHeight,
                    backgroundColor
                );
                AddSimpleText(
                    card.transform,
                    value,
                    10,
                    FontStyle.Normal,
                    textColor,
                    8f,
                    4f,
                    8f,
                    cardHeight - 8f
                );
            }

            private GameObject CreateSimpleCard(
                string name,
                float height,
                Color color
            )
            {
                GameObject card = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image)
                );
                card.transform.SetParent(ContentTransform, false);

                RectTransform rect = card.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, height);

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

            private void AddSimpleText(
                Transform parent,
                string value,
                int fontSize,
                FontStyle style,
                Color color,
                float left,
                float top,
                float right,
                float height
            )
            {
                GameObject obj = new GameObject(
                    "Text",
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
                text.font = _font;
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.color = color;
                text.alignment = TextAnchor.MiddleLeft;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = fontSize;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
                text.text = value ?? "";
            }

            private Color GetRootColor(string rootId)
            {
                if (rootId == MonarchismIdeologyId)
                    return new Color(0.52f, 0.30f, 0.70f);
                if (rootId == ConservatismIdeologyId)
                    return new Color(0.52f, 0.40f, 0.20f);
                if (rootId == LiberalismIdeologyId)
                    return new Color(0.18f, 0.45f, 0.72f);
                if (rootId == DemocracyIdeologyId)
                    return new Color(0.18f, 0.62f, 0.68f);
                if (rootId == SocialismIdeologyId)
                    return new Color(0.68f, 0.24f, 0.28f);
                if (rootId == CommunismIdeologyId)
                    return new Color(0.66f, 0.12f, 0.12f);
                if (rootId == FascismIdeologyId)
                    return new Color(0.58f, 0.30f, 0.10f);
                if (rootId == AnarchismIdeologyId)
                    return new Color(0.38f, 0.38f, 0.38f);
                if (rootId == SyndicalismIdeologyId)
                    return new Color(0.66f, 0.36f, 0.10f);
                return new Color(0.30f, 0.34f, 0.38f);
            }
        }

    }
}
