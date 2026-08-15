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
    public partial class Main
    {
        // Step 9D FAST: state/citizen ideology persistence, city political memory, support, hostility and tension helpers moved unchanged.
        private static bool IsValidIdeology(
            string ideology
        )
        {
            if (string.IsNullOrEmpty(ideology))
            {
                return false;
            }

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                if (IdeologyIds[i] == ideology)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetStateIdeology(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return null;
            }

            string ideology = "";

            try
            {
                kingdom.data.get(
                    StateIdeologyDataKey,
                    out ideology,
                    ""
                );
            }
            catch
            {
                return null;
            }

            return IsValidIdeology(ideology)
                ? ideology
                : null;
        }

        private static bool SetStateIdeology(
            Kingdom kingdom,
            string ideology
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidIdeology(ideology)
            )
            {
                return false;
            }

            try
            {
                string previous = GetStateIdeology(kingdom);
                string previousCurrent = GetStateIdeologyCurrent(kingdom);

                kingdom.data.set(
                    StateIdeologyDataKey,
                    ideology
                );

                if (previous != ideology)
                {
                    ResetStateIdeologyCurrent(kingdom);

                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.IdeologyChanged,
                        kingdom,
                        previous ?? "",
                        ideology
                    );

                    if (!string.IsNullOrEmpty(previousCurrent))
                    {
                        PoliticalWorldAPI.InternalEmitCoreEvent(
                            PoliticalWorldAPI.Events.CurrentChanged,
                            kingdom,
                            previousCurrent,
                            ""
                        );
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not save state ideology: " +
                    exception.Message
                );
                return false;
            }
        }

        private static string GetCitizenIdeology(
            Actor actor
        )
        {
            if (actor == null || actor.data == null)
            {
                return null;
            }

            string ideology = "";

            try
            {
                actor.data.get(
                    CitizenIdeologyDataKey,
                    out ideology,
                    ""
                );
            }
            catch
            {
                return null;
            }

            return IsValidIdeology(ideology)
                ? ideology
                : null;
        }

        private static void SetCitizenIdeology(
            Actor actor,
            string ideology
        )
        {
            if (
                actor == null ||
                actor.data == null ||
                !IsValidIdeology(ideology)
            )
            {
                return;
            }

            try
            {
                actor.data.set(
                    CitizenIdeologyDataKey,
                    ideology
                );
            }
            catch
            {
            }
        }

        private static int GetCitizenIdeologyConviction(
            Actor actor
        )
        {
            if (actor == null || actor.data == null)
            {
                return DefaultIdeologyConviction;
            }

            int conviction = DefaultIdeologyConviction;

            try
            {
                actor.data.get(
                    CitizenIdeologyConvictionDataKey,
                    out conviction,
                    DefaultIdeologyConviction
                );
            }
            catch
            {
                conviction = DefaultIdeologyConviction;
            }

            return ClampInt(conviction, 0, 100);
        }

        private static void SetCitizenIdeologyConviction(
            Actor actor,
            int conviction
        )
        {
            if (actor == null || actor.data == null)
            {
                return;
            }

            try
            {
                actor.data.set(
                    CitizenIdeologyConvictionDataKey,
                    ClampInt(conviction, 0, 100)
                );
            }
            catch
            {
            }
        }

        private static void AdjustCitizenIdeologyConviction(
            Actor actor,
            int delta
        )
        {
            SetCitizenIdeologyConviction(
                actor,
                GetCitizenIdeologyConviction(actor) + delta
            );
        }

        private static string GetInitialStateIdeology(
            Kingdom kingdom,
            Actor ruler,
            string course
        )
        {
            string[] choices;

            if (course == ReformerTraitId)
            {
                choices = new string[]
                {
                    LiberalismIdeologyId,
                    DemocracyIdeologyId,
                    SocialismIdeologyId
                };
            }
            else if (course == MilitaristTraitId)
            {
                choices = new string[]
                {
                    MonarchismIdeologyId,
                    ConservatismIdeologyId,
                    FascismIdeologyId
                };
            }
            else if (course == DiplomatTraitId)
            {
                choices = new string[]
                {
                    DemocracyIdeologyId,
                    LiberalismIdeologyId,
                    ConservatismIdeologyId
                };
            }
            else
            {
                choices = new string[]
                {
                    MonarchismIdeologyId,
                    ConservatismIdeologyId,
                    DemocracyIdeologyId
                };
            }

            string identity = GetStableObjectIdentity(kingdom);
            int hash = string.IsNullOrEmpty(identity)
                ? 0
                : identity.GetHashCode();

            if (hash == int.MinValue)
            {
                hash = 0;
            }

            int index = Math.Abs(hash) % choices.Length;
            return choices[index];
        }

        private static void EnsureAndUpdateCityPoliticalMemory(
            City city,
            Kingdom kingdom,
            string stateIdeology
        )
        {
            if (city == null || city.data == null)
            {
                return;
            }

            int year = GetWorldYearSafe();
            int schema = GetCityIntData(
                city,
                PoliticalMemorySchemaDataKey,
                0
            );

            if (schema < PoliticalMemorySchemaVersion)
            {
                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string ideology = IdeologyIds[i];
                    int initial = GetCityIdeologySupport(
                        city,
                        ideology
                    );
                    SetCityIntData(
                        city,
                        PoliticalMemoryPrefix +
                            GetMovementKeySuffix(ideology),
                        initial
                    );
                }

                string ownerId = GetStableObjectIdentity(kingdom);
                SetCityStringData(
                    city,
                    PoliticalMemoryOwnerDataKey,
                    ownerId
                );
                SetCityIntData(
                    city,
                    PoliticalMemoryOwnerSinceDataKey,
                    year
                );
                SetCityStringData(
                    city,
                    PoliticalMemoryRegimeDataKey,
                    IsValidIdeology(stateIdeology)
                        ? stateIdeology
                        : ""
                );
                SetCityIntData(
                    city,
                    PoliticalMemoryRegimeSinceDataKey,
                    year
                );
                SetCityIntData(
                    city,
                    PoliticalMemoryLastYearDataKey,
                    year
                );
                SetCityIntData(
                    city,
                    PoliticalMemorySchemaDataKey,
                    PoliticalMemorySchemaVersion
                );
                return;
            }

            string currentOwner = GetStableObjectIdentity(kingdom);
            string previousOwner = GetCityStringData(
                city,
                PoliticalMemoryOwnerDataKey,
                ""
            );
            if (previousOwner != currentOwner)
            {
                SetCityStringData(
                    city,
                    PoliticalMemoryOwnerDataKey,
                    currentOwner
                );
                SetCityIntData(
                    city,
                    PoliticalMemoryOwnerSinceDataKey,
                    year
                );
            }

            string previousRegime = GetCityStringData(
                city,
                PoliticalMemoryRegimeDataKey,
                ""
            );
            string currentRegime =
                IsValidIdeology(stateIdeology)
                    ? stateIdeology
                    : "";
            if (previousRegime != currentRegime)
            {
                SetCityStringData(
                    city,
                    PoliticalMemoryRegimeDataKey,
                    currentRegime
                );
                SetCityIntData(
                    city,
                    PoliticalMemoryRegimeSinceDataKey,
                    year
                );
            }

            int lastYear = GetCityIntData(
                city,
                PoliticalMemoryLastYearDataKey,
                year
            );
            if (year <= lastYear)
            {
                return;
            }

            int elapsed = ClampInt(
                year - lastYear,
                1,
                PoliticalMemoryMaxYearsPerTick
            );

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string ideology = IdeologyIds[i];
                string key =
                    PoliticalMemoryPrefix +
                    GetMovementKeySuffix(ideology);
                int currentMemory = ClampInt(
                    GetCityIntData(city, key, 0),
                    0,
                    100
                );
                int localSupport = GetCityIdeologySupport(
                    city,
                    ideology
                );

                int target = (int)Math.Round(
                    localSupport * 0.78f
                );

                if (ideology == currentRegime)
                {
                    // Long-running state institutions leave a slow legacy,
                    // but they never overwrite citizen support directly.
                    target += 22;
                }

                target = ClampInt(target, 0, 100);
                int next = MoveTowardsInt(
                    currentMemory,
                    target,
                    elapsed
                );
                SetCityIntData(
                    city,
                    key,
                    next
                );
            }

            SetCityIntData(
                city,
                PoliticalMemoryLastYearDataKey,
                year
            );
        }

        private static int GetCityPoliticalMemory(
            City city,
            string ideology
        )
        {
            if (
                city == null ||
                !IsValidIdeology(ideology)
            )
            {
                return 0;
            }

            return ClampInt(
                GetCityIntData(
                    city,
                    PoliticalMemoryPrefix +
                        GetMovementKeySuffix(ideology),
                    0
                ),
                0,
                100
            );
        }

        private static void GetDominantCityPoliticalMemory(
            City city,
            out string ideology,
            out int memory
        )
        {
            ideology = null;
            memory = 0;

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                int candidate =
                    GetCityPoliticalMemory(
                        city,
                        IdeologyIds[i]
                    );

                if (candidate > memory)
                {
                    ideology = IdeologyIds[i];
                    memory = candidate;
                }
            }
        }

        private static int GetCityIntData(
            City city,
            string key,
            int fallback
        )
        {
            if (
                city == null ||
                city.data == null ||
                string.IsNullOrEmpty(key)
            )
            {
                return fallback;
            }

            int value = fallback;

            try
            {
                city.data.get(
                    key,
                    out value,
                    fallback
                );
            }
            catch
            {
                value = fallback;
            }

            return value;
        }

        private static void SetCityIntData(
            City city,
            string key,
            int value
        )
        {
            if (
                city == null ||
                city.data == null ||
                string.IsNullOrEmpty(key)
            )
            {
                return;
            }

            try
            {
                city.data.set(
                    key,
                    value
                );
            }
            catch
            {
            }
        }

        private static string GetCityStringData(
            City city,
            string key,
            string fallback
        )
        {
            if (
                city == null ||
                city.data == null ||
                string.IsNullOrEmpty(key)
            )
            {
                return fallback ?? "";
            }

            string value = fallback ?? "";

            try
            {
                city.data.get(
                    key,
                    out value,
                    fallback ?? ""
                );
            }
            catch
            {
                value = fallback ?? "";
            }

            return value ?? "";
        }

        private static void SetCityStringData(
            City city,
            string key,
            string value
        )
        {
            if (
                city == null ||
                city.data == null ||
                string.IsNullOrEmpty(key)
            )
            {
                return;
            }

            try
            {
                city.data.set(
                    key,
                    value ?? ""
                );
            }
            catch
            {
            }
        }

        private static City ChoosePartyOriginCity(
            Kingdom kingdom,
            Actor leader
        )
        {
            if (leader != null)
            {
                City actorCity = GetMemberValue(
                    leader,
                    "city",
                    "_city"
                ) as City;

                if (actorCity != null)
                {
                    Kingdom actorKingdom =
                        GetKingdomFromObject(actorCity);
                    if (actorKingdom == kingdom)
                    {
                        return actorCity;
                    }
                }
            }

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            if (capital != null)
            {
                return capital;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            return cities.Count > 0
                ? cities[0]
                : null;
        }

        private static int GetCityIdeologySupport(
            City city,
            string ideology
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return 0;
            }

            List<Actor> units = GetCityUnitsSafe(city);

            if (units.Count == 0)
            {
                return 0;
            }

            int total = 0;
            int matching = 0;
            int sample = Math.Min(
                IdeologySupportSamplePerCity,
                units.Count
            );
            int step = Math.Max(
                1,
                units.Count / sample
            );

            for (int i = 0;
                i < units.Count && total < sample;
                i += step)
            {
                Actor actor = units[i];

                if (
                    actor == null ||
                    actor.data == null ||
                    !actor.isAlive()
                )
                {
                    continue;
                }

                string current = GetCitizenIdeology(actor);

                if (!IsValidIdeology(current))
                {
                    Kingdom kingdom = GetKingdomFromObject(city);
                    string state = GetStateIdeology(kingdom);
                    current = PickInitialCitizenIdeology(
                        city,
                        kingdom,
                        state
                    );
                    SetCitizenIdeology(actor, current);
                }

                if (!IsValidIdeology(current))
                {
                    continue;
                }

                total++;

                if (current == ideology)
                {
                    matching++;
                }
            }

            if (total == 0)
            {
                return 0;
            }

            return ClampInt(
                (int)Math.Round(
                    matching * 100f / total
                ),
                0,
                100
            );
        }

        private static int GetKingdomIdeologySupport(
            Kingdom kingdom,
            string ideology
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology)
            )
            {
                return 0;
            }

            List<City> cities = GetCitiesSafe(kingdom);

            if (cities.Count == 0)
            {
                return 0;
            }

            long weighted = 0;
            long totalWeight = 0;

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                int population = Math.Max(
                    1,
                    GetCityPopulationSafe(city)
                );
                int support = GetCityIdeologySupport(
                    city,
                    ideology
                );

                weighted += (long)support * population;
                totalWeight += population;
            }

            if (totalWeight <= 0)
            {
                return 0;
            }

            return ClampInt(
                (int)Math.Round(
                    weighted / (double)totalWeight
                ),
                0,
                100
            );
        }

        private static string FormatStateIdeology(
            string ideology,
            int support
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            return GetIdeologyName(ideology) +
                " — " +
                LM.Get("ukiol_ideology_support_word") +
                " " +
                support.ToString() +
                "%";
        }

        private static void GetCityIdeologyOverview(
            City city,
            out string dominantIdeology,
            out int dominantSupport,
            out int ideologicalTension
        )
        {
            dominantIdeology = null;
            dominantSupport = 0;
            ideologicalTension = 0;

            List<Actor> units = GetCityUnitsSafe(city);

            if (units.Count == 0)
            {
                return;
            }

            int[] counts = new int[IdeologyIds.Length];
            int total = 0;
            int sample = Math.Min(
                IdeologySupportSamplePerCity,
                units.Count
            );
            int step = Math.Max(
                1,
                units.Count / sample
            );
            Kingdom kingdom = GetKingdomFromObject(city);
            string stateIdeology = GetStateIdeology(kingdom);

            for (
                int i = 0;
                i < units.Count && total < sample;
                i += step
            )
            {
                Actor actor = units[i];

                if (
                    actor == null ||
                    actor.data == null ||
                    !actor.isAlive()
                )
                {
                    continue;
                }

                string ideology = GetCitizenIdeology(actor);

                if (!IsValidIdeology(ideology))
                {
                    ideology = PickInitialCitizenIdeology(
                        city,
                        kingdom,
                        stateIdeology
                    );
                    SetCitizenIdeology(actor, ideology);
                }

                int ideologyIndex = GetIdeologyIndex(ideology);

                if (ideologyIndex >= 0)
                {
                    counts[ideologyIndex]++;
                    total++;
                }
            }

            if (total <= 0)
            {
                return;
            }

            int dominantIndex = -1;
            int dominantCount = -1;

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > dominantCount)
                {
                    dominantCount = counts[i];
                    dominantIndex = i;
                }
            }

            if (dominantIndex < 0)
            {
                return;
            }

            dominantIdeology = IdeologyIds[dominantIndex];
            dominantSupport = ClampInt(
                (int)Math.Round(
                    dominantCount * 100f / total
                ),
                0,
                100
            );

            int tension = 100 - dominantSupport;

            if (IsValidIdeology(stateIdeology))
            {
                int stateIndex = GetIdeologyIndex(stateIdeology);
                int stateSupport = 0;

                if (stateIndex >= 0)
                {
                    stateSupport = ClampInt(
                        (int)Math.Round(
                            counts[stateIndex] * 100f / total
                        ),
                        0,
                        100
                    );
                }

                if (stateSupport < 25)
                {
                    tension += 15;
                }
                else if (stateSupport < 40)
                {
                    tension += 8;
                }

                if (dominantIdeology != stateIdeology)
                {
                    tension += Math.Max(
                        5,
                        (dominantSupport - stateSupport) / 2
                    );
                }
            }

            ideologicalTension = ClampInt(
                tension,
                0,
                100
            );
        }

        private static int GetIdeologyHostility(
            string ideologyA,
            string ideologyB
        )
        {
            if (
                !IsValidIdeology(ideologyA) ||
                !IsValidIdeology(ideologyB) ||
                ideologyA == ideologyB
            )
            {
                return 0;
            }

            float similarity = GetIdeologySimilarity(ideologyA, ideologyB);
            int hostility = (int)Math.Round((1f - similarity) * 70f);

            if (IsIdeologyPair(ideologyA, ideologyB, CommunismIdeologyId, FascismIdeologyId))
            {
                hostility += 35;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, AnarchismIdeologyId, FascismIdeologyId))
            {
                hostility += 35;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, AnarchismIdeologyId, MonarchismIdeologyId))
            {
                hostility += 28;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, LiberalismIdeologyId, FascismIdeologyId))
            {
                hostility += 24;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, DemocracyIdeologyId, FascismIdeologyId))
            {
                hostility += 24;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, SyndicalismIdeologyId, FascismIdeologyId))
            {
                hostility += 24;
            }

            if (IsIdeologyPair(ideologyA, ideologyB, MonarchismIdeologyId, ConservatismIdeologyId))
            {
                hostility -= 18;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, LiberalismIdeologyId, DemocracyIdeologyId))
            {
                hostility -= 20;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, SocialismIdeologyId, CommunismIdeologyId))
            {
                hostility -= 10;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, SocialismIdeologyId, SyndicalismIdeologyId))
            {
                hostility -= 12;
            }
            else if (IsIdeologyPair(ideologyA, ideologyB, AnarchismIdeologyId, SyndicalismIdeologyId))
            {
                hostility -= 10;
            }

            return ClampInt(hostility, 0, 100);
        }

        private static bool IsIdeologyPair(
            string ideologyA,
            string ideologyB,
            string pairA,
            string pairB
        )
        {
            return
                (ideologyA == pairA && ideologyB == pairB) ||
                (ideologyA == pairB && ideologyB == pairA);
        }

        private static int GetCityIdeologicalHostility(City city)
        {
            List<Actor> units = GetCityUnitsSafe(city);
            if (units.Count == 0)
            {
                return 0;
            }

            int[] counts = new int[IdeologyIds.Length];
            int total = 0;
            int sample = Math.Min(IdeologySupportSamplePerCity, units.Count);
            int step = Math.Max(1, units.Count / sample);
            Kingdom kingdom = GetKingdomFromObject(city);
            string stateIdeology = GetStateIdeology(kingdom);

            for (int i = 0; i < units.Count && total < sample; i += step)
            {
                Actor actor = units[i];
                if (
                    actor == null ||
                    actor.data == null ||
                    !actor.isAlive()
                )
                {
                    continue;
                }

                string ideology = GetCitizenIdeology(actor);
                if (!IsValidIdeology(ideology))
                {
                    ideology = PickInitialCitizenIdeology(
                        city,
                        kingdom,
                        stateIdeology
                    );
                    SetCitizenIdeology(actor, ideology);
                }

                int index = GetIdeologyIndex(ideology);
                if (index >= 0)
                {
                    counts[index]++;
                    total++;
                }
            }

            if (total <= 1)
            {
                return 0;
            }

            double score = 0.0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] <= 0) continue;
                double shareI = counts[i] / (double)total;

                for (int j = i + 1; j < counts.Length; j++)
                {
                    if (counts[j] <= 0) continue;
                    double shareJ = counts[j] / (double)total;
                    int pairHostility = GetIdeologyHostility(
                        IdeologyIds[i],
                        IdeologyIds[j]
                    );

                    score +=
                        4.0 * shareI * shareJ * pairHostility;
                }
            }

            return ClampInt((int)Math.Round(score), 0, 100);
        }

        private static string FormatIdeologicalHostility(int hostility)
        {
            return hostility + "% — " + GetIdeologicalHostilityText(hostility);
        }

        private static string GetIdeologicalHostilityText(int hostility)
        {
            if (hostility <= 20) return LM.Get("ukiol_ideological_hostility_low");
            if (hostility <= 40) return LM.Get("ukiol_ideological_hostility_moderate");
            if (hostility <= 60) return LM.Get("ukiol_ideological_hostility_hostile");
            if (hostility <= 80) return LM.Get("ukiol_ideological_hostility_severe");
            return LM.Get("ukiol_ideological_hostility_extreme");
        }

        private static string GetIdeologicalHostilityColor(int hostility)
        {
            if (hostility <= 20) return "#72E58A";
            if (hostility <= 40) return "#E8D36A";
            if (hostility <= 60) return "#FFC14D";
            if (hostility <= 80) return "#FF7A3A";
            return "#FF3A3A";
        }

        private static int GetCityIdeologicalTension(
            City city
        )
        {
            string dominantIdeology;
            int dominantSupport;
            int tension;

            GetCityIdeologyOverview(
                city,
                out dominantIdeology,
                out dominantSupport,
                out tension
            );

            return tension;
        }

        private static string FormatCityPoliticalIdentity(
            City city,
            string ideology,
            int support
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            string current = GetCityIdeologyCurrent(city, ideology);
            string name = !string.IsNullOrEmpty(current)
                ? GetIdeologyCurrentName(current)
                : GetIdeologyName(ideology);

            return name +
                " — " +
                support.ToString() +
                "%";
        }

        private static string FormatDominantCityIdeology(
            string ideology,
            int support
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            return GetIdeologyName(ideology) +
                " — " +
                support.ToString() +
                "%";
        }

        private static string FormatCityPoliticalTradition(
            string ideology,
            int memory
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            return GetIdeologyName(ideology) +
                " — " +
                ClampInt(memory, 0, 100).ToString() +
                "%";
        }

        private static string FormatCityStateIdeologySupport(
            string ideology,
            int support
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            return support.ToString() +
                "% — " +
                GetIdeologyName(ideology);
        }

        private static string FormatIdeologicalTension(
            int tension
        )
        {
            return tension.ToString() +
                "% — " +
                GetIdeologicalTensionText(tension);
        }

        private static string GetIdeologicalTensionText(
            int tension
        )
        {
            if (tension <= 20)
            {
                return LM.Get("ukiol_ideological_tension_low");
            }

            if (tension <= 40)
            {
                return LM.Get("ukiol_ideological_tension_moderate");
            }

            if (tension <= 60)
            {
                return LM.Get("ukiol_ideological_tension_noticeable");
            }

            if (tension <= 80)
            {
                return LM.Get("ukiol_ideological_tension_high");
            }

            return LM.Get("ukiol_ideological_tension_critical");
        }

        private static string GetIdeologicalTensionColor(
            int tension
        )
        {
            if (tension <= 20)
            {
                return "#66D17A";
            }

            if (tension <= 40)
            {
                return "#B8D66A";
            }

            if (tension <= 60)
            {
                return "#E8D36A";
            }

            if (tension <= 80)
            {
                return "#E59A4D";
            }

            return "#E75C5C";
        }

        private static string GetIdeologyIconPath(
            string ideology
        )
        {
            switch (ideology)
            {
                case MonarchismIdeologyId:
                    return MonarchismIconPath;
                case ConservatismIdeologyId:
                    return ConservatismIconPath;
                case LiberalismIdeologyId:
                    return LiberalismIconPath;
                case DemocracyIdeologyId:
                    return DemocracyIconPath;
                case SocialismIdeologyId:
                    return SocialismIconPath;
                case CommunismIdeologyId:
                    return CommunismIconPath;
                case FascismIdeologyId:
                    return FascismIconPath;
                case AnarchismIdeologyId:
                    return AnarchismIconPath;
                case SyndicalismIdeologyId:
                    return SyndicalismIconPath;
                default:
                    return "iconLeaders";
            }
        }

        private static int GetIdeologyIndex(
            string ideology
        )
        {
            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                if (IdeologyIds[i] == ideology)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetIdeologyName(
            string ideology
        )
        {
            if (string.IsNullOrEmpty(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            PoliticalWorldAPI.IdeologyInfo info =
                PoliticalWorldAPI.GetIdeology(ideology);
            if (
                info != null &&
                !string.IsNullOrEmpty(info.DisplayName)
            )
            {
                return info.DisplayName;
            }

            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_ideology_none");
            }

            return ideology;
        }

        private static string GetIdeologySupportColor(
            int support
        )
        {
            if (support >= 60)
            {
                return "#43FF43";
            }

            if (support >= 40)
            {
                return "#E8D36A";
            }

            if (support >= 25)
            {
                return "#FF9F43";
            }

            return "#FF5555";
        }

    }
}
