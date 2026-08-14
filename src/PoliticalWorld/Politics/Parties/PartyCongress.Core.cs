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
        private static void UpdatePartyCongressFoundation(
            Kingdom kingdom,
            int currentYear
        )
        {
            string system = GetPoliticalSystem(kingdom);
            if (
                system == PoliticalSystemOnePartyId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                EnsureNonElectiveRulingParty(
                    kingdom,
                    GetGovernmentForm(kingdom)
                );
            }

            int schema = GetKingdomIntData(
                kingdom,
                PartyLeadershipSchemaVersionDataKey,
                0
            );
            int legacyCommitteeSize = GetKingdomIntData(
                kingdom,
                CentralCommitteeSizeDataKey,
                0
            );

            // dev4 migration: a numeric committee existed without actors.
            // Convert it immediately into a real congress instead of pretending
            // those old placeholder seats were already occupied.
            if (
                schema < PartyLeadershipSchemaVersion &&
                legacyCommitteeSize > 0 &&
                string.IsNullOrEmpty(
                    GetKingdomStringData(
                        kingdom,
                        CentralCommitteeMemberIdsDataKey,
                        ""
                    )
                )
            )
            {
                HoldPartyCongress(kingdom, currentYear, true);
                return;
            }

            if (schema < PartyLeadershipSchemaVersion)
            {
                SetKingdomIntData(
                    kingdom,
                    PartyLeadershipSchemaVersionDataKey,
                    PartyLeadershipSchemaVersion
                );
            }

            int nextYear = GetKingdomIntData(
                kingdom,
                PartyCongressNextYearDataKey,
                0
            );
            if (nextYear <= 0)
            {
                nextYear = Math.Max(1, currentYear + 1);
                SetKingdomIntData(
                    kingdom,
                    PartyCongressNextYearDataKey,
                    nextYear
                );
            }

            if (currentYear >= nextYear)
            {
                HoldPartyCongress(kingdom, currentYear, false);
                return;
            }

            MaintainPartyLeadership(kingdom, currentYear);
        }

        private static void HoldPartyCongress(
            Kingdom kingdom,
            int currentYear,
            bool migrationCongress
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            EnsureNonElectiveRulingParty(
                kingdom,
                GetGovernmentForm(kingdom)
            );

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            PoliticalParty rulingParty = FindPoliticalPartyById(
                parties,
                GetKingdomStringData(
                    kingdom,
                    ElectionRulingPartyIdDataKey,
                    ""
                )
            );

            if (rulingParty == null)
            {
                SetKingdomIntData(kingdom, CentralCommitteeSizeDataKey, 0);
                SetKingdomIntData(
                    kingdom,
                    PartyCongressNextYearDataKey,
                    Math.Max(1, currentYear + 1)
                );
                return;
            }

            int desiredSize = CalculateCentralCommitteeSize(kingdom);
            List<PartyLeadershipMember> committee =
                SelectCentralCommitteeMembers(
                    kingdom,
                    rulingParty,
                    desiredSize,
                    currentYear
                );

            SaveCentralCommittee(kingdom, committee);
            SetKingdomIntData(
                kingdom,
                CentralCommitteeSizeDataKey,
                committee.Count
            );

            List<PartyLeadershipMember> politburo =
                SelectPolitburoMembers(
                    kingdom,
                    rulingParty,
                    committee,
                    currentYear
                );
            SavePolitburo(kingdom, politburo);

            string oldSecretaryId = GetKingdomStringData(
                kingdom,
                GeneralSecretaryIdentityDataKey,
                ""
            );
            PartyLeadershipMember secretary =
                SelectGeneralSecretary(
                    kingdom,
                    rulingParty,
                    committee,
                    currentYear,
                    oldSecretaryId
                );

            SetGeneralSecretary(
                kingdom,
                rulingParty,
                secretary,
                currentYear,
                false
            );

            SetKingdomIntData(
                kingdom,
                PartyCongressLastYearDataKey,
                currentYear
            );
            SetKingdomIntData(
                kingdom,
                PartyCongressNextYearDataKey,
                Math.Max(1, currentYear + PartyCongressIntervalYears)
            );
            SetKingdomIntData(
                kingdom,
                PartyLeadershipLastCheckYearDataKey,
                currentYear
            );
            SetKingdomIntData(
                kingdom,
                PartyLeadershipSchemaVersionDataKey,
                PartyLeadershipSchemaVersion
            );

            string secretaryName = secretary != null
                ? secretary.Name
                : LM.Get("ukiol_general_secretary_none");

            PublishPoliticalEvent(
                string.Format(
                    migrationCongress
                        ? LM.Get("ukiol_event_party_congress_migration")
                        : LM.Get("ukiol_event_party_congress"),
                    GetWorldObjectDisplayName(kingdom),
                    committee.Count,
                    secretaryName
                ),
                kingdom,
                null,
                secretary != null ? secretary.Actor : GetLivingRuler(kingdom),
                PartiesIconPath,
                migrationCongress
                    ? "party_congress_migration"
                    : "party_congress",
                35f
            );
        }

        private static int CalculateCentralCommitteeSize(Kingdom kingdom)
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

            int size = 5 + cities.Count * 2 + population / 250;
            size = ClampInt(size, 5, 31);
            if (size % 2 == 0)
            {
                size = Math.Min(31, size + 1);
            }
            return size;
        }

        private static List<PartyLeadershipMember>
            SelectCentralCommitteeMembers(
                Kingdom kingdom,
                PoliticalParty rulingParty,
                int desiredSize,
                int currentYear
            )
        {
            List<PartyLeadershipCandidate> candidates =
                BuildPartyLeadershipCandidates(
                    kingdom,
                    rulingParty,
                    currentYear
                );
            List<PartyLeadershipMember> result =
                new List<PartyLeadershipMember>();

            for (
                int i = 0;
                i < candidates.Count && result.Count < desiredSize;
                i++
            )
            {
                PartyLeadershipCandidate candidate = candidates[i];
                if (candidate == null || candidate.Actor == null)
                {
                    continue;
                }
                result.Add(
                    new PartyLeadershipMember()
                    {
                        Identity = candidate.Identity,
                        Name = candidate.Name,
                        Current = candidate.Current,
                        Actor = candidate.Actor
                    }
                );
            }

            return result;
        }

        private static List<PartyLeadershipCandidate>
            BuildPartyLeadershipCandidates(
                Kingdom kingdom,
                PoliticalParty rulingParty,
                int currentYear
            )
        {
            List<PartyLeadershipCandidate> result =
                new List<PartyLeadershipCandidate>();
            if (kingdom == null || rulingParty == null)
            {
                return result;
            }

            string rulingIdeology = rulingParty.Ideology;
            string rulerId = GetStableObjectIdentity(GetLivingRuler(kingdom));
            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (
                int c = 0;
                c < cities.Count && inspected < 800;
                c++
            )
            {
                List<Actor> units = GetCityUnitsSafe(cities[c]);
                for (
                    int i = 0;
                    i < units.Count && inspected < 800;
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

                    string citizenIdeology = GetCitizenIdeology(actor);
                    float score = 0f;
                    if (citizenIdeology == rulingIdeology)
                    {
                        score += 90f;
                        score += GetCitizenIdeologyConviction(actor) * 0.65f;
                    }
                    else if (citizenIdeology == GetStateIdeology(kingdom))
                    {
                        score += 35f;
                    }
                    else
                    {
                        score -= 25f;
                    }

                    if (identity == rulingParty.LeaderIdentity)
                    {
                        score += 220f;
                    }
                    if (!string.IsNullOrEmpty(rulerId) && identity == rulerId)
                    {
                        score += 35f;
                    }

                    try
                    {
                        if (actor.stats != null)
                        {
                            score += actor.stats[S.diplomacy] * 0.75f;
                            score += actor.stats[S.stewardship] * 0.70f;
                            score += actor.stats[S.warfare] * 0.45f;
                        }
                    }
                    catch
                    {
                    }

                    score += (
                        Math.Abs(
                            StablePartyHash(
                                identity + "|cc|" + currentYear
                            )
                        ) % 120
                    ) / 20f;

                    result.Add(
                        new PartyLeadershipCandidate()
                        {
                            Actor = actor,
                            Identity = identity,
                            Name = GetWorldObjectDisplayName(actor),
                            Current = DeterminePartyInternalCurrent(
                                actor,
                                rulingParty,
                                currentYear
                            ),
                            Score = score
                        }
                    );
                }
            }

            result.Sort(
                delegate(
                    PartyLeadershipCandidate a,
                    PartyLeadershipCandidate b
                )
                {
                    int scoreCompare = b.Score.CompareTo(a.Score);
                    if (scoreCompare != 0)
                    {
                        return scoreCompare;
                    }
                    return string.CompareOrdinal(
                        a.Identity ?? "",
                        b.Identity ?? ""
                    );
                }
            );
            return result;
        }

        private static string DeterminePartyInternalCurrent(
            Actor actor,
            PoliticalParty rulingParty,
            int currentYear
        )
        {
            if (actor == null)
            {
                return PartyCurrentOrthodoxId;
            }

            string politicalTrait = GetPoliticalTrait(actor);
            if (politicalTrait == ReformerTraitId)
            {
                return PartyCurrentReformistId;
            }
            if (politicalTrait == MilitaristTraitId)
            {
                return PartyCurrentMilitaristId;
            }

            float diplomacy = 0f;
            float stewardship = 0f;
            float warfare = 0f;
            try
            {
                if (actor.stats != null)
                {
                    diplomacy = actor.stats[S.diplomacy];
                    stewardship = actor.stats[S.stewardship];
                    warfare = actor.stats[S.warfare];
                }
            }
            catch
            {
            }

            if (warfare >= diplomacy + 3f && warfare >= stewardship + 2f)
            {
                return PartyCurrentMilitaristId;
            }
            if (
                politicalTrait == DiplomatTraitId ||
                diplomacy + stewardship >= warfare * 2f + 6f
            )
            {
                return PartyCurrentReformistId;
            }

            int hash = Math.Abs(
                StablePartyHash(
                    GetStableObjectIdentity(actor) +
                    "|current|" +
                    (rulingParty != null ? rulingParty.Id : "") +
                    "|" +
                    (currentYear / PartyCongressIntervalYears)
                )
            );
            if (hash % 10 < 2)
            {
                return PartyCurrentNationalId;
            }
            return PartyCurrentOrthodoxId;
        }

        private static List<PartyLeadershipMember> SelectPolitburoMembers(
            Kingdom kingdom,
            PoliticalParty rulingParty,
            List<PartyLeadershipMember> committee,
            int currentYear
        )
        {
            List<PartyLeadershipMember> result =
                new List<PartyLeadershipMember>();
            if (committee == null || committee.Count == 0)
            {
                return result;
            }

            int desired = ClampInt(
                3 + committee.Count / 5,
                3,
                Math.Min(9, committee.Count)
            );
            List<PartyLeadershipMember> ordered =
                new List<PartyLeadershipMember>(committee);
            ordered.Sort(
                delegate(
                    PartyLeadershipMember a,
                    PartyLeadershipMember b
                )
                {
                    float aScore = CalculateLeadershipMemberScore(
                        kingdom,
                        rulingParty,
                        a,
                        currentYear,
                        "politburo"
                    );
                    float bScore = CalculateLeadershipMemberScore(
                        kingdom,
                        rulingParty,
                        b,
                        currentYear,
                        "politburo"
                    );
                    int scoreCompare = bScore.CompareTo(aScore);
                    if (scoreCompare != 0)
                    {
                        return scoreCompare;
                    }
                    return string.CompareOrdinal(
                        a.Identity ?? "",
                        b.Identity ?? ""
                    );
                }
            );

            for (int i = 0; i < ordered.Count && result.Count < desired; i++)
            {
                result.Add(ordered[i]);
            }
            return result;
        }

        private static PartyLeadershipMember SelectGeneralSecretary(
            Kingdom kingdom,
            PoliticalParty rulingParty,
            List<PartyLeadershipMember> committee,
            int currentYear,
            string incumbentIdentity
        )
        {
            if (committee == null || committee.Count == 0)
            {
                return null;
            }

            PartyLeadershipMember best = null;
            float bestScore = -100000f;
            for (int i = 0; i < committee.Count; i++)
            {
                PartyLeadershipMember member = committee[i];
                float score = CalculateLeadershipMemberScore(
                    kingdom,
                    rulingParty,
                    member,
                    currentYear,
                    "secretary"
                );
                if (
                    member != null &&
                    member.Identity == incumbentIdentity
                )
                {
                    score += 12f;
                }
                if (
                    rulingParty != null &&
                    member != null &&
                    member.Identity == rulingParty.LeaderIdentity
                )
                {
                    score += 22f;
                }
                if (
                    best == null ||
                    score > bestScore ||
                    (Math.Abs(score - bestScore) < 0.001f &&
                        string.CompareOrdinal(
                            member.Identity ?? "",
                            best.Identity ?? ""
                        ) < 0)
                )
                {
                    best = member;
                    bestScore = score;
                }
            }
            return best;
        }

        private static float CalculateLeadershipMemberScore(
            Kingdom kingdom,
            PoliticalParty rulingParty,
            PartyLeadershipMember member,
            int currentYear,
            string salt
        )
        {
            if (member == null)
            {
                return -100000f;
            }

            Actor actor = member.Actor;
            if (actor == null)
            {
                actor = FindCommitteeActorByIdentity(
                    kingdom,
                    member.Identity,
                    member.Name
                );
            }
            if (actor == null || !actor.isAlive())
            {
                return -100000f;
            }

            float score = GetCitizenIdeologyConviction(actor) * 0.55f;
            try
            {
                if (actor.stats != null)
                {
                    score += actor.stats[S.diplomacy] * 0.90f;
                    score += actor.stats[S.stewardship] * 0.85f;
                    score += actor.stats[S.warfare] * 0.35f;
                }
            }
            catch
            {
            }

            if (member.Current == PartyCurrentOrthodoxId)
            {
                score += 4f;
            }
            if (
                rulingParty != null &&
                member.Identity == rulingParty.LeaderIdentity
            )
            {
                score += 18f;
            }
            score += (
                Math.Abs(
                    StablePartyHash(
                        (member.Identity ?? "") +
                        "|" + salt + "|" + currentYear
                    )
                ) % 90
            ) / 15f;
            member.Actor = actor;
            return score;
        }

        private static void MaintainPartyLeadership(
            Kingdom kingdom,
            int currentYear
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            int lastCheck = GetKingdomIntData(
                kingdom,
                PartyLeadershipLastCheckYearDataKey,
                -1
            );
            if (lastCheck == currentYear)
            {
                return;
            }
            SetKingdomIntData(
                kingdom,
                PartyLeadershipLastCheckYearDataKey,
                currentYear
            );

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            PoliticalParty rulingParty = FindPoliticalPartyById(
                parties,
                GetKingdomStringData(
                    kingdom,
                    ElectionRulingPartyIdDataKey,
                    ""
                )
            );
            if (rulingParty == null)
            {
                return;
            }

            List<PartyLeadershipMember> committee =
                LoadCentralCommittee(kingdom);
            if (committee.Count == 0)
            {
                return;
            }

            List<PartyLeadershipMember> living =
                new List<PartyLeadershipMember>();
            HashSet<string> used = new HashSet<string>();
            for (int i = 0; i < committee.Count; i++)
            {
                PartyLeadershipMember member = committee[i];
                Actor actor = FindCommitteeActorByIdentity(
                    kingdom,
                    member.Identity,
                    member.Name
                );
                if (actor == null || !actor.isAlive())
                {
                    continue;
                }
                member.Actor = actor;
                member.Identity = GetStableObjectIdentity(actor);
                member.Name = GetWorldObjectDisplayName(actor);
                if (used.Add(member.Identity))
                {
                    living.Add(member);
                }
            }

            int desired = CalculateCentralCommitteeSize(kingdom);
            if (living.Count < desired)
            {
                List<PartyLeadershipCandidate> candidates =
                    BuildPartyLeadershipCandidates(
                        kingdom,
                        rulingParty,
                        currentYear
                    );
                for (
                    int i = 0;
                    i < candidates.Count && living.Count < desired;
                    i++
                )
                {
                    PartyLeadershipCandidate candidate = candidates[i];
                    if (
                        candidate == null ||
                        candidate.Actor == null ||
                        !used.Add(candidate.Identity)
                    )
                    {
                        continue;
                    }
                    living.Add(
                        new PartyLeadershipMember()
                        {
                            Identity = candidate.Identity,
                            Name = candidate.Name,
                            Current = candidate.Current,
                            Actor = candidate.Actor
                        }
                    );
                }
            }

            SaveCentralCommittee(kingdom, living);
            SetKingdomIntData(
                kingdom,
                CentralCommitteeSizeDataKey,
                living.Count
            );
            SavePolitburo(
                kingdom,
                SelectPolitburoMembers(
                    kingdom,
                    rulingParty,
                    living,
                    currentYear
                )
            );

            string secretaryId = GetKingdomStringData(
                kingdom,
                GeneralSecretaryIdentityDataKey,
                ""
            );
            Actor secretaryActor = FindCommitteeActorByIdentity(
                kingdom,
                secretaryId,
                GetKingdomStringData(
                    kingdom,
                    GeneralSecretaryNameDataKey,
                    ""
                )
            );

            bool secretaryStillInCommittee = false;
            for (int i = 0; i < living.Count; i++)
            {
                if (living[i].Identity == secretaryId)
                {
                    secretaryStillInCommittee = true;
                    break;
                }
            }

            if (
                secretaryActor == null ||
                !secretaryActor.isAlive() ||
                !secretaryStillInCommittee
            )
            {
                string previousName = GetKingdomStringData(
                    kingdom,
                    GeneralSecretaryNameDataKey,
                    ""
                );
                PartyLeadershipMember replacement =
                    SelectGeneralSecretary(
                        kingdom,
                        rulingParty,
                        living,
                        currentYear,
                        ""
                    );
                SetGeneralSecretary(
                    kingdom,
                    rulingParty,
                    replacement,
                    currentYear,
                    true
                );

                if (replacement != null)
                {
                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_general_secretary_succession"),
                            GetWorldObjectDisplayName(kingdom),
                            replacement.Name,
                            string.IsNullOrEmpty(previousName)
                                ? LM.Get("ukiol_party_leader_unknown")
                                : previousName
                        ),
                        kingdom,
                        null,
                        replacement.Actor,
                        PartiesIconPath,
                        "general_secretary_succession",
                        35f
                    );
                }
            }
        }

        private static void SetGeneralSecretary(
            Kingdom kingdom,
            PoliticalParty rulingParty,
            PartyLeadershipMember secretary,
            int currentYear,
            bool emergencySuccession
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            if (secretary == null || secretary.Actor == null)
            {
                SetKingdomStringData(
                    kingdom,
                    GeneralSecretaryIdentityDataKey,
                    ""
                );
                SetKingdomStringData(
                    kingdom,
                    GeneralSecretaryNameDataKey,
                    ""
                );
                SetKingdomStringData(
                    kingdom,
                    GeneralSecretaryPartyIdDataKey,
                    rulingParty != null ? rulingParty.Id : ""
                );
                return;
            }

            string previousLeaderName = rulingParty != null
                ? rulingParty.LeaderName
                : "";
            string identity = GetStableObjectIdentity(secretary.Actor);
            string name = GetWorldObjectDisplayName(secretary.Actor);

            SetKingdomStringData(
                kingdom,
                GeneralSecretaryIdentityDataKey,
                identity
            );
            SetKingdomStringData(
                kingdom,
                GeneralSecretaryNameDataKey,
                name
            );
            SetKingdomStringData(
                kingdom,
                GeneralSecretaryPartyIdDataKey,
                rulingParty != null ? rulingParty.Id : ""
            );

            if (rulingParty != null)
            {
                rulingParty.LeaderActor = secretary.Actor;
                rulingParty.LeaderIdentity = identity;
                rulingParty.LeaderName = name;
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderIdentityPrefix,
                        rulingParty.Slot
                    ),
                    identity
                );
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderNamePrefix,
                        rulingParty.Slot
                    ),
                    name
                );

                if (
                    !string.IsNullOrEmpty(previousLeaderName) &&
                    previousLeaderName != name
                )
                {
                    RecordPartyHistoryEvent(
                        kingdom,
                        rulingParty,
                        PartyHistoryLeaderChanged,
                        name,
                        previousLeaderName,
                        currentYear
                    );
                }
            }
        }

        private static void SaveCentralCommittee(
            Kingdom kingdom,
            List<PartyLeadershipMember> members
        )
        {
            SavePartyLeadershipMemberList(
                kingdom,
                members,
                CentralCommitteeMemberIdsDataKey,
                CentralCommitteeMemberNamesDataKey,
                CentralCommitteeMemberCurrentsDataKey
            );
        }

        private static List<PartyLeadershipMember> LoadCentralCommittee(
            Kingdom kingdom
        )
        {
            return LoadPartyLeadershipMemberList(
                kingdom,
                CentralCommitteeMemberIdsDataKey,
                CentralCommitteeMemberNamesDataKey,
                CentralCommitteeMemberCurrentsDataKey
            );
        }

        private static void SavePolitburo(
            Kingdom kingdom,
            List<PartyLeadershipMember> members
        )
        {
            SavePartyLeadershipMemberList(
                kingdom,
                members,
                PolitburoMemberIdsDataKey,
                PolitburoMemberNamesDataKey,
                ""
            );
        }

        private static void SavePartyLeadershipMemberList(
            Kingdom kingdom,
            List<PartyLeadershipMember> members,
            string idsKey,
            string namesKey,
            string currentsKey
        )
        {
            List<string> ids = new List<string>();
            List<string> names = new List<string>();
            List<string> currents = new List<string>();
            if (members != null)
            {
                for (int i = 0; i < members.Count; i++)
                {
                    PartyLeadershipMember member = members[i];
                    if (member == null)
                    {
                        continue;
                    }
                    ids.Add(member.Identity ?? "");
                    names.Add(member.Name ?? "");
                    currents.Add(member.Current ?? PartyCurrentOrthodoxId);
                }
            }

            SetKingdomStringData(
                kingdom,
                idsKey,
                string.Join(
                    PartyLeadershipListSeparator.ToString(),
                    ids.ToArray()
                )
            );
            SetKingdomStringData(
                kingdom,
                namesKey,
                string.Join(
                    PartyLeadershipListSeparator.ToString(),
                    names.ToArray()
                )
            );
            if (!string.IsNullOrEmpty(currentsKey))
            {
                SetKingdomStringData(
                    kingdom,
                    currentsKey,
                    string.Join(
                        PartyLeadershipListSeparator.ToString(),
                        currents.ToArray()
                    )
                );
            }
        }

        private static List<PartyLeadershipMember>
            LoadPartyLeadershipMemberList(
                Kingdom kingdom,
                string idsKey,
                string namesKey,
                string currentsKey
            )
        {
            List<PartyLeadershipMember> result =
                new List<PartyLeadershipMember>();
            string idsText = GetKingdomStringData(kingdom, idsKey, "");
            string namesText = GetKingdomStringData(kingdom, namesKey, "");
            string currentsText = string.IsNullOrEmpty(currentsKey)
                ? ""
                : GetKingdomStringData(kingdom, currentsKey, "");

            if (string.IsNullOrEmpty(idsText) && string.IsNullOrEmpty(namesText))
            {
                return result;
            }

            char[] separator = new char[] { PartyLeadershipListSeparator };
            string[] ids = idsText.Split(separator);
            string[] names = namesText.Split(separator);
            string[] currents = string.IsNullOrEmpty(currentsText)
                ? new string[0]
                : currentsText.Split(separator);
            int count = Math.Max(ids.Length, names.Length);

            for (int i = 0; i < count; i++)
            {
                string identity = i < ids.Length ? ids[i] : "";
                string name = i < names.Length ? names[i] : "";
                if (string.IsNullOrEmpty(identity) && string.IsNullOrEmpty(name))
                {
                    continue;
                }
                result.Add(
                    new PartyLeadershipMember()
                    {
                        Identity = identity,
                        Name = name,
                        Current = i < currents.Length && !string.IsNullOrEmpty(currents[i])
                            ? currents[i]
                            : PartyCurrentOrthodoxId,
                        Actor = null
                    }
                );
            }
            return result;
        }

        private static Actor FindCommitteeActorByIdentity(
            Kingdom kingdom,
            string identity,
            string fallbackName
        )
        {
            if (
                kingdom == null ||
                (string.IsNullOrEmpty(identity) && string.IsNullOrEmpty(fallbackName))
            )
            {
                return null;
            }

            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);
            for (
                int c = 0;
                c < cities.Count && inspected < 1000;
                c++
            )
            {
                List<Actor> units = GetCityUnitsSafe(cities[c]);
                for (
                    int i = 0;
                    i < units.Count && inspected < 1000;
                    i++
                )
                {
                    Actor actor = units[i];
                    inspected++;
                    if (actor == null || !actor.isAlive())
                    {
                        continue;
                    }
                    if (
                        !string.IsNullOrEmpty(identity) &&
                        GetStableObjectIdentity(actor) == identity
                    )
                    {
                        return actor;
                    }
                    if (
                        string.IsNullOrEmpty(identity) &&
                        !string.IsNullOrEmpty(fallbackName) &&
                        GetWorldObjectDisplayName(actor) == fallbackName
                    )
                    {
                        return actor;
                    }
                }
            }
            return null;
        }

        private static List<PartyLeadershipMember> LoadPolitburo(
            Kingdom kingdom
        )
        {
            return LoadPartyLeadershipMemberList(
                kingdom,
                PolitburoMemberIdsDataKey,
                PolitburoMemberNamesDataKey,
                ""
            );
        }

        private static string FormatPolitburoSummary(Kingdom kingdom)
        {
            List<PartyLeadershipMember> members = LoadPolitburo(kingdom);
            if (members.Count == 0)
            {
                return LM.Get("ukiol_central_committee_forming");
            }
            List<string> names = new List<string>();
            int visible = Math.Min(4, members.Count);
            for (int i = 0; i < visible; i++)
            {
                names.Add(members[i].Name);
            }
            string result = string.Join(", ", names.ToArray());
            if (members.Count > visible)
            {
                result += " " + string.Format(
                    LM.Get("ukiol_central_committee_more"),
                    members.Count - visible
                );
            }
            return result;
        }

        private static string FormatCentralCommitteeMemberSummary(
            Kingdom kingdom
        )
        {
            List<PartyLeadershipMember> committee =
                LoadCentralCommittee(kingdom);
            if (committee.Count == 0)
            {
                return LM.Get("ukiol_central_committee_forming");
            }

            int visible = Math.Min(4, committee.Count);
            List<string> names = new List<string>();
            for (int i = 0; i < visible; i++)
            {
                names.Add(committee[i].Name);
            }
            string result = string.Join(", ", names.ToArray());
            if (committee.Count > visible)
            {
                result += " " + string.Format(
                    LM.Get("ukiol_central_committee_more"),
                    committee.Count - visible
                );
            }
            return result;
        }

        private static string FormatPartyInternalCurrents(Kingdom kingdom)
        {
            List<PartyLeadershipMember> committee =
                LoadCentralCommittee(kingdom);
            if (committee.Count == 0)
            {
                return LM.Get("ukiol_party_internal_currents_none");
            }

            Dictionary<string, int> counts =
                new Dictionary<string, int>();
            counts[PartyCurrentOrthodoxId] = 0;
            counts[PartyCurrentReformistId] = 0;
            counts[PartyCurrentMilitaristId] = 0;
            counts[PartyCurrentNationalId] = 0;

            for (int i = 0; i < committee.Count; i++)
            {
                string current = committee[i].Current;
                if (!counts.ContainsKey(current))
                {
                    counts[current] = 0;
                }
                counts[current]++;
            }

            List<string> parts = new List<string>();
            string[] order = new string[]
            {
                PartyCurrentOrthodoxId,
                PartyCurrentReformistId,
                PartyCurrentMilitaristId,
                PartyCurrentNationalId
            };
            for (int i = 0; i < order.Length; i++)
            {
                int count = counts.ContainsKey(order[i]) ? counts[order[i]] : 0;
                if (count <= 0)
                {
                    continue;
                }
                parts.Add(
                    GetPartyInternalCurrentName(order[i]) + " " + count
                );
            }
            return parts.Count > 0
                ? string.Join(" • ", parts.ToArray())
                : LM.Get("ukiol_party_internal_currents_none");
        }

        private static string GetPartyInternalCurrentName(string current)
        {
            if (current == PartyCurrentReformistId)
            {
                return LM.Get("ukiol_party_current_reformists");
            }
            if (current == PartyCurrentMilitaristId)
            {
                return LM.Get("ukiol_party_current_militarists");
            }
            if (current == PartyCurrentNationalId)
            {
                return LM.Get("ukiol_party_current_national");
            }
            return LM.Get("ukiol_party_current_orthodox");
        }

        private static void ClearPartyCongressFoundation(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }
            SetKingdomIntData(kingdom, PartyCongressLastYearDataKey, 0);
            SetKingdomIntData(kingdom, PartyCongressNextYearDataKey, 0);
            SetKingdomIntData(kingdom, CentralCommitteeSizeDataKey, 0);
            SetKingdomStringData(kingdom, CentralCommitteeMemberIdsDataKey, "");
            SetKingdomStringData(kingdom, CentralCommitteeMemberNamesDataKey, "");
            SetKingdomStringData(kingdom, CentralCommitteeMemberCurrentsDataKey, "");
            SetKingdomStringData(kingdom, PolitburoMemberIdsDataKey, "");
            SetKingdomStringData(kingdom, PolitburoMemberNamesDataKey, "");
            SetKingdomStringData(kingdom, GeneralSecretaryIdentityDataKey, "");
            SetKingdomStringData(kingdom, GeneralSecretaryNameDataKey, "");
            SetKingdomStringData(kingdom, GeneralSecretaryPartyIdDataKey, "");
            SetKingdomIntData(kingdom, PartyLeadershipLastCheckYearDataKey, -1);
            SetKingdomIntData(kingdom, PartyLeadershipSchemaVersionDataKey, 0);
        }

    }
}
