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
        // Step 9E FAST: warfare/diplomacy state and runtime models, moved unchanged from Main.cs.

        // v1.5.0-dev7: war & diplomacy foundation. Vanilla WorldBox still
        // owns armies, battles and territorial occupation. Political World
        // intercepts the diplomatic startWar request and turns it into a
        // short declaration/preparation phase before handing the exact call
        // back to vanilla. All access to diplomacy is reflective so 0.51.x
        // internal type-name changes do not become compile-time dependencies.
        private const string WarExhaustionDataKey = "ukiol_war_exhaustion";
        private const string WarExhaustionLastYearDataKey = "ukiol_war_exhaustion_last_year";
        private const string WarLastDeclarationYearDataKey = "ukiol_war_last_declaration_year";
        private const string WarPreparationStateDataKey = "ukiol_war_preparation_state";
        private const string WarPreparationTargetNameDataKey = "ukiol_war_preparation_target_name";
        private const string WarPreparationCasusBelliDataKey = "ukiol_war_preparation_casus_belli";
        private const string WarLatestTruceUntilYearDataKey = "ukiol_war_latest_truce_until_year";
        private const string WarPairTruceDataPrefix = "ukiol_war_truce_";
        private const int WarTruceYears = 3;
        private const int WarExhaustionAtWarPerYear = 5;
        private const int WarExhaustionPeaceRecoveryPerYear = 8;
        private const int WarExhaustionDeclarationShock = 10;
        private const float WarPreparationSeconds = 4.0f;

        // v1.5.0-dev8: war goals, war score and negotiated peace. The game
        // still executes the actual vanilla peace operation; Political World
        // decides when both sides have enough reason to negotiate. Pair data
        // is persisted on kingdoms so a save/reload does not erase the goal.
        private const string WarPairActiveDataPrefix = "ukiol_war_active_";
        private const string WarPairRoleDataPrefix = "ukiol_war_role_";
        private const string WarPairStartYearDataPrefix = "ukiol_war_start_year_";
        private const string WarPairStartCitiesDataPrefix = "ukiol_war_start_cities_";
        private const string WarPairStartPopulationDataPrefix = "ukiol_war_start_population_";
        private const string WarPairCasusBelliDataPrefix = "ukiol_war_casus_";
        private const string WarPairNegotiationDataPrefix = "ukiol_war_negotiation_";
        private const int WarMinimumPeaceYears = 1;
        private const int WarGoalPeaceYears = 2;
        private const int WarLongWarYears = 10;
        private const int WarMajorVictoryScore = 70;
        private const int WarGoalSatisfiedScore = 25;
        private const int WarCriticalExhaustion = 90;
        private const int WarMutualExhaustion = 68;
        private const float WarPeaceNegotiationSeconds = 3.0f;

        // v1.5.0-dev13: diplomatic crises now sit between an AI request to
        // start a normal international war and the existing declaration /
        // mobilization pipeline. Runtime state keeps the original vanilla
        // startWar call intact, while a small mirrored state on both kingdoms
        // makes the active crisis visible in Politics.
        private const string DiplomaticCrisisActiveDataKey = "ukiol_diplomatic_crisis_active";
        private const string DiplomaticCrisisCounterpartDataKey = "ukiol_diplomatic_crisis_counterpart";
        private const string DiplomaticCrisisDemandDataKey = "ukiol_diplomatic_crisis_demand";
        private const string DiplomaticCrisisStageDataKey = "ukiol_diplomatic_crisis_stage";
        private const string DiplomaticCrisisTensionDataKey = "ukiol_diplomatic_crisis_tension";
        private const string DiplomaticCrisisRoleDataKey = "ukiol_diplomatic_crisis_role";
        private const string DiplomaticReputationDataKey = "ukiol_diplomatic_reputation";
        private const float DiplomaticCrisisOpeningSeconds = 3.5f;
        private const float DiplomaticUltimatumSeconds = 3.5f;
        private const float DiplomaticCrisisRuntimeCooldownSeconds = 20f;
        private const int DiplomaticSettlementTruceYears = 2;
        private const int SurpriseAttackAttackerExhaustion = 4;
        private const int SurpriseAttackDefenderExhaustion = 8;

        private static bool _warStartPatchInstalled;
        private static bool _warPeacePatchInstalled;
        private static float _nextWarPatchAttemptTime;
        private static bool _allowPoliticalWarStart;
        private static MethodInfo _patchedWarStartMethod;
        private static object _patchedDiplomacyInstance;
        private static readonly Dictionary<string, PendingWarDeclaration>
            PendingWarDeclarations = new Dictionary<string, PendingWarDeclaration>();
        private static readonly Dictionary<string, PendingDiplomaticCrisis>
            PendingDiplomaticCrises = new Dictionary<string, PendingDiplomaticCrisis>();
        private static readonly Dictionary<string, float>
            WarPairNextRuntimeStartTime = new Dictionary<string, float>();
        private static float _nextDiplomaticCrisisCleanupTime;
        private static readonly HashSet<string>
            PatchedWarPeaceMethods = new HashSet<string>();
        private static readonly Dictionary<string, ActiveWarSimulation>
            ActiveWarSimulations = new Dictionary<string, ActiveWarSimulation>();
        private static readonly Dictionary<string, PendingPeaceAgreement>
            PendingPeaceAgreements = new Dictionary<string, PendingPeaceAgreement>();
        private static readonly Dictionary<string, float>
            WarPairNextPeaceAttemptTime = new Dictionary<string, float>();

        private sealed class PendingWarDeclaration
        {
            public Kingdom Attacker;
            public Kingdom Defender;
            public object Diplomacy;
            public MethodInfo StartMethod;
            public object[] Args;
            public float ExecuteAt;
            public string CasusBelli;
            public string PairKey;
            public int DeclaredYear;
            public bool FromDiplomaticCrisis;
            public bool SurpriseAttack;
            public bool FromBlocCollectiveDefense;
        }

        private sealed class PendingDiplomaticCrisis
        {
            public Kingdom Attacker;
            public Kingdom Defender;
            public object Diplomacy;
            public MethodInfo StartMethod;
            public object[] Args;
            public string PairKey;
            public string CasusBelli;
            public string Demand;
            public int Stage;
            public int Tension;
            public int StartedYear;
            public float StageEndsAt;
        }

        private sealed class ActiveWarSimulation
        {
            public Kingdom Attacker;
            public Kingdom Defender;
            public string PairKey;
            public string CasusBelli;
            public int StartYear;
            public int AttackerStartCities;
            public int DefenderStartCities;
            public int AttackerStartPopulation;
            public int DefenderStartPopulation;
            public int LastCalculatedYear;
            public int LastWarScore;
            public object VanillaWarObject;
        }

        private sealed class PendingPeaceAgreement
        {
            public ActiveWarSimulation War;
            public string PairKey;
            public string Reason;
            public float ExecuteAt;
        }
    }
}
