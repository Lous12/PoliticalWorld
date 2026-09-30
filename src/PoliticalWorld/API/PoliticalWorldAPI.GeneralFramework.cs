using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// General-purpose addon framework primitives introduced in API 1.10.
    ///
    /// These helpers are deliberately event-driven and object-scoped. They do
    /// not add an Update loop, global actor scan, or hidden first-party path.
    /// Actor/City data is stored through the vanilla object's own data container
    /// so it follows the same save lifecycle used by Political World's existing
    /// actor/city state.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public delegate bool ActorCondition(Actor actor);
        public delegate bool CityCondition(City city);
        public delegate void ActorAction(Actor actor);
        public delegate void CityAction(City city);

        private const string GeneralObjectDataPrefix = "pw_api10_data_";
        private const string PrivateTagsKey = "__private_tags";

        private static readonly Dictionary<string, GenericContentTypeInfo>
            GenericContentTypes = new Dictionary<string, GenericContentTypeInfo>(StringComparer.Ordinal);

        private static readonly Dictionary<string, GenericContentInfo>
            GenericContent = new Dictionary<string, GenericContentInfo>(StringComparer.Ordinal);

        private static readonly Dictionary<string, HashSet<string>>
            AddonCapabilities = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        private static readonly Dictionary<string, HashSet<string>>
            CapabilityProviders = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        public sealed class GenericContentTypeDefinition
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public string[] Tags;
        }

        public sealed class GenericContentTypeInfo
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public string Source;
            public string[] Tags;
        }

        public sealed class GenericContentDefinition
        {
            public string Id;
            public string TypeId;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public string[] Tags;
            public IDictionary<string, string> Metadata;
        }

        public sealed class GenericContentInfo
        {
            public string Id;
            public string TypeId;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public string Source;
            public string[] Tags;
            public Dictionary<string, string> Metadata;
        }

        /// <summary>
        /// Typed addon-owned data stored on Actor, City or Kingdom objects.
        /// Keys are collision-safe: addon id + local key are UTF-8 hex encoded.
        /// </summary>
        public static class Data
        {
            public static int GetInt(Actor actor, string addonId, string key, int fallback = 0)
            {
                if (!ValidateObjectDataRequest(actor, addonId, key)) return fallback;
                string dataKey = MakeGeneralObjectDataKey(addonId, key);
                int value = fallback;
                try { actor.data.get(dataKey, out value, fallback); }
                catch { value = fallback; }
                return value;
            }

            public static bool SetInt(Actor actor, string addonId, string key, int value)
            {
                if (!ValidateObjectDataRequest(actor, addonId, key)) return false;
                try { actor.data.set(MakeGeneralObjectDataKey(addonId, key), value); return true; }
                catch { return false; }
            }

            public static string GetString(Actor actor, string addonId, string key, string fallback = "")
            {
                if (!ValidateObjectDataRequest(actor, addonId, key)) return fallback ?? "";
                string value = fallback ?? "";
                try { actor.data.get(MakeGeneralObjectDataKey(addonId, key), out value, fallback ?? ""); }
                catch { value = fallback ?? ""; }
                return value ?? "";
            }

            public static bool SetString(Actor actor, string addonId, string key, string value)
            {
                if (!ValidateObjectDataRequest(actor, addonId, key)) return false;
                try { actor.data.set(MakeGeneralObjectDataKey(addonId, key), value ?? ""); return true; }
                catch { return false; }
            }

            public static bool GetBool(Actor actor, string addonId, string key, bool fallback = false)
            {
                return GetInt(actor, addonId, key, fallback ? 1 : 0) != 0;
            }

            public static bool SetBool(Actor actor, string addonId, string key, bool value)
            {
                return SetInt(actor, addonId, key, value ? 1 : 0);
            }

            public static float GetFloat(Actor actor, string addonId, string key, float fallback = 0f)
            {
                string raw = GetString(actor, addonId, key, "");
                float value;
                return !string.IsNullOrEmpty(raw) &&
                    float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    ? value
                    : fallback;
            }

            public static bool SetFloat(Actor actor, string addonId, string key, float value)
            {
                return SetString(actor, addonId, key, value.ToString("R", CultureInfo.InvariantCulture));
            }

            public static int GetInt(City city, string addonId, string key, int fallback = 0)
            {
                if (!ValidateObjectDataRequest(city, addonId, key)) return fallback;
                string dataKey = MakeGeneralObjectDataKey(addonId, key);
                int value = fallback;
                try { city.data.get(dataKey, out value, fallback); }
                catch { value = fallback; }
                return value;
            }

            public static bool SetInt(City city, string addonId, string key, int value)
            {
                if (!ValidateObjectDataRequest(city, addonId, key)) return false;
                try { city.data.set(MakeGeneralObjectDataKey(addonId, key), value); return true; }
                catch { return false; }
            }

            public static string GetString(City city, string addonId, string key, string fallback = "")
            {
                if (!ValidateObjectDataRequest(city, addonId, key)) return fallback ?? "";
                string value = fallback ?? "";
                try { city.data.get(MakeGeneralObjectDataKey(addonId, key), out value, fallback ?? ""); }
                catch { value = fallback ?? ""; }
                return value ?? "";
            }

            public static bool SetString(City city, string addonId, string key, string value)
            {
                if (!ValidateObjectDataRequest(city, addonId, key)) return false;
                try { city.data.set(MakeGeneralObjectDataKey(addonId, key), value ?? ""); return true; }
                catch { return false; }
            }

            public static bool GetBool(City city, string addonId, string key, bool fallback = false)
            {
                return GetInt(city, addonId, key, fallback ? 1 : 0) != 0;
            }

            public static bool SetBool(City city, string addonId, string key, bool value)
            {
                return SetInt(city, addonId, key, value ? 1 : 0);
            }

            public static float GetFloat(City city, string addonId, string key, float fallback = 0f)
            {
                string raw = GetString(city, addonId, key, "");
                float value;
                return !string.IsNullOrEmpty(raw) &&
                    float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    ? value
                    : fallback;
            }

            public static bool SetFloat(City city, string addonId, string key, float value)
            {
                return SetString(city, addonId, key, value.ToString("R", CultureInfo.InvariantCulture));
            }

            public static int GetInt(Kingdom kingdom, string addonId, string key, int fallback = 0)
            {
                return PoliticalWorldAPI.GetKingdomInt(kingdom, addonId, key, fallback);
            }

            public static bool SetInt(Kingdom kingdom, string addonId, string key, int value)
            {
                return PoliticalWorldAPI.SetKingdomInt(kingdom, addonId, key, value);
            }

            public static string GetString(Kingdom kingdom, string addonId, string key, string fallback = "")
            {
                return PoliticalWorldAPI.GetKingdomString(kingdom, addonId, key, fallback ?? "");
            }

            public static bool SetString(Kingdom kingdom, string addonId, string key, string value)
            {
                return PoliticalWorldAPI.SetKingdomString(kingdom, addonId, key, value ?? "");
            }

            public static bool GetBool(Kingdom kingdom, string addonId, string key, bool fallback = false)
            {
                return PoliticalWorldAPI.GetKingdomBool(kingdom, addonId, key, fallback);
            }

            public static bool SetBool(Kingdom kingdom, string addonId, string key, bool value)
            {
                return PoliticalWorldAPI.SetKingdomBool(kingdom, addonId, key, value);
            }

            public static float GetFloat(Kingdom kingdom, string addonId, string key, float fallback = 0f)
            {
                return PoliticalWorldAPI.GetKingdomFloat(kingdom, addonId, key, fallback);
            }

            public static bool SetFloat(Kingdom kingdom, string addonId, string key, float value)
            {
                return PoliticalWorldAPI.SetKingdomFloat(kingdom, addonId, key, value);
            }
        }

        /// <summary>
        /// Addon-private tags on Actor, City and Kingdom objects.
        /// Tags are local to an addon and never collide with another addon.
        /// </summary>
        public static class Tags
        {
            public static List<string> Get(Actor actor, string addonId)
            {
                return ParseTags(Data.GetString(actor, addonId, PrivateTagsKey, ""));
            }

            public static bool Has(Actor actor, string addonId, string localTag)
            {
                return ContainsTag(Get(actor, addonId), localTag);
            }

            public static bool Add(Actor actor, string addonId, string localTag)
            {
                if (!IsSafeLocalTag(localTag) || actor == null) return false;
                List<string> tags = Get(actor, addonId);
                string wanted = localTag.Trim();
                if (ContainsTag(tags, wanted)) return true;
                tags.Add(wanted);
                return Data.SetString(actor, addonId, PrivateTagsKey, string.Join("|", tags.ToArray()));
            }

            public static bool Remove(Actor actor, string addonId, string localTag)
            {
                if (!IsSafeLocalTag(localTag) || actor == null) return false;
                List<string> tags = Get(actor, addonId);
                bool removed = RemoveTag(tags, localTag.Trim());
                return removed && Data.SetString(actor, addonId, PrivateTagsKey, string.Join("|", tags.ToArray()));
            }

            public static List<string> Get(City city, string addonId)
            {
                return ParseTags(Data.GetString(city, addonId, PrivateTagsKey, ""));
            }

            public static bool Has(City city, string addonId, string localTag)
            {
                return ContainsTag(Get(city, addonId), localTag);
            }

            public static bool Add(City city, string addonId, string localTag)
            {
                if (!IsSafeLocalTag(localTag) || city == null) return false;
                List<string> tags = Get(city, addonId);
                string wanted = localTag.Trim();
                if (ContainsTag(tags, wanted)) return true;
                tags.Add(wanted);
                return Data.SetString(city, addonId, PrivateTagsKey, string.Join("|", tags.ToArray()));
            }

            public static bool Remove(City city, string addonId, string localTag)
            {
                if (!IsSafeLocalTag(localTag) || city == null) return false;
                List<string> tags = Get(city, addonId);
                bool removed = RemoveTag(tags, localTag.Trim());
                return removed && Data.SetString(city, addonId, PrivateTagsKey, string.Join("|", tags.ToArray()));
            }

            public static List<string> Get(Kingdom kingdom, string addonId)
            {
                return PoliticalWorldAPI.GetAddonKingdomTags(kingdom, addonId);
            }

            public static bool Has(Kingdom kingdom, string addonId, string localTag)
            {
                return PoliticalWorldAPI.HasAddonKingdomTag(kingdom, addonId, localTag);
            }

            public static bool Add(Kingdom kingdom, string addonId, string localTag)
            {
                return PoliticalWorldAPI.AddAddonKingdomTag(kingdom, addonId, localTag);
            }

            public static bool Remove(Kingdom kingdom, string addonId, string localTag)
            {
                return PoliticalWorldAPI.RemoveAddonKingdomTag(kingdom, addonId, localTag);
            }
        }

        /// <summary>
        /// Object-scoped reusable Conditions. These are simple delegates and
        /// create no polling. Addons decide when to evaluate them.
        /// </summary>
        public static class WorldConditions
        {
            public static ActorCondition ActorAll(params ActorCondition[] conditions)
            {
                return delegate(Actor actor)
                {
                    if (conditions == null) return true;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && !conditions[i](actor)) return false;
                    }
                    return true;
                };
            }

            public static ActorCondition ActorAny(params ActorCondition[] conditions)
            {
                return delegate(Actor actor)
                {
                    if (conditions == null || conditions.Length == 0) return false;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && conditions[i](actor)) return true;
                    }
                    return false;
                };
            }

            public static ActorCondition ActorNot(ActorCondition condition)
            {
                return delegate(Actor actor) { return condition == null || !condition(actor); };
            }

            public static ActorCondition ActorHasTrait(string traitId)
            {
                string wanted = traitId == null ? "" : traitId.Trim();
                return delegate(Actor actor)
                {
                    if (actor == null || string.IsNullOrEmpty(wanted)) return false;
                    try { return actor.hasTrait(wanted); }
                    catch { return false; }
                };
            }

            public static ActorCondition ActorHasTag(string addonId, string localTag)
            {
                return delegate(Actor actor) { return Tags.Has(actor, addonId, localTag); };
            }

            public static ActorCondition ActorIntAtLeast(string addonId, string key, int value)
            {
                return delegate(Actor actor) { return Data.GetInt(actor, addonId, key, int.MinValue) >= value; };
            }

            public static ActorCondition ActorIntAtMost(string addonId, string key, int value)
            {
                return delegate(Actor actor) { return Data.GetInt(actor, addonId, key, int.MaxValue) <= value; };
            }

            public static ActorCondition ActorBoolIs(string addonId, string key, bool value)
            {
                return delegate(Actor actor) { return Data.GetBool(actor, addonId, key, !value) == value; };
            }

            public static ActorCondition ActorStringIs(string addonId, string key, string value)
            {
                string wanted = value ?? "";
                return delegate(Actor actor)
                {
                    return string.Equals(Data.GetString(actor, addonId, key, "\u0001PW_MISSING\u0001"), wanted, StringComparison.Ordinal);
                };
            }

            public static ActorCondition CustomActor(ActorCondition condition)
            {
                return condition;
            }

            public static CityCondition CityAll(params CityCondition[] conditions)
            {
                return delegate(City city)
                {
                    if (conditions == null) return true;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && !conditions[i](city)) return false;
                    }
                    return true;
                };
            }

            public static CityCondition CityAny(params CityCondition[] conditions)
            {
                return delegate(City city)
                {
                    if (conditions == null || conditions.Length == 0) return false;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && conditions[i](city)) return true;
                    }
                    return false;
                };
            }

            public static CityCondition CityNot(CityCondition condition)
            {
                return delegate(City city) { return condition == null || !condition(city); };
            }

            public static CityCondition CityHasTag(string addonId, string localTag)
            {
                return delegate(City city) { return Tags.Has(city, addonId, localTag); };
            }

            public static CityCondition CityIntAtLeast(string addonId, string key, int value)
            {
                return delegate(City city) { return Data.GetInt(city, addonId, key, int.MinValue) >= value; };
            }

            public static CityCondition CityIntAtMost(string addonId, string key, int value)
            {
                return delegate(City city) { return Data.GetInt(city, addonId, key, int.MaxValue) <= value; };
            }

            public static CityCondition CityBoolIs(string addonId, string key, bool value)
            {
                return delegate(City city) { return Data.GetBool(city, addonId, key, !value) == value; };
            }

            public static CityCondition CityStringIs(string addonId, string key, string value)
            {
                string wanted = value ?? "";
                return delegate(City city)
                {
                    return string.Equals(Data.GetString(city, addonId, key, "\u0001PW_MISSING\u0001"), wanted, StringComparison.Ordinal);
                };
            }

            public static CityCondition CustomCity(CityCondition condition)
            {
                return condition;
            }
        }

        /// <summary>
        /// Object-scoped effect builders for Actor/City data and tags.
        /// </summary>
        public static class WorldEffects
        {
            public static ActorAction ActorSequence(params ActorAction[] effects)
            {
                return delegate(Actor actor)
                {
                    if (effects == null) return;
                    for (int i = 0; i < effects.Length; i++) if (effects[i] != null) effects[i](actor);
                };
            }

            public static ActorAction ActorAddTag(string addonId, string localTag)
            {
                return delegate(Actor actor) { Tags.Add(actor, addonId, localTag); };
            }

            public static ActorAction ActorRemoveTag(string addonId, string localTag)
            {
                return delegate(Actor actor) { Tags.Remove(actor, addonId, localTag); };
            }

            public static ActorAction ActorSetInt(string addonId, string key, int value)
            {
                return delegate(Actor actor) { Data.SetInt(actor, addonId, key, value); };
            }

            public static ActorAction ActorChangeInt(string addonId, string key, int delta)
            {
                return delegate(Actor actor) { Data.SetInt(actor, addonId, key, Data.GetInt(actor, addonId, key, 0) + delta); };
            }

            public static ActorAction ActorSetBool(string addonId, string key, bool value)
            {
                return delegate(Actor actor) { Data.SetBool(actor, addonId, key, value); };
            }

            public static ActorAction ActorSetString(string addonId, string key, string value)
            {
                return delegate(Actor actor) { Data.SetString(actor, addonId, key, value); };
            }

            public static ActorAction ActorSetFloat(string addonId, string key, float value)
            {
                return delegate(Actor actor) { Data.SetFloat(actor, addonId, key, value); };
            }

            public static ActorAction CustomActor(ActorAction action)
            {
                return action;
            }

            public static CityAction CitySequence(params CityAction[] effects)
            {
                return delegate(City city)
                {
                    if (effects == null) return;
                    for (int i = 0; i < effects.Length; i++) if (effects[i] != null) effects[i](city);
                };
            }

            public static CityAction CityAddTag(string addonId, string localTag)
            {
                return delegate(City city) { Tags.Add(city, addonId, localTag); };
            }

            public static CityAction CityRemoveTag(string addonId, string localTag)
            {
                return delegate(City city) { Tags.Remove(city, addonId, localTag); };
            }

            public static CityAction CitySetInt(string addonId, string key, int value)
            {
                return delegate(City city) { Data.SetInt(city, addonId, key, value); };
            }

            public static CityAction CityChangeInt(string addonId, string key, int delta)
            {
                return delegate(City city) { Data.SetInt(city, addonId, key, Data.GetInt(city, addonId, key, 0) + delta); };
            }

            public static CityAction CitySetBool(string addonId, string key, bool value)
            {
                return delegate(City city) { Data.SetBool(city, addonId, key, value); };
            }

            public static CityAction CitySetString(string addonId, string key, string value)
            {
                return delegate(City city) { Data.SetString(city, addonId, key, value); };
            }

            public static CityAction CitySetFloat(string addonId, string key, float value)
            {
                return delegate(City city) { Data.SetFloat(city, addonId, key, value); };
            }

            public static CityAction CustomCity(CityAction action)
            {
                return action;
            }
        }

        /// <summary>
        /// Generic addon-owned content. A content type can represent anything:
        /// spell, religion, profession, technology, disease, resource, etc.
        /// Political World does not need a dedicated RegisterX method for each.
        /// </summary>
        public static class Content
        {
            public static bool RegisterType(string addonId, GenericContentTypeDefinition definition)
            {
                string owner = addonId == null ? "" : addonId.Trim();
                if (!IsAddonRegistered(owner) || definition == null) return false;
                string id = definition.Id == null ? "" : definition.Id.Trim();
                if (!IsOwnedContentId(owner, id)) return false;
                if (GenericContentTypes.ContainsKey(id))
                {
                    InternalRecordFrameworkIssue(owner, "WARN", "PWEC210", "Generic content type id is already registered: " + id);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(definition.NameKey) && !string.IsNullOrWhiteSpace(definition.DisplayName))
                    InternalSeedLocalizationFallback(owner, definition.NameKey, definition.DisplayName);
                if (!string.IsNullOrWhiteSpace(definition.DescriptionKey) && !string.IsNullOrWhiteSpace(definition.Description))
                    InternalSeedLocalizationFallback(owner, definition.DescriptionKey, definition.Description);

                GenericContentTypes[id] = new GenericContentTypeInfo()
                {
                    Id = id,
                    NameKey = Trim(definition.NameKey),
                    DisplayName = Trim(definition.DisplayName),
                    DescriptionKey = Trim(definition.DescriptionKey),
                    Description = Trim(definition.Description),
                    Icon = Trim(definition.Icon),
                    SortOrder = definition.SortOrder,
                    Source = owner,
                    Tags = NormalizeTags(definition.Tags)
                };
                return true;
            }

            public static bool Register(string addonId, GenericContentDefinition definition)
            {
                string owner = addonId == null ? "" : addonId.Trim();
                if (!IsAddonRegistered(owner) || definition == null) return false;
                string id = Trim(definition.Id);
                string typeId = Trim(definition.TypeId);
                if (!IsOwnedContentId(owner, id) || !GenericContentTypes.ContainsKey(typeId)) return false;
                if (GenericContent.ContainsKey(id))
                {
                    InternalRecordFrameworkIssue(owner, "WARN", "PWEC211", "Generic content id is already registered: " + id);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(definition.NameKey) && !string.IsNullOrWhiteSpace(definition.DisplayName))
                    InternalSeedLocalizationFallback(owner, definition.NameKey, definition.DisplayName);
                if (!string.IsNullOrWhiteSpace(definition.DescriptionKey) && !string.IsNullOrWhiteSpace(definition.Description))
                    InternalSeedLocalizationFallback(owner, definition.DescriptionKey, definition.Description);

                GenericContent[id] = new GenericContentInfo()
                {
                    Id = id,
                    TypeId = typeId,
                    NameKey = Trim(definition.NameKey),
                    DisplayName = Trim(definition.DisplayName),
                    DescriptionKey = Trim(definition.DescriptionKey),
                    Description = Trim(definition.Description),
                    Icon = Trim(definition.Icon),
                    SortOrder = definition.SortOrder,
                    Source = owner,
                    Tags = NormalizeTags(definition.Tags),
                    Metadata = CloneMetadata(definition.Metadata)
                };
                return true;
            }

            public static GenericContentTypeInfo GetType(string typeId)
            {
                GenericContentTypeInfo info;
                return GenericContentTypes.TryGetValue(Trim(typeId), out info) && info != null ? CloneType(info) : null;
            }

            public static List<GenericContentTypeInfo> GetTypes()
            {
                List<GenericContentTypeInfo> result = new List<GenericContentTypeInfo>();
                foreach (GenericContentTypeInfo info in GenericContentTypes.Values) if (info != null) result.Add(CloneType(info));
                result.Sort(delegate(GenericContentTypeInfo a, GenericContentTypeInfo b)
                {
                    int order = a.SortOrder.CompareTo(b.SortOrder);
                    return order != 0 ? order : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
                });
                return result;
            }

            public static GenericContentInfo Get(string contentId)
            {
                GenericContentInfo info;
                return GenericContent.TryGetValue(Trim(contentId), out info) && info != null ? CloneContent(info) : null;
            }

            public static List<GenericContentInfo> GetByType(string typeId)
            {
                string wanted = Trim(typeId);
                return GetFiltered(delegate(GenericContentInfo info) { return string.Equals(info.TypeId, wanted, StringComparison.Ordinal); });
            }

            public static List<GenericContentInfo> GetByAddon(string addonId)
            {
                string wanted = Trim(addonId);
                return GetFiltered(delegate(GenericContentInfo info) { return string.Equals(info.Source, wanted, StringComparison.Ordinal); });
            }

            public static List<GenericContentInfo> GetByTag(string tag)
            {
                string wanted = Trim(tag);
                return GetFiltered(delegate(GenericContentInfo info) { return ContainsTagArray(info.Tags, wanted); });
            }

            public static bool Unregister(string addonId, string contentId)
            {
                GenericContentInfo info;
                string id = Trim(contentId);
                if (!GenericContent.TryGetValue(id, out info) || info == null || !string.Equals(info.Source, Trim(addonId), StringComparison.Ordinal)) return false;
                return GenericContent.Remove(id);
            }

            public static bool UnregisterType(string addonId, string typeId)
            {
                string id = Trim(typeId);
                GenericContentTypeInfo info;
                if (!GenericContentTypes.TryGetValue(id, out info) || info == null || !string.Equals(info.Source, Trim(addonId), StringComparison.Ordinal)) return false;
                foreach (GenericContentInfo item in GenericContent.Values)
                {
                    if (item != null && string.Equals(item.TypeId, id, StringComparison.Ordinal)) return false;
                }
                return GenericContentTypes.Remove(id);
            }
        }

        /// <summary>
        /// Cross-addon feature discovery. Capabilities are intentionally not
        /// exclusive: several addons may provide the same semantic capability.
        /// </summary>
        public static class AddonFeatures
        {
            public static bool Register(string addonId, string capability)
            {
                string owner = Trim(addonId);
                string cap = Trim(capability);
                if (!IsAddonRegistered(owner) || !IsSafeCapabilityId(cap)) return false;

                HashSet<string> addonSet;
                if (!AddonCapabilities.TryGetValue(owner, out addonSet))
                {
                    addonSet = new HashSet<string>(StringComparer.Ordinal);
                    AddonCapabilities[owner] = addonSet;
                }
                bool added = addonSet.Add(cap);

                HashSet<string> providers;
                if (!CapabilityProviders.TryGetValue(cap, out providers))
                {
                    providers = new HashSet<string>(StringComparer.Ordinal);
                    CapabilityProviders[cap] = providers;
                }
                providers.Add(owner);
                return added;
            }

            public static bool Unregister(string addonId, string capability)
            {
                string owner = Trim(addonId);
                string cap = Trim(capability);
                HashSet<string> addonSet;
                if (!AddonCapabilities.TryGetValue(owner, out addonSet) || !addonSet.Remove(cap)) return false;
                if (addonSet.Count == 0) AddonCapabilities.Remove(owner);

                HashSet<string> providers;
                if (CapabilityProviders.TryGetValue(cap, out providers))
                {
                    providers.Remove(owner);
                    if (providers.Count == 0) CapabilityProviders.Remove(cap);
                }
                return true;
            }

            public static bool Has(string addonId, string capability)
            {
                HashSet<string> set;
                return AddonCapabilities.TryGetValue(Trim(addonId), out set) && set != null && set.Contains(Trim(capability));
            }

            public static bool IsProvided(string capability)
            {
                HashSet<string> providers;
                return CapabilityProviders.TryGetValue(Trim(capability), out providers) && providers != null && providers.Count > 0;
            }

            public static string[] Get(string addonId)
            {
                HashSet<string> set;
                if (!AddonCapabilities.TryGetValue(Trim(addonId), out set) || set == null || set.Count == 0) return new string[0];
                string[] result = new string[set.Count];
                set.CopyTo(result);
                Array.Sort(result, StringComparer.Ordinal);
                return result;
            }

            public static string[] GetProviders(string capability)
            {
                HashSet<string> providers;
                if (!CapabilityProviders.TryGetValue(Trim(capability), out providers) || providers == null || providers.Count == 0) return new string[0];
                string[] result = new string[providers.Count];
                providers.CopyTo(result);
                Array.Sort(result, StringComparer.Ordinal);
                return result;
            }
        }

        public static bool RegisterAddonEvent(string addonId, string eventId)
        {
            return InternalRegisterCustomEvent(addonId, eventId);
        }

        public static bool PublishAddonEvent(
            string addonId,
            string eventId,
            IDictionary<string, string> payload = null,
            Kingdom kingdom = null,
            City city = null,
            Actor actor = null,
            string category = ""
        )
        {
            string owner = Trim(addonId);
            string id = Trim(eventId);
            if (!IsAddonRegistered(owner) || !IsOwnedContentId(owner, id)) return false;
            if (!InternalRegisterCustomEvent(owner, id)) return false;
            return InternalEmitAddonEvent(owner, id, payload, kingdom, city, actor, category);
        }

        private static bool ValidateObjectDataRequest(Actor actor, string addonId, string key)
        {
            return actor != null && actor.data != null && ValidateDataOwnerAndKey(addonId, key);
        }

        private static bool ValidateObjectDataRequest(City city, string addonId, string key)
        {
            return city != null && city.data != null && ValidateDataOwnerAndKey(addonId, key);
        }

        private static bool ValidateDataOwnerAndKey(string addonId, string key)
        {
            string owner = Trim(addonId);
            string localKey = Trim(key);
            return IsAddonRegistered(owner) && localKey.Length > 0 && localKey.Length <= 128;
        }

        private static string MakeGeneralObjectDataKey(string addonId, string key)
        {
            if (!ValidateDataOwnerAndKey(addonId, key)) return "";
            return GeneralObjectDataPrefix + EncodeGeneralToken(Trim(addonId)) + "_" + EncodeGeneralToken(Trim(key));
        }

        private static string EncodeGeneralToken(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
            char[] chars = new char[bytes.Length * 2];
            const string hex = "0123456789ABCDEF";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[(bytes[i] >> 4) & 15];
                chars[i * 2 + 1] = hex[bytes[i] & 15];
            }
            return new string(chars);
        }

        private static List<string> ParseTags(string raw)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(raw)) return result;
            string[] parts = raw.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string value = Trim(parts[i]);
                if (IsSafeLocalTag(value) && !ContainsTag(result, value)) result.Add(value);
            }
            return result;
        }

        private static bool IsSafeLocalTag(string tag)
        {
            string value = Trim(tag);
            return value.Length > 0 && value.Length <= 96 && value.IndexOf('|') < 0;
        }

        private static bool ContainsTag(List<string> tags, string tag)
        {
            string wanted = Trim(tag);
            if (tags == null || wanted.Length == 0) return false;
            for (int i = 0; i < tags.Count; i++) if (string.Equals(tags[i], wanted, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool RemoveTag(List<string> tags, string tag)
        {
            string wanted = Trim(tag);
            if (tags == null || wanted.Length == 0) return false;
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                if (string.Equals(tags[i], wanted, StringComparison.Ordinal)) { tags.RemoveAt(i); return true; }
            }
            return false;
        }

        private static string Trim(string value)
        {
            return value == null ? "" : value.Trim();
        }

        private static string[] NormalizeTags(string[] tags)
        {
            if (tags == null || tags.Length == 0) return new string[0];
            List<string> result = new List<string>();
            for (int i = 0; i < tags.Length; i++)
            {
                string value = Trim(tags[i]);
                if (value.Length == 0 || value.IndexOf('|') >= 0 || ContainsTag(result, value)) continue;
                result.Add(value);
            }
            return result.ToArray();
        }

        private static bool ContainsTagArray(string[] tags, string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Length; i++) if (string.Equals(tags[i], tag, StringComparison.Ordinal)) return true;
            return false;
        }

        private static Dictionary<string, string> CloneMetadata(IDictionary<string, string> source)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (source == null) return result;
            foreach (KeyValuePair<string, string> pair in source)
            {
                string key = Trim(pair.Key);
                if (key.Length == 0 || key.Length > 128) continue;
                result[key] = pair.Value ?? "";
            }
            return result;
        }

        private static GenericContentTypeInfo CloneType(GenericContentTypeInfo source)
        {
            if (source == null) return null;
            return new GenericContentTypeInfo()
            {
                Id = source.Id,
                NameKey = source.NameKey,
                DisplayName = ResolveLocalization(source.NameKey, source.DisplayName),
                DescriptionKey = source.DescriptionKey,
                Description = ResolveLocalization(source.DescriptionKey, source.Description),
                Icon = source.Icon,
                SortOrder = source.SortOrder,
                Source = source.Source,
                Tags = source.Tags == null ? new string[0] : (string[])source.Tags.Clone()
            };
        }

        private static GenericContentInfo CloneContent(GenericContentInfo source)
        {
            if (source == null) return null;
            return new GenericContentInfo()
            {
                Id = source.Id,
                TypeId = source.TypeId,
                NameKey = source.NameKey,
                DisplayName = ResolveLocalization(source.NameKey, source.DisplayName),
                DescriptionKey = source.DescriptionKey,
                Description = ResolveLocalization(source.DescriptionKey, source.Description),
                Icon = source.Icon,
                SortOrder = source.SortOrder,
                Source = source.Source,
                Tags = source.Tags == null ? new string[0] : (string[])source.Tags.Clone(),
                Metadata = CloneMetadata(source.Metadata)
            };
        }

        private static List<GenericContentInfo> GetFiltered(Predicate<GenericContentInfo> predicate)
        {
            List<GenericContentInfo> result = new List<GenericContentInfo>();
            foreach (GenericContentInfo info in GenericContent.Values)
            {
                if (info != null && (predicate == null || predicate(info))) result.Add(CloneContent(info));
            }
            result.Sort(delegate(GenericContentInfo a, GenericContentInfo b)
            {
                int order = a.SortOrder.CompareTo(b.SortOrder);
                return order != 0 ? order : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
            });
            return result;
        }

        private static bool IsSafeCapabilityId(string capability)
        {
            if (string.IsNullOrEmpty(capability) || capability.Length > 160) return false;
            for (int i = 0; i < capability.Length; i++)
            {
                char c = capability[i];
                bool safe = char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '_' || c == '-';
                if (!safe) return false;
            }
            return true;
        }
    }
}
