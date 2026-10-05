using System;
using System.Collections.Generic;
using NeoModLoader.General;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // 1.10-dev2: keep the original/player name separately, then expose the
        // political name through kingdom.data.name so vanilla WorldBox labels
        // can actually display it. Manual renames are detected and become the
        // new base, avoiding "Republic of Republic of Whatever" nonsense.
        private const string PoliticalCountryNameCacheDataKey =
            "ukiol_country_political_name_cache_v1";

        private const string BaseCountryNameDataKey =
            "ukiol_country_base_name_v1";

        // 1.11.0 fix: "manual" = a player picked the name, the repair must never
        // strip it. "repaired" = the one-time migration already ran.
        // Unwrapping is a guess, so it is capped instead of eating whole names.
        private const string CountryNameSourceDataKey =
            "ukiol_country_base_name_source_v1";

        private const string CountryNameSourceManual = "manual";
        private const string CountryNameSourceRepaired = "repaired";

        private const int CountryNameMaxUnwrapLayers = 4;

        // 1.11-dev1: monarchy rank is derived from the current scale of the
        // state. This intentionally starts simple and deterministic so we can
        // balance it from real long-run worlds before adding prestige/vassals.
        private static string GetMonarchyRankId(Kingdom kingdom)
        {
            if (kingdom == null) return "";

            string government = GetGovernmentPublicId(kingdom) ?? "";
            string baseGovernment = GetGovernmentForm(kingdom) ?? "";
            bool monarchy =
                string.Equals(
                    government,
                    "ukiol_government_absolute_monarchy",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    government,
                    "ukiol_government_constitutional_monarchy",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    baseGovernment,
                    "ukiol_government_absolute_monarchy",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    baseGovernment,
                    "ukiol_government_constitutional_monarchy",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!monarchy) return "";

            int cities = GetCitiesSafe(kingdom).Count;
            int population = GetKingdomPopulationSafe(kingdom);

            // A one-city microstate can grow from a barony into a county
            // without needing to conquer a second settlement.
            if (cities <= 1)
            {
                return population < 90 ? "barony" : "county";
            }

            if (cities <= 3) return "duchy";
            if (cities <= 8) return "kingdom";
            return "empire";
        }

        private static string GetBaseCountryName(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return "";
            }

            string rawName = "";
            try
            {
                rawName = kingdom.data.name == null
                    ? ""
                    : kingdom.data.name.Trim();
            }
            catch
            {
            }

            string storedBase = GetKingdomStringData(
                kingdom,
                BaseCountryNameDataKey,
                ""
            );
            string cachedPolitical = GetKingdomStringData(
                kingdom,
                PoliticalCountryNameCacheDataKey,
                ""
            );

            // First 1.10 pass: remember the vanilla/player name before Political
            // World starts decorating it. This is the name addons should use
            // when they want the underlying kingdom identity.
            if (string.IsNullOrWhiteSpace(storedBase))
            {
                storedBase = !string.IsNullOrWhiteSpace(rawName)
                    ? rawName
                    : GetStableObjectIdentity(kingdom);

                // Load order can hand us a name we already wrapped ("State of
                // X" in an old save). Storing that as the base is what turned it
                // into "National State of State of X" for good.
                storedBase = UnwrapPoliticalNameLayers(
                    storedBase,
                    CountryNameMaxUnwrapLayers
                );

                if (!string.IsNullOrWhiteSpace(storedBase))
                {
                    SetKingdomStringData(
                        kingdom,
                        BaseCountryNameDataKey,
                        storedBase
                    );
                }
                return storedBase ?? "";
            }

            // Player/another mod renamed the kingdom while a political name was
            // active. Treat the new raw name as a new base instead of producing
            // "Republic of Republic of Something".
            if (
                !string.IsNullOrWhiteSpace(rawName) &&
                !string.Equals(rawName, storedBase, StringComparison.Ordinal) &&
                !string.Equals(rawName, cachedPolitical, StringComparison.Ordinal)
            )
            {
                storedBase = rawName;
                SetKingdomStringData(
                    kingdom,
                    BaseCountryNameDataKey,
                    storedBase
                );
                // A human picked this one. The repair must leave it alone.
                SetKingdomStringData(
                    kingdom,
                    CountryNameSourceDataKey,
                    CountryNameSourceManual
                );
            }

            // One-time repair for saves from before the fix. The only evidence
            // an old save still has is its cached display name, so peel only
            // when that cache is this base wrapped by a core template. Hand-typed
            // names are marked manual and never touched.
            if (
                string.IsNullOrEmpty(
                    GetKingdomStringData(kingdom, CountryNameSourceDataKey, "")
                ) &&
                !string.IsNullOrWhiteSpace(storedBase) &&
                !string.IsNullOrWhiteSpace(cachedPolitical)
            )
            {
                string wrappedInner;
                if (
                    TryUnwrapPoliticalName(cachedPolitical, out wrappedInner) &&
                    string.Equals(
                        wrappedInner,
                        storedBase,
                        StringComparison.Ordinal
                    )
                )
                {
                    storedBase = UnwrapPoliticalNameLayers(
                        storedBase,
                        CountryNameMaxUnwrapLayers
                    );
                    SetKingdomStringData(
                        kingdom,
                        BaseCountryNameDataKey,
                        storedBase
                    );
                }

                // Mark it either way: running twice would peel a second layer.
                SetKingdomStringData(
                    kingdom,
                    CountryNameSourceDataKey,
                    CountryNameSourceRepaired
                );
            }

            return storedBase;
        }

        // Peels one of our own wrappers off a name ("National State of X" -> "X").
        // Localized templates work too, because the "{0}" slot is located.
        // Only core templates count as evidence, and we never strip a name down
        // to nothing, so a hand-typed "State of X" survives.
        private static bool TryUnwrapPoliticalName(string name, out string inner)
        {
            inner = "";
            string value = name == null ? "" : name.Trim();
            if (value.Length == 0)
            {
                return false;
            }

            List<PoliticalWorldAPI.CountryNameTemplateInfo> templates =
                PoliticalWorldAPI.Countries.GetNameTemplatesInternal();

            string best = "";
            int bestWeight = -1;

            for (int i = 0; i < templates.Count; i++)
            {
                PoliticalWorldAPI.CountryNameTemplateInfo template = templates[i];
                if (
                    template == null ||
                    !string.Equals(
                        template.SourceAddonId,
                        PoliticalWorldAPI.CoreModId,
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                string candidate;
                int candidateWeight;

                if (
                    TrySplitNameTemplate(
                        template.FallbackTemplate,
                        value,
                        out candidate,
                        out candidateWeight
                    ) &&
                    candidateWeight > bestWeight
                )
                {
                    best = candidate;
                    bestWeight = candidateWeight;
                }

                if (
                    TrySplitNameTemplate(
                        PoliticalWorldAPI.ResolveLocalization(
                            template.NameKey,
                            template.FallbackTemplate
                        ),
                        value,
                        out candidate,
                        out candidateWeight
                    ) &&
                    candidateWeight > bestWeight
                )
                {
                    best = candidate;
                    bestWeight = candidateWeight;
                }
            }

            if (bestWeight < 0)
            {
                return false;
            }

            inner = best;
            return true;
        }

        // Longest affix wins. Localized templates overlap: "{0}共和国" would
        // otherwise leave "人民" glued onto a "人民共和国" name.
        private static bool TrySplitNameTemplate(
            string format,
            string value,
            out string inner,
            out int weight
        )
        {
            inner = "";
            weight = 0;
            if (string.IsNullOrWhiteSpace(format))
            {
                return false;
            }

            int marker = format.IndexOf("{0}", StringComparison.Ordinal);
            if (marker < 0)
            {
                return false;
            }

            // The stored name is trimmed, so the spaces around {0} have to go too.
            string prefix = format.Substring(0, marker).TrimEnd();
            string suffix = format.Substring(marker + 3).TrimStart();

            // A template that is just "{0}" adds nothing and would match everything.
            if (prefix.Length == 0 && suffix.Length == 0)
            {
                return false;
            }

            if (value.Length <= prefix.Length + suffix.Length)
            {
                return false;
            }

            if (
                prefix.Length > 0 &&
                !value.StartsWith(prefix, StringComparison.Ordinal)
            )
            {
                return false;
            }

            if (
                suffix.Length > 0 &&
                !value.EndsWith(suffix, StringComparison.Ordinal)
            )
            {
                return false;
            }

            inner = value
                .Substring(
                    prefix.Length,
                    value.Length - prefix.Length - suffix.Length
                )
                .Trim();

            if (inner.Length == 0)
            {
                return false;
            }

            weight = prefix.Length + suffix.Length;
            return true;
        }

        private static string UnwrapPoliticalNameLayers(string name, int maxLayers)
        {
            string current = name ?? "";
            for (int i = 0; i < maxLayers; i++)
            {
                string next;
                if (!TryUnwrapPoliticalName(current, out next))
                {
                    break;
                }
                current = next;
            }
            return current;
        }

        private static string GetKingdomRaceIdForPolitics(Kingdom kingdom)
        {
            if (kingdom == null) return "";

            string raceId = GetActorRaceIdSafe(GetLivingRuler(kingdom));
            if (!string.IsNullOrWhiteSpace(raceId)) return raceId;

            // Rulers can briefly be null during succession. Try the kingdom
            // template itself before falling back to a neutral profile.
            object directRace = GetMemberValue(
                kingdom,
                "race",
                "_race",
                "species",
                "_species"
            );
            raceId = GetObjectIdStringSafe(directRace);
            if (!string.IsNullOrWhiteSpace(raceId)) return raceId;

            object asset = GetMemberValue(kingdom, "asset", "_asset");
            raceId = GetObjectIdStringSafe(asset);
            return raceId ?? "";
        }

        private static string GetPoliticalCountryDisplayName(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return "";
            }

            string baseName = GetBaseCountryName(kingdom);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return "";
            }

            string ideology = GetStateIdeology(kingdom) ?? "";
            string current = GetStateIdeologyCurrent(kingdom) ?? "";
            string government = GetGovernmentPublicId(kingdom) ?? "";
            string baseGovernment = GetGovernmentForm(kingdom) ?? "";
            string raceId = GetKingdomRaceIdForPolitics(kingdom);

            List<PoliticalWorldAPI.CountryNameTemplateInfo> templates =
                PoliticalWorldAPI.Countries.GetNameTemplatesInternal();

            for (int i = 0; i < templates.Count; i++)
            {
                PoliticalWorldAPI.CountryNameTemplateInfo template = templates[i];
                if (
                    template == null ||
                    !CountryNameTemplateMatches(
                        template,
                        raceId,
                        government,
                        baseGovernment,
                        ideology,
                        current,
                        GetMonarchyRankId(kingdom)
                    )
                )
                {
                    continue;
                }

                string format = PoliticalWorldAPI.ResolveLocalization(
                    template.NameKey,
                    template.FallbackTemplate
                );
                if (string.IsNullOrWhiteSpace(format))
                {
                    continue;
                }

                try
                {
                    string name = string.Format(format, baseName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }
                catch
                {
                    // Bad third-party template? Skip it instead of breaking UI.
                }
            }

            return baseName;
        }

        private static bool CountryNameTemplateMatches(
            PoliticalWorldAPI.CountryNameTemplateInfo template,
            string raceId,
            string government,
            string baseGovernment,
            string ideology,
            string current,
            string monarchyRank
        )
        {
            if (template == null) return false;
            if (!StringListMatches(template.RaceIds, raceId)) return false;
            if (
                !StringListMatches(template.GovernmentIds, government) &&
                !StringListMatches(template.GovernmentIds, baseGovernment)
            ) return false;
            if (!StringListMatches(template.IdeologyIds, ideology)) return false;
            if (!StringListMatches(template.CurrentIds, current)) return false;
            if (!StringListMatches(template.RankIds, monarchyRank)) return false;

            string[] requiredTags = template.RequiredRaceTags;
            if (requiredTags != null)
            {
                for (int i = 0; i < requiredTags.Length; i++)
                {
                    string tag = requiredTags[i];
                    if (
                        !string.IsNullOrWhiteSpace(tag) &&
                        !PoliticalWorldAPI.Races.HasTag(raceId, tag)
                    )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool StringListMatches(string[] values, string actual)
        {
            if (values == null || values.Length == 0)
            {
                return true;
            }

            string wanted = actual ?? "";
            for (int i = 0; i < values.Length; i++)
            {
                if (
                    string.Equals(
                        values[i] ?? "",
                        wanted,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }
            return false;
        }

        private static string RefreshPoliticalCountryDisplayName(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return "";
            }

            // GetBaseCountryName also notices a manual/player rename before we
            // calculate the new political wrapper.
            GetBaseCountryName(kingdom);

            string currentName = GetPoliticalCountryDisplayName(kingdom);
            string previousName = GetKingdomStringData(
                kingdom,
                PoliticalCountryNameCacheDataKey,
                ""
            );

            SetKingdomStringData(
                kingdom,
                PoliticalCountryNameCacheDataKey,
                currentName
            );

            // dev2: actually expose the political name to vanilla WorldBox UI.
            // The original/player name lives separately in BaseCountryNameDataKey,
            // so changing government/ideology can safely rewrite the visible name.
            try
            {
                if (
                    !string.IsNullOrWhiteSpace(currentName) &&
                    !string.Equals(
                        kingdom.data.name ?? "",
                        currentName,
                        StringComparison.Ordinal
                    )
                )
                {
                    kingdom.data.name = currentName;
                }
            }
            catch
            {
            }

            if (
                !string.IsNullOrEmpty(previousName) &&
                !string.Equals(previousName, currentName, StringComparison.Ordinal)
            )
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.CountryNameChanged,
                    kingdom,
                    oldValue: previousName,
                    newValue: currentName,
                    oldName: previousName,
                    newName: currentName,
                    category: "country-name",
                    year: GetWorldYearSafe(),
                    ideologyId: GetStateIdeology(kingdom) ?? "",
                    currentId: GetStateIdeologyCurrent(kingdom) ?? "",
                    governmentId: GetGovernmentPublicId(kingdom) ?? ""
                );
            }

            return currentName;
        }

        private static void UpdatePoliticalCountryNames()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null || kingdom.data == null || kingdom.isRekt())
                {
                    continue;
                }

                RefreshPoliticalCountryDisplayName(kingdom);
            }
        }
    }
}
