using System;
using System.Collections.Generic;
using NeoModLoader.General.UI.Window;
using UnityEngine;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // Runtime-only state must never leak from one WorldBox world/save into
        // another. Harmony patches and addon registries are process-wide and
        // intentionally survive; object references, cooldowns and UI targets do not.
        private object _runtimeObservedWorldReference;
        private object _runtimeObservedKingdomManagerReference;
        private object _runtimeObservedMapStatsReference;

        private void SynchronizeRuntimeWorldReference()
        {
            object currentWorld = null;
            object currentKingdomManager = null;
            object currentMapStats = null;
            try
            {
                currentWorld = World.world;
                if (World.world != null)
                {
                    currentKingdomManager = World.world.kingdoms;
                    currentMapStats = GetMemberValue(
                        World.world,
                        "map_stats",
                        "mapStats",
                        "stats"
                    );
                }
            }
            catch
            {
                currentWorld = null;
                currentKingdomManager = null;
            }

            bool worldChanged = !object.ReferenceEquals(
                currentWorld,
                _runtimeObservedWorldReference
            );
            bool managerReplaced =
                !worldChanged &&
                currentWorld != null &&
                _runtimeObservedKingdomManagerReference != null &&
                currentKingdomManager != null &&
                !object.ReferenceEquals(
                    currentKingdomManager,
                    _runtimeObservedKingdomManagerReference
                );
            bool mapStatsReplaced =
                !worldChanged &&
                currentWorld != null &&
                _runtimeObservedMapStatsReference != null &&
                !object.ReferenceEquals(
                    currentMapStats,
                    _runtimeObservedMapStatsReference
                );

            if (!worldChanged && !managerReplaced && !mapStatsReplaced)
            {
                if (
                    _runtimeObservedKingdomManagerReference == null &&
                    currentKingdomManager != null
                )
                {
                    _runtimeObservedKingdomManagerReference =
                        currentKingdomManager;
                }
                if (
                    _runtimeObservedMapStatsReference == null &&
                    currentMapStats != null
                )
                {
                    _runtimeObservedMapStatsReference = currentMapStats;
                }
                return;
            }

            _runtimeObservedWorldReference = currentWorld;
            _runtimeObservedKingdomManagerReference = currentKingdomManager;
            _runtimeObservedMapStatsReference = currentMapStats;
            ResetWorldRuntimeState();
        }

        private void ResetWorldRuntimeState()
        {
            // Staggered simulation/timers.
            _politicalPipelineStage = -1;
            _nextPoliticsCheckTime = 0f;
            _nextEconomyTickTime = 0f;
            _nextStabilityTickTime = 0f;
            _nextRuntimeDiplomacyUpdateTime = 0f;
            _nextRuntimeSummitUpdateTime = 0f;
            _nextNativeUiScanTime = 0f;
            _nextIdeologyTickTime = 0f;

            // Loaded worlds are not safe for autonomous mutation the instant a
            // kingdom manager appears. Wait for kingdom/city topology to stop
            // changing, then arm timers from a clean point instead of allowing
            // every zeroed timer to fire in one startup burst.
            _worldLoadBootstrapPending = true;
            _worldLoadSimulationArmed = false;
            _worldLoadStableSinceUnscaled = Time.unscaledTime;
            _worldLoadAutonomyAllowedAtUnscaled = float.PositiveInfinity;
            _nextWorldLoadTopologyProbeTime = 0f;
            _worldLoadLastKingdomCount = -1;
            _worldLoadLastCityCount = -1;
            _internationalBlocBootstrapPending = true;
            _nextGlobalRebellionAllowedTime = 0f;

            ResetPoliticalPartyUpdatePass();
            ResetPoliticalCrisisUpdatePass();

            WorldTopologySettleUntil = 0f;
            WorldTopologySettleReason = "";

            // World-object caches.
            LastKnownKingdomCourses.Clear();
            NextRebellionAttemptTime.Clear();
            FastKingdomCourseCache.Clear();
            CachedArmyLimitPoliticalDelta.Clear();
            CachedArmyLimitPoliticalDeltaUntil.Clear();

            // War/diplomacy runtime. Keep Harmony installation flags/methods:
            // patches belong to the type, while the manager instance belongs
            // to one concrete world and must be reacquired.
            PendingWarDeclarations.Clear();
            PendingDiplomaticCrises.Clear();
            WarPairNextRuntimeStartTime.Clear();
            WarStartEventLastRuntimeTime.Clear();
            ApiWarStartSourceContext = PoliticalWorldAPI.WarStartSource.Unknown;
            ActiveWarSimulations.Clear();
            PendingPeaceAgreements.Clear();
            WarPairNextPeaceAttemptTime.Clear();
            _patchedDiplomacyInstance = null;
            _allowPoliticalWarStart = false;
            _nextWarPatchAttemptTime = 0f;
            _nextDiplomaticCrisisCleanupTime = 0f;

            // International runtime mirrors are rebuilt from persistent
            // kingdom data in the new world.
            InternationalBlocs.Clear();
            ActiveInternationalSummits.Clear();
            _lastInternationalBlocUpdateYear = int.MinValue;
            _internationalBlocGeographyCacheValid = false;
            _internationalBlocGeographyKingdomCount = 0;
            _nextInternationalSummitCleanupTime = 0f;

            // Presentation caches that contain old City/Kingdom references or
            // ever-growing per-party keys.
            PoliticalMapCityVisualCache.Clear();
            PoliticalMapPartyCache.Clear();
            PoliticalMapMetaCache.Clear();
            PoliticalMapColorCache.Clear();
            _politicalMapCachedMode = -1;
            _politicalMapVisualCacheExpiresAt = 0f;
            PoliticalEventNextAllowedTime.Clear();
            ResetPoliticalChronicle();
            ResetPoliticalChroniclePersistenceForWorld();

            // Kingdom-window instance ids and selected targets are only valid
            // inside the world where they were captured.
            try
            {
                RestoreAllKingdomPoliticsHostScrolls();
            }
            catch
            {
                _kingdomPoliticsHostScrolls.Clear();
                _kingdomPoliticsHostScrollEnabled.Clear();
            }

            _kingdomPoliticsWindows.Clear();
            _kingdomPoliticsPages.Clear();
            _kingdomPoliticsSelectedPartyIds.Clear();
            _kingdomPoliticsSelectedPartyKingdomIds.Clear();
            _kingdomTabButtonsHooked.Clear();
            _kingdomPoliticsUiScanLogged.Clear();
            _kingdomPoliticsHostScrolls.Clear();
            _kingdomPoliticsHostScrollEnabled.Clear();
            _cachedKingdomWindows = new KingdomWindow[0];
            _nextKingdomWindowCacheRefreshTime = 0f;
            _kingdomPoliticsTrueNativeWindowId = -1;

            try
            {
                RestoreAllCityPoliticsHostScrolls();
            }
            catch
            {
                _cityPoliticsHostScrolls.Clear();
                _cityPoliticsHostScrollEnabled.Clear();
            }
            _cityPoliticsWindows.Clear();
            _cityPoliticsAddonPageIds.Clear();
            _kingdomPoliticsAddonPageIds.Clear();
            _cityPoliticsHostScrolls.Clear();
            _cityPoliticsHostScrollEnabled.Clear();
            _cachedCityWindows = new CityWindow[0];
            _nextCityWindowCacheRefreshTime = 0f;
            DestroyCityPoliticsOverlayButton();

            _politicsStableWindowId = -1;
            _politicsHasLastWindowScreenRect = false;
            _politicsWindowStableSince = -1f;

            PoliticalOverviewWindow.ResetWorldSelection();
            PartyEditorWindow.ResetWorldSelection();
            IdeologyEditorWindow.ResetWorldSelection();
            AddonInspectorWindow.ResetWorldSelection();
            PoliticalChronicleWindow.ResetWorldSelection();
        }



        private bool IsWorldLoadAutonomyBlocked()
        {
            if (World.world == null || World.world.kingdoms == null)
            {
                return true;
            }

            float nowUnscaled = Time.unscaledTime;

            if (_worldLoadBootstrapPending)
            {
                if (nowUnscaled >= _nextWorldLoadTopologyProbeTime)
                {
                    _nextWorldLoadTopologyProbeTime =
                        nowUnscaled + WorldLoadTopologyProbeInterval;

                    int kingdomCount = 0;
                    int cityCount = 0;
                    List<Kingdom> kingdoms = GetKingdomsSafe();
                    kingdomCount = kingdoms.Count;
                    for (int i = 0; i < kingdoms.Count; i++)
                    {
                        cityCount += GetCitiesSafe(kingdoms[i]).Count;
                    }

                    if (
                        kingdomCount != _worldLoadLastKingdomCount ||
                        cityCount != _worldLoadLastCityCount
                    )
                    {
                        _worldLoadLastKingdomCount = kingdomCount;
                        _worldLoadLastCityCount = cityCount;
                        _worldLoadStableSinceUnscaled = nowUnscaled;
                    }
                    else if (
                        nowUnscaled - _worldLoadStableSinceUnscaled >=
                        WorldLoadTopologyStableSeconds
                    )
                    {
                        _worldLoadBootstrapPending = false;
                        _worldLoadAutonomyAllowedAtUnscaled =
                            nowUnscaled + WorldLoadAutonomyGraceSeconds;

                        LogInfo(
                            "[PW-MAP-LOAD] topology stable kingdoms=" +
                            kingdomCount + " cities=" + cityCount +
                            "; autonomy grace=" +
                            WorldLoadAutonomyGraceSeconds + "s"
                        );
                    }
                }

                return true;
            }

            if (nowUnscaled < _worldLoadAutonomyAllowedAtUnscaled)
            {
                return true;
            }

            if (!_worldLoadSimulationArmed)
            {
                ArmWorldSimulationAfterLoad();
                _worldLoadSimulationArmed = true;
                return true;
            }

            return false;
        }

        private void ArmWorldSimulationAfterLoad()
        {
            float now = Time.time;

            // Do not replay a backlog of zeroed timers on the first active
            // frame. Each subsystem gets its normal interval from this point.
            _nextRuntimeDiplomacyUpdateTime =
                now + RuntimeDiplomacyUpdateInterval;
            _nextRuntimeSummitUpdateTime =
                now + RuntimeSummitUpdateInterval;
            _nextPoliticsCheckTime = now + PoliticsCheckInterval;
            _nextEconomyTickTime = now + EconomyTickInterval;
            _nextStabilityTickTime = now + StabilityTickInterval;
            _nextIdeologyTickTime = now + 2f;
            _politicalPipelineStage = -1;

            ResetPoliticalPartyUpdatePass();
            ResetPoliticalCrisisUpdatePass();

            LogInfo(
                "[PW-MAP-LOAD] autonomous simulation armed after stable load"
            );
        }

        private static bool IsPoliticalSimulationPaused()
        {
            // WorldBox owns its pause state. Do not rely on Unity timeScale:
            // the game can be paused while the Unity scaled clock remains live.
            try
            {
                if (Config.paused)
                {
                    return true;
                }
            }
            catch
            {
            }

            // Compatibility fallback for other builds/modded pause systems.
            return Time.timeScale <= 0.0001f;
        }
    }
}
