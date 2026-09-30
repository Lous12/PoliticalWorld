using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// API 1.16: public Politics-page registration.
    ///
    /// Addons can add full pages to Political World's kingdom and settlement
    /// Politics hosts without Harmony-patching private window methods. The host
    /// owns navigation, scrollbars and lifecycle; the addon only renders its
    /// page content into the supplied Content transform.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public sealed class PoliticsPageContext
        {
            public InspectorTargetKind TargetKind;
            public KingdomWindow KingdomWindow;
            public CityWindow CityWindow;
            public Kingdom Kingdom;
            public City City;
            public Transform Content;
            public Text TextTemplate;

            public object Target
            {
                get
                {
                    if (TargetKind == InspectorTargetKind.City) return City;
                    return Kingdom;
                }
            }
        }

        public sealed class PoliticsPageDefinition
        {
            public string Id;
            public InspectorTargetKind TargetKind;
            public string NameKey;
            public string DisplayName;
            public string IconPath;
            public int SortOrder;
            public Func<PoliticsPageContext, bool> Visible;
            public Action<PoliticsPageContext> Render;
        }

        public sealed class PoliticsPageInfo
        {
            public string Id;
            public string Source;
            public InspectorTargetKind TargetKind;
            public string DisplayName;
            public string IconPath;
            public int SortOrder;
            public Action<PoliticsPageContext> Render;
        }

        private sealed class PoliticsPageRegistration
        {
            public string Source;
            public PoliticsPageDefinition Definition;
        }

        private static readonly Dictionary<string, PoliticsPageRegistration>
            PoliticsPages = new Dictionary<string, PoliticsPageRegistration>(StringComparer.Ordinal);

        public static partial class UI
        {
            public static bool RegisterPoliticsPage(string addonId, PoliticsPageDefinition definition)
            {
                string owner = Trim(addonId);
                if (!IsAddonRegistered(owner) || definition == null || definition.Render == null) return false;

                string id = Trim(definition.Id);
                if (string.IsNullOrEmpty(id) || !IsOwnedContentId(owner, id)) return false;
                if (definition.TargetKind != InspectorTargetKind.City && definition.TargetKind != InspectorTargetKind.Kingdom) return false;
                if (PoliticsPages.ContainsKey(id))
                {
                    InternalRecordFrameworkIssue(owner, "WARN", "PWEC232", "Politics page id is already registered: " + id);
                    return false;
                }

                PoliticsPages[id] = new PoliticsPageRegistration
                {
                    Source = owner,
                    Definition = new PoliticsPageDefinition
                    {
                        Id = id,
                        TargetKind = definition.TargetKind,
                        NameKey = Trim(definition.NameKey),
                        DisplayName = Trim(definition.DisplayName),
                        IconPath = Trim(definition.IconPath),
                        SortOrder = definition.SortOrder,
                        Visible = definition.Visible,
                        Render = definition.Render
                    }
                };
                return true;
            }

            public static bool RegisterKingdomPoliticsPage(string addonId, PoliticsPageDefinition definition)
            {
                if (definition == null) return false;
                definition.TargetKind = InspectorTargetKind.Kingdom;
                return RegisterPoliticsPage(addonId, definition);
            }

            public static bool RegisterSettlementPoliticsPage(string addonId, PoliticsPageDefinition definition)
            {
                if (definition == null) return false;
                definition.TargetKind = InspectorTargetKind.City;
                return RegisterPoliticsPage(addonId, definition);
            }

            public static bool UnregisterPoliticsPage(string addonId, string pageId)
            {
                string owner = Trim(addonId);
                string id = Trim(pageId);
                PoliticsPageRegistration registration;
                if (!PoliticsPages.TryGetValue(id, out registration) || registration == null) return false;
                if (!string.Equals(registration.Source, owner, StringComparison.Ordinal)) return false;
                return PoliticsPages.Remove(id);
            }

            public static List<PoliticsPageInfo> GetPoliticsPages(Kingdom kingdom)
            {
                PoliticsPageContext context = kingdom == null ? null : new PoliticsPageContext
                {
                    TargetKind = InspectorTargetKind.Kingdom,
                    Kingdom = kingdom
                };
                return BuildPoliticsPages(context);
            }

            public static List<PoliticsPageInfo> GetPoliticsPages(City city)
            {
                PoliticsPageContext context = city == null ? null : new PoliticsPageContext
                {
                    TargetKind = InspectorTargetKind.City,
                    City = city,
                    Kingdom = GetKingdomFromCitySafe(city)
                };
                return BuildPoliticsPages(context);
            }

            public static bool RenderPoliticsPage(PoliticsPageInfo page, PoliticsPageContext context)
            {
                if (page == null || page.Render == null || context == null || context.Content == null) return false;
                try
                {
                    page.Render(context);
                    return true;
                }
                catch (Exception exception)
                {
                    InternalRecordDiagnostic(page.Source, "WARN", "PWDIAG123", exception.Message);
                    return false;
                }
            }

            private static List<PoliticsPageInfo> BuildPoliticsPages(PoliticsPageContext context)
            {
                List<PoliticsPageInfo> result = new List<PoliticsPageInfo>();
                if (context == null || context.Target == null) return result;

                foreach (KeyValuePair<string, PoliticsPageRegistration> pair in PoliticsPages)
                {
                    PoliticsPageRegistration registration = pair.Value;
                    PoliticsPageDefinition definition = registration == null ? null : registration.Definition;
                    if (definition == null || definition.TargetKind != context.TargetKind) continue;

                    bool visible = true;
                    if (definition.Visible != null)
                    {
                        try { visible = definition.Visible(context); }
                        catch (Exception exception)
                        {
                            visible = false;
                            InternalRecordDiagnostic(registration.Source, "WARN", "PWDIAG124", exception.Message);
                        }
                    }
                    if (!visible) continue;

                    result.Add(new PoliticsPageInfo
                    {
                        Id = definition.Id,
                        Source = registration.Source,
                        TargetKind = definition.TargetKind,
                        DisplayName = ResolveLocalization(definition.NameKey, definition.DisplayName),
                        IconPath = definition.IconPath,
                        SortOrder = definition.SortOrder,
                        Render = definition.Render
                    });
                }

                result.Sort(delegate(PoliticsPageInfo a, PoliticsPageInfo b)
                {
                    int order = a.SortOrder.CompareTo(b.SortOrder);
                    if (order != 0) return order;
                    return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
                });
                return result;
            }

            private static int UnregisterAddonPoliticsPagesInternal(string owner)
            {
                if (string.IsNullOrEmpty(owner)) return 0;
                List<string> ids = new List<string>();
                foreach (KeyValuePair<string, PoliticsPageRegistration> pair in PoliticsPages)
                {
                    if (pair.Value != null && string.Equals(pair.Value.Source, owner, StringComparison.Ordinal)) ids.Add(pair.Key);
                }
                int removed = 0;
                for (int i = 0; i < ids.Count; i++) if (PoliticsPages.Remove(ids[i])) removed++;
                return removed;
            }
        }
    }
}
