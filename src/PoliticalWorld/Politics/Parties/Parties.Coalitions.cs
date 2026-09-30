using System;
using System.Collections.Generic;
using NeoModLoader.General;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // 1.11-dev5: electoral alliances between parties.
        //
        // These are intentionally election blocs rather than permanent mergers.
        // Two parties keep their own ideology, leader and local support, but can
        // coordinate in a competitive election when they are politically close.
        private const string PartyCoalitionIdPrefix =
            "ukiol_party2_coalition_id_";
        private const string PartyCoalitionSinceYearPrefix =
            "ukiol_party2_coalition_since_year_";
        private const string PartyCoalitionLastReviewYearDataKey =
            "ukiol_party_coalition_last_review_year";
        private const string ElectionWinningCoalitionIdDataKey =
            "ukiol_election_winning_coalition_id";
        private const string ElectionWinningCoalitionNameDataKey =
            "ukiol_election_winning_coalition_name";
        private const string ElectionWinningCoalitionSupportDataKey =
            "ukiol_election_winning_coalition_support";
        private const int PartyCoalitionReviewYears = 3;
        private const int PartyCoalitionMaxIdeologyDistance = 135;
        private const int PartyCoalitionBreakIdeologyDistance = 175;
        private const int PartyCoalitionMinCombinedSupport = 18;

        private static void UpdatePartyCoalitions(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (kingdom == null || kingdom.data == null || parties == null)
            {
                return;
            }

            // Electoral alliances only make sense in a competitive system.
            if (GetPoliticalSystem(kingdom) != PoliticalSystemCompetitiveId)
            {
                ClearAllPartyCoalitions(kingdom, parties, false);
                return;
            }

            int worldYear = GetWorldYearSafe();
            if (worldYear <= 0)
            {
                return;
            }

            int lastReview = GetKingdomIntData(
                kingdom,
                PartyCoalitionLastReviewYearDataKey,
                0
            );
            if (
                lastReview > 0 &&
                worldYear - lastReview < PartyCoalitionReviewYears
            )
            {
                return;
            }
            SetKingdomIntData(
                kingdom,
                PartyCoalitionLastReviewYearDataKey,
                worldYear
            );

            List<PoliticalParty> active = new List<PoliticalParty>();
            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (
                    party != null &&
                    party.Active &&
                    party.Slot >= 0 &&
                    !string.IsNullOrEmpty(party.Id)
                )
                {
                    active.Add(party);
                }
            }

            // Validate old alliances first. A bloc is a pair in dev5. If one
            // member disappears or the parties drift too far apart, it ends.
            HashSet<string> processedCoalitions = new HashSet<string>();
            for (int i = 0; i < active.Count; i++)
            {
                PoliticalParty party = active[i];
                string coalitionId = GetPartyCoalitionId(kingdom, party);
                if (
                    string.IsNullOrEmpty(coalitionId) ||
                    processedCoalitions.Contains(coalitionId)
                )
                {
                    continue;
                }

                processedCoalitions.Add(coalitionId);
                List<PoliticalParty> members = GetPartyCoalitionMembers(
                    kingdom,
                    active,
                    coalitionId
                );

                if (members.Count != 2 || !CanCoalitionRemain(members[0], members[1]))
                {
                    DissolvePartyCoalition(
                        kingdom,
                        members,
                        coalitionId,
                        true
                    );
                }
            }

            // Rebuild the unaligned set after possible dissolutions.
            List<PoliticalParty> free = new List<PoliticalParty>();
            for (int i = 0; i < active.Count; i++)
            {
                if (string.IsNullOrEmpty(GetPartyCoalitionId(kingdom, active[i])))
                {
                    free.Add(active[i]);
                }
            }

            // With only two parties a permanent alliance would effectively
            // erase electoral competition. Start forming blocs at 3+ parties.
            if (active.Count < 3 || free.Count < 2)
            {
                return;
            }

            string rulingPartyId = GetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                ""
            ) ?? "";
            int nextElectionYear = GetKingdomIntData(
                kingdom,
                ElectionNextYearDataKey,
                0
            );
            bool electionSoon =
                nextElectionYear > 0 &&
                nextElectionYear - worldYear <= 2;

            for (int i = 0; i < free.Count; i++)
            {
                PoliticalParty first = free[i];
                if (
                    first == null ||
                    !string.IsNullOrEmpty(GetPartyCoalitionId(kingdom, first)) ||
                    first.Support < 4 ||
                    first.Support >= 58
                )
                {
                    continue;
                }

                PoliticalParty best = null;
                int bestScore = int.MinValue;

                for (int j = i + 1; j < free.Count; j++)
                {
                    PoliticalParty second = free[j];
                    if (
                        second == null ||
                        !string.IsNullOrEmpty(GetPartyCoalitionId(kingdom, second)) ||
                        second.Support < 4 ||
                        second.Support >= 58 ||
                        first.Support + second.Support < PartyCoalitionMinCombinedSupport ||
                        !CanPartiesFormCoalition(first, second, worldYear)
                    )
                    {
                        continue;
                    }

                    int distance = GetPartyIdeologyDistance(first, second);
                    int score = 220 - distance;
                    score += Math.Min(35, first.Support + second.Support);

                    bool bothOpposition =
                        first.Id != rulingPartyId &&
                        second.Id != rulingPartyId;
                    if (bothOpposition)
                    {
                        score += 12;
                    }
                    if (electionSoon)
                    {
                        score += 15;
                    }
                    if (first.Ideology == second.Ideology)
                    {
                        score += 18;
                    }

                    // Stable historical noise prevents every world from pairing
                    // the exact same ideological neighbours in the same order.
                    score += Math.Abs(
                        StablePartyHash(
                            first.Id + "|coalition|" + second.Id + "|" +
                            (worldYear / PartyCoalitionReviewYears)
                        )
                    ) % 17;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = second;
                    }
                }

                if (best == null || bestScore < 120)
                {
                    continue;
                }

                FormPartyCoalition(
                    kingdom,
                    first,
                    best,
                    worldYear
                );
            }
        }

        private static bool CanPartiesFormCoalition(
            PoliticalParty a,
            PoliticalParty b,
            int worldYear
        )
        {
            if (a == null || b == null || a == b)
            {
                return false;
            }

            int distance = GetPartyIdeologyDistance(a, b);
            if (
                distance > PartyCoalitionMaxIdeologyDistance ||
                Math.Abs(a.Radicalism - b.Radicalism) > 42
            )
            {
                return false;
            }

            // A fresh split should actually behave like a split for a while
            // instead of immediately re-forming the parent party as a bloc.
            bool parentChild =
                a.ParentPartyId == b.Id ||
                b.ParentPartyId == a.Id;
            if (parentChild)
            {
                int newestYear = Math.Max(a.FoundedYear, b.FoundedYear);
                if (newestYear > 0 && worldYear - newestYear < 10)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanCoalitionRemain(
            PoliticalParty a,
            PoliticalParty b
        )
        {
            if (a == null || b == null || !a.Active || !b.Active)
            {
                return false;
            }

            return
                GetPartyIdeologyDistance(a, b) <=
                    PartyCoalitionBreakIdeologyDistance &&
                Math.Abs(a.Radicalism - b.Radicalism) <= 58;
        }

        private static int GetPartyIdeologyDistance(
            PoliticalParty a,
            PoliticalParty b
        )
        {
            if (
                a == null || b == null ||
                !IsValidIdeology(a.Ideology) ||
                !IsValidIdeology(b.Ideology)
            )
            {
                return 500;
            }

            return GetIdeologyBehaviorDistance(
                GetIdeologyBehaviorProfile(a.Ideology),
                GetIdeologyBehaviorProfile(b.Ideology)
            );
        }

        private static void FormPartyCoalition(
            Kingdom kingdom,
            PoliticalParty a,
            PoliticalParty b,
            int worldYear
        )
        {
            if (kingdom == null || a == null || b == null)
            {
                return;
            }

            string left = string.CompareOrdinal(a.Id, b.Id) <= 0 ? a.Id : b.Id;
            string right = left == a.Id ? b.Id : a.Id;
            string coalitionId = "coal_" + Math.Abs(
                StablePartyHash(left + "|" + right)
            ).ToString();

            SetPartyCoalitionId(kingdom, a, coalitionId, worldYear);
            SetPartyCoalitionId(kingdom, b, coalitionId, worldYear);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_party_coalition_formed"),
                    a.Name,
                    b.Name,
                    GetWorldObjectDisplayName(kingdom)
                ),
                kingdom,
                null,
                null,
                PartiesIconPath,
                "party_coalition_formed_" + coalitionId,
                22f
            );
        }

        private static void DissolvePartyCoalition(
            Kingdom kingdom,
            List<PoliticalParty> members,
            string coalitionId,
            bool publishEvent
        )
        {
            if (kingdom == null || members == null)
            {
                return;
            }

            string firstName = members.Count > 0 && members[0] != null
                ? members[0].Name
                : "";
            string secondName = members.Count > 1 && members[1] != null
                ? members[1].Name
                : "";

            for (int i = 0; i < members.Count; i++)
            {
                PoliticalParty party = members[i];
                if (party == null)
                {
                    continue;
                }
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(PartyCoalitionIdPrefix, party.Slot),
                    ""
                );
                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(PartyCoalitionSinceYearPrefix, party.Slot),
                    0
                );
            }

            if (
                publishEvent &&
                !string.IsNullOrEmpty(firstName) &&
                !string.IsNullOrEmpty(secondName)
            )
            {
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_party_coalition_dissolved"),
                        firstName,
                        secondName,
                        GetWorldObjectDisplayName(kingdom)
                    ),
                    kingdom,
                    null,
                    null,
                    PartiesIconPath,
                    "party_coalition_dissolved_" + coalitionId,
                    18f
                );
            }
        }

        private static void ClearAllPartyCoalitions(
            Kingdom kingdom,
            List<PoliticalParty> parties,
            bool publishEvent
        )
        {
            if (kingdom == null || parties == null)
            {
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (party == null || party.Slot < 0)
                {
                    continue;
                }
                string id = GetPartyCoalitionId(kingdom, party);
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            foreach (string id in ids)
            {
                DissolvePartyCoalition(
                    kingdom,
                    GetPartyCoalitionMembers(kingdom, parties, id),
                    id,
                    publishEvent
                );
            }
        }

        private static string GetPartyCoalitionId(
            Kingdom kingdom,
            PoliticalParty party
        )
        {
            if (kingdom == null || party == null || party.Slot < 0)
            {
                return "";
            }

            return GetKingdomStringData(
                kingdom,
                PartySlotKey(PartyCoalitionIdPrefix, party.Slot),
                ""
            ) ?? "";
        }

        private static void SetPartyCoalitionId(
            Kingdom kingdom,
            PoliticalParty party,
            string coalitionId,
            int worldYear
        )
        {
            if (kingdom == null || party == null || party.Slot < 0)
            {
                return;
            }

            SetKingdomStringData(
                kingdom,
                PartySlotKey(PartyCoalitionIdPrefix, party.Slot),
                coalitionId ?? ""
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(PartyCoalitionSinceYearPrefix, party.Slot),
                worldYear
            );
        }

        private static List<PoliticalParty> GetPartyCoalitionMembers(
            Kingdom kingdom,
            List<PoliticalParty> parties,
            string coalitionId
        )
        {
            List<PoliticalParty> result = new List<PoliticalParty>();
            if (
                kingdom == null ||
                parties == null ||
                string.IsNullOrEmpty(coalitionId)
            )
            {
                return result;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (
                    party != null &&
                    party.Active &&
                    GetPartyCoalitionId(kingdom, party) == coalitionId
                )
                {
                    result.Add(party);
                }
            }
            return result;
        }

        private static string GetPartyCoalitionDisplayName(
            List<PoliticalParty> members
        )
        {
            if (members == null || members.Count == 0)
            {
                return "";
            }

            string result = "";
            for (int i = 0; i < members.Count; i++)
            {
                PoliticalParty party = members[i];
                if (party == null || string.IsNullOrEmpty(party.Name))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(result))
                {
                    result += " + ";
                }
                result += party.Name;
            }
            return result;
        }

        private static PoliticalParty ResolveElectionWinnerWithCoalitions(
            Kingdom kingdom,
            List<PoliticalParty> parties,
            Dictionary<string, int> rawVotesByParty,
            int totalRawVotes,
            out int winnerPartyVotes,
            out string winningCoalitionId,
            out string winningCoalitionName,
            out int winningCoalitionVotes
        )
        {
            winnerPartyVotes = -1;
            winningCoalitionId = "";
            winningCoalitionName = "";
            winningCoalitionVotes = 0;

            if (
                kingdom == null ||
                parties == null ||
                rawVotesByParty == null
            )
            {
                return null;
            }

            Dictionary<string, List<PoliticalParty>> groups =
                new Dictionary<string, List<PoliticalParty>>();

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (
                    party == null ||
                    !party.Active ||
                    string.IsNullOrEmpty(party.Id)
                )
                {
                    continue;
                }

                string coalitionId = GetPartyCoalitionId(kingdom, party);
                string groupKey = string.IsNullOrEmpty(coalitionId)
                    ? "party:" + party.Id
                    : "coalition:" + coalitionId;

                List<PoliticalParty> group;
                if (!groups.TryGetValue(groupKey, out group))
                {
                    group = new List<PoliticalParty>();
                    groups[groupKey] = group;
                }
                group.Add(party);
            }

            PoliticalParty winner = null;
            int bestGroupRawVotes = -1;
            string bestGroupKey = "";
            List<PoliticalParty> bestMembers = null;

            foreach (KeyValuePair<string, List<PoliticalParty>> pair in groups)
            {
                int groupRawVotes = 0;
                PoliticalParty groupLeader = null;
                int groupLeaderRawVotes = -1;

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    PoliticalParty party = pair.Value[i];
                    int rawVotes = 0;
                    rawVotesByParty.TryGetValue(party.Id, out rawVotes);
                    groupRawVotes += rawVotes;

                    if (
                        groupLeader == null ||
                        rawVotes > groupLeaderRawVotes ||
                        (rawVotes == groupLeaderRawVotes &&
                            string.CompareOrdinal(party.Id, groupLeader.Id) < 0)
                    )
                    {
                        groupLeader = party;
                        groupLeaderRawVotes = rawVotes;
                    }
                }

                if (
                    winner == null ||
                    groupRawVotes > bestGroupRawVotes ||
                    (groupRawVotes == bestGroupRawVotes &&
                        string.CompareOrdinal(pair.Key, bestGroupKey) < 0)
                )
                {
                    winner = groupLeader;
                    bestGroupRawVotes = groupRawVotes;
                    bestGroupKey = pair.Key;
                    bestMembers = pair.Value;
                }
            }

            if (winner == null)
            {
                return null;
            }

            int winnerRawVotes = 0;
            rawVotesByParty.TryGetValue(winner.Id, out winnerRawVotes);
            winnerPartyVotes = totalRawVotes <= 0
                ? 0
                : ClampInt(
                    (int)Math.Round(winnerRawVotes * 100.0 / totalRawVotes),
                    0,
                    100
                );

            if (
                bestGroupKey.StartsWith("coalition:", StringComparison.Ordinal) &&
                bestMembers != null &&
                bestMembers.Count >= 2
            )
            {
                winningCoalitionId = bestGroupKey.Substring("coalition:".Length);
                winningCoalitionName = GetPartyCoalitionDisplayName(bestMembers);
                winningCoalitionVotes = totalRawVotes <= 0
                    ? 0
                    : ClampInt(
                        (int)Math.Round(bestGroupRawVotes * 100.0 / totalRawVotes),
                        0,
                        100
                    );
            }

            return winner;
        }

        private static void StoreElectionWinningCoalition(
            Kingdom kingdom,
            string coalitionId,
            string coalitionName,
            int coalitionSupport
        )
        {
            if (kingdom == null)
            {
                return;
            }

            SetKingdomStringData(
                kingdom,
                ElectionWinningCoalitionIdDataKey,
                coalitionId ?? ""
            );
            SetKingdomStringData(
                kingdom,
                ElectionWinningCoalitionNameDataKey,
                coalitionName ?? ""
            );
            SetKingdomIntData(
                kingdom,
                ElectionWinningCoalitionSupportDataKey,
                ClampInt(coalitionSupport, 0, 100)
            );
        }
    }
}
