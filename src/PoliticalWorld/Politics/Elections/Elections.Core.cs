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
        private static void UpdateElections()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            int currentYear = GetWorldYearSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                string form = GetGovernmentForm(kingdom);
                string system = GetPoliticalSystem(kingdom);
                string trackedForm = GetKingdomStringData(
                    kingdom,
                    ElectionGovernmentFormDataKey,
                    ""
                );

                if (trackedForm != form)
                {
                    SetKingdomStringData(
                        kingdom,
                        ElectionGovernmentFormDataKey,
                        form
                    );
                }

                if (system != PoliticalSystemCompetitiveId)
                {
                    StoreElectionWinningCoalition(
                        kingdom,
                        "",
                        "",
                        0
                    );
                    SetKingdomIntData(
                        kingdom,
                        ElectionNextYearDataKey,
                        0
                    );

                    if (
                        system == PoliticalSystemOnePartyId ||
                        system == PoliticalSystemSovietOnePartyId
                    )
                    {
                        EnsureNonElectiveRulingParty(kingdom, form);
                    }
                    else
                    {
                        ClearRulingParty(kingdom);
                    }
                    continue;
                }

                int term = GetGovernmentElectionTermYears(form);
                if (term <= 0)
                {
                    continue;
                }

                int nextYear = GetKingdomIntData(
                    kingdom,
                    ElectionNextYearDataKey,
                    0
                );

                if (nextYear <= 0)
                {
                    nextYear = Math.Max(1, currentYear + 1);
                    SetKingdomIntData(
                        kingdom,
                        ElectionNextYearDataKey,
                        nextYear
                    );
                }

                if (currentYear >= nextYear)
                {
                    RunKingdomElection(
                        kingdom,
                        form,
                        currentYear
                    );
                }
            }
        }

        private static void EnsureNonElectiveRulingParty(
            Kingdom kingdom,
            string form
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            // Only an explicit one-party state has a formal ruling party.
            // Absolute monarchies, military dictatorships and oligarchies
            // can still contain parties, but the state is not governed by
            // an electoral party mandate.
            if (
                form != GovernmentOnePartyStateId &&
                GetPoliticalSystem(kingdom) != PoliticalSystemSovietOnePartyId
            )
            {
                ClearRulingParty(kingdom);
                return;
            }

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            if (parties.Count == 0)
            {
                ClearRulingParty(kingdom);
                return;
            }

            string stateIdeology = GetStateIdeology(kingdom);
            PoliticalParty best = null;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty candidate = parties[i];
                if (candidate == null || !candidate.Active)
                {
                    continue;
                }

                bool preferred = candidate.Ideology == stateIdeology;
                bool bestPreferred =
                    best != null && best.Ideology == stateIdeology;

                if (
                    best == null ||
                    (preferred && !bestPreferred) ||
                    (preferred == bestPreferred &&
                        candidate.Support > best.Support)
                )
                {
                    best = candidate;
                }
            }

            if (best != null)
            {
                SetRulingPartyFromElection(kingdom, best);
            }
        }

        private static void ClearRulingParty(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            string previousPartyId = GetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                ""
            ) ?? "";

            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                ""
            );
            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyNameDataKey,
                ""
            );
            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdeologyDataKey,
                ""
            );
            SetKingdomIntData(
                kingdom,
                ElectionRulingPartySupportDataKey,
                0
            );

            if (!string.IsNullOrEmpty(previousPartyId))
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.RulingPartyChanged,
                    kingdom,
                    previousPartyId,
                    "",
                    0,
                    0,
                    ""
                );
            }
        }

        private static void RunKingdomElection(
            Kingdom kingdom,
            string governmentForm,
            int electionYear
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !GovernmentUsesCompetitiveElections(governmentForm)
            )
            {
                return;
            }

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            Dictionary<string, int> rawVotesByParty =
                new Dictionary<string, int>();
            int totalRawVotes = 0;

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty candidate = parties[i];
                if (
                    candidate == null ||
                    !candidate.Active ||
                    string.IsNullOrEmpty(candidate.Id)
                )
                {
                    continue;
                }

                int rawVotes = CalculateElectionPartyVoteShare(
                    kingdom,
                    candidate,
                    parties
                );
                rawVotes = Math.Max(0, rawVotes);
                rawVotesByParty[candidate.Id] = rawVotes;
                totalRawVotes += rawVotes;
            }

            int winnerVotes;
            string winningCoalitionId;
            string winningCoalitionName;
            int winningCoalitionVotes;
            PoliticalParty winner = ResolveElectionWinnerWithCoalitions(
                kingdom,
                parties,
                rawVotesByParty,
                totalRawVotes,
                out winnerVotes,
                out winningCoalitionId,
                out winningCoalitionName,
                out winningCoalitionVotes
            );

            int term = GetGovernmentElectionTermYears(governmentForm);
            if (winner == null)
            {
                StoreElectionWinningCoalition(
                    kingdom,
                    "",
                    "",
                    0
                );
                // Parties may not have formed yet. Retry next year instead of
                // recording a fake election with no candidate.
                SetKingdomIntData(
                    kingdom,
                    ElectionNextYearDataKey,
                    Math.Max(1, electionYear + 1)
                );
                return;
            }

            winnerVotes = ClampInt(winnerVotes, 0, 100);
            StoreElectionWinningCoalition(
                kingdom,
                winningCoalitionId,
                winningCoalitionName,
                winningCoalitionVotes
            );
            SetRulingPartyFromElection(kingdom, winner);

            // In elective republics the person shown by vanilla WorldBox as
            // the country's ruler should follow the election result as well.
            // Constitutional monarchies are deliberately excluded: elections
            // change the government there, not the monarch.
            SyncVanillaRulerWithElectionWinner(
                kingdom,
                governmentForm,
                winner
            );

            SetKingdomIntData(
                kingdom,
                ElectionRulingPartySupportDataKey,
                winnerVotes
            );
            SetKingdomIntData(
                kingdom,
                ElectionLastYearDataKey,
                electionYear
            );
            int nextElectionYear = Math.Max(
                1,
                electionYear + Math.Max(1, term)
            );
            SetKingdomIntData(
                kingdom,
                ElectionNextYearDataKey,
                nextElectionYear
            );

            RecordElectionHistory(
                kingdom,
                electionYear,
                winner.Id,
                winner.Name,
                winner.Ideology,
                winnerVotes
            );

            PoliticalWorldAPI.InternalEmitCoreEvent(
                eventId: PoliticalWorldAPI.Events.ElectionFinished,
                kingdom: kingdom,
                oldValue: winner.Ideology ?? "",
                newValue: winner.Name ?? "",
                oldNumber: electionYear,
                newNumber: winnerVotes,
                partyId: winner.Id ?? "",
                actor: winner.LeaderActor,
                actorIdentity: winner.LeaderIdentity ?? "",
                actorName: winner.LeaderName ?? "",
                newName: winner.Name ?? "",
                eventKey: "election_result_" + (winner.Id ?? ""),
                category: "election",
                year: electionYear,
                ideologyId: winner.Ideology ?? "",
                currentId: GetStateIdeologyCurrent(kingdom) ?? "",
                governmentId: GetGovernmentPublicId(kingdom) ?? "",
                payload: new Dictionary<string, string>()
                {
                    { "winner_party_id", winner.Id ?? "" },
                    { "winner_party_name", winner.Name ?? "" },
                    { "winner_ideology_id", winner.Ideology ?? "" },
                    { "winner_support", winnerVotes.ToString() },
                    { "next_election_year", nextElectionYear.ToString() },
                    { "coalition_id", winningCoalitionId ?? "" },
                    { "coalition_name", winningCoalitionName ?? "" },
                    { "coalition_support", winningCoalitionVotes.ToString() }
                }
            );

            string electionEventText = string.IsNullOrEmpty(winningCoalitionId)
                ? string.Format(
                    LM.Get("ukiol_event_election_result"),
                    GetWorldObjectDisplayName(kingdom),
                    winner.Name,
                    winnerVotes
                )
                : string.Format(
                    LM.Get("ukiol_event_election_coalition_result"),
                    GetWorldObjectDisplayName(kingdom),
                    winningCoalitionName,
                    winningCoalitionVotes,
                    winner.Name
                );

            PublishPoliticalEvent(
                electionEventText,
                kingdom,
                null,
                winner.LeaderActor,
                GetIdeologyIconPath(winner.Ideology),
                "election_result_" + winner.Id,
                35f,
                new List<string>
                {
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_election_votes"),
                        winnerVotes
                    ),
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_election_government"),
                        GetGovernmentFormName(governmentForm)
                    )
                },
                new List<string>
                {
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_election_ruling_party"),
                        winner.Name
                    ),
                    string.Format(
                        LM.Get("ukiol_chronicle_detail_election_next"),
                        nextElectionYear
                    )
                }
            );
        }

        private static void SyncVanillaRulerWithElectionWinner(
            Kingdom kingdom,
            string governmentForm,
            PoliticalParty winner
        )
        {
            if (
                kingdom == null ||
                winner == null ||
                (governmentForm != GovernmentParliamentaryRepublicId &&
                    governmentForm != GovernmentPresidentialRepublicId)
            )
            {
                return;
            }

            Actor electedLeader = winner.LeaderActor;
            if (electedLeader == null || !electedLeader.isAlive())
            {
                electedLeader = FindPartyActorByIdentity(
                    kingdom,
                    winner.LeaderIdentity,
                    winner.LeaderName
                );
            }

            if (electedLeader == null || !electedLeader.isAlive())
            {
                return;
            }

            // If the same party/leader wins again there is nothing to do.
            if (GetLivingRuler(kingdom) == electedLeader)
            {
                return;
            }

            TryInstallPoliticalRuler(kingdom, electedLeader);
        }

        private static int CalculateElectionPartyVoteShare(
            Kingdom kingdom,
            PoliticalParty party,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                party == null ||
                parties == null
            )
            {
                return 0;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            long weightedSupport = 0;
            long totalPopulation = 0;

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (city == null)
                {
                    continue;
                }

                int population = Math.Max(
                    1,
                    GetCityPopulationSafe(city)
                );
                int support = GetPartyCitySupport(
                    kingdom,
                    city,
                    party,
                    parties
                );

                weightedSupport += (long)support * population;
                totalPopulation += population;
            }

            if (totalPopulation <= 0)
            {
                return ClampInt(party.Support, 0, 100);
            }

            return ClampInt(
                (int)Math.Round(
                    weightedSupport / (double)totalPopulation
                ),
                0,
                100
            );
        }

        private static void SetRulingPartyFromElection(
            Kingdom kingdom,
            PoliticalParty party
        )
        {
            if (kingdom == null || party == null)
            {
                return;
            }

            string previousPartyId = GetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                ""
            ) ?? "";

            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                party.Id ?? ""
            );
            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyNameDataKey,
                party.Name ?? ""
            );
            SetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdeologyDataKey,
                party.Ideology ?? ""
            );
            SetKingdomIntData(
                kingdom,
                ElectionRulingPartySupportDataKey,
                ClampInt(party.Support, 0, 100)
            );

            string nextPartyId = party.Id ?? "";
            if (
                !string.Equals(
                    previousPartyId,
                    nextPartyId,
                    StringComparison.Ordinal
                )
            )
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.RulingPartyChanged,
                    kingdom,
                    previousPartyId,
                    nextPartyId,
                    0,
                    0,
                    nextPartyId
                );
            }
        }

        private static string GetGovernmentElectionStatusText(
            Kingdom kingdom,
            string form
        )
        {
            string system = GetPoliticalSystem(kingdom);

            if (system == PoliticalSystemOnePartyId)
            {
                return LM.Get("ukiol_elections_one_party");
            }
            if (
                system == PoliticalSystemSovietId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                int nextCouncil = GetKingdomIntData(
                    kingdom,
                    CouncilNextYearDataKey,
                    0
                );
                if (nextCouncil <= 0)
                {
                    return system == PoliticalSystemSovietOnePartyId
                        ? LM.Get("ukiol_elections_soviet_one_party")
                        : LM.Get("ukiol_elections_soviet");
                }
                return string.Format(
                    LM.Get(
                        system == PoliticalSystemSovietOnePartyId
                            ? "ukiol_elections_soviet_one_party_status"
                            : "ukiol_elections_soviet_status"
                    ),
                    CouncilTermYears,
                    nextCouncil,
                    Math.Max(0, nextCouncil - GetWorldYearSafe())
                );
            }
            if (system == PoliticalSystemDecentralizedId)
            {
                return LM.Get("ukiol_elections_decentralized");
            }
            if (system == PoliticalSystemNonElectoralId)
            {
                return LM.Get("ukiol_government_elections_none");
            }

            int term = GetGovernmentElectionTermYears(form);
            if (term <= 0)
            {
                return LM.Get("ukiol_government_elections_none");
            }

            int nextYear = GetKingdomIntData(
                kingdom,
                ElectionNextYearDataKey,
                0
            );
            if (nextYear <= 0)
            {
                return GetGovernmentElectionText(form);
            }

            int currentYear = GetWorldYearSafe();
            int yearsLeft = Math.Max(0, nextYear - currentYear);
            return string.Format(
                LM.Get("ukiol_government_elections_status"),
                term,
                nextYear,
                yearsLeft
            );
        }

        private static Color GetElectionPartyColor(
            Kingdom kingdom,
            string partyId,
            string ideology
        )
        {
            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                if (party != null && party.Id == partyId)
                {
                    return GetPartyIdentityColor(
                        party.Ideology,
                        party.ColorSeed
                    );
                }
            }

            return GetEmbeddedPartyIdeologyColor(ideology);
        }

        private static string NormalizeElectionHistoryValue(
            string value
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static string SerializeElectionHistory(
            List<ElectionHistoryEntry> history
        )
        {
            if (history == null || history.Count == 0)
            {
                return "";
            }

            List<string> encoded = new List<string>();
            int start = Math.Max(
                0,
                history.Count - MaxElectionHistoryEntries
            );

            for (int i = start; i < history.Count; i++)
            {
                ElectionHistoryEntry entry = history[i];
                if (entry == null)
                {
                    continue;
                }

                string raw =
                    entry.Year + "\t" +
                    NormalizeElectionHistoryValue(entry.WinnerPartyId) + "\t" +
                    NormalizeElectionHistoryValue(entry.WinnerPartyName) + "\t" +
                    NormalizeElectionHistoryValue(entry.WinnerIdeology) + "\t" +
                    entry.WinnerSupport;

                try
                {
                    encoded.Add(
                        Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes(raw)
                        )
                    );
                }
                catch
                {
                }
            }

            return string.Join(";", encoded.ToArray());
        }

        private static List<ElectionHistoryEntry> ParseElectionHistory(
            string serialized
        )
        {
            List<ElectionHistoryEntry> result =
                new List<ElectionHistoryEntry>();

            if (string.IsNullOrEmpty(serialized))
            {
                return result;
            }

            string[] tokens = serialized.Split(';');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (string.IsNullOrEmpty(tokens[i]))
                {
                    continue;
                }

                try
                {
                    string raw = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(tokens[i])
                    );
                    string[] fields = raw.Split(
                        new char[] { '\t' },
                        5
                    );
                    if (fields.Length < 5)
                    {
                        continue;
                    }

                    int year = 0;
                    int support = 0;
                    int.TryParse(fields[0], out year);
                    int.TryParse(fields[4], out support);

                    ElectionHistoryEntry entry =
                        new ElectionHistoryEntry();
                    entry.Year = Math.Max(0, year);
                    entry.WinnerPartyId = fields[1] ?? "";
                    entry.WinnerPartyName = fields[2] ?? "";
                    entry.WinnerIdeology = fields[3] ?? "";
                    entry.WinnerSupport = ClampInt(
                        support,
                        0,
                        100
                    );
                    result.Add(entry);
                }
                catch
                {
                }
            }

            if (result.Count > MaxElectionHistoryEntries)
            {
                result.RemoveRange(
                    0,
                    result.Count - MaxElectionHistoryEntries
                );
            }

            return result;
        }

        private static List<ElectionHistoryEntry> LoadElectionHistory(
            Kingdom kingdom
        )
        {
            return ParseElectionHistory(
                GetKingdomStringData(
                    kingdom,
                    ElectionHistoryDataKey,
                    ""
                )
            );
        }

        private static void RecordElectionHistory(
            Kingdom kingdom,
            int year,
            string winnerPartyId,
            string winnerPartyName,
            string winnerIdeology,
            int winnerSupport
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            List<ElectionHistoryEntry> history =
                LoadElectionHistory(kingdom);

            ElectionHistoryEntry entry =
                new ElectionHistoryEntry();
            entry.Year = Math.Max(0, year);
            entry.WinnerPartyId = winnerPartyId ?? "";
            entry.WinnerPartyName = winnerPartyName ?? "";
            entry.WinnerIdeology = winnerIdeology ?? "";
            entry.WinnerSupport = ClampInt(
                winnerSupport,
                0,
                100
            );

            history.Add(entry);
            if (history.Count > MaxElectionHistoryEntries)
            {
                history.RemoveRange(
                    0,
                    history.Count - MaxElectionHistoryEntries
                );
            }

            SetKingdomStringData(
                kingdom,
                ElectionHistoryDataKey,
                SerializeElectionHistory(history)
            );
        }

        // -----------------------------------------------------------------
        // v1.5.0-dev9.3 — Politics layout fix
        // -----------------------------------------------------------------
        // The old implementation treated every political current as a flat
        // switch-case entry. The registry makes currents real tree nodes.
        // Existing current IDs are intentionally preserved, so old saves and
        // every system that stores an ideology/current string keep working.
    }
}
