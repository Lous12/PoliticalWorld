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
        // Step 9D FAST: ideology simulation, registry, behavior profiles, tree evolution and current resolution moved unchanged from the runtime-confirmed Step 9C build.
        private static void UpdateIdeologySystem()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int kingdomIndex = 0;
                kingdomIndex < kingdoms.Count;
                kingdomIndex++)
            {
                Kingdom kingdom = kingdoms[kingdomIndex];

                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                string stateIdeology = GetStateIdeology(kingdom);

                if (!IsValidIdeology(stateIdeology))
                {
                    Actor ruler = GetLivingRuler(kingdom);
                    stateIdeology = GetInitialStateIdeology(
                        kingdom,
                        ruler,
                        GetKingdomCourse(kingdom)
                    );
                    SetStateIdeology(kingdom, stateIdeology);
                }

                UpdateIdeologyEvolutionPressures(
                    kingdom,
                    stateIdeology
                );

                UpdateKingdomIdeologyCurrent(
                    kingdom,
                    stateIdeology
                );

                List<City> cities = GetCitiesSafe(kingdom);

                for (int cityIndex = 0;
                    cityIndex < cities.Count;
                    cityIndex++)
                {
                    ProcessCityIdeologies(
                        cities[cityIndex],
                        kingdom,
                        stateIdeology
                    );
                }
            }
        }

        private static void ProcessCityIdeologies(
            City city,
            Kingdom kingdom,
            string stateIdeology
        )
        {
            if (city == null || city.data == null)
            {
                return;
            }

            List<Actor> units = GetCityUnitsSafe(city);

            if (units.Count == 0)
            {
                return;
            }

            EnsureAndUpdateCityPoliticalMemory(
                city,
                kingdom,
                stateIdeology
            );

            string dominantIdeology;
            int dominantSupport;
            int ideologicalTension;

            GetCityIdeologyOverview(
                city,
                out dominantIdeology,
                out dominantSupport,
                out ideologicalTension
            );

            int localStability = GetLocalStability(city);
            int nationalStability = GetNationalStability(kingdom);
            int stateSupport = IsValidIdeology(stateIdeology)
                ? GetCityIdeologySupport(city, stateIdeology)
                : 0;
            string stateCurrent = GetStateIdeologyCurrent(kingdom);
            string cityCurrent = GetCityIdeologyCurrent(
                city,
                dominantIdeology
            );
            float cityCurrentDiffusion =
                GetIdeologyCurrentDiffusionMultiplier(cityCurrent);

            string neighbourIdeology;
            int neighbourSupport;
            GetStrongestNeighbourIdeology(
                city,
                out neighbourIdeology,
                out neighbourSupport
            );

            int cursor = 0;

            try
            {
                city.data.get(
                    IdeologyCursorDataKey,
                    out cursor,
                    0
                );
            }
            catch
            {
                cursor = 0;
            }

            if (cursor < 0 || cursor >= units.Count)
            {
                cursor = 0;
            }

            int amount = Math.Min(
                IdeologyActorsPerCityTick,
                units.Count
            );

            for (int offset = 0; offset < amount; offset++)
            {
                int index = (cursor + offset) % units.Count;
                Actor actor = units[index];

                if (
                    actor == null ||
                    actor.data == null ||
                    !actor.isAlive()
                )
                {
                    continue;
                }

                string current = GetCitizenIdeology(actor);

                if (!IsValidIdeology(current))
                {
                    string initial = PickInitialCitizenIdeology(
                        city,
                        kingdom,
                        stateIdeology
                    );
                    SetCitizenIdeology(actor, initial);
                    SetCitizenIdeologyConviction(
                        actor,
                        UnityEngine.Random.Range(40, 71)
                    );
                    continue;
                }

                int conviction = GetCitizenIdeologyConviction(actor);
                float resistance = Math.Max(
                    0.20f,
                    1f - conviction / 125f
                );

                // A deep local tradition makes established beliefs harder to
                // dislodge, without creating a second ideology percentage.
                int currentMemory = GetCityPoliticalMemory(
                    city,
                    current
                );
                resistance *= Math.Max(
                    0.72f,
                    1f - currentMemory / 400f
                );

                string candidate = null;
                float bestPressure = 0f;

                if (
                    IsValidIdeology(stateIdeology) &&
                    stateIdeology != current
                )
                {
                    float statePressure =
                        0.008f +
                        nationalStability / 100f * 0.025f +
                        stateSupport / 100f * 0.018f;

                    if (nationalStability < 30)
                    {
                        statePressure *= 0.45f;
                    }

                    statePressure *= GetIdeologySimilarity(
                        current,
                        stateIdeology
                    );
                    statePressure *= GetIdeologyCurrentDiffusionMultiplier(
                        stateCurrent
                    );

                    ConsiderIdeologyPressure(
                        ref candidate,
                        ref bestPressure,
                        stateIdeology,
                        statePressure
                    );
                }

                if (
                    IsValidIdeology(dominantIdeology) &&
                    dominantIdeology != current &&
                    dominantSupport >= 35
                )
                {
                    float localPressure =
                        0.010f +
                        dominantSupport / 100f * 0.040f;

                    if (localStability < 35)
                    {
                        localPressure *= 1.35f;
                    }

                    localPressure *= GetIdeologySimilarity(
                        current,
                        dominantIdeology
                    );
                    localPressure *= cityCurrentDiffusion;

                    ConsiderIdeologyPressure(
                        ref candidate,
                        ref bestPressure,
                        dominantIdeology,
                        localPressure
                    );
                }

                if (
                    IsValidIdeology(neighbourIdeology) &&
                    neighbourIdeology != current &&
                    neighbourSupport >= 55
                )
                {
                    float neighbourPressure =
                        0.004f +
                        neighbourSupport / 100f * 0.020f;

                    neighbourPressure *= GetIdeologySimilarity(
                        current,
                        neighbourIdeology
                    );

                    ConsiderIdeologyPressure(
                        ref candidate,
                        ref bestPressure,
                        neighbourIdeology,
                        neighbourPressure
                    );
                }

                if (
                    IsValidIdeology(candidate) &&
                    candidate != current
                )
                {
                    int candidateMemory = GetCityPoliticalMemory(
                        city,
                        candidate
                    );
                    int memoryDelta = candidateMemory - currentMemory;

                    if (memoryDelta > 0)
                    {
                        bestPressure +=
                            Math.Min(0.006f, memoryDelta / 100f * 0.006f);
                    }
                    else if (memoryDelta < 0)
                    {
                        bestPressure *= Math.Max(
                            0.88f,
                            1f + memoryDelta / 100f * 0.12f
                        );
                    }

                    float switchChance = bestPressure * resistance;

                    if (UnityEngine.Random.value < switchChance)
                    {
                        SetCitizenIdeology(actor, candidate);
                        SetCitizenIdeologyConviction(
                            actor,
                            UnityEngine.Random.Range(30, 51)
                        );
                        continue;
                    }

                    if (bestPressure >= 0.018f)
                    {
                        AdjustCitizenIdeologyConviction(actor, -1);
                    }
                }
                else if (
                    current == dominantIdeology ||
                    current == stateIdeology
                )
                {
                    AdjustCitizenIdeologyConviction(actor, 1);
                }

                // Во время серьёзного кризиса часть жителей начинает
                // искать соседнее по политическим координатам течение.
                if (
                    localStability < 30 &&
                    UnityEngine.Random.value < 0.0035f * resistance
                )
                {
                    string drift = PickSimilarIdeology(current);

                    if (
                        IsValidIdeology(drift) &&
                        drift != current
                    )
                    {
                        SetCitizenIdeology(actor, drift);
                        SetCitizenIdeologyConviction(
                            actor,
                            UnityEngine.Random.Range(25, 46)
                        );
                    }
                }
            }

            cursor = (cursor + amount) % units.Count;

            try
            {
                city.data.set(
                    IdeologyCursorDataKey,
                    cursor
                );
            }
            catch
            {
            }
        }


        private static void ConsiderIdeologyPressure(
            ref string candidate,
            ref float bestPressure,
            string ideology,
            float pressure
        )
        {
            if (
                !IsValidIdeology(ideology) ||
                pressure <= bestPressure
            )
            {
                return;
            }

            candidate = ideology;
            bestPressure = pressure;
        }

        private static void GetStrongestNeighbourIdeology(
            City city,
            out string ideology,
            out int support
        )
        {
            ideology = null;
            support = 0;

            List<City> neighbours = GetNeighbourCitiesSafe(city);

            for (int i = 0; i < neighbours.Count; i++)
            {
                City neighbour = neighbours[i];

                if (neighbour == null || neighbour == city)
                {
                    continue;
                }

                string neighbourDominant;
                int neighbourDominantSupport;
                int neighbourTension;

                GetCityIdeologyOverview(
                    neighbour,
                    out neighbourDominant,
                    out neighbourDominantSupport,
                    out neighbourTension
                );

                if (
                    IsValidIdeology(neighbourDominant) &&
                    neighbourDominantSupport > support
                )
                {
                    ideology = neighbourDominant;
                    support = neighbourDominantSupport;
                }
            }
        }

        private static string PickInitialCitizenIdeology(
            City city,
            Kingdom kingdom,
            string stateIdeology
        )
        {
            string memoryIdeology;
            int memoryStrength;
            GetDominantCityPoliticalMemory(
                city,
                out memoryIdeology,
                out memoryStrength
            );

            if (
                IsValidIdeology(memoryIdeology) &&
                memoryStrength >= 45
            )
            {
                float memoryChance = Math.Min(
                    0.36f,
                    0.16f +
                    (memoryStrength - 45) / 220f
                );

                if (UnityEngine.Random.value < memoryChance)
                {
                    return memoryIdeology;
                }
            }

            if (!IsValidIdeology(stateIdeology))
            {
                return ScenarioBridge.PickRandomSeedIdeology();
            }

            float roll = UnityEngine.Random.value;

            if (roll < 0.55f)
            {
                return stateIdeology;
            }

            if (roll < 0.82f)
            {
                string similar = PickSimilarIdeology(
                    stateIdeology
                );

                if (IsValidIdeology(similar))
                {
                    return similar;
                }
            }

            return ScenarioBridge.PickRandomSeedIdeology();
        }

        private static string PickSimilarIdeology(
            string ideology
        )
        {
            string[] choices;

            switch (ideology)
            {
                case MonarchismIdeologyId:
                    choices = new string[]
                    {
                        ConservatismIdeologyId,
                        FascismIdeologyId,
                        LiberalismIdeologyId
                    };
                    break;
                case ConservatismIdeologyId:
                    choices = new string[]
                    {
                        MonarchismIdeologyId,
                        LiberalismIdeologyId,
                        DemocracyIdeologyId
                    };
                    break;
                case LiberalismIdeologyId:
                    choices = new string[]
                    {
                        DemocracyIdeologyId,
                        ConservatismIdeologyId,
                        SocialismIdeologyId
                    };
                    break;
                case DemocracyIdeologyId:
                    choices = new string[]
                    {
                        LiberalismIdeologyId,
                        SocialismIdeologyId,
                        ConservatismIdeologyId
                    };
                    break;
                case SocialismIdeologyId:
                    choices = new string[]
                    {
                        CommunismIdeologyId,
                        SyndicalismIdeologyId,
                        DemocracyIdeologyId
                    };
                    break;
                case CommunismIdeologyId:
                    choices = new string[]
                    {
                        SocialismIdeologyId,
                        SyndicalismIdeologyId,
                        AnarchismIdeologyId
                    };
                    break;
                case FascismIdeologyId:
                    choices = new string[]
                    {
                        MonarchismIdeologyId,
                        ConservatismIdeologyId,
                        LiberalismIdeologyId
                    };
                    break;
                case AnarchismIdeologyId:
                    choices = new string[]
                    {
                        SyndicalismIdeologyId,
                        SocialismIdeologyId,
                        CommunismIdeologyId
                    };
                    break;
                case SyndicalismIdeologyId:
                    choices = new string[]
                    {
                        SocialismIdeologyId,
                        AnarchismIdeologyId,
                        CommunismIdeologyId
                    };
                    break;
                default:
                    return stateFallbackIdeology();
            }

            string picked = choices[
                UnityEngine.Random.Range(0, choices.Length)
            ];

            if (!IsValidIdeology(picked))
            {
                return ConservatismIdeologyId;
            }

            return picked;
        }

        private static string stateFallbackIdeology()
        {
            return ConservatismIdeologyId;
        }

        private static float GetIdeologySimilarity(
            string first,
            string second
        )
        {
            if (first == second)
            {
                return 1f;
            }

            float firstX;
            float firstY;
            float secondX;
            float secondY;

            GetIdeologyCoordinates(
                first,
                out firstX,
                out firstY
            );
            GetIdeologyCoordinates(
                second,
                out secondX,
                out secondY
            );

            float dx = firstX - secondX;
            float dy = firstY - secondY;
            float distance = (float)Math.Sqrt(
                dx * dx + dy * dy
            );

            return Math.Max(
                0.12f,
                1f - distance / 2.8f
            );
        }

        private static void GetIdeologyCoordinates(
            string ideology,
            out float economic,
            out float authority
        )
        {
            economic = 0f;
            authority = 0f;

            switch (ideology)
            {
                case MonarchismIdeologyId:
                    economic = 0.15f;
                    authority = 0.75f;
                    return;
                case ConservatismIdeologyId:
                    economic = 0.35f;
                    authority = 0.30f;
                    return;
                case LiberalismIdeologyId:
                    economic = 0.55f;
                    authority = -0.35f;
                    return;
                case DemocracyIdeologyId:
                    economic = 0.05f;
                    authority = -0.20f;
                    return;
                case SocialismIdeologyId:
                    economic = -0.55f;
                    authority = -0.10f;
                    return;
                case CommunismIdeologyId:
                    economic = -0.90f;
                    authority = 0.55f;
                    return;
                case FascismIdeologyId:
                    economic = 0.45f;
                    authority = 0.95f;
                    return;
                case AnarchismIdeologyId:
                    economic = -0.55f;
                    authority = -0.95f;
                    return;
                case SyndicalismIdeologyId:
                    economic = -0.80f;
                    authority = -0.60f;
                    return;
            }
        }

        // -----------------------------------------------------------------
        // Foundation Step 9C FAST: Politics Core moved into dedicated partial
        // modules under Politics/ and Core/. Runtime behavior is unchanged.
        // -----------------------------------------------------------------

        private static void EnsureIdeologyRegistry()
        {
            if (_ideologyRegistryInitialized)
            {
                return;
            }

            IdeologyNodeRegistry.Clear();
            IdeologyChildrenRegistry.Clear();
            IdeologyBehaviorRegistry.Clear();

            RegisterIdeologyRoot(MonarchismIdeologyId, "monarchy", "hierarchy");
            RegisterIdeologyRoot(ConservatismIdeologyId, "tradition", "order");
            RegisterIdeologyRoot(LiberalismIdeologyId, "liberty", "market");
            RegisterIdeologyRoot(DemocracyIdeologyId, "democracy", "pluralism");
            RegisterIdeologyRoot(SocialismIdeologyId, "equality", "social");
            RegisterIdeologyRoot(CommunismIdeologyId, "communist", "collective");
            RegisterIdeologyRoot(FascismIdeologyId, "authoritarian", "nationalist");
            RegisterIdeologyRoot(AnarchismIdeologyId, "stateless", "decentralized");
            RegisterIdeologyRoot(SyndicalismIdeologyId, "workers", "syndicates");

            // MONARCHISM
            RegisterIdeologyCurrent(AbsoluteMonarchyCurrentId, MonarchismIdeologyId, 3, 55, -4, 1f, "monarchy", "centralized", "hereditary");
            RegisterIdeologyCurrent(EnlightenedAbsolutismCurrentId, AbsoluteMonarchyCurrentId, 4, 50, -2, 0.95f, "monarchy", "reformist", "centralized");
            RegisterIdeologyCurrent(AutocraticMonarchismCurrentId, AbsoluteMonarchyCurrentId, 2, 60, -5, 1.05f, "monarchy", "autocratic", "centralized");
            RegisterIdeologyCurrent(PatrimonialMonarchyCurrentId, AbsoluteMonarchyCurrentId, 3, 55, -3, 0.95f, "monarchy", "traditional", "hereditary");
            RegisterIdeologyCurrent(ConstitutionalMonarchyCurrentId, MonarchismIdeologyId, 4, -1, 4, 0.95f, "monarchy", "constitutional", "pluralism");
            RegisterIdeologyCurrent(ParliamentaryMonarchyCurrentId, ConstitutionalMonarchyCurrentId, 5, -1, 5, 0.92f, "monarchy", "parliamentary", "pluralism");
            RegisterIdeologyCurrent(DualistMonarchyCurrentId, ConstitutionalMonarchyCurrentId, 3, -1, 3, 1f, "monarchy", "constitutional", "executive");
            RegisterIdeologyCurrent(ElectiveMonarchyCurrentId, MonarchismIdeologyId, 2, -1, 2, 1f, "monarchy", "elective");
            RegisterIdeologyCurrent(AristocraticElectiveMonarchyCurrentId, ElectiveMonarchyCurrentId, 2, -1, 2, 0.95f, "monarchy", "elective", "elite");
            RegisterIdeologyCurrent(PopularElectiveMonarchyCurrentId, ElectiveMonarchyCurrentId, 3, -1, 3, 1f, "monarchy", "elective", "participatory");

            // CONSERVATISM
            RegisterIdeologyCurrent(TraditionalismCurrentId, ConservatismIdeologyId, 3, 50, 0, 1f, "tradition", "order");
            RegisterIdeologyCurrent(ReactionaryConservatismCurrentId, TraditionalismCurrentId, 1, 55, -3, 1.05f, "conservative", "reactionary", "tradition");
            RegisterIdeologyCurrent(PaternalisticConservatismCurrentId, TraditionalismCurrentId, 4, 45, 1, 0.95f, "conservative", "paternalist", "social");
            RegisterIdeologyCurrent(NationalConservatismCurrentId, TraditionalismCurrentId, 2, 55, -2, 1.05f, "conservative", "nationalist", "order");
            RegisterIdeologyCurrent(LiberalConservatismCurrentId, ConservatismIdeologyId, 3, -1, 3, 1f, "conservative", "liberal", "pluralism");
            RegisterIdeologyCurrent(FiscalConservatismCurrentId, LiberalConservatismCurrentId, 3, -1, 3, 0.95f, "conservative", "market", "liberal");
            RegisterIdeologyCurrent(ProgressiveConservatismCurrentId, LiberalConservatismCurrentId, 4, -1, 4, 0.95f, "conservative", "reformist", "social");
            RegisterIdeologyCurrent(AuthoritarianConservatismCurrentId, ConservatismIdeologyId, 1, 60, -5, 1.08f, "conservative", "authoritarian", "order");
            RegisterIdeologyCurrent(OrderConservatismCurrentId, AuthoritarianConservatismCurrentId, 2, 55, -4, 1.02f, "conservative", "authoritarian", "stability");

            // LIBERALISM
            RegisterIdeologyCurrent(ClassicalLiberalismCurrentId, LiberalismIdeologyId, 2, -1, 2, 1f, "liberty", "market");
            RegisterIdeologyCurrent(EconomicLiberalismCurrentId, ClassicalLiberalismCurrentId, 2, -1, 2, 1f, "liberal", "market", "economic");
            RegisterIdeologyCurrent(OrdoliberalismCurrentId, ClassicalLiberalismCurrentId, 3, -1, 3, 0.95f, "liberal", "market", "order");
            RegisterIdeologyCurrent(LibertarianismCurrentId, ClassicalLiberalismCurrentId, 0, -1, 0, 1.05f, "liberal", "libertarian", "decentralized");
            RegisterIdeologyCurrent(MinarchismCurrentId, LibertarianismCurrentId, -1, -1, -1, 1.05f, "liberal", "libertarian", "minimal-state");
            RegisterIdeologyCurrent(SocialLiberalismCurrentId, LiberalismIdeologyId, 4, -1, 4, 1f, "liberty", "social", "pluralism");
            RegisterIdeologyCurrent(ProgressiveLiberalismCurrentId, SocialLiberalismCurrentId, 4, -1, 4, 1f, "liberal", "progressive", "pluralism");
            RegisterIdeologyCurrent(WelfareLiberalismCurrentId, SocialLiberalismCurrentId, 5, -1, 5, 0.95f, "liberal", "social", "welfare");
            RegisterIdeologyCurrent(NationalLiberalismCurrentId, LiberalismIdeologyId, 2, -1, 2, 1f, "liberal", "national", "market");
            RegisterIdeologyCurrent(CivicNationalLiberalismCurrentId, NationalLiberalismCurrentId, 3, -1, 3, 0.98f, "liberal", "civic", "national");

            // DEMOCRACY
            RegisterIdeologyCurrent(ParliamentaryDemocracyCurrentId, DemocracyIdeologyId, 5, -1, 5, 0.95f, "democracy", "parliamentary", "pluralism");
            RegisterIdeologyCurrent(LiberalDemocracyCurrentId, ParliamentaryDemocracyCurrentId, 5, -1, 5, 0.95f, "democracy", "liberal", "pluralism");
            RegisterIdeologyCurrent(ConsensusDemocracyCurrentId, ParliamentaryDemocracyCurrentId, 6, -1, 6, 0.90f, "democracy", "consensus", "pluralism");
            RegisterIdeologyCurrent(RadicalDemocracyCurrentId, DemocracyIdeologyId, -2, -1, -2, 1f, "democracy", "radical", "participatory");
            RegisterIdeologyCurrent(DirectDemocracyCurrentId, RadicalDemocracyCurrentId, -1, -1, -1, 1f, "democracy", "direct", "participatory");
            RegisterIdeologyCurrent(ParticipatoryDemocracyCurrentId, RadicalDemocracyCurrentId, 1, -1, 1, 1f, "democracy", "participatory", "pluralism");
            RegisterIdeologyCurrent(PresidentialDemocracyCurrentId, DemocracyIdeologyId, 3, -1, 3, 1f, "democracy", "presidential", "executive");
            RegisterIdeologyCurrent(ConstitutionalPresidentialismCurrentId, PresidentialDemocracyCurrentId, 4, -1, 4, 0.98f, "democracy", "presidential", "constitutional");
            RegisterIdeologyCurrent(CouncilDemocracyCurrentId, DemocracyIdeologyId, 2, -1, 2, 1f, "democracy", "councils", "decentralized");
            RegisterIdeologyCurrent(DelegativeCouncilDemocracyCurrentId, CouncilDemocracyCurrentId, 2, -1, 2, 1f, "democracy", "councils", "delegative");

            // SOCIALISM
            RegisterIdeologyCurrent(SocialDemocracyCurrentId, SocialismIdeologyId, 5, -1, 5, 0.95f, "social", "democracy", "reformist");
            RegisterIdeologyCurrent(ModerateSocialDemocracyCurrentId, SocialDemocracyCurrentId, 5, -1, 5, 0.92f, "socialist", "reformist", "moderate");
            RegisterIdeologyCurrent(LeftSocialDemocracyCurrentId, SocialDemocracyCurrentId, 4, -1, 4, 0.98f, "socialist", "reformist", "left");
            RegisterIdeologyCurrent(DemocraticSocialismCurrentId, SocialismIdeologyId, 3, -1, 3, 1f, "socialist", "democracy");
            RegisterIdeologyCurrent(MarketSocialismCurrentId, DemocraticSocialismCurrentId, 3, -1, 3, 0.98f, "socialist", "market", "democracy");
            RegisterIdeologyCurrent(LibertarianSocialismCurrentId, DemocraticSocialismCurrentId, 1, -1, 1, 1.05f, "socialist", "libertarian", "decentralized");
            RegisterIdeologyCurrent(GuildSocialismCurrentId, DemocraticSocialismCurrentId, 3, -1, 3, 1f, "socialist", "guilds", "workers");
            RegisterIdeologyCurrent(CooperativeSocialismCurrentId, DemocraticSocialismCurrentId, 4, -1, 4, 0.95f, "socialist", "cooperative", "workers");
            RegisterIdeologyCurrent(RevolutionarySocialismCurrentId, SocialismIdeologyId, -6, -1, -6, 1.30f, "socialist", "revolutionary");
            RegisterIdeologyCurrent(MarxistSocialismCurrentId, RevolutionarySocialismCurrentId, -4, -1, -4, 1.20f, "socialist", "marxist", "revolutionary");
            RegisterIdeologyCurrent(RevolutionaryDemocraticSocialismCurrentId, RevolutionarySocialismCurrentId, -2, -1, -2, 1.15f, "socialist", "revolutionary", "democratic");
            RegisterIdeologyCurrent(UtopianSocialismCurrentId, SocialismIdeologyId, 1, -1, 1, 1f, "socialist", "utopian", "communal");

            // COMMUNISM
            RegisterIdeologyCurrent(CentralistCommunismCurrentId, CommunismIdeologyId, 3, 60, -5, 1.25f, "communist", "centralized", "party-state");
            RegisterIdeologyCurrent(MarxismLeninismCurrentId, CentralistCommunismCurrentId, 2, 60, -5, 1.20f, "communist", "marxist-leninist", "party-state");
            RegisterIdeologyCurrent(OrthodoxMarxismLeninismCurrentId, MarxismLeninismCurrentId, 2, 65, -5, 1.18f, "communist", "orthodox", "party-state");
            RegisterIdeologyCurrent(ReformistMarxismLeninismCurrentId, MarxismLeninismCurrentId, 3, 50, -2, 1.05f, "communist", "reformist", "party-state");
            RegisterIdeologyCurrent(EurocommunismCurrentId, ReformistMarxismLeninismCurrentId, 4, -1, 4, 0.95f, "communist", "reformist", "pluralism");
            RegisterIdeologyCurrent(NationalCommunismCurrentId, MarxismLeninismCurrentId, 1, 60, -5, 1.18f, "communist", "national", "centralized");
            RegisterIdeologyCurrent(MaoismCurrentId, MarxismLeninismCurrentId, -1, 60, -6, 1.25f, "communist", "revolutionary", "militarist");
            RegisterIdeologyCurrent(PartyCommunismCurrentId, CentralistCommunismCurrentId, 3, 55, -4, 1.15f, "communist", "party-state", "centralized");
            RegisterIdeologyCurrent(CouncilCommunismCurrentId, CommunismIdeologyId, 1, -1, 1, 1f, "communist", "councils", "decentralized");
            RegisterIdeologyCurrent(WorkersCouncilCommunismCurrentId, CouncilCommunismCurrentId, 2, -1, 2, 0.98f, "communist", "councils", "workers");
            RegisterIdeologyCurrent(LeftCommunismCurrentId, CouncilCommunismCurrentId, 0, -1, 0, 1.05f, "communist", "left-communist", "decentralized");
            RegisterIdeologyCurrent(LibertarianCommunismCurrentId, CommunismIdeologyId, 0, -1, 0, 1.05f, "communist", "libertarian", "decentralized");
            RegisterIdeologyCurrent(CommunalCommunismCurrentId, LibertarianCommunismCurrentId, 1, -1, 1, 1.02f, "communist", "communal", "decentralized");

            // FASCISM
            RegisterIdeologyCurrent(CorporatistFascismCurrentId, FascismIdeologyId, 2, 60, -4, 1f, "fascist", "corporatist", "authoritarian");
            RegisterIdeologyCurrent(StateCorporatismCurrentId, CorporatistFascismCurrentId, 2, 60, -4, 1f, "fascist", "corporatist", "state");
            RegisterIdeologyCurrent(ClericalFascismCurrentId, CorporatistFascismCurrentId, 2, 60, -4, 0.98f, "fascist", "clerical", "traditional");
            RegisterIdeologyCurrent(FalangismCurrentId, CorporatistFascismCurrentId, 0, 60, -5, 1.08f, "fascist", "falangist", "syndicalist");
            RegisterIdeologyCurrent(RadicalFascismCurrentId, FascismIdeologyId, -5, -1, -5, 1.30f, "fascist", "radical", "militarist");
            RegisterIdeologyCurrent(NationalSocialismCurrentId, RadicalFascismCurrentId, -5, -1, -6, 1.25f, "fascist", "national-socialist", "totalitarian");
            RegisterIdeologyCurrent(TotalitarianFascismCurrentId, RadicalFascismCurrentId, -6, -1, -6, 1.30f, "fascist", "totalitarian", "militarist");
            RegisterIdeologyCurrent(IntegralFascismCurrentId, RadicalFascismCurrentId, -3, -1, -5, 1.18f, "fascist", "integral", "authoritarian");
            RegisterIdeologyCurrent(NationalSyndicalismFascistCurrentId, FascismIdeologyId, -1, 55, -5, 1.15f, "fascist", "national-syndicalist", "workers");

            // ANARCHISM
            RegisterIdeologyCurrent(IndividualistAnarchismCurrentId, AnarchismIdeologyId, -3, -1, -3, 1f, "stateless", "individualist", "decentralized");
            RegisterIdeologyCurrent(EgoistAnarchismCurrentId, IndividualistAnarchismCurrentId, -3, -1, -3, 1f, "stateless", "individualist", "egoist");
            RegisterIdeologyCurrent(MutualismCurrentId, IndividualistAnarchismCurrentId, 0, -1, 0, 1f, "stateless", "mutualist", "market");
            RegisterIdeologyCurrent(SocialAnarchismCurrentId, AnarchismIdeologyId, -2, -1, -2, 1.08f, "stateless", "social", "communal");
            RegisterIdeologyCurrent(AnarchoCommunismCurrentId, SocialAnarchismCurrentId, -4, -1, -4, 1.15f, "stateless", "communist", "communal");
            RegisterIdeologyCurrent(AnarchoSyndicalismCurrentId, SocialAnarchismCurrentId, -4, -1, -4, 1.20f, "stateless", "syndicalist", "workers");
            RegisterIdeologyCurrent(CollectivistAnarchismCurrentId, SocialAnarchismCurrentId, -2, -1, -2, 1.10f, "stateless", "collectivist", "communal");
            RegisterIdeologyCurrent(MarketAnarchismCurrentId, AnarchismIdeologyId, -2, -1, -2, 1.05f, "stateless", "market", "individualist");
            RegisterIdeologyCurrent(AnarchoCapitalismCurrentId, MarketAnarchismCurrentId, -3, -1, -3, 1.05f, "stateless", "market", "private-property");
            RegisterIdeologyCurrent(AgorismCurrentId, MarketAnarchismCurrentId, -2, -1, -2, 1.08f, "stateless", "market", "counter-economy");
            RegisterIdeologyCurrent(LeftMarketAnarchismCurrentId, MarketAnarchismCurrentId, -1, -1, -1, 1.05f, "stateless", "market", "left");
            RegisterIdeologyCurrent(GreenAnarchismCurrentId, AnarchismIdeologyId, -2, -1, -2, 1f, "stateless", "green", "decentralized");
            RegisterIdeologyCurrent(EcoAnarchismCurrentId, GreenAnarchismCurrentId, -1, -1, -1, 0.98f, "stateless", "green", "communal");
            RegisterIdeologyCurrent(AnarchoPrimitivismCurrentId, GreenAnarchismCurrentId, -4, -1, -4, 0.92f, "stateless", "green", "primitivist");
            RegisterIdeologyCurrent(AnarchoPacifismCurrentId, AnarchismIdeologyId, 1, -1, 1, 0.90f, "stateless", "pacifist", "decentralized");
            RegisterIdeologyCurrent(InsurrectionaryAnarchismCurrentId, AnarchismIdeologyId, -6, -1, -6, 1.28f, "stateless", "insurrectionary", "revolutionary");

            // SYNDICALISM
            RegisterIdeologyCurrent(IndustrialSyndicalismCurrentId, SyndicalismIdeologyId, 2, -1, 2, 1f, "syndicalist", "industrial", "workers");
            RegisterIdeologyCurrent(ReformistSyndicalismCurrentId, IndustrialSyndicalismCurrentId, 3, -1, 3, 0.95f, "syndicalist", "reformist", "workers");
            RegisterIdeologyCurrent(GuildSyndicalismCurrentId, IndustrialSyndicalismCurrentId, 3, -1, 3, 0.98f, "syndicalist", "guilds", "workers");
            RegisterIdeologyCurrent(CooperativeSyndicalismCurrentId, IndustrialSyndicalismCurrentId, 4, -1, 4, 0.95f, "syndicalist", "cooperative", "workers");
            RegisterIdeologyCurrent(RevolutionarySyndicalismCurrentId, SyndicalismIdeologyId, -5, -1, -5, 1.25f, "syndicalist", "revolutionary", "workers");
            RegisterIdeologyCurrent(RevolutionaryUnionismCurrentId, RevolutionarySyndicalismCurrentId, -4, -1, -4, 1.20f, "syndicalist", "revolutionary", "unionist");
            RegisterIdeologyCurrent(CouncilSyndicalismCurrentId, RevolutionarySyndicalismCurrentId, -2, -1, -2, 1.12f, "syndicalist", "councils", "decentralized");
            RegisterIdeologyCurrent(DemocraticSyndicalismCurrentId, SyndicalismIdeologyId, 3, -1, 3, 0.98f, "syndicalist", "democratic", "workers");
            RegisterIdeologyCurrent(ParliamentarySyndicalismCurrentId, DemocraticSyndicalismCurrentId, 4, -1, 4, 0.95f, "syndicalist", "parliamentary", "workers");
            RegisterIdeologyCurrent(CooperativeCommonwealthCurrentId, DemocraticSyndicalismCurrentId, 5, -1, 5, 0.92f, "syndicalist", "cooperative", "democratic");

            _ideologyRegistryInitialized = true;
        }

        private static void RegisterIdeologyRoot(
            string id,
            params string[] tags
        )
        {
            RegisterIdeologyNode(
                id,
                "",
                id,
                0,
                -1,
                0,
                1f,
                tags
            );
        }

        private static void RegisterIdeologyCurrent(
            string id,
            string parentId,
            int highSupportStability,
            int supportThreshold,
            int lowSupportStability,
            float diffusionMultiplier,
            params string[] tags
        )
        {
            RegisterIdeologyNode(
                id,
                parentId,
                id,
                highSupportStability,
                supportThreshold,
                lowSupportStability,
                diffusionMultiplier,
                tags
            );
        }

        private static void RegisterIdeologyNode(
            string id,
            string parentId,
            string nameKey,
            int highSupportStability,
            int supportThreshold,
            int lowSupportStability,
            float diffusionMultiplier,
            params string[] tags
        )
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            string rootId = id;
            int tier = 0;

            if (!string.IsNullOrEmpty(parentId))
            {
                IdeologyNode parent;
                if (!IdeologyNodeRegistry.TryGetValue(parentId, out parent))
                {
                    LogWarning(
                        "Ideology Framework 2.0: parent node is missing for " +
                        id + ": " + parentId
                    );
                    return;
                }

                rootId = parent.RootIdeologyId;
                tier = parent.Tier + 1;
            }

            IdeologyNode node = new IdeologyNode();
            node.Id = id;
            node.ParentId = parentId ?? "";
            node.RootIdeologyId = rootId;
            node.Tier = tier;
            node.NameKey = string.IsNullOrEmpty(nameKey) ? id : nameKey;
            node.HighSupportStability = highSupportStability;
            node.StabilitySupportThreshold = supportThreshold;
            node.LowSupportStability = lowSupportStability;
            node.DiffusionMultiplier = diffusionMultiplier <= 0f
                ? 1f
                : diffusionMultiplier;
            node.Tags = tags ?? new string[0];

            IdeologyNodeRegistry[id] = node;

            if (!string.IsNullOrEmpty(parentId))
            {
                List<string> children;
                if (!IdeologyChildrenRegistry.TryGetValue(parentId, out children))
                {
                    children = new List<string>();
                    IdeologyChildrenRegistry[parentId] = children;
                }

                if (!children.Contains(id))
                {
                    children.Add(id);
                }
            }
        }

        private static IdeologyBehaviorProfile GetIdeologyBehaviorProfile(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return CreateDefaultIdeologyBehaviorProfile();
            }

            string nodeId = GetStateIdeologyCurrent(kingdom);
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = GetStateIdeology(kingdom);
            }

            return GetIdeologyBehaviorProfile(nodeId);
        }

        private static IdeologyBehaviorProfile GetIdeologyBehaviorProfile(
            string nodeId
        )
        {
            EnsureIdeologyRegistry();

            if (string.IsNullOrEmpty(nodeId))
            {
                return CreateDefaultIdeologyBehaviorProfile();
            }

            IdeologyBehaviorProfile cached;
            if (IdeologyBehaviorRegistry.TryGetValue(nodeId, out cached))
            {
                return cached;
            }

            IdeologyNode node;
            if (!IdeologyNodeRegistry.TryGetValue(nodeId, out node) || node == null)
            {
                return CreateDefaultIdeologyBehaviorProfile();
            }

            IdeologyBehaviorProfile profile =
                CreateRootIdeologyBehaviorProfile(node.RootIdeologyId);
            HashSet<string> inheritedTags = new HashSet<string>();

            IdeologyNode cursor = node;
            int guard = 0;
            while (cursor != null && guard < IdeologyRegistryMaxDepth)
            {
                if (cursor.Tags != null)
                {
                    for (int i = 0; i < cursor.Tags.Length; i++)
                    {
                        string tag = cursor.Tags[i];
                        if (!string.IsNullOrEmpty(tag))
                        {
                            inheritedTags.Add(tag);
                        }
                    }
                }

                if (string.IsNullOrEmpty(cursor.ParentId))
                {
                    break;
                }

                IdeologyNode parent;
                if (!IdeologyNodeRegistry.TryGetValue(cursor.ParentId, out parent))
                {
                    break;
                }
                cursor = parent;
                guard++;
            }

            foreach (string tag in inheritedTags)
            {
                ApplyIdeologyBehaviorTag(profile, tag);
            }

            profile.Market = ClampInt(profile.Market, 0, 100);
            profile.Welfare = ClampInt(profile.Welfare, 0, 100);
            profile.Centralization = ClampInt(profile.Centralization, 0, 100);
            profile.Pluralism = ClampInt(profile.Pluralism, 0, 100);
            profile.Militarism = ClampInt(profile.Militarism, 0, 100);
            profile.Pacifist = inheritedTags.Contains("pacifist");
            profile.Stateless = inheritedTags.Contains("stateless");
            profile.Primitivist = inheritedTags.Contains("primitivist");

            if (profile.Stateless)
            {
                profile.Centralization = Math.Min(profile.Centralization, 8);
            }
            if (profile.Pacifist)
            {
                profile.Militarism = Math.Min(profile.Militarism, 8);
            }

            IdeologyBehaviorRegistry[nodeId] = profile;
            return profile;
        }

        private static IdeologyBehaviorProfile CreateDefaultIdeologyBehaviorProfile()
        {
            IdeologyBehaviorProfile profile = new IdeologyBehaviorProfile();
            profile.Market = 50;
            profile.Welfare = 50;
            profile.Centralization = 50;
            profile.Pluralism = 50;
            profile.Militarism = 50;
            return profile;
        }

        private static IdeologyBehaviorProfile CreateRootIdeologyBehaviorProfile(
            string rootId
        )
        {
            IdeologyBehaviorProfile profile = CreateDefaultIdeologyBehaviorProfile();

            if (rootId == MonarchismIdeologyId)
            {
                profile.Market = 45; profile.Welfare = 35;
                profile.Centralization = 70; profile.Pluralism = 30;
                profile.Militarism = 50;
            }
            else if (rootId == ConservatismIdeologyId)
            {
                profile.Market = 60; profile.Welfare = 35;
                profile.Centralization = 60; profile.Pluralism = 40;
                profile.Militarism = 48;
            }
            else if (rootId == LiberalismIdeologyId)
            {
                profile.Market = 78; profile.Welfare = 35;
                profile.Centralization = 35; profile.Pluralism = 72;
                profile.Militarism = 35;
            }
            else if (rootId == DemocracyIdeologyId)
            {
                profile.Market = 58; profile.Welfare = 50;
                profile.Centralization = 40; profile.Pluralism = 85;
                profile.Militarism = 35;
            }
            else if (rootId == SocialismIdeologyId)
            {
                profile.Market = 40; profile.Welfare = 72;
                profile.Centralization = 50; profile.Pluralism = 68;
                profile.Militarism = 40;
            }
            else if (rootId == CommunismIdeologyId)
            {
                profile.Market = 22; profile.Welfare = 82;
                profile.Centralization = 72; profile.Pluralism = 28;
                profile.Militarism = 50;
            }
            else if (rootId == FascismIdeologyId)
            {
                profile.Market = 35; profile.Welfare = 50;
                profile.Centralization = 88; profile.Pluralism = 8;
                profile.Militarism = 82;
            }
            else if (rootId == AnarchismIdeologyId)
            {
                profile.Market = 52; profile.Welfare = 45;
                profile.Centralization = 5; profile.Pluralism = 82;
                profile.Militarism = 25;
            }
            else if (rootId == SyndicalismIdeologyId)
            {
                profile.Market = 35; profile.Welfare = 70;
                profile.Centralization = 32; profile.Pluralism = 62;
                profile.Militarism = 48;
            }

            return profile;
        }

        private static void ApplyIdeologyBehaviorTag(
            IdeologyBehaviorProfile p,
            string tag
        )
        {
            if (p == null || string.IsNullOrEmpty(tag)) return;

            if (tag == "market") p.Market += 12;
            else if (tag == "economic") p.Market += 8;
            else if (tag == "private-property") { p.Market += 25; p.Welfare -= 15; p.Centralization -= 5; }
            else if (tag == "counter-economy") { p.Market += 15; p.Centralization -= 10; }
            else if (tag == "social" || tag == "equality") p.Welfare += 10;
            else if (tag == "welfare") p.Welfare += 25;
            else if (tag == "paternalist") p.Welfare += 15;
            else if (tag == "cooperative") { p.Welfare += 12; p.Pluralism += 5; }
            else if (tag == "communal") { p.Market -= 10; p.Welfare += 15; p.Centralization -= 10; p.Pluralism += 5; }
            else if (tag == "collective" || tag == "collectivist") { p.Market -= 12; p.Welfare += 12; }
            else if (tag == "communist") { p.Market -= 12; p.Welfare += 10; }
            else if (tag == "socialist") { p.Market -= 8; p.Welfare += 10; }
            else if (tag == "workers") p.Welfare += 8;
            else if (tag == "syndicalist" || tag == "syndicates" || tag == "guilds") { p.Market -= 8; p.Welfare += 10; p.Centralization -= 5; p.Pluralism += 5; }
            else if (tag == "corporatist") { p.Market -= 8; p.Welfare += 5; p.Centralization += 10; }
            else if (tag == "centralized") p.Centralization += 18;
            else if (tag == "party-state") { p.Centralization += 20; p.Pluralism -= 25; }
            else if (tag == "authoritarian") { p.Centralization += 18; p.Pluralism -= 30; p.Militarism += 5; }
            else if (tag == "autocratic") { p.Centralization += 15; p.Pluralism -= 15; }
            else if (tag == "totalitarian") { p.Centralization += 15; p.Pluralism -= 35; p.Militarism += 15; }
            else if (tag == "decentralized") { p.Centralization -= 25; p.Pluralism += 10; }
            else if (tag == "stateless") { p.Centralization -= 40; p.Pluralism += 15; }
            else if (tag == "minimal-state") { p.Centralization -= 25; p.Welfare -= 10; p.Market += 10; }
            else if (tag == "pluralism") { p.Pluralism += 25; p.Centralization -= 15; }
            else if (tag == "democracy" || tag == "democratic") { p.Pluralism += 15; p.Centralization -= 5; }
            else if (tag == "participatory" || tag == "direct" || tag == "consensus") { p.Pluralism += 10; p.Centralization -= 10; }
            else if (tag == "libertarian") { p.Pluralism += 15; p.Centralization -= 15; }
            else if (tag == "constitutional") { p.Pluralism += 10; p.Centralization -= 5; }
            else if (tag == "parliamentary") p.Pluralism += 10;
            else if (tag == "councils") { p.Centralization -= 12; p.Pluralism += 10; }
            else if (tag == "elective") p.Pluralism += 5;
            else if (tag == "executive") p.Centralization += 8;
            else if (tag == "hereditary") { p.Centralization += 5; p.Pluralism -= 10; }
            else if (tag == "elite") p.Pluralism -= 10;
            else if (tag == "militarist") p.Militarism += 25;
            else if (tag == "revolutionary") p.Militarism += 15;
            else if (tag == "insurrectionary") p.Militarism += 25;
            else if (tag == "pacifist") p.Militarism -= 60;
            else if (tag == "nationalist" || tag == "national") { p.Militarism += 8; p.Centralization += 5; }
            else if (tag == "order" || tag == "stability") p.Centralization += 8;
            else if (tag == "traditional" || tag == "tradition") p.Centralization += 5;
            else if (tag == "reactionary") { p.Centralization += 10; p.Pluralism -= 15; }
            else if (tag == "reformist" || tag == "progressive") { p.Pluralism += 8; p.Centralization -= 8; }
            else if (tag == "moderate") { p.Pluralism += 5; p.Militarism -= 5; }
            else if (tag == "radical") { p.Militarism += 10; p.Pluralism -= 5; }
            else if (tag == "primitivist") { p.Market -= 35; p.Centralization -= 10; p.Militarism -= 10; }
            else if (tag == "green") { p.Market -= 5; p.Welfare += 5; }
            else if (tag == "mutualist") { p.Market += 5; p.Welfare += 5; p.Centralization -= 10; }
            else if (tag == "individualist" || tag == "egoist") { p.Market += 10; p.Welfare -= 10; p.Centralization -= 10; }
            else if (tag == "fascist") { p.Centralization += 10; p.Pluralism -= 15; p.Militarism += 15; }
            else if (tag == "monarchy") { p.Centralization += 5; p.Pluralism -= 5; }
            else if (tag == "liberal") { p.Market += 5; p.Pluralism += 5; }
            else if (tag == "conservative") { p.Centralization += 5; p.Pluralism -= 3; }
            else if (tag == "utopian") { p.Welfare += 10; p.Militarism -= 10; }
            else if (tag == "clerical") { p.Centralization += 5; p.Pluralism -= 8; }
        }

        private static int GetIdeologyBehaviorStabilityModifier(Kingdom kingdom)
        {
            if (kingdom == null) return 0;
            IdeologyBehaviorProfile p = GetIdeologyBehaviorProfile(kingdom);
            string ideology = GetStateIdeology(kingdom);
            int support = IsValidIdeology(ideology)
                ? GetKingdomIdeologySupport(kingdom, ideology)
                : 50;
            int result = 0;

            if (support >= 60)
            {
                if (p.Centralization >= 80) result += 2;
                if (p.Pluralism >= 80) result += 2;
            }
            else if (support < 40)
            {
                if (p.Pluralism >= 70) result += 3;
                if (p.Pluralism <= 20) result -= 5;
            }

            int cityCount = GetCitiesSafe(kingdom).Count;
            if (cityCount > 4 && p.Centralization <= 20)
            {
                result += Math.Min(6, cityCount - 4);
            }
            if (cityCount > 8 && p.Centralization >= 90)
            {
                result -= Math.Min(4, cityCount - 8);
            }

            return ClampInt(result, -8, 8);
        }

        private static int GetIdeologyWarExhaustionPerYear(Kingdom kingdom)
        {
            IdeologyBehaviorProfile p = GetIdeologyBehaviorProfile(kingdom);
            if (p.Pacifist) return 9;
            if (p.Militarism >= 85) return 3;
            if (p.Militarism >= 70) return 4;
            if (p.Militarism <= 20) return 8;
            if (p.Militarism <= 35) return 6;
            return WarExhaustionAtWarPerYear;
        }

        private static int GetIdeologyWarDeclarationShock(Kingdom kingdom)
        {
            IdeologyBehaviorProfile p = GetIdeologyBehaviorProfile(kingdom);
            if (p.Pacifist) return 18;
            if (p.Militarism >= 85) return 6;
            if (p.Militarism >= 70) return 8;
            if (p.Militarism <= 25) return 14;
            return WarExhaustionDeclarationShock;
        }

        private static bool ShouldIdeologyBlockAggressiveWar(Kingdom attacker)
        {
            if (attacker == null) return false;
            IdeologyBehaviorProfile p = GetIdeologyBehaviorProfile(attacker);
            return p.Pacifist && GetKingdomCourse(attacker) != MilitaristTraitId;
        }

        private static string FormatIdeologyBehaviorPercent(int value)
        {
            return ClampInt(value, 0, 100).ToString() + "%";
        }

        private static string FormatSignedPoliticalValue(int value)
        {
            return value > 0 ? "+" + value.ToString() : value.ToString();
        }

        private static string FormatSignedPoliticalPercent(float value)
        {
            string sign = value > 0.0001f ? "+" : "";
            return sign + value.ToString("0.0") + "%";
        }

        private static string GetIdeologyEconomyEffectSummary(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return LM.Get("ukiol_ideology_effect_none");
            }

            IdeologyBehaviorProfile behavior = GetIdeologyBehaviorProfile(kingdom);
            int ideologySupport = GetKingdomIdeologySupport(
                kingdom,
                GetStateIdeology(kingdom)
            );

            if (ideologySupport < 45)
            {
                return LM.Get("ukiol_ideology_effect_inactive_support");
            }

            int gold = 0;
            int bread = 0;
            if (behavior.Market >= 70) gold += 1;
            else if (behavior.Primitivist) gold -= 1;
            if (behavior.Welfare >= 70) bread += 1;
            if (behavior.Market <= 25 && behavior.Centralization >= 65) gold -= 1;

            List<string> pieces = new List<string>();
            if (gold != 0)
            {
                pieces.Add(string.Format(
                    LM.Get("ukiol_ideology_effect_gold_city"),
                    FormatSignedPoliticalValue(gold)
                ));
            }
            if (bread != 0)
            {
                pieces.Add(string.Format(
                    LM.Get("ukiol_ideology_effect_bread_city"),
                    FormatSignedPoliticalValue(bread)
                ));
            }

            return pieces.Count == 0
                ? LM.Get("ukiol_ideology_effect_none")
                : string.Join(", ", pieces.ToArray());
        }

        private static float GetIdeologyArmyLimitEffectPercent(Kingdom kingdom)
        {
            if (kingdom == null) return 0f;
            IdeologyBehaviorProfile behavior = GetIdeologyBehaviorProfile(kingdom);
            string ideology = GetStateIdeology(kingdom);
            int support = IsValidIdeology(ideology)
                ? GetKingdomIdeologySupport(kingdom, ideology)
                : 50;
            float strength = Math.Max(0f, Math.Min(1f, support / 100f));
            return (behavior.Militarism - 50) * 0.15f * strength;
        }

        private static IdeologyNode GetIdeologyNode(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            EnsureIdeologyRegistry();
            IdeologyNode node;
            return IdeologyNodeRegistry.TryGetValue(id, out node)
                ? node
                : null;
        }

        private static bool IsIdeologyNodeDescendantOf(
            string nodeId,
            string ancestorId
        )
        {
            if (
                string.IsNullOrEmpty(nodeId) ||
                string.IsNullOrEmpty(ancestorId)
            )
            {
                return false;
            }

            IdeologyNode node = GetIdeologyNode(nodeId);
            int guard = 0;
            while (node != null && guard < IdeologyRegistryMaxDepth)
            {
                if (node.Id == ancestorId)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(node.ParentId))
                {
                    break;
                }

                node = GetIdeologyNode(node.ParentId);
                guard++;
            }

            return false;
        }

        private static List<string> GetIdeologyNodeChildren(string nodeId)
        {
            EnsureIdeologyRegistry();
            List<string> children;
            if (
                string.IsNullOrEmpty(nodeId) ||
                !IdeologyChildrenRegistry.TryGetValue(nodeId, out children)
            )
            {
                return new List<string>();
            }

            return new List<string>(children);
        }

        private static string GetIdeologyNodeDisplayName(string nodeId)
        {
            IdeologyNode node = GetIdeologyNode(nodeId);
            if (node == null)
            {
                return LM.Get("ukiol_current_forming");
            }

            return LM.Get(node.NameKey);
        }

        private static string GetIdeologyTreePath(
            string rootIdeology,
            string leafNode
        )
        {
            EnsureIdeologyRegistry();

            string startId = !string.IsNullOrEmpty(leafNode)
                ? leafNode
                : rootIdeology;
            IdeologyNode node = GetIdeologyNode(startId);

            if (node == null)
            {
                return IsValidIdeology(rootIdeology)
                    ? GetIdeologyName(rootIdeology)
                    : LM.Get("ukiol_current_forming");
            }

            List<string> names = new List<string>();
            int guard = 0;
            while (node != null && guard < IdeologyRegistryMaxDepth)
            {
                names.Add(LM.Get(node.NameKey));
                if (string.IsNullOrEmpty(node.ParentId))
                {
                    break;
                }

                node = GetIdeologyNode(node.ParentId);
                guard++;
            }

            names.Reverse();
            return string.Join(" → ", names.ToArray());
        }

        private static string GetKingdomIdeologyTreePath(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return LM.Get("ukiol_current_forming");
            }

            string root = GetStateIdeology(kingdom);
            string leaf = GetStateIdeologyCurrent(kingdom);
            return GetIdeologyTreePath(root, leaf);
        }

        private static void UpdateIdeologyFrameworkMigrations()
        {
            EnsureIdeologyRegistry();

            foreach (Kingdom kingdom in GetKingdomsSafe())
            {
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                int schema = GetKingdomIntData(
                    kingdom,
                    IdeologyFrameworkSchemaDataKey,
                    0
                );
                if (schema >= IdeologyFrameworkSchemaVersion)
                {
                    continue;
                }

                string ideology = GetStateIdeology(kingdom);
                if (IsValidIdeology(ideology))
                {
                    string rawCurrent = GetKingdomStringData(
                        kingdom,
                        StateIdeologyCurrentDataKey,
                        ""
                    );

                    if (
                        string.IsNullOrEmpty(rawCurrent) ||
                        !IsCurrentCompatibleWithIdeology(rawCurrent, ideology)
                    )
                    {
                        string desired = DetermineIdeologyCurrent(
                            kingdom,
                            ideology
                        );
                        if (!string.IsNullOrEmpty(desired))
                        {
                            SetStateIdeologyCurrent(kingdom, desired);
                        }
                    }
                }

                SetKingdomIntData(
                    kingdom,
                    IdeologyFrameworkSchemaDataKey,
                    IdeologyFrameworkSchemaVersion
                );
            }
        }

        private static void UpdateKingdomIdeologyCurrent(
            Kingdom kingdom,
            string stateIdeology
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidIdeology(stateIdeology)
            )
            {
                return;
            }

            string desiredLeaf = DetermineIdeologyCurrent(
                kingdom,
                stateIdeology
            );

            if (string.IsNullOrEmpty(desiredLeaf))
            {
                return;
            }

            string current = GetStateIdeologyCurrent(kingdom);

            // New states still receive a valid branch immediately. Afterwards,
            // dev12 only allows movement through neighbouring tree nodes.
            if (
                string.IsNullOrEmpty(current) ||
                !IsCurrentCompatibleWithIdeology(
                    current,
                    stateIdeology
                )
            )
            {
                string initial = GetTopLevelIdeologyBranch(
                    desiredLeaf,
                    stateIdeology
                );
                if (string.IsNullOrEmpty(initial))
                {
                    initial = desiredLeaf;
                }
                SetStateIdeologyCurrent(kingdom, initial);
                ResetIdeologyCurrentCandidate(kingdom);
                return;
            }

            if (current == desiredLeaf)
            {
                ResetIdeologyCurrentCandidate(kingdom);
                return;
            }

            string desired = GetNextIdeologyEvolutionStep(
                current,
                desiredLeaf,
                stateIdeology
            );

            if (
                string.IsNullOrEmpty(desired) ||
                desired == current
            )
            {
                ResetIdeologyCurrentCandidate(kingdom);
                return;
            }

            string candidate = "";
            int pressure = 0;

            try
            {
                kingdom.data.get(
                    StateIdeologyCurrentCandidateDataKey,
                    out candidate,
                    ""
                );
                kingdom.data.get(
                    StateIdeologyCurrentPressureDataKey,
                    out pressure,
                    0
                );
            }
            catch
            {
                candidate = "";
                pressure = 0;
            }

            if (candidate == desired)
            {
                pressure++;
            }
            else
            {
                candidate = desired;
                pressure = 1;
            }

            string transitionDirection = GetIdeologyTransitionDirection(
                current,
                desired
            );
            int evolutionThreshold = GetIdeologyEvolutionThreshold(
                kingdom,
                transitionDirection
            );

            if (pressure >= evolutionThreshold)
            {
                string previous = current;
                SetStateIdeologyCurrent(kingdom, desired);
                ResetIdeologyCurrentCandidate(kingdom);

                int dominantPressure = transitionDirection == "radicalization"
                    ? GetKingdomIntData(
                        kingdom,
                        IdeologyRadicalizationPressureDataKey,
                        0
                    )
                    : transitionDirection == "reform"
                        ? GetKingdomIntData(
                            kingdom,
                            IdeologyReformPressureDataKey,
                            0
                        )
                        : Math.Max(
                            GetKingdomIntData(
                                kingdom,
                                IdeologyReformPressureDataKey,
                                0
                            ),
                            GetKingdomIntData(
                                kingdom,
                                IdeologyRadicalizationPressureDataKey,
                                0
                            )
                        );

                string eventKey = "ukiol_event_current_shifted";
                if (transitionDirection == "radicalization")
                {
                    eventKey = "ukiol_event_current_radicalized_dev12";
                }
                else if (transitionDirection == "reform")
                {
                    eventKey = "ukiol_event_current_reformed_dev12";
                }

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get(eventKey),
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyCurrentName(previous),
                        GetIdeologyCurrentName(desired)
                    ),
                    kingdom,
                    null,
                    null,
                    GetIdeologyIconPath(stateIdeology),
                    "current_changed_" + desired,
                    30f
                );

                TryTriggerIdeologicalPartySplit(
                    kingdom,
                    stateIdeology,
                    previous,
                    desired,
                    transitionDirection,
                    dominantPressure
                );
                return;
            }

            try
            {
                kingdom.data.set(
                    StateIdeologyCurrentCandidateDataKey,
                    candidate
                );
                kingdom.data.set(
                    StateIdeologyCurrentPressureDataKey,
                    pressure
                );
            }
            catch
            {
            }
        }

        private static void UpdateIdeologyEvolutionPressures(
            Kingdom kingdom,
            string stateIdeology
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidIdeology(stateIdeology)
            )
            {
                return;
            }

            int stability = GetNationalStability(kingdom);
            int support = GetKingdomIdeologySupport(
                kingdom,
                stateIdeology
            );
            int crisis = GetPoliticalCrisisPressure(kingdom);
            int warExhaustion = GetKingdomIntData(
                kingdom,
                WarExhaustionDataKey,
                0
            );
            string course = GetKingdomCourse(kingdom);
            string rulerTrait = GetPoliticalTrait(
                GetLivingRuler(kingdom)
            );
            IdeologyBehaviorProfile behavior =
                GetIdeologyBehaviorProfile(kingdom);

            int reformTarget = 18;
            int radicalTarget = 15;

            if (stability >= 65)
            {
                reformTarget += (stability - 55) / 2;
            }
            else if (stability < 50)
            {
                radicalTarget += (50 - stability);
            }

            if (support >= 60)
            {
                reformTarget += (support - 50) / 3;
            }
            else if (support < 42)
            {
                radicalTarget += (42 - support) / 2;
            }

            radicalTarget += crisis / 3;
            radicalTarget += warExhaustion / 5;

            if (course == ReformerTraitId)
            {
                reformTarget += 24;
            }
            else if (course == DiplomatTraitId)
            {
                reformTarget += 16;
            }
            else if (course == MilitaristTraitId)
            {
                radicalTarget += 22;
            }

            if (rulerTrait == ReformerTraitId)
            {
                reformTarget += 16;
            }
            else if (rulerTrait == DiplomatTraitId)
            {
                reformTarget += 10;
            }
            else if (rulerTrait == MilitaristTraitId)
            {
                radicalTarget += 14;
            }

            if (behavior.Pluralism >= 70)
            {
                reformTarget += 9;
            }
            else if (behavior.Pluralism <= 25)
            {
                radicalTarget += 8;
            }

            if (behavior.Militarism >= 75)
            {
                radicalTarget += 8;
            }
            if (behavior.Pacifist)
            {
                reformTarget += 8;
                radicalTarget -= 6;
            }

            reformTarget = ClampInt(reformTarget, 0, 100);
            radicalTarget = ClampInt(radicalTarget, 0, 100);

            int oldReform = GetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                reformTarget
            );
            int oldRadical = GetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                radicalTarget
            );
            int reform = MoveTowardsInt(
                oldReform,
                reformTarget,
                6
            );
            int radical = MoveTowardsInt(
                oldRadical,
                radicalTarget,
                6
            );

            SetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                reform
            );
            SetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                radical
            );

            string direction = "stable";
            if (radical >= reform + 15 && radical >= 45)
            {
                direction = "radicalization";
            }
            else if (reform >= radical + 15 && reform >= 45)
            {
                direction = "reform";
            }
            else if (Math.Max(reform, radical) >= 50)
            {
                direction = "contested";
            }

            SetKingdomStringData(
                kingdom,
                IdeologyEvolutionDirectionDataKey,
                direction
            );

            MaybePublishIdeologyEvolutionSignal(
                kingdom,
                direction,
                reform,
                radical,
                stateIdeology
            );
        }

        private static void MaybePublishIdeologyEvolutionSignal(
            Kingdom kingdom,
            string direction,
            int reform,
            int radical,
            string stateIdeology
        )
        {
            if (
                kingdom == null ||
                direction == "stable" ||
                !IsValidIdeology(stateIdeology)
            )
            {
                return;
            }

            int dominant = direction == "reform"
                ? reform
                : direction == "radicalization"
                    ? radical
                    : Math.Min(reform, radical);
            if (dominant < 65)
            {
                return;
            }

            int year = GetWorldYearSafe();
            if (year <= 0)
            {
                return;
            }

            string lastSignal = GetKingdomStringData(
                kingdom,
                IdeologyEvolutionLastSignalDataKey,
                ""
            );
            int lastYear = GetKingdomIntData(
                kingdom,
                IdeologyEvolutionLastSignalYearDataKey,
                0
            );

            if (
                lastYear > 0 &&
                year - lastYear < IdeologyEvolutionSignalCooldownYears
            )
            {
                return;
            }

            string key = direction == "reform"
                ? "ukiol_event_reform_wave"
                : direction == "radicalization"
                    ? "ukiol_event_radicalization_wave"
                    : "ukiol_event_ideological_polarization";

            PublishPoliticalEvent(
                string.Format(
                    LM.Get(key),
                    GetWorldObjectDisplayName(kingdom),
                    reform,
                    radical
                ),
                kingdom,
                null,
                null,
                GetIdeologyIconPath(stateIdeology),
                "ideology_evolution_" + direction,
                45f
            );

            SetKingdomStringData(
                kingdom,
                IdeologyEvolutionLastSignalDataKey,
                direction
            );
            SetKingdomIntData(
                kingdom,
                IdeologyEvolutionLastSignalYearDataKey,
                year
            );
        }

        private static string GetTopLevelIdeologyBranch(
            string nodeId,
            string rootIdeology
        )
        {
            IdeologyNode node = GetIdeologyNode(nodeId);
            if (
                node == null ||
                node.RootIdeologyId != rootIdeology ||
                node.Tier <= 0
            )
            {
                return null;
            }

            IdeologyNode cursor = node;
            int guard = 0;
            while (
                cursor != null &&
                cursor.Tier > 1 &&
                guard < IdeologyRegistryMaxDepth
            )
            {
                cursor = GetIdeologyNode(cursor.ParentId);
                guard++;
            }

            return cursor != null && cursor.Tier > 0
                ? cursor.Id
                : null;
        }

        private static string GetNextIdeologyEvolutionStep(
            string current,
            string target,
            string rootIdeology
        )
        {
            IdeologyNode currentNode = GetIdeologyNode(current);
            IdeologyNode targetNode = GetIdeologyNode(target);
            if (
                currentNode == null ||
                targetNode == null ||
                currentNode.RootIdeologyId != rootIdeology ||
                targetNode.RootIdeologyId != rootIdeology
            )
            {
                return target;
            }

            if (current == target)
            {
                return current;
            }

            // Moving deeper in the same branch: select the first child on the
            // path instead of teleporting directly to a fourth-level leaf.
            if (IsIdeologyNodeDescendantOf(target, current))
            {
                IdeologyNode cursor = targetNode;
                int guard = 0;
                while (
                    cursor != null &&
                    cursor.ParentId != current &&
                    guard < IdeologyRegistryMaxDepth
                )
                {
                    cursor = GetIdeologyNode(cursor.ParentId);
                    guard++;
                }
                return cursor != null ? cursor.Id : target;
            }

            // If target is an ancestor, back out by one level. Root ideology
            // itself is not stored as an active current, so a top-level branch
            // never collapses into an invalid tier-0 current.
            if (IsIdeologyNodeDescendantOf(current, target))
            {
                if (currentNode.ParentId == rootIdeology)
                {
                    return current;
                }
                return currentNode.ParentId;
            }

            // Different branches: climb toward the common ancestor. If we are
            // already at a top-level branch, switch to the target top-level
            // branch in one legal step instead of persisting the root node.
            if (currentNode.ParentId == rootIdeology)
            {
                string targetTop = GetTopLevelIdeologyBranch(
                    target,
                    rootIdeology
                );
                return string.IsNullOrEmpty(targetTop)
                    ? target
                    : targetTop;
            }

            return currentNode.ParentId;
        }

        private static string GetIdeologyTransitionDirection(
            string current,
            string next
        )
        {
            int currentScore = GetIdeologyRadicalismScore(current);
            int nextScore = GetIdeologyRadicalismScore(next);

            if (nextScore >= currentScore + 6)
            {
                return "radicalization";
            }
            if (nextScore <= currentScore - 6)
            {
                return "reform";
            }
            return "shift";
        }

        private static int GetIdeologyEvolutionThreshold(
            Kingdom kingdom,
            string direction
        )
        {
            int reform = GetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                0
            );
            int radical = GetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            int threshold = IdeologyCurrentEvolutionThreshold;

            if (direction == "radicalization")
            {
                if (radical >= 75) threshold = 2;
                else if (radical < 35) threshold = 5;
                if (reform >= radical + 20) threshold++;
            }
            else if (direction == "reform")
            {
                if (reform >= 75) threshold = 2;
                else if (reform < 35) threshold = 5;
                if (radical >= reform + 20) threshold++;
            }
            else
            {
                if (Math.Max(reform, radical) < 35)
                {
                    threshold = 4;
                }
            }

            return ClampInt(threshold, 2, 6);
        }

        private static int GetIdeologyRadicalismScore(string nodeId)
        {
            IdeologyBehaviorProfile profile =
                GetIdeologyBehaviorProfile(nodeId);
            int score = 20;
            score += (100 - profile.Pluralism) * 30 / 100;
            score += profile.Militarism * 24 / 100;
            score += profile.Centralization * 14 / 100;

            if (IdeologyNodeHasInheritedTag(nodeId, "revolutionary")) score += 16;
            if (IdeologyNodeHasInheritedTag(nodeId, "radical")) score += 12;
            if (IdeologyNodeHasInheritedTag(nodeId, "insurrectionary")) score += 20;
            if (IdeologyNodeHasInheritedTag(nodeId, "totalitarian")) score += 20;
            if (IdeologyNodeHasInheritedTag(nodeId, "reactionary")) score += 10;
            if (IdeologyNodeHasInheritedTag(nodeId, "authoritarian")) score += 8;
            if (IdeologyNodeHasInheritedTag(nodeId, "reformist")) score -= 15;
            if (IdeologyNodeHasInheritedTag(nodeId, "moderate")) score -= 10;
            if (IdeologyNodeHasInheritedTag(nodeId, "pluralism")) score -= 12;
            if (IdeologyNodeHasInheritedTag(nodeId, "democratic")) score -= 8;
            if (IdeologyNodeHasInheritedTag(nodeId, "pacifist")) score -= 18;

            return ClampInt(score, 0, 100);
        }

        private static bool IdeologyNodeHasInheritedTag(
            string nodeId,
            string tag
        )
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(tag))
            {
                return false;
            }

            IdeologyNode node = GetIdeologyNode(nodeId);
            int guard = 0;
            while (node != null && guard < IdeologyRegistryMaxDepth)
            {
                if (node.Tags != null)
                {
                    for (int i = 0; i < node.Tags.Length; i++)
                    {
                        if (node.Tags[i] == tag)
                        {
                            return true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(node.ParentId))
                {
                    break;
                }
                node = GetIdeologyNode(node.ParentId);
                guard++;
            }

            return false;
        }

        private static void TryTriggerIdeologicalPartySplit(
            Kingdom kingdom,
            string ideology,
            string previousCurrent,
            string newCurrent,
            string direction,
            int changePressure
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology) ||
                changePressure < 65 ||
                direction == "shift"
            )
            {
                return;
            }

            int year = GetWorldYearSafe();
            if (year <= 0)
            {
                return;
            }

            string cooldownKey =
                IdeologyBranchSplitLastYearPrefix +
                GetMovementKeySuffix(ideology);
            int lastYear = GetKingdomIntData(
                kingdom,
                cooldownKey,
                0
            );
            if (
                lastYear > 0 &&
                year - lastYear < IdeologicalBranchSplitCooldownYears
            )
            {
                return;
            }

            List<PoliticalParty> parties = GetPoliticalParties(kingdom);
            List<PoliticalParty> same = GetPartiesForIdeology(
                parties,
                ideology
            );
            if (
                same.Count == 0 ||
                same.Count >= MaxPoliticalPartiesPerIdeology ||
                parties.Count >= MaxPoliticalParties
            )
            {
                return;
            }

            PoliticalParty parent = null;
            for (int i = 0; i < same.Count; i++)
            {
                PoliticalParty candidate = same[i];
                if (
                    candidate != null &&
                    (parent == null || candidate.Support > parent.Support)
                )
                {
                    parent = candidate;
                }
            }

            if (parent == null || parent.Support < 20)
            {
                return;
            }

            int branchDistance = Math.Abs(
                GetIdeologyRadicalismScore(newCurrent) -
                GetIdeologyRadicalismScore(previousCurrent)
            );
            if (branchDistance < 6 && changePressure < 78)
            {
                return;
            }

            int childRadicalism = direction == "radicalization"
                ? ClampInt(parent.Radicalism + 24, 5, 95)
                : ClampInt(parent.Radicalism - 22, 5, 95);

            PoliticalParty created = CreatePoliticalParty(
                kingdom,
                ideology,
                childRadicalism,
                true
            );
            if (created == null)
            {
                return;
            }

            created.ParentPartyId = parent.Id;
            created.ParentPartyName = parent.Name;
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyIdPrefix,
                    created.Slot
                ),
                parent.Id
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyNamePrefix,
                    created.Slot
                ),
                parent.Name
            );

            created.ColorSeed = ChooseRelatedPartyColorSeed(
                kingdom,
                created.Ideology,
                parent.ColorSeed,
                created.Slot
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2ColorSeedPrefix,
                    created.Slot
                ),
                created.ColorSeed
            );

            RecordPartyHistoryEvent(
                kingdom,
                created,
                PartyHistorySplitFrom,
                parent.Name,
                GetIdeologyCurrentName(newCurrent),
                year
            );
            RecordPartyHistoryEvent(
                kingdom,
                parent,
                PartyHistorySplitChild,
                created.Name,
                GetIdeologyCurrentName(newCurrent),
                year
            );

            SetKingdomIntData(
                kingdom,
                cooldownKey,
                year
            );

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_ideological_party_split"),
                    parent.Name,
                    created.Name,
                    GetWorldObjectDisplayName(kingdom),
                    GetIdeologyCurrentName(previousCurrent),
                    GetIdeologyCurrentName(newCurrent)
                ),
                kingdom,
                null,
                created.LeaderActor,
                GetIdeologyIconPath(ideology),
                "ideological_party_split_" + created.Id,
                35f
            );
        }

        private static string DetermineIdeologyCurrent(
            Kingdom kingdom,
            string stateIdeology
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(stateIdeology)
            )
            {
                return null;
            }

            int stability = GetNationalStability(kingdom);
            int stateSupport = GetKingdomIdeologySupport(
                kingdom,
                stateIdeology
            );
            int crisis = GetPoliticalCrisisPressure(kingdom);
            string course = GetKingdomCourse(kingdom);
            Actor ruler = GetLivingRuler(kingdom);
            string rulerTrait = GetPoliticalTrait(ruler);

            switch (stateIdeology)
            {
                case MonarchismIdeologyId:
                {
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int democracy = GetKingdomIdeologySupport(kingdom, DemocracyIdeologyId);
                    int conservative = GetKingdomIdeologySupport(kingdom, ConservatismIdeologyId);
                    int reformInfluence = liberal + democracy;

                    if (democracy >= 35 && stateSupport < 50)
                    {
                        return stability >= 50
                            ? PopularElectiveMonarchyCurrentId
                            : AristocraticElectiveMonarchyCurrentId;
                    }

                    if (
                        stability >= 45 &&
                        (
                            course == ReformerTraitId ||
                            course == DiplomatTraitId ||
                            rulerTrait == ReformerTraitId ||
                            rulerTrait == DiplomatTraitId ||
                            reformInfluence >= 45
                        )
                    )
                    {
                        return democracy >= 35 && stability >= 55
                            ? ParliamentaryMonarchyCurrentId
                            : DualistMonarchyCurrentId;
                    }

                    if (
                        (course == ReformerTraitId || rulerTrait == ReformerTraitId) &&
                        stability >= 50
                    )
                    {
                        return EnlightenedAbsolutismCurrentId;
                    }

                    if (stability < 35 || crisis >= 60)
                    {
                        return AutocraticMonarchismCurrentId;
                    }

                    return conservative >= 30
                        ? PatrimonialMonarchyCurrentId
                        : AutocraticMonarchismCurrentId;
                }

                case ConservatismIdeologyId:
                {
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int monarchy = GetKingdomIdeologySupport(kingdom, MonarchismIdeologyId);
                    int fascism = GetKingdomIdeologySupport(kingdom, FascismIdeologyId);

                    if (
                        course == ReformerTraitId ||
                        course == DiplomatTraitId ||
                        liberal >= 25
                    )
                    {
                        return socialism >= 20
                            ? ProgressiveConservatismCurrentId
                            : FiscalConservatismCurrentId;
                    }

                    if (
                        stability < 38 ||
                        crisis >= 55 ||
                        course == MilitaristTraitId ||
                        rulerTrait == MilitaristTraitId
                    )
                    {
                        return OrderConservatismCurrentId;
                    }

                    if (fascism >= 20)
                    {
                        return NationalConservatismCurrentId;
                    }

                    return monarchy >= 25
                        ? PaternalisticConservatismCurrentId
                        : ReactionaryConservatismCurrentId;
                }

                case LiberalismIdeologyId:
                {
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int democracy = GetKingdomIdeologySupport(kingdom, DemocracyIdeologyId);
                    int anarchism = GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                    int conservatism = GetKingdomIdeologySupport(kingdom, ConservatismIdeologyId);
                    int socialInfluence = socialism + democracy;

                    if (anarchism >= 25 && socialism < 25)
                    {
                        return stateSupport >= 55
                            ? MinarchismCurrentId
                            : LibertarianismCurrentId;
                    }

                    if (conservatism >= 32 && democracy < 35)
                    {
                        return CivicNationalLiberalismCurrentId;
                    }

                    if (
                        socialInfluence >= 45 ||
                        (course == ReformerTraitId && stateSupport >= 50)
                    )
                    {
                        return socialism >= 28
                            ? WelfareLiberalismCurrentId
                            : ProgressiveLiberalismCurrentId;
                    }

                    return conservatism >= 24
                        ? OrdoliberalismCurrentId
                        : EconomicLiberalismCurrentId;
                }

                case DemocracyIdeologyId:
                {
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int syndicalism = GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId);
                    int anarchism = GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                    int conservatism = GetKingdomIdeologySupport(kingdom, ConservatismIdeologyId);
                    int councilInfluence = socialism + syndicalism + anarchism;

                    if (councilInfluence >= 55)
                    {
                        return DelegativeCouncilDemocracyCurrentId;
                    }

                    if (stability < 35)
                    {
                        return DirectDemocracyCurrentId;
                    }

                    if (stateSupport < 42)
                    {
                        return ParticipatoryDemocracyCurrentId;
                    }

                    if (conservatism >= 35 && liberal < 30)
                    {
                        return ConstitutionalPresidentialismCurrentId;
                    }

                    if (liberal >= 30 && socialism >= 20)
                    {
                        return ConsensusDemocracyCurrentId;
                    }

                    return LiberalDemocracyCurrentId;
                }

                case SocialismIdeologyId:
                {
                    int democracy = GetKingdomIdeologySupport(kingdom, DemocracyIdeologyId);
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int communism = GetKingdomIdeologySupport(kingdom, CommunismIdeologyId);
                    int anarchism = GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                    int syndicalism = GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId);
                    int democraticInfluence = democracy + liberal;

                    if (stability < 35 || crisis >= 60)
                    {
                        return communism >= 25
                            ? MarxistSocialismCurrentId
                            : RevolutionaryDemocraticSocialismCurrentId;
                    }

                    if (anarchism + syndicalism >= 38)
                    {
                        return syndicalism >= 24
                            ? GuildSocialismCurrentId
                            : LibertarianSocialismCurrentId;
                    }

                    if (democraticInfluence >= 45)
                    {
                        return liberal >= 28
                            ? ModerateSocialDemocracyCurrentId
                            : LeftSocialDemocracyCurrentId;
                    }

                    if (liberal >= 22)
                    {
                        return MarketSocialismCurrentId;
                    }

                    if (stateSupport < 42)
                    {
                        return UtopianSocialismCurrentId;
                    }

                    return CooperativeSocialismCurrentId;
                }

                case CommunismIdeologyId:
                {
                    int anarchism = GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                    int syndicalism = GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId);
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int democracy = GetKingdomIdeologySupport(kingdom, DemocracyIdeologyId);
                    int conservatism = GetKingdomIdeologySupport(kingdom, ConservatismIdeologyId);
                    int decentralistInfluence = anarchism + syndicalism;

                    if (
                        decentralistInfluence >= 35 &&
                        course != MilitaristTraitId &&
                        rulerTrait != MilitaristTraitId
                    )
                    {
                        if (anarchism >= 28)
                        {
                            return LeftCommunismCurrentId;
                        }

                        if (syndicalism >= 28)
                        {
                            return WorkersCouncilCommunismCurrentId;
                        }

                        return CommunalCommunismCurrentId;
                    }

                    if (course == ReformerTraitId || rulerTrait == ReformerTraitId)
                    {
                        return liberal + democracy >= 38
                            ? EurocommunismCurrentId
                            : ReformistMarxismLeninismCurrentId;
                    }

                    if (course == MilitaristTraitId || rulerTrait == MilitaristTraitId)
                    {
                        if (stability < 35 || crisis >= 60)
                        {
                            return MaoismCurrentId;
                        }

                        if (conservatism >= 24)
                        {
                            return NationalCommunismCurrentId;
                        }

                        return OrthodoxMarxismLeninismCurrentId;
                    }

                    return stateSupport >= 65
                        ? OrthodoxMarxismLeninismCurrentId
                        : PartyCommunismCurrentId;
                }

                case FascismIdeologyId:
                {
                    int syndicalism = GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId);
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int conservatism = GetKingdomIdeologySupport(kingdom, ConservatismIdeologyId);
                    int monarchy = GetKingdomIdeologySupport(kingdom, MonarchismIdeologyId);

                    if (syndicalism >= 30)
                    {
                        return socialism >= 20
                            ? NationalSyndicalismFascistCurrentId
                            : FalangismCurrentId;
                    }

                    if (stability < 35 || crisis >= 60)
                    {
                        return course == MilitaristTraitId || rulerTrait == MilitaristTraitId
                            ? TotalitarianFascismCurrentId
                            : IntegralFascismCurrentId;
                    }

                    if (course == MilitaristTraitId || rulerTrait == MilitaristTraitId)
                    {
                        return stateSupport >= 60
                            ? NationalSocialismCurrentId
                            : TotalitarianFascismCurrentId;
                    }

                    if (conservatism + monarchy >= 55)
                    {
                        return ClericalFascismCurrentId;
                    }

                    return StateCorporatismCurrentId;
                }

                case AnarchismIdeologyId:
                {
                    int syndicalism = GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId);
                    int communism = GetKingdomIdeologySupport(kingdom, CommunismIdeologyId);
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);
                    int socialLeft = communism + socialism;

                    if (stability < 30 || crisis >= 65)
                    {
                        return InsurrectionaryAnarchismCurrentId;
                    }

                    if (course == DiplomatTraitId || rulerTrait == DiplomatTraitId)
                    {
                        return AnarchoPacifismCurrentId;
                    }

                    if (syndicalism >= 25)
                    {
                        return AnarchoSyndicalismCurrentId;
                    }

                    if (socialLeft >= 38)
                    {
                        return syndicalism >= 18
                            ? CollectivistAnarchismCurrentId
                            : AnarchoCommunismCurrentId;
                    }

                    if (liberal >= 35 && socialLeft < 30)
                    {
                        if (stateSupport >= 55)
                        {
                            return AnarchoCapitalismCurrentId;
                        }

                        return liberal >= 50
                            ? AgorismCurrentId
                            : LeftMarketAnarchismCurrentId;
                    }

                    if (course == ReformerTraitId || rulerTrait == ReformerTraitId)
                    {
                        return MutualismCurrentId;
                    }

                    return EgoistAnarchismCurrentId;
                }

                case SyndicalismIdeologyId:
                {
                    int anarchism = GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                    int socialism = GetKingdomIdeologySupport(kingdom, SocialismIdeologyId);
                    int democracy = GetKingdomIdeologySupport(kingdom, DemocracyIdeologyId);
                    int liberal = GetKingdomIdeologySupport(kingdom, LiberalismIdeologyId);

                    if (stability < 35 || crisis >= 60)
                    {
                        return anarchism >= 25
                            ? CouncilSyndicalismCurrentId
                            : RevolutionaryUnionismCurrentId;
                    }

                    if (democracy + liberal >= 42)
                    {
                        return socialism >= 28
                            ? CooperativeCommonwealthCurrentId
                            : ParliamentarySyndicalismCurrentId;
                    }

                    if (socialism >= 35)
                    {
                        return stateSupport >= 55
                            ? CooperativeSyndicalismCurrentId
                            : GuildSyndicalismCurrentId;
                    }

                    return course == ReformerTraitId || rulerTrait == ReformerTraitId
                        ? ReformistSyndicalismCurrentId
                        : GuildSyndicalismCurrentId;
                }
            }

            return null;
        }

        private static string GetStateIdeologyCurrent(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return null;
            }

            string current = "";

            try
            {
                kingdom.data.get(
                    StateIdeologyCurrentDataKey,
                    out current,
                    ""
                );
            }
            catch
            {
                return null;
            }

            string ideology = GetStateIdeology(kingdom);

            return IsCurrentCompatibleWithIdeology(
                current,
                ideology
            )
                ? current
                : null;
        }

        private static string GetCityIdeologyCurrent(
            City city,
            string dominantIdeology = null
        )
        {
            if (city == null || city.data == null)
            {
                return null;
            }

            if (!IsValidIdeology(dominantIdeology))
            {
                int support;
                int tension;
                GetCityIdeologyOverview(
                    city,
                    out dominantIdeology,
                    out support,
                    out tension
                );
            }

            string current = GetCityStringData(
                city,
                CityIdeologyCurrentDataKey,
                ""
            );

            IdeologyNode node = GetIdeologyNode(current);
            if (
                node == null ||
                node.Tier <= 0 ||
                !IsValidIdeology(dominantIdeology) ||
                node.RootIdeologyId != dominantIdeology
            )
            {
                return null;
            }

            return current;
        }

        private static void SetStateIdeologyCurrent(
            Kingdom kingdom,
            string current
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                string.IsNullOrEmpty(current)
            )
            {
                return;
            }

            try
            {
                string previous = GetStateIdeologyCurrent(kingdom);

                kingdom.data.set(
                    StateIdeologyCurrentDataKey,
                    current
                );

                if (previous != current)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        PoliticalWorldAPI.Events.CurrentChanged,
                        kingdom,
                        previous ?? "",
                        current
                    );
                }
            }
            catch
            {
            }
        }

        private static void ResetIdeologyCurrentCandidate(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            try
            {
                kingdom.data.set(
                    StateIdeologyCurrentCandidateDataKey,
                    ""
                );
                kingdom.data.set(
                    StateIdeologyCurrentPressureDataKey,
                    0
                );
            }
            catch
            {
            }
        }

        private static void ResetStateIdeologyCurrent(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            try
            {
                kingdom.data.set(
                    StateIdeologyCurrentDataKey,
                    ""
                );
            }
            catch
            {
            }

            ResetIdeologyCurrentCandidate(kingdom);
        }

        private static bool IsCurrentCompatibleWithIdeology(
            string current,
            string ideology
        )
        {
            if (
                string.IsNullOrEmpty(current) ||
                !IsValidIdeology(ideology)
            )
            {
                return false;
            }

            IdeologyNode node = GetIdeologyNode(current);
            return
                node != null &&
                node.Tier > 0 &&
                node.RootIdeologyId == ideology;
        }

        private static int GetIdeologyCurrentStabilityModifier(
            string current,
            int stateSupport
        )
        {
            IdeologyNode node = GetIdeologyNode(current);
            if (node == null || node.Tier <= 0)
            {
                return 0;
            }

            if (node.StabilitySupportThreshold >= 0)
            {
                return stateSupport >= node.StabilitySupportThreshold
                    ? node.HighSupportStability
                    : node.LowSupportStability;
            }

            return node.HighSupportStability;
        }

        private static float GetIdeologyCurrentDiffusionMultiplier(
            string current
        )
        {
            IdeologyNode node = GetIdeologyNode(current);
            return node != null && node.Tier > 0
                ? node.DiffusionMultiplier
                : 1f;
        }

        private static string GetIdeologyCurrentName(
            string current
        )
        {
            if (string.IsNullOrEmpty(current))
            {
                return LM.Get("ukiol_current_forming");
            }

            IdeologyNode node = GetIdeologyNode(current);
            return node != null
                ? LM.Get(node.NameKey)
                : LM.Get("ukiol_current_forming");
        }

        private static int GetRulerIdeologyCompatibilityBonus(
            Actor ruler,
            string ideology
        )
        {
            string trait = GetPoliticalTrait(ruler);

            if (string.IsNullOrEmpty(trait))
            {
                return 0;
            }

            if (trait == ReformerTraitId)
            {
                if (
                    ideology == LiberalismIdeologyId ||
                    ideology == DemocracyIdeologyId ||
                    ideology == SocialismIdeologyId
                )
                {
                    return 4;
                }
            }
            else if (trait == MilitaristTraitId)
            {
                if (
                    ideology == MonarchismIdeologyId ||
                    ideology == ConservatismIdeologyId ||
                    ideology == FascismIdeologyId
                )
                {
                    return 4;
                }
            }
            else if (trait == DiplomatTraitId)
            {
                if (
                    ideology == LiberalismIdeologyId ||
                    ideology == DemocracyIdeologyId ||
                    ideology == ConservatismIdeologyId
                )
                {
                    return 4;
                }
            }

            return -2;
        }

    }
}
