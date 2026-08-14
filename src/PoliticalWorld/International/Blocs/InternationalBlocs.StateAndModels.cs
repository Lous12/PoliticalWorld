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
        // Step 9E FAST: international bloc/summit persistent state and runtime models, moved unchanged from Main.cs.

        // v1.5.0-dev14: international blocs add political/economic depth.
        // v1.5.0-dev16: every Political World bloc is now mirrored by a real
        // vanilla Alliance so WorldBox itself can render it in Alliance Zones,
        // use its native alliance membership and handle ordinary allied wars.
        // Political World remains authoritative for bloc type/unity/integration.
        private const string InternationalBlocIdDataKey = "ukiol_bloc_id";
        private const string InternationalBlocTypeDataKey = "ukiol_bloc_type";
        private const string InternationalBlocNameDataKey = "ukiol_bloc_name";
        private const string InternationalBlocLeaderIdentityDataKey = "ukiol_bloc_leader_identity";
        private const string InternationalBlocLeaderNameDataKey = "ukiol_bloc_leader_name";
        private const string InternationalBlocFoundingYearDataKey = "ukiol_bloc_founding_year";
        private const string InternationalBlocUnityDataKey = "ukiol_bloc_unity";
        private const string InternationalBlocIntegrationDataKey = "ukiol_bloc_integration";
        private const string InternationalBlocMemberCountDataKey = "ukiol_bloc_member_count";
        private const string InternationalBlocCooldownUntilYearDataKey = "ukiol_bloc_cooldown_until_year";
        private const string InternationalBlocLastTypeChangeYearDataKey = "ukiol_bloc_last_type_change_year";
        private const string InternationalBlocNativeAllianceIdDataKey = "ukiol_bloc_native_alliance_id";
        private const int InternationalBlocMinimumMembers = 2;
        private const int InternationalBlocRejoinCooldownYears = 3;
        private const int InternationalBlocDissolveUnity = 14;
        private const int InternationalBlocDefenceUnity = 35;
        private const int InternationalBlocMaxCollectiveDefenders = 4;

        // v1.5.0-dev15: international summits are real gatherings of the
        // rulers of a bloc. The persistent mirror lives on member kingdoms;
        // runtime state only coordinates travel, attendance and voting.
        private const string InternationalSummitActiveDataKey = "ukiol_summit_active";
        private const string InternationalSummitBlocIdDataKey = "ukiol_summit_bloc_id";
        private const string InternationalSummitHostKingdomDataKey = "ukiol_summit_host_kingdom";
        private const string InternationalSummitHostCityDataKey = "ukiol_summit_host_city";
        private const string InternationalSummitAgendaDataKey = "ukiol_summit_agenda";
        private const string InternationalSummitStageDataKey = "ukiol_summit_stage";
        private const string InternationalSummitAttendeesDataKey = "ukiol_summit_attendees";
        private const string InternationalSummitYesVotesDataKey = "ukiol_summit_yes_votes";
        private const string InternationalSummitNoVotesDataKey = "ukiol_summit_no_votes";
        private const string InternationalSummitLastResultDataKey = "ukiol_summit_last_result";
        private const string InternationalSummitNextYearDataKey = "ukiol_summit_next_year";
        private const string InternationalSummitLastYearDataKey = "ukiol_summit_last_year";
        private const int InternationalSummitIntervalYears = 5;
        private const int InternationalSummitInitialDelayYears = 2;
        private const int InternationalSummitRetryYears = 2;
        private const int InternationalSummitMinimumQuorum = 2;
        private const float InternationalSummitTravelSeconds = 18f;
        private const float InternationalSummitSessionSeconds = 5f;
        private const float InternationalSummitClosingSeconds = 3f;

        private static readonly Dictionary<string, InternationalBlocSnapshot>
            InternationalBlocs = new Dictionary<string, InternationalBlocSnapshot>();
        private static int _lastInternationalBlocUpdateYear = int.MinValue;
        private static readonly Dictionary<string, InternationalSummitSnapshot>
            ActiveInternationalSummits = new Dictionary<string, InternationalSummitSnapshot>();
        private static float _nextInternationalSummitCleanupTime;

        private sealed class InternationalBlocSnapshot
        {
            public string Id;
            public string Type;
            public string Name;
            public string LeaderIdentity;
            public Kingdom Leader;
            public List<Kingdom> Members = new List<Kingdom>();
            public int FoundingYear;
            public int Unity;
            public int Integration;
            public string NativeAllianceId;
            public Alliance NativeAlliance;
        }

        private sealed class InternationalSummitSnapshot
        {
            public string Id;
            public string BlocId;
            public InternationalBlocSnapshot Bloc;
            public Kingdom HostKingdom;
            public City HostCity;
            public string Agenda;
            public Kingdom CandidateKingdom;
            public Kingdom LeadershipCandidate;
            public int StartedYear;
            public int Stage;
            public float StageEndsAt;
            public float NextTravelOrderAt;
            public List<Kingdom> Participants = new List<Kingdom>();
            public Dictionary<string, Actor> Delegates = new Dictionary<string, Actor>();
            public HashSet<string> Arrived = new HashSet<string>();
            public int YesVotes;
            public int NoVotes;
            public string Result;
        }
    }
}
