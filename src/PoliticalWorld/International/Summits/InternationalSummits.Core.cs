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
        // Step 9E FAST: physical bloc summits and congresses, moved unchanged from Main.cs.

        // -----------------------------------------------------------------
        // v1.5.0-dev15 - Summits & International Congresses
        // -----------------------------------------------------------------

        private static void ScheduleInternationalBlocSummits(int currentYear)
        {
            List<InternationalBlocSnapshot> blocs =
                new List<InternationalBlocSnapshot>(InternationalBlocs.Values);
            for (int i = 0; i < blocs.Count; i++)
            {
                InternationalBlocSnapshot bloc = blocs[i];
                if (
                    bloc == null || bloc.Members == null ||
                    bloc.Members.Count < InternationalBlocMinimumMembers ||
                    ActiveInternationalSummits.ContainsKey(bloc.Id)
                )
                {
                    continue;
                }

                int nextYear = GetInternationalBlocNextSummitYear(bloc);
                if (nextYear <= 0)
                {
                    nextYear = currentYear + InternationalSummitInitialDelayYears;
                    SetInternationalBlocNextSummitYear(bloc, nextYear);
                    continue;
                }
                if (currentYear < nextYear)
                {
                    continue;
                }

                StartInternationalBlocSummit(bloc, currentYear);
            }
        }

        private static int GetInternationalBlocNextSummitYear(
            InternationalBlocSnapshot bloc
        )
        {
            if (bloc == null || bloc.Members == null)
            {
                return 0;
            }
            int result = 0;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (member == null) continue;
                int value = GetKingdomIntData(
                    member,
                    InternationalSummitNextYearDataKey,
                    0
                );
                if (value > 0 && (result <= 0 || value < result))
                {
                    result = value;
                }
            }
            return result;
        }

        private static void SetInternationalBlocNextSummitYear(
            InternationalBlocSnapshot bloc,
            int year
        )
        {
            if (bloc == null || bloc.Members == null) return;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                SetKingdomIntData(
                    bloc.Members[i],
                    InternationalSummitNextYearDataKey,
                    Math.Max(0, year)
                );
            }
        }

        private static void StartInternationalBlocSummit(
            InternationalBlocSnapshot bloc,
            int currentYear
        )
        {
            if (
                bloc == null || bloc.Members == null ||
                bloc.Members.Count < InternationalBlocMinimumMembers
            )
            {
                return;
            }

            Kingdom hostKingdom = SelectInternationalSummitHost(bloc, currentYear);
            City hostCity = GetKingdomCapitalCitySafe(hostKingdom);
            if (hostKingdom == null || hostCity == null)
            {
                SetInternationalBlocNextSummitYear(
                    bloc,
                    currentYear + InternationalSummitRetryYears
                );
                return;
            }

            InternationalSummitSnapshot summit = new InternationalSummitSnapshot();
            summit.Id = "S" + currentYear.ToString() + "_" +
                StablePartyHash(bloc.Id + "|" + currentYear.ToString()).ToString();
            summit.BlocId = bloc.Id;
            summit.Bloc = bloc;
            summit.HostKingdom = hostKingdom;
            summit.HostCity = hostCity;
            summit.StartedYear = currentYear;
            summit.Stage = 1;
            summit.StageEndsAt = Time.time + InternationalSummitTravelSeconds;
            summit.NextTravelOrderAt = 0f;
            summit.CandidateKingdom = FindInternationalSummitAdmissionCandidate(bloc);
            summit.LeadershipCandidate = GetStrongestInternationalBlocLeader(bloc);
            summit.Agenda = SelectInternationalSummitAgenda(summit);

            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                Actor ruler = GetLivingRuler(member);
                if (member == null || ruler == null) continue;
                summit.Participants.Add(member);
                summit.Delegates[GetStableObjectIdentity(member)] = ruler;
            }

            if (summit.Participants.Count < InternationalSummitMinimumQuorum)
            {
                SetInternationalBlocNextSummitYear(
                    bloc,
                    currentYear + InternationalSummitRetryYears
                );
                return;
            }

            ActiveInternationalSummits[bloc.Id] = summit;
            SyncInternationalSummitMirror(summit);
            IssueInternationalSummitTravelOrders(summit);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_summit_called"),
                    bloc.Name,
                    GetWorldObjectDisplayName(hostCity),
                    GetInternationalSummitAgendaName(summit.Agenda),
                    summit.Participants.Count
                ),
                hostKingdom,
                hostCity,
                GetLivingRuler(hostKingdom),
                DiplomatIconPath,
                "summit_called_" + summit.Id,
                20f
            );
        }

        private static Kingdom SelectInternationalSummitHost(
            InternationalBlocSnapshot bloc,
            int currentYear
        )
        {
            if (bloc == null || bloc.Members == null || bloc.Members.Count == 0)
            {
                return null;
            }
            int seed = Math.Abs(StablePartyHash(bloc.Id + "|host"));
            int start = (seed + currentYear) % bloc.Members.Count;
            for (int step = 0; step < bloc.Members.Count; step++)
            {
                Kingdom candidate = bloc.Members[(start + step) % bloc.Members.Count];
                if (
                    candidate != null &&
                    GetLivingRuler(candidate) != null &&
                    GetKingdomCapitalCitySafe(candidate) != null &&
                    !IsKingdomAtWarSafe(candidate)
                )
                {
                    return candidate;
                }
            }
            return bloc.Leader ?? bloc.Members[0];
        }

        private static City GetKingdomCapitalCitySafe(Kingdom kingdom)
        {
            if (kingdom == null) return null;
            City capital = GetMemberValue(kingdom, "capital", "_capital") as City;
            if (capital != null && GetKingdomFromObject(capital) == kingdom)
            {
                return capital;
            }
            List<City> cities = GetCitiesSafe(kingdom);
            return cities.Count > 0 ? cities[0] : null;
        }

        private static Kingdom FindInternationalSummitAdmissionCandidate(
            InternationalBlocSnapshot bloc
        )
        {
            if (bloc == null || bloc.Members == null || bloc.Members.Count >= 7)
            {
                return null;
            }
            List<Kingdom> kingdoms = GetKingdomsSafe();
            Kingdom best = null;
            int bestScore = 60;
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom candidate = kingdoms[i];
                if (
                    candidate == null || bloc.Members.Contains(candidate) ||
                    !string.IsNullOrEmpty(GetKingdomStringData(
                        candidate, InternationalBlocIdDataKey, "")) ||
                    IsKingdomAtWarSafe(candidate) ||
                    GetDiplomaticReputation(candidate) < 38
                )
                {
                    continue;
                }
                int score = CalculateCandidateBlocCompatibility(candidate, bloc);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        private static string SelectInternationalSummitAgenda(
            InternationalSummitSnapshot summit
        )
        {
            InternationalBlocSnapshot bloc = summit == null ? null : summit.Bloc;
            if (bloc == null) return "common_declaration";

            if (
                summit.CandidateKingdom != null &&
                UnityEngine.Random.Range(0, 100) < 28
            )
            {
                return "admission";
            }
            if (
                summit.LeadershipCandidate != null && bloc.Leader != null &&
                summit.LeadershipCandidate != bloc.Leader &&
                GetBlocLeadershipScore(summit.LeadershipCandidate) >=
                    GetBlocLeadershipScore(bloc.Leader) + 18
            )
            {
                return "leadership_review";
            }
            if (bloc.Integration < 65)
            {
                return "integration";
            }
            if (IsSecurityInternationalBlocType(bloc.Type))
            {
                return "collective_defense";
            }
            if (IsEconomicInternationalBlocType(bloc.Type))
            {
                return "economic_coordination";
            }
            return "common_declaration";
        }

        private static void UpdateInternationalSummits()
        {
            CleanupStaleInternationalSummitMirrors();
            if (ActiveInternationalSummits.Count == 0) return;

            List<string> ids = new List<string>(ActiveInternationalSummits.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                InternationalSummitSnapshot summit;
                if (!ActiveInternationalSummits.TryGetValue(ids[i], out summit))
                    continue;
                if (summit == null || summit.Bloc == null)
                {
                    ActiveInternationalSummits.Remove(ids[i]);
                    continue;
                }

                if (
                    summit.HostCity == null || summit.HostKingdom == null ||
                    GetKingdomFromObject(summit.HostCity) != summit.HostKingdom
                )
                {
                    CancelInternationalSummit(summit, "host_lost");
                    continue;
                }

                if (summit.Stage == 1)
                {
                    if (Time.time >= summit.NextTravelOrderAt)
                    {
                        IssueInternationalSummitTravelOrders(summit);
                        summit.NextTravelOrderAt = Time.time + 2.5f;
                    }
                    RefreshInternationalSummitArrivals(summit);
                    SyncInternationalSummitMirror(summit);

                    if (
                        summit.Arrived.Count >= summit.Participants.Count ||
                        Time.time >= summit.StageEndsAt
                    )
                    {
                        if (summit.Arrived.Count < InternationalSummitMinimumQuorum)
                        {
                            CancelInternationalSummit(summit, "no_quorum");
                            continue;
                        }
                        summit.Stage = 2;
                        summit.StageEndsAt = Time.time + InternationalSummitSessionSeconds;
                        SyncInternationalSummitMirror(summit);
                        PublishPoliticalEvent(
                            string.Format(
                                LM.Get("ukiol_event_summit_opened"),
                                summit.Bloc.Name,
                                summit.Arrived.Count,
                                summit.Participants.Count,
                                GetWorldObjectDisplayName(summit.HostCity)
                            ),
                            summit.HostKingdom,
                            summit.HostCity,
                            GetLivingRuler(summit.HostKingdom),
                            DiplomatIconPath,
                            "summit_opened_" + summit.Id,
                            20f
                        );
                    }
                }
                else if (summit.Stage == 2 && Time.time >= summit.StageEndsAt)
                {
                    ResolveInternationalSummitVote(summit);
                    summit.Stage = 3;
                    summit.StageEndsAt = Time.time + InternationalSummitClosingSeconds;
                    SyncInternationalSummitMirror(summit);
                }
                else if (summit.Stage == 3 && Time.time >= summit.StageEndsAt)
                {
                    CompleteInternationalSummit(summit);
                }
            }
        }

        private static void IssueInternationalSummitTravelOrders(
            InternationalSummitSnapshot summit
        )
        {
            if (summit == null || summit.HostCity == null) return;
            for (int i = 0; i < summit.Participants.Count; i++)
            {
                Kingdom member = summit.Participants[i];
                if (member == null) continue;
                string id = GetStableObjectIdentity(member);
                if (summit.Arrived.Contains(id)) continue;
                Actor actor;
                if (!summit.Delegates.TryGetValue(id, out actor) ||
                    actor == null || !actor.isAlive())
                {
                    actor = GetLivingRuler(member);
                    if (actor == null) continue;
                    summit.Delegates[id] = actor;
                }
                if (member == summit.HostKingdom)
                {
                    summit.Arrived.Add(id);
                    continue;
                }
                TrySendActorToSummitCity(actor, summit.HostCity);
            }
        }

        private static bool TrySendActorToSummitCity(Actor actor, City city)
        {
            if (actor == null || city == null) return false;
            object tile = null;
            try { tile = city.getTile(); } catch { }
            if (tile == null) return false;

            if (TryInvokeActorTravelTarget(actor, tile)) return true;
            object ai = GetMemberValue(actor, "ai", "_ai", "brain", "behaviour");
            if (ai != null && TryInvokeActorTravelTarget(ai, tile)) return true;
            return false;
        }

        private static bool TryInvokeActorTravelTarget(object owner, object tile)
        {
            if (owner == null || tile == null) return false;
            string[] names = new string[]
            {
                "goTo", "goToTile", "walkTo", "moveTo", "setMoveTarget",
                "setTargetTile", "setTarget"
            };
            MethodInfo[] methods;
            try { methods = owner.GetType().GetMethods(MemberFlags); }
            catch { return false; }

            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != names[n]) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters.Length > 5) continue;
                    if (!parameters[0].ParameterType.IsInstanceOfType(tile) &&
                        parameters[0].ParameterType != typeof(object))
                    {
                        continue;
                    }
                    object[] args = new object[parameters.Length];
                    bool valid = true;
                    for (int p = 0; p < parameters.Length; p++)
                    {
                        ParameterInfo parameter = parameters[p];
                        if (p == 0) args[p] = tile;
                        else if (parameter.HasDefaultValue) args[p] = parameter.DefaultValue;
                        else if (parameter.ParameterType == typeof(bool)) args[p] = true;
                        else if (parameter.ParameterType == typeof(int)) args[p] = 0;
                        else if (parameter.ParameterType == typeof(float)) args[p] = 0f;
                        else { valid = false; break; }
                    }
                    if (!valid) continue;
                    try
                    {
                        method.Invoke(owner, args);
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        private static void RefreshInternationalSummitArrivals(
            InternationalSummitSnapshot summit
        )
        {
            if (summit == null || summit.HostCity == null) return;
            for (int i = 0; i < summit.Participants.Count; i++)
            {
                Kingdom member = summit.Participants[i];
                if (member == null) continue;
                string id = GetStableObjectIdentity(member);
                if (summit.Arrived.Contains(id)) continue;
                Actor actor;
                if (!summit.Delegates.TryGetValue(id, out actor) ||
                    actor == null || !actor.isAlive())
                {
                    continue;
                }
                if (
                    member == summit.HostKingdom ||
                    IsActorNearSummitCity(actor, summit.HostCity)
                )
                {
                    summit.Arrived.Add(id);
                }
            }
        }

        private static bool IsActorNearSummitCity(Actor actor, City city)
        {
            if (actor == null || city == null) return false;
            City actorCity = GetMemberValue(actor, "city", "_city") as City;
            if (actorCity == city) return true;

            object actorTile = GetMemberValue(
                actor, "currentTile", "current_tile", "tile", "_tile"
            );
            object cityTile = null;
            try { cityTile = city.getTile(); } catch { }
            if (actorTile == null || cityTile == null) return false;

            int ax, ay, cx, cy;
            if (!TryGetTileCoordinates(actorTile, out ax, out ay) ||
                !TryGetTileCoordinates(cityTile, out cx, out cy))
            {
                return object.ReferenceEquals(actorTile, cityTile);
            }
            return Math.Abs(ax - cx) + Math.Abs(ay - cy) <= 8;
        }

        private static bool TryGetTileCoordinates(
            object tile, out int x, out int y
        )
        {
            x = 0; y = 0;
            if (tile == null) return false;
            object xv = GetMemberValue(tile, "x", "posX", "tileX", "_x");
            object yv = GetMemberValue(tile, "y", "posY", "tileY", "_y");
            if (xv == null || yv == null) return false;
            try
            {
                x = Convert.ToInt32(xv);
                y = Convert.ToInt32(yv);
                return true;
            }
            catch { return false; }
        }

        private static void ResolveInternationalSummitVote(
            InternationalSummitSnapshot summit
        )
        {
            if (summit == null || summit.Bloc == null) return;
            RefreshInternationalSummitArrivals(summit);
            summit.YesVotes = 0;
            summit.NoVotes = 0;

            for (int i = 0; i < summit.Participants.Count; i++)
            {
                Kingdom member = summit.Participants[i];
                if (member == null) continue;
                string id = GetStableObjectIdentity(member);
                if (!summit.Arrived.Contains(id)) continue;
                Actor delegateActor;
                if (!summit.Delegates.TryGetValue(id, out delegateActor) ||
                    delegateActor == null || !delegateActor.isAlive())
                {
                    continue;
                }
                int score = CalculateInternationalSummitVoteScore(member, summit);
                if (score >= 50) summit.YesVotes++;
                else summit.NoVotes++;
            }

            int votes = summit.YesVotes + summit.NoVotes;
            bool passed = votes >= InternationalSummitMinimumQuorum &&
                summit.YesVotes > summit.NoVotes;
            summit.Result = passed ? "passed" : "rejected";
            if (passed) ApplyInternationalSummitDecision(summit);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get(passed
                        ? "ukiol_event_summit_vote_passed"
                        : "ukiol_event_summit_vote_rejected"),
                    summit.Bloc.Name,
                    GetInternationalSummitAgendaName(summit.Agenda),
                    summit.YesVotes,
                    summit.NoVotes
                ),
                summit.HostKingdom,
                summit.HostCity,
                GetLivingRuler(summit.HostKingdom),
                DiplomatIconPath,
                "summit_vote_" + summit.Id,
                20f
            );
        }

        private static int CalculateInternationalSummitVoteScore(
            Kingdom member,
            InternationalSummitSnapshot summit
        )
        {
            if (member == null || summit == null || summit.Bloc == null) return 0;
            InternationalBlocSnapshot bloc = summit.Bloc;
            int score = 50;
            if (bloc.Leader != null)
                score += (CalculateKingdomBlocCompatibility(member, bloc.Leader) - 50) / 3;
            score += (GetDiplomaticReputation(member) - 50) / 5;
            score += (GetNationalStability(member) - 50) / 8;
            score += (bloc.Unity - 50) / 5;
            IdeologyBehaviorProfile behavior = GetIdeologyBehaviorProfile(member);

            if (summit.Agenda == "integration")
                score += (behavior.Pluralism + behavior.Welfare - 100) / 8;
            else if (summit.Agenda == "collective_defense")
                score += (behavior.Militarism - 50) / 3;
            else if (summit.Agenda == "economic_coordination")
                score += (Math.Max(behavior.Market, behavior.Welfare) - 50) / 3;
            else if (summit.Agenda == "admission" && summit.CandidateKingdom != null)
                score += (CalculateKingdomBlocCompatibility(member, summit.CandidateKingdom) - 50) / 2;
            else if (summit.Agenda == "leadership_review" && summit.LeadershipCandidate != null)
                score += (GetBlocLeadershipScore(summit.LeadershipCandidate) -
                    GetBlocLeadershipScore(bloc.Leader)) / 8;

            if (GetKingdomCourse(member) == DiplomatTraitId) score += 5;
            if (GetKingdomCourse(member) == MilitaristTraitId &&
                summit.Agenda == "collective_defense") score += 7;
            score += UnityEngine.Random.Range(-12, 13);
            return ClampInt(score, 0, 100);
        }

        private static void ApplyInternationalSummitDecision(
            InternationalSummitSnapshot summit
        )
        {
            InternationalBlocSnapshot bloc = summit.Bloc;
            if (bloc == null) return;

            if (summit.Agenda == "integration")
            {
                bloc.Integration = ClampInt(bloc.Integration + 8, 0, 100);
                bloc.Unity = ClampInt(bloc.Unity + 3, 0, 100);
            }
            else if (summit.Agenda == "collective_defense")
            {
                bloc.Unity = ClampInt(bloc.Unity + 7, 0, 100);
                bloc.Integration = ClampInt(bloc.Integration + 3, 0, 100);
            }
            else if (summit.Agenda == "economic_coordination")
            {
                bloc.Integration = ClampInt(bloc.Integration + 6, 0, 100);
                for (int i = 0; i < bloc.Members.Count; i++)
                {
                    List<City> cities = GetCitiesSafe(bloc.Members[i]);
                    if (cities.Count > 0) TryChangeCityResource(cities[0], "gold", 2);
                }
            }
            else if (summit.Agenda == "common_declaration")
            {
                bloc.Unity = ClampInt(bloc.Unity + 4, 0, 100);
                for (int i = 0; i < bloc.Members.Count; i++)
                    ChangeDiplomaticReputation(bloc.Members[i], 1);
            }
            else if (
                summit.Agenda == "leadership_review" &&
                summit.LeadershipCandidate != null &&
                bloc.Members.Contains(summit.LeadershipCandidate)
            )
            {
                bloc.Leader = summit.LeadershipCandidate;
                bloc.LeaderIdentity = GetStableObjectIdentity(bloc.Leader);
            }
            else if (
                summit.Agenda == "admission" &&
                summit.CandidateKingdom != null &&
                string.IsNullOrEmpty(GetKingdomStringData(
                    summit.CandidateKingdom, InternationalBlocIdDataKey, ""))
            )
            {
                bloc.Members.Add(summit.CandidateKingdom);
                SetKingdomIntData(
                    summit.CandidateKingdom,
                    InternationalBlocCooldownUntilYearDataKey,
                    0
                );
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_bloc_joined"),
                        GetWorldObjectDisplayName(summit.CandidateKingdom),
                        bloc.Name
                    ),
                    summit.CandidateKingdom,
                    null,
                    GetLivingRuler(summit.CandidateKingdom),
                    DiplomatIconPath,
                    "summit_admission_" + summit.Id,
                    20f
                );
            }
            SyncInternationalBlocToMembers(bloc);
        }

        private static void CompleteInternationalSummit(
            InternationalSummitSnapshot summit
        )
        {
            if (summit == null || summit.Bloc == null) return;
            int currentYear = GetWorldYearSafe();
            SetInternationalBlocNextSummitYear(
                summit.Bloc,
                currentYear + InternationalSummitIntervalYears
            );
            for (int i = 0; i < summit.Bloc.Members.Count; i++)
            {
                Kingdom member = summit.Bloc.Members[i];
                SetKingdomIntData(member, InternationalSummitLastYearDataKey, currentYear);
                SetKingdomStringData(
                    member,
                    InternationalSummitLastResultDataKey,
                    summit.Result ?? "rejected"
                );
                ClearInternationalSummitActiveMirror(member);
            }
            ActiveInternationalSummits.Remove(summit.BlocId);
        }

        private static void CancelInternationalSummit(
            InternationalSummitSnapshot summit,
            string reason
        )
        {
            if (summit == null || summit.Bloc == null) return;
            int currentYear = GetWorldYearSafe();
            SetInternationalBlocNextSummitYear(
                summit.Bloc,
                currentYear + InternationalSummitRetryYears
            );
            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_summit_cancelled"),
                    summit.Bloc.Name,
                    GetInternationalSummitCancelReasonName(reason)
                ),
                summit.HostKingdom,
                summit.HostCity,
                GetLivingRuler(summit.HostKingdom),
                DiplomatIconPath,
                "summit_cancelled_" + summit.Id,
                20f
            );
            for (int i = 0; i < summit.Bloc.Members.Count; i++)
                ClearInternationalSummitActiveMirror(summit.Bloc.Members[i]);
            ActiveInternationalSummits.Remove(summit.BlocId);
        }

        private static void SyncInternationalSummitMirror(
            InternationalSummitSnapshot summit
        )
        {
            if (summit == null || summit.Bloc == null || summit.Bloc.Members == null)
                return;
            for (int i = 0; i < summit.Bloc.Members.Count; i++)
            {
                Kingdom member = summit.Bloc.Members[i];
                if (member == null) continue;
                SetKingdomIntData(member, InternationalSummitActiveDataKey, 1);
                SetKingdomStringData(member, InternationalSummitBlocIdDataKey, summit.BlocId);
                SetKingdomStringData(
                    member, InternationalSummitHostKingdomDataKey,
                    GetWorldObjectDisplayName(summit.HostKingdom)
                );
                SetKingdomStringData(
                    member, InternationalSummitHostCityDataKey,
                    GetWorldObjectDisplayName(summit.HostCity)
                );
                SetKingdomStringData(member, InternationalSummitAgendaDataKey, summit.Agenda ?? "common_declaration");
                SetKingdomIntData(member, InternationalSummitStageDataKey, summit.Stage);
                SetKingdomIntData(member, InternationalSummitAttendeesDataKey, summit.Arrived.Count);
                SetKingdomIntData(member, InternationalSummitYesVotesDataKey, summit.YesVotes);
                SetKingdomIntData(member, InternationalSummitNoVotesDataKey, summit.NoVotes);
            }
        }

        private static void ClearInternationalSummitActiveMirror(Kingdom kingdom)
        {
            if (kingdom == null) return;
            SetKingdomIntData(kingdom, InternationalSummitActiveDataKey, 0);
            SetKingdomStringData(kingdom, InternationalSummitBlocIdDataKey, "");
            SetKingdomStringData(kingdom, InternationalSummitHostKingdomDataKey, "");
            SetKingdomStringData(kingdom, InternationalSummitHostCityDataKey, "");
            SetKingdomStringData(kingdom, InternationalSummitAgendaDataKey, "");
            SetKingdomIntData(kingdom, InternationalSummitStageDataKey, 0);
            SetKingdomIntData(kingdom, InternationalSummitAttendeesDataKey, 0);
            SetKingdomIntData(kingdom, InternationalSummitYesVotesDataKey, 0);
            SetKingdomIntData(kingdom, InternationalSummitNoVotesDataKey, 0);
        }

        private static void CleanupStaleInternationalSummitMirrors()
        {
            if (Time.time < _nextInternationalSummitCleanupTime) return;
            _nextInternationalSummitCleanupTime = Time.time + 3f;
            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null || GetKingdomIntData(
                    kingdom, InternationalSummitActiveDataKey, 0) == 0)
                    continue;
                string blocId = GetKingdomStringData(
                    kingdom, InternationalSummitBlocIdDataKey, ""
                );
                if (string.IsNullOrEmpty(blocId) ||
                    !ActiveInternationalSummits.ContainsKey(blocId))
                {
                    ClearInternationalSummitActiveMirror(kingdom);
                }
            }
        }

        private static string GetInternationalSummitAgendaName(string agenda)
        {
            string safe = string.IsNullOrEmpty(agenda) ? "common_declaration" : agenda;
            string key = "ukiol_summit_agenda_" + safe;
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? safe : value;
        }

        private static string GetInternationalSummitStageName(int stage)
        {
            string key = "ukiol_summit_stage_" + ClampInt(stage, 1, 3).ToString();
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? stage.ToString() : value;
        }

        private static string GetInternationalSummitResultName(string result)
        {
            string safe = string.IsNullOrEmpty(result) ? "none" : result;
            string key = "ukiol_summit_result_" + safe;
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? safe : value;
        }

        private static string GetInternationalSummitCancelReasonName(string reason)
        {
            string safe = string.IsNullOrEmpty(reason) ? "no_quorum" : reason;
            string key = "ukiol_summit_cancel_" + safe;
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? safe : value;
        }
    }
}
