using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// API 1.15 political-expansion surface.
    ///
    /// This file intentionally keeps race/country integration data-driven.
    /// Unknown races remain neutral, while other mods can register their own
    /// political tendencies and country-name templates without Harmony patches.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public sealed class RacePoliticalProfileDefinition
        {
            public string RaceId;
            public string Archetype;
            public string[] Tags;
            public Dictionary<string, float> IdeologyWeights;
        }

        public sealed class RacePoliticalProfileInfo
        {
            public string RaceId;
            public string Archetype;
            public string SourceAddonId;
            public string[] Tags;
            public Dictionary<string, float> IdeologyWeights;
        }

        public sealed class CountryNameTemplateDefinition
        {
            public string Id;
            public string NameKey;
            public string FallbackTemplate;
            public int Priority;
            public string[] RaceIds;
            public string[] GovernmentIds;
            public string[] IdeologyIds;
            public string[] CurrentIds;
            public string[] RankIds;
            public string[] RequiredRaceTags;
        }

        public sealed class CountryNameTemplateInfo
        {
            public string Id;
            public string NameKey;
            public string FallbackTemplate;
            public int Priority;
            public string SourceAddonId;
            public string[] RaceIds;
            public string[] GovernmentIds;
            public string[] IdeologyIds;
            public string[] CurrentIds;
            public string[] RankIds;
            public string[] RequiredRaceTags;
        }

        private static readonly Dictionary<string, RacePoliticalProfileInfo>
            RacePoliticalProfiles = new Dictionary<string, RacePoliticalProfileInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CountryNameTemplateInfo>
            CountryNameTemplates = new Dictionary<string, CountryNameTemplateInfo>(StringComparer.Ordinal);
        private static readonly Dictionary<string, object>
            InteropProviders = new Dictionary<string, object>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string>
            InteropProviderOwners = new Dictionary<string, string>(StringComparer.Ordinal);

        public static class Races
        {
            public static bool RegisterPoliticalProfile(
                string addonId,
                RacePoliticalProfileDefinition definition
            )
            {
                string owner = Trim(addonId);
                if (!IsAddonRegistered(owner) || definition == null)
                {
                    return false;
                }

                string raceId = Trim(definition.RaceId);
                if (string.IsNullOrEmpty(raceId))
                {
                    return false;
                }

                RacePoliticalProfileInfo existing;
                if (
                    RacePoliticalProfiles.TryGetValue(raceId, out existing) &&
                    existing != null &&
                    !string.Equals(existing.SourceAddonId, owner, StringComparison.Ordinal) &&
                    !string.Equals(owner, CoreModId, StringComparison.Ordinal)
                )
                {
                    InternalRecordFrameworkIssue(
                        owner,
                        "WARN",
                        "PWRACE001",
                        "Race political profile is already owned by '" +
                        (existing.SourceAddonId ?? "") + "': " + raceId
                    );
                    return false;
                }

                RacePoliticalProfiles[raceId] = new RacePoliticalProfileInfo()
                {
                    RaceId = raceId,
                    Archetype = string.IsNullOrWhiteSpace(definition.Archetype)
                        ? "neutral"
                        : definition.Archetype.Trim(),
                    SourceAddonId = owner,
                    Tags = NormalizeTags(definition.Tags),
                    IdeologyWeights = NormalizeIdeologyWeights(definition.IdeologyWeights)
                };
                return true;
            }

            public static RacePoliticalProfileInfo GetPoliticalProfile(string raceId)
            {
                RacePoliticalProfileInfo info;
                if (!RacePoliticalProfiles.TryGetValue(Trim(raceId), out info) || info == null)
                {
                    return new RacePoliticalProfileInfo()
                    {
                        RaceId = Trim(raceId),
                        Archetype = "neutral",
                        SourceAddonId = CoreModId,
                        Tags = new string[0],
                        IdeologyWeights = new Dictionary<string, float>(StringComparer.Ordinal)
                    };
                }
                return CloneRacePoliticalProfile(info);
            }

            public static float GetIdeologyWeight(string raceId, string ideologyId)
            {
                RacePoliticalProfileInfo info;
                if (!RacePoliticalProfiles.TryGetValue(Trim(raceId), out info) || info == null)
                {
                    return 1f;
                }

                float weight;
                if (
                    info.IdeologyWeights != null &&
                    info.IdeologyWeights.TryGetValue(Trim(ideologyId), out weight)
                )
                {
                    return ClampWeight(weight);
                }
                return 1f;
            }

            public static bool HasTag(string raceId, string tag)
            {
                string wanted = Trim(tag);
                if (string.IsNullOrEmpty(wanted)) return false;
                RacePoliticalProfileInfo info;
                if (!RacePoliticalProfiles.TryGetValue(Trim(raceId), out info) || info == null || info.Tags == null)
                {
                    return false;
                }
                for (int i = 0; i < info.Tags.Length; i++)
                {
                    if (string.Equals(info.Tags[i], wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }

            public static List<RacePoliticalProfileInfo> GetRegisteredProfiles()
            {
                List<RacePoliticalProfileInfo> result = new List<RacePoliticalProfileInfo>();
                foreach (RacePoliticalProfileInfo info in RacePoliticalProfiles.Values)
                {
                    if (info != null) result.Add(CloneRacePoliticalProfile(info));
                }
                result.Sort(delegate(RacePoliticalProfileInfo a, RacePoliticalProfileInfo b)
                {
                    return string.Compare(a == null ? "" : a.RaceId, b == null ? "" : b.RaceId, StringComparison.OrdinalIgnoreCase);
                });
                return result;
            }
        }

        public enum SettlementSeparatistStage
        {
            None = 0,
            Movement = 1,
            AutonomyCampaign = 2,
            SecessionCrisis = 3
        }

        /// <summary>
        /// Settlement political metrics and separatist-state read access.
        /// </summary>
        public static class Settlements
        {
            public static int GetGovernmentLoyalty(City city)
            {
                return Main.ScenarioBridge.GetSettlementGovernmentLoyalty(city);
            }

            public static int GetSeparatistSentiment(City city)
            {
                return Main.ScenarioBridge.GetSettlementSeparatistSentiment(city);
            }

            public static SettlementSeparatistStage GetSeparatistStage(City city)
            {
                int stage = Main.ScenarioBridge.GetSettlementSeparatistStage(city);
                if (stage < 0) stage = 0;
                if (stage > 3) stage = 3;
                return (SettlementSeparatistStage)stage;
            }

            public static bool HasSeparatistMovement(City city)
            {
                return GetSeparatistStage(city) != SettlementSeparatistStage.None;
            }
        }

        public static class Countries
        {
            public static string GetBaseName(Kingdom kingdom)
            {
                return Main.ScenarioBridge.GetBaseCountryName(kingdom);
            }

            public static string GetPoliticalName(Kingdom kingdom)
            {
                return Main.ScenarioBridge.GetPoliticalCountryName(kingdom);
            }

            /// <summary>
            /// Returns barony/county/duchy/kingdom/empire for monarchies.
            /// Non-monarchies return an empty string.
            /// </summary>
            public static string GetMonarchyRank(Kingdom kingdom)
            {
                return Main.ScenarioBridge.GetMonarchyRank(kingdom);
            }

            /// <summary>
            /// Re-evaluates the dynamic display name and emits
            /// kingdom.country-name.changed when the political name changed.
            /// It does not overwrite the vanilla WorldBox name.
            /// </summary>
            public static string RefreshPoliticalName(Kingdom kingdom)
            {
                return Main.ScenarioBridge.RefreshPoliticalCountryName(kingdom);
            }

            public static bool RegisterNameTemplate(
                string addonId,
                CountryNameTemplateDefinition definition
            )
            {
                string owner = Trim(addonId);
                if (!IsAddonRegistered(owner) || definition == null)
                {
                    return false;
                }

                string id = Trim(definition.Id);
                if (string.IsNullOrEmpty(id) || CountryNameTemplates.ContainsKey(id))
                {
                    return false;
                }

                string nameKey = Trim(definition.NameKey);
                string fallback = definition.FallbackTemplate == null
                    ? ""
                    : definition.FallbackTemplate.Trim();
                if (string.IsNullOrEmpty(nameKey) && string.IsNullOrEmpty(fallback))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(nameKey) && !string.IsNullOrEmpty(fallback))
                {
                    InternalSeedLocalizationFallback(owner, nameKey, fallback);
                }

                CountryNameTemplates[id] = new CountryNameTemplateInfo()
                {
                    Id = id,
                    NameKey = nameKey,
                    FallbackTemplate = fallback,
                    Priority = definition.Priority,
                    SourceAddonId = owner,
                    RaceIds = NormalizeStringArray(definition.RaceIds),
                    GovernmentIds = NormalizeStringArray(definition.GovernmentIds),
                    IdeologyIds = NormalizeStringArray(definition.IdeologyIds),
                    CurrentIds = NormalizeStringArray(definition.CurrentIds),
                    RankIds = NormalizeStringArray(definition.RankIds),
                    RequiredRaceTags = NormalizeStringArray(definition.RequiredRaceTags)
                };
                return true;
            }

            public static List<CountryNameTemplateInfo> GetNameTemplates()
            {
                List<CountryNameTemplateInfo> result = new List<CountryNameTemplateInfo>();
                foreach (CountryNameTemplateInfo info in CountryNameTemplates.Values)
                {
                    if (info != null) result.Add(CloneCountryNameTemplate(info));
                }
                result.Sort(delegate(CountryNameTemplateInfo a, CountryNameTemplateInfo b)
                {
                    int priority = (b == null ? 0 : b.Priority).CompareTo(a == null ? 0 : a.Priority);
                    return priority != 0
                        ? priority
                        : string.CompareOrdinal(a == null ? "" : a.Id, b == null ? "" : b.Id);
                });
                return result;
            }

            internal static List<CountryNameTemplateInfo> GetNameTemplatesInternal()
            {
                return GetNameTemplates();
            }
        }

        /// <summary>
        /// Tiny provider registry for optional cross-mod integration. Political
        /// World never hard-depends on the provider's assembly or interface.
        /// </summary>
        public static class Interop
        {
            public static bool RegisterProvider(string addonId, string providerId, object provider)
            {
                string owner = Trim(addonId);
                string id = Trim(providerId);
                if (!IsAddonRegistered(owner) || string.IsNullOrEmpty(id) || provider == null)
                {
                    return false;
                }
                string existingOwner;
                if (
                    InteropProviderOwners.TryGetValue(id, out existingOwner) &&
                    !string.Equals(existingOwner, owner, StringComparison.Ordinal)
                )
                {
                    return false;
                }
                InteropProviderOwners[id] = owner;
                InteropProviders[id] = provider;
                return true;
            }

            public static object GetProvider(string providerId)
            {
                object provider;
                return InteropProviders.TryGetValue(Trim(providerId), out provider)
                    ? provider
                    : null;
            }

            public static T GetProvider<T>(string providerId) where T : class
            {
                return GetProvider(providerId) as T;
            }

            public static bool HasProvider(string providerId)
            {
                return InteropProviders.ContainsKey(Trim(providerId));
            }

            public static bool RemoveProvider(string addonId, string providerId)
            {
                string owner = Trim(addonId);
                string id = Trim(providerId);
                string existingOwner;
                if (!InteropProviderOwners.TryGetValue(id, out existingOwner) || !string.Equals(existingOwner, owner, StringComparison.Ordinal))
                {
                    return false;
                }
                InteropProviderOwners.Remove(id);
                InteropProviders.Remove(id);
                return true;
            }
        }

        internal static void InternalSeedCorePoliticalProfiles()
        {
            RegisterCoreRaceProfile(
                "human",
                "neutral",
                new string[] { "adaptive" },
                null
            );
            RegisterCoreRaceProfile(
                "orc",
                "martial",
                new string[] { "martial", "nomadic", "clan" },
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "ukiol_ideology_monarchism", 1.20f },
                    { "ukiol_ideology_conservatism", 1.08f },
                    { "ukiol_ideology_liberalism", 0.90f },
                    { "ukiol_ideology_democracy", 0.90f },
                    { "ukiol_ideology_fascism", 1.25f },
                    { "ukiol_current_khanism", 1.60f }
                }
            );
            RegisterCoreRaceProfile(
                "dwarf",
                "mercantile",
                new string[] { "mercantile", "guild", "industrial" },
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "ukiol_ideology_monarchism", 1.08f },
                    { "ukiol_ideology_conservatism", 1.18f },
                    { "ukiol_ideology_liberalism", 1.08f },
                    { "ukiol_ideology_syndicalism", 1.15f },
                    { "ukiol_current_forge_syndicalism", 1.65f },
                    { "ukiol_current_technocratic_democracy", 1.35f }
                }
            );
            RegisterCoreRaceProfile(
                "elf",
                "traditional",
                new string[] { "traditional", "communal", "sylvan" },
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "ukiol_ideology_conservatism", 1.10f },
                    { "ukiol_ideology_liberalism", 1.18f },
                    { "ukiol_ideology_democracy", 1.15f },
                    { "ukiol_ideology_fascism", 0.82f },
                    { "ukiol_current_sylvan_concord", 1.55f }
                }
            );
            // Crablin moment. This is a tendency, not a destiny: crises and
            // actual political history are still much stronger than this bias.
            RegisterCoreRaceProfile(
                "crablin",
                "radical",
                new string[] { "martial", "radical", "crablin" },
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "ukiol_ideology_conservatism", 1.12f },
                    { "ukiol_ideology_liberalism", 0.82f },
                    { "ukiol_ideology_democracy", 0.82f },
                    { "ukiol_ideology_fascism", 1.40f }
                }
            );
        }

        internal static void InternalSeedCoreCountryNameTemplates()
        {
            RegisterCoreCountryTemplate("core.burgundian_order_state", "ukiol_country_name_order_state", "Order State of {0}", 300, null, null, null, new string[] { "ukiol_current_burgundian_system" }, null, null);
            RegisterCoreCountryTemplate("core.orc_khaganate", "ukiol_country_name_orc_khaganate", "Khaganate of {0}", 220, new string[] { "orc" }, new string[] { "ukiol_government_absolute_monarchy" }, null, new string[] { "ukiol_current_khanism" }, null, null);
            RegisterCoreCountryTemplate("core.orc_khanate", "ukiol_country_name_orc_khanate", "Khanate of {0}", 180, new string[] { "orc" }, new string[] { "ukiol_government_absolute_monarchy" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.dwarf_grand_hold", "ukiol_country_name_dwarf_grand_hold", "Grand Hold of {0}", 180, new string[] { "dwarf" }, new string[] { "ukiol_government_absolute_monarchy" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.elf_high_kingdom", "ukiol_country_name_elf_high_kingdom", "High Kingdom of {0}", 180, new string[] { "elf" }, new string[] { "ukiol_government_absolute_monarchy" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.dwarf_forge_republic", "ukiol_country_name_dwarf_forge_republic", "Forge Republic of {0}", 170, new string[] { "dwarf" }, new string[] { "ukiol_government_council_republic" }, null, new string[] { "ukiol_current_forge_syndicalism" }, null, null);
            RegisterCoreCountryTemplate("core.elf_sylvan_commonwealth", "ukiol_country_name_elf_sylvan_commonwealth", "Sylvan Commonwealth of {0}", 170, new string[] { "elf" }, new string[] { "ukiol_government_parliamentary_republic", "ukiol_government_presidential_republic", "ukiol_government_council_republic" }, null, new string[] { "ukiol_current_sylvan_concord" }, null, null);

            // 1.11 monarchy progression. Race-specific monarchy templates above
            // keep their stronger cultural flavor; these rank templates handle
            // humans/unknown races and constitutional monarchies.
            RegisterCoreCountryTemplate("core.monarchy_barony", "ukiol_country_name_barony", "Barony of {0}", 165, null, new string[] { "ukiol_government_absolute_monarchy", "ukiol_government_constitutional_monarchy" }, null, null, new string[] { "barony" }, null);
            RegisterCoreCountryTemplate("core.monarchy_county", "ukiol_country_name_county", "County of {0}", 165, null, new string[] { "ukiol_government_absolute_monarchy", "ukiol_government_constitutional_monarchy" }, null, null, new string[] { "county" }, null);
            RegisterCoreCountryTemplate("core.monarchy_duchy", "ukiol_country_name_duchy", "Duchy of {0}", 165, null, new string[] { "ukiol_government_absolute_monarchy", "ukiol_government_constitutional_monarchy" }, null, null, new string[] { "duchy" }, null);
            RegisterCoreCountryTemplate("core.constitutional_kingdom", "ukiol_country_name_constitutional_kingdom", "Constitutional Kingdom of {0}", 165, null, new string[] { "ukiol_government_constitutional_monarchy" }, null, null, new string[] { "kingdom" }, null);
            RegisterCoreCountryTemplate("core.kingdom", "ukiol_country_name_kingdom", "Kingdom of {0}", 165, null, new string[] { "ukiol_government_absolute_monarchy" }, null, null, new string[] { "kingdom" }, null);
            RegisterCoreCountryTemplate("core.constitutional_empire", "ukiol_country_name_constitutional_empire", "Constitutional Empire of {0}", 165, null, new string[] { "ukiol_government_constitutional_monarchy" }, null, null, new string[] { "empire" }, null);
            RegisterCoreCountryTemplate("core.empire", "ukiol_country_name_empire", "Empire of {0}", 165, null, new string[] { "ukiol_government_absolute_monarchy" }, null, null, new string[] { "empire" }, null);

            RegisterCoreCountryTemplate("core.communist_republic", "ukiol_country_name_peoples_republic", "People's Republic of {0}", 120, null, new string[] { "ukiol_government_parliamentary_republic", "ukiol_government_presidential_republic", "ukiol_government_one_party_state" }, new string[] { "ukiol_ideology_communism" }, null, null, null);
            RegisterCoreCountryTemplate("core.socialist_republic", "ukiol_country_name_social_republic", "Social Republic of {0}", 115, null, new string[] { "ukiol_government_parliamentary_republic", "ukiol_government_presidential_republic" }, new string[] { "ukiol_ideology_socialism" }, null, null, null);
            RegisterCoreCountryTemplate("core.syndicalist_republic", "ukiol_country_name_workers_republic", "Workers' Republic of {0}", 115, null, new string[] { "ukiol_government_parliamentary_republic", "ukiol_government_presidential_republic", "ukiol_government_council_republic" }, new string[] { "ukiol_ideology_syndicalism" }, null, null, null);
            RegisterCoreCountryTemplate("core.anarchist_free_territory", "ukiol_country_name_free_territory", "Free Territory of {0}", 115, null, new string[] { "ukiol_government_council_republic" }, new string[] { "ukiol_ideology_anarchism" }, null, null, null);
            RegisterCoreCountryTemplate("core.fascist_state", "ukiol_country_name_national_state", "National State of {0}", 110, null, new string[] { "ukiol_government_one_party_state", "ukiol_government_military_dictatorship" }, new string[] { "ukiol_ideology_fascism" }, null, null, null);
            RegisterCoreCountryTemplate("core.military_state", "ukiol_country_name_military_state", "Military State of {0}", 100, null, new string[] { "ukiol_government_military_dictatorship" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.council_republic", "ukiol_country_name_council_republic", "Council Republic of {0}", 90, null, new string[] { "ukiol_government_council_republic" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.republic", "ukiol_country_name_republic", "Republic of {0}", 80, null, new string[] { "ukiol_government_parliamentary_republic", "ukiol_government_presidential_republic" }, null, null, null, null);
            RegisterCoreCountryTemplate("core.oligarchic_state", "ukiol_country_name_state", "State of {0}", 20, null, new string[] { "ukiol_government_oligarchy", "ukiol_government_one_party_state" }, null, null, null, null);
        }

        private static void RegisterCoreRaceProfile(string raceId, string archetype, string[] tags, Dictionary<string, float> weights)
        {
            RacePoliticalProfiles[raceId] = new RacePoliticalProfileInfo()
            {
                RaceId = raceId,
                Archetype = archetype,
                SourceAddonId = CoreModId,
                Tags = NormalizeTags(tags),
                IdeologyWeights = NormalizeIdeologyWeights(weights)
            };
        }

        private static void RegisterCoreCountryTemplate(
            string id,
            string nameKey,
            string fallback,
            int priority,
            string[] raceIds,
            string[] governmentIds,
            string[] ideologyIds,
            string[] currentIds,
            string[] rankIds,
            string[] requiredRaceTags
        )
        {
            CountryNameTemplates[id] = new CountryNameTemplateInfo()
            {
                Id = id,
                NameKey = nameKey,
                FallbackTemplate = fallback,
                Priority = priority,
                SourceAddonId = CoreModId,
                RaceIds = NormalizeStringArray(raceIds),
                GovernmentIds = NormalizeStringArray(governmentIds),
                IdeologyIds = NormalizeStringArray(ideologyIds),
                CurrentIds = NormalizeStringArray(currentIds),
                RankIds = NormalizeStringArray(rankIds),
                RequiredRaceTags = NormalizeStringArray(requiredRaceTags)
            };
        }

        private static Dictionary<string, float> NormalizeIdeologyWeights(IDictionary<string, float> source)
        {
            Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.Ordinal);
            if (source == null) return result;
            foreach (KeyValuePair<string, float> pair in source)
            {
                string id = Trim(pair.Key);
                if (string.IsNullOrEmpty(id)) continue;
                result[id] = ClampWeight(pair.Value);
            }
            return result;
        }

        private static float ClampWeight(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
            if (value < 0f) return 0f;
            if (value > 5f) return 5f;
            return value;
        }

        private static string[] NormalizeStringArray(string[] source)
        {
            if (source == null || source.Length == 0) return new string[0];
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < source.Length; i++)
            {
                string value = Trim(source[i]);
                if (!string.IsNullOrEmpty(value) && seen.Add(value)) result.Add(value);
            }
            return result.ToArray();
        }

        private static RacePoliticalProfileInfo CloneRacePoliticalProfile(RacePoliticalProfileInfo source)
        {
            if (source == null) return null;
            return new RacePoliticalProfileInfo()
            {
                RaceId = source.RaceId,
                Archetype = source.Archetype,
                SourceAddonId = source.SourceAddonId,
                Tags = CloneStringArray(source.Tags),
                IdeologyWeights = NormalizeIdeologyWeights(source.IdeologyWeights)
            };
        }

        private static CountryNameTemplateInfo CloneCountryNameTemplate(CountryNameTemplateInfo source)
        {
            if (source == null) return null;
            return new CountryNameTemplateInfo()
            {
                Id = source.Id,
                NameKey = source.NameKey,
                FallbackTemplate = source.FallbackTemplate,
                Priority = source.Priority,
                SourceAddonId = source.SourceAddonId,
                RaceIds = CloneStringArray(source.RaceIds),
                GovernmentIds = CloneStringArray(source.GovernmentIds),
                IdeologyIds = CloneStringArray(source.IdeologyIds),
                CurrentIds = CloneStringArray(source.CurrentIds),
                RankIds = CloneStringArray(source.RankIds),
                RequiredRaceTags = CloneStringArray(source.RequiredRaceTags)
            };
        }
    }
}
