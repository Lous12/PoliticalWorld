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
        private static void CityLoyaltyPostfix(
            City __instance,
            ref int __result
        )
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                int stability = GetLocalStability(__instance);

                // В стандартной логике WorldBox отрицательная
                // лояльность используется как опасная зона.
                // 20% нашей стабильности соответствует нулю
                // старой шкалы, поэтому базовые проверки игры
                // начинают реагировать на действительно
                // нестабильные поселения.
                int legacyLoyalty = (int)Math.Round(
                    (stability - 20) * 1.25f
                );

                __result = ClampInt(
                    legacyLoyalty,
                    -100,
                    100
                );
            }
            catch
            {
                // Если мост не сработал, WorldBox оставляет
                // собственный результат лояльности.
            }
        }

        private static void ProcessKingdomRulers()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            HashSet<Kingdom> activeKingdoms =
                new HashSet<Kingdom>();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                Actor ruler = GetLivingRuler(kingdom);

                if (ruler == null)
                {
                    continue;
                }

                activeKingdoms.Add(kingdom);

                int politicalTraitCount =
                    CountPoliticalTraits(ruler);

                if (politicalTraitCount == 0)
                {
                    AssignPoliticalTrait(
                        ruler,
                        GetPreferredPoliticalTrait(ruler)
                    );
                }
                else if (politicalTraitCount > 1)
                {
                    AssignPoliticalTrait(
                        ruler,
                        GetPreferredPoliticalTrait(ruler)
                    );
                }

                string currentCourse =
                    GetKingdomCourse(kingdom);

                if (string.IsNullOrEmpty(currentCourse))
                {
                    currentCourse = GetPoliticalTrait(ruler);
                    SetKingdomCourse(
                        kingdom,
                        currentCourse
                    );
                }

                if (string.IsNullOrEmpty(GetStateIdeology(kingdom)))
                {
                    if (!TryInitializeKingdomPoliticalSuccession(
                        kingdom,
                        ruler
                    ))
                    {
                        SetStateIdeology(
                            kingdom,
                            GetInitialStateIdeology(
                                kingdom,
                                ruler,
                                currentCourse
                            )
                        );
                    }

                    currentCourse = GetKingdomCourse(kingdom);
                }

                TrackLeaderLongevityFoundation(kingdom, ruler);

                string previousCourse = null;
                LastKnownKingdomCourses.TryGetValue(
                    kingdom,
                    out previousCourse
                );

                if (previousCourse != currentCourse)
                {
                    LastKnownKingdomCourses[kingdom] =
                        currentCourse;

                    RefreshKingdomUnitStats(kingdom);
                }
            }

            List<Kingdom> staleKingdoms =
                new List<Kingdom>();

            foreach (
                KeyValuePair<Kingdom, string> pair
                in LastKnownKingdomCourses
            )
            {
                if (!activeKingdoms.Contains(pair.Key))
                {
                    staleKingdoms.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleKingdoms.Count; i++)
            {
                Kingdom staleKingdom = staleKingdoms[i];
                LastKnownKingdomCourses.Remove(staleKingdom);
                FastKingdomCourseCache.Remove(staleKingdom);
                CachedArmyLimitPoliticalDelta.Remove(staleKingdom);
                CachedArmyLimitPoliticalDeltaUntil.Remove(staleKingdom);
            }
        }

        private static void TrackLeaderLongevityFoundation(
            Kingdom kingdom,
            Actor ruler
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                ruler == null
            )
            {
                return;
            }

            int currentYear = Math.Max(0, GetWorldYearSafe());
            string identity = GetStableObjectIdentity(ruler);
            if (string.IsNullOrEmpty(identity))
            {
                return;
            }

            string trackedIdentity = GetKingdomStringData(
                kingdom,
                LeaderTrackedIdentityDataKey,
                ""
            );
            string previousLeaderName = GetKingdomStringData(
                kingdom,
                LeaderTrackedNameDataKey,
                ""
            );

            bool hadTrackedLeader = !string.IsNullOrEmpty(trackedIdentity);
            bool leaderChanged = trackedIdentity != identity;
            int lastUpdatedYear = GetKingdomIntData(
                kingdom,
                LeaderFoundationLastYearDataKey,
                -1
            );

            if (!leaderChanged && lastUpdatedYear == currentYear)
            {
                return;
            }

            if (leaderChanged)
            {
                string newLeaderName = GetWorldObjectDisplayName(ruler);
                SetKingdomStringData(
                    kingdom,
                    LeaderTrackedIdentityDataKey,
                    identity
                );
                SetKingdomStringData(
                    kingdom,
                    LeaderTrackedNameDataKey,
                    newLeaderName
                );

                // Do not emit a fake succession when an older save is first
                // observed by the API. Every later ruler change is exact.
                if (hadTrackedLeader)
                {
                    PoliticalWorldAPI.InternalEmitCoreEvent(
                        eventId: PoliticalWorldAPI.Events.RulerChanged,
                        kingdom: kingdom,
                        oldValue: trackedIdentity,
                        newValue: identity,
                        actor: ruler,
                        actorIdentity: identity,
                        actorName: newLeaderName,
                        oldName: previousLeaderName,
                        newName: newLeaderName,
                        category: "ruler",
                        year: currentYear
                    );
                }

                // On migration from an older Political World build we cannot
                // reliably reconstruct the exact coronation/election year.
                // Start observing from the first dev3 tick instead of inventing
                // historical tenure. Every later succession is exact.
                SetKingdomIntData(
                    kingdom,
                    LeaderReignStartYearDataKey,
                    currentYear
                );
            }

            int reignStartYear = GetKingdomIntData(
                kingdom,
                LeaderReignStartYearDataKey,
                currentYear
            );
            if (reignStartYear < 0 || reignStartYear > currentYear)
            {
                reignStartYear = currentYear;
                SetKingdomIntData(
                    kingdom,
                    LeaderReignStartYearDataKey,
                    reignStartYear
                );
            }

            int tenure = Math.Max(0, currentYear - reignStartYear);
            int age = GetActorAgeYearsSafe(ruler);
            int expectedLifespan = GetActorExpectedLifespanYearsSafe(ruler);
            int subjectLifespan = GetKingdomSubjectExpectedLifespanYearsSafe(
                kingdom
            );

            string raceId = GetActorRaceIdSafe(ruler);
            if (!string.IsNullOrEmpty(raceId))
            {
                SetKingdomStringData(
                    kingdom,
                    LeaderRaceIdDataKey,
                    raceId
                );
            }

            SetKingdomIntData(kingdom, LeaderTenureYearsDataKey, tenure);
            SetKingdomIntData(kingdom, LeaderAgeYearsDataKey, age);
            SetKingdomIntData(
                kingdom,
                LeaderExpectedLifespanDataKey,
                expectedLifespan
            );
            SetKingdomIntData(
                kingdom,
                LeaderSubjectLifespanDataKey,
                subjectLifespan
            );
            SetKingdomIntData(
                kingdom,
                LeaderAgeRatioPermilleDataKey,
                RatioPermille(age, expectedLifespan)
            );
            SetKingdomIntData(
                kingdom,
                LeaderTenureRatioPermilleDataKey,
                RatioPermille(tenure, expectedLifespan)
            );
            SetKingdomIntData(
                kingdom,
                LeaderSubjectGenerationPermilleDataKey,
                RatioPermille(tenure, subjectLifespan)
            );
            SetKingdomIntData(
                kingdom,
                LeaderImmortalTraitDataKey,
                HasAnyActorTraitSafe(
                    ruler,
                    "immortal",
                    "trait_immortal"
                ) ? 1 : 0
            );
            SetKingdomIntData(
                kingdom,
                LeaderLongLivedTraitDataKey,
                HasAnyActorTraitSafe(
                    ruler,
                    "long_liver",
                    "long_lived",
                    "long_liver_trait",
                    "trait_long_liver"
                ) ? 1 : 0
            );
            SetKingdomIntData(
                kingdom,
                LeaderFoundationLastYearDataKey,
                currentYear
            );
        }

        private static int RatioPermille(int value, int baseline)
        {
            if (value <= 0 || baseline <= 0)
            {
                return 0;
            }

            long ratio = (long)value * 1000L / baseline;
            if (ratio > 100000L)
            {
                ratio = 100000L;
            }
            return (int)ratio;
        }

        private static bool HasAnyActorTraitSafe(
            Actor actor,
            params string[] traitIds
        )
        {
            if (actor == null || traitIds == null)
            {
                return false;
            }

            for (int i = 0; i < traitIds.Length; i++)
            {
                string traitId = traitIds[i];
                if (string.IsNullOrEmpty(traitId))
                {
                    continue;
                }

                try
                {
                    if (actor.hasTrait(traitId))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        private static string GetActorRaceIdSafe(Actor actor)
        {
            if (actor == null)
            {
                return "";
            }

            object directRace = GetMemberValue(
                actor,
                "race",
                "_race",
                "species",
                "_species"
            );
            string id = GetObjectIdStringSafe(directRace);
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            object asset = GetMemberValue(actor, "asset", "_asset");
            object assetRace = GetMemberValue(
                asset,
                "race",
                "_race",
                "species",
                "_species"
            );
            id = GetObjectIdStringSafe(assetRace);
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            // On some WorldBox builds the unit asset itself is race-specific
            // (for example human/elf/orc/dwarf). Keeping this fallback lets
            // modded races participate without a hardcoded race table.
            return GetObjectIdStringSafe(asset);
        }

        private static string GetObjectIdStringSafe(object obj)
        {
            if (obj == null)
            {
                return "";
            }
            if (obj is string)
            {
                return obj.ToString();
            }

            object id = GetMemberValue(
                obj,
                "id",
                "_id",
                "race_id",
                "raceId",
                "species_id",
                "speciesId"
            );
            return id == null ? "" : id.ToString();
        }

        private static int GetActorAgeYearsSafe(Actor actor)
        {
            if (actor == null)
            {
                return 0;
            }

            string[] methods =
            {
                "getAge",
                "getAgeYears",
                "getYearsOld"
            };
            for (int i = 0; i < methods.Length; i++)
            {
                int value = TryInvokeIntMethod(actor, methods[i], -1);
                if (value >= 0)
                {
                    return value;
                }
            }

            int direct;
            if (TryGetIntMember(
                actor,
                out direct,
                "age",
                "age_years",
                "ageYears",
                "years_old",
                "yearsOld"
            ))
            {
                return Math.Max(0, direct);
            }

            object data = GetMemberValue(actor, "data", "_data");
            if (TryGetIntMember(
                data,
                out direct,
                "age",
                "age_years",
                "ageYears",
                "years_old",
                "yearsOld"
            ))
            {
                return Math.Max(0, direct);
            }

            return 0;
        }

        private static int GetActorExpectedLifespanYearsSafe(Actor actor)
        {
            if (actor == null)
            {
                return 0;
            }

            string[] methods =
            {
                "getMaxAge",
                "getLifespan",
                "getLifeSpan",
                "getExpectedLifespan"
            };
            for (int i = 0; i < methods.Length; i++)
            {
                int value = TryInvokeIntMethod(actor, methods[i], -1);
                if (value > 0)
                {
                    return value;
                }
            }

            int valueFromObject = GetExpectedLifespanFromObjectSafe(actor);
            if (valueFromObject > 0)
            {
                return valueFromObject;
            }

            object asset = GetMemberValue(actor, "asset", "_asset");
            valueFromObject = GetExpectedLifespanFromObjectSafe(asset);
            if (valueFromObject > 0)
            {
                return valueFromObject;
            }

            object race = GetMemberValue(
                actor,
                "race",
                "_race",
                "species",
                "_species"
            );
            if (race == null)
            {
                race = GetMemberValue(
                    asset,
                    "race",
                    "_race",
                    "species",
                    "_species"
                );
            }
            return GetExpectedLifespanFromObjectSafe(race);
        }

        private static int GetExpectedLifespanFromObjectSafe(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int value;
            if (TryGetIntMember(
                obj,
                out value,
                "lifespan",
                "life_span",
                "lifeSpan",
                "max_age",
                "maxAge",
                "age_max",
                "ageMax",
                "expected_lifespan",
                "expectedLifespan"
            ))
            {
                return Math.Max(0, value);
            }
            return 0;
        }

        private static int GetKingdomSubjectExpectedLifespanYearsSafe(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return 0;
            }

            long total = 0;
            int count = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (int c = 0;
                c < cities.Count && count < LeaderSubjectLifespanSampleLimit;
                c++)
            {
                List<Actor> units = GetCityUnitsSafe(cities[c], LeaderSubjectLifespanSampleLimit - count);
                for (int i = 0;
                    i < units.Count && count < LeaderSubjectLifespanSampleLimit;
                    i++)
                {
                    Actor actor = units[i];
                    if (actor == null || !actor.isAlive())
                    {
                        continue;
                    }

                    int lifespan = GetActorExpectedLifespanYearsSafe(actor);
                    if (lifespan <= 0)
                    {
                        continue;
                    }

                    total += lifespan;
                    count++;
                }
            }

            if (count <= 0)
            {
                return 0;
            }
            return (int)Math.Max(1L, total / count);
        }

        private static void UpdateStabilitySystem()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];

                if (
                    kingdom == null ||
                    kingdom.data == null
                )
                {
                    continue;
                }

                int currentNational =
                    GetNationalStability(kingdom);

                Actor ruler = GetLivingRuler(kingdom);
                string rulerIdentity =
                    GetStableObjectIdentity(ruler);

                string previousRulerIdentity = "";

                try
                {
                    kingdom.data.get(
                        LastRulerIdentityDataKey,
                        out previousRulerIdentity,
                        ""
                    );
                }
                catch
                {
                    previousRulerIdentity = "";
                }

                if (
                    !string.IsNullOrEmpty(previousRulerIdentity) &&
                    !string.IsNullOrEmpty(rulerIdentity) &&
                    previousRulerIdentity != rulerIdentity
                )
                {
                    currentNational = ClampInt(
                        currentNational - 6,
                        0,
                        100
                    );
                }

                if (!string.IsNullOrEmpty(rulerIdentity))
                {
                    try
                    {
                        kingdom.data.set(
                            LastRulerIdentityDataKey,
                            rulerIdentity
                        );
                    }
                    catch
                    {
                    }
                }

                int nationalTarget =
                    CalculateNationalStabilityTarget(kingdom);

                currentNational = MoveTowardsInt(
                    currentNational,
                    nationalTarget,
                    NationalStabilityStep
                );

                SetNationalStability(
                    kingdom,
                    currentNational
                );

                List<City> cities = GetCitiesSafe(kingdom);

                for (int cityIndex = 0;
                    cityIndex < cities.Count;
                    cityIndex++)
                {
                    City city = cities[cityIndex];

                    if (city == null || city.data == null)
                    {
                        continue;
                    }

                    int currentLocal = GetLocalStability(city);
                    string ownerIdentity =
                        GetStableObjectIdentity(kingdom);
                    string previousOwnerIdentity = "";

                    try
                    {
                        city.data.get(
                            LocalStabilityOwnerDataKey,
                            out previousOwnerIdentity,
                            ""
                        );
                    }
                    catch
                    {
                        previousOwnerIdentity = "";
                    }

                    if (
                        !string.IsNullOrEmpty(previousOwnerIdentity) &&
                        !string.IsNullOrEmpty(ownerIdentity) &&
                        previousOwnerIdentity != ownerIdentity
                    )
                    {
                        currentLocal = Math.Min(
                            currentLocal,
                            25
                        );
                    }

                    if (!string.IsNullOrEmpty(ownerIdentity))
                    {
                        try
                        {
                            city.data.set(
                                LocalStabilityOwnerDataKey,
                                ownerIdentity
                            );
                        }
                        catch
                        {
                        }
                    }

                    int localTarget =
                        CalculateLocalStabilityTarget(city);

                    currentLocal = MoveTowardsInt(
                        currentLocal,
                        localTarget,
                        LocalStabilityStep
                    );

                    SetLocalStability(
                        city,
                        currentLocal
                    );
                }
            }

            ProcessRebellions();
        }

        private static int CalculateNationalStabilityTarget(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return DefaultNationalStability;
            }

            int target = 50;
            Actor ruler = GetLivingRuler(kingdom);
            string course = GetKingdomCourse(kingdom);
            string rulerTrait = GetPoliticalTrait(ruler);

            if (ruler == null)
            {
                target -= 15;
            }
            else
            {
                if (
                    !string.IsNullOrEmpty(course) &&
                    rulerTrait == course
                )
                {
                    target += 15;
                }
                else if (
                    !string.IsNullOrEmpty(course) &&
                    !string.IsNullOrEmpty(rulerTrait) &&
                    rulerTrait != course
                )
                {
                    target -= 12;
                }

                target += GetRulerCompetenceBonus(
                    ruler,
                    course
                );
            }

            string stateIdeology = GetStateIdeology(kingdom);

            if (IsValidIdeology(stateIdeology))
            {
                int support = GetKingdomIdeologySupport(
                    kingdom,
                    stateIdeology
                );

                if (support >= 70)
                {
                    target += 10;
                }
                else if (support >= 55)
                {
                    target += 5;
                }
                else if (support < 25)
                {
                    target -= 15;
                }
                else if (support < 40)
                {
                    target -= 8;
                }

                target += GetRulerIdeologyCompatibilityBonus(
                    ruler,
                    stateIdeology
                );

                target += GetIdeologyCurrentStabilityModifier(
                    GetStateIdeologyCurrent(kingdom),
                    support
                );
            }

            target += GetIdeologyBehaviorStabilityModifier(kingdom);

            List<City> cities = GetCitiesSafe(kingdom);
            int cityCount = cities.Count;

            if (cityCount > 4)
            {
                target -= Math.Min(
                    16,
                    (cityCount - 4) * 2
                );
            }

            int hungryCities = 0;
            int fedCities = 0;
            int discontentCities = 0;
            int crisisCities = 0;

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];

                if (GetCityHungrySafe(city) > 0)
                {
                    hungryCities++;
                }

                if (GetCityFoodSafe(city) > 0)
                {
                    fedCities++;
                }

                int localStability = GetLocalStability(city);

                if (localStability < 20)
                {
                    crisisCities++;
                }
                else if (localStability < 40)
                {
                    discontentCities++;
                }
            }

            target += Math.Min(8, fedCities);
            target -= Math.Min(15, hungryCities * 3);

            // Политический кризис в поселениях теперь
            // тянет вниз и стабильность всего государства.
            target -= Math.Min(
                20,
                crisisCities * 4 +
                discontentCities * 2
            );

            // Активный политический кризис сам по себе подрывает
            // доверие к центральной власти. Чем выше давление, тем
            // сильнее национальная нестабильность.
            if (IsPoliticalCrisisActive(kingdom))
            {
                int crisisPressure = GetPoliticalCrisisPressure(kingdom);
                target -= 4 + crisisPressure / 12;
            }

            return ClampInt(target, 0, 100);
        }

        private static int CalculateLocalStabilityTarget(
            City city
        )
        {
            if (city == null)
            {
                return DefaultLocalStability;
            }

            Kingdom kingdom = GetKingdomFromObject(city);

            if (kingdom == null)
            {
                return 0;
            }

            int national = GetNationalStability(kingdom);
            int target = 50 +
                (int)Math.Round((national - 50) * 0.60f);

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            if (capital == city)
            {
                target += 12;
            }

            Actor leader = GetMemberValue(
                city,
                "leader",
                "_leader"
            ) as Actor;

            if (leader != null && leader.stats != null)
            {
                try
                {
                    target += ClampInt(
                        (int)(leader.stats[S.stewardship] / 4f),
                        0,
                        8
                    );
                }
                catch
                {
                }
            }

            int population = GetCityPopulationSafe(city);
            int hungry = GetCityHungrySafe(city);
            int food = GetCityFoodSafe(city);

            if (food > 0)
            {
                target += 5;
            }

            if (population > 0 && hungry > 0)
            {
                float hungryShare = Math.Min(
                    1f,
                    hungry / (float)population
                );

                target -= (int)Math.Round(
                    hungryShare * 20f
                );
            }

            string localDominantIdeology;
            int localDominantSupport;
            int ideologicalTension;
            GetCityIdeologyOverview(
                city,
                out localDominantIdeology,
                out localDominantSupport,
                out ideologicalTension
            );

            if (ideologicalTension <= 20)
            {
                target += 5;
            }
            else if (ideologicalTension <= 40)
            {
                target += 2;
            }
            else if (ideologicalTension <= 60)
            {
                target -= 3;
            }
            else if (ideologicalTension <= 80)
            {
                target -= 7;
            }
            else
            {
                target -= 12;
            }

            string localCurrent = GetCityIdeologyCurrent(
                city,
                localDominantIdeology
            );
            if (!string.IsNullOrEmpty(localCurrent))
            {
                target += GetIdeologyCurrentStabilityModifier(
                    localCurrent,
                    localDominantSupport
                );
            }

            int ideologicalHostility = GetCityIdeologicalHostility(city);

            if (ideologicalHostility <= 20)
            {
                target += 2;
            }
            else if (ideologicalHostility <= 40)
            {
                // Низкая вражда почти не влияет на повседневную жизнь.
            }
            else if (ideologicalHostility <= 60)
            {
                target -= 2;
            }
            else if (ideologicalHostility <= 80)
            {
                target -= 5;
            }
            else
            {
                target -= 8;
            }

            string stateIdeology = GetStateIdeology(kingdom);

            if (IsValidIdeology(stateIdeology))
            {
                int stateSupport = GetCityIdeologySupport(
                    city,
                    stateIdeology
                );

                if (stateSupport >= 70)
                {
                    target += 8;
                }
                else if (stateSupport >= 55)
                {
                    target += 4;
                }
                else if (stateSupport < 25)
                {
                    target -= 10;
                }
                else if (stateSupport < 40)
                {
                    target -= 5;
                }
            }

            // Города, где кризисное движение особенно популярно,
            // сильнее ощущают политическое давление. Это создаёт
            // будущую географию революций и гражданских войн.
            if (IsPoliticalCrisisActive(kingdom))
            {
                string crisisIdeology = GetPoliticalCrisisIdeology(kingdom);
                int crisisPressure = GetPoliticalCrisisPressure(kingdom);

                if (IsValidIdeology(crisisIdeology))
                {
                    int crisisSupport = GetCityIdeologySupport(
                        city,
                        crisisIdeology
                    );

                    if (crisisSupport >= 20)
                    {
                        target -= Math.Min(
                            14,
                            (crisisSupport - 15) / 4 +
                            crisisPressure / 25
                        );
                    }
                }
            }

            int cityCount = GetCitiesSafe(kingdom).Count;

            if (cityCount > 4 && capital != city)
            {
                target -= Math.Min(
                    12,
                    cityCount - 4
                );
            }

            int unstableNeighbours = 0;
            List<City> neighbours = GetNeighbourCitiesSafe(city);

            for (int i = 0; i < neighbours.Count; i++)
            {
                City neighbour = neighbours[i];

                if (
                    GetKingdomFromObject(neighbour) == kingdom &&
                    GetLocalStability(neighbour) < 30
                )
                {
                    unstableNeighbours++;
                }
            }

            target -= Math.Min(
                10,
                unstableNeighbours * 3
            );

            return ClampInt(target, 0, 100);
        }

        private static bool TryInitializeKingdomPoliticalSuccession(
            Kingdom kingdom,
            Actor ruler
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                IsValidIdeology(GetStateIdeology(kingdom))
            )
            {
                return false;
            }

            if (
                GetKingdomIntData(
                    kingdom,
                    SuccessionSchemaDataKey,
                    0
                ) >= SuccessionSchemaVersion
            )
            {
                return false;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            if (cities.Count == 0)
            {
                return false;
            }

            string currentKingdomId =
                GetStableObjectIdentity(kingdom);
            Dictionary<string, long> predecessorWeights =
                new Dictionary<string, long>();

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                if (city == null || city.data == null)
                {
                    continue;
                }

                string previousOwner = GetCityStringData(
                    city,
                    PoliticalMemoryOwnerDataKey,
                    ""
                );

                if (
                    string.IsNullOrEmpty(previousOwner) ||
                    previousOwner == currentKingdomId
                )
                {
                    continue;
                }

                long weight = Math.Max(
                    1,
                    GetCityPopulationSafe(city)
                );

                if (!predecessorWeights.ContainsKey(previousOwner))
                {
                    predecessorWeights[previousOwner] = 0;
                }

                predecessorWeights[previousOwner] += weight;
            }

            string predecessorId = "";
            long predecessorBestWeight = 0;

            foreach (
                KeyValuePair<string, long> pair
                in predecessorWeights
            )
            {
                if (pair.Value > predecessorBestWeight)
                {
                    predecessorBestWeight = pair.Value;
                    predecessorId = pair.Key;
                }
            }

            if (string.IsNullOrEmpty(predecessorId))
            {
                return false;
            }

            Kingdom predecessorKingdom =
                FindKingdomByStableIdentity(predecessorId);
            City originCity = GetSuccessionOriginCity(kingdom);

            return InitializePoliticalSuccession(
                kingdom,
                predecessorKingdom,
                originCity,
                ruler,
                predecessorId
            );
        }

        private static Kingdom FindKingdomByStableIdentity(
            string identity
        )
        {
            if (string.IsNullOrEmpty(identity))
            {
                return null;
            }

            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (
                    kingdom != null &&
                    GetStableObjectIdentity(kingdom) == identity
                )
                {
                    return kingdom;
                }
            }

            return null;
        }

        private static City GetSuccessionOriginCity(
            Kingdom kingdom
        )
        {
            if (kingdom == null)
            {
                return null;
            }

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            if (
                capital != null &&
                GetKingdomFromObject(capital) == kingdom
            )
            {
                return capital;
            }

            List<City> cities = GetCitiesSafe(kingdom);
            City best = null;
            int bestPopulation = -1;

            for (int i = 0; i < cities.Count; i++)
            {
                City city = cities[i];
                int population = GetCityPopulationSafe(city);

                if (best == null || population > bestPopulation)
                {
                    best = city;
                    bestPopulation = population;
                }
            }

            return best;
        }

        private static bool InitializePoliticalSuccession(
            Kingdom successorKingdom,
            Kingdom predecessorKingdom,
            City originCity,
            Actor foundingRuler,
            string predecessorIdentity
        )
        {
            if (
                successorKingdom == null ||
                successorKingdom.data == null
            )
            {
                return false;
            }

            if (
                GetKingdomIntData(
                    successorKingdom,
                    SuccessionSchemaDataKey,
                    0
                ) >= SuccessionSchemaVersion &&
                IsValidIdeology(GetStateIdeology(successorKingdom))
            )
            {
                return true;
            }

            List<City> successorCities =
                GetCitiesSafe(successorKingdom);
            if (successorCities.Count == 0)
            {
                return false;
            }

            if (originCity == null)
            {
                originCity = GetSuccessionOriginCity(
                    successorKingdom
                );
            }

            if (string.IsNullOrEmpty(predecessorIdentity))
            {
                predecessorIdentity =
                    GetStableObjectIdentity(predecessorKingdom);
            }

            string ideology = DetermineSuccessionIdeology(
                successorKingdom,
                predecessorKingdom,
                successorCities,
                originCity
            );

            if (!IsValidIdeology(ideology))
            {
                return false;
            }

            SetStateIdeology(successorKingdom, ideology);
            SetKingdomCourse(
                successorKingdom,
                GetPreferredCourseForIdeology(
                    ideology,
                    "succession"
                )
            );

            Actor ruler = foundingRuler;
            if (
                ruler == null ||
                !ruler.isAlive() ||
                GetKingdomFromObject(ruler) != successorKingdom
            )
            {
                ruler = GetLivingRuler(successorKingdom);
            }

            if (ruler != null)
            {
                AssignPoliticalTrait(
                    ruler,
                    GetPreferredRulerTraitForIdeology(
                        ideology,
                        "succession"
                    )
                );
            }

            int inheritedParties = InheritSuccessorParties(
                successorKingdom,
                predecessorKingdom,
                successorCities,
                originCity,
                ruler
            );

            SetKingdomIntData(
                successorKingdom,
                SuccessionSchemaDataKey,
                SuccessionSchemaVersion
            );
            SetKingdomStringData(
                successorKingdom,
                SuccessionParentKingdomDataKey,
                predecessorIdentity ?? ""
            );
            SetKingdomStringData(
                successorKingdom,
                SuccessionOriginCityDataKey,
                originCity == null
                    ? ""
                    : GetStableObjectIdentity(originCity)
            );
            SetKingdomIntData(
                successorKingdom,
                SuccessionYearDataKey,
                GetWorldYearSafe()
            );
            SetKingdomStringData(
                successorKingdom,
                SuccessionIdeologyDataKey,
                ideology
            );

            if (!string.IsNullOrEmpty(predecessorIdentity))
            {
                string predecessorName =
                    predecessorKingdom == null
                        ? LM.Get("ukiol_succession_previous_state")
                        : GetWorldObjectDisplayName(
                            predecessorKingdom
                        );

                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_political_succession"),
                        GetWorldObjectDisplayName(successorKingdom),
                        predecessorName,
                        GetIdeologyName(ideology),
                        inheritedParties
                    ),
                    successorKingdom,
                    originCity,
                    ruler,
                    GetIdeologyIconPath(ideology),
                    "political_succession_" +
                        GetStableObjectIdentity(successorKingdom),
                    75f
                );
            }

            return true;
        }

        private static string DetermineSuccessionIdeology(
            Kingdom successorKingdom,
            Kingdom predecessorKingdom,
            List<City> cities,
            City originCity
        )
        {
            if (
                successorKingdom == null ||
                cities == null ||
                cities.Count == 0
            )
            {
                return null;
            }

            List<PoliticalParty> predecessorParties =
                predecessorKingdom == null
                    ? new List<PoliticalParty>()
                    : LoadPoliticalPartiesInternal(
                        predecessorKingdom,
                        false
                    );

            int worldYear = GetWorldYearSafe();
            string bestIdeology = null;
            double bestScore = double.MinValue;

            for (int ideologyIndex = 0;
                ideologyIndex < IdeologyIds.Length;
                ideologyIndex++)
            {
                string ideology = IdeologyIds[ideologyIndex];
                double weightedScore = 0d;
                long totalPopulation = 0;

                for (int cityIndex = 0;
                    cityIndex < cities.Count;
                    cityIndex++)
                {
                    City city = cities[cityIndex];
                    if (city == null)
                    {
                        continue;
                    }

                    int population = Math.Max(
                        1,
                        GetCityPopulationSafe(city)
                    );
                    int support = GetCityIdeologySupport(
                        city,
                        ideology
                    );
                    int memory = GetCityPoliticalMemory(
                        city,
                        ideology
                    );
                    int partySupport =
                        GetInheritedIdeologyPartySupport(
                            city,
                            ideology,
                            predecessorParties
                        );

                    double cityScore =
                        support * 0.58d +
                        memory * 0.24d +
                        partySupport * 0.10d;

                    string previousRegime = GetCityStringData(
                        city,
                        PoliticalMemoryRegimeDataKey,
                        ""
                    );

                    if (previousRegime == ideology)
                    {
                        int ownerSince = GetCityIntData(
                            city,
                            PoliticalMemoryOwnerSinceDataKey,
                            worldYear
                        );
                        int tenure = ClampInt(
                            worldYear - ownerSince,
                            0,
                            200
                        );

                        // A long-integrated region keeps a stronger imprint
                        // of the former regime. Recently annexed territory
                        // receives only a small institutional bonus.
                        cityScore += 4d + Math.Min(
                            8d,
                            tenure / 15d
                        );
                    }

                    if (city == originCity)
                    {
                        cityScore += support * 0.06d;
                        cityScore += memory * 0.02d;
                    }

                    weightedScore += cityScore * population;
                    totalPopulation += population;
                }

                double finalScore = totalPopulation <= 0
                    ? 0d
                    : weightedScore / totalPopulation;

                if (finalScore > bestScore)
                {
                    bestScore = finalScore;
                    bestIdeology = ideology;
                }
            }

            return IsValidIdeology(bestIdeology)
                ? bestIdeology
                : null;
        }

        private static int GetInheritedIdeologyPartySupport(
            City city,
            string ideology,
            List<PoliticalParty> predecessorParties
        )
        {
            if (
                city == null ||
                !IsValidIdeology(ideology) ||
                predecessorParties == null
            )
            {
                return 0;
            }

            int total = 0;

            for (int i = 0; i < predecessorParties.Count; i++)
            {
                PoliticalParty party = predecessorParties[i];
                if (
                    party == null ||
                    !party.Active ||
                    party.Ideology != ideology
                )
                {
                    continue;
                }

                int support;
                bool initialized;
                GetLocalPartySupport(
                    city,
                    party,
                    out support,
                    out initialized
                );

                if (initialized)
                {
                    total += support;
                }
            }

            return ClampInt(
                total,
                0,
                GetCityIdeologySupport(city, ideology)
            );
        }




        private sealed class SuccessionPartyCandidate
        {
            public PoliticalParty Parent;
            public int NationalSupport;
            public int MaxLocalSupport;
            public City StrongestCity;
        }

        private static int InheritSuccessorParties(
            Kingdom successorKingdom,
            Kingdom predecessorKingdom,
            List<City> successorCities,
            City originCity,
            Actor foundingRuler
        )
        {
            if (
                successorKingdom == null ||
                predecessorKingdom == null ||
                successorCities == null ||
                successorCities.Count == 0
            )
            {
                return 0;
            }

            EnsurePoliticalPartySchema(successorKingdom);

            // A native game update or another mod may already have created
            // party data for the new state. Never duplicate an existing
            // organization set during succession initialization.
            if (
                LoadPoliticalPartiesInternal(
                    successorKingdom,
                    false
                ).Count > 0
            )
            {
                return 0;
            }

            List<PoliticalParty> predecessorParties =
                LoadPoliticalPartiesInternal(
                    predecessorKingdom,
                    false
                );
            if (predecessorParties.Count == 0)
            {
                return 0;
            }

            List<SuccessionPartyCandidate> candidates =
                new List<SuccessionPartyCandidate>();

            for (int p = 0; p < predecessorParties.Count; p++)
            {
                PoliticalParty parent = predecessorParties[p];
                if (parent == null || !parent.Active)
                {
                    continue;
                }

                long weighted = 0;
                long totalPopulation = 0;
                int maxSupport = 0;
                City strongestCity = null;

                for (int c = 0; c < successorCities.Count; c++)
                {
                    City city = successorCities[c];
                    int support;
                    bool initialized;
                    GetLocalPartySupport(
                        city,
                        parent,
                        out support,
                        out initialized
                    );

                    if (!initialized)
                    {
                        continue;
                    }

                    int population = Math.Max(
                        1,
                        GetCityPopulationSafe(city)
                    );
                    weighted += (long)support * population;
                    totalPopulation += population;

                    if (support > maxSupport)
                    {
                        maxSupport = support;
                        strongestCity = city;
                    }
                }

                int nationalSupport = totalPopulation <= 0
                    ? 0
                    : ClampInt(
                        (int)Math.Round(
                            weighted /
                            (double)totalPopulation
                        ),
                        0,
                        100
                    );

                if (
                    nationalSupport <
                        SuccessionPartyMinimumLocalSupport &&
                    maxSupport <
                        SuccessionPartyMinimumLocalSupport + 8
                )
                {
                    continue;
                }

                SuccessionPartyCandidate candidate =
                    new SuccessionPartyCandidate();
                candidate.Parent = parent;
                candidate.NationalSupport = nationalSupport;
                candidate.MaxLocalSupport = maxSupport;
                candidate.StrongestCity = strongestCity;
                candidates.Add(candidate);
            }

            candidates.Sort(
                delegate(
                    SuccessionPartyCandidate a,
                    SuccessionPartyCandidate b
                )
                {
                    int supportCompare =
                        b.NationalSupport.CompareTo(
                            a.NationalSupport
                        );
                    if (supportCompare != 0)
                    {
                        return supportCompare;
                    }

                    return b.MaxLocalSupport.CompareTo(
                        a.MaxLocalSupport
                    );
                }
            );

            Dictionary<string, int> ideologyCounts =
                new Dictionary<string, int>();
            HashSet<string> reservedLeaders =
                new HashSet<string>();
            int inherited = 0;

            for (int i = 0;
                i < candidates.Count &&
                inherited < SuccessionMaxInheritedParties;
                i++)
            {
                SuccessionPartyCandidate candidate =
                    candidates[i];
                PoliticalParty parent = candidate.Parent;

                int ideologyCount = 0;
                ideologyCounts.TryGetValue(
                    parent.Ideology,
                    out ideologyCount
                );
                if (
                    ideologyCount >=
                    MaxPoliticalPartiesPerIdeology
                )
                {
                    continue;
                }

                Actor leader = FindPartyActorByIdentity(
                    successorKingdom,
                    parent.LeaderIdentity,
                    parent.LeaderName
                );

                if (
                    leader != null &&
                    reservedLeaders.Contains(
                        GetStableObjectIdentity(leader)
                    )
                )
                {
                    leader = null;
                }

                if (leader == null)
                {
                    leader = FindPartyLeaderActor(
                        successorKingdom,
                        parent.Ideology,
                        reservedLeaders
                    );
                }

                if (leader == null)
                {
                    continue;
                }

                City successorOrigin =
                    candidate.StrongestCity ?? originCity;

                PoliticalParty created =
                    CreatePoliticalPartySlot(
                        successorKingdom,
                        parent.Ideology,
                        parent.Radicalism,
                        parent.NameVariant,
                        GetWorldYearSafe(),
                        leader,
                        GetWorldObjectDisplayName(leader),
                        false
                    );

                if (created == null)
                {
                    continue;
                }

                CopySuccessorPartyIdentity(
                    successorKingdom,
                    created,
                    parent,
                    successorOrigin
                );

                RecordPartyHistoryEvent(
                    successorKingdom,
                    created,
                    PartyHistorySuccession,
                    parent.Name,
                    "",
                    GetWorldYearSafe()
                );

                for (int c = 0;
                    c < successorCities.Count;
                    c++)
                {
                    City city = successorCities[c];
                    int inheritedSupport;
                    bool initialized;
                    GetLocalPartySupport(
                        city,
                        parent,
                        out inheritedSupport,
                        out initialized
                    );

                    if (!initialized)
                    {
                        continue;
                    }

                    SetLocalPartySupport(
                        city,
                        created,
                        Math.Min(
                            inheritedSupport,
                            GetCityIdeologySupport(
                                city,
                                created.Ideology
                            )
                        )
                    );
                }

                string leaderIdentity =
                    GetStableObjectIdentity(leader);
                if (!string.IsNullOrEmpty(leaderIdentity))
                {
                    reservedLeaders.Add(leaderIdentity);
                }

                ideologyCounts[parent.Ideology] =
                    ideologyCount + 1;
                inherited++;
            }

            if (inherited > 0)
            {
                List<PoliticalParty> inheritedParties =
                    LoadPoliticalPartiesInternal(
                        successorKingdom,
                        false
                    );
                AggregatePartySupportFromCities(
                    successorKingdom,
                    inheritedParties
                );
                SyncLegacyPartyCompatibility(
                    successorKingdom,
                    inheritedParties
                );
            }

            return inherited;
        }

        private static void CopySuccessorPartyIdentity(
            Kingdom successorKingdom,
            PoliticalParty created,
            PoliticalParty parent,
            City successorOrigin
        )
        {
            if (
                successorKingdom == null ||
                created == null ||
                parent == null
            )
            {
                return;
            }

            created.ParentPartyId = parent.Id;
            created.ParentPartyName = parent.Name;
            created.Position = parent.Position;
            created.Strategy = parent.Strategy;
            created.ForeignStance = parent.ForeignStance;
            created.SupportBias = ClampInt(
                parent.SupportBias,
                60,
                140
            );
            created.ColorSeed = NormalizePartyColorSeed(
                parent.ColorSeed
            );
            created.Traits = parent.Traits == null
                ? new List<string>()
                : new List<string>(parent.Traits);

            if (successorOrigin != null)
            {
                created.OriginCityId =
                    GetStableObjectIdentity(successorOrigin);
                created.OriginCityName =
                    GetWorldObjectDisplayName(successorOrigin);
            }

            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2ParentPartyIdPrefix,
                    created.Slot
                ),
                created.ParentPartyId
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2ParentPartyNamePrefix,
                    created.Slot
                ),
                created.ParentPartyName
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2PositionPrefix,
                    created.Slot
                ),
                created.Position
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2StrategyPrefix,
                    created.Slot
                ),
                created.Strategy
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2ForeignStancePrefix,
                    created.Slot
                ),
                created.ForeignStance
            );
            SetKingdomIntData(
                successorKingdom,
                PartySlotKey(
                    PartyV2SupportBiasPrefix,
                    created.Slot
                ),
                created.SupportBias
            );
            SetKingdomIntData(
                successorKingdom,
                PartySlotKey(
                    PartyV2ColorSeedPrefix,
                    created.Slot
                ),
                created.ColorSeed
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2TraitsPrefix,
                    created.Slot
                ),
                SerializePartyTraits(created.Traits)
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2OriginCityIdPrefix,
                    created.Slot
                ),
                created.OriginCityId ?? ""
            );
            SetKingdomStringData(
                successorKingdom,
                PartySlotKey(
                    PartyV2OriginCityNamePrefix,
                    created.Slot
                ),
                created.OriginCityName ?? ""
            );
        }

        private static void ProcessRebellions()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int kingdomIndex = 0;
                kingdomIndex < kingdoms.Count;
                kingdomIndex++)
            {
                Kingdom originalKingdom =
                    kingdoms[kingdomIndex];

                if (
                    originalKingdom == null ||
                    originalKingdom.data == null
                )
                {
                    continue;
                }

                List<City> cities =
                    GetCitiesSafe(originalKingdom);

                if (cities.Count <= 1)
                {
                    continue;
                }

                City capital = GetMemberValue(
                    originalKingdom,
                    "capital",
                    "_capital"
                ) as City;

                for (int cityIndex = 0;
                    cityIndex < cities.Count;
                    cityIndex++)
                {
                    City seed = cities[cityIndex];

                    if (
                        seed == null ||
                        seed == capital ||
                        GetKingdomFromObject(seed) != originalKingdom
                    )
                    {
                        continue;
                    }

                    int localStability =
                        GetLocalStability(seed);

                    if (localStability > RebellionThreshold)
                    {
                        continue;
                    }

                    float nextAttempt = 0f;
                    NextRebellionAttemptTime.TryGetValue(
                        seed,
                        out nextAttempt
                    );

                    if (Time.time < nextAttempt)
                    {
                        continue;
                    }

                    NextRebellionAttemptTime[seed] =
                        Time.time + RebellionAttemptCooldown;

                    int nationalStability =
                        GetNationalStability(originalKingdom);

                    float chance = CalculateRebellionChance(
                        localStability,
                        nationalStability
                    );

                    if (UnityEngine.Random.value > chance)
                    {
                        continue;
                    }

                    Actor rebelLeader =
                        GetRebellionLeader(seed);

                    if (rebelLeader == null)
                    {
                        continue;
                    }

                    List<City> originalCities =
                        new List<City>(cities);

                    Kingdom rebelKingdom =
                        StartCityRebellion(
                            seed,
                            rebelLeader,
                            originalKingdom
                        );

                    if (
                        rebelKingdom == null ||
                        rebelKingdom == originalKingdom
                    )
                    {
                        continue;
                    }

                    SetNationalStability(
                        rebelKingdom,
                        35
                    );

                    SetLocalStability(
                        seed,
                        45
                    );

                    JoinNeighbouringRebelCities(
                        seed,
                        originalKingdom,
                        rebelKingdom,
                        originalCities
                    );

                    InitializePoliticalSuccession(
                        rebelKingdom,
                        originalKingdom,
                        seed,
                        rebelLeader,
                        GetStableObjectIdentity(originalKingdom)
                    );

                    PublishPoliticalEvent(
                        string.Format(
                            LM.Get("ukiol_event_rebellion_started"),
                            GetWorldObjectDisplayName(originalKingdom),
                            GetWorldObjectDisplayName(seed),
                            GetWorldObjectDisplayName(rebelKingdom)
                        ),
                        originalKingdom,
                        seed,
                        rebelLeader,
                        GetIdeologyIconPath(GetStateIdeology(originalKingdom)),
                        "rebellion_started_" + GetStableObjectIdentity(seed),
                        90f
                    );

                    // Одно восстание за один проход достаточно:
                    // следующий кризис получит собственный шанс
                    // на следующем тике системы.
                    break;
                }
            }
        }

        private static float CalculateRebellionChance(
            int localStability,
            int nationalStability
        )
        {
            if (localStability <= 0)
            {
                return 1f;
            }

            float chance;

            if (localStability <= 5)
            {
                chance = 0.45f;
            }
            else if (localStability <= 10)
            {
                chance = 0.22f;
            }
            else
            {
                chance = 0.08f;
            }

            if (nationalStability < 20)
            {
                chance += 0.18f;
            }
            else if (nationalStability < 35)
            {
                chance += 0.08f;
            }
            else if (nationalStability >= 65)
            {
                chance -= 0.04f;
            }

            return Math.Max(
                0.01f,
                Math.Min(1f, chance)
            );
        }

        private static string GetRebellionRiskText(
            City city,
            int localStability,
            int nationalStability
        )
        {
            Kingdom kingdom = GetKingdomFromObject(city);

            if (
                kingdom == null ||
                GetCitiesSafe(kingdom).Count <= 1
            )
            {
                return LM.Get("ukiol_rebellion_risk_none");
            }

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            if (capital == city)
            {
                return LM.Get("ukiol_rebellion_risk_capital");
            }

            if (localStability > RebellionThreshold)
            {
                return LM.Get("ukiol_rebellion_risk_low");
            }

            float chance = CalculateRebellionChance(
                localStability,
                nationalStability
            );

            if (chance >= 0.60f)
            {
                return LM.Get(
                    "ukiol_rebellion_risk_imminent"
                );
            }

            if (chance >= 0.30f)
            {
                return LM.Get(
                    "ukiol_rebellion_risk_critical"
                );
            }

            if (chance >= 0.12f)
            {
                return LM.Get(
                    "ukiol_rebellion_risk_high"
                );
            }

            return LM.Get(
                "ukiol_rebellion_risk_growing"
            );
        }

        private static string GetRebellionRiskColor(
            City city,
            int localStability,
            int nationalStability
        )
        {
            Kingdom kingdom = GetKingdomFromObject(city);

            if (
                kingdom == null ||
                GetCitiesSafe(kingdom).Count <= 1
            )
            {
                return "#AAAAAA";
            }

            City capital = GetMemberValue(
                kingdom,
                "capital",
                "_capital"
            ) as City;

            if (capital == city)
            {
                return "#AAAAAA";
            }

            float chance = CalculateRebellionChance(
                localStability,
                nationalStability
            );

            if (localStability > RebellionThreshold)
            {
                return "#43FF43";
            }

            if (chance >= 0.60f)
            {
                return "#FF3030";
            }

            if (chance >= 0.30f)
            {
                return "#FF4F4F";
            }

            if (chance >= 0.12f)
            {
                return "#FF9C43";
            }

            return "#FFD45A";
        }

        private static Actor GetRebellionLeader(
            City city
        )
        {
            if (city == null)
            {
                return null;
            }

            Actor leader = GetMemberValue(
                city,
                "leader",
                "_leader"
            ) as Actor;

            if (
                leader != null &&
                leader.isAlive()
            )
            {
                return leader;
            }

            List<Actor> units = GetCityUnitsSafe(city);

            for (int i = 0; i < units.Count; i++)
            {
                Actor actor = units[i];

                if (
                    actor != null &&
                    actor.isAlive()
                )
                {
                    return actor;
                }
            }

            return null;
        }

        private static Kingdom StartCityRebellion(
            City city,
            Actor rebelLeader,
            Kingdom originalKingdom
        )
        {
            if (
                city == null ||
                rebelLeader == null ||
                originalKingdom == null
            )
            {
                return null;
            }

            // Сначала пробуем родной механизм WorldBox:
            // City.useInspire(Actor) сам создаёт новое
            // государство и начинает войну за независимость.
            try
            {
                MethodInfo inspireMethod =
                    FindMethodByNameAndParameterCount(
                        city.GetType(),
                        "useInspire",
                        1
                    );

                if (inspireMethod != null)
                {
                    inspireMethod.Invoke(
                        city,
                        new object[]
                        {
                            rebelLeader
                        }
                    );

                    Kingdom result =
                        GetKingdomFromObject(city);

                    if (
                        result != null &&
                        result != originalKingdom
                    )
                    {
                        return result;
                    }
                }
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Native rebellion call failed: " +
                    exception.Message
                );
            }

            // Fallback на более низкоуровневый метод.
            try
            {
                MethodInfo makeOwnMethod =
                    FindMethodByNameAndParameterCount(
                        city.GetType(),
                        "makeOwnKingdom",
                        3
                    );

                if (makeOwnMethod == null)
                {
                    return null;
                }

                object result = makeOwnMethod.Invoke(
                    city,
                    new object[]
                    {
                        rebelLeader,
                        true,
                        false
                    }
                );

                Kingdom rebelKingdom =
                    result as Kingdom;

                if (rebelKingdom == null)
                {
                    rebelKingdom =
                        GetKingdomFromObject(city);
                }

                if (
                    rebelKingdom == null ||
                    rebelKingdom == originalKingdom
                )
                {
                    return null;
                }

                TryStartRebellionWar(
                    originalKingdom,
                    rebelKingdom
                );

                return rebelKingdom;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Fallback rebellion creation failed: " +
                    exception.Message
                );

                return null;
            }
        }

        private static void JoinNeighbouringRebelCities(
            City seed,
            Kingdom originalKingdom,
            Kingdom rebelKingdom,
            List<City> originalCities
        )
        {
            if (
                seed == null ||
                originalKingdom == null ||
                rebelKingdom == null ||
                originalCities == null
            )
            {
                return;
            }

            List<City> joined = new List<City>();
            joined.Add(seed);

            bool added;

            do
            {
                added = false;

                for (int i = 0;
                    i < originalCities.Count;
                    i++)
                {
                    City candidate = originalCities[i];

                    if (
                        candidate == null ||
                        joined.Contains(candidate) ||
                        GetKingdomFromObject(candidate) !=
                            originalKingdom ||
                        GetLocalStability(candidate) >
                            RebellionJoinThreshold
                    )
                    {
                        continue;
                    }

                    City capital = GetMemberValue(
                        originalKingdom,
                        "capital",
                        "_capital"
                    ) as City;

                    if (candidate == capital)
                    {
                        continue;
                    }

                    bool touchesRebellion = false;

                    for (int joinedIndex = 0;
                        joinedIndex < joined.Count;
                        joinedIndex++)
                    {
                        if (AreNeighbourCities(
                            candidate,
                            joined[joinedIndex]
                        ))
                        {
                            touchesRebellion = true;
                            break;
                        }
                    }

                    if (!touchesRebellion)
                    {
                        continue;
                    }

                    if (JoinCityToRebelKingdom(
                        candidate,
                        rebelKingdom
                    ))
                    {
                        joined.Add(candidate);
                        SetLocalStability(
                            candidate,
                            40
                        );
                        added = true;
                    }
                }
            }
            while (added);
        }

        private static bool JoinCityToRebelKingdom(
            City city,
            Kingdom rebelKingdom
        )
        {
            try
            {
                MethodInfo method =
                    FindMethodByNameAndParameterCount(
                        city.GetType(),
                        "joinAnotherKingdom",
                        3
                    );

                if (method == null)
                {
                    return false;
                }

                method.Invoke(
                    city,
                    new object[]
                    {
                        rebelKingdom,
                        false,
                        true
                    }
                );

                return GetKingdomFromObject(city) ==
                    rebelKingdom;
            }
            catch
            {
                return false;
            }
        }

        private static bool AreNeighbourCities(
            City first,
            City second
        )
        {
            if (
                first == null ||
                second == null ||
                first == second
            )
            {
                return false;
            }

            object collection = GetMemberValue(
                first,
                "neighbours_cities",
                "neighbors_cities",
                "_neighbours_cities",
                "_neighbors_cities"
            );

            IEnumerable enumerable =
                collection as IEnumerable;

            if (enumerable == null)
            {
                return false;
            }

            foreach (object item in enumerable)
            {
                if (ReferenceEquals(item, second))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<City> GetNeighbourCitiesSafe(
            City city
        )
        {
            List<City> result = new List<City>();

            if (city == null)
            {
                return result;
            }

            object collection = GetMemberValue(
                city,
                "neighbours_cities",
                "neighbors_cities",
                "_neighbours_cities",
                "_neighbors_cities"
            );

            AddCitiesFromCollection(
                collection,
                result
            );

            return result;
        }

        private static List<Actor> GetCityUnitsSafe(
            City city
        )
        {
            return GetCityUnitsSafe(city, int.MaxValue);
        }

        /// <summary>
        /// Reads at most maxItems actors from one city. Several political
        /// leader/candidate searches intentionally inspect only a small sample;
        /// capping the copy here prevents a city with thousands of spawned
        /// humans from allocating/scanning the full population first.
        /// </summary>
        private static List<Actor> GetCityUnitsSafe(
            City city,
            int maxItems
        )
        {
            List<Actor> result = new List<Actor>();

            if (city == null || maxItems <= 0)
            {
                return result;
            }

            object collection = GetMemberValue(
                city,
                KingdomUnitCollectionMemberNames
            );

            AddActorsFromCollection(
                collection,
                result,
                maxItems
            );

            return result;
        }







        private static MethodInfo FindMethodByNameAndParameterCount(
            Type type,
            string methodName,
            int parameterCount
        )
        {
            if (
                type == null ||
                string.IsNullOrEmpty(methodName)
            )
            {
                return null;
            }

            for (
                Type current = type;
                current != null;
                current = current.BaseType
            )
            {
                MethodInfo[] methods =
                    current.GetMethods(MemberFlags);

                for (int i = 0; i < methods.Length; i++)
                {
                    if (
                        methods[i].Name == methodName &&
                        methods[i].GetParameters().Length ==
                            parameterCount
                    )
                    {
                        return methods[i];
                    }
                }
            }

            return null;
        }


        private static int GetRulerCompetenceBonus(
            Actor ruler,
            string course
        )
        {
            if (ruler == null || ruler.stats == null)
            {
                return 0;
            }

            try
            {
                float stat = ruler.stats[S.stewardship];

                if (course == MilitaristTraitId)
                {
                    stat = ruler.stats[S.warfare];
                }
                else if (course == DiplomatTraitId)
                {
                    stat = ruler.stats[S.diplomacy];
                }

                return ClampInt(
                    (int)(stat / 4f),
                    0,
                    10
                );
            }
            catch
            {
                return 0;
            }
        }

        private static int GetNationalStability(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return DefaultNationalStability;
            }

            int stability = DefaultNationalStability;

            try
            {
                kingdom.data.get(
                    NationalStabilityDataKey,
                    out stability,
                    DefaultNationalStability
                );
            }
            catch
            {
                stability = DefaultNationalStability;
            }

            return ClampInt(stability, 0, 100);
        }

        private static void SetNationalStability(
            Kingdom kingdom,
            int stability
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            try
            {
                kingdom.data.set(
                    NationalStabilityDataKey,
                    ClampInt(stability, 0, 100)
                );
            }
            catch
            {
            }
        }

        private static int GetLocalStability(
            City city
        )
        {
            if (city == null || city.data == null)
            {
                return DefaultLocalStability;
            }

            int stability = DefaultLocalStability;

            try
            {
                city.data.get(
                    LocalStabilityDataKey,
                    out stability,
                    DefaultLocalStability
                );
            }
            catch
            {
                stability = DefaultLocalStability;
            }

            return ClampInt(stability, 0, 100);
        }

        private static void SetLocalStability(
            City city,
            int stability
        )
        {
            if (city == null || city.data == null)
            {
                return;
            }

            try
            {
                city.data.set(
                    LocalStabilityDataKey,
                    ClampInt(stability, 0, 100)
                );
            }
            catch
            {
            }
        }

        private static string FormatStabilityValue(
            int stability,
            int target,
            bool local
        )
        {
            string trend = "→";

            if (target > stability + 1)
            {
                trend = "↑";
            }
            else if (target < stability - 1)
            {
                trend = "↓";
            }

            string statusKey;

            if (local)
            {
                if (stability >= 80)
                {
                    statusKey = "ukiol_stability_local_very_stable";
                }
                else if (stability >= 60)
                {
                    statusKey = "ukiol_stability_stable";
                }
                else if (stability >= 40)
                {
                    statusKey = "ukiol_stability_tense";
                }
                else if (stability >= 20)
                {
                    statusKey = "ukiol_stability_discontent";
                }
                else if (stability > 0)
                {
                    statusKey = "ukiol_stability_crisis";
                }
                else
                {
                    statusKey = "ukiol_stability_revolt";
                }
            }
            else
            {
                if (stability >= 70)
                {
                    statusKey = "ukiol_stability_stable";
                }
                else if (stability >= 40)
                {
                    statusKey = "ukiol_stability_normal";
                }
                else if (stability >= 20)
                {
                    statusKey = "ukiol_stability_tense";
                }
                else
                {
                    statusKey = "ukiol_stability_crisis";
                }
            }

            return stability.ToString() +
                "% " +
                trend +
                " — " +
                LM.Get(statusKey);
        }

        private static string GetStabilityColor(
            int stability
        )
        {
            if (stability >= 70)
            {
                return "#43FF43";
            }

            if (stability >= 40)
            {
                return "#FFD45A";
            }

            if (stability >= 20)
            {
                return "#FF9C43";
            }

            return "#FF4F4F";
        }

        private static int MoveTowardsInt(
            int current,
            int target,
            int maxDelta
        )
        {
            if (current < target)
            {
                return Math.Min(
                    target,
                    current + maxDelta
                );
            }

            if (current > target)
            {
                return Math.Max(
                    target,
                    current - maxDelta
                );
            }

            return current;
        }

        private static int ClampInt(
            int value,
            int min,
            int max
        )
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string GetStableObjectIdentity(
            object obj
        )
        {
            if (obj == null)
            {
                return "";
            }

            object id = GetMemberValue(
                obj,
                "id",
                "_id"
            );

            if (id != null)
            {
                return id.ToString();
            }

            return obj.GetHashCode().ToString();
        }

        private static int GetCityPopulationSafe(
            City city
        )
        {
            if (city == null)
            {
                return 0;
            }

            object status = GetMemberValue(
                city,
                "status",
                "_status"
            );

            int value;

            if (TryGetIntMember(
                status,
                out value,
                "population"
            ))
            {
                return Math.Max(0, value);
            }

            return TryInvokeIntMethod(
                city,
                "getPopulationPeople",
                0
            );
        }

        private static int GetCityHungrySafe(
            City city
        )
        {
            if (city == null)
            {
                return 0;
            }

            object status = GetMemberValue(
                city,
                "status",
                "_status"
            );

            int value;

            if (TryGetIntMember(
                status,
                out value,
                "hungry"
            ))
            {
                return Math.Max(0, value);
            }

            return 0;
        }

        private static int GetCityFoodSafe(
            City city
        )
        {
            int food = TryInvokeIntMethod(
                city,
                "countFoodTotal",
                -1
            );

            if (food >= 0)
            {
                return food;
            }

            return Math.Max(
                0,
                TryInvokeIntMethod(
                    city,
                    "getTotalFood",
                    0
                )
            );
        }

        private static bool TryGetIntMember(
            object owner,
            out int value,
            params string[] memberNames
        )
        {
            value = 0;
            object raw = GetMemberValue(
                owner,
                memberNames
            );

            if (raw == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int TryInvokeIntMethod(
            object owner,
            string methodName,
            int fallback
        )
        {
            if (
                owner == null ||
                string.IsNullOrEmpty(methodName)
            )
            {
                return fallback;
            }

            try
            {
                MethodInfo method = owner.GetType().GetMethod(
                    methodName,
                    MemberFlags,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (method == null)
                {
                    return fallback;
                }

                object result = method.Invoke(
                    owner,
                    null
                );

                if (result == null)
                {
                    return fallback;
                }

                return Convert.ToInt32(result);
            }
            catch
            {
                return fallback;
            }
        }

        private static void ApplyKingdomEconomyEffects()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                Actor ruler = GetLivingRuler(kingdom);

                if (ruler == null)
                {
                    continue;
                }

                string policy = GetKingdomCourse(kingdom);

                // dev11 ideology behaviour applies even when the ruler has
                // no explicit Reformer/Militarist/Diplomat state course.
                // A null course simply means no extra course resource delta.

                // These values are kingdom-wide and do not change while
                // iterating its cities. Compute them once per economy tick.
                string ideology = GetStateIdeology(kingdom);
                int ideologySupport = IsValidIdeology(ideology)
                    ? GetKingdomIdeologySupport(kingdom, ideology)
                    : 0;
                IdeologyBehaviorProfile behavior = ideologySupport >= 45
                    ? GetIdeologyBehaviorProfile(kingdom)
                    : null;

                List<City> cities = GetCitiesSafe(kingdom);

                for (int cityIndex = 0;
                    cityIndex < cities.Count;
                    cityIndex++)
                {
                    City city = cities[cityIndex];

                    if (policy == ReformerTraitId)
                    {
                        TryChangeCityResource(
                            city,
                            "gold",
                            ReformerGoldPerCity
                        );

                        TryChangeCityResource(
                            city,
                            "bread",
                            ReformerBreadPerCity
                        );
                    }
                    else if (policy == MilitaristTraitId)
                    {
                        TryChangeCityResource(
                            city,
                            "gold",
                            MilitaristGoldPerCity
                        );
                    }
                    else if (policy == DiplomatTraitId)
                    {
                        TryChangeCityResource(
                            city,
                            "gold",
                            DiplomatGoldPerCity
                        );
                    }

                    if (behavior != null)
                    {
                        if (behavior.Market >= 70)
                        {
                            TryChangeCityResource(city, "gold", 1);
                        }
                        else if (behavior.Primitivist)
                        {
                            TryChangeCityResource(city, "gold", -1);
                        }

                        if (behavior.Welfare >= 70)
                        {
                            TryChangeCityResource(city, "bread", 1);
                        }

                        if (
                            behavior.Market <= 25 &&
                            behavior.Centralization >= 65
                        )
                        {
                            TryChangeCityResource(city, "gold", -1);
                        }
                    }
                }
            }
        }

        private static Actor GetLivingRuler(Kingdom kingdom)
        {
            if (
                kingdom == null ||
                kingdom.king == null ||
                !kingdom.king.isAlive()
            )
            {
                return null;
            }

            return kingdom.king;
        }

        private static string GetPoliticalTrait(Actor ruler)
        {
            if (ruler == null)
            {
                return null;
            }

            if (ruler.hasTrait(ReformerTraitId))
            {
                return ReformerTraitId;
            }

            if (ruler.hasTrait(MilitaristTraitId))
            {
                return MilitaristTraitId;
            }

            if (ruler.hasTrait(DiplomatTraitId))
            {
                return DiplomatTraitId;
            }

            return null;
        }

        private static bool IsValidCourse(string course)
        {
            return
                course == ReformerTraitId ||
                course == MilitaristTraitId ||
                course == DiplomatTraitId;
        }

        private static string GetKingdomCourse(
            Kingdom kingdom
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null
            )
            {
                return null;
            }

            string cachedCourse;
            if (
                FastKingdomCourseCache.TryGetValue(kingdom, out cachedCourse) &&
                IsValidCourse(cachedCourse)
            )
            {
                return cachedCourse;
            }

            string course = "";

            try
            {
                kingdom.data.get(
                    StateCourseDataKey,
                    out course,
                    ""
                );
            }
            catch
            {
                return null;
            }

            if (!IsValidCourse(course))
            {
                return null;
            }

            FastKingdomCourseCache[kingdom] = course;
            return course;
        }

        private static bool SetKingdomCourse(
            Kingdom kingdom,
            string course
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidCourse(course)
            )
            {
                return false;
            }

            try
            {
                kingdom.data.set(
                    StateCourseDataKey,
                    course
                );

                LastKnownKingdomCourses[kingdom] =
                    course;
                FastKingdomCourseCache[kingdom] =
                    course;

                RefreshKingdomUnitStats(kingdom);
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Could not save state course: " +
                    exception.Message
                );

                return false;
            }
        }

        private static List<Kingdom> GetKingdomsSafe()
        {
            List<Kingdom> result = new List<Kingdom>();

            if (
                World.world == null ||
                World.world.kingdoms == null
            )
            {
                return result;
            }

            object manager = World.world.kingdoms;

            if (AddKingdomsFromCollection(manager, result))
            {
                return result;
            }

            FindCollectionMembers(
                manager,
                KingdomManagerCollectionMemberNames,
                value => AddKingdomsFromCollection(
                    value,
                    result
                )
            );

            return result;
        }

        private static List<City> GetCitiesSafe(
            Kingdom kingdom
        )
        {
            List<City> result = new List<City>();

            if (kingdom == null)
            {
                return result;
            }

            if (AddCitiesFromCollection(kingdom, result))
            {
                return result;
            }

            FindCollectionMembers(
                kingdom,
                KingdomCityCollectionMemberNames,
                value => AddCitiesFromCollection(
                    value,
                    result
                )
            );

            return result;
        }

        private static List<Actor> GetKingdomUnitsSafe(
            Kingdom kingdom
        )
        {
            List<Actor> result = new List<Actor>();

            if (kingdom == null)
            {
                return result;
            }

            if (AddActorsFromCollection(kingdom, result))
            {
                return result;
            }

            FindCollectionMembers(
                kingdom,
                KingdomUnitCollectionMemberNames,
                value => AddActorsFromCollection(
                    value,
                    result
                )
            );

            return result;
        }

        private static void FindCollectionMembers(
            object owner,
            string[] preferredMemberNames,
            Func<object, bool> collectionReader
        )
        {
            if (owner == null || collectionReader == null)
            {
                return;
            }

            Type ownerType = owner.GetType();

            for (int i = 0;
                i < preferredMemberNames.Length;
                i++)
            {
                object value = GetMemberValue(
                    owner,
                    preferredMemberNames[i]
                );

                if (collectionReader(value))
                {
                    return;
                }
            }

            for (
                Type currentType = ownerType;
                currentType != null;
                currentType = currentType.BaseType
            )
            {
                FieldInfo[] fields =
                    currentType.GetFields(MemberFlags);

                for (int i = 0; i < fields.Length; i++)
                {
                    object value = null;

                    try
                    {
                        value = fields[i].GetValue(owner);
                    }
                    catch
                    {
                        value = null;
                    }

                    if (collectionReader(value))
                    {
                        return;
                    }
                }

                PropertyInfo[] properties =
                    currentType.GetProperties(MemberFlags);

                for (int i = 0;
                    i < properties.Length;
                    i++)
                {
                    if (
                        properties[i]
                            .GetIndexParameters()
                            .Length != 0
                    )
                    {
                        continue;
                    }

                    object value = null;

                    try
                    {
                        value = properties[i]
                            .GetValue(owner, null);
                    }
                    catch
                    {
                        value = null;
                    }

                    if (collectionReader(value))
                    {
                        return;
                    }
                }
            }
        }

        private static bool AddKingdomsFromCollection(
            object collection,
            List<Kingdom> result
        )
        {
            return AddItemsFromCollection(
                collection,
                result
            );
        }

        private static bool AddCitiesFromCollection(
            object collection,
            List<City> result
        )
        {
            return AddItemsFromCollection(
                collection,
                result
            );
        }

        private static bool AddActorsFromCollection(
            object collection,
            List<Actor> result,
            int maxItems = int.MaxValue
        )
        {
            return AddItemsFromCollection(
                collection,
                result,
                maxItems,
                true
            );
        }

        private static bool AddItemsFromCollection<T>(
            object collection,
            List<T> result,
            int maxItems = int.MaxValue,
            bool useHashSet = false
        ) where T : class
        {
            if (
                collection == null ||
                collection is string ||
                result == null ||
                maxItems <= 0
            )
            {
                return false;
            }

            IEnumerable enumerable =
                collection as IEnumerable;

            if (enumerable == null)
            {
                return false;
            }

            bool foundAny = false;
            HashSet<T> seen = useHashSet
                ? (result.Count == 0
                    ? new HashSet<T>()
                    : new HashSet<T>(result))
                : null;

            foreach (object item in enumerable)
            {
                T typedItem = item as T;

                if (typedItem == null)
                {
                    continue;
                }

                foundAny = true;
                bool shouldAdd = seen != null
                    ? seen.Add(typedItem)
                    : !result.Contains(typedItem);
                if (shouldAdd)
                {
                    result.Add(typedItem);
                    if (result.Count >= maxItems)
                    {
                        break;
                    }
                }
            }

            return foundAny;
        }

        private static Kingdom GetKingdomFromObject(
            object worldObject
        )
        {
            if (worldObject == null)
            {
                return null;
            }

            Type type = worldObject.GetType();
            FieldInfo cachedField;
            if (KingdomFieldAccessorCache.TryGetValue(type, out cachedField))
            {
                try
                {
                    return cachedField.GetValue(worldObject) as Kingdom;
                }
                catch
                {
                    KingdomFieldAccessorCache.Remove(type);
                }
            }

            PropertyInfo cachedProperty;
            if (KingdomPropertyAccessorCache.TryGetValue(type, out cachedProperty))
            {
                try
                {
                    return cachedProperty.GetValue(worldObject, null) as Kingdom;
                }
                catch
                {
                    KingdomPropertyAccessorCache.Remove(type);
                }
            }

            if (KingdomAccessorMissingCache.Contains(type))
            {
                return null;
            }

            for (Type current = type; current != null; current = current.BaseType)
            {
                for (int i = 0; i < KingdomAccessorMemberNames.Length; i++)
                {
                    FieldInfo field = current.GetField(KingdomAccessorMemberNames[i], MemberFlags);
                    if (field != null)
                    {
                        KingdomFieldAccessorCache[type] = field;
                        try
                        {
                            return field.GetValue(worldObject) as Kingdom;
                        }
                        catch
                        {
                            KingdomFieldAccessorCache.Remove(type);
                        }
                    }

                    PropertyInfo property = current.GetProperty(KingdomAccessorMemberNames[i], MemberFlags);
                    if (
                        property != null &&
                        property.GetIndexParameters().Length == 0
                    )
                    {
                        KingdomPropertyAccessorCache[type] = property;
                        try
                        {
                            return property.GetValue(worldObject, null) as Kingdom;
                        }
                        catch
                        {
                            KingdomPropertyAccessorCache.Remove(type);
                        }
                    }
                }
            }

            KingdomAccessorMissingCache.Add(type);
            return null;
        }

        private static object GetMemberValue(
            object owner,
            params string[] memberNames
        )
        {
            if (owner == null || memberNames == null)
            {
                return null;
            }

            for (
                Type currentType = owner.GetType();
                currentType != null;
                currentType = currentType.BaseType
            )
            {
                for (int i = 0;
                    i < memberNames.Length;
                    i++)
                {
                    string memberName = memberNames[i];

                    FieldInfo field = currentType.GetField(
                        memberName,
                        MemberFlags
                    );

                    if (field != null)
                    {
                        try
                        {
                            return field.GetValue(owner);
                        }
                        catch
                        {
                            // Пробуем следующее имя.
                        }
                    }

                    PropertyInfo property =
                        currentType.GetProperty(
                            memberName,
                            MemberFlags
                        );

                    if (
                        property != null &&
                        property.GetIndexParameters().Length == 0
                    )
                    {
                        try
                        {
                            return property.GetValue(owner, null);
                        }
                        catch
                        {
                            // Пробуем следующее имя.
                        }
                    }
                }
            }

            return null;
        }

        private static bool TryChangeCityResource(
            City city,
            string resourceId,
            int amount
        )
        {
            if (
                city == null ||
                string.IsNullOrEmpty(resourceId) ||
                amount == 0
            )
            {
                return false;
            }

            object cityData = GetMemberValue(
                city,
                CityDataMemberNames
            );

            object storage = GetMemberValue(
                cityData,
                CityStorageMemberNames
            );

            if (storage == null)
            {
                return false;
            }

            Type storageType = storage.GetType();
            MethodInfo method;
            if (!CityStorageChangeMethodCache.TryGetValue(storageType, out method))
            {
                if (CityStorageChangeMethodMissingCache.Contains(storageType))
                {
                    return false;
                }

                MethodInfo[] methods = storageType.GetMethods(MemberFlags);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo candidate = methods[i];
                    if (candidate == null || candidate.Name != "change")
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (
                        parameters.Length != 2 ||
                        parameters[0].ParameterType != typeof(string)
                    )
                    {
                        continue;
                    }

                    // Validate that an int delta can be converted to the second
                    // parameter before caching this overload.
                    try
                    {
                        Convert.ChangeType(amount, parameters[1].ParameterType);
                        method = candidate;
                        CityStorageChangeMethodCache[storageType] = candidate;
                        break;
                    }
                    catch
                    {
                    }
                }

                if (method == null)
                {
                    CityStorageChangeMethodMissingCache.Add(storageType);
                    return false;
                }
            }

            try
            {
                ParameterInfo[] cachedParameters = method.GetParameters();
                object convertedAmount = Convert.ChangeType(
                    amount,
                    cachedParameters[1].ParameterType
                );
                method.Invoke(
                    storage,
                    new object[]
                    {
                        resourceId,
                        convertedAmount
                    }
                );
                return true;
            }
            catch
            {
                // Runtime type/overload changed unexpectedly. Drop the cache so
                // the next tick can rediscover instead of permanently failing.
                CityStorageChangeMethodCache.Remove(storageType);
                return false;
            }
        }

        private static void RefreshKingdomUnitStats(
            Kingdom kingdom
        )
        {
            if (!_soldierStatsPatchInstalled)
            {
                return;
            }

            List<Actor> actors =
                GetKingdomUnitsSafe(kingdom);

            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors[i];

                if (actor == null)
                {
                    continue;
                }

                try
                {
                    actor.setStatsDirty();
                }
                catch
                {
                    // Один повреждённый юнит не должен
                    // остановить обновление остальных.
                }
            }
        }

        private static string GetPreferredPoliticalTrait(
            Actor ruler
        )
        {
            if (ruler == null || ruler.stats == null)
            {
                return ReformerTraitId;
            }

            float warfare = ruler.stats[S.warfare];
            float diplomacy = ruler.stats[S.diplomacy];
            float stewardship = ruler.stats[S.stewardship];

            if (
                warfare >= diplomacy &&
                warfare >= stewardship
            )
            {
                return MilitaristTraitId;
            }

            if (diplomacy >= stewardship)
            {
                return DiplomatTraitId;
            }

            return ReformerTraitId;
        }

        private static int CountPoliticalTraits(Actor actor)
        {
            if (actor == null)
            {
                return 0;
            }

            int count = 0;

            if (actor.hasTrait(ReformerTraitId))
            {
                count++;
            }

            if (actor.hasTrait(MilitaristTraitId))
            {
                count++;
            }

            if (actor.hasTrait(DiplomatTraitId))
            {
                count++;
            }

            return count;
        }

        private static void CreatePoliticsGroup()
        {
            ActorTraitGroupAsset politicsGroup =
                new ActorTraitGroupAsset()
                {
                    id = TraitGroupId,
                    name = "trait_group_ukiol_politics"
                };

            AssetManager.trait_groups.add(politicsGroup);
        }

        private static ActorTrait CreateBaseTrait(
            string traitId,
            string iconPath
        )
        {
            ActorTrait trait = new ActorTrait()
            {
                id = traitId,
                group_id = TraitGroupId,
                path_icon = iconPath,
                can_be_given = true,
                can_be_removed = true,
                needs_to_be_explored = false
            };

            if (trait.base_stats == null)
            {
                trait.base_stats = new BaseStats();
            }

            return trait;
        }

        private static void CreateReformerTrait()
        {
            ActorTrait reformer = CreateBaseTrait(
                ReformerTraitId,
                ReformerIconPath
            );

            reformer.base_stats[S.health] = 20f;
            reformer.base_stats[S.stewardship] = 8f;
            reformer.base_stats[S.intelligence] = 4f;

            AssetManager.traits.add(reformer);
        }

        private static void CreateMilitaristTrait()
        {
            ActorTrait militarist = CreateBaseTrait(
                MilitaristTraitId,
                MilitaristIconPath
            );

            militarist.base_stats[S.damage] = 10f;
            militarist.base_stats[S.warfare] = 10f;

            AssetManager.traits.add(militarist);
        }

        private static void CreateDiplomatTrait()
        {
            ActorTrait diplomat = CreateBaseTrait(
                DiplomatTraitId,
                DiplomatIconPath
            );

            diplomat.base_stats[S.diplomacy] = 12f;
            diplomat.base_stats[S.stewardship] = 4f;

            AssetManager.traits.add(diplomat);
        }

    }
}
