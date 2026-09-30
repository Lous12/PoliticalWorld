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
        // Step 9E FAST: international blocs and native Alliance synchronization, moved unchanged from Main.cs.

        // -----------------------------------------------------------------
        // v1.5.0-dev14 - International Blocs
        // -----------------------------------------------------------------

        private static void UpdateInternationalBlocs()
        {
            int currentYear = GetWorldYearSafe();
            if (_lastInternationalBlocUpdateYear == currentYear)
            {
                return;
            }
            _lastInternationalBlocUpdateYear = currentYear;
            RefreshInternationalBlocGeographyCache();

            RebuildInternationalBlocRuntimeIndex();

            // Vanilla alliances that already exist in the world should also
            // participate in Political World instead of competing with it.
            ImportUnmanagedNativeAlliancesAsInternationalBlocs(currentYear);
            RebuildInternationalBlocRuntimeIndex();

            // v1.7.2-dev1 MAP LOAD FIX: the first bloc pass for a loaded world
            // is import-only. Preserve the map author's existing vanilla
            // alliances instead of immediately dissolving/joining/founding
            // several new ones in the same startup year. Normal autonomous
            // bloc politics begins on the next world-year update.
            if (_internationalBlocBootstrapPending)
            {
                _internationalBlocBootstrapPending = false;
                ApplyInternationalBlocBenefits();
                LogInfo(
                    "[PW-MAP-LOAD] international blocs imported without " +
                    "startup alliance mutations; autonomous bloc changes " +
                    "resume next world year"
                );
                return;
            }

            MaintainInternationalBlocs(currentYear);
            RebuildInternationalBlocRuntimeIndex();
            AttemptInternationalBlocJoins(currentYear);
            RebuildInternationalBlocRuntimeIndex();
            AttemptInternationalBlocFoundations(currentYear);
            RebuildInternationalBlocRuntimeIndex();

            // dev16 bridge: by this point the Political World membership for
            // the current year is stable. Mirror it into real vanilla
            // alliances before benefits, wars and the Alliance Zones layer.
            SynchronizeInternationalBlocsWithNativeAlliances(currentYear);

            ApplyInternationalBlocBenefits();
            ScheduleInternationalBlocSummits(currentYear);
        }

        private static void RebuildInternationalBlocRuntimeIndex()
        {
            InternationalBlocs.Clear();
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null)
                {
                    continue;
                }

                string blocId = GetKingdomStringData(
                    kingdom,
                    InternationalBlocIdDataKey,
                    ""
                );
                if (string.IsNullOrEmpty(blocId))
                {
                    continue;
                }

                InternationalBlocSnapshot bloc;
                if (!InternationalBlocs.TryGetValue(blocId, out bloc))
                {
                    bloc = new InternationalBlocSnapshot();
                    bloc.Id = blocId;
                    bloc.Type = GetKingdomStringData(
                        kingdom,
                        InternationalBlocTypeDataKey,
                        "commonwealth"
                    );
                    bloc.Name = GetKingdomStringData(
                        kingdom,
                        InternationalBlocNameDataKey,
                        ""
                    );
                    bloc.LeaderIdentity = GetKingdomStringData(
                        kingdom,
                        InternationalBlocLeaderIdentityDataKey,
                        ""
                    );
                    bloc.FoundingYear = GetKingdomIntData(
                        kingdom,
                        InternationalBlocFoundingYearDataKey,
                        GetWorldYearSafe()
                    );
                    bloc.Unity = ClampInt(
                        GetKingdomIntData(
                            kingdom,
                            InternationalBlocUnityDataKey,
                            50
                        ),
                        0,
                        100
                    );
                    bloc.Integration = ClampInt(
                        GetKingdomIntData(
                            kingdom,
                            InternationalBlocIntegrationDataKey,
                            20
                        ),
                        0,
                        100
                    );
                    bloc.NativeAllianceId = GetKingdomStringData(
                        kingdom,
                        InternationalBlocNativeAllianceIdDataKey,
                        ""
                    );
                    InternationalBlocs[blocId] = bloc;
                }

                if (!bloc.Members.Contains(kingdom))
                {
                    bloc.Members.Add(kingdom);
                }
            }

            List<string> invalidIds = new List<string>();
            foreach (
                KeyValuePair<string, InternationalBlocSnapshot> pair
                in InternationalBlocs
            )
            {
                InternationalBlocSnapshot bloc = pair.Value;
                if (
                    bloc == null ||
                    bloc.Members == null ||
                    bloc.Members.Count < InternationalBlocMinimumMembers
                )
                {
                    invalidIds.Add(pair.Key);
                    if (bloc != null && bloc.Members != null)
                    {
                        for (int m = 0; m < bloc.Members.Count; m++)
                        {
                            ClearInternationalBlocMembership(
                                bloc.Members[m],
                                GetWorldYearSafe() +
                                    InternationalBlocRejoinCooldownYears
                            );
                        }
                    }
                    continue;
                }

                ResolveInternationalBlocLeader(bloc, false);
                SyncInternationalBlocToMembers(bloc);
            }

            for (int i = 0; i < invalidIds.Count; i++)
            {
                InternationalBlocs.Remove(invalidIds[i]);
            }
        }

        private static void MaintainInternationalBlocs(int currentYear)
        {
            List<InternationalBlocSnapshot> blocs =
                new List<InternationalBlocSnapshot>(InternationalBlocs.Values);

            for (int i = 0; i < blocs.Count; i++)
            {
                InternationalBlocSnapshot bloc = blocs[i];
                if (
                    bloc == null ||
                    bloc.Members == null ||
                    bloc.Members.Count < InternationalBlocMinimumMembers
                )
                {
                    continue;
                }

                ResolveInternationalBlocLeader(bloc, true);
                bloc.Unity = CalculateInternationalBlocUnity(bloc);

                int integrationDelta = 0;
                if (bloc.Unity >= 75) integrationDelta = 3;
                else if (bloc.Unity >= 60) integrationDelta = 2;
                else if (bloc.Unity >= 48) integrationDelta = 1;
                else if (bloc.Unity < 22) integrationDelta = -4;
                else if (bloc.Unity < 35) integrationDelta = -2;

                if (
                    IsEconomicInternationalBlocType(bloc.Type) &&
                    bloc.Unity >= 55
                )
                {
                    integrationDelta += 1;
                }

                bloc.Integration = ClampInt(
                    bloc.Integration + integrationDelta,
                    0,
                    100
                );

                if (
                    bloc.Unity <= InternationalBlocDissolveUnity ||
                    (bloc.Integration <= 4 && bloc.Unity < 28)
                )
                {
                    DissolveInternationalBloc(bloc, currentYear);
                    continue;
                }

                TryEvolveInternationalBlocType(bloc, currentYear);

                List<Kingdom> leavers = new List<Kingdom>();
                if (bloc.Members.Count > 2 && bloc.Leader != null)
                {
                    for (int m = 0; m < bloc.Members.Count; m++)
                    {
                        Kingdom member = bloc.Members[m];
                        if (member == null || member == bloc.Leader)
                        {
                            continue;
                        }

                        int compatibility = CalculateKingdomBlocCompatibility(
                            member,
                            bloc.Leader
                        );
                        int reputation = GetDiplomaticReputation(member);
                        int leaveChance = 0;
                        if (compatibility < 30) leaveChance += 24;
                        if (reputation < 25) leaveChance += 18;
                        if (bloc.Unity < 30) leaveChance += 18;
                        if (GetNationalStability(member) < 25) leaveChance += 8;

                        if (
                            leaveChance > 0 &&
                            UnityEngine.Random.Range(0, 100) <
                                ClampInt(leaveChance, 0, 65)
                        )
                        {
                            leavers.Add(member);
                        }
                    }
                }

                for (int l = 0; l < leavers.Count; l++)
                {
                    RemoveKingdomFromInternationalBloc(
                        leavers[l],
                        bloc,
                        currentYear,
                        true,
                        "autonomous"
                    );
                }

                if (bloc.Members.Count < InternationalBlocMinimumMembers)
                {
                    DissolveInternationalBloc(bloc, currentYear);
                }
                else
                {
                    SyncInternationalBlocToMembers(bloc);
                }
            }
        }

        private static void AttemptInternationalBlocJoins(int currentYear)
        {
            if (InternationalBlocs.Count == 0)
            {
                return;
            }

            List<Kingdom> kingdoms = GetKingdomsSafe();
            List<InternationalBlocSnapshot> blocs =
                new List<InternationalBlocSnapshot>(InternationalBlocs.Values);

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom candidate = kingdoms[i];
                if (
                    candidate == null ||
                    !string.IsNullOrEmpty(
                        GetKingdomStringData(
                            candidate,
                            InternationalBlocIdDataKey,
                            ""
                        )
                    ) ||
                    GetKingdomIntData(
                        candidate,
                        InternationalBlocCooldownUntilYearDataKey,
                        0
                    ) > currentYear ||
                    GetDiplomaticReputation(candidate) < 38 ||
                    GetNationalStability(candidate) < 28 ||
                    IsKingdomAtWarSafe(candidate)
                )
                {
                    continue;
                }

                InternationalBlocSnapshot bestBloc = null;
                int bestScore = 0;
                for (int b = 0; b < blocs.Count; b++)
                {
                    InternationalBlocSnapshot bloc = blocs[b];
                    if (!CanKingdomJoinInternationalBloc(candidate, bloc))
                    {
                        continue;
                    }

                    int score = CalculateCandidateBlocCompatibility(
                        candidate,
                        bloc
                    );
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestBloc = bloc;
                    }
                }

                if (bestBloc == null || bestScore < 60)
                {
                    continue;
                }

                int chance = ClampInt(35 + (bestScore - 60), 25, 82);
                if (GetKingdomCourse(candidate) == DiplomatTraitId)
                {
                    chance += 10;
                }
                if (UnityEngine.Random.Range(0, 100) >= chance)
                {
                    continue;
                }

                bestBloc.Members.Add(candidate);
                bestBloc.Unity = ClampInt(
                    (bestBloc.Unity * (bestBloc.Members.Count - 1) + bestScore) /
                        Math.Max(1, bestBloc.Members.Count),
                    0,
                    100
                );
                SyncInternationalBlocToMembers(bestBloc);
                EnsureInternationalBlocNativeAlliance(bestBloc);

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_bloc_joined"),
                        GetWorldObjectDisplayName(candidate),
                        bestBloc.Name
                    ),
                    candidate,
                    null,
                    GetLivingRuler(candidate),
                    DiplomatIconPath,
                    "bloc_joined_" + bestBloc.Id + "_" +
                        GetStableObjectIdentity(candidate),
                    20f
                );

                // Avoid map-wide alliance reshuffles in one annual simulation
                // pass. Additional eligible kingdoms can join in later years.
                return;
            }
        }

        private static void AttemptInternationalBlocFoundations(int currentYear)
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom first = kingdoms[i];
                if (!CanFoundInternationalBloc(first, currentYear))
                {
                    continue;
                }

                Kingdom bestPartner = null;
                int bestScore = 0;
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    Kingdom second = kingdoms[j];
                    if (!CanFoundInternationalBloc(second, currentYear))
                    {
                        continue;
                    }

                    int score = CalculateKingdomBlocCompatibility(first, second);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPartner = second;
                    }
                }

                if (bestPartner == null || bestScore < 58)
                {
                    continue;
                }

                int chance = ClampInt(45 + (bestScore - 58), 35, 88);
                if (
                    GetKingdomCourse(first) == DiplomatTraitId ||
                    GetKingdomCourse(bestPartner) == DiplomatTraitId
                )
                {
                    chance += 8;
                }
                if (UnityEngine.Random.Range(0, 100) >= chance)
                {
                    continue;
                }

                CreateInternationalBloc(
                    first,
                    bestPartner,
                    bestScore,
                    currentYear
                );

                // At most one brand-new bloc per world year. The previous loop
                // could pair most unaligned kingdoms at once after loading a
                // Workshop map, which looked like the map had been rewritten.
                return;
            }
        }

        private static bool CanFoundInternationalBloc(
            Kingdom kingdom,
            int currentYear
        )
        {
            return
                kingdom != null &&
                string.IsNullOrEmpty(
                    GetKingdomStringData(
                        kingdom,
                        InternationalBlocIdDataKey,
                        ""
                    )
                ) &&
                GetKingdomIntData(
                    kingdom,
                    InternationalBlocCooldownUntilYearDataKey,
                    0
                ) <= currentYear &&
                GetDiplomaticReputation(kingdom) >= 38 &&
                GetNationalStability(kingdom) >= 28 &&
                !IsKingdomAtWarSafe(kingdom) &&
                GetNativeAllianceSafe(kingdom) == null;
        }

        private static void CreateInternationalBloc(
            Kingdom first,
            Kingdom second,
            int compatibility,
            int currentYear
        )
        {
            if (first == null || second == null)
            {
                return;
            }

            Kingdom leader = GetBlocLeadershipScore(first) >=
                GetBlocLeadershipScore(second)
                    ? first
                    : second;
            string type = DetermineInternationalBlocType(first, second);
            string seed = GetStableObjectIdentity(first) + "|" +
                GetStableObjectIdentity(second) + "|" +
                currentYear.ToString() + "|" +
                UnityEngine.Random.Range(0, 100000).ToString();
            string id = "B" + currentYear.ToString() + "_" +
                StablePartyHash(seed).ToString();

            InternationalBlocSnapshot bloc = new InternationalBlocSnapshot();
            bloc.Id = id;
            bloc.Type = type;
            bloc.Leader = leader;
            bloc.LeaderIdentity = GetStableObjectIdentity(leader);
            bloc.Name = BuildInternationalBlocName(type, leader);
            bloc.FoundingYear = currentYear;
            bloc.Unity = ClampInt(compatibility, 42, 84);
            bloc.Integration = GetInitialInternationalBlocIntegration(type);
            bloc.Members.Add(first);
            bloc.Members.Add(second);

            InternationalBlocs[id] = bloc;
            SyncInternationalBlocToMembers(bloc);
            EnsureInternationalBlocNativeAlliance(bloc);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_bloc_founded"),
                    bloc.Name,
                    GetInternationalBlocTypeName(type),
                    GetWorldObjectDisplayName(first),
                    GetWorldObjectDisplayName(second)
                ),
                leader,
                null,
                GetLivingRuler(leader),
                DiplomatIconPath,
                "bloc_founded_" + id,
                25f
            );
        }

        private static int GetInitialInternationalBlocIntegration(string type)
        {
            if (type == "political_economic_union") return 34;
            if (type == "economic_union") return 27;
            if (type == "military_political") return 25;
            if (type == "trade_union") return 20;
            if (type == "defensive") return 18;
            return 14;
        }

        private static string DetermineInternationalBlocType(
            Kingdom first,
            Kingdom second
        )
        {
            IdeologyBehaviorProfile a = GetIdeologyBehaviorProfile(first);
            IdeologyBehaviorProfile b = GetIdeologyBehaviorProfile(second);
            int militarism = (a.Militarism + b.Militarism) / 2;
            int market = (a.Market + b.Market) / 2;
            int welfare = (a.Welfare + b.Welfare) / 2;
            int centralization = (a.Centralization + b.Centralization) / 2;
            int pluralism = (a.Pluralism + b.Pluralism) / 2;

            if (militarism >= 72 && centralization >= 58)
            {
                return "military_political";
            }
            if (militarism >= 58)
            {
                return "defensive";
            }
            if (market >= 70 && pluralism >= 52)
            {
                return "trade_union";
            }
            if (
                welfare >= 68 &&
                centralization >= 58 &&
                GetStateIdeology(first) == GetStateIdeology(second)
            )
            {
                return "political_economic_union";
            }
            if (market >= 54 || welfare >= 58)
            {
                return "economic_union";
            }
            return "commonwealth";
        }

        private static int CalculateKingdomBlocCompatibility(
            Kingdom first,
            Kingdom second
        )
        {
            if (first == null || second == null)
            {
                return 0;
            }
            if (first == second)
            {
                return 100;
            }
            if (IsKingdomPairAtWarSafe(first, second))
            {
                return 0;
            }

            IdeologyBehaviorProfile a = GetIdeologyBehaviorProfile(first);
            IdeologyBehaviorProfile b = GetIdeologyBehaviorProfile(second);
            int distance =
                Math.Abs(a.Market - b.Market) +
                Math.Abs(a.Welfare - b.Welfare) +
                Math.Abs(a.Centralization - b.Centralization) +
                Math.Abs(a.Pluralism - b.Pluralism) +
                Math.Abs(a.Militarism - b.Militarism);
            int averageDistance = distance / 5;

            int score = 30;
            score += (100 - averageDistance) * 36 / 100;

            string firstIdeology = GetStateIdeology(first);
            string secondIdeology = GetStateIdeology(second);
            if (
                IsValidIdeology(firstIdeology) &&
                firstIdeology == secondIdeology
            )
            {
                score += 18;
            }

            int reputationAverage =
                (GetDiplomaticReputation(first) +
                    GetDiplomaticReputation(second)) / 2;
            score += (reputationAverage - 50) / 3;

            int stabilityAverage =
                (GetNationalStability(first) +
                    GetNationalStability(second)) / 2;
            score += (stabilityAverage - 50) / 5;

            if (GetKingdomCourse(first) == DiplomatTraitId) score += 7;
            if (GetKingdomCourse(second) == DiplomatTraitId) score += 7;
            if (GetKingdomCourse(first) == MilitaristTraitId) score -= 3;
            if (GetKingdomCourse(second) == MilitaristTraitId) score -= 3;

            // Nearby states are more likely to build the first generation of
            // blocs. Distant states are still possible when ideology,
            // reputation and later bloc integration are strong enough.
            score += CalculateInternationalBlocGeographicModifier(
                first,
                second
            );

            bool hardIdeologyConflict =
                (firstIdeology == FascismIdeologyId &&
                    (secondIdeology == CommunismIdeologyId ||
                        secondIdeology == AnarchismIdeologyId)) ||
                (secondIdeology == FascismIdeologyId &&
                    (firstIdeology == CommunismIdeologyId ||
                        firstIdeology == AnarchismIdeologyId));
            if (hardIdeologyConflict)
            {
                score -= 22;
            }

            return ClampInt(score, 0, 100);
        }

        private static void RefreshInternationalBlocGeographyCache()
        {
            _internationalBlocGeographyCacheValid = false;
            _internationalBlocGeographyKingdomCount = 0;

            List<Kingdom> kingdoms = GetKingdomsSafe();
            bool hasCoordinate = false;
            int minX = 0;
            int maxX = 0;
            int minY = 0;
            int maxY = 0;

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                City capital = GetKingdomCapitalCitySafe(kingdom);
                if (capital == null)
                {
                    continue;
                }

                object tile = null;
                try { tile = capital.getTile(); } catch { }

                int x;
                int y;
                if (!TryGetTileCoordinates(tile, out x, out y))
                {
                    continue;
                }

                if (!hasCoordinate)
                {
                    minX = maxX = x;
                    minY = maxY = y;
                    hasCoordinate = true;
                }
                else
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }

                _internationalBlocGeographyKingdomCount++;
            }

            if (!hasCoordinate)
            {
                return;
            }

            _internationalBlocGeographyMinX = minX;
            _internationalBlocGeographyMaxX = maxX;
            _internationalBlocGeographyMinY = minY;
            _internationalBlocGeographyMaxY = maxY;
            _internationalBlocGeographyCacheValid = true;
        }

        private static int CalculateInternationalBlocGeographicModifier(
            Kingdom first,
            Kingdom second
        )
        {
            if (first == null || second == null || first == second)
            {
                return 0;
            }

            if (!_internationalBlocGeographyCacheValid)
            {
                RefreshInternationalBlocGeographyCache();
            }

            // With only two surviving states there is no meaningful
            // alternative regional partner, so geography stays neutral.
            if (
                !_internationalBlocGeographyCacheValid ||
                _internationalBlocGeographyKingdomCount <= 2
            )
            {
                return 0;
            }

            City firstCapital = GetKingdomCapitalCitySafe(first);
            City secondCapital = GetKingdomCapitalCitySafe(second);
            if (firstCapital == null || secondCapital == null)
            {
                return 0;
            }

            object firstTile = null;
            object secondTile = null;
            try { firstTile = firstCapital.getTile(); } catch { }
            try { secondTile = secondCapital.getTile(); } catch { }

            int firstX;
            int firstY;
            int secondX;
            int secondY;
            if (
                !TryGetTileCoordinates(firstTile, out firstX, out firstY) ||
                !TryGetTileCoordinates(secondTile, out secondX, out secondY)
            )
            {
                return 0;
            }

            int worldSpan =
                Math.Max(1,
                    (_internationalBlocGeographyMaxX -
                        _internationalBlocGeographyMinX) +
                    (_internationalBlocGeographyMaxY -
                        _internationalBlocGeographyMinY)
                );
            int capitalDistance =
                Math.Abs(firstX - secondX) +
                Math.Abs(firstY - secondY);
            int distancePercent = ClampInt(
                capitalDistance * 100 / worldSpan,
                0,
                100
            );

            if (distancePercent <= 12) return 12;
            if (distancePercent <= 22) return 8;
            if (distancePercent <= 35) return 3;
            if (distancePercent <= 50) return -5;
            if (distancePercent <= 65) return -11;
            if (distancePercent <= 80) return -18;
            return -26;
        }

        private static int CalculateCandidateBlocCompatibility(
            Kingdom candidate,
            InternationalBlocSnapshot bloc
        )
        {
            if (
                candidate == null ||
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count == 0
            )
            {
                return 0;
            }

            int total = 0;
            int counted = 0;
            for (int i = 0; i < bloc.Members.Count && counted < 4; i++)
            {
                Kingdom member = bloc.Members[i];
                if (member == null)
                {
                    continue;
                }
                total += CalculateKingdomBlocCompatibility(candidate, member);
                counted++;
            }

            int score = counted > 0 ? total / counted : 0;
            score += bloc.Unity / 10;
            score += bloc.Integration / 20;
            return ClampInt(score, 0, 100);
        }

        private static bool CanKingdomJoinInternationalBloc(
            Kingdom candidate,
            InternationalBlocSnapshot bloc
        )
        {
            if (
                candidate == null ||
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count < InternationalBlocMinimumMembers
            )
            {
                return false;
            }

            Alliance candidateAlliance = GetNativeAllianceSafe(candidate);
            Alliance blocAlliance = GetInternationalBlocNativeAlliance(bloc);
            if (
                candidateAlliance != null &&
                (
                    blocAlliance == null ||
                    !Alliance.isSame(candidateAlliance, blocAlliance)
                )
            )
            {
                return false;
            }

            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (
                    member != null &&
                    (IsKingdomPairAtWarSafe(candidate, member) ||
                        GetPairTruceUntilYear(candidate, member) >
                            GetWorldYearSafe())
                )
                {
                    return false;
                }
            }
            return true;
        }

        private static int CalculateInternationalBlocUnity(
            InternationalBlocSnapshot bloc
        )
        {
            if (
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count < InternationalBlocMinimumMembers
            )
            {
                return 0;
            }

            Kingdom leader = bloc.Leader;
            if (leader == null)
            {
                leader = GetStrongestInternationalBlocLeader(bloc);
            }
            if (leader == null)
            {
                return 0;
            }

            int totalCompatibility = 0;
            int totalReputation = 0;
            int counted = 0;
            bool internalWar = false;

            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (member == null)
                {
                    continue;
                }
                totalCompatibility +=
                    CalculateKingdomBlocCompatibility(member, leader);
                totalReputation += GetDiplomaticReputation(member);
                counted++;

                for (int j = i + 1; j < bloc.Members.Count; j++)
                {
                    if (
                        bloc.Members[j] != null &&
                        IsKingdomPairAtWarSafe(member, bloc.Members[j])
                    )
                    {
                        internalWar = true;
                    }
                }
            }

            if (counted <= 0)
            {
                return 0;
            }

            int score = totalCompatibility / counted;
            int averageReputation = totalReputation / counted;
            score += (averageReputation - 50) / 5;
            score += bloc.Integration / 12;
            if (internalWar) score -= 35;
            return ClampInt(score, 0, 100);
        }

        private static int GetBlocLeadershipScore(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 0;
            }
            long score = CalculateDiplomaticPower(kingdom);
            score += GetDiplomaticReputation(kingdom) * 4L;
            score += GetNationalStability(kingdom) * 2L;
            return score > int.MaxValue ? int.MaxValue : (int)score;
        }

        private static Kingdom GetStrongestInternationalBlocLeader(
            InternationalBlocSnapshot bloc
        )
        {
            if (bloc == null || bloc.Members == null)
            {
                return null;
            }

            Kingdom best = null;
            int bestScore = -1;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                int score = GetBlocLeadershipScore(member);
                if (member != null && score > bestScore)
                {
                    best = member;
                    bestScore = score;
                }
            }
            return best;
        }

        private static void ResolveInternationalBlocLeader(
            InternationalBlocSnapshot bloc,
            bool publishChange
        )
        {
            if (bloc == null || bloc.Members == null)
            {
                return;
            }

            Kingdom current = null;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (
                    member != null &&
                    GetStableObjectIdentity(member) == bloc.LeaderIdentity
                )
                {
                    current = member;
                    break;
                }
            }

            Kingdom strongest = GetStrongestInternationalBlocLeader(bloc);
            bool replace = current == null;
            if (
                !replace &&
                strongest != null &&
                strongest != current
            )
            {
                int currentScore = Math.Max(1, GetBlocLeadershipScore(current));
                int strongestScore = GetBlocLeadershipScore(strongest);
                replace = strongestScore >= currentScore + currentScore / 3;
            }

            if (replace && strongest != null)
            {
                Kingdom previous = current;
                bloc.Leader = strongest;
                bloc.LeaderIdentity = GetStableObjectIdentity(strongest);
                if (publishChange && previous != strongest)
                {
                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_bloc_leader_changed"),
                            bloc.Name,
                            GetWorldObjectDisplayName(strongest)
                        ),
                        strongest,
                        null,
                        GetLivingRuler(strongest),
                        DiplomatIconPath,
                        "bloc_leader_" + bloc.Id + "_" +
                            bloc.LeaderIdentity,
                        25f
                    );
                }
            }
            else
            {
                bloc.Leader = current ?? strongest;
                if (bloc.Leader != null)
                {
                    bloc.LeaderIdentity = GetStableObjectIdentity(bloc.Leader);
                }
            }
        }

        private static void TryEvolveInternationalBlocType(
            InternationalBlocSnapshot bloc,
            int currentYear
        )
        {
            if (
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count < 2
            )
            {
                return;
            }

            int lastChange = 0;
            if (bloc.Members.Count > 0)
            {
                lastChange = GetKingdomIntData(
                    bloc.Members[0],
                    InternationalBlocLastTypeChangeYearDataKey,
                    0
                );
            }
            if (currentYear - lastChange < 4)
            {
                return;
            }

            int market = 0;
            int militarism = 0;
            int centralization = 0;
            int pluralism = 0;
            int counted = 0;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (member == null) continue;
                IdeologyBehaviorProfile behavior =
                    GetIdeologyBehaviorProfile(member);
                market += behavior.Market;
                militarism += behavior.Militarism;
                centralization += behavior.Centralization;
                pluralism += behavior.Pluralism;
                counted++;
            }
            if (counted <= 0) return;
            market /= counted;
            militarism /= counted;
            centralization /= counted;
            pluralism /= counted;

            string newType = bloc.Type;
            if (
                bloc.Type == "defensive" &&
                bloc.Integration >= 70 &&
                militarism >= 62
            )
            {
                newType = "military_political";
            }
            else if (
                bloc.Type == "trade_union" &&
                bloc.Integration >= 55
            )
            {
                newType = "economic_union";
            }
            else if (
                bloc.Type == "economic_union" &&
                bloc.Integration >= 80 &&
                bloc.Unity >= 62
            )
            {
                newType = "political_economic_union";
            }
            else if (
                bloc.Type == "commonwealth" &&
                bloc.Integration >= 48
            )
            {
                if (militarism >= 60) newType = "defensive";
                else if (market >= 60 && pluralism >= 50)
                    newType = "trade_union";
                else if (centralization >= 55)
                    newType = "economic_union";
            }

            if (newType == bloc.Type)
            {
                return;
            }

            string oldType = bloc.Type;
            bloc.Type = newType;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                SetKingdomIntData(
                    bloc.Members[i],
                    InternationalBlocLastTypeChangeYearDataKey,
                    currentYear
                );
            }
            SyncInternationalBlocToMembers(bloc);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_bloc_evolved"),
                    bloc.Name,
                    GetInternationalBlocTypeName(oldType),
                    GetInternationalBlocTypeName(newType)
                ),
                bloc.Leader,
                null,
                GetLivingRuler(bloc.Leader),
                DiplomatIconPath,
                "bloc_evolved_" + bloc.Id + "_" + newType,
                25f
            );
        }

        private static void SyncInternationalBlocToMembers(
            InternationalBlocSnapshot bloc
        )
        {
            if (bloc == null || bloc.Members == null)
            {
                return;
            }

            string leaderName = bloc.Leader == null
                ? "?"
                : GetWorldObjectDisplayName(bloc.Leader);
            bloc.LeaderIdentity = bloc.Leader == null
                ? bloc.LeaderIdentity
                : GetStableObjectIdentity(bloc.Leader);

            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom member = bloc.Members[i];
                if (member == null) continue;
                SetKingdomStringData(member, InternationalBlocIdDataKey, bloc.Id);
                SetKingdomStringData(member, InternationalBlocTypeDataKey, bloc.Type);
                SetKingdomStringData(member, InternationalBlocNameDataKey, bloc.Name);
                SetKingdomStringData(
                    member,
                    InternationalBlocLeaderIdentityDataKey,
                    bloc.LeaderIdentity ?? ""
                );
                SetKingdomStringData(
                    member,
                    InternationalBlocLeaderNameDataKey,
                    leaderName
                );
                SetKingdomIntData(
                    member,
                    InternationalBlocFoundingYearDataKey,
                    bloc.FoundingYear
                );
                SetKingdomIntData(
                    member,
                    InternationalBlocUnityDataKey,
                    ClampInt(bloc.Unity, 0, 100)
                );
                SetKingdomIntData(
                    member,
                    InternationalBlocIntegrationDataKey,
                    ClampInt(bloc.Integration, 0, 100)
                );
                SetKingdomIntData(
                    member,
                    InternationalBlocMemberCountDataKey,
                    bloc.Members.Count
                );
                SetKingdomStringData(
                    member,
                    InternationalBlocNativeAllianceIdDataKey,
                    bloc.NativeAllianceId ?? ""
                );
            }
        }

        private static void ClearInternationalBlocMembership(
            Kingdom kingdom,
            int cooldownUntilYear
        )
        {
            if (kingdom == null)
            {
                return;
            }
            SetKingdomStringData(kingdom, InternationalBlocIdDataKey, "");
            SetKingdomStringData(kingdom, InternationalBlocTypeDataKey, "");
            SetKingdomStringData(kingdom, InternationalBlocNameDataKey, "");
            SetKingdomStringData(
                kingdom,
                InternationalBlocLeaderIdentityDataKey,
                ""
            );
            SetKingdomStringData(
                kingdom,
                InternationalBlocLeaderNameDataKey,
                ""
            );
            SetKingdomIntData(kingdom, InternationalBlocFoundingYearDataKey, 0);
            SetKingdomIntData(kingdom, InternationalBlocUnityDataKey, 0);
            SetKingdomIntData(kingdom, InternationalBlocIntegrationDataKey, 0);
            SetKingdomIntData(kingdom, InternationalBlocMemberCountDataKey, 0);
            SetKingdomStringData(
                kingdom,
                InternationalBlocNativeAllianceIdDataKey,
                ""
            );
            SetKingdomIntData(
                kingdom,
                InternationalBlocCooldownUntilYearDataKey,
                Math.Max(
                    GetKingdomIntData(
                        kingdom,
                        InternationalBlocCooldownUntilYearDataKey,
                        0
                    ),
                    cooldownUntilYear
                )
            );
        }

        private static void RemoveKingdomFromInternationalBloc(
            Kingdom kingdom,
            InternationalBlocSnapshot bloc,
            int currentYear,
            bool publish,
            string chronicleReason = ""
        )
        {
            if (kingdom == null || bloc == null || bloc.Members == null)
            {
                return;
            }

            Alliance nativeAlliance = GetInternationalBlocNativeAlliance(bloc);
            if (nativeAlliance != null)
            {
                RemoveKingdomFromNativeAlliance(kingdom, nativeAlliance);
            }

            bloc.Members.Remove(kingdom);
            ClearInternationalBlocMembership(
                kingdom,
                currentYear + InternationalBlocRejoinCooldownYears
            );
            ChangeDiplomaticReputation(kingdom, -2);

            if (publish)
            {
                List<string> causes = new List<string>();
                if (chronicleReason == "internal_war")
                {
                    causes.Add(
                        string.Format(
                            LM.Get(
                                "ukiol_chronicle_detail_bloc_internal_war"
                            ),
                            bloc.Unity,
                            InternationalBlocDefenceUnity
                        )
                    );
                }
                else
                {
                    int compatibility = bloc.Leader == null
                        ? 100
                        : CalculateKingdomBlocCompatibility(
                            kingdom,
                            bloc.Leader
                        );
                    int reputation = GetDiplomaticReputation(kingdom);
                    int stability = GetNationalStability(kingdom);

                    if (compatibility < 30)
                    {
                        causes.Add(
                            string.Format(
                                LM.Get(
                                    "ukiol_chronicle_detail_bloc_compatibility"
                                ),
                                compatibility
                            )
                        );
                    }
                    if (reputation < 25)
                    {
                        causes.Add(
                            string.Format(
                                LM.Get(
                                    "ukiol_chronicle_detail_bloc_reputation"
                                ),
                                reputation
                            )
                        );
                    }
                    if (bloc.Unity < 30)
                    {
                        causes.Add(
                            string.Format(
                                LM.Get("ukiol_chronicle_detail_bloc_unity"),
                                bloc.Unity
                            )
                        );
                    }
                    if (stability < 25)
                    {
                        causes.Add(
                            string.Format(
                                LM.Get(
                                    "ukiol_chronicle_detail_bloc_stability"
                                ),
                                stability
                            )
                        );
                    }
                    if (causes.Count == 0)
                    {
                        causes.Add(
                            LM.Get(
                                "ukiol_chronicle_detail_bloc_membership_unstable"
                            )
                        );
                    }
                }

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_bloc_left"),
                        GetWorldObjectDisplayName(kingdom),
                        bloc.Name
                    ),
                    kingdom,
                    null,
                    GetLivingRuler(kingdom),
                    DiplomatIconPath,
                    "bloc_left_" + bloc.Id + "_" +
                        GetStableObjectIdentity(kingdom),
                    25f,
                    causes,
                    new List<string>
                    {
                        string.Format(
                            LM.Get(
                                "ukiol_chronicle_detail_bloc_left_consequence"
                            ),
                            bloc.Name
                        ),
                        string.Format(
                            LM.Get(
                                "ukiol_chronicle_detail_bloc_rejoin"
                            ),
                            currentYear +
                                InternationalBlocRejoinCooldownYears
                        )
                    }
                );
            }
        }

        private static void DissolveInternationalBloc(
            InternationalBlocSnapshot bloc,
            int currentYear
        )
        {
            if (bloc == null)
            {
                return;
            }

            Alliance nativeAlliance = GetInternationalBlocNativeAlliance(bloc);
            if (
                nativeAlliance != null &&
                World.world != null &&
                World.world.alliances != null
            )
            {
                try
                {
                    World.world.alliances.dissolveAlliance(nativeAlliance);
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "Native alliance dissolve failed for bloc " +
                        bloc.Id + ": " + exception.Message
                    );
                }
            }

            List<Kingdom> members = bloc.Members == null
                ? new List<Kingdom>()
                : new List<Kingdom>(bloc.Members);
            for (int i = 0; i < members.Count; i++)
            {
                ClearInternationalBlocMembership(
                    members[i],
                    currentYear + InternationalBlocRejoinCooldownYears
                );
            }
            InternationalBlocs.Remove(bloc.Id);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_bloc_dissolved"),
                    bloc.Name
                ),
                bloc.Leader,
                null,
                GetLivingRuler(bloc.Leader),
                DiplomatIconPath,
                "bloc_dissolved_" + bloc.Id,
                30f
            );
        }

        private static void ApplyInternationalBlocBenefits()
        {
            List<InternationalBlocSnapshot> blocs =
                new List<InternationalBlocSnapshot>(InternationalBlocs.Values);
            for (int b = 0; b < blocs.Count; b++)
            {
                InternationalBlocSnapshot bloc = blocs[b];
                if (bloc == null || bloc.Members == null) continue;

                for (int m = 0; m < bloc.Members.Count; m++)
                {
                    Kingdom member = bloc.Members[m];
                    if (member == null) continue;

                    if (IsEconomicInternationalBlocType(bloc.Type))
                    {
                        List<City> cities = GetCitiesSafe(member);
                        int goldCities = bloc.Type == "trade_union" ? 1 : 2;
                        if (bloc.Type == "political_economic_union")
                            goldCities = 3;
                        goldCities = Math.Min(goldCities, cities.Count);
                        for (int c = 0; c < goldCities; c++)
                        {
                            TryChangeCityResource(cities[c], "gold", 1);
                            if (
                                bloc.Type == "political_economic_union" ||
                                (bloc.Type == "economic_union" && c == 0)
                            )
                            {
                                TryChangeCityResource(cities[c], "bread", 1);
                            }
                        }
                    }

                    if (
                        IsSecurityInternationalBlocType(bloc.Type) &&
                        IsKingdomAtWarSafe(member) &&
                        bloc.Unity >= 50
                    )
                    {
                        AddWarExhaustion(
                            member,
                            bloc.Type == "military_political" ? -2 : -1
                        );
                    }

                    if (
                        bloc.Type == "commonwealth" &&
                        bloc.Unity >= 65 &&
                        GetNationalStability(member) < 70
                    )
                    {
                        SetNationalStability(
                            member,
                            GetNationalStability(member) + 1
                        );
                    }
                }
            }
        }

        private static bool IsSecurityInternationalBlocType(string type)
        {
            return
                type == "defensive" ||
                type == "military_political";
        }

        private static bool IsEconomicInternationalBlocType(string type)
        {
            return
                type == "trade_union" ||
                type == "economic_union" ||
                type == "political_economic_union";
        }

        private static string BuildInternationalBlocName(
            string type,
            Kingdom leader
        )
        {
            string templateKey = "ukiol_bloc_name_template_" +
                (string.IsNullOrEmpty(type) ? "commonwealth" : type);
            string template = LM.Get(templateKey);
            if (
                string.IsNullOrEmpty(template) ||
                template == templateKey
            )
            {
                template = "{0} Pact";
            }
            return string.Format(
                template,
                GetWorldObjectDisplayName(leader)
            );
        }

        private static string GetInternationalBlocTypeName(string type)
        {
            string safe = string.IsNullOrEmpty(type)
                ? "commonwealth"
                : type;
            string key = "ukiol_bloc_type_" + safe;
            string value = LM.Get(key);
            return string.IsNullOrEmpty(value) || value == key
                ? safe
                : value;
        }

        private static Color GetInternationalBlocColor(string type)
        {
            if (type == "military_political")
                return new Color(0.93f, 0.43f, 0.36f, 1f);
            if (type == "defensive")
                return new Color(0.92f, 0.68f, 0.30f, 1f);
            if (type == "trade_union")
                return new Color(0.55f, 0.83f, 0.91f, 1f);
            if (type == "economic_union")
                return new Color(0.45f, 0.86f, 0.65f, 1f);
            if (type == "political_economic_union")
                return new Color(0.58f, 0.76f, 0.95f, 1f);
            return new Color(0.76f, 0.72f, 0.90f, 1f);
        }

        private static InternationalBlocSnapshot GetInternationalBlocForKingdom(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return null;
            }
            string id = GetKingdomStringData(
                kingdom,
                InternationalBlocIdDataKey,
                ""
            );
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            InternationalBlocSnapshot bloc;
            if (!InternationalBlocs.TryGetValue(id, out bloc))
            {
                RebuildInternationalBlocRuntimeIndex();
                InternationalBlocs.TryGetValue(id, out bloc);
            }
            return bloc;
        }

        private static bool AreKingdomsInSameInternationalBloc(
            Kingdom first,
            Kingdom second
        )
        {
            if (first == null || second == null)
            {
                return false;
            }
            string firstId = GetKingdomStringData(
                first,
                InternationalBlocIdDataKey,
                ""
            );
            string secondId = GetKingdomStringData(
                second,
                InternationalBlocIdDataKey,
                ""
            );
            return
                !string.IsNullOrEmpty(firstId) &&
                firstId == secondId;
        }

        private static bool HandleInternationalBlocInternalWarAttempt(
            Kingdom attacker,
            Kingdom defender
        )
        {
            if (!AreKingdomsInSameInternationalBloc(attacker, defender))
            {
                return false;
            }

            InternationalBlocSnapshot bloc = GetInternationalBlocForKingdom(
                attacker
            );
            int unity = bloc == null
                ? GetKingdomIntData(
                    attacker,
                    InternationalBlocUnityDataKey,
                    50
                )
                : bloc.Unity;

            if (unity >= InternationalBlocDefenceUnity)
            {
                ChangeDiplomaticReputation(attacker, -3);
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_bloc_internal_war_blocked"),
                        GetWorldObjectDisplayName(attacker),
                        GetWorldObjectDisplayName(defender),
                        bloc == null
                            ? GetKingdomStringData(
                                attacker,
                                InternationalBlocNameDataKey,
                                "?"
                            )
                            : bloc.Name
                    ),
                    attacker,
                    null,
                    GetLivingRuler(attacker),
                    DiplomatIconPath,
                    "bloc_internal_blocked_" + GetWarPairKey(attacker, defender),
                    30f
                );
                return true;
            }

            if (bloc != null)
            {
                RemoveKingdomFromInternationalBloc(
                    attacker,
                    bloc,
                    GetWorldYearSafe(),
                    true,
                    "internal_war"
                );
                RebuildInternationalBlocRuntimeIndex();
            }
            return false;
        }

        private static void ActivateInternationalBlocCollectiveDefense(
            PendingWarDeclaration originalWar
        )
        {
            if (
                originalWar == null ||
                originalWar.Attacker == null ||
                originalWar.Defender == null ||
                originalWar.FromBlocCollectiveDefense
            )
            {
                return;
            }

            InternationalBlocSnapshot bloc =
                GetInternationalBlocForKingdom(originalWar.Defender);

            // dev16: when this bloc is backed by a vanilla Alliance, WorldBox
            // already expands wars to allies. Scheduling our old helper wars
            // as well would duplicate the same collective-defense reaction.
            if (
                bloc != null &&
                GetInternationalBlocNativeAlliance(bloc) != null
            )
            {
                return;
            }

            if (
                bloc == null ||
                !IsSecurityInternationalBlocType(bloc.Type) ||
                bloc.Unity < InternationalBlocDefenceUnity ||
                bloc.Members == null
            )
            {
                return;
            }

            int currentYear = GetWorldYearSafe();
            int scheduled = 0;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                Kingdom helper = bloc.Members[i];
                if (
                    helper == null ||
                    helper == originalWar.Defender ||
                    helper == originalWar.Attacker ||
                    scheduled >= InternationalBlocMaxCollectiveDefenders ||
                    IsKingdomPairAtWarSafe(helper, originalWar.Attacker) ||
                    GetPairTruceUntilYear(helper, originalWar.Attacker) >
                        currentYear
                )
                {
                    continue;
                }

                string pairKey = GetWarPairKey(helper, originalWar.Attacker);
                if (
                    PendingWarDeclarations.ContainsKey(pairKey) ||
                    PendingDiplomaticCrises.ContainsKey(pairKey)
                )
                {
                    continue;
                }

                object[] helperArgs = BuildWarArgsForBlocCollectiveDefense(
                    originalWar.Args,
                    helper,
                    originalWar.Attacker
                );
                if (helperArgs == null)
                {
                    continue;
                }

                PendingWarDeclaration defence = new PendingWarDeclaration();
                defence.Attacker = helper;
                defence.Defender = originalWar.Attacker;
                defence.Diplomacy = originalWar.Diplomacy;
                defence.StartMethod = originalWar.StartMethod;
                defence.Args = helperArgs;
                defence.ExecuteAt = Time.time + 0.9f + scheduled * 0.45f;
                defence.CasusBelli = "collective_defense";
                defence.PairKey = pairKey;
                defence.DeclaredYear = currentYear;
                defence.FromBlocCollectiveDefense = true;
                PendingWarDeclarations[pairKey] = defence;

                SetKingdomIntData(helper, WarPreparationStateDataKey, 2);
                SetKingdomStringData(
                    helper,
                    WarPreparationTargetNameDataKey,
                    GetWorldObjectDisplayName(originalWar.Attacker)
                );
                SetKingdomStringData(
                    helper,
                    WarPreparationCasusBelliDataKey,
                    "collective_defense"
                );
                scheduled++;
            }

            if (scheduled > 0)
            {
                SetNationalStability(
                    originalWar.Defender,
                    GetNationalStability(originalWar.Defender) +
                        Math.Min(3, scheduled)
                );
                AddWarExhaustion(originalWar.Defender, -1);

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_bloc_collective_defense"),
                        bloc.Name,
                        GetWorldObjectDisplayName(originalWar.Defender),
                        scheduled,
                        GetWorldObjectDisplayName(originalWar.Attacker)
                    ),
                    originalWar.Defender,
                    null,
                    GetLivingRuler(originalWar.Defender),
                    MilitaristIconPath,
                    "bloc_collective_defense_" + bloc.Id + "_" +
                        originalWar.PairKey,
                    20f
                );
            }
        }

        private static object[] BuildWarArgsForBlocCollectiveDefense(
            object[] source,
            Kingdom helper,
            Kingdom aggressor
        )
        {
            if (source == null || helper == null || aggressor == null)
            {
                return null;
            }

            object[] result = CloneObjectArray(source);
            int kingdomIndex = 0;
            for (int i = 0; i < result.Length; i++)
            {
                if (!(result[i] is Kingdom))
                {
                    continue;
                }

                if (kingdomIndex == 0)
                {
                    result[i] = helper;
                }
                else if (kingdomIndex == 1)
                {
                    result[i] = aggressor;
                    return result;
                }
                kingdomIndex++;
            }
            return null;
        }


        // -----------------------------------------------------------------
        // v1.5.0-dev16 - Native Alliance Integration
        // -----------------------------------------------------------------


        private static Alliance GetNativeAllianceSafe(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return null;
            }

            try
            {
                return kingdom.getAlliance();
            }
            catch
            {
                return null;
            }
        }

        private static void ImportUnmanagedNativeAlliancesAsInternationalBlocs(
            int currentYear
        )
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            List<Alliance> nativeAlliances = new List<Alliance>();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Alliance alliance = GetNativeAllianceSafe(kingdoms[i]);
                if (alliance == null)
                {
                    continue;
                }

                bool known = false;
                for (int a = 0; a < nativeAlliances.Count; a++)
                {
                    if (Alliance.isSame(nativeAlliances[a], alliance))
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    nativeAlliances.Add(alliance);
                }
            }

            for (int a = 0; a < nativeAlliances.Count; a++)
            {
                Alliance alliance = nativeAlliances[a];
                List<Kingdom> members = new List<Kingdom>();

                try
                {
                    foreach (Kingdom member in alliance.kingdoms_list)
                    {
                        if (
                            member != null &&
                            !members.Contains(member)
                        )
                        {
                            members.Add(member);
                        }
                    }
                }
                catch
                {
                    continue;
                }

                if (members.Count < InternationalBlocMinimumMembers)
                {
                    continue;
                }

                bool alreadyManaged = false;
                bool hasUnmanagedMember = false;
                bool conflictingManagedBlocs = false;
                string existingBlocId = "";

                for (int i = 0; i < members.Count; i++)
                {
                    string memberBlocId = GetKingdomStringData(
                        members[i],
                        InternationalBlocIdDataKey,
                        ""
                    );

                    if (string.IsNullOrEmpty(memberBlocId))
                    {
                        hasUnmanagedMember = true;
                        continue;
                    }

                    if (string.IsNullOrEmpty(existingBlocId))
                    {
                        existingBlocId = memberBlocId;
                        alreadyManaged = true;
                    }
                    else if (existingBlocId != memberBlocId)
                    {
                        conflictingManagedBlocs = true;
                        break;
                    }
                }

                if (
                    conflictingManagedBlocs ||
                    (alreadyManaged && hasUnmanagedMember)
                )
                {
                    // Do not let an unrelated vanilla alliance inflate an
                    // existing dev15 Political World bloc during migration.
                    continue;
                }

                if (alreadyManaged)
                {
                    InternationalBlocSnapshot existingBloc = null;
                    if (
                        InternationalBlocs.TryGetValue(
                            existingBlocId,
                            out existingBloc
                        ) &&
                        existingBloc != null
                    )
                    {
                        existingBloc.NativeAlliance = alliance;
                        existingBloc.NativeAllianceId =
                            GetNativeAllianceIdentity(alliance);

                        for (int i = 0; i < members.Count; i++)
                        {
                            if (!existingBloc.Members.Contains(members[i]))
                            {
                                existingBloc.Members.Add(members[i]);
                            }
                        }

                        SyncInternationalBlocToMembers(existingBloc);
                    }
                    continue;
                }

                Kingdom leader = null;
                int leaderScore = -1;
                for (int i = 0; i < members.Count; i++)
                {
                    int score = GetBlocLeadershipScore(members[i]);
                    if (score > leaderScore)
                    {
                        leader = members[i];
                        leaderScore = score;
                    }
                }

                string type = DetermineInternationalBlocType(
                    members[0],
                    members[1]
                );

                string nativeId = GetNativeAllianceIdentity(alliance);
                string blocId = "VANILLA_" +
                    StablePartyHash(
                        nativeId + "|" + currentYear.ToString()
                    ).ToString();

                InternationalBlocSnapshot bloc =
                    new InternationalBlocSnapshot();

                bloc.Id = blocId;
                bloc.Type = type;
                bloc.Name =
                    alliance.data == null ||
                    string.IsNullOrEmpty(alliance.data.name)
                        ? BuildInternationalBlocName(type, leader)
                        : alliance.data.name;
                bloc.Leader = leader;
                bloc.LeaderIdentity = GetStableObjectIdentity(leader);
                bloc.FoundingYear = currentYear;
                bloc.Unity = ClampInt(
                    CalculateKingdomBlocCompatibility(
                        members[0],
                        members[1]
                    ),
                    42,
                    84
                );
                bloc.Integration =
                    GetInitialInternationalBlocIntegration(type);
                bloc.NativeAlliance = alliance;
                bloc.NativeAllianceId = nativeId;

                for (int i = 0; i < members.Count; i++)
                {
                    bloc.Members.Add(members[i]);
                }

                InternationalBlocs[bloc.Id] = bloc;
                SyncInternationalBlocToMembers(bloc);

                LogInfo(
                    "Imported vanilla alliance into Political World: " +
                    bloc.Name + " (" + bloc.Id + ")"
                );
            }
        }

        private static void SynchronizeInternationalBlocsWithNativeAlliances(
            int currentYear
        )
        {
            if (World.world == null || World.world.alliances == null)
            {
                return;
            }

            List<InternationalBlocSnapshot> blocs =
                new List<InternationalBlocSnapshot>(InternationalBlocs.Values);

            for (int i = 0; i < blocs.Count; i++)
            {
                InternationalBlocSnapshot bloc = blocs[i];
                if (bloc == null)
                {
                    continue;
                }

                bool hadNativeLink =
                    !string.IsNullOrEmpty(bloc.NativeAllianceId);
                Alliance alliance = GetInternationalBlocNativeAlliance(bloc);

                // dev8: once a Political World bloc is linked to a real
                // vanilla alliance, vanilla membership changes are imported
                // back into the bloc before we mirror Political World changes
                // out again. This makes the bridge genuinely bidirectional.
                if (alliance != null)
                {
                    if (!SynchronizeInternationalBlocFromNativeAlliance(
                        bloc,
                        alliance,
                        currentYear
                    ))
                    {
                        continue;
                    }
                }
                else if (hadNativeLink)
                {
                    // The linked vanilla alliance disappeared/dissolved.
                    // Do not silently recreate it and trap the former members
                    // back inside the bloc; mirror the vanilla dissolution.
                    DissolveInternationalBloc(bloc, currentYear);
                    continue;
                }

                EnsureInternationalBlocNativeAlliance(bloc);
            }
        }

        private static bool SynchronizeInternationalBlocFromNativeAlliance(
            InternationalBlocSnapshot bloc,
            Alliance alliance,
            int currentYear
        )
        {
            if (bloc == null || alliance == null || bloc.Members == null)
            {
                return false;
            }

            List<Kingdom> nativeMembers =
                GetNativeAllianceMembersSafe(alliance);

            // A linked alliance with fewer than two actual members is no
            // longer a valid WorldBox alliance. Mirror that dissolution into
            // Political World instead of recreating it next tick.
            if (nativeMembers.Count < InternationalBlocMinimumMembers)
            {
                DissolveInternationalBloc(bloc, currentYear);
                return false;
            }

            // Alliance rename -> Political World bloc rename. Compare against
            // the value we ourselves would display in vanilla so a long bloc
            // name that was merely truncated to 30 characters is not mistaken
            // for a user/vanilla rename.
            try
            {
                if (alliance.data != null)
                {
                    string nativeName = alliance.data.name ?? "";
                    string mirroredName =
                        GetNativeAllianceDisplayName(bloc.Name);
                    if (
                        !string.IsNullOrEmpty(nativeName) &&
                        nativeName != mirroredName
                    )
                    {
                        bloc.Name = nativeName;
                    }
                }
            }
            catch
            {
                // A name mismatch must never break membership sync.
            }

            // Vanilla leave/kick -> Political World leave. PW-initiated
            // removals already remove the native membership first, so by the
            // time this yearly reconciliation runs both sides still agree.
            List<Kingdom> politicalMembers =
                new List<Kingdom>(bloc.Members);
            for (int i = 0; i < politicalMembers.Count; i++)
            {
                Kingdom member = politicalMembers[i];
                if (member == null || nativeMembers.Contains(member))
                {
                    continue;
                }

                bloc.Members.Remove(member);
                ClearInternationalBlocMembership(
                    member,
                    currentYear + InternationalBlocRejoinCooldownYears
                );
                ChangeDiplomaticReputation(member, -2);
            }

            // Vanilla join -> Political World join, but never steal a kingdom
            // that is already owned by a different managed bloc. The existing
            // outward pass will detach such conflicts from this alliance.
            for (int i = 0; i < nativeMembers.Count; i++)
            {
                Kingdom member = nativeMembers[i];
                if (member == null || bloc.Members.Contains(member))
                {
                    continue;
                }

                string otherBlocId = GetKingdomStringData(
                    member,
                    InternationalBlocIdDataKey,
                    ""
                );
                if (
                    string.IsNullOrEmpty(otherBlocId) ||
                    otherBlocId == bloc.Id
                )
                {
                    bloc.Members.Add(member);
                }
            }

            if (bloc.Members.Count < InternationalBlocMinimumMembers)
            {
                DissolveInternationalBloc(bloc, currentYear);
                return false;
            }

            if (bloc.Leader == null || !bloc.Members.Contains(bloc.Leader))
            {
                bloc.Leader = GetStrongestInternationalBlocLeader(bloc);
                bloc.LeaderIdentity = bloc.Leader == null
                    ? ""
                    : GetStableObjectIdentity(bloc.Leader);
            }

            bloc.NativeAlliance = alliance;
            bloc.NativeAllianceId = GetNativeAllianceIdentity(alliance);
            SyncInternationalBlocToMembers(bloc);
            return true;
        }

        private static List<Kingdom> GetNativeAllianceMembersSafe(
            Alliance alliance
        )
        {
            List<Kingdom> members = new List<Kingdom>();
            if (alliance == null)
            {
                return members;
            }

            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null)
                {
                    continue;
                }

                Alliance current = GetNativeAllianceSafe(kingdom);
                if (
                    current != null &&
                    Alliance.isSame(current, alliance) &&
                    !members.Contains(kingdom)
                )
                {
                    members.Add(kingdom);
                }
            }
            return members;
        }

        private static Alliance GetInternationalBlocNativeAlliance(
            InternationalBlocSnapshot bloc
        )
        {
            if (bloc == null)
            {
                return null;
            }

            if (bloc.NativeAlliance != null)
            {
                return bloc.NativeAlliance;
            }

            string expectedId = bloc.NativeAllianceId ?? "";

            if (bloc.Members != null)
            {
                Alliance shared = null;
                bool sharedValid = true;

                for (int i = 0; i < bloc.Members.Count; i++)
                {
                    Kingdom member = bloc.Members[i];
                    if (member == null)
                    {
                        continue;
                    }

                    Alliance alliance = GetNativeAllianceSafe(member);

                    if (
                        alliance != null &&
                        !string.IsNullOrEmpty(expectedId) &&
                        GetNativeAllianceIdentity(alliance) == expectedId
                    )
                    {
                        bloc.NativeAlliance = alliance;
                        return alliance;
                    }

                    if (alliance == null)
                    {
                        sharedValid = false;
                        continue;
                    }

                    if (shared == null)
                    {
                        shared = alliance;
                    }
                    else if (!Alliance.isSame(shared, alliance))
                    {
                        sharedValid = false;
                    }
                }

                // Old dev15 saves have no stored native alliance id yet.
                // If every member already happens to share one vanilla
                // alliance, adopt it instead of destroying/recreating it.
                if (
                    string.IsNullOrEmpty(expectedId) &&
                    sharedValid &&
                    shared != null
                )
                {
                    bloc.NativeAlliance = shared;
                    bloc.NativeAllianceId = GetNativeAllianceIdentity(shared);
                    return shared;
                }
            }

            return null;
        }

        private static string GetNativeAllianceIdentity(Alliance alliance)
        {
            if (alliance == null)
            {
                return "";
            }

            return GetStableObjectIdentity(alliance);
        }

        private static bool IsKingdomInCurrentWorld(Kingdom kingdom)
        {
            if (kingdom == null || World.world == null || World.world.kingdoms == null)
            {
                return false;
            }

            List<Kingdom> currentKingdoms = GetKingdomsSafe();
            for (int i = 0; i < currentKingdoms.Count; i++)
            {
                if (object.ReferenceEquals(currentKingdoms[i], kingdom))
                {
                    return true;
                }
            }

            return false;
        }

        private static Alliance CreateNativeAllianceForInternationalBloc(
            InternationalBlocSnapshot bloc
        )
        {
            if (
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count < InternationalBlocMinimumMembers ||
                World.world == null ||
                World.world.alliances == null
            )
            {
                return null;
            }

            int currentMemberCount = 0;
            for (int i = 0; i < bloc.Members.Count; i++)
            {
                if (IsKingdomInCurrentWorld(bloc.Members[i]))
                {
                    currentMemberCount++;
                }
            }
            if (currentMemberCount < InternationalBlocMinimumMembers)
            {
                return null;
            }

            Alliance alliance = null;

            try
            {
                alliance = World.world.alliances.newObject();
                if (alliance == null)
                {
                    return null;
                }

                alliance.createNewAlliance();

                Kingdom founder = bloc.Leader;
                if (founder == null && bloc.Members.Count > 0)
                {
                    founder = bloc.Members[0];
                }

                if (founder != null && founder.data != null)
                {
                    alliance.data.founder_kingdom_id = founder.data.id;
                    alliance.data.founder_kingdom_name = founder.data.name;

                    if (founder.king != null && founder.king.data != null)
                    {
                        alliance.data.founder_actor_id = founder.king.data.id;
                        alliance.data.founder_actor_name =
                            founder.king.getName();
                    }
                }

                alliance.data.name = GetNativeAllianceDisplayName(bloc.Name);
                bloc.NativeAlliance = alliance;
                bloc.NativeAllianceId = GetNativeAllianceIdentity(alliance);

                for (int i = 0; i < bloc.Members.Count; i++)
                {
                    ForceKingdomIntoNativeAlliance(
                        bloc.Members[i],
                        alliance
                    );
                }

                alliance.recalculate();
                WorldLog.logAllianceCreated(alliance);

                LogInfo(
                    "Native alliance created for Political World bloc " +
                    bloc.Id + " -> " + alliance.data.name
                );

                return alliance;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Native alliance creation failed for bloc " +
                    (bloc == null ? "?" : bloc.Id) + ": " +
                    exception.Message
                );
                return null;
            }
        }

        private static string GetNativeAllianceDisplayName(string blocName)
        {
            string value = string.IsNullOrEmpty(blocName)
                ? "Political Bloc"
                : blocName;

            // Vanilla alliance names are limited in the UI. Keeping the
            // Political World name within the same practical limit avoids
            // clipping in the Alliance list/layer.
            if (value.Length > 30)
            {
                value = value.Substring(0, 30);
            }

            return value;
        }

        private static void EnsureInternationalBlocNativeAlliance(
            InternationalBlocSnapshot bloc
        )
        {
            if (
                bloc == null ||
                bloc.Members == null ||
                bloc.Members.Count < InternationalBlocMinimumMembers ||
                World.world == null ||
                World.world.alliances == null
            )
            {
                return;
            }

            Alliance alliance = GetInternationalBlocNativeAlliance(bloc);
            if (alliance == null)
            {
                alliance = CreateNativeAllianceForInternationalBloc(bloc);
                if (alliance == null)
                {
                    return;
                }
            }

            bloc.NativeAlliance = alliance;
            bloc.NativeAllianceId = GetNativeAllianceIdentity(alliance);

            try
            {
                alliance.data.name = GetNativeAllianceDisplayName(bloc.Name);
            }
            catch
            {
                // Name sync is cosmetic; membership is the important part.
            }

            // dev8 reconciliation has already imported legitimate vanilla
            // joins/leaves. This outward pass now resolves only conflicts and
            // guarantees that both systems finish the tick with one roster.
            List<Kingdom> nativeMembers = new List<Kingdom>();
            try
            {
                foreach (Kingdom member in alliance.kingdoms_list)
                {
                    if (member != null && !nativeMembers.Contains(member))
                    {
                        nativeMembers.Add(member);
                    }
                }
            }
            catch
            {
                // If the list cannot be read on a future game build, joining
                // our known members below is still enough for the overlay.
            }

            for (int i = 0; i < nativeMembers.Count; i++)
            {
                Kingdom nativeMember = nativeMembers[i];
                if (bloc.Members.Contains(nativeMember))
                {
                    continue;
                }

                string otherBlocId = GetKingdomStringData(
                    nativeMember,
                    InternationalBlocIdDataKey,
                    ""
                );

                // If vanilla naturally added a previously-unaffiliated state
                // to this alliance, adopt it into the Political World bloc.
                // A state already owned by a different PW bloc is detached
                // instead so one kingdom cannot belong to two blocs.
                if (
                    string.IsNullOrEmpty(otherBlocId) ||
                    otherBlocId == bloc.Id
                )
                {
                    bloc.Members.Add(nativeMember);
                }
                else
                {
                    RemoveKingdomFromNativeAlliance(
                        nativeMember,
                        alliance
                    );
                }
            }

            for (int i = 0; i < bloc.Members.Count; i++)
            {
                ForceKingdomIntoNativeAlliance(
                    bloc.Members[i],
                    alliance
                );
            }

            try
            {
                alliance.recalculate();
            }
            catch
            {
                // Non-fatal. WorldBox will recalculate again on its own.
            }

            SyncInternationalBlocToMembers(bloc);
        }

        private static void ForceKingdomIntoNativeAlliance(
            Kingdom kingdom,
            Alliance targetAlliance
        )
        {
            if (
                kingdom == null ||
                targetAlliance == null ||
                World.world == null ||
                World.world.alliances == null ||
                !IsKingdomInCurrentWorld(kingdom)
            )
            {
                return;
            }

            Alliance currentAlliance = GetNativeAllianceSafe(kingdom);

            if (
                currentAlliance != null &&
                Alliance.isSame(currentAlliance, targetAlliance)
            )
            {
                try
                {
                    targetAlliance.kingdoms_hashset.Add(kingdom);
                }
                catch { }
                return;
            }

            if (currentAlliance != null)
            {
                RemoveKingdomFromNativeAlliance(
                    kingdom,
                    currentAlliance
                );
            }

            try
            {
                // Let the kingdom update its own alliance state first. Adding
                // it to the native hashset before allianceJoin() can leave a
                // ghost member behind if WorldBox rejects a stale/half-loaded
                // kingdom reference. Alliance.checkActive() then repeatedly
                // tries to remove that invalid member.
                kingdom.allianceJoin(targetAlliance);
                targetAlliance.kingdoms_hashset.Add(kingdom);
                targetAlliance.recalculate();
                targetAlliance.data.timestamp_member_joined =
                    World.world.getCurWorldTime();
            }
            catch (Exception exception)
            {
                try
                {
                    targetAlliance.kingdoms_hashset.Remove(kingdom);
                }
                catch
                {
                }

                LogWarning(
                    "Native alliance join failed for " +
                    GetWorldObjectDisplayName(kingdom) + ": " +
                    exception.Message
                );
            }
        }

        private static void RemoveKingdomFromNativeAlliance(
            Kingdom kingdom,
            Alliance alliance
        )
        {
            if (
                kingdom == null ||
                alliance == null ||
                !IsKingdomInCurrentWorld(kingdom)
            )
            {
                return;
            }

            try
            {
                Alliance current = kingdom.getAlliance();
                if (
                    current == null ||
                    !Alliance.isSame(current, alliance)
                )
                {
                    return;
                }

                kingdom.allianceLeave(alliance);
                alliance.kingdoms_hashset.Remove(kingdom);
                alliance.recalculate();

                if (
                    World.world != null &&
                    World.world.alliances != null &&
                    alliance.kingdoms_hashset.Count < 2
                )
                {
                    World.world.alliances.dissolveAlliance(alliance);
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Native alliance leave failed for " +
                    GetWorldObjectDisplayName(kingdom) + ": " +
                    exception.Message
                );
            }
        }
    }
}
