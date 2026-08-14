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
        private static void UpdateCouncilSystem(
            Kingdom kingdom,
            int currentYear
        )
        {
            int nextYear = GetKingdomIntData(
                kingdom,
                CouncilNextYearDataKey,
                0
            );
            if (nextYear <= 0)
            {
                nextYear = Math.Max(1, currentYear + 1);
                SetKingdomIntData(
                    kingdom,
                    CouncilNextYearDataKey,
                    nextYear
                );
            }

            if (
                GetKingdomIntData(
                    kingdom,
                    CouncilDelegateCountDataKey,
                    0
                ) > 0 &&
                string.IsNullOrEmpty(
                    GetKingdomStringData(
                        kingdom,
                        CouncilDelegateIdsDataKey,
                        ""
                    )
                )
            )
            {
                List<CouncilDelegateMember> migratedDelegates =
                    SelectCouncilDelegates(
                        kingdom,
                        CalculateCouncilDelegateCount(kingdom),
                        currentYear
                    );
                SaveCouncilDelegates(kingdom, migratedDelegates);
                SetKingdomIntData(
                    kingdom,
                    CouncilDelegateCountDataKey,
                    migratedDelegates.Count
                );
            }

            if (currentYear < nextYear)
            {
                return;
            }

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            PoliticalParty dominant = null;
            int dominantSupport = -1;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (party == null || !party.Active)
                {
                    continue;
                }

                int support = CalculateElectionPartyVoteShare(
                    kingdom,
                    party,
                    parties
                );
                if (
                    dominant == null ||
                    support > dominantSupport ||
                    (support == dominantSupport &&
                        string.CompareOrdinal(
                            party.Id ?? "",
                            dominant.Id ?? ""
                        ) < 0)
                )
                {
                    dominant = party;
                    dominantSupport = support;
                }
            }

            int desiredDelegates = CalculateCouncilDelegateCount(kingdom);
            List<CouncilDelegateMember> electedDelegates =
                SelectCouncilDelegates(
                    kingdom,
                    desiredDelegates,
                    currentYear
                );
            SaveCouncilDelegates(kingdom, electedDelegates);
            int delegates = electedDelegates.Count;
            SetKingdomIntData(
                kingdom,
                CouncilDelegateCountDataKey,
                delegates
            );
            SetKingdomIntData(
                kingdom,
                CouncilLastYearDataKey,
                currentYear
            );
            SetKingdomIntData(
                kingdom,
                CouncilNextYearDataKey,
                Math.Max(1, currentYear + CouncilTermYears)
            );

            if (dominant != null)
            {
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyIdDataKey,
                    dominant.Id ?? ""
                );
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyNameDataKey,
                    dominant.Name ?? ""
                );
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyIdeologyDataKey,
                    dominant.Ideology ?? ""
                );
                SetKingdomIntData(
                    kingdom,
                    CouncilDominantPartySupportDataKey,
                    ClampInt(dominantSupport, 0, 100)
                );
            }
            else
            {
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyIdDataKey,
                    ""
                );
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyNameDataKey,
                    ""
                );
                SetKingdomStringData(
                    kingdom,
                    CouncilDominantPartyIdeologyDataKey,
                    ""
                );
                SetKingdomIntData(
                    kingdom,
                    CouncilDominantPartySupportDataKey,
                    0
                );
            }

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_council_renewed"),
                    GetWorldObjectDisplayName(kingdom),
                    delegates
                ),
                kingdom,
                null,
                GetLivingRuler(kingdom),
                PartiesIconPath,
                "council_renewed",
                35f
            );
        }

        private static int CalculateCouncilDelegateCount(Kingdom kingdom)
        {
            List<City> cities = GetCitiesSafe(kingdom);
            int population = 0;
            for (int i = 0; i < cities.Count; i++)
            {
                population += Math.Max(
                    0,
                    GetCityPopulationSafe(cities[i])
                );
            }

            int delegates = Math.Max(3, cities.Count * 2);
            delegates += population / 100;
            return ClampInt(delegates, 3, 80);
        }

        private static void ClearCouncilState(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }
            SetKingdomIntData(kingdom, CouncilLastYearDataKey, 0);
            SetKingdomIntData(kingdom, CouncilNextYearDataKey, 0);
            SetKingdomIntData(kingdom, CouncilDelegateCountDataKey, 0);
            SetKingdomStringData(kingdom, CouncilDelegateIdsDataKey, "");
            SetKingdomStringData(kingdom, CouncilDelegateNamesDataKey, "");
            SetKingdomStringData(kingdom, CouncilDelegateCityNamesDataKey, "");
            SetKingdomStringData(kingdom, CouncilDominantPartyIdDataKey, "");
            SetKingdomStringData(kingdom, CouncilDominantPartyNameDataKey, "");
            SetKingdomStringData(kingdom, CouncilDominantPartyIdeologyDataKey, "");
            SetKingdomIntData(kingdom, CouncilDominantPartySupportDataKey, 0);
        }

        private static List<CouncilDelegateMember> SelectCouncilDelegates(
            Kingdom kingdom,
            int desiredCount,
            int currentYear
        )
        {
            List<CouncilDelegateMember> result =
                new List<CouncilDelegateMember>();
            if (kingdom == null || desiredCount <= 0)
            {
                return result;
            }

            HashSet<string> used = new HashSet<string>();
            List<City> cities = GetCitiesSafe(kingdom);
            int totalPopulation = 0;
            for (int i = 0; i < cities.Count; i++)
            {
                totalPopulation += Math.Max(1, GetCityPopulationSafe(cities[i]));
            }

            for (int c = 0; c < cities.Count && result.Count < desiredCount; c++)
            {
                City city = cities[c];
                int population = Math.Max(1, GetCityPopulationSafe(city));
                int quota = totalPopulation > 0
                    ? Math.Max(
                        1,
                        (int)Math.Round(
                            desiredCount * population / (double)totalPopulation
                        )
                    )
                    : 1;
                quota = Math.Min(quota, desiredCount - result.Count);

                List<CouncilDelegateCandidate> candidates =
                    BuildCouncilDelegateCandidates(
                        kingdom,
                        city,
                        currentYear
                    );
                int electedHere = 0;
                for (
                    int i = 0;
                    i < candidates.Count &&
                    electedHere < quota &&
                    result.Count < desiredCount;
                    i++
                )
                {
                    CouncilDelegateCandidate candidate = candidates[i];
                    if (
                        candidate == null ||
                        candidate.Actor == null ||
                        !used.Add(candidate.Identity)
                    )
                    {
                        continue;
                    }
                    result.Add(
                        new CouncilDelegateMember()
                        {
                            Identity = candidate.Identity,
                            Name = candidate.Name,
                            CityName = GetWorldObjectDisplayName(city),
                            Actor = candidate.Actor
                        }
                    );
                    electedHere++;
                }
            }

            // Rounding and tiny cities can leave unfilled seats. Fill the
            // remainder from the strongest unelected candidates anywhere in
            // the kingdom, while keeping every delegate a real living actor.
            if (result.Count < desiredCount)
            {
                List<CouncilDelegateCandidate> reserve =
                    new List<CouncilDelegateCandidate>();
                for (int c = 0; c < cities.Count; c++)
                {
                    reserve.AddRange(
                        BuildCouncilDelegateCandidates(
                            kingdom,
                            cities[c],
                            currentYear
                        )
                    );
                }
                reserve.Sort(
                    delegate(
                        CouncilDelegateCandidate a,
                        CouncilDelegateCandidate b
                    )
                    {
                        int cmp = b.Score.CompareTo(a.Score);
                        if (cmp != 0)
                        {
                            return cmp;
                        }
                        return string.CompareOrdinal(
                            a.Identity ?? "",
                            b.Identity ?? ""
                        );
                    }
                );

                for (
                    int i = 0;
                    i < reserve.Count && result.Count < desiredCount;
                    i++
                )
                {
                    CouncilDelegateCandidate candidate = reserve[i];
                    if (
                        candidate == null ||
                        candidate.Actor == null ||
                        !used.Add(candidate.Identity)
                    )
                    {
                        continue;
                    }
                    result.Add(
                        new CouncilDelegateMember()
                        {
                            Identity = candidate.Identity,
                            Name = candidate.Name,
                            CityName = candidate.CityName,
                            Actor = candidate.Actor
                        }
                    );
                }
            }

            return result;
        }

        private static List<CouncilDelegateCandidate>
            BuildCouncilDelegateCandidates(
                Kingdom kingdom,
                City city,
                int currentYear
            )
        {
            List<CouncilDelegateCandidate> result =
                new List<CouncilDelegateCandidate>();
            if (kingdom == null || city == null)
            {
                return result;
            }

            string stateIdeology = GetStateIdeology(kingdom);
            string system = GetPoliticalSystem(kingdom);
            string rulingIdeology = GetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdeologyDataKey,
                ""
            );
            List<Actor> units = GetCityUnitsSafe(city);
            int inspected = 0;
            for (
                int i = 0;
                i < units.Count && inspected < 260;
                i++
            )
            {
                Actor actor = units[i];
                inspected++;
                if (actor == null || !actor.isAlive())
                {
                    continue;
                }
                int age = GetActorAgeYearsSafe(actor);
                if (age > 0 && age < 16)
                {
                    continue;
                }

                string identity = GetStableObjectIdentity(actor);
                if (string.IsNullOrEmpty(identity))
                {
                    continue;
                }
                string ideology = GetCitizenIdeology(actor);
                float score = GetCitizenIdeologyConviction(actor) * 0.70f;
                if (ideology == stateIdeology)
                {
                    score += 18f;
                }
                if (
                    system == PoliticalSystemSovietOnePartyId &&
                    !string.IsNullOrEmpty(rulingIdeology) &&
                    ideology == rulingIdeology
                )
                {
                    score += 55f;
                }
                try
                {
                    if (actor.stats != null)
                    {
                        score += actor.stats[S.diplomacy] * 0.65f;
                        score += actor.stats[S.stewardship] * 0.60f;
                        score += actor.stats[S.warfare] * 0.20f;
                    }
                }
                catch
                {
                }
                score += (
                    Math.Abs(
                        StablePartyHash(
                            identity + "|delegate|" + currentYear
                        )
                    ) % 70
                ) / 14f;

                result.Add(
                    new CouncilDelegateCandidate()
                    {
                        Actor = actor,
                        Identity = identity,
                        Name = GetWorldObjectDisplayName(actor),
                        CityName = GetWorldObjectDisplayName(city),
                        Score = score
                    }
                );
            }
            result.Sort(
                delegate(
                    CouncilDelegateCandidate a,
                    CouncilDelegateCandidate b
                )
                {
                    int cmp = b.Score.CompareTo(a.Score);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                    return string.CompareOrdinal(
                        a.Identity ?? "",
                        b.Identity ?? ""
                    );
                }
            );
            return result;
        }

        private static void SaveCouncilDelegates(
            Kingdom kingdom,
            List<CouncilDelegateMember> delegates
        )
        {
            List<string> ids = new List<string>();
            List<string> names = new List<string>();
            List<string> cities = new List<string>();
            if (delegates != null)
            {
                for (int i = 0; i < delegates.Count; i++)
                {
                    CouncilDelegateMember member = delegates[i];
                    if (member == null)
                    {
                        continue;
                    }
                    ids.Add(member.Identity ?? "");
                    names.Add(member.Name ?? "");
                    cities.Add(member.CityName ?? "");
                }
            }
            string sep = PartyLeadershipListSeparator.ToString();
            SetKingdomStringData(
                kingdom,
                CouncilDelegateIdsDataKey,
                string.Join(sep, ids.ToArray())
            );
            SetKingdomStringData(
                kingdom,
                CouncilDelegateNamesDataKey,
                string.Join(sep, names.ToArray())
            );
            SetKingdomStringData(
                kingdom,
                CouncilDelegateCityNamesDataKey,
                string.Join(sep, cities.ToArray())
            );
        }

        private static List<CouncilDelegateMember> LoadCouncilDelegates(
            Kingdom kingdom
        )
        {
            List<CouncilDelegateMember> result =
                new List<CouncilDelegateMember>();
            string idsText = GetKingdomStringData(
                kingdom,
                CouncilDelegateIdsDataKey,
                ""
            );
            string namesText = GetKingdomStringData(
                kingdom,
                CouncilDelegateNamesDataKey,
                ""
            );
            string citiesText = GetKingdomStringData(
                kingdom,
                CouncilDelegateCityNamesDataKey,
                ""
            );
            if (string.IsNullOrEmpty(idsText) && string.IsNullOrEmpty(namesText))
            {
                return result;
            }
            char[] sep = new char[] { PartyLeadershipListSeparator };
            string[] ids = idsText.Split(sep);
            string[] names = namesText.Split(sep);
            string[] cities = citiesText.Split(sep);
            int count = Math.Max(ids.Length, names.Length);
            for (int i = 0; i < count; i++)
            {
                string id = i < ids.Length ? ids[i] : "";
                string name = i < names.Length ? names[i] : "";
                if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
                {
                    continue;
                }
                result.Add(
                    new CouncilDelegateMember()
                    {
                        Identity = id,
                        Name = name,
                        CityName = i < cities.Length ? cities[i] : "",
                        Actor = null
                    }
                );
            }
            return result;
        }

        private static string FormatCouncilDelegateSummary(Kingdom kingdom)
        {
            List<CouncilDelegateMember> delegates =
                LoadCouncilDelegates(kingdom);
            if (delegates.Count == 0)
            {
                return LM.Get("ukiol_council_forming");
            }
            int visible = Math.Min(4, delegates.Count);
            List<string> names = new List<string>();
            for (int i = 0; i < visible; i++)
            {
                string city = delegates[i].CityName;
                names.Add(
                    string.IsNullOrEmpty(city)
                        ? delegates[i].Name
                        : delegates[i].Name + " (" + city + ")"
                );
            }
            string result = string.Join(", ", names.ToArray());
            if (delegates.Count > visible)
            {
                result += " " + string.Format(
                    LM.Get("ukiol_central_committee_more"),
                    delegates.Count - visible
                );
            }
            return result;
        }

    }
}
