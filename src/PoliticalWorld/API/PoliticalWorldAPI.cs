using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Stable public facade for third-party Political World addons.
    ///
    /// Design rule: registration happens on addon load and all scenario writes
    /// happen only when explicitly called. The API does not create its own
    /// Update loop or per-actor simulation.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public const string ApiVersion = "1.9.0";
        public const int ApiMajor = 1;
        public const int ApiMinor = 9;
        public const string CoreModId = "Lous12.PoliticalWorld";

        public delegate bool KingdomCondition(Kingdom kingdom);
        public delegate void KingdomAction(Kingdom kingdom);

        public sealed class ValidationIssue
        {
            public string Code;
            public string Message;
            public bool IsError;
        }

        public sealed class ValidationResult
        {
            public bool IsValid;
            public List<ValidationIssue> Issues = new List<ValidationIssue>();

            public string Summary
            {
                get
                {
                    if (Issues == null || Issues.Count == 0) return IsValid ? "OK" : "Validation failed";
                    List<string> parts = new List<string>();
                    for (int i = 0; i < Issues.Count; i++)
                    {
                        ValidationIssue issue = Issues[i];
                        if (issue == null) continue;
                        parts.Add((issue.IsError ? "ERROR " : "WARN ") + (issue.Code ?? "PW000") + ": " + (issue.Message ?? ""));
                    }
                    return string.Join(" | ", parts.ToArray());
                }
            }
        }

        public sealed class AddonDefinition
        {
            public string Id;
            public string Name;
            public string Version;
            public string Description;
            public string Author;
        }

        public sealed class AddonInfo
        {
            public string Id;
            public string Name;
            public string Version;
            public string Description;
            public string Author;
        }

        public sealed class IdeologyDefinition
        {
            public string Id;
            public string ParentId;
            public string NameKey;
            // Literal fallback text. English is recommended, but any readable
            // default is accepted. Localization is optional.
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public int HighSupportStability;
            public int SupportThreshold = -1;
            public int LowSupportStability;
            public float DiffusionMultiplier = 1f;
            // 0 by default: addon ideologies do not randomly appear unless
            // the addon author explicitly opts in.
            public int RandomWeight;
            public string[] Tags;
        }

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
        /// Mechanical archetype reused by custom governments. Political World
        /// keeps optimized core behaviour while addons provide a distinct
        /// public identity, localization and tags.
        /// </summary>
        public enum GovernmentArchetype
        {
            Unknown = 0,
            AbsoluteMonarchy = 1,
            ConstitutionalMonarchy = 2,
            ParliamentaryRepublic = 3,
            PresidentialRepublic = 4,
            OnePartyState = 5,
            MilitaryDictatorship = 6,
            CouncilRepublic = 7,
            Oligarchy = 8
        }

        public sealed class GovernmentDefinition
        {
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

        public sealed class GovernmentInfo
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Icon;
            public int SortOrder;
            public GovernmentArchetype BaseArchetype;
            public string Source;
            public bool IsCustom;
            public string[] Tags;
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

        public sealed class ActionDefinition
        {
            public string Id;
            public string Category;
            public string NameKey;
            public string DescriptionKey;
            public string DisplayName;
            public string Description;
            public string Icon;
            public int SortOrder;
            public KingdomCondition Condition;
            public KingdomAction Handler;
        }

        public sealed class ActionInfo
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

        /// <summary>
        /// Rare kingdom-level political event evaluated by Political World's
        /// existing yearly political pipeline. No addon Update loop is needed.
        /// ChancePermille uses 0..1000 where 10 = 1%, 100 = 10%, 1000 = 100%.
        /// </summary>
        public sealed class RarePoliticalEventDefinition
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public int CheckIntervalYears = 1;
            public int CooldownYears = 5;
            public int ChancePermille = 50;
            public bool CheckImmediately;
            public KingdomCondition Condition;
            public KingdomAction Handler;
        }

        public sealed class RarePoliticalEventInfo
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public string Source;
            public int CheckIntervalYears;
            public int CooldownYears;
            public int ChancePermille;
            public bool CheckImmediately;
        }

        /// <summary>
        /// Developer-facing result for operations that can be rejected by the
        /// current political state. Code is stable and suitable for UI logic;
        /// Message is a human-readable diagnostic.
        /// </summary>
        public sealed class OperationCheck
        {
            public bool Allowed;
            public string Code;
            public string Message;
        }

        public sealed class PoliticalSystemInfo
        {
            public string Id;
            public string DisplayName;
            public bool CompetitiveElections;
            public bool PartyMandate;
            public bool CouncilBased;
            public bool Decentralized;
        }

        /// <summary>
        /// Stable public constants for Political World's current political
        /// systems. Addons should use these constants instead of hardcoding
        /// legacy ukiol_* save IDs.
        /// </summary>
        public static class PoliticalSystems
        {
            public const string Competitive = "ukiol_political_system_competitive";
            public const string OneParty = "ukiol_political_system_one_party";
            public const string Soviet = "ukiol_political_system_soviet";
            public const string SovietOneParty = "ukiol_political_system_soviet_one_party";
            public const string NonElectoral = "ukiol_political_system_non_electoral";
            public const string Decentralized = "ukiol_political_system_decentralized";
        }

        private static readonly Dictionary<string, AddonInfo> RegisteredAddons =
            new Dictionary<string, AddonInfo>(StringComparer.Ordinal);

        static PoliticalWorldAPI()
        {
            RegisteredAddons[CoreModId] = new AddonInfo()
            {
                Id = CoreModId,
                Name = "Political World",
                Version = "1.9.0",
                Description = "Political World core API",
                Author = "Lous12"
            };
        }

        private static readonly string[] PoliticalSystemIds = new string[]
        {
            PoliticalSystems.Competitive,
            PoliticalSystems.OneParty,
            PoliticalSystems.Soviet,
            PoliticalSystems.SovietOneParty,
            PoliticalSystems.NonElectoral,
            PoliticalSystems.Decentralized
        };

        private static readonly string[] Capabilities = new string[]
        {
            "addon.registry",
            "action.registry",
            "condition.helpers",
            "ideology.read",
            "ideology.register",
            "government.read",
            "government.register",
            "government.tags",
            "government.archetypes",
            "kingdom.read",
            "kingdom.write",
            "kingdom.tags",
            "kingdom.addon-tags",
            "kingdom.addon-data",
            "kingdom.addon-data.v2",
            "kingdom.addon-data.typed",
            "content.lookup",
            "content.filter-by-addon",
            "localization.safe",
            "localization.fallback",
            "localization.register",
            "content.metadata",
            "content.batch-register",
            "content.query",
            "effect.helpers",
            "condition.helpers.v2",
            "operation.result",
            "party.addon-data",
            "diagnostics.report",
            "political-system.read",
            "operation.checks",
            "action.inspect",
            "validation",
            "party.read",
            "party.write",
            "party.lifecycle",
            "party.ideology.write",
            "party.leadership",
            "party.ruling",
            "event.publish",
            "event.subscribe",
            "event.core-hooks",
            "political-event.registry",
            "political-event.rare",
            "political-event.rare.execute",
            "diagnostics"
        };

        public static bool IsReady()
        {
            return Main.ScenarioBridge.IsReady();
        }

        public static bool IsCompatible(int requiredMajor, int requiredMinor)
        {
            if (requiredMajor != ApiMajor)
            {
                return false;
            }
            return ApiMinor >= requiredMinor;
        }

        public static string[] GetCapabilities()
        {
            return (string[])Capabilities.Clone();
        }

        public static bool HasCapability(string capability)
        {
            return InternalHasCapabilityFast(capability);
        }

        public static ValidationResult ValidateAddon(AddonDefinition definition)
        {
            ValidationResult result = NewValidationResult();
            if (definition == null)
            {
                AddValidationIssue(result, "PW100", "Addon definition is null.", true);
                return FinishValidation(result);
            }

            string id = definition.Id == null ? "" : definition.Id.Trim();
            string name = definition.Name == null ? "" : definition.Name.Trim();
            if (string.IsNullOrEmpty(id))
            {
                AddValidationIssue(result, "PW101", "Addon Id is required. Example: Lous12.MyAddon", true);
            }
            else
            {
                if (!IsSafeAddonId(id))
                {
                    AddValidationIssue(result, "PW102", "Addon Id may contain only letters, numbers, '.', '_' and '-', must start/end with a letter or number, and must be 3-96 characters long.", true);
                }
                if (string.Equals(id, CoreModId, StringComparison.Ordinal))
                {
                    AddValidationIssue(result, "PW103", "Addon Id is reserved by Political World core.", true);
                }
                else if (RegisteredAddons.ContainsKey(id))
                {
                    AddValidationIssue(result, "PW104", "An addon with Id '" + id + "' is already registered.", true);
                }
                if (id.IndexOf('.') < 0)
                {
                    AddValidationIssue(result, "PW105", "Addon Id should be namespaced (for example: YourName.MyAddon).", false);
                }
            }
            if (string.IsNullOrEmpty(name))
            {
                AddValidationIssue(result, "PW106", "Addon Name is required.", true);
            }
            return FinishValidation(result);
        }

        public static ValidationResult ValidateIdeology(string addonId, IdeologyDefinition definition)
        {
            ValidationResult result = NewValidationResult();
            string owner = addonId == null ? "" : addonId.Trim();
            if (!IsAddonRegistered(owner))
            {
                AddValidationIssue(result, "PW200", "Addon '" + owner + "' is not registered. Call RegisterAddon first.", true);
            }
            if (definition == null)
            {
                AddValidationIssue(result, "PW201", "Ideology definition is null.", true);
                return FinishValidation(result);
            }
            string id = definition.Id == null ? "" : definition.Id.Trim();
            if (string.IsNullOrEmpty(id))
            {
                AddValidationIssue(result, "PW202", "Ideology Id is required.", true);
            }
            else if (!IsOwnedContentId(owner, id))
            {
                AddValidationIssue(result, "PW203", "Ideology Id must start with the addon Id followed by '.', ':' or '_'. Example: " + owner + ".technocracy", true);
            }
            else if (FindIdeology(id) != null)
            {
                AddValidationIssue(result, "PW204", "Ideology Id '" + id + "' is already registered.", true);
            }

            string parentId = definition.ParentId == null ? "" : definition.ParentId.Trim();
            if (!string.IsNullOrEmpty(parentId) && FindIdeology(parentId) == null)
            {
                AddValidationIssue(result, "PW205", "Parent ideology '" + parentId + "' was not found. Register the parent first or use an existing Political World ideology Id.", true);
            }
            if (definition.DiffusionMultiplier <= 0f)
            {
                AddValidationIssue(result, "PW206", "DiffusionMultiplier <= 0 will be normalized to 1.0 by the core.", false);
            }
            if (definition.RandomWeight < 0)
            {
                AddValidationIssue(result, "PW207", "RandomWeight cannot be negative; it will effectively behave as 0.", false);
            }
            if (string.IsNullOrWhiteSpace(definition.DisplayName) && string.IsNullOrWhiteSpace(definition.NameKey))
            {
                AddValidationIssue(result, "PW208", "Ideology has no DisplayName or NameKey; its Id will be used as readable fallback text.", false);
            }
            return FinishValidation(result);
        }

        public static ValidationResult ValidateAction(string addonId, ActionDefinition definition)
        {
            ValidationResult result = NewValidationResult();
            string owner = addonId == null ? "" : addonId.Trim();
            if (!IsAddonRegistered(owner))
            {
                AddValidationIssue(result, "PW300", "Addon '" + owner + "' is not registered. Call RegisterAddon first.", true);
            }
            if (definition == null)
            {
                AddValidationIssue(result, "PW301", "Action definition is null.", true);
                return FinishValidation(result);
            }
            string id = definition.Id == null ? "" : definition.Id.Trim();
            if (string.IsNullOrEmpty(id))
            {
                AddValidationIssue(result, "PW302", "Action Id is required.", true);
            }
            else if (!IsOwnedContentId(owner, id))
            {
                AddValidationIssue(result, "PW303", "Action Id must be owned by the addon. Example: " + owner + ".my_action", true);
            }
            if (definition.Handler == null)
            {
                AddValidationIssue(result, "PW304", "Action Handler is required.", true);
            }
            if (string.IsNullOrWhiteSpace(definition.DisplayName) && string.IsNullOrWhiteSpace(definition.NameKey))
            {
                AddValidationIssue(result, "PW305", "Action has no DisplayName or NameKey; its Id will be shown to the player.", false);
            }
            return FinishValidation(result);
        }

        public static bool RegisterAddon(AddonDefinition definition)
        {
            ValidationResult validation = ValidateAddon(definition);
            if (!validation.IsValid)
            {
                LogValidationFailure("RegisterAddon", validation);
                return false;
            }

            string id = definition.Id.Trim();
            RegisteredAddons[id] = new AddonInfo()
            {
                Id = id,
                Name = definition.Name.Trim(),
                Version = definition.Version == null ? "" : definition.Version.Trim(),
                Description = definition.Description == null ? "" : definition.Description.Trim(),
                Author = definition.Author == null ? "" : definition.Author.Trim()
            };
            InternalEnsureDiagnostics(id);
            InternalRecordDiagnostic(
                id,
                "INFO",
                "PWDIAG001",
                "Addon registered: " + definition.Name.Trim()
            );
            return true;
        }

        public static bool IsAddonRegistered(string addonId)
        {
            return !string.IsNullOrWhiteSpace(addonId) &&
                RegisteredAddons.ContainsKey(addonId.Trim());
        }

        public static AddonInfo GetAddon(string addonId)
        {
            if (string.IsNullOrWhiteSpace(addonId))
            {
                return null;
            }

            AddonInfo info;
            if (!RegisteredAddons.TryGetValue(addonId.Trim(), out info) || info == null)
            {
                return null;
            }
            return new AddonInfo()
            {
                Id = info.Id,
                Name = info.Name,
                Version = info.Version,
                Description = info.Description,
                Author = info.Author
            };
        }

        /// <summary>
        /// Checks localization without forcing getText to emit a missing-text
        /// error. Useful for optional localization supplied by third-party addons.
        /// </summary>
        public static bool HasLocalization(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }
            try
            {
                return NeoModLoader.General.LM.Has(key.Trim());
            }
            catch
            {
                return false;
            }
        }

        public static string ResolveLocalization(string key, string fallback = "")
        {
            string normalized = key == null ? "" : key.Trim();

            string registered = InternalResolveRegisteredLocalization(normalized);
            if (!string.IsNullOrEmpty(registered))
            {
                return registered;
            }

            if (!string.IsNullOrEmpty(normalized))
            {
                try
                {
                    if (NeoModLoader.General.LM.Has(normalized))
                    {
                        string value = NeoModLoader.General.LM.Get(normalized);
                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
                catch
                {
                }

                registered = InternalResolveEnglishLocalization(normalized);
                if (!string.IsNullOrEmpty(registered))
                {
                    return registered;
                }
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }
            return normalized;
        }

        public static List<AddonInfo> GetRegisteredAddons()
        {
            List<AddonInfo> result = new List<AddonInfo>();
            foreach (KeyValuePair<string, AddonInfo> pair in RegisteredAddons)
            {
                AddonInfo info = pair.Value;
                if (info == null)
                {
                    continue;
                }
                result.Add(new AddonInfo()
                {
                    Id = info.Id,
                    Name = info.Name,
                    Version = info.Version,
                    Description = info.Description,
                    Author = info.Author
                });
            }
            result.Sort(delegate(AddonInfo a, AddonInfo b)
            {
                return string.CompareOrdinal(a.Name ?? "", b.Name ?? "");
            });
            return result;
        }

        public static List<IdeologyInfo> GetIdeologies()
        {
            List<Main.ScenarioBridge.IdeologyInfo> source = Main.ScenarioBridge.GetIdeologies();
            List<IdeologyInfo> result = new List<IdeologyInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                Main.ScenarioBridge.IdeologyInfo item = source[i];
                if (item == null) continue;
                result.Add(ConvertIdeology(item));
            }
            return result;
        }

        public static IdeologyInfo GetIdeology(string ideologyId)
        {
            Main.ScenarioBridge.IdeologyInfo item =
                Main.ScenarioBridge.GetIdeology(ideologyId);
            return item == null ? null : ConvertIdeology(item);
        }

        public static List<IdeologyInfo> GetIdeologiesByAddon(string addonId)
        {
            List<Main.ScenarioBridge.IdeologyInfo> source =
                Main.ScenarioBridge.GetIdeologiesBySource(addonId);
            List<IdeologyInfo> result = new List<IdeologyInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    result.Add(ConvertIdeology(source[i]));
                }
            }
            return result;
        }

        public static List<IdeologyInfo> GetRootIdeologies()
        {
            List<Main.ScenarioBridge.IdeologyInfo> source = Main.ScenarioBridge.GetRootIdeologies();
            List<IdeologyInfo> result = new List<IdeologyInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) result.Add(ConvertIdeology(source[i]));
            }
            return result;
        }

        public static List<IdeologyInfo> GetCurrentsForRoot(string rootId)
        {
            List<Main.ScenarioBridge.IdeologyInfo> source = Main.ScenarioBridge.GetCurrentsForRoot(rootId);
            List<IdeologyInfo> result = new List<IdeologyInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) result.Add(ConvertIdeology(source[i]));
            }
            return result;
        }

        public static bool RegisterIdeology(string addonId, IdeologyDefinition definition)
        {
            ValidationResult validation = ValidateIdeology(addonId, definition);
            if (!validation.IsValid)
            {
                InternalRecordDiagnostic(
                    addonId,
                    "ERROR",
                    "PWDIAG110",
                    validation.Summary
                );
                LogValidationFailure("RegisterIdeology", validation);
                return false;
            }

            bool registered = Main.ScenarioBridge.RegisterAddonIdeology(
                new Main.ScenarioBridge.AddonIdeologyDefinition()
                {
                    Id = definition.Id,
                    ParentId = definition.ParentId,
                    NameKey = definition.NameKey,
                    DisplayName = definition.DisplayName,
                    DescriptionKey = definition.DescriptionKey,
                    Description = definition.Description,
                    Icon = definition.Icon,
                    SortOrder = definition.SortOrder,
                    Source = addonId.Trim(),
                    HighSupportStability = definition.HighSupportStability,
                    SupportThreshold = definition.SupportThreshold,
                    LowSupportStability = definition.LowSupportStability,
                    DiffusionMultiplier = definition.DiffusionMultiplier,
                    RandomWeight = definition.RandomWeight,
                    Tags = definition.Tags
                }
            );
            if (registered)
            {
                InternalRecordIdeologyRegistered(addonId.Trim(), definition.Id);
            }
            return registered;
        }

        public static string[] GetIdeologyTags(string ideologyId, bool includeParents)
        {
            return Main.ScenarioBridge.GetIdeologyTags(ideologyId, includeParents);
        }

        public static bool HasIdeologyTag(string ideologyId, string tag, bool includeParents)
        {
            return Main.ScenarioBridge.HasIdeologyTag(ideologyId, tag, includeParents);
        }

        public static List<GovernmentInfo> GetGovernmentForms()
        {
            List<Main.ScenarioBridge.GovernmentInfo> source = Main.ScenarioBridge.GetGovernmentForms();
            List<GovernmentInfo> result = new List<GovernmentInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                Main.ScenarioBridge.GovernmentInfo item = source[i];
                if (item == null) continue;
                result.Add(new GovernmentInfo()
                {
                    Id = item.Id,
                    NameKey = item.Id,
                    DisplayName = item.DisplayName,
                    DescriptionKey = "",
                    Description = "",
                    Icon = "",
                    SortOrder = 0,
                    BaseArchetype = InternalGetArchetypeForCoreGovernmentId(item.Id),
                    Source = CoreModId,
                    IsCustom = false,
                    Tags = new string[0]
                });
            }

            List<GovernmentInfo> custom = InternalGetRegisteredGovernments();
            for (int i = 0; i < custom.Count; i++)
            {
                result.Add(custom[i]);
            }
            return result;
        }

        public static GovernmentInfo GetGovernment(string governmentId)
        {
            if (string.IsNullOrWhiteSpace(governmentId)) return null;
            string wanted = governmentId.Trim();
            GovernmentInfo custom = InternalGetRegisteredGovernment(wanted);
            if (custom != null) return custom;

            List<Main.ScenarioBridge.GovernmentInfo> source = Main.ScenarioBridge.GetGovernmentForms();
            for (int i = 0; i < source.Count; i++)
            {
                Main.ScenarioBridge.GovernmentInfo item = source[i];
                if (item == null || !string.Equals(item.Id, wanted, StringComparison.Ordinal)) continue;
                return new GovernmentInfo()
                {
                    Id = item.Id,
                    NameKey = item.Id,
                    DisplayName = item.DisplayName,
                    DescriptionKey = "",
                    Description = "",
                    Icon = "",
                    SortOrder = 0,
                    BaseArchetype = InternalGetArchetypeForCoreGovernmentId(item.Id),
                    Source = CoreModId,
                    IsCustom = false,
                    Tags = new string[0]
                };
            }
            return null;
        }

        public static List<GovernmentInfo> GetGovernmentsByAddon(string addonId)
        {
            string wanted = addonId == null ? "" : addonId.Trim();
            if (string.Equals(wanted, CoreModId, StringComparison.Ordinal))
            {
                List<GovernmentInfo> all = GetGovernmentForms();
                List<GovernmentInfo> coreOnly = new List<GovernmentInfo>();
                for (int i = 0; i < all.Count; i++)
                {
                    GovernmentInfo info = all[i];
                    if (info != null &&
                        string.Equals(info.Source ?? "", CoreModId, StringComparison.Ordinal))
                    {
                        coreOnly.Add(info);
                    }
                }
                return coreOnly;
            }
            return InternalGetRegisteredGovernmentsByOwner(wanted);
        }

        public static List<PoliticalSystemInfo> GetPoliticalSystems()
        {
            List<PoliticalSystemInfo> result = new List<PoliticalSystemInfo>();
            for (int i = 0; i < PoliticalSystemIds.Length; i++)
            {
                PoliticalSystemInfo info = GetPoliticalSystem(PoliticalSystemIds[i]);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        public static PoliticalSystemInfo GetPoliticalSystem(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return null;
            }

            string id = systemId.Trim();
            bool known =
                id == PoliticalSystems.Competitive ||
                id == PoliticalSystems.OneParty ||
                id == PoliticalSystems.Soviet ||
                id == PoliticalSystems.SovietOneParty ||
                id == PoliticalSystems.NonElectoral ||
                id == PoliticalSystems.Decentralized;
            if (!known)
            {
                return null;
            }

            return new PoliticalSystemInfo()
            {
                Id = id,
                DisplayName = ResolveLocalization(id, id),
                CompetitiveElections = id == PoliticalSystems.Competitive,
                PartyMandate =
                    id == PoliticalSystems.Competitive ||
                    id == PoliticalSystems.OneParty ||
                    id == PoliticalSystems.SovietOneParty,
                CouncilBased =
                    id == PoliticalSystems.Soviet ||
                    id == PoliticalSystems.SovietOneParty ||
                    id == PoliticalSystems.Decentralized,
                Decentralized = id == PoliticalSystems.Decentralized
            };
        }

        public static KingdomState GetKingdomState(Kingdom kingdom)
        {
            Main.ScenarioBridge.KingdomState state = Main.ScenarioBridge.GetKingdomState(kingdom);
            if (state == null) return null;
            return new KingdomState()
            {
                KingdomName = state.KingdomName,
                IdeologyId = state.IdeologyId,
                IdeologyName = state.IdeologyName,
                CurrentId = state.CurrentId,
                CurrentName = state.CurrentName,
                GovernmentId = state.GovernmentId,
                GovernmentName = state.GovernmentName,
                PoliticalSystemId = state.PoliticalSystemId,
                PoliticalSystemName = state.PoliticalSystemName,
                Stability = state.Stability
            };
        }

        public static Actor GetKingdomRuler(Kingdom kingdom)
        {
            return Main.ScenarioBridge.GetKingdomRuler(kingdom);
        }

        public static bool RulerHasTrait(Kingdom kingdom, string traitId)
        {
            return Main.ScenarioBridge.RulerHasTrait(kingdom, traitId);
        }

        public static string GetRulerRaceId(Kingdom kingdom)
        {
            return Main.ScenarioBridge.GetRulerRaceId(kingdom);
        }

        public static bool RulerIsImmortal(Kingdom kingdom)
        {
            return Main.ScenarioBridge.RulerIsImmortal(kingdom);
        }

        public static bool SetKingdomIdeology(Kingdom kingdom, string ideologyId)
        {
            return Main.ScenarioBridge.SetKingdomIdeology(kingdom, ideologyId);
        }

        public static bool SetKingdomCurrent(Kingdom kingdom, string currentId)
        {
            return Main.ScenarioBridge.SetKingdomCurrent(kingdom, currentId);
        }

        public static bool SetKingdomGovernment(Kingdom kingdom, string governmentId, bool publishEvent)
        {
            return Main.ScenarioBridge.SetKingdomGovernment(kingdom, governmentId, publishEvent);
        }

        public static bool SetKingdomStability(Kingdom kingdom, int value)
        {
            return Main.ScenarioBridge.SetKingdomStability(kingdom, value);
        }

        public static bool ChangeKingdomStability(Kingdom kingdom, int delta)
        {
            return Main.ScenarioBridge.ChangeKingdomStability(kingdom, delta);
        }

        public static List<PartyInfo> GetKingdomParties(Kingdom kingdom)
        {
            return GetKingdomParties(kingdom, false);
        }

        /// <summary>
        /// Returns Political World parties for a kingdom. By default the historical
        /// API only returns active parties; pass includeInactive=true when building
        /// editors, inspectors or migration tools.
        /// </summary>
        public static List<PartyInfo> GetKingdomParties(Kingdom kingdom, bool includeInactive)
        {
            List<Main.ScenarioBridge.PartyInfo> source =
                Main.ScenarioBridge.GetKingdomParties(kingdom, includeInactive);
            List<PartyInfo> result = new List<PartyInfo>();
            for (int i = 0; i < source.Count; i++)
            {
                Main.ScenarioBridge.PartyInfo item = source[i];
                if (item == null) continue;
                result.Add(ConvertParty(item));
            }
            return result;
        }

        /// <summary>
        /// Finds one party by its stable Political World party ID.
        /// Inactive parties can be inspected as well.
        /// </summary>
        public static PartyInfo GetKingdomParty(Kingdom kingdom, string partyId, bool includeInactive = true)
        {
            Main.ScenarioBridge.PartyInfo item =
                Main.ScenarioBridge.GetKingdomParty(kingdom, partyId, includeInactive);
            return item == null ? null : ConvertParty(item);
        }

        /// <summary>
        /// Returns the currently stored ruling/election party, or null when the
        /// current political system has no party mandate.
        /// </summary>
        public static PartyInfo GetKingdomRulingParty(Kingdom kingdom)
        {
            Main.ScenarioBridge.PartyInfo item =
                Main.ScenarioBridge.GetKingdomRulingParty(kingdom);
            return item == null ? null : ConvertParty(item);
        }

        /// <summary>
        /// Returns the live actor currently stored as party leader, if the leader
        /// still belongs to this kingdom and can be resolved.
        /// </summary>
        public static Actor GetKingdomPartyLeader(Kingdom kingdom, string partyId)
        {
            return Main.ScenarioBridge.GetKingdomPartyLeader(kingdom, partyId);
        }

        public static string CreateKingdomParty(Kingdom kingdom, string ideologyId, int radicalism, string customName)
        {
            return Main.ScenarioBridge.CreateKingdomParty(kingdom, ideologyId, radicalism, customName);
        }

        public static bool RenameKingdomParty(Kingdom kingdom, string partyId, string newName)
        {
            return Main.ScenarioBridge.RenameKingdomParty(kingdom, partyId, newName);
        }

        public static bool SetKingdomPartyRadicalism(Kingdom kingdom, string partyId, int radicalism)
        {
            return Main.ScenarioBridge.SetKingdomPartyRadicalism(kingdom, partyId, radicalism);
        }

        public static bool SetKingdomPartySupport(Kingdom kingdom, string partyId, int support)
        {
            return Main.ScenarioBridge.SetKingdomPartySupport(kingdom, partyId, support);
        }

        /// <summary>
        /// Changes a party's root ideology while preserving its stable party ID,
        /// history, founder and custom name. Active-party limits are enforced.
        /// </summary>
        public static bool SetKingdomPartyIdeology(Kingdom kingdom, string partyId, string ideologyId)
        {
            return Main.ScenarioBridge.SetKingdomPartyIdeology(kingdom, partyId, ideologyId);
        }

        /// <summary>
        /// Soft lifecycle control. Deactivation is the supported equivalent of
        /// removal: hard deletion is intentionally not exposed because party IDs
        /// can be referenced by election history, split lineage and city support.
        /// </summary>
        public static bool SetKingdomPartyActive(Kingdom kingdom, string partyId, bool active)
        {
            return Main.ScenarioBridge.SetKingdomPartyActive(kingdom, partyId, active);
        }

        public static bool DeactivateKingdomParty(Kingdom kingdom, string partyId)
        {
            return Main.ScenarioBridge.SetKingdomPartyActive(kingdom, partyId, false);
        }

        public static bool ReactivateKingdomParty(Kingdom kingdom, string partyId)
        {
            return Main.ScenarioBridge.SetKingdomPartyActive(kingdom, partyId, true);
        }

        /// <summary>
        /// Assigns a live resident whose citizen ideology matches the party.
        /// A person who already leads another active party is rejected.
        /// </summary>
        public static bool SetKingdomPartyLeader(Kingdom kingdom, string partyId, Actor actor)
        {
            return Main.ScenarioBridge.SetKingdomPartyLeader(kingdom, partyId, actor);
        }

        /// <summary>
        /// Uses Political World's existing leader-selection logic to choose a
        /// suitable leader for the party.
        /// </summary>
        public static bool AssignBestKingdomPartyLeader(Kingdom kingdom, string partyId)
        {
            return Main.ScenarioBridge.AssignBestKingdomPartyLeader(kingdom, partyId);
        }

        /// <summary>
        /// Manually changes the party mandate without forging an election-history
        /// entry. The kingdom must use competitive elections or a one-party system.
        /// </summary>
        public static OperationCheck CheckSetKingdomRulingParty(
            Kingdom kingdom,
            string partyId
        )
        {
            string code = Main.ScenarioBridge.CheckSetKingdomRulingParty(
                kingdom,
                partyId
            );
            return new OperationCheck()
            {
                Allowed = string.Equals(code, "ok", StringComparison.Ordinal),
                Code = code ?? "unknown",
                Message = GetOperationCheckMessage(code)
            };
        }

        public static bool SetKingdomRulingParty(Kingdom kingdom, string partyId)
        {
            return Main.ScenarioBridge.SetKingdomRulingParty(kingdom, partyId);
        }

        public static bool ClearKingdomRulingParty(Kingdom kingdom)
        {
            return Main.ScenarioBridge.ClearKingdomRulingParty(kingdom);
        }

        public static List<string> GetKingdomTags(Kingdom kingdom)
        {
            return Main.ScenarioBridge.GetKingdomTags(kingdom);
        }

        public static bool HasKingdomTag(Kingdom kingdom, string tag)
        {
            return Main.ScenarioBridge.HasKingdomTag(kingdom, tag);
        }

        public static bool AddKingdomTag(Kingdom kingdom, string tag)
        {
            return Main.ScenarioBridge.AddKingdomTag(kingdom, tag);
        }

        public static bool RemoveKingdomTag(Kingdom kingdom, string tag)
        {
            return Main.ScenarioBridge.RemoveKingdomTag(kingdom, tag);
        }

        /// <summary>
        /// Returns private tags owned by one addon. Unlike shared kingdom tags,
        /// these are stored in a collision-safe addon namespace.
        /// </summary>
        public static List<string> GetAddonKingdomTags(Kingdom kingdom, string addonId)
        {
            if (!IsAddonRegistered(addonId)) return new List<string>();
            return Main.ScenarioBridge.GetAddonKingdomTags(kingdom, addonId);
        }

        public static bool HasAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
        {
            return IsAddonRegistered(addonId) &&
                Main.ScenarioBridge.HasAddonKingdomTag(kingdom, addonId, localTag);
        }

        public static bool AddAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
        {
            if (!IsAddonRegistered(addonId)) return false;
            if (string.IsNullOrWhiteSpace(localTag)) return false;
            return Main.ScenarioBridge.AddAddonKingdomTag(kingdom, addonId, localTag);
        }

        public static bool RemoveAddonKingdomTag(Kingdom kingdom, string addonId, string localTag)
        {
            return IsAddonRegistered(addonId) &&
                Main.ScenarioBridge.RemoveAddonKingdomTag(kingdom, addonId, localTag);
        }

        public static int GetKingdomInt(Kingdom kingdom, string addonId, string key, int fallback)
        {
            return Main.ScenarioBridge.GetAddonKingdomInt(kingdom, addonId, key, fallback);
        }

        public static bool SetKingdomInt(Kingdom kingdom, string addonId, string key, int value)
        {
            return IsAddonRegistered(addonId) &&
                Main.ScenarioBridge.SetAddonKingdomInt(kingdom, addonId, key, value);
        }

        public static string GetKingdomString(Kingdom kingdom, string addonId, string key, string fallback)
        {
            return Main.ScenarioBridge.GetAddonKingdomString(kingdom, addonId, key, fallback);
        }

        public static bool SetKingdomString(Kingdom kingdom, string addonId, string key, string value)
        {
            return IsAddonRegistered(addonId) &&
                Main.ScenarioBridge.SetAddonKingdomString(kingdom, addonId, key, value);
        }

        public static bool GetKingdomBool(
            Kingdom kingdom,
            string addonId,
            string key,
            bool fallback
        )
        {
            int encoded = GetKingdomInt(
                kingdom,
                addonId,
                key,
                fallback ? 1 : 0
            );
            return encoded != 0;
        }

        public static bool SetKingdomBool(
            Kingdom kingdom,
            string addonId,
            string key,
            bool value
        )
        {
            return SetKingdomInt(
                kingdom,
                addonId,
                key,
                value ? 1 : 0
            );
        }

        public static float GetKingdomFloat(
            Kingdom kingdom,
            string addonId,
            string key,
            float fallback
        )
        {
            string value = GetKingdomString(
                kingdom,
                addonId,
                key,
                ""
            );
            float parsed;
            if (
                !string.IsNullOrEmpty(value) &&
                float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsed
                )
            )
            {
                return parsed;
            }
            return fallback;
        }

        public static bool SetKingdomFloat(
            Kingdom kingdom,
            string addonId,
            string key,
            float value
        )
        {
            return SetKingdomString(
                kingdom,
                addonId,
                key,
                value.ToString("R", CultureInfo.InvariantCulture)
            );
        }

        public static bool PublishKingdomEvent(Kingdom kingdom, string text, string eventId, float cooldownSeconds)
        {
            return Main.ScenarioBridge.PublishKingdomEvent(kingdom, text, eventId, cooldownSeconds);
        }

        public static bool RegisterAction(string addonId, ActionDefinition definition)
        {
            ValidationResult validation = ValidateAction(addonId, definition);
            if (!validation.IsValid)
            {
                InternalRecordDiagnostic(
                    addonId,
                    "ERROR",
                    "PWDIAG120",
                    validation.Summary
                );
                LogValidationFailure("RegisterAction", validation);
                return false;
            }

            Main.ScenarioBridge.KingdomActionCondition bridgeCondition = null;
            if (definition.Condition != null)
            {
                bridgeCondition = delegate(Kingdom kingdom)
                {
                    return definition.Condition(kingdom);
                };
            }

            Main.ScenarioBridge.KingdomActionHandler bridgeHandler = delegate(Kingdom kingdom)
            {
                definition.Handler(kingdom);
            };

            bool registered = Main.ScenarioBridge.RegisterKingdomActionEx(
                definition.Id,
                definition.Category,
                definition.NameKey,
                definition.DescriptionKey,
                definition.DisplayName,
                definition.Description,
                addonId.Trim(),
                definition.Icon,
                definition.SortOrder,
                bridgeCondition,
                bridgeHandler
            );
            if (registered)
            {
                InternalSeedLocalizationFallback(addonId, definition.NameKey, definition.DisplayName);
                InternalSeedLocalizationFallback(addonId, definition.DescriptionKey, definition.Description);
                InternalRecordActionRegistered(addonId.Trim(), definition.Id);
            }
            return registered;
        }

        public static bool UnregisterAction(string addonId, string actionId)
        {
            if (!IsAddonRegistered(addonId) || !IsOwnedContentId(addonId, actionId))
            {
                return false;
            }
            bool removed = Main.ScenarioBridge.UnregisterKingdomAction(actionId);
            if (removed)
            {
                InternalRecordActionUnregistered(addonId.Trim(), actionId);
            }
            return removed;
        }

        public static ActionInfo GetAction(string actionId, Kingdom kingdom = null)
        {
            Main.ScenarioBridge.KingdomActionInfo item =
                Main.ScenarioBridge.GetKingdomAction(actionId, kingdom);
            return item == null ? null : ConvertAction(item);
        }

        public static List<ActionInfo> GetActionsByAddon(
            string addonId,
            Kingdom kingdom = null
        )
        {
            List<Main.ScenarioBridge.KingdomActionInfo> source =
                Main.ScenarioBridge.GetKingdomActionsBySource(addonId, kingdom);
            return ConvertActions(source);
        }

        public static List<ActionInfo> GetActions(Kingdom kingdom)
        {
            return ConvertActions(Main.ScenarioBridge.GetKingdomActions(kingdom));
        }

        public static bool CanExecuteAction(string actionId, Kingdom kingdom)
        {
            return Main.ScenarioBridge.CanExecuteKingdomAction(actionId, kingdom);
        }

        public static bool ExecuteAction(string actionId, Kingdom kingdom)
        {
            return Main.ScenarioBridge.ExecuteKingdomAction(actionId, kingdom);
        }

        public static partial class Conditions
        {
            public static KingdomCondition All(params KingdomCondition[] conditions)
            {
                return delegate(Kingdom kingdom)
                {
                    if (conditions == null) return true;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && !conditions[i](kingdom))
                        {
                            return false;
                        }
                    }
                    return true;
                };
            }

            public static KingdomCondition Any(params KingdomCondition[] conditions)
            {
                return delegate(Kingdom kingdom)
                {
                    if (conditions == null || conditions.Length == 0) return true;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        if (conditions[i] != null && conditions[i](kingdom))
                        {
                            return true;
                        }
                    }
                    return false;
                };
            }

            public static KingdomCondition Not(KingdomCondition condition)
            {
                return delegate(Kingdom kingdom)
                {
                    return condition == null || !condition(kingdom);
                };
            }

            public static KingdomCondition GovernmentIs(string governmentId)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && string.Equals(state.GovernmentId, governmentId, StringComparison.Ordinal);
                };
            }

            public static KingdomCondition IdeologyIs(string ideologyId)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && string.Equals(state.IdeologyId, ideologyId, StringComparison.Ordinal);
                };
            }

            public static KingdomCondition CurrentIs(string currentId)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && string.Equals(state.CurrentId, currentId, StringComparison.Ordinal);
                };
            }

            public static KingdomCondition PoliticalSystemIs(string systemId)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && string.Equals(state.PoliticalSystemId, systemId, StringComparison.Ordinal);
                };
            }

            public static KingdomCondition StabilityAtLeast(int value)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && state.Stability >= value;
                };
            }

            public static KingdomCondition StabilityAtMost(int value)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null && state.Stability <= value;
                };
            }

            public static KingdomCondition RulerHasTrait(string traitId)
            {
                return delegate(Kingdom kingdom)
                {
                    return PoliticalWorldAPI.RulerHasTrait(kingdom, traitId);
                };
            }

            public static KingdomCondition RulerRaceIs(string raceId)
            {
                return delegate(Kingdom kingdom)
                {
                    return string.Equals(
                        PoliticalWorldAPI.GetRulerRaceId(kingdom),
                        raceId,
                        StringComparison.Ordinal
                    );
                };
            }

            public static KingdomCondition RulerIsImmortal()
            {
                return delegate(Kingdom kingdom)
                {
                    return PoliticalWorldAPI.RulerIsImmortal(kingdom);
                };
            }

            public static KingdomCondition KingdomHasTag(string tag)
            {
                return delegate(Kingdom kingdom)
                {
                    return PoliticalWorldAPI.HasKingdomTag(kingdom, tag);
                };
            }

            public static KingdomCondition IdeologyHasTag(string tag)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    if (state == null) return false;
                    string nodeId = string.IsNullOrEmpty(state.CurrentId)
                        ? state.IdeologyId
                        : state.CurrentId;
                    return PoliticalWorldAPI.HasIdeologyTag(nodeId, tag, true);
                };
            }
        }

        private static ValidationResult NewValidationResult()
        {
            return new ValidationResult() { IsValid = true };
        }

        private static void AddValidationIssue(ValidationResult result, string code, string message, bool isError)
        {
            if (result == null) return;
            if (result.Issues == null) result.Issues = new List<ValidationIssue>();
            result.Issues.Add(new ValidationIssue()
            {
                Code = code ?? "PW000",
                Message = message ?? "",
                IsError = isError
            });
            if (isError) result.IsValid = false;
        }

        private static ValidationResult FinishValidation(ValidationResult result)
        {
            if (result == null) return new ValidationResult() { IsValid = false };
            bool valid = true;
            if (result.Issues != null)
            {
                for (int i = 0; i < result.Issues.Count; i++)
                {
                    if (result.Issues[i] != null && result.Issues[i].IsError)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            result.IsValid = valid;
            return result;
        }

        private static void LogValidationFailure(string operation, ValidationResult result)
        {
            try
            {
                UnityEngine.Debug.LogWarning("[Political World API] " + (operation ?? "Validation") + " failed: " + (result == null ? "unknown validation error" : result.Summary));
            }
            catch
            {
            }
        }

        private static bool IsSafeAddonId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 3 || value.Length > 96) return false;
            if (!IsAsciiLetterOrDigit(value[0]) || !IsAsciiLetterOrDigit(value[value.Length - 1])) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (IsAsciiLetterOrDigit(c) || c == '.' || c == '_' || c == '-') continue;
                return false;
            }
            return true;
        }

        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9');
        }

        private static IdeologyInfo FindIdeology(string ideologyId)
        {
            return GetIdeology(ideologyId);
        }

        private static string GetOperationCheckMessage(string code)
        {
            switch (code)
            {
                case "ok":
                    return "Operation is allowed.";
                case "invalid-kingdom":
                    return "Kingdom is missing or has no data.";
                case "party-mandate-not-supported":
                    return "This political system does not support a ruling-party mandate.";
                case "party-id-required":
                    return "A party Id is required.";
                case "party-not-found":
                    return "The requested party was not found in this kingdom.";
                case "party-inactive":
                    return "The requested party is inactive.";
                default:
                    return "The operation was rejected.";
            }
        }

        private static ActionInfo ConvertAction(
            Main.ScenarioBridge.KingdomActionInfo item
        )
        {
            if (item == null)
            {
                return null;
            }
            return new ActionInfo()
            {
                Id = item.Id,
                Category = item.Category,
                NameKey = item.NameKey,
                DescriptionKey = item.DescriptionKey,
                DisplayName = item.DisplayName,
                Description = item.Description,
                Source = item.Source,
                Icon = item.Icon,
                SortOrder = item.SortOrder,
                Enabled = item.Enabled
            };
        }

        private static List<ActionInfo> ConvertActions(
            List<Main.ScenarioBridge.KingdomActionInfo> source
        )
        {
            List<ActionInfo> result = new List<ActionInfo>();
            if (source == null)
            {
                return result;
            }
            for (int i = 0; i < source.Count; i++)
            {
                ActionInfo info = ConvertAction(source[i]);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        private static PartyInfo ConvertParty(Main.ScenarioBridge.PartyInfo item)
        {
            if (item == null) return null;
            return new PartyInfo()
            {
                Id = item.Id ?? "",
                Name = item.Name ?? "",
                IdeologyId = item.IdeologyId ?? "",
                IdeologyName = item.IdeologyName ?? "",
                LeaderIdentity = item.LeaderIdentity ?? "",
                LeaderName = item.LeaderName ?? "",
                FounderIdentity = item.FounderIdentity ?? "",
                FounderName = item.FounderName ?? "",
                FoundedYear = item.FoundedYear,
                Support = item.Support,
                Radicalism = item.Radicalism,
                Active = item.Active,
                IsRuling = item.IsRuling,
                ColorSeed = item.ColorSeed,
                Position = item.Position ?? "",
                Strategy = item.Strategy ?? "",
                ForeignStance = item.ForeignStance ?? "",
                Traits = item.Traits == null ? new string[0] : (string[])item.Traits.Clone(),
                OriginCityId = item.OriginCityId ?? "",
                OriginCityName = item.OriginCityName ?? "",
                ParentPartyId = item.ParentPartyId ?? "",
                ParentPartyName = item.ParentPartyName ?? ""
            };
        }

        private static IdeologyInfo ConvertIdeology(Main.ScenarioBridge.IdeologyInfo item)
        {
            return new IdeologyInfo()
            {
                Id = item.Id,
                ParentId = item.ParentId,
                RootId = item.RootId,
                NameKey = item.NameKey,
                DisplayName = item.DisplayName,
                DescriptionKey = item.DescriptionKey,
                Description = item.Description,
                Icon = item.Icon,
                SortOrder = item.SortOrder,
                Source = item.Source,
                Tier = item.Tier,
                Tags = item.Tags == null ? new string[0] : (string[])item.Tags.Clone()
            };
        }

        private static bool IsOwnedContentId(string addonId, string contentId)
        {
            if (string.IsNullOrWhiteSpace(addonId) || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }
            string owner = addonId.Trim();
            string id = contentId.Trim();
            return
                id.StartsWith(owner + ".", StringComparison.Ordinal) ||
                id.StartsWith(owner + ":", StringComparison.Ordinal) ||
                id.StartsWith(owner + "_", StringComparison.Ordinal);
        }
    }
}
