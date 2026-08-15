using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Lightweight custom-government registry. Custom governments reuse one
    /// built-in GovernmentArchetype for mechanics, elections and leadership.
    /// No Update loop is created by this registry.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        private sealed class RegisteredGovernment
        {
            public string Owner;
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public GovernmentArchetype BaseArchetype;
            public string[] Tags;
        }

        private static readonly Dictionary<string, RegisteredGovernment> RegisteredGovernments =
            new Dictionary<string, RegisteredGovernment>(StringComparer.Ordinal);
        private static List<string> _sortedGovernmentIdsCache;

        public static ValidationResult ValidateGovernment(string addonId, GovernmentDefinition definition)
        {
            ValidationResult result = NewValidationResult();
            string owner = addonId == null ? "" : addonId.Trim();
            if (!IsAddonRegistered(owner))
            {
                AddValidationIssue(result, "PW400", "Addon '" + owner + "' is not registered. Call RegisterAddon first.", true);
            }
            if (definition == null)
            {
                AddValidationIssue(result, "PW401", "Government definition is null.", true);
                return FinishValidation(result);
            }

            string id = definition.Id == null ? "" : definition.Id.Trim();
            if (string.IsNullOrEmpty(id))
            {
                AddValidationIssue(result, "PW402", "Government Id is required.", true);
            }
            else if (!IsOwnedContentId(owner, id))
            {
                AddValidationIssue(result, "PW403", "Government Id must be owned by the addon. Example: " + owner + ".magocracy", true);
            }
            else if (GetGovernment(id) != null)
            {
                AddValidationIssue(result, "PW404", "Government Id '" + id + "' is already registered.", true);
            }

            if (definition.BaseArchetype == GovernmentArchetype.Unknown ||
                string.IsNullOrEmpty(InternalGetCoreGovernmentId(definition.BaseArchetype)))
            {
                AddValidationIssue(result, "PW405", "BaseArchetype is required and must be a supported Political World government archetype.", true);
            }
            if (string.IsNullOrWhiteSpace(definition.DisplayName) && string.IsNullOrWhiteSpace(definition.NameKey))
            {
                AddValidationIssue(result, "PW406", "Government has no DisplayName or NameKey; its Id will be shown to the player.", false);
            }
            return FinishValidation(result);
        }

        public static bool RegisterGovernment(string addonId, GovernmentDefinition definition)
        {
            ValidationResult validation = ValidateGovernment(addonId, definition);
            if (!validation.IsValid)
            {
                InternalRecordDiagnostic(addonId, "ERROR", "PWDIAG050", "Government registration failed: " + validation.Summary);
                LogValidationFailure("RegisterGovernment", validation);
                return false;
            }

            string owner = addonId.Trim();
            string id = definition.Id.Trim();
            RegisteredGovernments[id] = new RegisteredGovernment()
            {
                Owner = owner,
                Id = id,
                NameKey = definition.NameKey == null ? "" : definition.NameKey.Trim(),
                DisplayName = definition.DisplayName == null ? "" : definition.DisplayName.Trim(),
                DescriptionKey = definition.DescriptionKey == null ? "" : definition.DescriptionKey.Trim(),
                Description = definition.Description == null ? "" : definition.Description.Trim(),
                Icon = definition.Icon == null ? "" : definition.Icon.Trim(),
                SortOrder = definition.SortOrder,
                BaseArchetype = definition.BaseArchetype,
                Tags = NormalizeGovernmentTags(definition.Tags)
            };
            InternalSeedLocalizationFallback(owner, definition.NameKey, definition.DisplayName);
            InternalSeedLocalizationFallback(owner, definition.DescriptionKey, definition.Description);
            _sortedGovernmentIdsCache = null;
            InternalRecordGovernmentRegistered(owner, id);
            return true;
        }

        public static string[] GetGovernmentTags(string governmentId)
        {
            RegisteredGovernment value;
            if (string.IsNullOrWhiteSpace(governmentId) ||
                !RegisteredGovernments.TryGetValue(governmentId.Trim(), out value) || value == null)
            {
                return new string[0];
            }
            return value.Tags == null ? new string[0] : (string[])value.Tags.Clone();
        }

        public static bool HasGovernmentTag(string governmentId, string tag)
        {
            if (string.IsNullOrWhiteSpace(governmentId) || string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }
            RegisteredGovernment value;
            if (!RegisteredGovernments.TryGetValue(governmentId.Trim(), out value) ||
                value == null ||
                value.Tags == null)
            {
                return false;
            }
            string wanted = tag.Trim();
            for (int i = 0; i < value.Tags.Length; i++)
            {
                if (string.Equals(value.Tags[i], wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetCoreGovernmentId(GovernmentArchetype archetype)
        {
            return InternalGetCoreGovernmentId(archetype);
        }

        internal static bool InternalIsRegisteredGovernment(string governmentId)
        {
            return !string.IsNullOrWhiteSpace(governmentId) && RegisteredGovernments.ContainsKey(governmentId.Trim());
        }

        internal static string InternalGetRegisteredGovernmentBaseId(string governmentId)
        {
            RegisteredGovernment value;
            if (string.IsNullOrWhiteSpace(governmentId) ||
                !RegisteredGovernments.TryGetValue(governmentId.Trim(), out value) || value == null)
            {
                return "";
            }
            return InternalGetCoreGovernmentId(value.BaseArchetype);
        }

        internal static string InternalGetGovernmentDisplayName(string governmentId)
        {
            RegisteredGovernment value;
            if (string.IsNullOrWhiteSpace(governmentId) ||
                !RegisteredGovernments.TryGetValue(governmentId.Trim(), out value) || value == null)
            {
                return "";
            }
            return ResolveGovernmentDisplayName(value);
        }

        internal static List<GovernmentInfo> InternalGetRegisteredGovernments()
        {
            if (_sortedGovernmentIdsCache == null)
            {
                _sortedGovernmentIdsCache = new List<string>(RegisteredGovernments.Keys);
                _sortedGovernmentIdsCache.Sort(StringComparer.Ordinal);
            }

            List<GovernmentInfo> result = new List<GovernmentInfo>();
            for (int i = 0; i < _sortedGovernmentIdsCache.Count; i++)
            {
                RegisteredGovernment value;
                if (!RegisteredGovernments.TryGetValue(_sortedGovernmentIdsCache[i], out value))
                {
                    continue;
                }
                GovernmentInfo info = ConvertRegisteredGovernment(value);
                if (info != null) result.Add(info);
            }
            return result;
        }

        internal static List<GovernmentInfo> InternalGetRegisteredGovernmentsByOwner(
            string addonId
        )
        {
            string wanted = addonId == null ? "" : addonId.Trim();
            List<GovernmentInfo> result = new List<GovernmentInfo>();
            List<GovernmentInfo> all = InternalGetRegisteredGovernments();
            for (int i = 0; i < all.Count; i++)
            {
                GovernmentInfo info = all[i];
                if (info != null &&
                    string.Equals(info.Source ?? "", wanted, StringComparison.Ordinal))
                {
                    result.Add(info);
                }
            }
            return result;
        }

        internal static GovernmentInfo InternalGetRegisteredGovernment(string governmentId)
        {
            RegisteredGovernment value;
            if (string.IsNullOrWhiteSpace(governmentId) ||
                !RegisteredGovernments.TryGetValue(governmentId.Trim(), out value)) return null;
            return ConvertRegisteredGovernment(value);
        }

        internal static GovernmentArchetype InternalGetArchetypeForCoreGovernmentId(string governmentId)
        {
            if (governmentId == "ukiol_government_absolute_monarchy") return GovernmentArchetype.AbsoluteMonarchy;
            if (governmentId == "ukiol_government_constitutional_monarchy") return GovernmentArchetype.ConstitutionalMonarchy;
            if (governmentId == "ukiol_government_parliamentary_republic") return GovernmentArchetype.ParliamentaryRepublic;
            if (governmentId == "ukiol_government_presidential_republic") return GovernmentArchetype.PresidentialRepublic;
            if (governmentId == "ukiol_government_one_party_state") return GovernmentArchetype.OnePartyState;
            if (governmentId == "ukiol_government_military_dictatorship") return GovernmentArchetype.MilitaryDictatorship;
            if (governmentId == "ukiol_government_council_republic") return GovernmentArchetype.CouncilRepublic;
            if (governmentId == "ukiol_government_oligarchy") return GovernmentArchetype.Oligarchy;
            return GovernmentArchetype.Unknown;
        }

        internal static string InternalGetCoreGovernmentId(GovernmentArchetype archetype)
        {
            switch (archetype)
            {
                case GovernmentArchetype.AbsoluteMonarchy: return "ukiol_government_absolute_monarchy";
                case GovernmentArchetype.ConstitutionalMonarchy: return "ukiol_government_constitutional_monarchy";
                case GovernmentArchetype.ParliamentaryRepublic: return "ukiol_government_parliamentary_republic";
                case GovernmentArchetype.PresidentialRepublic: return "ukiol_government_presidential_republic";
                case GovernmentArchetype.OnePartyState: return "ukiol_government_one_party_state";
                case GovernmentArchetype.MilitaryDictatorship: return "ukiol_government_military_dictatorship";
                case GovernmentArchetype.CouncilRepublic: return "ukiol_government_council_republic";
                case GovernmentArchetype.Oligarchy: return "ukiol_government_oligarchy";
                default: return "";
            }
        }

        private static GovernmentInfo ConvertRegisteredGovernment(RegisteredGovernment value)
        {
            if (value == null) return null;
            return new GovernmentInfo()
            {
                Id = value.Id,
                NameKey = value.NameKey,
                DisplayName = ResolveGovernmentDisplayName(value),
                DescriptionKey = value.DescriptionKey,
                Description = ResolveLocalization(value.DescriptionKey, value.Description),
                Icon = value.Icon,
                SortOrder = value.SortOrder,
                BaseArchetype = value.BaseArchetype,
                Source = value.Owner,
                IsCustom = true,
                Tags = value.Tags == null ? new string[0] : (string[])value.Tags.Clone()
            };
        }

        private static string ResolveGovernmentDisplayName(RegisteredGovernment value)
        {
            if (value == null) return "";
            string fallback = !string.IsNullOrWhiteSpace(value.DisplayName)
                ? value.DisplayName
                : (!string.IsNullOrWhiteSpace(value.NameKey) ? value.NameKey : (value.Id ?? ""));
            return ResolveLocalization(value.NameKey, fallback);
        }

        private static string[] NormalizeGovernmentTags(string[] values)
        {
            if (values == null || values.Length == 0) return new string[0];
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] == null ? "" : values[i].Trim();
                if (string.IsNullOrEmpty(value) || !seen.Add(value)) continue;
                result.Add(value);
            }
            return result.ToArray();
        }
    }
}
