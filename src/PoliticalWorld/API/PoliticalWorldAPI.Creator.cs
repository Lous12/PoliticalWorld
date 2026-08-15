using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Creator-oriented API additions introduced in API 1.9.
    ///
    /// The main goals are:
    /// - localization must be optional;
    /// - English/default text must remain readable on any game language;
    /// - common addon logic should be possible without polling or internal APIs;
    /// - bulk registration/query helpers should reduce boilerplate.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public const string DefaultFallbackLanguage = "en";

        public sealed class OperationResult
        {
            public bool Success;
            public string Code;
            public string Message;
        }

        public sealed class BatchRegistrationResult
        {
            public int Requested;
            public int Registered;
            public List<string> FailedIds = new List<string>();

            public bool AllSucceeded
            {
                get
                {
                    return Registered == Requested &&
                        (FailedIds == null || FailedIds.Count == 0);
                }
            }
        }

        public sealed class AddonContentSummary
        {
            public string AddonId;
            public int Ideologies;
            public int Governments;
            public int Actions;
            public int RarePoliticalEvents;
            public string[] IdeologyIds;
            public string[] GovernmentIds;
            public string[] ActionIds;
            public string[] RarePoliticalEventIds;
        }

        private static readonly Dictionary<string, Dictionary<string, string>>
            RegisteredLocalization =
                new Dictionary<string, Dictionary<string, string>>(
                    StringComparer.OrdinalIgnoreCase
                );

        private static readonly Dictionary<string, string>
            RegisteredLocalizationOwners =
                new Dictionary<string, string>(StringComparer.Ordinal);

        private static HashSet<string> _capabilitySet;

        /// <summary>
        /// Registers one translation owned by an addon.
        ///
        /// This is optional. Addons may instead use literal DisplayName /
        /// Description fallback strings, or normal NeoModLoader locale files.
        /// </summary>
        public static bool RegisterLocalization(
            string addonId,
            string languageId,
            string key,
            string value
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string language = NormalizeLanguageId(languageId);
            string normalizedKey = key == null ? "" : key.Trim();

            if (
                !IsAddonRegistered(owner) ||
                string.IsNullOrEmpty(language) ||
                string.IsNullOrEmpty(normalizedKey) ||
                string.IsNullOrEmpty(value) ||
                !IsOwnedContentId(owner, normalizedKey)
            )
            {
                return false;
            }

            string existingOwner;
            if (
                RegisteredLocalizationOwners.TryGetValue(
                    normalizedKey,
                    out existingOwner
                ) &&
                !string.Equals(existingOwner, owner, StringComparison.Ordinal)
            )
            {
                InternalRecordDiagnostic(
                    owner,
                    "WARN",
                    "PWDIAG190",
                    "Localization key '" + normalizedKey +
                    "' is already owned by addon '" + existingOwner + "'."
                );
                return false;
            }

            Dictionary<string, string> table;
            if (!RegisteredLocalization.TryGetValue(language, out table))
            {
                table = new Dictionary<string, string>(StringComparer.Ordinal);
                RegisteredLocalization[language] = table;
            }

            string normalizedValue = value.Trim();
            table[normalizedKey] = normalizedValue;
            RegisteredLocalizationOwners[normalizedKey] = owner;

            try
            {
                NeoModLoader.General.LM.Add(
                    language,
                    normalizedKey,
                    normalizedValue
                );

                string current = GetCurrentLanguageId();
                if (
                    string.Equals(
                        language,
                        current,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    NeoModLoader.General.LM.AddToCurrentLocale(
                        normalizedKey,
                        normalizedValue
                    );
                }
                else if (
                    string.Equals(
                        language,
                        DefaultFallbackLanguage,
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    !NeoModLoader.General.LM.Has(normalizedKey)
                )
                {
                    // English/default fallback is injected only when the
                    // current language has no translation. A later native or
                    // API translation can still overwrite it.
                    NeoModLoader.General.LM.AddToCurrentLocale(
                        normalizedKey,
                        normalizedValue
                    );
                }
            }
            catch
            {
                // The Political World registry still keeps the translation,
                // so ResolveLocalization continues to work.
            }

            return true;
        }

        public static int RegisterLocalizationPack(
            string addonId,
            string languageId,
            IDictionary<string, string> entries
        )
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int registered = 0;
            foreach (KeyValuePair<string, string> pair in entries)
            {
                if (
                    RegisterLocalization(
                        addonId,
                        languageId,
                        pair.Key,
                        pair.Value
                    )
                )
                {
                    registered++;
                }
            }
            return registered;
        }

        public static int RegisterEnglishLocalization(
            string addonId,
            IDictionary<string, string> entries
        )
        {
            return RegisterLocalizationPack(
                addonId,
                DefaultFallbackLanguage,
                entries
            );
        }

        public static bool HasRegisteredLocalization(
            string languageId,
            string key
        )
        {
            string language = NormalizeLanguageId(languageId);
            string normalizedKey = key == null ? "" : key.Trim();
            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(normalizedKey))
            {
                return false;
            }

            Dictionary<string, string> table;
            return RegisteredLocalization.TryGetValue(language, out table) &&
                table != null &&
                table.ContainsKey(normalizedKey);
        }

        internal static string InternalResolveRegisteredLocalization(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            string current = GetCurrentLanguageId();
            Dictionary<string, string> table;
            string value;

            if (
                !string.IsNullOrEmpty(current) &&
                RegisteredLocalization.TryGetValue(current, out table) &&
                table != null &&
                table.TryGetValue(key, out value) &&
                !string.IsNullOrEmpty(value)
            )
            {
                return value;
            }

            return "";
        }

        internal static string InternalResolveEnglishLocalization(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            Dictionary<string, string> table;
            string value;
            if (
                RegisteredLocalization.TryGetValue(
                    DefaultFallbackLanguage,
                    out table
                ) &&
                table != null &&
                table.TryGetValue(key, out value) &&
                !string.IsNullOrEmpty(value)
            )
            {
                return value;
            }
            return "";
        }

        internal static void InternalSeedLocalizationFallback(
            string addonId,
            string key,
            string fallback
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string normalizedKey = key == null ? "" : key.Trim();
            string normalizedFallback = fallback == null ? "" : fallback.Trim();

            if (
                string.IsNullOrEmpty(owner) ||
                string.IsNullOrEmpty(normalizedKey) ||
                string.IsNullOrEmpty(normalizedFallback) ||
                !IsAddonRegistered(owner) ||
                !IsOwnedContentId(owner, normalizedKey)
            )
            {
                return;
            }

            RegisterLocalization(
                owner,
                DefaultFallbackLanguage,
                normalizedKey,
                normalizedFallback
            );
        }

        private static string NormalizeLanguageId(string languageId)
        {
            string value = languageId == null ? "" : languageId.Trim();
            if (value.Length < 2 || value.Length > 24)
            {
                return "";
            }
            return value;
        }

        private static string GetCurrentLanguageId()
        {
            try
            {
                if (
                    LocalizedTextManager.instance != null &&
                    !string.IsNullOrEmpty(
                        LocalizedTextManager.instance.language
                    )
                )
                {
                    return LocalizedTextManager.instance.language;
                }
            }
            catch
            {
            }
            return "";
        }

        /// <summary>
        /// O(1) capability lookup for creator tools that probe many optional
        /// features while building a UI.
        /// </summary>
        internal static bool InternalHasCapabilityFast(string capability)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                return false;
            }
            if (_capabilitySet == null)
            {
                _capabilitySet = new HashSet<string>(
                    Capabilities,
                    StringComparer.Ordinal
                );
            }
            return _capabilitySet.Contains(capability.Trim());
        }

        public static BatchRegistrationResult RegisterIdeologies(
            string addonId,
            IEnumerable<IdeologyDefinition> definitions
        )
        {
            return RegisterBatch(
                definitions,
                d => d == null ? "" : d.Id,
                d => RegisterIdeology(addonId, d)
            );
        }

        public static BatchRegistrationResult RegisterGovernments(
            string addonId,
            IEnumerable<GovernmentDefinition> definitions
        )
        {
            return RegisterBatch(
                definitions,
                d => d == null ? "" : d.Id,
                d => RegisterGovernment(addonId, d)
            );
        }

        public static BatchRegistrationResult RegisterActions(
            string addonId,
            IEnumerable<ActionDefinition> definitions
        )
        {
            return RegisterBatch(
                definitions,
                d => d == null ? "" : d.Id,
                d => RegisterAction(addonId, d)
            );
        }

        public static BatchRegistrationResult RegisterRarePoliticalEvents(
            string addonId,
            IEnumerable<RarePoliticalEventDefinition> definitions
        )
        {
            return RegisterBatch(
                definitions,
                d => d == null ? "" : d.Id,
                d => RegisterRarePoliticalEvent(addonId, d)
            );
        }

        private static BatchRegistrationResult RegisterBatch<T>(
            IEnumerable<T> definitions,
            Func<T, string> getId,
            Func<T, bool> register
        )
        {
            BatchRegistrationResult result = new BatchRegistrationResult();
            if (definitions == null)
            {
                return result;
            }

            foreach (T definition in definitions)
            {
                result.Requested++;
                bool success = false;
                try
                {
                    success = register != null && register(definition);
                }
                catch
                {
                    success = false;
                }

                if (success)
                {
                    result.Registered++;
                }
                else
                {
                    string id = getId == null ? "" : (getId(definition) ?? "");
                    result.FailedIds.Add(id);
                }
            }
            return result;
        }

        public static AddonContentSummary GetAddonContentSummary(string addonId)
        {
            string owner = addonId == null ? "" : addonId.Trim();

            List<IdeologyInfo> ideologies = GetIdeologiesByAddon(owner);
            List<GovernmentInfo> governments = GetGovernmentsByAddon(owner);
            List<ActionInfo> actions = GetActionsByAddon(owner);
            List<RarePoliticalEventInfo> rare = GetRarePoliticalEventsByAddon(owner);

            return new AddonContentSummary()
            {
                AddonId = owner,
                Ideologies = ideologies.Count,
                Governments = governments.Count,
                Actions = actions.Count,
                RarePoliticalEvents = rare.Count,
                IdeologyIds = ExtractIds(ideologies, x => x.Id),
                GovernmentIds = ExtractIds(governments, x => x.Id),
                ActionIds = ExtractIds(actions, x => x.Id),
                RarePoliticalEventIds = ExtractIds(rare, x => x.Id)
            };
        }

        private static string[] ExtractIds<T>(
            List<T> source,
            Func<T, string> getId
        )
            where T : class
        {
            if (source == null || source.Count == 0)
            {
                return new string[0];
            }

            string[] result = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = getId == null || source[i] == null
                    ? ""
                    : (getId(source[i]) ?? "");
            }
            return result;
        }

        public static List<IdeologyInfo> GetIdeologiesByTag(
            string tag,
            bool includeParentTags = true
        )
        {
            List<IdeologyInfo> result = new List<IdeologyInfo>();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return result;
            }

            List<IdeologyInfo> all = GetIdeologies();
            for (int i = 0; i < all.Count; i++)
            {
                IdeologyInfo info = all[i];
                if (
                    info != null &&
                    HasIdeologyTag(
                        info.Id,
                        tag.Trim(),
                        includeParentTags
                    )
                )
                {
                    result.Add(info);
                }
            }
            return result;
        }

        public static List<GovernmentInfo> GetGovernmentsByTag(string tag)
        {
            List<GovernmentInfo> result = new List<GovernmentInfo>();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return result;
            }

            List<GovernmentInfo> all = GetGovernmentForms();
            for (int i = 0; i < all.Count; i++)
            {
                GovernmentInfo info = all[i];
                if (
                    info != null &&
                    HasGovernmentTag(info.Id, tag.Trim())
                )
                {
                    result.Add(info);
                }
            }
            return result;
        }

        public static List<ActionInfo> GetActionsByCategory(
            string category,
            Kingdom kingdom = null
        )
        {
            List<ActionInfo> result = new List<ActionInfo>();
            string wanted = category == null ? "" : category.Trim();
            if (string.IsNullOrEmpty(wanted))
            {
                return result;
            }

            List<ActionInfo> all = GetActions(kingdom);
            for (int i = 0; i < all.Count; i++)
            {
                ActionInfo info = all[i];
                if (
                    info != null &&
                    string.Equals(
                        info.Category ?? "",
                        wanted,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    result.Add(info);
                }
            }
            return result;
        }

        public static OperationResult TryExecuteAction(
            string actionId,
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return Operation(false, "action-id-required", "Action Id is required.");
            }

            ActionInfo info = GetAction(actionId, kingdom);
            if (info == null)
            {
                return Operation(false, "action-not-found", "The requested action is not registered.");
            }
            if (!info.Enabled)
            {
                return Operation(false, "action-condition-failed", "The action condition is not satisfied.");
            }
            if (!ExecuteAction(actionId, kingdom))
            {
                return Operation(false, "action-execution-failed", "The action handler failed or rejected execution.");
            }
            return Operation(true, "ok", "Action executed.");
        }

        public static OperationResult TryExecuteRarePoliticalEvent(
            string eventId,
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return Operation(false, "event-id-required", "Rare political event Id is required.");
            }
            if (GetRarePoliticalEvent(eventId) == null)
            {
                return Operation(false, "event-not-found", "The requested rare political event is not registered.");
            }
            if (!CanExecuteRarePoliticalEvent(eventId, kingdom))
            {
                return Operation(false, "event-condition-failed", "The rare political event condition is not satisfied.");
            }
            if (!ExecuteRarePoliticalEvent(eventId, kingdom))
            {
                return Operation(false, "event-execution-failed", "The rare political event handler failed.");
            }
            return Operation(true, "ok", "Rare political event executed.");
        }

        public static OperationResult TrySetKingdomIdeology(
            Kingdom kingdom,
            string ideologyId
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }

            IdeologyInfo info = GetIdeology(ideologyId);
            if (info == null)
            {
                return Operation(false, "ideology-not-found", "The requested ideology is not registered.");
            }
            if (info.Tier != 0)
            {
                return Operation(false, "ideology-not-root", "SetKingdomIdeology requires a root ideology.");
            }
            return SetKingdomIdeology(kingdom, ideologyId)
                ? Operation(true, "ok", "Ideology changed.")
                : Operation(false, "rejected", "Political World rejected the ideology change.");
        }

        public static OperationResult TrySetKingdomCurrent(
            Kingdom kingdom,
            string currentId
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }

            IdeologyInfo info = GetIdeology(currentId);
            if (info == null)
            {
                return Operation(false, "current-not-found", "The requested ideology current is not registered.");
            }
            if (info.Tier <= 0)
            {
                return Operation(false, "current-required", "SetKingdomCurrent requires a non-root ideology current.");
            }

            KingdomState state = GetKingdomState(kingdom);
            if (
                state == null ||
                !string.Equals(
                    info.RootId ?? "",
                    state.IdeologyId ?? "",
                    StringComparison.Ordinal
                )
            )
            {
                return Operation(false, "current-root-mismatch", "The current does not belong to the kingdom's root ideology.");
            }

            return SetKingdomCurrent(kingdom, currentId)
                ? Operation(true, "ok", "Ideology current changed.")
                : Operation(false, "rejected", "Political World rejected the ideology-current change.");
        }

        public static OperationResult TrySetKingdomGovernment(
            Kingdom kingdom,
            string governmentId
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }
            if (GetGovernment(governmentId) == null)
            {
                return Operation(false, "government-not-found", "The requested government is not registered.");
            }
            return SetKingdomGovernment(kingdom, governmentId, true)
                ? Operation(true, "ok", "Government changed.")
                : Operation(false, "rejected", "Political World rejected the government change.");
        }

        public static OperationResult TrySetKingdomStability(
            Kingdom kingdom,
            int value
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return Operation(false, "invalid-kingdom", "Kingdom is missing or has no data.");
            }
            return SetKingdomStability(kingdom, value)
                ? Operation(true, "ok", "Stability changed.")
                : Operation(false, "rejected", "Political World rejected the stability change.");
        }

        private static OperationResult Operation(
            bool success,
            string code,
            string message
        )
        {
            return new OperationResult()
            {
                Success = success,
                Code = code ?? "",
                Message = message ?? ""
            };
        }

        public static bool ReportDiagnostic(
            string addonId,
            string severity,
            string code,
            string message
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            if (!IsAddonRegistered(owner))
            {
                return false;
            }

            string level = string.IsNullOrWhiteSpace(severity)
                ? "INFO"
                : severity.Trim().ToUpperInvariant();
            if (level != "INFO" && level != "WARN" && level != "ERROR")
            {
                level = "INFO";
            }

            InternalRecordDiagnostic(
                owner,
                level,
                string.IsNullOrWhiteSpace(code)
                    ? "PWADDON"
                    : code.Trim(),
                message ?? ""
            );
            return true;
        }

        public static int GetPartyInt(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            int fallback
        )
        {
            if (GetKingdomParty(kingdom, partyId, true) == null)
            {
                return fallback;
            }
            return GetKingdomInt(
                kingdom,
                addonId,
                MakePartyDataKey(partyId, key),
                fallback
            );
        }

        public static bool SetPartyInt(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            int value
        )
        {
            return GetKingdomParty(kingdom, partyId, true) != null &&
                SetKingdomInt(
                    kingdom,
                    addonId,
                    MakePartyDataKey(partyId, key),
                    value
                );
        }

        public static string GetPartyString(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            string fallback
        )
        {
            if (GetKingdomParty(kingdom, partyId, true) == null)
            {
                return fallback;
            }
            return GetKingdomString(
                kingdom,
                addonId,
                MakePartyDataKey(partyId, key),
                fallback
            );
        }

        public static bool SetPartyString(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            string value
        )
        {
            return GetKingdomParty(kingdom, partyId, true) != null &&
                SetKingdomString(
                    kingdom,
                    addonId,
                    MakePartyDataKey(partyId, key),
                    value
                );
        }

        public static bool GetPartyBool(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            bool fallback
        )
        {
            if (GetKingdomParty(kingdom, partyId, true) == null)
            {
                return fallback;
            }
            return GetKingdomBool(
                kingdom,
                addonId,
                MakePartyDataKey(partyId, key),
                fallback
            );
        }

        public static bool SetPartyBool(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            bool value
        )
        {
            return GetKingdomParty(kingdom, partyId, true) != null &&
                SetKingdomBool(
                    kingdom,
                    addonId,
                    MakePartyDataKey(partyId, key),
                    value
                );
        }

        public static float GetPartyFloat(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            float fallback
        )
        {
            if (GetKingdomParty(kingdom, partyId, true) == null)
            {
                return fallback;
            }
            return GetKingdomFloat(
                kingdom,
                addonId,
                MakePartyDataKey(partyId, key),
                fallback
            );
        }

        public static bool SetPartyFloat(
            Kingdom kingdom,
            string addonId,
            string partyId,
            string key,
            float value
        )
        {
            return GetKingdomParty(kingdom, partyId, true) != null &&
                SetKingdomFloat(
                    kingdom,
                    addonId,
                    MakePartyDataKey(partyId, key),
                    value
                );
        }

        private static string MakePartyDataKey(
            string partyId,
            string key
        )
        {
            string party = partyId == null ? "" : partyId.Trim();
            string local = key == null ? "" : key.Trim();
            if (string.IsNullOrEmpty(party) || string.IsNullOrEmpty(local))
            {
                return "";
            }
            return "party/" + party + "/" + local;
        }

        /// <summary>
        /// Common condition builders for addons that want event-driven
        /// behavior without implementing their own polling loop.
        /// </summary>
        public static partial class Conditions
        {
            public static KingdomCondition GovernmentHasTag(string tag)
            {
                return delegate(Kingdom kingdom)
                {
                    KingdomState state = GetKingdomState(kingdom);
                    return state != null &&
                        HasGovernmentTag(state.GovernmentId, tag);
                };
            }

            public static KingdomCondition HasRulingParty()
            {
                return delegate(Kingdom kingdom)
                {
                    return GetKingdomRulingParty(kingdom) != null;
                };
            }

            public static KingdomCondition RulingPartyIdeologyIs(
                string ideologyId
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PartyInfo party = GetKingdomRulingParty(kingdom);
                    return party != null &&
                        string.Equals(
                            party.IdeologyId ?? "",
                            ideologyId ?? "",
                            StringComparison.Ordinal
                        );
                };
            }

            public static KingdomCondition HasActivePartyIdeology(
                string ideologyId
            )
            {
                return delegate(Kingdom kingdom)
                {
                    List<PartyInfo> parties =
                        GetKingdomParties(kingdom, false);
                    for (int i = 0; i < parties.Count; i++)
                    {
                        PartyInfo party = parties[i];
                        if (
                            party != null &&
                            party.Active &&
                            string.Equals(
                                party.IdeologyId ?? "",
                                ideologyId ?? "",
                                StringComparison.Ordinal
                            )
                        )
                        {
                            return true;
                        }
                    }
                    return false;
                };
            }

            public static KingdomCondition PartySupportAtLeast(
                string partyId,
                int support
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PartyInfo party = GetKingdomParty(
                        kingdom,
                        partyId,
                        true
                    );
                    return party != null && party.Support >= support;
                };
            }

            public static KingdomCondition AddonIntAtLeast(
                string addonId,
                string key,
                int value
            )
            {
                return delegate(Kingdom kingdom)
                {
                    return GetKingdomInt(
                        kingdom,
                        addonId,
                        key,
                        int.MinValue
                    ) >= value;
                };
            }

            public static KingdomCondition AddonIntAtMost(
                string addonId,
                string key,
                int value
            )
            {
                return delegate(Kingdom kingdom)
                {
                    return GetKingdomInt(
                        kingdom,
                        addonId,
                        key,
                        int.MaxValue
                    ) <= value;
                };
            }

            public static KingdomCondition AddonBoolIs(
                string addonId,
                string key,
                bool value
            )
            {
                return delegate(Kingdom kingdom)
                {
                    return GetKingdomBool(
                        kingdom,
                        addonId,
                        key,
                        !value
                    ) == value;
                };
            }

            public static KingdomCondition KingdomHasAddonTag(
                string addonId,
                string localTag
            )
            {
                return delegate(Kingdom kingdom)
                {
                    return HasAddonKingdomTag(
                        kingdom,
                        addonId,
                        localTag
                    );
                };
            }
        }

        /// <summary>
        /// Common effect builders. They return KingdomAction delegates so they
        /// can be plugged directly into ActionDefinition or
        /// RarePoliticalEventDefinition. No Update loop is created.
        /// </summary>
        public static class Effects
        {
            public static KingdomAction Sequence(params KingdomAction[] effects)
            {
                return delegate(Kingdom kingdom)
                {
                    if (effects == null) return;
                    for (int i = 0; i < effects.Length; i++)
                    {
                        if (effects[i] != null)
                        {
                            effects[i](kingdom);
                        }
                    }
                };
            }

            public static KingdomAction ChangeStability(int delta)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.ChangeKingdomStability(
                        kingdom,
                        delta
                    );
                };
            }

            public static KingdomAction SetStability(int value)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomStability(
                        kingdom,
                        value
                    );
                };
            }

            public static KingdomAction SetIdeology(string ideologyId)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomIdeology(
                        kingdom,
                        ideologyId
                    );
                };
            }

            public static KingdomAction SetCurrent(string currentId)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomCurrent(
                        kingdom,
                        currentId
                    );
                };
            }

            public static KingdomAction SetGovernment(string governmentId)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomGovernment(
                        kingdom,
                        governmentId,
                        true
                    );
                };
            }

            public static KingdomAction AddTag(string tag)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.AddKingdomTag(kingdom, tag);
                };
            }

            public static KingdomAction RemoveTag(string tag)
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.RemoveKingdomTag(kingdom, tag);
                };
            }

            public static KingdomAction AddAddonTag(
                string addonId,
                string localTag
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.AddAddonKingdomTag(
                        kingdom,
                        addonId,
                        localTag
                    );
                };
            }

            public static KingdomAction RemoveAddonTag(
                string addonId,
                string localTag
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.RemoveAddonKingdomTag(
                        kingdom,
                        addonId,
                        localTag
                    );
                };
            }

            public static KingdomAction SetAddonInt(
                string addonId,
                string key,
                int value
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomInt(
                        kingdom,
                        addonId,
                        key,
                        value
                    );
                };
            }

            public static KingdomAction ChangeAddonInt(
                string addonId,
                string key,
                int delta
            )
            {
                return delegate(Kingdom kingdom)
                {
                    int current = PoliticalWorldAPI.GetKingdomInt(
                        kingdom,
                        addonId,
                        key,
                        0
                    );
                    PoliticalWorldAPI.SetKingdomInt(
                        kingdom,
                        addonId,
                        key,
                        current + delta
                    );
                };
            }

            public static KingdomAction SetAddonBool(
                string addonId,
                string key,
                bool value
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomBool(
                        kingdom,
                        addonId,
                        key,
                        value
                    );
                };
            }

            public static KingdomAction PublishEvent(
                string text,
                string eventId,
                float cooldownSeconds = 1f
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.PublishKingdomEvent(
                        kingdom,
                        text,
                        eventId,
                        cooldownSeconds
                    );
                };
            }

            public static KingdomAction SetPartySupport(
                string partyId,
                int support
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomPartySupport(
                        kingdom,
                        partyId,
                        support
                    );
                };
            }

            public static KingdomAction SetPartyRadicalism(
                string partyId,
                int radicalism
            )
            {
                return delegate(Kingdom kingdom)
                {
                    PoliticalWorldAPI.SetKingdomPartyRadicalism(
                        kingdom,
                        partyId,
                        radicalism
                    );
                };
            }
        }
    }
}
