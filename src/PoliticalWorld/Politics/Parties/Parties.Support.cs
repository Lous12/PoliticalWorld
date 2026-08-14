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
        private static void ApplyPartySupportShares(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                parties == null
            )
            {
                return;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                parties[i].Support = 0;
            }

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string ideology = IdeologyIds[i];
                List<PoliticalParty> same =
                    GetPartiesForIdeology(
                        parties,
                        ideology
                    );

                if (same.Count == 0)
                {
                    continue;
                }

                int totalSupport = GetKingdomIdeologySupport(
                    kingdom,
                    ideology
                );

                if (same.Count == 1)
                {
                    same[0].Support = totalSupport;
                    continue;
                }

                int movementRadicalism = GetKingdomIntData(
                    kingdom,
                    MovementRadicalismPrefix +
                        GetMovementKeySuffix(ideology),
                    20
                );
                int totalWeight = 0;
                int bestIndex = 0;
                int bestWeight = -1;
                int[] weights = new int[same.Count];

                for (int p = 0; p < same.Count; p++)
                {
                    int distance = Math.Abs(
                        same[p].Radicalism -
                        movementRadicalism
                    );
                    int alignmentBonus = Math.Max(
                        0,
                        45 - distance
                    );
                    int traitModifier =
                        GetPartyTraitSupportModifier(
                            kingdom,
                            same[p],
                            movementRadicalism
                        );
                    int weight = Math.Max(
                        20,
                        same[p].SupportBias +
                        alignmentBonus +
                        traitModifier
                    );

                    weights[p] = weight;
                    totalWeight += weight;

                    if (weight > bestWeight)
                    {
                        bestWeight = weight;
                        bestIndex = p;
                    }
                }

                int assigned = 0;

                for (int p = 0; p < same.Count; p++)
                {
                    int share = totalWeight <= 0
                        ? 0
                        : (int)Math.Floor(
                            totalSupport *
                            (weights[p] / (float)totalWeight)
                        );

                    same[p].Support = share;
                    assigned += share;
                }

                if (
                    bestIndex >= 0 &&
                    bestIndex < same.Count
                )
                {
                    same[bestIndex].Support +=
                        Math.Max(
                            0,
                            totalSupport - assigned
                        );
                }
            }
        }

        private static int GetPartyCitySupport(
            Kingdom kingdom,
            City city,
            PoliticalParty party,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                city == null ||
                party == null ||
                parties == null
            )
            {
                return 0;
            }

            int stored;
            bool initialized;
            GetLocalPartySupport(
                city,
                party,
                out stored,
                out initialized
            );

            if (initialized)
            {
                return ClampInt(
                    stored,
                    0,
                    GetCityIdeologySupport(
                        city,
                        party.Ideology
                    )
                );
            }

            Dictionary<string, int> targets =
                CalculateCityPartyTargets(
                    kingdom,
                    city,
                    parties,
                    party.Ideology
                );

            int target;
            if (
                targets.TryGetValue(
                    party.Id,
                    out target
                )
            )
            {
                return target;
            }

            return 0;
        }

        private static void UpdateRegionalPartySupport(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                parties == null ||
                parties.Count == 0
            )
            {
                return;
            }

            List<City> cities = GetCitiesSafe(kingdom);

            for (int c = 0; c < cities.Count; c++)
            {
                City city = cities[c];

                if (city == null || city.data == null)
                {
                    continue;
                }

                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string ideology = IdeologyIds[i];
                    List<PoliticalParty> same =
                        GetPartiesForIdeology(
                            parties,
                            ideology
                        );

                    if (same.Count == 0)
                    {
                        continue;
                    }

                    Dictionary<string, int> targets =
                        CalculateCityPartyTargets(
                            kingdom,
                            city,
                            parties,
                            ideology
                        );

                    int cityIdeologySupport =
                        GetCityIdeologySupport(
                            city,
                            ideology
                        );
                    int assigned = 0;
                    int[] nextValues = new int[same.Count];

                    for (int p = 0; p < same.Count; p++)
                    {
                        PoliticalParty party = same[p];
                        int target = 0;
                        targets.TryGetValue(
                            party.Id,
                            out target
                        );

                        int current;
                        bool initialized;
                        GetLocalPartySupport(
                            city,
                            party,
                            out current,
                            out initialized
                        );

                        int next = initialized
                            ? MoveTowardsInt(
                                current,
                                target,
                                LocalPartySupportStep
                            )
                            : target;

                        next = ClampInt(
                            next,
                            0,
                            cityIdeologySupport
                        );
                        nextValues[p] = next;
                        assigned += next;
                    }

                    // A sudden ideology collapse must not leave more party
                    // supporters than citizens of that ideology.
                    if (
                        assigned > cityIdeologySupport &&
                        assigned > 0
                    )
                    {
                        int scaledAssigned = 0;
                        int bestIndex = 0;
                        int bestValue = -1;

                        for (int p = 0; p < nextValues.Length; p++)
                        {
                            int scaled = (int)Math.Floor(
                                nextValues[p] *
                                cityIdeologySupport /
                                (float)assigned
                            );
                            nextValues[p] = Math.Max(0, scaled);
                            scaledAssigned += nextValues[p];

                            if (nextValues[p] > bestValue)
                            {
                                bestValue = nextValues[p];
                                bestIndex = p;
                            }
                        }

                        if (
                            bestIndex >= 0 &&
                            bestIndex < nextValues.Length
                        )
                        {
                            nextValues[bestIndex] += Math.Max(
                                0,
                                cityIdeologySupport -
                                scaledAssigned
                            );
                        }
                    }

                    for (int p = 0; p < same.Count; p++)
                    {
                        SetLocalPartySupport(
                            city,
                            same[p],
                            nextValues[p]
                        );
                    }
                }
            }

            AggregatePartySupportFromCities(
                kingdom,
                parties
            );
        }

        private static Dictionary<string, int>
            CalculateCityPartyTargets(
                Kingdom kingdom,
                City city,
                List<PoliticalParty> parties,
                string ideology
            )
        {
            Dictionary<string, int> result =
                new Dictionary<string, int>();

            if (
                kingdom == null ||
                city == null ||
                parties == null ||
                !IsValidIdeology(ideology)
            )
            {
                return result;
            }

            List<PoliticalParty> same =
                GetPartiesForIdeology(
                    parties,
                    ideology
                );

            if (same.Count == 0)
            {
                return result;
            }

            int ideologySupport =
                GetCityIdeologySupport(
                    city,
                    ideology
                );

            if (ideologySupport <= 0)
            {
                for (int p = 0; p < same.Count; p++)
                {
                    result[same[p].Id] = 0;
                }
                return result;
            }

            int organization = LocalPartyBaseOrganization;
            if (same.Count > 1)
            {
                organization += 2;
            }

            for (int p = 0; p < same.Count; p++)
            {
                if (HasPartyTrait(same[p], PartyTraitMass))
                {
                    organization += 2;
                }
                if (HasPartyTrait(same[p], PartyTraitElite))
                {
                    organization -= 2;
                }
            }

            organization = ClampInt(
                organization,
                72,
                96
            );
            int organizedSupport = ClampInt(
                (int)Math.Round(
                    ideologySupport *
                    organization / 100f
                ),
                0,
                ideologySupport
            );

            int movementRadicalism = GetKingdomIntData(
                kingdom,
                MovementRadicalismPrefix +
                    GetMovementKeySuffix(ideology),
                20
            );

            string cityCurrent = GetCityIdeologyCurrent(city, ideology);
            if (!string.IsNullOrEmpty(cityCurrent))
            {
                int localCurrentRadicalism =
                    GetIdeologyRadicalismScore(cityCurrent);
                movementRadicalism = ClampInt(
                    (int)Math.Round(
                        (movementRadicalism + localCurrentRadicalism) / 2f
                    ),
                    0,
                    100
                );
            }

            string cityId = GetStableObjectIdentity(city);
            int localStability = GetLocalStability(city);
            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            int totalWeight = 0;
            int bestIndex = 0;
            int bestWeight = -1;
            int[] weights = new int[same.Count];

            for (int p = 0; p < same.Count; p++)
            {
                PoliticalParty party = same[p];
                int distance = Math.Abs(
                    party.Radicalism -
                    movementRadicalism
                );
                int alignmentBonus = Math.Max(
                    0,
                    40 - distance
                );
                int weight =
                    party.SupportBias +
                    alignmentBonus +
                    GetPartyTraitSupportModifier(
                        kingdom,
                        party,
                        movementRadicalism
                    );

                int regionalHash = StablePartyHash(
                    cityId +
                    "|" +
                    party.Id +
                    "|regional"
                );
                if (regionalHash == int.MinValue)
                {
                    regionalHash = 0;
                }
                regionalHash = Math.Abs(regionalHash);
                weight +=
                    regionalHash %
                    (LocalPartyRegionalAffinityRange * 2 + 1) -
                    LocalPartyRegionalAffinityRange;

                if (
                    !string.IsNullOrEmpty(party.OriginCityId) &&
                    party.OriginCityId == cityId
                )
                {
                    weight += LocalPartyOriginBonus;
                }

                if (HasPartyTrait(party, PartyTraitMass))
                {
                    weight += 8;
                }
                if (HasPartyTrait(party, PartyTraitDisciplined))
                {
                    weight += 5;
                }
                if (HasPartyTrait(party, PartyTraitPopulist))
                {
                    weight += Math.Max(
                        0,
                        (55 - localStability) / 3
                    );
                }
                if (HasPartyTrait(party, PartyTraitRevolutionary))
                {
                    weight += Math.Max(
                        0,
                        (45 - localStability) / 2
                    );
                }
                if (
                    HasPartyTrait(party, PartyTraitReformist) &&
                    localStability >= 55
                )
                {
                    weight += 8;
                }
                if (
                    HasPartyTrait(party, PartyTraitElite) &&
                    capital == city
                )
                {
                    weight += 12;
                }
                if (HasPartyTrait(party, PartyTraitFactional))
                {
                    weight -= 5;
                }

                int current;
                bool initialized;
                GetLocalPartySupport(
                    city,
                    party,
                    out current,
                    out initialized
                );
                if (initialized)
                {
                    // Inertia makes regional strongholds persist instead of
                    // rerolling every update.
                    weight += current;
                }

                weight = Math.Max(10, weight);
                weights[p] = weight;
                totalWeight += weight;

                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    bestIndex = p;
                }
            }

            int assigned = 0;

            for (int p = 0; p < same.Count; p++)
            {
                int target = totalWeight <= 0
                    ? 0
                    : (int)Math.Floor(
                        organizedSupport *
                        weights[p] /
                        (float)totalWeight
                    );

                result[same[p].Id] = target;
                assigned += target;
            }

            if (
                bestIndex >= 0 &&
                bestIndex < same.Count
            )
            {
                result[same[bestIndex].Id] += Math.Max(
                    0,
                    organizedSupport - assigned
                );
            }

            return result;
        }

        private static void AggregatePartySupportFromCities(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                parties == null
            )
            {
                return;
            }

            List<City> cities = GetCitiesSafe(kingdom);

            for (int p = 0; p < parties.Count; p++)
            {
                PoliticalParty party = parties[p];
                long weighted = 0;
                long totalPopulation = 0;
                int strongholdSupport = -1;
                City strongholdCity = null;

                for (int c = 0; c < cities.Count; c++)
                {
                    City city = cities[c];
                    int population = Math.Max(
                        1,
                        GetCityPopulationSafe(city)
                    );

                    int support;
                    bool initialized;
                    GetLocalPartySupport(
                        city,
                        party,
                        out support,
                        out initialized
                    );

                    if (!initialized)
                    {
                        Dictionary<string, int> targets =
                            CalculateCityPartyTargets(
                                kingdom,
                                city,
                                parties,
                                party.Ideology
                            );
                        targets.TryGetValue(
                            party.Id,
                            out support
                        );
                    }

                    weighted += (long)support * population;
                    totalPopulation += population;

                    if (support > strongholdSupport)
                    {
                        strongholdSupport = support;
                        strongholdCity = city;
                    }
                }

                party.Support = totalPopulation <= 0
                    ? 0
                    : ClampInt(
                        (int)Math.Round(
                            weighted /
                            (double)totalPopulation
                        ),
                        0,
                        100
                    );

                bool meaningfulStronghold =
                    strongholdCity != null &&
                    strongholdSupport >= PartyStrongholdMinimumSupport;

                party.StrongholdSupport = meaningfulStronghold
                    ? Math.Max(0, strongholdSupport)
                    : 0;
                party.StrongholdCityId = meaningfulStronghold
                    ? GetStableObjectIdentity(strongholdCity)
                    : "";
                party.StrongholdCityName = meaningfulStronghold
                    ? GetWorldObjectDisplayName(strongholdCity)
                    : "";
            }
        }

        private static void GetLocalPartySupport(
            City city,
            PoliticalParty party,
            out int support,
            out bool initialized
        )
        {
            support = 0;
            initialized = false;

            if (
                city == null ||
                city.data == null ||
                party == null ||
                string.IsNullOrEmpty(party.Id)
            )
            {
                return;
            }

            string suffix = party.Id;

            initialized =
                GetCityIntData(
                    city,
                    LocalPartySupportInitPrefix + suffix,
                    0
                ) != 0;

            if (initialized)
            {
                support = ClampInt(
                    GetCityIntData(
                        city,
                        LocalPartySupportPrefix + suffix,
                        0
                    ),
                    0,
                    100
                );
            }
        }

        private static void SetLocalPartySupport(
            City city,
            PoliticalParty party,
            int support
        )
        {
            if (
                city == null ||
                city.data == null ||
                party == null ||
                string.IsNullOrEmpty(party.Id)
            )
            {
                return;
            }

            string suffix = party.Id;

            SetCityIntData(
                city,
                LocalPartySupportPrefix + suffix,
                ClampInt(support, 0, 100)
            );
            SetCityIntData(
                city,
                LocalPartySupportInitPrefix + suffix,
                1
            );
        }

        private static void SyncLegacyPartyCompatibility(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                parties == null
            )
            {
                return;
            }

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string ideology = IdeologyIds[i];
                string suffix =
                    GetMovementKeySuffix(ideology);
                PoliticalParty best = null;

                for (int p = 0; p < parties.Count; p++)
                {
                    PoliticalParty candidate =
                        parties[p];

                    if (
                        candidate.Ideology != ideology ||
                        !candidate.Active
                    )
                    {
                        continue;
                    }

                    if (
                        best == null ||
                        candidate.Support > best.Support
                    )
                    {
                        best = candidate;
                    }
                }

                if (best == null)
                {
                    SetKingdomIntData(
                        kingdom,
                        PartyActivePrefix + suffix,
                        0
                    );
                    continue;
                }

                SetKingdomIntData(
                    kingdom,
                    PartyActivePrefix + suffix,
                    1
                );
                SetKingdomIntData(
                    kingdom,
                    PartyNameVariantPrefix + suffix,
                    best.NameVariant
                );
                SetKingdomStringData(
                    kingdom,
                    PartyLeaderNamePrefix + suffix,
                    best.LeaderName
                );
                SetKingdomIntData(
                    kingdom,
                    PartyFoundedYearPrefix + suffix,
                    best.FoundedYear
                );
            }
        }

        private static List<PartySupportHistoryEntry> ClonePartySupportHistory(
            List<PartySupportHistoryEntry> pHistory
        )
        {
            List<PartySupportHistoryEntry> result =
                new List<PartySupportHistoryEntry>();
            if (pHistory == null)
            {
                return result;
            }

            for (int i = 0; i < pHistory.Count; i++)
            {
                PartySupportHistoryEntry source = pHistory[i];
                if (source == null)
                {
                    continue;
                }
                result.Add(
                    new PartySupportHistoryEntry()
                    {
                        Year = source.Year,
                        Support = ClampInt(source.Support, 0, 100)
                    }
                );
            }
            return result;
        }

        private static string SerializePartySupportHistory(
            List<PartySupportHistoryEntry> pHistory
        )
        {
            if (pHistory == null || pHistory.Count == 0)
            {
                return "";
            }

            int start = Math.Max(
                0,
                pHistory.Count - MaxPartySupportHistoryEntries
            );
            List<string> chunks = new List<string>();
            for (int i = start; i < pHistory.Count; i++)
            {
                PartySupportHistoryEntry entry = pHistory[i];
                if (entry == null || entry.Year <= 0)
                {
                    continue;
                }
                chunks.Add(
                    entry.Year + ":" +
                    ClampInt(entry.Support, 0, 100)
                );
            }
            return string.Join(";", chunks.ToArray());
        }

        private static List<PartySupportHistoryEntry> ParsePartySupportHistory(
            string pSerialized
        )
        {
            List<PartySupportHistoryEntry> result =
                new List<PartySupportHistoryEntry>();
            if (string.IsNullOrEmpty(pSerialized))
            {
                return result;
            }

            string[] chunks = pSerialized.Split(';');
            for (int i = 0; i < chunks.Length; i++)
            {
                string chunk = chunks[i];
                if (string.IsNullOrEmpty(chunk))
                {
                    continue;
                }
                string[] parts = chunk.Split(':');
                if (parts.Length != 2)
                {
                    continue;
                }
                int year;
                int support;
                if (
                    !int.TryParse(parts[0], out year) ||
                    !int.TryParse(parts[1], out support) ||
                    year <= 0
                )
                {
                    continue;
                }
                result.Add(
                    new PartySupportHistoryEntry()
                    {
                        Year = year,
                        Support = ClampInt(support, 0, 100)
                    }
                );
            }

            result.Sort(
                delegate(
                    PartySupportHistoryEntry a,
                    PartySupportHistoryEntry b
                )
                {
                    return a.Year.CompareTo(b.Year);
                }
            );
            if (result.Count > MaxPartySupportHistoryEntries)
            {
                result.RemoveRange(
                    0,
                    result.Count - MaxPartySupportHistoryEntries
                );
            }
            return result;
        }

        private static List<PartySupportHistoryEntry> LoadPartySupportHistory(
            Kingdom pKingdom,
            PoliticalParty pParty
        )
        {
            if (
                pKingdom == null ||
                pParty == null ||
                pParty.Slot < 0
            )
            {
                return new List<PartySupportHistoryEntry>();
            }
            return ParsePartySupportHistory(
                GetKingdomStringData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2SupportHistoryPrefix,
                        pParty.Slot
                    ),
                    ""
                )
            );
        }

        private static void SavePartySupportHistory(
            Kingdom pKingdom,
            PoliticalParty pParty
        )
        {
            if (
                pKingdom == null ||
                pParty == null ||
                pParty.Slot < 0
            )
            {
                return;
            }
            if (pParty.SupportHistory == null)
            {
                pParty.SupportHistory =
                    new List<PartySupportHistoryEntry>();
            }
            while (
                pParty.SupportHistory.Count >
                MaxPartySupportHistoryEntries
            )
            {
                pParty.SupportHistory.RemoveAt(0);
            }
            SetKingdomStringData(
                pKingdom,
                PartySlotKey(
                    PartyV2SupportHistoryPrefix,
                    pParty.Slot
                ),
                SerializePartySupportHistory(
                    pParty.SupportHistory
                )
            );
        }

        private static void UpdatePartySupportHistorySnapshots(
            Kingdom pKingdom,
            List<PoliticalParty> pParties
        )
        {
            if (pKingdom == null || pParties == null)
            {
                return;
            }
            int worldYear = GetWorldYearSafe();
            if (worldYear <= 0)
            {
                return;
            }

            for (int i = 0; i < pParties.Count; i++)
            {
                PoliticalParty party = pParties[i];
                if (party == null || party.Slot < 0)
                {
                    continue;
                }
                if (party.SupportHistory == null)
                {
                    party.SupportHistory = LoadPartySupportHistory(
                        pKingdom,
                        party
                    );
                }

                int lastYear = GetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2SupportHistoryLastYearPrefix,
                        party.Slot
                    ),
                    0
                );
                if (
                    lastYear <= 0 &&
                    party.SupportHistory.Count > 0
                )
                {
                    lastYear = party.SupportHistory[
                        party.SupportHistory.Count - 1
                    ].Year;
                }

                bool shouldRecord =
                    party.SupportHistory.Count == 0 ||
                    lastYear <= 0 ||
                    worldYear - lastYear >=
                        PartySupportHistoryIntervalYears;
                if (!shouldRecord)
                {
                    continue;
                }

                // Do not backfill skipped years with invented values. One
                // real current snapshot is enough even after a long fast-forward.
                party.SupportHistory.Add(
                    new PartySupportHistoryEntry()
                    {
                        Year = worldYear,
                        Support = ClampInt(party.Support, 0, 100)
                    }
                );
                while (
                    party.SupportHistory.Count >
                    MaxPartySupportHistoryEntries
                )
                {
                    party.SupportHistory.RemoveAt(0);
                }
                SetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2SupportHistoryLastYearPrefix,
                        party.Slot
                    ),
                    worldYear
                );
                SavePartySupportHistory(pKingdom, party);
            }
        }

        private static bool TryGetPartySupportTrend(
            PartyOverviewEntry pParty,
            out int pDelta,
            out int pBaselineYear
        )
        {
            pDelta = 0;
            pBaselineYear = 0;
            if (
                pParty == null ||
                pParty.SupportHistory == null ||
                pParty.SupportHistory.Count == 0
            )
            {
                return false;
            }

            int worldYear = GetWorldYearSafe();
            if (worldYear <= 0)
            {
                return false;
            }
            int targetYear =
                worldYear - PartySupportHistoryIntervalYears;
            PartySupportHistoryEntry baseline = null;
            for (int i = pParty.SupportHistory.Count - 1; i >= 0; i--)
            {
                PartySupportHistoryEntry entry =
                    pParty.SupportHistory[i];
                if (entry != null && entry.Year <= targetYear)
                {
                    baseline = entry;
                    break;
                }
            }
            if (baseline == null)
            {
                return false;
            }

            pDelta = ClampInt(pParty.Support, 0, 100) -
                ClampInt(baseline.Support, 0, 100);
            pBaselineYear = baseline.Year;
            return true;
        }

        private static string GetPartySupportTrendArrow(int pDelta)
        {
            if (pDelta >= 2)
            {
                return "↑";
            }
            if (pDelta <= -2)
            {
                return "↓";
            }
            return "→";
        }

        private static string FormatPartySupportWithTrend(
            PartyOverviewEntry pParty
        )
        {
            if (pParty == null)
            {
                return "0%";
            }
            int delta;
            int baselineYear;
            if (!TryGetPartySupportTrend(
                pParty,
                out delta,
                out baselineYear
            ))
            {
                return pParty.Support + "%";
            }
            return pParty.Support + "% " +
                GetPartySupportTrendArrow(delta);
        }

        private static string FormatPartySupportTrendValue(
            PartyOverviewEntry pParty
        )
        {
            int delta;
            int baselineYear;
            if (!TryGetPartySupportTrend(
                pParty,
                out delta,
                out baselineYear
            ))
            {
                return LM.Get("ukiol_party_profile_support_no_trend");
            }
            string prefix = delta > 0 ? "+" : "";
            return prefix + delta + "% " +
                GetPartySupportTrendArrow(delta);
        }

        private static Color GetPartySupportTrendColor(
            PartyOverviewEntry pParty
        )
        {
            int delta;
            int baselineYear;
            if (!TryGetPartySupportTrend(
                pParty,
                out delta,
                out baselineYear
            ))
            {
                return new Color(0.70f, 0.71f, 0.67f, 1f);
            }
            if (delta >= 2)
            {
                return new Color(0.38f, 0.92f, 0.44f, 1f);
            }
            if (delta <= -2)
            {
                return new Color(0.94f, 0.40f, 0.40f, 1f);
            }
            return new Color(0.88f, 0.79f, 0.40f, 1f);
        }

        private static int GetWorldYearSafe()
        {
            try
            {
                // WorldBox exposes the elapsed world age through Date.
                // mapStats does not expose a plain public "year" member on
                // every 0.51.x build, which is why dev3 history rows ended up
                // with zero and displayed a dash.
                int dateYear = (int)Date.getYearsSince(0.0);
                if (dateYear > 0)
                {
                    return dateYear;
                }
            }
            catch
            {
            }

            // Compatibility fallback for builds/modded environments that do
            // expose a year-like value through mapStats.
            try
            {
                object world = World.world;
                if (world != null)
                {
                    object mapStats = GetMemberValue(
                        world,
                        "mapStats",
                        "stats"
                    );

                    if (mapStats != null)
                    {
                        object year = GetMemberValue(
                            mapStats,
                            "year",
                            "years",
                            "current_year",
                            "currentYear"
                        );

                        if (year != null)
                        {
                            int parsed;
                            if (
                                int.TryParse(year.ToString(), out parsed) &&
                                parsed > 0
                            )
                            {
                                return parsed;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private static string GetPartyName(
            Kingdom kingdom,
            string ideology
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology)
            )
            {
                return LM.Get("ukiol_party_none");
            }

            List<PoliticalParty> parties =
                GetPoliticalParties(kingdom);
            PoliticalParty best = null;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty candidate = parties[i];

                if (candidate.Ideology != ideology)
                {
                    continue;
                }

                if (
                    best == null ||
                    candidate.Support > best.Support
                )
                {
                    best = candidate;
                }
            }

            return best == null
                ? LM.Get("ukiol_party_none")
                : best.Name;
        }

        private static void GetLeadingPoliticalParty(
            Kingdom kingdom,
            out string ideology,
            out string partyName,
            out string leaderName,
            out int support,
            out int radicalism
        )
        {
            ideology = null;
            partyName = LM.Get("ukiol_party_none");
            leaderName = "";
            support = 0;
            radicalism = 0;

            List<PoliticalParty> parties =
                GetPoliticalParties(kingdom);

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty candidate = parties[i];

                if (
                    candidate == null ||
                    candidate.Support <= support
                )
                {
                    continue;
                }

                ideology = candidate.Ideology;
                partyName = candidate.Name;
                leaderName = candidate.LeaderName;
                support = candidate.Support;
                radicalism = candidate.Radicalism;
            }
        }

        private static void GetLeadingCityPoliticalParty(
            City city,
            out string ideology,
            out string partyName,
            out int support,
            out int secondSupport
        )
        {
            ideology = null;
            partyName = LM.Get("ukiol_party_none");
            support = 0;
            secondSupport = 0;

            if (city == null)
            {
                return;
            }

            Kingdom kingdom = GetKingdomFromObject(city);

            if (kingdom == null)
            {
                return;
            }

            List<PoliticalParty> parties =
                GetPoliticalParties(kingdom);

            GetLeadingCityPoliticalPartyFromList(
                kingdom,
                city,
                parties,
                out ideology,
                out partyName,
                out support,
                out secondSupport
            );
        }

        private static void GetLeadingCityPoliticalPartyFromList(
            Kingdom kingdom,
            City city,
            List<PoliticalParty> parties,
            out string ideology,
            out string partyName,
            out int support,
            out int secondSupport
        )
        {
            ideology = null;
            partyName = LM.Get("ukiol_party_none");
            support = 0;
            secondSupport = 0;

            if (
                kingdom == null ||
                city == null ||
                parties == null
            )
            {
                return;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty candidate = parties[i];
                int candidateSupport =
                    GetPartyCitySupport(
                        kingdom,
                        city,
                        candidate,
                        parties
                    );

                if (candidateSupport > support)
                {
                    secondSupport = support;
                    support = candidateSupport;
                    ideology = candidate.Ideology;
                    partyName = candidate.Name;
                }
                else if (
                    candidateSupport > secondSupport
                )
                {
                    secondSupport = candidateSupport;
                }
            }
        }

        private static PoliticalParty GetStrongholdPartyForCity(
            Kingdom kingdom,
            City city
        )
        {
            if (kingdom == null || city == null)
            {
                return null;
            }

            string cityId = GetStableObjectIdentity(city);
            if (string.IsNullOrEmpty(cityId))
            {
                return null;
            }

            List<PoliticalParty> parties =
                GetPoliticalParties(kingdom);
            PoliticalParty best = null;
            int bestSupport = -1;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (
                    party == null ||
                    party.StrongholdCityId != cityId
                )
                {
                    continue;
                }

                int support = GetPartyCitySupport(
                    kingdom,
                    city,
                    party,
                    parties
                );

                if (support > bestSupport)
                {
                    best = party;
                    bestSupport = support;
                }
            }

            return best;
        }

        private static string FormatPartyWithSupport(
            string partyName,
            int support
        )
        {
            if (
                string.IsNullOrEmpty(partyName) ||
                partyName == LM.Get("ukiol_party_none")
            )
            {
                return LM.Get("ukiol_party_none");
            }

            return partyName + " — " + support + "%";
        }

        private static string FormatPartyLeader(
            string ideology,
            string leaderName
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_party_none");
            }

            return string.IsNullOrEmpty(leaderName)
                ? LM.Get("ukiol_party_leader_unknown")
                : leaderName;
        }

        private static string FormatPartyProfile(
            string ideology,
            int radicalism,
            string stateCourse
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_party_none");
            }

            string moderation;

            if (radicalism >= 65)
            {
                moderation = LM.Get("ukiol_party_position_radical");
            }
            else if (radicalism >= 35)
            {
                moderation = LM.Get("ukiol_party_position_mainstream");
            }
            else
            {
                moderation = LM.Get("ukiol_party_position_moderate");
            }

            string strategy;

            if (radicalism >= 65)
            {
                strategy = LM.Get("ukiol_party_strategy_revolutionary");
            }
            else if (radicalism >= 45)
            {
                strategy = LM.Get("ukiol_party_strategy_protest");
            }
            else
            {
                strategy = LM.Get("ukiol_party_strategy_parliamentary");
            }

            string foreignStance;

            if (stateCourse == MilitaristTraitId)
            {
                foreignStance = LM.Get("ukiol_party_foreign_hawkish");
            }
            else if (stateCourse == DiplomatTraitId)
            {
                foreignStance = LM.Get("ukiol_party_foreign_dovish");
            }
            else
            {
                foreignStance = LM.Get("ukiol_party_foreign_pragmatic");
            }

            return moderation +
                " · " +
                strategy +
                " · " +
                foreignStance;
        }

        private static string FormatPartyEntityDetails(
            PartyOverviewEntry party
        )
        {
            if (party == null)
            {
                return LM.Get("ukiol_party_none");
            }

            string leader = string.IsNullOrEmpty(
                party.Leader
            )
                ? LM.Get("ukiol_party_leader_unknown")
                : party.Leader;
            string profile = FormatPartyStoredProfile(
                party.Position,
                party.Strategy,
                party.ForeignStance
            );
            string founded = party.FoundedYear > 0
                ? string.Format(
                    LM.Get("ukiol_party_founded_short"),
                    party.FoundedYear
                )
                : LM.Get("ukiol_party_founded_unknown");

            return leader +
                " · " +
                profile +
                " · " +
                founded;
        }

        private static string FormatPartyStoredProfile(
            string position,
            string strategy,
            string foreignStance
        )
        {
            string positionText;

            switch (position)
            {
                case "radical":
                    positionText = LM.Get(
                        "ukiol_party_position_radical"
                    );
                    break;
                case "mainstream":
                    positionText = LM.Get(
                        "ukiol_party_position_mainstream"
                    );
                    break;
                default:
                    positionText = LM.Get(
                        "ukiol_party_position_moderate"
                    );
                    break;
            }

            string strategyText;

            switch (strategy)
            {
                case "revolutionary":
                    strategyText = LM.Get(
                        "ukiol_party_strategy_revolutionary"
                    );
                    break;
                case "protest":
                    strategyText = LM.Get(
                        "ukiol_party_strategy_protest"
                    );
                    break;
                default:
                    strategyText = LM.Get(
                        "ukiol_party_strategy_parliamentary"
                    );
                    break;
            }

            string foreignText;

            switch (foreignStance)
            {
                case "hawkish":
                    foreignText = LM.Get(
                        "ukiol_party_foreign_hawkish"
                    );
                    break;
                case "dovish":
                    foreignText = LM.Get(
                        "ukiol_party_foreign_dovish"
                    );
                    break;
                default:
                    foreignText = LM.Get(
                        "ukiol_party_foreign_pragmatic"
                    );
                    break;
            }

            return positionText +
                " · " +
                strategyText +
                " · " +
                foreignText;
        }

        private static string FormatCityPartyCompetition(
            string partyName,
            int support,
            int secondSupport
        )
        {
            if (
                string.IsNullOrEmpty(partyName) ||
                support <= 0
            )
            {
                return LM.Get("ukiol_party_none");
            }

            int margin = Math.Max(0, support - secondSupport);

            if (margin <= 5)
            {
                return partyName +
                    " — " +
                    support +
                    "% (" +
                    LM.Get("ukiol_party_competition_tossup") +
                    ")";
            }

            if (margin <= 15)
            {
                return partyName +
                    " — " +
                    support +
                    "% (" +
                    LM.Get("ukiol_party_competition_competitive") +
                    ")";
            }

            return partyName +
                " — " +
                support +
                "% (" +
                LM.Get("ukiol_party_competition_stronghold") +
                ")";
        }

        private static string GetPartyCompetitionColor(
            int support,
            int secondSupport
        )
        {
            if (support <= 0)
            {
                return "#A0A0A0";
            }

            int margin = Math.Max(0, support - secondSupport);

            if (margin <= 5)
            {
                return "#E8D36A";
            }

            if (margin <= 15)
            {
                return "#B8D36A";
            }

            return "#72E58A";
        }

    }
}
