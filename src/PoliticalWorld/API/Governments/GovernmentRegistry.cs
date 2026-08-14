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
            public GovernmentArchetype BaseArchetype;
            public string[] Tags;
        }

        private static readonly Dictionary<string, RegisteredGovernment> RegisteredGovernments =
            new Dictionary<string, RegisteredGovernment>(StringComparer.Ordinal);

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
                BaseArchetype = definition.BaseArchetype,
                Tags = NormalizeGovernmentTags(definition.Tags)
            };
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
            if (string.IsNullOrWhiteSpace(tag)) return false;
            string wanted = tag.Trim();
            string[] tags = GetGovernmentTags(governmentId);
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], wanted, StringComparison.OrdinalIgnoreCase)) return true;
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
            List<GovernmentInfo> result = new List<GovernmentInfo>();
            foreach (KeyValuePair<string, RegisteredGovernment> pair in RegisteredGovernments)
            {
                GovernmentInfo info = ConvertRegisteredGovernment(pair.Value);
                if (info != null) result.Add(info);
            }
            result.Sort((a, b) => string.Compare(a == null ? "" : a.Id, b == null ? "" : b.Id, StringComparison.Ordinal));
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
                BaseArchetype = value.BaseArchetype,
                Source = value.Owner,
                IsCustom = true,
                Tags = value.Tags == null ? new string[0] : (string[])value.Tags.Clone()
            };
        }

        private static string ResolveGovernmentDisplayName(RegisteredGovernment value)
        {
            if (value == null) return "";
            if (!string.IsNullOrWhiteSpace(value.NameKey))
            {
                try
                {
                    string localized = NeoModLoader.General.LM.Get(value.NameKey);
                    if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, value.NameKey, StringComparison.Ordinal))
                    {
                        return localized;
                    }
                }
                catch { }
            }
            if (!string.IsNullOrWhiteSpace(value.DisplayName)) return value.DisplayName;
            if (!string.IsNullOrWhiteSpace(value.NameKey)) return value.NameKey;
            return value.Id ?? "";
        }

        private static string[] NormalizeGovernmentTags(string[] values)
        {
            if (values == null || values.Length == 0) return new string[0];
            List<string> result = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] == null ? "" : values[i].Trim();
                if (string.IsNullOrEmpty(value)) continue;
                bool exists = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (string.Equals(result[j], value, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                }
                if (!exists) result.Add(value);
            }
            return result.ToArray();
        }
    }
}
