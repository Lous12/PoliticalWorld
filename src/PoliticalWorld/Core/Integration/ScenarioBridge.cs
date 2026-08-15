using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        /// <summary>
        /// Lightweight public bridge for Scenario Tools and future Political World addons.
        /// It does not run its own simulation loop. All work happens only when an addon
        /// explicitly calls the API or when a registered action is executed.
        /// </summary>
        internal static class ScenarioBridge
        {
            public const string ApiVersion = "1.8.0";

            public delegate bool KingdomActionCondition(Kingdom kingdom);
            public delegate void KingdomActionHandler(Kingdom kingdom);

            public sealed class IdeologyInfo
            {
                public string Id;
                public string ParentId;
                public string RootId;
                public string NameKey;
                public string DisplayName;
                public string DescriptionKey;
                public string Description;
                public string Icon;
                public int SortOrder;
                public string Source;
                public int Tier;
                public string[] Tags;
            }

            /// <summary>
            /// Public definition used by addons to register a new ideology root
            /// or a child current. ParentId empty = root ideology.
            /// </summary>
            public sealed class AddonIdeologyDefinition
            {
                public string Id;
                public string ParentId;
                public string NameKey;
                public string DisplayName;
                public string DescriptionKey;
                public string Description;
                public string Icon;
                public int SortOrder;
                public string Source;
                public int HighSupportStability;
                public int SupportThreshold = -1;
                public int LowSupportStability;
                public float DiffusionMultiplier = 1f;
                // 0 = never appears through Political World's generic random
                // citizen seeding. Addons can opt in with a positive weight.
                public int RandomWeight;
                public string[] Tags;
            }

            public sealed class GovernmentInfo
            {
                public string Id;
                public string DisplayName;
            }

            public sealed class KingdomState
            {
                public string KingdomName;
                public string IdeologyId;
                public string IdeologyName;
                public string CurrentId;
                public string CurrentName;
                public string GovernmentId;
                public string GovernmentName;
                public string PoliticalSystemId;
                public string PoliticalSystemName;
                public int Stability;
            }

            public sealed class PartyInfo
            {
                public string Id;
                public string Name;
                public string IdeologyId;
                public string IdeologyName;
                public string LeaderIdentity;
                public string LeaderName;
                public string FounderIdentity;
                public string FounderName;
                public int FoundedYear;
                public int Support;
                public int Radicalism;
                public bool Active;
                public bool IsRuling;
                public int ColorSeed;
                public string Position;
                public string Strategy;
                public string ForeignStance;
                public string[] Traits;
                public string OriginCityId;
                public string OriginCityName;
                public string ParentPartyId;
                public string ParentPartyName;
            }

            public sealed class KingdomActionInfo
            {
                public string Id;
                public string Category;
                public string NameKey;
                public string DescriptionKey;
                public string DisplayName;
                public string Description;
                public string Source;
                public string Icon;
                public int SortOrder;
                public bool Enabled;
            }

            private sealed class KingdomActionRegistration
            {
                public string Id;
                public string Category;
                public string NameKey;
                public string DescriptionKey;
                public string DisplayName;
                public string Description;
                public string Source;
                public string Icon;
                public int SortOrder;
                public KingdomActionCondition Condition;
                public KingdomActionHandler Handler;
            }

            private static readonly Dictionary<string, KingdomActionRegistration>
                KingdomActions = new Dictionary<string, KingdomActionRegistration>(StringComparer.Ordinal);

            private static readonly Dictionary<string, string>
                AddonIdeologySources = new Dictionary<string, string>(StringComparer.Ordinal);
            private static readonly Dictionary<string, int>
                AddonIdeologyRandomWeights = new Dictionary<string, int>(StringComparer.Ordinal);
            private static readonly Dictionary<string, string>
                AddonIdeologyDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal);
            private static readonly Dictionary<string, string>
                AddonIdeologyDescriptionKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            private static readonly Dictionary<string, string>
                AddonIdeologyDescriptions = new Dictionary<string, string>(StringComparer.Ordinal);
            private static readonly Dictionary<string, string>
                AddonIdeologyIcons = new Dictionary<string, string>(StringComparer.Ordinal);
            private static readonly Dictionary<string, int>
                AddonIdeologySortOrders = new Dictionary<string, int>(StringComparer.Ordinal);

            // Shared/global tags intentionally keep their legacy save key for compatibility.
            private const string AddonKingdomTagsDataKey = "ukiol_api_kingdom_tags";
            private const string LegacyAddonDataPrefix = "ukiol_api_data_";
            private const string AddonDataV2Prefix = "pw_api2_data_";

            public static bool IsReady()
            {
                EnsureIdeologyRegistry();
                return _ideologyRegistryInitialized;
            }

            internal static string GetKingdomDisplayName(Kingdom kingdom)
            {
                return kingdom == null ? "" : (GetWorldObjectDisplayName(kingdom) ?? "");
            }

            public static List<IdeologyInfo> GetIdeologies()
            {
                EnsureIdeologyRegistry();
                List<IdeologyInfo> result = new List<IdeologyInfo>();

                foreach (KeyValuePair<string, IdeologyNode> pair in IdeologyNodeRegistry)
                {
                    IdeologyNode node = pair.Value;
                    if (node == null)
                    {
                        continue;
                    }

                    result.Add(BuildIdeologyInfo(node));
                }

                SortIdeologyInfos(result);
                return result;
            }

            public static IdeologyInfo GetIdeology(string ideologyId)
            {
                if (string.IsNullOrWhiteSpace(ideologyId))
                {
                    return null;
                }

                EnsureIdeologyRegistry();
                IdeologyNode node = GetIdeologyNode(ideologyId.Trim());
                return node == null ? null : BuildIdeologyInfo(node);
            }

            public static List<IdeologyInfo> GetIdeologiesBySource(string source)
            {
                EnsureIdeologyRegistry();
                string wanted = source == null ? "" : source.Trim();
                List<IdeologyInfo> result = new List<IdeologyInfo>();
                foreach (KeyValuePair<string, IdeologyNode> pair in IdeologyNodeRegistry)
                {
                    IdeologyNode node = pair.Value;
                    if (
                        node == null ||
                        !string.Equals(GetIdeologySource(node.Id), wanted, StringComparison.Ordinal)
                    )
                    {
                        continue;
                    }
                    result.Add(BuildIdeologyInfo(node));
                }
                SortIdeologyInfos(result);
                return result;
            }

            public static List<IdeologyInfo> GetRootIdeologies()
            {
                EnsureIdeologyRegistry();
                List<IdeologyInfo> result = new List<IdeologyInfo>();
                foreach (KeyValuePair<string, IdeologyNode> pair in IdeologyNodeRegistry)
                {
                    IdeologyNode node = pair.Value;
                    if (node != null && node.Tier == 0)
                    {
                        result.Add(BuildIdeologyInfo(node));
                    }
                }
                SortIdeologyInfos(result);
                return result;
            }

            public static List<IdeologyInfo> GetCurrentsForRoot(string rootId)
            {
                EnsureIdeologyRegistry();
                string wanted = rootId == null ? "" : rootId.Trim();
                List<IdeologyInfo> result = new List<IdeologyInfo>();
                foreach (KeyValuePair<string, IdeologyNode> pair in IdeologyNodeRegistry)
                {
                    IdeologyNode node = pair.Value;
                    if (
                        node != null &&
                        node.Tier > 0 &&
                        string.Equals(node.RootIdeologyId ?? "", wanted, StringComparison.Ordinal)
                    )
                    {
                        result.Add(BuildIdeologyInfo(node));
                    }
                }
                SortIdeologyInfos(result);
                return result;
            }

            private static IdeologyInfo BuildIdeologyInfo(IdeologyNode node)
            {
                if (node == null)
                {
                    return null;
                }
                return new IdeologyInfo()
                {
                    Id = node.Id ?? "",
                    ParentId = node.ParentId ?? "",
                    RootId = node.RootIdeologyId ?? "",
                    NameKey = node.NameKey ?? node.Id ?? "",
                    DisplayName = ResolveIdeologyDisplayName(node),
                    DescriptionKey = GetAddonIdeologyMetadata(AddonIdeologyDescriptionKeys, node.Id),
                    Description = ResolveIdeologyDescription(node),
                    Icon = GetAddonIdeologyMetadata(AddonIdeologyIcons, node.Id),
                    SortOrder = GetAddonIdeologySortOrder(node.Id),
                    Source = GetIdeologySource(node.Id),
                    Tier = node.Tier,
                    Tags = node.Tags == null
                        ? new string[0]
                        : (string[])node.Tags.Clone()
                };
            }

            private static void SortIdeologyInfos(List<IdeologyInfo> result)
            {
                if (result == null)
                {
                    return;
                }
                result.Sort(delegate(IdeologyInfo a, IdeologyInfo b)
                {
                    int rootCompare = string.CompareOrdinal(
                        a == null ? "" : (a.RootId ?? ""),
                        b == null ? "" : (b.RootId ?? "")
                    );
                    if (rootCompare != 0)
                    {
                        return rootCompare;
                    }

                    int tierCompare = (a == null ? 0 : a.Tier).CompareTo(
                        b == null ? 0 : b.Tier
                    );
                    if (tierCompare != 0)
                    {
                        return tierCompare;
                    }

                    int orderCompare = (a == null ? 0 : a.SortOrder).CompareTo(
                        b == null ? 0 : b.SortOrder
                    );
                    if (orderCompare != 0)
                    {
                        return orderCompare;
                    }

                    return string.CompareOrdinal(
                        a == null ? "" : (a.DisplayName ?? ""),
                        b == null ? "" : (b.DisplayName ?? "")
                    );
                });
            }

            /// <summary>
            /// Registers addon ideology content into the same registry used by
            /// Political World. This is a registration-time operation, not a
            /// simulation loop. Root ideologies are also added to the dynamic
            /// root list used by support/evolution code.
            /// </summary>
            public static bool RegisterAddonIdeology(AddonIdeologyDefinition definition)
            {
                if (
                    definition == null ||
                    string.IsNullOrWhiteSpace(definition.Id) ||
                    string.IsNullOrWhiteSpace(definition.Source)
                )
                {
                    return false;
                }

                EnsureIdeologyRegistry();
                string id = definition.Id.Trim();
                if (GetIdeologyNode(id) != null)
                {
                    return false;
                }

                string parentId = string.IsNullOrWhiteSpace(definition.ParentId)
                    ? ""
                    : definition.ParentId.Trim();
                if (!string.IsNullOrEmpty(parentId) && GetIdeologyNode(parentId) == null)
                {
                    return false;
                }

                string nameKey = string.IsNullOrWhiteSpace(definition.NameKey)
                    ? id
                    : definition.NameKey.Trim();
                string[] tags = NormalizeStringIds(definition.Tags);

                RegisterIdeologyNode(
                    id,
                    parentId,
                    nameKey,
                    definition.HighSupportStability,
                    definition.SupportThreshold,
                    definition.LowSupportStability,
                    definition.DiffusionMultiplier <= 0f ? 1f : definition.DiffusionMultiplier,
                    tags
                );

                IdeologyNode registered = GetIdeologyNode(id);
                if (registered == null)
                {
                    return false;
                }

                AddonIdeologySources[id] = definition.Source.Trim();
                AddonIdeologyDisplayNames[id] = definition.DisplayName == null ? "" : definition.DisplayName.Trim();
                AddonIdeologyDescriptionKeys[id] = definition.DescriptionKey == null ? "" : definition.DescriptionKey.Trim();
                AddonIdeologyDescriptions[id] = definition.Description == null ? "" : definition.Description.Trim();
                AddonIdeologyIcons[id] = definition.Icon == null ? "" : definition.Icon.Trim();
                AddonIdeologySortOrders[id] = definition.SortOrder;
                PoliticalWorldAPI.InternalSeedLocalizationFallback(
                    definition.Source,
                    nameKey,
                    definition.DisplayName
                );
                PoliticalWorldAPI.InternalSeedLocalizationFallback(
                    definition.Source,
                    definition.DescriptionKey,
                    definition.Description
                );
                IdeologyBehaviorRegistry.Remove(id);

                if (registered.Tier == 0)
                {
                    AddonIdeologyRandomWeights[id] = Math.Max(0, definition.RandomWeight);
                    if (!RootIdeologyArrayContains(id))
                    {
                        Array.Resize(ref IdeologyIds, IdeologyIds.Length + 1);
                        IdeologyIds[IdeologyIds.Length - 1] = id;
                    }
                }
                return true;
            }

            public static string GetIdeologySource(string ideologyId)
            {
                if (string.IsNullOrEmpty(ideologyId))
                {
                    return "";
                }

                string source;
                if (AddonIdeologySources.TryGetValue(ideologyId, out source))
                {
                    return source ?? "";
                }
                return GetIdeologyNode(ideologyId) == null
                    ? ""
                    : "Lous12.PoliticalWorld";
            }

            public static string[] GetIdeologyTags(string ideologyId, bool includeParents)
            {
                IdeologyNode node = GetIdeologyNode(ideologyId);
                if (node == null)
                {
                    return new string[0];
                }

                List<string> result = new List<string>();
                int guard = 0;
                IdeologyNode cursor = node;
                while (cursor != null && guard < IdeologyRegistryMaxDepth)
                {
                    if (cursor.Tags != null)
                    {
                        for (int i = 0; i < cursor.Tags.Length; i++)
                        {
                            string tag = cursor.Tags[i];
                            if (!string.IsNullOrEmpty(tag) && !result.Contains(tag))
                            {
                                result.Add(tag);
                            }
                        }
                    }
                    if (!includeParents || string.IsNullOrEmpty(cursor.ParentId))
                    {
                        break;
                    }
                    cursor = GetIdeologyNode(cursor.ParentId);
                    guard++;
                }
                return result.ToArray();
            }

            public static bool HasIdeologyTag(string ideologyId, string tag, bool includeParents)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return false;
                }
                string wanted = tag.Trim();
                string[] tags = GetIdeologyTags(ideologyId, includeParents);
                for (int i = 0; i < tags.Length; i++)
                {
                    if (string.Equals(tags[i], wanted, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }

            public static Actor GetKingdomRuler(Kingdom kingdom)
            {
                return kingdom == null ? null : GetLivingRuler(kingdom);
            }

            public static bool RulerHasTrait(Kingdom kingdom, string traitId)
            {
                Actor ruler = GetKingdomRuler(kingdom);
                if (ruler == null || string.IsNullOrWhiteSpace(traitId))
                {
                    return false;
                }
                try
                {
                    return ruler.hasTrait(traitId.Trim());
                }
                catch
                {
                    return false;
                }
            }

            public static string GetRulerRaceId(Kingdom kingdom)
            {
                return GetActorRaceIdSafe(GetKingdomRuler(kingdom));
            }

            public static bool RulerIsImmortal(Kingdom kingdom)
            {
                Actor ruler = GetKingdomRuler(kingdom);
                return HasAnyActorTraitSafe(
                    ruler,
                    "immortal",
                    "trait_immortal"
                );
            }

            public static List<string> GetKingdomTags(Kingdom kingdom)
            {
                List<string> result = new List<string>();
                if (kingdom == null)
                {
                    return result;
                }
                string raw = GetKingdomStringData(kingdom, AddonKingdomTagsDataKey, "");
                string[] pieces = (raw ?? "").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < pieces.Length; i++)
                {
                    string tag = pieces[i] == null ? "" : pieces[i].Trim();
                    if (!string.IsNullOrEmpty(tag) && !result.Contains(tag))
                    {
                        result.Add(tag);
                    }
                }
                result.Sort(StringComparer.Ordinal);
                return result;
            }

            public static bool HasKingdomTag(Kingdom kingdom, string tag)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(tag))
                {
                    return false;
                }
                string wanted = tag.Trim();
                List<string> tags = GetKingdomTags(kingdom);
                return tags.Contains(wanted);
            }

            public static bool AddKingdomTag(Kingdom kingdom, string tag)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(tag) || tag.IndexOf('|') >= 0)
                {
                    return false;
                }
                string wanted = tag.Trim();
                List<string> tags = GetKingdomTags(kingdom);
                if (!tags.Contains(wanted))
                {
                    tags.Add(wanted);
                    tags.Sort(StringComparer.Ordinal);
                    SetKingdomStringData(kingdom, AddonKingdomTagsDataKey, string.Join("|", tags.ToArray()));
                }
                return true;
            }

            public static bool RemoveKingdomTag(Kingdom kingdom, string tag)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(tag))
                {
                    return false;
                }
                List<string> tags = GetKingdomTags(kingdom);
                bool removed = tags.Remove(tag.Trim());
                if (removed)
                {
                    SetKingdomStringData(kingdom, AddonKingdomTagsDataKey, string.Join("|", tags.ToArray()));
                }
                return removed;
            }

            public static int GetAddonKingdomInt(Kingdom kingdom, string addonId, string key, int fallback)
            {
                string dataKey = MakeAddonDataKey(addonId, key);
                if (kingdom == null || string.IsNullOrEmpty(dataKey)) return fallback;

                const int missing = int.MinValue;
                int current = GetKingdomIntData(kingdom, dataKey, missing);
                if (current != missing) return current;

                // API 1.1 migration fallback. If legacy data exists, copy it once
                // into the collision-safe v2 namespace and keep the old key untouched.
                string legacyKey = MakeLegacyAddonDataKey(addonId, key);
                int legacy = string.IsNullOrEmpty(legacyKey)
                    ? missing
                    : GetKingdomIntData(kingdom, legacyKey, missing);
                if (legacy != missing)
                {
                    SetKingdomIntData(kingdom, dataKey, legacy);
                    return legacy;
                }
                return fallback;
            }

            public static bool SetAddonKingdomInt(Kingdom kingdom, string addonId, string key, int value)
            {
                string dataKey = MakeAddonDataKey(addonId, key);
                if (kingdom == null || string.IsNullOrEmpty(dataKey))
                {
                    return false;
                }
                SetKingdomIntData(kingdom, dataKey, value);
                return true;
            }

            public static string GetAddonKingdomString(Kingdom kingdom, string addonId, string key, string fallback)
            {
                string dataKey = MakeAddonDataKey(addonId, key);
                if (kingdom == null || string.IsNullOrEmpty(dataKey)) return fallback;

                const string missing = "\u0001PW_API_MISSING\u0001";
                string current = GetKingdomStringData(kingdom, dataKey, missing);
                if (!string.Equals(current, missing, StringComparison.Ordinal)) return current;

                string legacyKey = MakeLegacyAddonDataKey(addonId, key);
                string legacy = string.IsNullOrEmpty(legacyKey)
                    ? missing
                    : GetKingdomStringData(kingdom, legacyKey, missing);
                if (!string.Equals(legacy, missing, StringComparison.Ordinal))
                {
                    SetKingdomStringData(kingdom, dataKey, legacy);
                    return legacy;
                }
                return fallback;
            }

            public static bool SetAddonKingdomString(Kingdom kingdom, string addonId, string key, string value)
            {
                string dataKey = MakeAddonDataKey(addonId, key);
                if (kingdom == null || string.IsNullOrEmpty(dataKey))
                {
                    return false;
                }
                SetKingdomStringData(kingdom, dataKey, value ?? "");
                return true;
            }

            public static List<string> GetAddonKingdomTags(Kingdom kingdom, string addonId)
            {
                List<string> result = new List<string>();
                if (kingdom == null || string.IsNullOrWhiteSpace(addonId)) return result;
                string raw = GetAddonKingdomString(kingdom, addonId, "__private_tags", "");
                if (string.IsNullOrEmpty(raw)) return result;
                string[] parts = raw.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    string value = parts[i] == null ? "" : parts[i].Trim();
                    if (!string.IsNullOrEmpty(value) && !result.Contains(value)) result.Add(value);
                }
                return result;
            }

            public static bool HasAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
            {
                if (string.IsNullOrWhiteSpace(localTag)) return false;
                string wanted = localTag.Trim();
                List<string> tags = GetAddonKingdomTags(kingdom, addonId);
                for (int i = 0; i < tags.Count; i++)
                {
                    if (string.Equals(tags[i], wanted, StringComparison.Ordinal)) return true;
                }
                return false;
            }

            public static bool AddAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(addonId) || string.IsNullOrWhiteSpace(localTag)) return false;
                string wanted = localTag.Trim();
                if (wanted.IndexOf('|') >= 0) return false;
                List<string> tags = GetAddonKingdomTags(kingdom, addonId);
                if (tags.Contains(wanted)) return true;
                tags.Add(wanted);
                return SetAddonKingdomString(kingdom, addonId, "__private_tags", string.Join("|", tags.ToArray()));
            }

            public static bool RemoveAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(localTag)) return false;
                List<string> tags = GetAddonKingdomTags(kingdom, addonId);
                bool removed = tags.Remove(localTag.Trim());
                if (!removed) return false;
                return SetAddonKingdomString(kingdom, addonId, "__private_tags", string.Join("|", tags.ToArray()));
            }

            private static string MakeAddonDataKey(string addonId, string key)
            {
                if (string.IsNullOrWhiteSpace(addonId) || string.IsNullOrWhiteSpace(key))
                {
                    return "";
                }
                // Hex-encode UTF-8 bytes instead of replacing punctuation.
                // This keeps identifiers such as author.my-addon and author.my_addon
                // distinct while producing a save-key made only of [0-9A-F].
                return AddonDataV2Prefix + EncodeUtf8Hex(addonId.Trim()) + "_" + EncodeUtf8Hex(key.Trim());
            }

            private static string MakeLegacyAddonDataKey(string addonId, string key)
            {
                if (string.IsNullOrWhiteSpace(addonId) || string.IsNullOrWhiteSpace(key)) return "";
                return LegacyAddonDataPrefix + SanitizeLegacyDataToken(addonId) + "_" + SanitizeLegacyDataToken(key);
            }

            private static string EncodeUtf8Hex(string value)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value ?? "");
                char[] chars = new char[bytes.Length * 2];
                const string hex = "0123456789ABCDEF";
                for (int i = 0; i < bytes.Length; i++)
                {
                    chars[i * 2] = hex[(bytes[i] >> 4) & 15];
                    chars[i * 2 + 1] = hex[bytes[i] & 15];
                }
                return new string(chars);
            }

            private static string SanitizeLegacyDataToken(string value)
            {
                string source = value == null ? "" : value.Trim();
                char[] chars = source.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    char c = chars[i];
                    bool safe =
                        (c >= 'a' && c <= 'z') ||
                        (c >= 'A' && c <= 'Z') ||
                        (c >= '0' && c <= '9') ||
                        c == '_';
                    if (!safe) chars[i] = '_';
                }
                return new string(chars);
            }

            private static string[] NormalizeStringIds(string[] values)
            {
                if (values == null || values.Length == 0)
                {
                    return new string[0];
                }
                List<string> result = new List<string>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i] == null ? "" : values[i].Trim();
                    if (!string.IsNullOrEmpty(value) && seen.Add(value))
                    {
                        result.Add(value);
                    }
                }
                return result.ToArray();
            }

            public static string PickRandomSeedIdeology()
            {
                EnsureIdeologyRegistry();
                int totalWeight = 0;
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string id = IdeologyIds[i];
                    int weight = 100;
                    if (AddonIdeologySources.ContainsKey(id))
                    {
                        if (!AddonIdeologyRandomWeights.TryGetValue(id, out weight))
                        {
                            weight = 0;
                        }
                    }
                    totalWeight += Math.Max(0, weight);
                }

                if (totalWeight <= 0)
                {
                    return ConservatismIdeologyId;
                }

                int roll = UnityEngine.Random.Range(0, totalWeight);
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string id = IdeologyIds[i];
                    int weight = 100;
                    if (AddonIdeologySources.ContainsKey(id))
                    {
                        if (!AddonIdeologyRandomWeights.TryGetValue(id, out weight))
                        {
                            weight = 0;
                        }
                    }
                    weight = Math.Max(0, weight);
                    if (roll < weight)
                    {
                        return id;
                    }
                    roll -= weight;
                }
                return ConservatismIdeologyId;
            }

            private static bool RootIdeologyArrayContains(string id)
            {
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    if (string.Equals(IdeologyIds[i], id, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }

            public static List<GovernmentInfo> GetGovernmentForms()
            {
                string[] ids = new string[]
                {
                    GovernmentAbsoluteMonarchyId,
                    GovernmentConstitutionalMonarchyId,
                    GovernmentParliamentaryRepublicId,
                    GovernmentPresidentialRepublicId,
                    GovernmentOnePartyStateId,
                    GovernmentMilitaryDictatorshipId,
                    GovernmentCouncilRepublicId,
                    GovernmentOligarchyId
                };

                List<GovernmentInfo> result = new List<GovernmentInfo>();
                for (int i = 0; i < ids.Length; i++)
                {
                    result.Add(new GovernmentInfo()
                    {
                        Id = ids[i],
                        DisplayName = GetGovernmentFormName(ids[i])
                    });
                }
                return result;
            }

            public static KingdomState GetKingdomState(Kingdom kingdom)
            {
                if (kingdom == null)
                {
                    return null;
                }

                string ideology = GetStateIdeology(kingdom) ?? "";
                string current = GetStateIdeologyCurrent(kingdom) ?? "";
                string government = GetGovernmentPublicId(kingdom) ?? "";
                string system = GetPoliticalSystem(kingdom) ?? "";

                return new KingdomState()
                {
                    KingdomName = GetWorldObjectDisplayName(kingdom),
                    IdeologyId = ideology,
                    IdeologyName = ResolveIdeologyDisplayName(GetIdeologyNode(ideology)),
                    CurrentId = current,
                    CurrentName = ResolveIdeologyDisplayName(GetIdeologyNode(current)),
                    GovernmentId = government,
                    GovernmentName = GetGovernmentPublicName(kingdom),
                    PoliticalSystemId = system,
                    PoliticalSystemName = GetPoliticalSystemName(system),
                    Stability = GetNationalStability(kingdom)
                };
            }

            public static bool SetKingdomIdeology(Kingdom kingdom, string ideologyId)
            {
                EnsureIdeologyRegistry();
                IdeologyNode node = GetIdeologyNode(ideologyId);
                if (node == null || node.Tier != 0)
                {
                    return false;
                }

                return SetStateIdeology(kingdom, ideologyId);
            }

            public static bool SetKingdomCurrent(Kingdom kingdom, string currentId)
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return false;
                }

                EnsureIdeologyRegistry();
                IdeologyNode node = GetIdeologyNode(currentId);
                string ideology = GetStateIdeology(kingdom);
                if (
                    node == null ||
                    node.Tier <= 0 ||
                    string.IsNullOrEmpty(ideology) ||
                    !string.Equals(node.RootIdeologyId, ideology, StringComparison.Ordinal)
                )
                {
                    return false;
                }

                SetStateIdeologyCurrent(kingdom, currentId);
                ResetIdeologyCurrentCandidate(kingdom);
                return true;
            }

            public static bool SetKingdomGovernment(Kingdom kingdom, string governmentId, bool publishEvent)
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(governmentId))
                {
                    return false;
                }

                string wanted = governmentId.Trim();
                if (PoliticalWorldAPI.InternalIsRegisteredGovernment(wanted))
                {
                    string baseId = PoliticalWorldAPI.InternalGetRegisteredGovernmentBaseId(wanted);
                    if (!IsValidGovernmentForm(baseId)) return false;

                    string previousPublic = GetGovernmentPublicId(kingdom);
                    SetGovernmentForm(kingdom, baseId, false, true);
                    SetCustomGovernmentIdentity(kingdom, wanted);
                    string currentPublic = GetGovernmentPublicId(kingdom);

                    if (!string.Equals(previousPublic, currentPublic, StringComparison.Ordinal))
                    {
                        PoliticalWorldAPI.InternalEmitCoreEvent(
                            PoliticalWorldAPI.Events.GovernmentChanged,
                            kingdom,
                            previousPublic,
                            currentPublic
                        );
                    }

                    if (publishEvent && !string.Equals(previousPublic, currentPublic, StringComparison.Ordinal))
                    {
                        PublishPoliticalEvent(
                            string.Format(
                                NeoModLoader.General.LM.Get("ukiol_event_government_changed"),
                                GetWorldObjectDisplayName(kingdom),
                                GetGovernmentPublicName(kingdom)
                            ),
                            kingdom,
                            null,
                            GetLivingRuler(kingdom),
                            OverviewIconPath,
                            "government_changed_" + wanted,
                            35f
                        );
                    }
                    GetPoliticalSystem(kingdom);
                    return true;
                }

                if (!IsValidGovernmentForm(wanted)) return false;
                SetGovernmentForm(kingdom, wanted, publishEvent);
                GetPoliticalSystem(kingdom);
                return true;
            }

            public static bool SetKingdomStability(Kingdom kingdom, int value)
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return false;
                }

                SetNationalStability(kingdom, value);
                return true;
            }

            public static bool ChangeKingdomStability(Kingdom kingdom, int delta)
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return false;
                }

                SetNationalStability(kingdom, GetNationalStability(kingdom) + delta);
                return true;
            }

            public static List<PartyInfo> GetKingdomParties(Kingdom kingdom)
            {
                return GetKingdomParties(kingdom, false);
            }

            public static List<PartyInfo> GetKingdomParties(
                Kingdom kingdom,
                bool includeInactive
            )
            {
                List<PartyInfo> result = new List<PartyInfo>();
                if (kingdom == null)
                {
                    return result;
                }

                List<PoliticalParty> parties;
                if (includeInactive)
                {
                    parties = LoadPoliticalPartiesInternal(kingdom, true);
                    List<PoliticalParty> activeParties = new List<PoliticalParty>();
                    for (int i = 0; i < parties.Count; i++)
                    {
                        if (parties[i] != null && parties[i].Active)
                        {
                            activeParties.Add(parties[i]);
                        }
                    }
                    AggregatePartySupportFromCities(kingdom, activeParties);
                }
                else
                {
                    parties = GetPoliticalParties(kingdom);
                }

                string rulingId = GetStoredRulingPartyId(kingdom);
                for (int i = 0; i < parties.Count; i++)
                {
                    PoliticalParty party = parties[i];
                    if (party == null)
                    {
                        continue;
                    }
                    result.Add(BuildPartyInfo(kingdom, party, rulingId));
                }

                result.Sort(delegate(PartyInfo a, PartyInfo b)
                {
                    if (a.Active != b.Active)
                    {
                        return a.Active ? -1 : 1;
                    }
                    int supportCompare = b.Support.CompareTo(a.Support);
                    if (supportCompare != 0)
                    {
                        return supportCompare;
                    }
                    return string.CompareOrdinal(a.Name ?? "", b.Name ?? "");
                });
                return result;
            }

            public static PartyInfo GetKingdomParty(
                Kingdom kingdom,
                string partyId,
                bool includeInactive
            )
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (
                    party == null ||
                    (!includeInactive && !party.Active)
                )
                {
                    return null;
                }

                if (party.Active)
                {
                    List<PoliticalParty> active = GetPoliticalParties(kingdom);
                    for (int i = 0; i < active.Count; i++)
                    {
                        if (active[i] != null && active[i].Id == party.Id)
                        {
                            party = active[i];
                            break;
                        }
                    }
                }

                return BuildPartyInfo(
                    kingdom,
                    party,
                    GetStoredRulingPartyId(kingdom)
                );
            }

            public static PartyInfo GetKingdomRulingParty(Kingdom kingdom)
            {
                string rulingId = GetStoredRulingPartyId(kingdom);
                if (string.IsNullOrEmpty(rulingId))
                {
                    return null;
                }

                PoliticalParty party = FindPartyById(kingdom, rulingId);
                if (party == null || !party.Active)
                {
                    return null;
                }

                List<PoliticalParty> active = GetPoliticalParties(kingdom);
                for (int i = 0; i < active.Count; i++)
                {
                    if (active[i] != null && active[i].Id == rulingId)
                    {
                        party = active[i];
                        break;
                    }
                }

                return BuildPartyInfo(kingdom, party, rulingId);
            }

            public static Actor GetKingdomPartyLeader(
                Kingdom kingdom,
                string partyId
            )
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null)
                {
                    return null;
                }

                return FindPartyActorByIdentity(
                    kingdom,
                    party.LeaderIdentity,
                    party.LeaderName
                );
            }

            public static string CreateKingdomParty(
                Kingdom kingdom,
                string ideologyId,
                int radicalism,
                string customName
            )
            {
                if (kingdom == null || !IsValidIdeology(ideologyId))
                {
                    return "";
                }

                PoliticalParty party = CreatePoliticalParty(
                    kingdom,
                    ideologyId,
                    ClampInt(radicalism, 0, 100),
                    false
                );
                if (party == null)
                {
                    return "";
                }

                if (!string.IsNullOrWhiteSpace(customName))
                {
                    string cleaned = customName.Trim();
                    SetKingdomStringData(
                        kingdom,
                        PartySlotKey(PartyV2CustomNamePrefix, party.Slot),
                        cleaned
                    );
                    party.Name = cleaned;
                }

                UpdateRegionalPartySupport(kingdom, GetPoliticalParties(kingdom));
                SyncPartyMandateCache(kingdom, party);
                return party.Id ?? "";
            }

            public static bool RenameKingdomParty(Kingdom kingdom, string partyId, string newName)
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null || string.IsNullOrWhiteSpace(newName))
                {
                    return false;
                }

                string previousName = party.Name ?? "";
                string cleaned = newName.Trim();
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(PartyV2CustomNamePrefix, party.Slot),
                    cleaned
                );
                party.Name = cleaned;
                SyncPartyMandateCache(kingdom, party);

                if (previousName != cleaned)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.PartyRenamed,
                        kingdom,
                        previousName,
                        cleaned,
                        0,
                        0,
                        party.Id ?? ""
                    );
                }
                return true;
            }

            public static bool SetKingdomPartyRadicalism(Kingdom kingdom, string partyId, int radicalism)
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null)
                {
                    return false;
                }

                int previousRadicalism = ClampInt(party.Radicalism, 0, 100);
                int nextRadicalism = ClampInt(radicalism, 0, 100);

                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(PartyV2RadicalismPrefix, party.Slot),
                    nextRadicalism
                );
                party.Radicalism = nextRadicalism;

                if (previousRadicalism != nextRadicalism)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.PartyRadicalismChanged,
                        kingdom,
                        party.Ideology ?? "",
                        party.Ideology ?? "",
                        previousRadicalism,
                        nextRadicalism,
                        party.Id ?? ""
                    );
                }
                return true;
            }

            public static bool SetKingdomPartySupport(Kingdom kingdom, string partyId, int support)
            {
                if (kingdom == null)
                {
                    return false;
                }

                List<PoliticalParty> parties = GetPoliticalParties(kingdom);
                PoliticalParty target = null;
                for (int i = 0; i < parties.Count; i++)
                {
                    if (parties[i] != null && parties[i].Id == partyId)
                    {
                        target = parties[i];
                        break;
                    }
                }
                if (target == null)
                {
                    return false;
                }

                int previousSupport = ClampInt(target.Support, 0, 100);
                int targetSupport = ClampInt(support, 0, 100);
                List<City> cities = GetCitiesSafe(kingdom);
                for (int c = 0; c < cities.Count; c++)
                {
                    City city = cities[c];
                    if (city == null)
                    {
                        continue;
                    }

                    List<PoliticalParty> others = new List<PoliticalParty>();
                    List<int> otherSupport = new List<int>();
                    int otherTotal = 0;

                    for (int p = 0; p < parties.Count; p++)
                    {
                        PoliticalParty party = parties[p];
                        if (party == null || !party.Active || party.Id == target.Id)
                        {
                            continue;
                        }

                        int current;
                        bool initialized;
                        GetLocalPartySupport(city, party, out current, out initialized);
                        if (!initialized)
                        {
                            Dictionary<string, int> targets = CalculateCityPartyTargets(
                                kingdom,
                                city,
                                parties,
                                party.Ideology
                            );
                            targets.TryGetValue(party.Id, out current);
                        }

                        current = ClampInt(current, 0, 100);
                        others.Add(party);
                        otherSupport.Add(current);
                        otherTotal += current;
                    }

                    if (others.Count == 0)
                    {
                        SetLocalPartySupport(city, target, 100);
                        continue;
                    }

                    SetLocalPartySupport(city, target, targetSupport);
                    int remaining = 100 - targetSupport;
                    int assigned = 0;

                    for (int i = 0; i < others.Count; i++)
                    {
                        int value;
                        if (otherTotal <= 0)
                        {
                            value = remaining / others.Count;
                        }
                        else
                        {
                            value = (int)Math.Floor(
                                remaining * (otherSupport[i] / (double)otherTotal)
                            );
                        }

                        value = ClampInt(value, 0, remaining);
                        assigned += value;
                        SetLocalPartySupport(city, others[i], value);
                    }

                    int leftover = Math.Max(0, remaining - assigned);
                    if (leftover > 0)
                    {
                        PoliticalParty receiver = others[0];
                        int receiverCurrent;
                        bool receiverInitialized;
                        GetLocalPartySupport(city, receiver, out receiverCurrent, out receiverInitialized);
                        SetLocalPartySupport(
                            city,
                            receiver,
                            ClampInt(receiverCurrent + leftover, 0, 100)
                        );
                    }
                }

                AggregatePartySupportFromCities(kingdom, parties);
                SyncLegacyPartyCompatibility(kingdom, parties);
                SyncPartyMandateCache(kingdom, target);

                int newSupport = ClampInt(target.Support, 0, 100);
                if (previousSupport != newSupport)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.PartySupportChanged,
                        kingdom,
                        target.Ideology ?? "",
                        target.Ideology ?? "",
                        previousSupport,
                        newSupport,
                        target.Id ?? ""
                    );
                }
                return true;
            }

            public static bool SetKingdomPartyIdeology(
                Kingdom kingdom,
                string partyId,
                string ideologyId
            )
            {
                if (
                    kingdom == null ||
                    !IsValidIdeology(ideologyId)
                )
                {
                    return false;
                }

                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null)
                {
                    return false;
                }

                string previousIdeology = party.Ideology ?? "";
                if (previousIdeology == ideologyId)
                {
                    return true;
                }

                if (party.Active)
                {
                    List<PoliticalParty> active = GetPoliticalParties(kingdom);
                    int sameIdeology = 0;
                    for (int i = 0; i < active.Count; i++)
                    {
                        PoliticalParty candidate = active[i];
                        if (
                            candidate != null &&
                            candidate.Id != party.Id &&
                            candidate.Ideology == ideologyId
                        )
                        {
                            sameIdeology++;
                        }
                    }
                    if (sameIdeology >= MaxPoliticalPartiesPerIdeology)
                    {
                        return false;
                    }
                }

                string previousName = party.Name ?? "";
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(PartyV2IdeologyPrefix, party.Slot),
                    ideologyId
                );
                party.Ideology = ideologyId;

                string customName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(PartyV2CustomNamePrefix, party.Slot),
                    ""
                );
                if (string.IsNullOrWhiteSpace(customName))
                {
                    party.Name = GetPartyLocalizedName(
                        ideologyId,
                        party.NameVariant
                    );
                }

                Actor currentLeader = FindPartyActorByIdentity(
                    kingdom,
                    party.LeaderIdentity,
                    party.LeaderName
                );
                if (
                    currentLeader == null ||
                    GetCitizenIdeology(currentLeader) != ideologyId
                )
                {
                    Actor replacement = FindPartyLeaderActor(
                        kingdom,
                        ideologyId,
                        GetReservedPartyLeaderIdentities(kingdom, party.Id)
                    );
                    ApplyPartyLeader(
                        kingdom,
                        party,
                        replacement,
                        replacement != null
                    );
                }

                List<PoliticalParty> activeAfter = GetPoliticalParties(kingdom);
                UpdateRegionalPartySupport(kingdom, activeAfter);
                SyncLegacyPartyCompatibility(kingdom, activeAfter);
                SyncPartyMandateCache(kingdom, party);

                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.PartyIdeologyChanged,
                    kingdom,
                    previousIdeology,
                    ideologyId,
                    0,
                    0,
                    party.Id ?? ""
                );

                if (previousName != (party.Name ?? ""))
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.PartyRenamed,
                        kingdom,
                        previousName,
                        party.Name ?? "",
                        0,
                        0,
                        party.Id ?? ""
                    );
                }

                return true;
            }

            public static bool SetKingdomPartyActive(
                Kingdom kingdom,
                string partyId,
                bool active
            )
            {
                if (kingdom == null)
                {
                    return false;
                }

                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null)
                {
                    return false;
                }
                if (party.Active == active)
                {
                    return true;
                }

                if (!active)
                {
                    PoliticalParty activeParty = party;
                    List<PoliticalParty> before =
                        GetPoliticalParties(kingdom);
                    for (int i = 0; i < before.Count; i++)
                    {
                        if (
                            before[i] != null &&
                            before[i].Id == party.Id
                        )
                        {
                            activeParty = before[i];
                            break;
                        }
                    }

                    DeactivatePoliticalParty(kingdom, activeParty);
                    List<PoliticalParty> remaining = GetPoliticalParties(kingdom);
                    UpdateRegionalPartySupport(kingdom, remaining);
                    SyncLegacyPartyCompatibility(kingdom, remaining);
                    ReconcileRulingPartyAfterPartyMutation(kingdom);
                    return true;
                }

                List<PoliticalParty> activeParties = GetPoliticalParties(kingdom);
                if (activeParties.Count >= MaxPoliticalParties)
                {
                    return false;
                }

                int sameIdeology = 0;
                for (int i = 0; i < activeParties.Count; i++)
                {
                    PoliticalParty candidate = activeParties[i];
                    if (
                        candidate != null &&
                        candidate.Ideology == party.Ideology
                    )
                    {
                        sameIdeology++;
                    }
                }
                if (sameIdeology >= MaxPoliticalPartiesPerIdeology)
                {
                    return false;
                }

                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(PartyV2ActivePrefix, party.Slot),
                    1
                );
                party.Active = true;

                List<PoliticalParty> reloaded = GetPoliticalParties(kingdom);
                UpdateRegionalPartySupport(kingdom, reloaded);
                SyncLegacyPartyCompatibility(kingdom, reloaded);

                int activatedSupport = 0;
                for (int i = 0; i < reloaded.Count; i++)
                {
                    if (
                        reloaded[i] != null &&
                        reloaded[i].Id == party.Id
                    )
                    {
                        activatedSupport = ClampInt(
                            reloaded[i].Support,
                            0,
                            100
                        );
                        break;
                    }
                }

                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.PartyActivated,
                    kingdom,
                    "",
                    party.Ideology ?? "",
                    0,
                    activatedSupport,
                    party.Id ?? ""
                );

                ReconcileRulingPartyAfterPartyMutation(kingdom);
                return true;
            }

            public static bool SetKingdomPartyLeader(
                Kingdom kingdom,
                string partyId,
                Actor actor
            )
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (
                    party == null ||
                    actor == null ||
                    !actor.isAlive() ||
                    !IsActorResidentOfKingdom(kingdom, actor) ||
                    GetCitizenIdeology(actor) != party.Ideology
                )
                {
                    return false;
                }

                string identity = GetStableObjectIdentity(actor);
                HashSet<string> reserved =
                    GetReservedPartyLeaderIdentities(kingdom, party.Id);
                if (
                    !string.IsNullOrEmpty(identity) &&
                    reserved.Contains(identity)
                )
                {
                    return false;
                }

                return ApplyPartyLeader(
                    kingdom,
                    party,
                    actor,
                    true
                );
            }

            public static bool AssignBestKingdomPartyLeader(
                Kingdom kingdom,
                string partyId
            )
            {
                PoliticalParty party = FindPartyById(kingdom, partyId);
                if (party == null)
                {
                    return false;
                }

                Actor replacement = FindPartyLeaderActor(
                    kingdom,
                    party.Ideology,
                    GetReservedPartyLeaderIdentities(kingdom, party.Id)
                );
                if (replacement == null)
                {
                    return false;
                }

                return ApplyPartyLeader(
                    kingdom,
                    party,
                    replacement,
                    true
                );
            }

            public static string CheckSetKingdomRulingParty(
                Kingdom kingdom,
                string partyId
            )
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return "invalid-kingdom";
                }
                if (!KingdomSupportsPartyMandate(kingdom))
                {
                    return "party-mandate-not-supported";
                }
                if (string.IsNullOrWhiteSpace(partyId))
                {
                    return "party-id-required";
                }

                PoliticalParty party = FindPartyById(kingdom, partyId.Trim());
                if (party == null)
                {
                    return "party-not-found";
                }
                if (!party.Active)
                {
                    return "party-inactive";
                }
                return "ok";
            }

            public static bool SetKingdomRulingParty(
                Kingdom kingdom,
                string partyId
            )
            {
                if (CheckSetKingdomRulingParty(kingdom, partyId) != "ok")
                {
                    return false;
                }

                PoliticalParty party = FindPartyById(kingdom, partyId.Trim());
                if (party == null || !party.Active)
                {
                    return false;
                }

                SetRulingPartyFromElection(kingdom, party);
                return true;
            }

            public static bool ClearKingdomRulingParty(Kingdom kingdom)
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return false;
                }

                ClearRulingParty(kingdom);
                return true;
            }

            public static bool PublishKingdomEvent(
                Kingdom kingdom,
                string text,
                string eventId,
                float cooldownSeconds
            )
            {
                if (kingdom == null || string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                PublishPoliticalEvent(
                    text,
                    kingdom,
                    null,
                    GetLivingRuler(kingdom),
                    OverviewIconPath,
                    string.IsNullOrEmpty(eventId) ? "addon_event" : eventId,
                    Math.Max(1f, cooldownSeconds)
                );
                return true;
            }

            /// <summary>
            /// Registers a zero-tick kingdom action. Scenario Tools can enumerate these
            /// actions only while its window is open. Future content addons can use the
            /// same registry to expose presets/events without Scenario Tools referencing
            /// their assemblies directly.
            /// </summary>
            public static bool RegisterKingdomAction(
                string id,
                string category,
                string displayName,
                string description,
                string source,
                KingdomActionCondition condition,
                KingdomActionHandler handler
            )
            {
                return RegisterKingdomActionEx(
                    id,
                    category,
                    "",
                    "",
                    displayName,
                    description,
                    source,
                    "",
                    0,
                    condition,
                    handler
                );
            }

            public static bool RegisterKingdomActionEx(
                string id,
                string category,
                string nameKey,
                string descriptionKey,
                string displayName,
                string description,
                string source,
                string icon,
                int sortOrder,
                KingdomActionCondition condition,
                KingdomActionHandler handler
            )
            {
                if (string.IsNullOrWhiteSpace(id) || handler == null)
                {
                    return false;
                }

                string normalized = id.Trim();
                KingdomActions[normalized] = new KingdomActionRegistration()
                {
                    Id = normalized,
                    Category = string.IsNullOrWhiteSpace(category) ? "general" : category.Trim(),
                    NameKey = nameKey == null ? "" : nameKey.Trim(),
                    DescriptionKey = descriptionKey == null ? "" : descriptionKey.Trim(),
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
                    Description = description == null ? "" : description.Trim(),
                    Source = string.IsNullOrWhiteSpace(source) ? "addon" : source.Trim(),
                    Icon = icon == null ? "" : icon.Trim(),
                    SortOrder = sortOrder,
                    Condition = condition,
                    Handler = handler
                };
                return true;
            }

            public static bool UnregisterKingdomAction(string id)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return false;
                }
                return KingdomActions.Remove(id.Trim());
            }

            public static KingdomActionInfo GetKingdomAction(
                string id,
                Kingdom kingdom
            )
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return null;
                }
                KingdomActionRegistration registration;
                if (!KingdomActions.TryGetValue(id.Trim(), out registration) || registration == null)
                {
                    return null;
                }
                return BuildKingdomActionInfo(registration, kingdom);
            }

            public static List<KingdomActionInfo> GetKingdomActionsBySource(
                string source,
                Kingdom kingdom
            )
            {
                string wanted = source == null ? "" : source.Trim();
                List<KingdomActionInfo> result = new List<KingdomActionInfo>();
                foreach (KeyValuePair<string, KingdomActionRegistration> pair in KingdomActions)
                {
                    KingdomActionRegistration registration = pair.Value;
                    if (
                        registration == null ||
                        !string.Equals(registration.Source ?? "", wanted, StringComparison.Ordinal)
                    )
                    {
                        continue;
                    }
                    result.Add(BuildKingdomActionInfo(registration, kingdom));
                }
                SortKingdomActionInfos(result);
                return result;
            }

            public static List<KingdomActionInfo> GetKingdomActions(Kingdom kingdom)
            {
                List<KingdomActionInfo> result = new List<KingdomActionInfo>();
                foreach (KeyValuePair<string, KingdomActionRegistration> pair in KingdomActions)
                {
                    KingdomActionRegistration registration = pair.Value;
                    if (registration == null)
                    {
                        continue;
                    }
                    result.Add(BuildKingdomActionInfo(registration, kingdom));
                }

                SortKingdomActionInfos(result);
                return result;
            }

            public static bool CanExecuteKingdomAction(string id, Kingdom kingdom)
            {
                if (string.IsNullOrWhiteSpace(id) || kingdom == null)
                {
                    return false;
                }

                KingdomActionRegistration registration;
                if (!KingdomActions.TryGetValue(id.Trim(), out registration) || registration == null)
                {
                    return false;
                }
                return EvaluateKingdomActionCondition(registration, kingdom);
            }

            public static bool ExecuteKingdomAction(string id, Kingdom kingdom)
            {
                if (string.IsNullOrWhiteSpace(id) || kingdom == null)
                {
                    return false;
                }

                KingdomActionRegistration registration;
                if (!KingdomActions.TryGetValue(id.Trim(), out registration) || registration == null)
                {
                    return false;
                }

                if (!EvaluateKingdomActionCondition(registration, kingdom))
                {
                    return false;
                }

                try
                {
                    registration.Handler(kingdom);
                    return true;
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "ScenarioBridge action failed for " +
                        registration.Id + ": " + exception.Message
                    );
                    return false;
                }
            }

            private static KingdomActionInfo BuildKingdomActionInfo(
                KingdomActionRegistration registration,
                Kingdom kingdom
            )
            {
                if (registration == null)
                {
                    return null;
                }
                return new KingdomActionInfo()
                {
                    Id = registration.Id,
                    Category = registration.Category,
                    NameKey = registration.NameKey,
                    DescriptionKey = registration.DescriptionKey,
                    DisplayName = ResolveActionLocale(registration.NameKey, registration.DisplayName),
                    Description = ResolveActionLocale(registration.DescriptionKey, registration.Description),
                    Source = registration.Source,
                    Icon = registration.Icon,
                    SortOrder = registration.SortOrder,
                    Enabled = kingdom != null && EvaluateKingdomActionCondition(registration, kingdom)
                };
            }

            private static bool EvaluateKingdomActionCondition(
                KingdomActionRegistration registration,
                Kingdom kingdom
            )
            {
                if (registration == null || kingdom == null)
                {
                    return false;
                }
                if (registration.Condition == null)
                {
                    return true;
                }
                try
                {
                    return registration.Condition(kingdom);
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "ScenarioBridge action condition failed for " +
                        registration.Id + ": " + exception.Message
                    );
                    return false;
                }
            }

            private static void SortKingdomActionInfos(List<KingdomActionInfo> result)
            {
                if (result == null)
                {
                    return;
                }
                result.Sort(delegate(KingdomActionInfo a, KingdomActionInfo b)
                {
                    int categoryCompare = string.CompareOrdinal(
                        a == null ? "" : (a.Category ?? ""),
                        b == null ? "" : (b.Category ?? "")
                    );
                    if (categoryCompare != 0)
                    {
                        return categoryCompare;
                    }
                    int orderCompare = (a == null ? 0 : a.SortOrder).CompareTo(
                        b == null ? 0 : b.SortOrder
                    );
                    if (orderCompare != 0)
                    {
                        return orderCompare;
                    }
                    return string.CompareOrdinal(
                        a == null ? "" : (a.DisplayName ?? ""),
                        b == null ? "" : (b.DisplayName ?? "")
                    );
                });
            }

            private static string ResolveActionLocale(string key, string fallback)
            {
                return PoliticalWorldAPI.ResolveLocalization(key, fallback ?? "");
            }

            private static PartyInfo BuildPartyInfo(
                Kingdom kingdom,
                PoliticalParty party,
                string rulingId
            )
            {
                if (party == null)
                {
                    return null;
                }

                return new PartyInfo()
                {
                    Id = party.Id ?? "",
                    Name = party.Name ?? "",
                    IdeologyId = party.Ideology ?? "",
                    IdeologyName = ResolveIdeologyDisplayName(
                        GetIdeologyNode(party.Ideology)
                    ),
                    LeaderIdentity = party.LeaderIdentity ?? "",
                    LeaderName = party.LeaderName ?? "",
                    FounderIdentity = party.FounderIdentity ?? "",
                    FounderName = party.FounderName ?? "",
                    FoundedYear = party.FoundedYear,
                    Support = ClampInt(party.Support, 0, 100),
                    Radicalism = ClampInt(party.Radicalism, 0, 100),
                    Active = party.Active,
                    IsRuling =
                        !string.IsNullOrEmpty(rulingId) &&
                        string.Equals(
                            rulingId,
                            party.Id ?? "",
                            StringComparison.Ordinal
                        ),
                    ColorSeed = party.ColorSeed,
                    Position = party.Position ?? "",
                    Strategy = party.Strategy ?? "",
                    ForeignStance = party.ForeignStance ?? "",
                    Traits = party.Traits == null
                        ? new string[0]
                        : party.Traits.ToArray(),
                    OriginCityId = party.OriginCityId ?? "",
                    OriginCityName = party.OriginCityName ?? "",
                    ParentPartyId = party.ParentPartyId ?? "",
                    ParentPartyName = party.ParentPartyName ?? ""
                };
            }

            private static string GetStoredRulingPartyId(Kingdom kingdom)
            {
                if (kingdom == null || kingdom.data == null)
                {
                    return "";
                }

                return GetKingdomStringData(
                    kingdom,
                    ElectionRulingPartyIdDataKey,
                    ""
                ) ?? "";
            }

            private static void SyncPartyMandateCache(
                Kingdom kingdom,
                PoliticalParty party
            )
            {
                if (
                    kingdom == null ||
                    party == null
                )
                {
                    return;
                }

                PoliticalParty current = party;
                if (party.Active)
                {
                    List<PoliticalParty> active =
                        GetPoliticalParties(kingdom);
                    for (int i = 0; i < active.Count; i++)
                    {
                        if (
                            active[i] != null &&
                            active[i].Id == party.Id
                        )
                        {
                            current = active[i];
                            break;
                        }
                    }
                }

                string rulingId = GetStoredRulingPartyId(kingdom);
                if (
                    current.Active &&
                    !string.IsNullOrEmpty(rulingId) &&
                    rulingId == current.Id
                )
                {
                    SetRulingPartyFromElection(kingdom, current);
                }

                string councilId = GetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyIdDataKey,
                    ""
                );
                if (
                    !string.IsNullOrEmpty(councilId) &&
                    councilId == current.Id
                )
                {
                    SetKingdomStringData(
                        kingdom,
                        CouncilDominantPartyNameDataKey,
                        current.Name ?? ""
                    );
                    SetKingdomStringData(
                        kingdom,
                        CouncilDominantPartyIdeologyDataKey,
                        current.Ideology ?? ""
                    );
                    SetKingdomIntData(
                        kingdom,
                        CouncilDominantPartySupportDataKey,
                        ClampInt(current.Support, 0, 100)
                    );
                }
            }

            private static bool KingdomSupportsPartyMandate(Kingdom kingdom)
            {
                if (kingdom == null)
                {
                    return false;
                }

                string form = GetGovernmentForm(kingdom);
                string system = GetPoliticalSystem(kingdom);
                return
                    GovernmentUsesCompetitiveElections(form) ||
                    form == GovernmentOnePartyStateId ||
                    system == PoliticalSystemOnePartyId ||
                    system == PoliticalSystemSovietOnePartyId;
            }

            private static void ReconcileRulingPartyAfterPartyMutation(
                Kingdom kingdom
            )
            {
                if (kingdom == null)
                {
                    return;
                }

                string rulingId = GetStoredRulingPartyId(kingdom);
                if (!string.IsNullOrEmpty(rulingId))
                {
                    PoliticalParty ruling = FindPartyById(
                        kingdom,
                        rulingId
                    );
                    if (ruling == null || !ruling.Active)
                    {
                        ClearRulingParty(kingdom);
                    }
                }

                string form = GetGovernmentForm(kingdom);
                string system = GetPoliticalSystem(kingdom);
                if (
                    string.IsNullOrEmpty(GetStoredRulingPartyId(kingdom)) &&
                    (
                        form == GovernmentOnePartyStateId ||
                        system == PoliticalSystemSovietOnePartyId
                    )
                )
                {
                    EnsureNonElectiveRulingParty(kingdom, form);
                }
            }

            private static bool IsActorResidentOfKingdom(
                Kingdom kingdom,
                Actor actor
            )
            {
                if (
                    kingdom == null ||
                    actor == null ||
                    !actor.isAlive()
                )
                {
                    return false;
                }

                string identity = GetStableObjectIdentity(actor);
                List<City> cities = GetCitiesSafe(kingdom);
                for (int c = 0; c < cities.Count; c++)
                {
                    List<Actor> units = GetCityUnitsSafe(cities[c]);
                    for (int i = 0; i < units.Count; i++)
                    {
                        Actor candidate = units[i];
                        if (candidate == null)
                        {
                            continue;
                        }
                        if (object.ReferenceEquals(candidate, actor))
                        {
                            return true;
                        }
                        if (
                            !string.IsNullOrEmpty(identity) &&
                            GetStableObjectIdentity(candidate) == identity
                        )
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static HashSet<string> GetReservedPartyLeaderIdentities(
                Kingdom kingdom,
                string exceptPartyId
            )
            {
                HashSet<string> result = new HashSet<string>();
                if (kingdom == null)
                {
                    return result;
                }

                List<PoliticalParty> parties =
                    LoadPoliticalPartiesInternal(kingdom, false);
                for (int i = 0; i < parties.Count; i++)
                {
                    PoliticalParty candidate = parties[i];
                    if (
                        candidate == null ||
                        candidate.Id == exceptPartyId ||
                        string.IsNullOrEmpty(candidate.LeaderIdentity)
                    )
                    {
                        continue;
                    }
                    result.Add(candidate.LeaderIdentity);
                }
                return result;
            }

            private static bool ApplyPartyLeader(
                Kingdom kingdom,
                PoliticalParty party,
                Actor actor,
                bool recordHistory
            )
            {
                if (
                    kingdom == null ||
                    party == null
                )
                {
                    return false;
                }

                string previousIdentity = party.LeaderIdentity ?? "";
                string previousName = party.LeaderName ?? "";
                string nextIdentity = actor == null
                    ? ""
                    : GetStableObjectIdentity(actor);
                string nextName = actor == null
                    ? ""
                    : GetWorldObjectDisplayName(actor);

                if (
                    previousIdentity == nextIdentity &&
                    previousName == nextName
                )
                {
                    party.LeaderActor = actor;
                    return true;
                }

                party.LeaderActor = actor;
                party.LeaderIdentity = nextIdentity ?? "";
                party.LeaderName = nextName ?? "";

                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderIdentityPrefix,
                        party.Slot
                    ),
                    party.LeaderIdentity
                );
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderNamePrefix,
                        party.Slot
                    ),
                    party.LeaderName
                );

                if (recordHistory)
                {
                    RecordPartyHistoryEvent(
                        kingdom,
                        party,
                        PartyHistoryLeaderChanged,
                        party.LeaderName,
                        previousName,
                        GetWorldYearSafe()
                    );
                }

                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.PartyLeaderChanged,
                    kingdom,
                    previousName,
                    party.LeaderName,
                    0,
                    0,
                    party.Id ?? ""
                );
                return true;
            }

            private static PoliticalParty FindPartyById(Kingdom kingdom, string partyId)
            {
                if (kingdom == null || string.IsNullOrEmpty(partyId))
                {
                    return null;
                }

                List<PoliticalParty> parties = LoadPoliticalPartiesInternal(kingdom, true);
                for (int i = 0; i < parties.Count; i++)
                {
                    if (parties[i] != null && parties[i].Id == partyId)
                    {
                        return parties[i];
                    }
                }
                return null;
            }

            private static string ResolveIdeologyDisplayName(IdeologyNode node)
            {
                if (node == null)
                {
                    return "";
                }

                string key = string.IsNullOrEmpty(node.NameKey)
                    ? node.Id
                    : node.NameKey;
                string fallback = GetAddonIdeologyMetadata(
                    AddonIdeologyDisplayNames,
                    node.Id
                );
                if (string.IsNullOrWhiteSpace(fallback))
                {
                    fallback = key;
                }
                return PoliticalWorldAPI.ResolveLocalization(key, fallback);
            }

            private static string ResolveIdeologyDescription(IdeologyNode node)
            {
                if (node == null) return "";
                string key = GetAddonIdeologyMetadata(
                    AddonIdeologyDescriptionKeys,
                    node.Id
                );
                string fallback = GetAddonIdeologyMetadata(
                    AddonIdeologyDescriptions,
                    node.Id
                );
                return PoliticalWorldAPI.ResolveLocalization(key, fallback);
            }

            private static string GetAddonIdeologyMetadata(
                Dictionary<string, string> source,
                string ideologyId
            )
            {
                if (source == null || string.IsNullOrEmpty(ideologyId))
                {
                    return "";
                }
                string value;
                return source.TryGetValue(ideologyId, out value)
                    ? (value ?? "")
                    : "";
            }

            private static int GetAddonIdeologySortOrder(string ideologyId)
            {
                if (string.IsNullOrEmpty(ideologyId)) return 0;
                int value;
                return AddonIdeologySortOrders.TryGetValue(ideologyId, out value)
                    ? value
                    : 0;
            }
        }
    }
}
