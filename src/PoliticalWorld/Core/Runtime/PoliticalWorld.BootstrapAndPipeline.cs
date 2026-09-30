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
    public partial class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            LogRuntimeCompatibility();
            EnsureIdeologyRegistry();
            CreatePoliticalWorldLaws();
            CreatePoliticsGroup();

            CreateReformerTrait();
            CreateMilitaristTrait();
            CreateDiplomatTrait();

            CreateReformerPower();
            CreateMilitaristPower();
            CreateDiplomatPower();

            CreateReformerCoursePower();
            CreateMilitaristCoursePower();
            CreateDiplomatCoursePower();

            CreateStabilizeCityPower();
            CreateDestabilizeCityPower();

            CreateEncourageReformPower();
            CreateIncreaseRadicalizationPower();
            CreateReduceRadicalizationPower();

            CreateMonarchismPower();
            CreateConservatismPower();
            CreateLiberalismPower();
            CreateDemocracyPower();
            CreateSocialismPower();
            CreateCommunismPower();
            CreateFascismPower();
            CreateAnarchismPower();
            CreateSyndicalismPower();

            CreateCityMonarchismPower();
            CreateCityConservatismPower();
            CreateCityLiberalismPower();
            CreateCityDemocracyPower();
            CreateCitySocialismPower();
            CreateCityCommunismPower();
            CreateCityFascismPower();
            CreateCityAnarchismPower();
            CreateCitySyndicalismPower();

            CreateStateIdeologyEditorPower();
            CreateCityIdeologyEditorPower();
            CreateIdeologyEditorWindow();
            CreatePartyEditorWindow();
            CreateAddonInspectorPower();
            CreateAddonInspectorWindow();
            CreatePoliticalChronicleWindow();

            // Custom MetaTypes must be known by MetaTypeExtensions/Zones before
            // the meta library links the Political Layer asset. Install the
            // patches first, then register the actual layer and its toolbar.
            InstallSafePatches();
            CreatePoliticalMapLayer();
            CreatePoliticsTab();
            // Native WorldLog assets are registered lazily by
            // GetOrCreatePoliticalWorldLogAsset(). No custom handler is
            // required on WorldBox 0.51.2.

            LogInfo(
                "Political World 1.11.0 loaded, API 1.19 (" +
                IdeologyNodeRegistry.Count + " registered ideology nodes)."
            );
        }

        private static void LogRuntimeCompatibility()
        {
            try
            {
                string version = Application.version ?? string.Empty;
                string buildCode = Config.versionCodeText ?? string.Empty;
                string gitCode = Config.gitCodeText ?? string.Empty;

                string detected = version + " (" + buildCode;
                if (!string.IsNullOrEmpty(gitCode))
                {
                    detected += "@" + gitCode;
                }
                detected += ")";

                bool versionMatch = string.Equals(
                    version,
                    TargetWorldBoxVersion,
                    StringComparison.Ordinal
                );
                bool buildMatch = string.IsNullOrEmpty(buildCode) ||
                    string.Equals(
                        buildCode,
                        TargetWorldBoxBuildCode,
                        StringComparison.Ordinal
                    );
                bool gitMatch = string.IsNullOrEmpty(gitCode) ||
                    string.Equals(
                        gitCode,
                        TargetWorldBoxGitCode,
                        StringComparison.Ordinal
                    );

                if (versionMatch && buildMatch && gitMatch)
                {
                    LogInfo(
                        "Compatibility check OK: WorldBox " + detected +
                        "; target 0.51.2 (719@build-719@5dec)."
                    );
                }
                else
                {
                    LogWarning(
                        "Compatibility warning: detected WorldBox " +
                        detected +
                        ", while Political World 1.7.2-dev3-test2 targets " +
                        "0.51.2 (719@build-719@5dec). The mod will still " +
                        "load, but version-sensitive UI/Harmony hooks may " +
                        "need revalidation."
                    );
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not read WorldBox build metadata: " +
                    exception.Message
                );
            }
        }

        private void Update()
        {
            // API 1.11: constant-time world reference/readiness tracking.
            // This reuses Political World's existing Update loop and never
            // performs an automatic world scan.
            PoliticalWorldAPI.InternalTickWorldLifecycle();
            SynchronizeRuntimeWorldReference();

            // v1.4.0-dev3.1: native kingdom side-rail tabs can be clicked
            // while our embedded Politics panel is open. Those controls are
            // not ordinary UnityEngine.UI.Buttons in WorldBox 0.51.x, so the
            // older onClick exit hooks do not see every click. Detect a click
            // in the native left rail and retire our overlay before WorldBox
            // switches to its own page.
            // A true WindowMetaTab is switched by WorldBox's own
            // WindowMetaTabButtonsContainer. The old rail-click detector is
            // only needed by the overlay fallback.
            if (_kingdomPoliticsTrueNativeTab == null)
            {
                HandleKingdomPoliticsRailExitClick();
            }

            // KingdomWindow side tabs are not always children of the
            // KingdomWindow component itself on WorldBox 0.51.x. Poll the
            // active windows independently from the simulation ticks so the
            // Politics button is injected as soon as a kingdom window opens.
            if (Time.unscaledTime >= _nextNativeUiScanTime)
            {
                float scanInterval =
                    _kingdomPoliticsTrueNativeTab != null &&
                    _kingdomPoliticsTrueNativeTab.gameObject != null
                        ? NativeUiScanIntervalInstalled
                        : NativeUiScanIntervalSearching;
                _nextNativeUiScanTime = Time.unscaledTime + scanInterval;
                RefreshActiveKingdomPoliticsTabs();
                RefreshActiveCityPoliticsTabs();
            }

            // X/Z changes the int state of a multi-toggle map option without
            // necessarily invoking the GodPower's click delegate. Poll only
            // the tiny toolbar visual so the three vanilla mode dots always
            // follow the actual Political Layer mode.
            SyncPoliticalMapToolbarModeIndicators();

            if (
                World.world == null ||
                World.world.kingdoms == null
            )
            {
                return;
            }

            // Diplomacy is created with the world, not necessarily when the
            // mod itself loads. Install the runtime Harmony hooks lazily and
            // advance declarations every frame so the delay is visible even
            // when the slower political simulation tick has not fired yet.
            EnsureWarDiplomacyPatches();

            // INTERACTION FIX1: a vanilla kingdom split rewires city/kingdom,
            // diplomacy and army references over several operations. During the
            // short settle window keep lifecycle/UI alive but do not run any
            // simulation system or addon-facing structural scan.
            if (IsWorldTopologySettling())
            {
                ResetPoliticalPartyUpdatePass();
                ResetPoliticalCrisisUpdatePass();
                _politicalPipelineStage = -1;
                return;
            }

            // v1.7.2-dev1 MAP LOAD FIX: loading a populated Workshop/save map
            // can expose a usable kingdom manager while ownership/topology is
            // still settling. Keep UI/lifecycle/Harmony alive, but do not allow
            // autonomous politics to mutate that half-loaded world.
            bool worldLoadBlocked = IsWorldLoadAutonomyBlocked();
            UpdatePoliticalChroniclePersistenceAfterLoad();
            if (worldLoadBlocked)
            {
                return;
            }

            // Keep automatic politics frozen together with WorldBox. UI,
            // lifecycle tracking, sandbox clicks and Harmony availability stay
            // responsive, but no pending crisis/war/summit or staged political
            // pass is allowed to advance while the game is paused.
            if (IsPoliticalSimulationPaused())
            {
                return;
            }

            // These sequences are measured in seconds, not frames. 10 Hz is
            // visually indistinguishable from per-frame polling and removes
            // a large amount of idle work on high-refresh-rate systems.
            if (Time.time >= _nextRuntimeDiplomacyUpdateTime)
            {
                _nextRuntimeDiplomacyUpdateTime =
                    Time.time + RuntimeDiplomacyUpdateInterval;
                UpdatePendingDiplomaticCrises();
                UpdatePendingWarDeclarations();
                UpdatePendingPeaceAgreements();
            }

            if (Time.time >= _nextRuntimeSummitUpdateTime)
            {
                _nextRuntimeSummitUpdateTime =
                    Time.time + RuntimeSummitUpdateInterval;
                UpdateInternationalSummits();
            }

            if (Time.time >= _nextPoliticsCheckTime)
            {
                _nextPoliticsCheckTime =
                    Time.time + PoliticsCheckInterval;

                ProcessKingdomRulers();
            }

            if (Time.time >= _nextEconomyTickTime)
            {
                _nextEconomyTickTime =
                    Time.time + EconomyTickInterval;

                ApplyKingdomEconomyEffects();
            }

            if (Time.time >= _nextStabilityTickTime)
            {
                _nextStabilityTickTime =
                    Time.time + StabilityTickInterval;

                UpdateStabilitySystem();
            }

            // A rebellion may have been created inside UpdateStabilitySystem()
            // in this same rendered frame. Re-check the guard before advancing
            // the staged political pipeline; otherwise the next stage can scan
            // the freshly-mutated world immediately.
            if (IsWorldTopologySettling())
            {
                ResetPoliticalPartyUpdatePass();
                ResetPoliticalCrisisUpdatePass();
                _politicalPipelineStage = -1;
                return;
            }

            if (
                Time.time >= _nextIdeologyTickTime &&
                _politicalPipelineStage < 0
            )
            {
                _nextIdeologyTickTime =
                    Time.time + IdeologyTickInterval;
                _politicalPipelineStage = 0;
            }

            // dev17: the old 12-second tick executed eleven heavyweight
            // systems in a single rendered frame. Preserve their order but
            // spread them over successive frames to remove periodic hitches.
            RunPoliticalSimulationPipelineStep();
        }

        private void RunPoliticalSimulationPipelineStep()
        {
            if (IsPoliticalSimulationPaused())
            {
                return;
            }

            if (_politicalPipelineStage < 0)
            {
                return;
            }

            int stage = _politicalPipelineStage;
            string stageName = GetPoliticalPipelineStageName(stage);
            if (VerbosePoliticalDiagnostics)
            {
                LogInfo(
                    "[PW-PIPELINE-GUARD] stage begin stage=" +
                    stage + " name=" + stageName
                );
            }

            bool completed = true;
            try
            {
                switch (stage)
                {
                    case 0:
                        UpdateIdeologySystem();
                        break;
                    case 1:
                        UpdateIdeologyFrameworkMigrations();
                        break;
                    case 2:
                        UpdatePoliticalMovements();
                        break;
                    case 3:
                        completed = UpdatePoliticalPartiesIncremental();
                        break;
                    case 4:
                        // PARENT CORE FIX3: Player(10).log ends immediately
                        // after stage 3 completes with a freshly-created Kysu.
                        // Process crises incrementally and keep stage 4 active
                        // until its stable kingdom snapshot has finished.
                        completed = UpdatePoliticalCrisesIncremental();
                        break;
                    case 5:
                        UpdateGovernmentForms();
                        UpdatePoliticalCountryNames();
                        break;
                    case 6:
                        UpdatePoliticalSystems();
                        break;
                    case 7:
                        UpdateElections();
                        break;
                    case 8:
                        UpdateGovernmentLeadership();
                        break;
                    case 9:
                        UpdateWarDiplomacyFoundation();
                        break;
                    case 10:
                        UpdateInternationalBlocs();
                        break;
                    case 11:
                        PoliticalWorldAPI.InternalEvaluateRarePoliticalEvents(
                            GetKingdomsSafe(),
                            GetWorldYearSafe()
                        );
                        break;
                }
            }
            catch (Exception exception)
            {
                // Managed exceptions must never silently kill the political
                // scheduler. Preserve the full exception and advance past the
                // failed stage so a bad subsystem cannot deadlock the mod.
                LogWarning(
                    "[PW-PIPELINE-GUARD] stage failed stage=" +
                    stage + " name=" + stageName +
                    " exception=" + exception
                );

                // Incremental stages keep their own stable snapshots/indexes.
                // A failed pass must not be resumed during the next political
                // cycle with stale world references or a half-advanced index.
                if (stage == 3)
                {
                    ResetPoliticalPartyUpdatePass();
                }
                else if (stage == 4)
                {
                    ResetPoliticalCrisisUpdatePass();
                }

                completed = true;
            }

            if (!completed)
            {
                return;
            }

            if (VerbosePoliticalDiagnostics)
            {
                LogInfo(
                    "[PW-PIPELINE-GUARD] stage end stage=" +
                    stage + " name=" + stageName
                );
            }

            _politicalPipelineStage++;
            if (_politicalPipelineStage > 11)
            {
                _politicalPipelineStage = -1;
            }
        }

        private static string GetPoliticalPipelineStageName(int stage)
        {
            switch (stage)
            {
                case 0: return "ideologies";
                case 1: return "ideology-migrations";
                case 2: return "movements";
                case 3: return "parties";
                case 4: return "crises";
                case 5: return "governments";
                case 6: return "political-systems";
                case 7: return "elections";
                case 8: return "leadership";
                case 9: return "war-diplomacy";
                case 10: return "international-blocs";
                case 11: return "rare-events";
                default: return "unknown";
            }
        }
    }
}
