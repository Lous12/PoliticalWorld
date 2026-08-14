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
        private static void UpdateGovernmentLeadership()
        {
            if (World.world == null || World.world.kingdoms == null)
            {
                return;
            }

            int currentYear = GetWorldYearSafe();
            foreach (Kingdom kingdom in GetKingdomsSafe())
            {
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }
                UpdateGovernmentLeadershipForKingdom(kingdom, currentYear);
            }
        }

        private static void UpdateGovernmentLeadershipForKingdom(
            Kingdom kingdom,
            int currentYear
        )
        {
            string form = GetGovernmentForm(kingdom);
            string system = GetPoliticalSystem(kingdom);
            bool migration = GetKingdomIntData(
                kingdom,
                GovernmentLeadershipSchemaDataKey,
                0
            ) < GovernmentLeadershipSchemaVersion;

            LeadershipCandidate state = DetermineHeadOfState(
                kingdom,
                form,
                system,
                currentYear
            );
            LeadershipCandidate government = DetermineHeadOfGovernment(
                kingdom,
                form,
                system,
                currentYear,
                state
            );

            bool stateRequired = system != PoliticalSystemDecentralizedId;
            bool governmentRequired = system != PoliticalSystemDecentralizedId;
            bool crisis = (stateRequired && !IsLivingLeadershipCandidate(state)) ||
                (governmentRequired && !IsLivingLeadershipCandidate(government));

            ApplyLeadershipOffice(
                kingdom,
                state,
                LeadershipRoleState,
                HeadOfStateIdentityDataKey,
                HeadOfStateNameDataKey,
                HeadOfStateTitleDataKey,
                HeadOfStateSinceYearDataKey,
                currentYear,
                !migration
            );
            ApplyLeadershipOffice(
                kingdom,
                government,
                LeadershipRoleGovernment,
                HeadOfGovernmentIdentityDataKey,
                HeadOfGovernmentNameDataKey,
                HeadOfGovernmentTitleDataKey,
                HeadOfGovernmentSinceYearDataKey,
                currentYear,
                !migration
            );

            int previousCrisis = GetKingdomIntData(
                kingdom,
                LeadershipCrisisDataKey,
                0
            );
            if (crisis)
            {
                SetKingdomIntData(kingdom, LeadershipCrisisDataKey, 1);
                if (previousCrisis == 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        LeadershipCrisisSinceYearDataKey,
                        currentYear
                    );
                    if (!migration)
                    {
                        PoliticalWorldAPI.InternalEmitCoreEvent(
                            eventId: PoliticalWorldAPI.Events.LeadershipCrisisStarted,
                            kingdom: kingdom,
                            oldNumber: 0,
                            newNumber: 1,
                            eventKey: "leadership_crisis",
                            category: "leadership-crisis",
                            year: currentYear
                        );

                        PublishPoliticalEvent(
                            string.Format(
                                LM.Get("ukiol_event_leadership_crisis"),
                                GetWorldObjectDisplayName(kingdom)
                            ),
                            kingdom,
                            null,
                            GetLivingRuler(kingdom),
                            HistoryIconPath,
                            "leadership_crisis",
                            35f
                        );
                    }
                }
            }
            else
            {
                SetKingdomIntData(kingdom, LeadershipCrisisDataKey, 0);
                if (previousCrisis != 0 && !migration)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        eventId: PoliticalWorldAPI.Events.LeadershipCrisisResolved,
                        kingdom: kingdom,
                        oldNumber: 1,
                        newNumber: 0,
                        actor: state != null ? state.Actor : GetLivingRuler(kingdom),
                        eventKey: "leadership_crisis_resolved",
                        category: "leadership-crisis",
                        year: currentYear
                    );

                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_leadership_crisis_resolved"),
                            GetWorldObjectDisplayName(kingdom)
                        ),
                        kingdom,
                        null,
                        state != null ? state.Actor : GetLivingRuler(kingdom),
                        OverviewIconPath,
                        "leadership_crisis_resolved",
                        30f
                    );
                }
            }

            SetKingdomIntData(
                kingdom,
                GovernmentLeadershipSchemaDataKey,
                GovernmentLeadershipSchemaVersion
            );
        }

        private static LeadershipCandidate DetermineHeadOfState(
            Kingdom kingdom,
            string form,
            string system,
            int currentYear
        )
        {
            if (system == PoliticalSystemDecentralizedId)
            {
                return null;
            }

            if (
                form == GovernmentAbsoluteMonarchyId ||
                form == GovernmentConstitutionalMonarchyId
            )
            {
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleMonarch
                );
            }

            if (form == GovernmentPresidentialRepublicId)
            {
                Actor president = GetRulingPartyLeaderActor(kingdom);
                if (president != null)
                {
                    return MakeLeadershipCandidate(
                        president,
                        LeadershipTitlePresident
                    );
                }
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleActingPresident
                );
            }

            if (form == GovernmentParliamentaryRepublicId)
            {
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitlePresident
                );
            }

            if (
                system == PoliticalSystemSovietId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                Actor chair = GetOrSelectCouncilChair(kingdom, currentYear);
                return MakeLeadershipCandidate(
                    chair,
                    LeadershipTitleCouncilChair
                );
            }

            if (form == GovernmentOnePartyStateId)
            {
                Actor partyLeader = GetGeneralSecretaryActor(kingdom);
                if (partyLeader == null)
                {
                    partyLeader = GetRulingPartyLeaderActor(kingdom);
                }
                return MakeLeadershipCandidate(
                    partyLeader,
                    partyLeader != null &&
                        !string.IsNullOrEmpty(
                            GetKingdomStringData(
                                kingdom,
                                GeneralSecretaryIdentityDataKey,
                                ""
                            )
                        )
                        ? LeadershipTitleGeneralSecretary
                        : LeadershipTitlePartyLeader
                );
            }

            if (form == GovernmentMilitaryDictatorshipId)
            {
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleMilitaryRuler
                );
            }

            return MakeLeadershipCandidate(
                GetLivingRuler(kingdom),
                LeadershipTitleOligarchicChair
            );
        }

        private static LeadershipCandidate DetermineHeadOfGovernment(
            Kingdom kingdom,
            string form,
            string system,
            int currentYear,
            LeadershipCandidate state
        )
        {
            if (system == PoliticalSystemDecentralizedId)
            {
                return null;
            }

            if (form == GovernmentAbsoluteMonarchyId)
            {
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleMonarch
                );
            }

            if (
                form == GovernmentConstitutionalMonarchyId ||
                form == GovernmentParliamentaryRepublicId
            )
            {
                Actor primeMinister = GetRulingPartyLeaderActor(kingdom);
                if (primeMinister != null)
                {
                    return MakeLeadershipCandidate(
                        primeMinister,
                        LeadershipTitlePrimeMinister
                    );
                }
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleActingPrimeMinister
                );
            }

            if (form == GovernmentPresidentialRepublicId)
            {
                Actor president = state != null ? state.Actor : null;
                return MakeLeadershipCandidate(
                    president,
                    LeadershipTitlePresident
                );
            }

            if (system == PoliticalSystemSovietOnePartyId)
            {
                Actor secretary = GetGeneralSecretaryActor(kingdom);
                if (secretary != null)
                {
                    return MakeLeadershipCandidate(
                        secretary,
                        LeadershipTitleGeneralSecretary
                    );
                }
                Actor rulingLeader = GetRulingPartyLeaderActor(kingdom);
                if (rulingLeader != null)
                {
                    return MakeLeadershipCandidate(
                        rulingLeader,
                        LeadershipTitlePartyLeader
                    );
                }
                return MakeLeadershipCandidate(
                    state != null ? state.Actor : null,
                    LeadershipTitleCouncilGovernmentChair
                );
            }

            if (system == PoliticalSystemSovietId)
            {
                Actor dominantLeader = GetCouncilDominantPartyLeaderActor(kingdom);
                if (dominantLeader != null)
                {
                    return MakeLeadershipCandidate(
                        dominantLeader,
                        LeadershipTitleCouncilGovernmentChair
                    );
                }
                return MakeLeadershipCandidate(
                    state != null ? state.Actor : null,
                    LeadershipTitleCouncilGovernmentChair
                );
            }

            if (form == GovernmentOnePartyStateId)
            {
                Actor partyLeader = GetGeneralSecretaryActor(kingdom);
                if (partyLeader == null)
                {
                    partyLeader = GetRulingPartyLeaderActor(kingdom);
                }
                return MakeLeadershipCandidate(
                    partyLeader,
                    partyLeader != null &&
                        !string.IsNullOrEmpty(
                            GetKingdomStringData(
                                kingdom,
                                GeneralSecretaryIdentityDataKey,
                                ""
                            )
                        )
                        ? LeadershipTitleGeneralSecretary
                        : LeadershipTitlePartyLeader
                );
            }

            if (form == GovernmentMilitaryDictatorshipId)
            {
                return MakeLeadershipCandidate(
                    GetLivingRuler(kingdom),
                    LeadershipTitleMilitaryRuler
                );
            }

            return MakeLeadershipCandidate(
                GetLivingRuler(kingdom),
                LeadershipTitleOligarchicChair
            );
        }

        private static LeadershipCandidate MakeLeadershipCandidate(
            Actor actor,
            string title
        )
        {
            if (actor == null || !actor.isAlive())
            {
                return null;
            }
            return new LeadershipCandidate()
            {
                Actor = actor,
                Identity = GetStableObjectIdentity(actor),
                Name = GetWorldObjectDisplayName(actor),
                Title = title ?? ""
            };
        }

        private static bool IsLivingLeadershipCandidate(
            LeadershipCandidate candidate
        )
        {
            return candidate != null &&
                candidate.Actor != null &&
                candidate.Actor.isAlive();
        }

        private static Actor GetRulingPartyLeaderActor(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return null;
            }
            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            PoliticalParty party = FindPoliticalPartyById(
                parties,
                GetKingdomStringData(
                    kingdom,
                    ElectionRulingPartyIdDataKey,
                    ""
                )
            );
            if (party == null || !party.Active)
            {
                return null;
            }
            Actor actor = FindPartyActorByIdentity(
                kingdom,
                party.LeaderIdentity,
                party.LeaderName
            );
            return actor != null && actor.isAlive() ? actor : null;
        }

        private static Actor GetCouncilDominantPartyLeaderActor(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return null;
            }
            string partyId = GetKingdomStringData(
                kingdom,
                CouncilDominantPartyIdDataKey,
                ""
            );
            if (string.IsNullOrEmpty(partyId))
            {
                return null;
            }
            PoliticalParty party = FindPoliticalPartyById(
                GetPoliticalParties(kingdom),
                partyId
            );
            if (party == null || !party.Active)
            {
                return null;
            }
            return FindPartyActorByIdentity(
                kingdom,
                party.LeaderIdentity,
                party.LeaderName
            );
        }

        private static Actor GetGeneralSecretaryActor(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return null;
            }
            Actor actor = FindCommitteeActorByIdentity(
                kingdom,
                GetKingdomStringData(
                    kingdom,
                    GeneralSecretaryIdentityDataKey,
                    ""
                ),
                GetKingdomStringData(
                    kingdom,
                    GeneralSecretaryNameDataKey,
                    ""
                )
            );
            return actor != null && actor.isAlive() ? actor : null;
        }

        private static Actor GetOrSelectCouncilChair(
            Kingdom kingdom,
            int currentYear
        )
        {
            List<CouncilDelegateMember> delegates =
                LoadCouncilDelegates(kingdom);
            if (delegates.Count == 0)
            {
                return null;
            }

            string incumbentId = GetKingdomStringData(
                kingdom,
                HeadOfStateIdentityDataKey,
                ""
            );
            for (int i = 0; i < delegates.Count; i++)
            {
                CouncilDelegateMember member = delegates[i];
                if (
                    member == null ||
                    string.IsNullOrEmpty(member.Identity) ||
                    member.Identity != incumbentId
                )
                {
                    continue;
                }
                Actor incumbent = FindCommitteeActorByIdentity(
                    kingdom,
                    member.Identity,
                    member.Name
                );
                if (incumbent != null && incumbent.isAlive())
                {
                    return incumbent;
                }
            }

            Actor best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < delegates.Count; i++)
            {
                CouncilDelegateMember member = delegates[i];
                if (member == null)
                {
                    continue;
                }
                Actor actor = FindCommitteeActorByIdentity(
                    kingdom,
                    member.Identity,
                    member.Name
                );
                if (actor == null || !actor.isAlive())
                {
                    continue;
                }
                float score = 0f;
                try
                {
                    if (actor.stats != null)
                    {
                        score += actor.stats[S.diplomacy] * 1.0f;
                        score += actor.stats[S.stewardship] * 0.8f;
                    }
                }
                catch
                {
                }
                score += (
                    Math.Abs(
                        StablePartyHash(
                            member.Identity + "|council_chair|" + currentYear
                        )
                    ) % 80
                ) / 20f;
                if (best == null || score > bestScore)
                {
                    best = actor;
                    bestScore = score;
                }
            }
            return best;
        }

        private static void ApplyLeadershipOffice(
            Kingdom kingdom,
            LeadershipCandidate candidate,
            string role,
            string identityKey,
            string nameKey,
            string titleKey,
            string sinceKey,
            int currentYear,
            bool publishEvent
        )
        {
            string oldIdentity = GetKingdomStringData(
                kingdom,
                identityKey,
                ""
            );
            string oldName = GetKingdomStringData(
                kingdom,
                nameKey,
                ""
            );
            string newIdentity = candidate != null
                ? candidate.Identity ?? ""
                : "";
            string newName = candidate != null
                ? candidate.Name ?? ""
                : "";
            string newTitle = candidate != null
                ? candidate.Title ?? ""
                : "";

            bool changed = oldIdentity != newIdentity ||
                (string.IsNullOrEmpty(oldIdentity) && oldName != newName);

            SetKingdomStringData(kingdom, identityKey, newIdentity);
            SetKingdomStringData(kingdom, nameKey, newName);
            SetKingdomStringData(kingdom, titleKey, newTitle);

            if (changed)
            {
                SetKingdomIntData(kingdom, sinceKey, currentYear);
                if (!string.IsNullOrEmpty(newName))
                {
                    RecordLeadershipHistory(
                        kingdom,
                        currentYear,
                        role,
                        newName,
                        newTitle
                    );
                }
                if (publishEvent && !string.IsNullOrEmpty(newName))
                {
                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_leadership_succession"),
                            GetWorldObjectDisplayName(kingdom),
                            GetLeadershipRoleName(role),
                            newName,
                            GetLeadershipTitleName(newTitle),
                            string.IsNullOrEmpty(oldName)
                                ? LM.Get("ukiol_party_leader_unknown")
                                : oldName
                        ),
                        kingdom,
                        null,
                        candidate != null ? candidate.Actor : null,
                        OverviewIconPath,
                        "leadership_succession_" + role,
                        30f
                    );
                }
            }
            else if (GetKingdomIntData(kingdom, sinceKey, 0) <= 0)
            {
                SetKingdomIntData(kingdom, sinceKey, currentYear);
            }
        }

        private static string FormatLeadershipOffice(
            Kingdom kingdom,
            string nameKey,
            string titleKey,
            string sinceKey
        )
        {
            string name = GetKingdomStringData(kingdom, nameKey, "");
            string title = GetKingdomStringData(kingdom, titleKey, "");
            if (string.IsNullOrEmpty(name))
            {
                if (GetPoliticalSystem(kingdom) == PoliticalSystemDecentralizedId)
                {
                    return LM.Get("ukiol_leadership_no_central_office");
                }
                return LM.Get("ukiol_leadership_vacant");
            }
            int since = GetKingdomIntData(kingdom, sinceKey, 0);
            int years = since > 0
                ? Math.Max(0, GetWorldYearSafe() - since)
                : 0;
            return string.Format(
                LM.Get("ukiol_leadership_office_value"),
                name,
                GetLeadershipTitleName(title),
                years
            );
        }

        private static string GetLeadershipTitleName(string title)
        {
            if (title == LeadershipTitleMonarch)
                return LM.Get("ukiol_leadership_title_monarch");
            if (title == LeadershipTitlePresident)
                return LM.Get("ukiol_leadership_title_president");
            if (title == LeadershipTitlePrimeMinister)
                return LM.Get("ukiol_leadership_title_prime_minister");
            if (title == LeadershipTitleCouncilChair)
                return LM.Get("ukiol_leadership_title_council_chair");
            if (title == LeadershipTitleCouncilGovernmentChair)
                return LM.Get("ukiol_leadership_title_council_government_chair");
            if (title == LeadershipTitleGeneralSecretary)
                return LM.Get("ukiol_leadership_title_general_secretary");
            if (title == LeadershipTitlePartyLeader)
                return LM.Get("ukiol_leadership_title_party_leader");
            if (title == LeadershipTitleMilitaryRuler)
                return LM.Get("ukiol_leadership_title_military_ruler");
            if (title == LeadershipTitleOligarchicChair)
                return LM.Get("ukiol_leadership_title_oligarchic_chair");
            if (title == LeadershipTitleActingPresident)
                return LM.Get("ukiol_leadership_title_acting_president");
            if (title == LeadershipTitleActingPrimeMinister)
                return LM.Get("ukiol_leadership_title_acting_prime_minister");
            return LM.Get("ukiol_leadership_title_unknown");
        }

        private static string GetLeadershipRoleName(string role)
        {
            return role == LeadershipRoleGovernment
                ? LM.Get("ukiol_head_of_government_label")
                : LM.Get("ukiol_head_of_state_label");
        }

        private static string NormalizeLeadershipHistoryValue(string value)
        {
            return (value ?? "")
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static string SerializeLeadershipHistory(
            List<LeadershipHistoryEntry> history
        )
        {
            if (history == null || history.Count == 0)
            {
                return "";
            }
            List<string> encoded = new List<string>();
            int start = Math.Max(0, history.Count - MaxLeadershipHistoryEntries);
            for (int i = start; i < history.Count; i++)
            {
                LeadershipHistoryEntry entry = history[i];
                if (entry == null)
                {
                    continue;
                }
                string raw =
                    entry.Year + "\t" +
                    NormalizeLeadershipHistoryValue(entry.Role) + "\t" +
                    NormalizeLeadershipHistoryValue(entry.Name) + "\t" +
                    NormalizeLeadershipHistoryValue(entry.Title);
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

        private static List<LeadershipHistoryEntry> ParseLeadershipHistory(
            string serialized
        )
        {
            List<LeadershipHistoryEntry> result =
                new List<LeadershipHistoryEntry>();
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
                    string[] fields = raw.Split(new char[] { '\t' }, 4);
                    if (fields.Length < 4)
                    {
                        continue;
                    }
                    int year = 0;
                    int.TryParse(fields[0], out year);
                    result.Add(
                        new LeadershipHistoryEntry()
                        {
                            Year = Math.Max(0, year),
                            Role = fields[1] ?? "",
                            Name = fields[2] ?? "",
                            Title = fields[3] ?? ""
                        }
                    );
                }
                catch
                {
                }
            }
            if (result.Count > MaxLeadershipHistoryEntries)
            {
                result.RemoveRange(
                    0,
                    result.Count - MaxLeadershipHistoryEntries
                );
            }
            return result;
        }

        private static List<LeadershipHistoryEntry> LoadLeadershipHistory(
            Kingdom kingdom
        )
        {
            return ParseLeadershipHistory(
                GetKingdomStringData(
                    kingdom,
                    LeadershipHistoryDataKey,
                    ""
                )
            );
        }

        private static void RecordLeadershipHistory(
            Kingdom kingdom,
            int year,
            string role,
            string name,
            string title
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }
            List<LeadershipHistoryEntry> history =
                LoadLeadershipHistory(kingdom);
            history.Add(
                new LeadershipHistoryEntry()
                {
                    Year = Math.Max(0, year),
                    Role = role ?? "",
                    Name = name ?? "",
                    Title = title ?? ""
                }
            );
            if (history.Count > MaxLeadershipHistoryEntries)
            {
                history.RemoveRange(
                    0,
                    history.Count - MaxLeadershipHistoryEntries
                );
            }
            SetKingdomStringData(
                kingdom,
                LeadershipHistoryDataKey,
                SerializeLeadershipHistory(history)
            );
        }

        private static string GetGovernmentElectionText(string form)
        {
            int term = GetGovernmentElectionTermYears(form);
            if (term <= 0)
            {
                return LM.Get("ukiol_government_elections_none");
            }

            return string.Format(
                LM.Get("ukiol_government_elections_every_years"),
                term
            );
        }

        private static Color GetGovernmentFormColor(string form)
        {
            if (
                form == GovernmentParliamentaryRepublicId ||
                form == GovernmentPresidentialRepublicId ||
                form == GovernmentConstitutionalMonarchyId
            )
            {
                return new Color(0.58f, 0.86f, 0.93f, 1f);
            }
            if (form == GovernmentCouncilRepublicId)
            {
                return new Color(0.92f, 0.72f, 0.45f, 1f);
            }
            if (
                form == GovernmentOnePartyStateId ||
                form == GovernmentMilitaryDictatorshipId
            )
            {
                return new Color(0.93f, 0.49f, 0.39f, 1f);
            }
            if (
                form == GovernmentAbsoluteMonarchyId ||
                form == GovernmentOligarchyId
            )
            {
                return new Color(0.91f, 0.82f, 0.43f, 1f);
            }
            return new Color(0.78f, 0.80f, 0.75f, 1f);
        }

    }
}
