using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// API 1.11: safe world access, lightweight world lifecycle hooks and
    /// lazy object-scoped save-data migrations.
    ///
    /// World queries are explicit and capped. Nothing in this file introduces
    /// a new Update loop or automatic full-world scan.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public delegate bool ActorMigrationStep(
            Actor actor,
            int fromVersion,
            int toVersion
        );

        public delegate bool CityMigrationStep(
            City city,
            int fromVersion,
            int toVersion
        );

        public delegate bool KingdomMigrationStep(
            Kingdom kingdom,
            int fromVersion,
            int toVersion
        );

        private const int DefaultKingdomQueryLimit = 256;
        private const int DefaultCityQueryLimit = 512;
        private const int DefaultActorQueryLimit = 1024;
        private const int MaxKingdomQueryLimit = 2048;
        private const int MaxCityQueryLimit = 8192;
        private const int MaxActorQueryLimit = 16384;
        private const int MaxMigrationStepsPerApply = 64;
        private const string MigrationVersionKey = "__pw_schema_version";

        private static readonly BindingFlags WorldQueryMemberFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly string[] WorldKingdomCollectionNames =
        {
            "list", "_list", "list_civs", "list_civ", "list_all",
            "kingdoms", "_kingdoms", "all_kingdoms", "civs"
        };

        private static readonly string[] WorldCityCollectionNames =
        {
            "cities", "_cities", "list_cities", "city_list", "settlements"
        };

        private static readonly string[] WorldActorCollectionNames =
        {
            "units", "_units", "list_units", "actors", "citizens", "_citizens"
        };

        private static readonly Dictionary<Type, MemberInfo>
            KingdomCollectionAccessorCache =
                new Dictionary<Type, MemberInfo>();

        private static readonly Dictionary<Type, MemberInfo>
            CityCollectionAccessorCache =
                new Dictionary<Type, MemberInfo>();

        private static readonly Dictionary<Type, MemberInfo>
            ActorCollectionAccessorCache =
                new Dictionary<Type, MemberInfo>();

        private static object _lastObservedWorld;
        private static object _lastObservedKingdomManager;
        private static bool _lastObservedWorldReady;
        private static int _worldSessionId;

        private sealed class ActorMigrationRule
        {
            public int FromVersion;
            public int ToVersion;
            public ActorMigrationStep Handler;
        }

        private sealed class CityMigrationRule
        {
            public int FromVersion;
            public int ToVersion;
            public CityMigrationStep Handler;
        }

        private sealed class KingdomMigrationRule
        {
            public int FromVersion;
            public int ToVersion;
            public KingdomMigrationStep Handler;
        }

        private static readonly Dictionary<
            string,
            SortedDictionary<int, ActorMigrationRule>
        > ActorMigrationRules =
            new Dictionary<
                string,
                SortedDictionary<int, ActorMigrationRule>
            >(StringComparer.Ordinal);

        private static readonly Dictionary<
            string,
            SortedDictionary<int, CityMigrationRule>
        > CityMigrationRules =
            new Dictionary<
                string,
                SortedDictionary<int, CityMigrationRule>
            >(StringComparer.Ordinal);

        private static readonly Dictionary<
            string,
            SortedDictionary<int, KingdomMigrationRule>
        > KingdomMigrationRules =
            new Dictionary<
                string,
                SortedDictionary<int, KingdomMigrationRule>
            >(StringComparer.Ordinal);

        /// <summary>
        /// Explicit capped access to live WorldBox objects.
        ///
        /// The API returns snapshots so addons do not retain or mutate vanilla
        /// manager collections. Predicates run only for items reached by the
        /// requested capped query.
        /// </summary>
        public static class WorldQuery
        {
            public static bool IsAvailable()
            {
                try
                {
                    return global::World.world != null;
                }
                catch
                {
                    return false;
                }
            }

            public static bool IsReady()
            {
                try
                {
                    return
                        global::World.world != null &&
                        global::World.world.kingdoms != null;
                }
                catch
                {
                    return false;
                }
            }

            public static int GetSessionId()
            {
                return _worldSessionId;
            }

            public static int GetYear()
            {
                try
                {
                    int dateYear = (int)Date.getYearsSince(0.0);
                    return dateYear < 0 ? 0 : dateYear;
                }
                catch
                {
                    return 0;
                }
            }

            public static List<Kingdom> GetKingdoms(
                int maxItems = DefaultKingdomQueryLimit,
                KingdomCondition condition = null
            )
            {
                List<Kingdom> snapshot = new List<Kingdom>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultKingdomQueryLimit,
                    MaxKingdomQueryLimit
                );

                if (limit <= 0)
                {
                    return snapshot;
                }

                object manager = null;
                try
                {
                    if (global::World.world != null)
                    {
                        manager = global::World.world.kingdoms;
                    }
                }
                catch
                {
                    manager = null;
                }

                // First cap the raw snapshot, then evaluate the predicate.
                // This guarantees a query cannot inspect an unbounded manager
                // just because its predicate matches nothing.
                CollectFromOwner(
                    manager,
                    WorldKingdomCollectionNames,
                    KingdomCollectionAccessorCache,
                    snapshot,
                    limit,
                    null
                );

                return FilterKingdomSnapshot(
                    snapshot,
                    condition
                );
            }

            public static Kingdom FindKingdom(
                KingdomCondition condition,
                int maxInspected = DefaultKingdomQueryLimit
            )
            {
                if (condition == null)
                {
                    return null;
                }

                List<Kingdom> matches = GetKingdoms(
                    maxInspected,
                    delegate(Kingdom kingdom)
                    {
                        return condition(kingdom);
                    }
                );

                return matches.Count > 0 ? matches[0] : null;
            }

            public static List<City> GetCities(
                Kingdom kingdom,
                int maxItems = DefaultCityQueryLimit,
                CityCondition condition = null
            )
            {
                List<City> snapshot = new List<City>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultCityQueryLimit,
                    MaxCityQueryLimit
                );

                if (kingdom == null || limit <= 0)
                {
                    return snapshot;
                }

                CollectFromOwner(
                    kingdom,
                    WorldCityCollectionNames,
                    CityCollectionAccessorCache,
                    snapshot,
                    limit,
                    null
                );

                return FilterCitySnapshot(
                    snapshot,
                    condition
                );
            }

            public static List<City> GetCities(
                int maxItems = DefaultCityQueryLimit,
                CityCondition condition = null
            )
            {
                List<City> snapshot = new List<City>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultCityQueryLimit,
                    MaxCityQueryLimit
                );

                if (limit <= 0)
                {
                    return snapshot;
                }

                List<Kingdom> kingdoms = GetKingdoms(
                    Math.Min(
                        MaxKingdomQueryLimit,
                        limit
                    ),
                    null
                );

                for (
                    int i = 0;
                    i < kingdoms.Count && snapshot.Count < limit;
                    i++
                )
                {
                    List<City> cities = GetCities(
                        kingdoms[i],
                        limit - snapshot.Count,
                        null
                    );

                    AddUniqueItems(
                        cities,
                        snapshot,
                        limit,
                        null
                    );
                }

                return FilterCitySnapshot(
                    snapshot,
                    condition
                );
            }

            public static City FindCity(
                CityCondition condition,
                int maxInspected = DefaultCityQueryLimit
            )
            {
                if (condition == null)
                {
                    return null;
                }

                List<City> matches = GetCities(
                    maxInspected,
                    delegate(City city)
                    {
                        return condition(city);
                    }
                );

                return matches.Count > 0 ? matches[0] : null;
            }

            public static List<Actor> GetActors(
                City city,
                int maxItems = DefaultActorQueryLimit,
                ActorCondition condition = null
            )
            {
                List<Actor> snapshot = new List<Actor>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultActorQueryLimit,
                    MaxActorQueryLimit
                );

                if (city == null || limit <= 0)
                {
                    return snapshot;
                }

                CollectFromOwner(
                    city,
                    WorldActorCollectionNames,
                    ActorCollectionAccessorCache,
                    snapshot,
                    limit,
                    null
                );

                return FilterActorSnapshot(
                    snapshot,
                    condition
                );
            }

            public static List<Actor> GetActors(
                Kingdom kingdom,
                int maxItems = DefaultActorQueryLimit,
                ActorCondition condition = null
            )
            {
                List<Actor> snapshot = new List<Actor>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultActorQueryLimit,
                    MaxActorQueryLimit
                );

                if (kingdom == null || limit <= 0)
                {
                    return snapshot;
                }

                // Prefer a direct kingdom unit collection when the runtime
                // exposes one. If it does not, fall back to city snapshots.
                CollectFromOwner(
                    kingdom,
                    WorldActorCollectionNames,
                    ActorCollectionAccessorCache,
                    snapshot,
                    limit,
                    null
                );

                if (snapshot.Count == 0)
                {
                    List<City> cities = GetCities(
                        kingdom,
                        Math.Min(
                            MaxCityQueryLimit,
                            limit
                        ),
                        null
                    );

                    for (
                        int i = 0;
                        i < cities.Count && snapshot.Count < limit;
                        i++
                    )
                    {
                        List<Actor> actors = GetActors(
                            cities[i],
                            limit - snapshot.Count,
                            null
                        );

                        AddUniqueItems(
                            actors,
                            snapshot,
                            limit,
                            null
                        );
                    }
                }

                return FilterActorSnapshot(
                    snapshot,
                    condition
                );
            }

            public static List<Actor> GetActors(
                int maxItems = DefaultActorQueryLimit,
                ActorCondition condition = null
            )
            {
                List<Actor> snapshot = new List<Actor>();
                int limit = NormalizeWorldQueryLimit(
                    maxItems,
                    DefaultActorQueryLimit,
                    MaxActorQueryLimit
                );

                if (limit <= 0)
                {
                    return snapshot;
                }

                List<Kingdom> kingdoms = GetKingdoms(
                    Math.Min(
                        MaxKingdomQueryLimit,
                        limit
                    ),
                    null
                );

                for (
                    int i = 0;
                    i < kingdoms.Count && snapshot.Count < limit;
                    i++
                )
                {
                    List<Actor> actors = GetActors(
                        kingdoms[i],
                        limit - snapshot.Count,
                        null
                    );

                    AddUniqueItems(
                        actors,
                        snapshot,
                        limit,
                        null
                    );
                }

                return FilterActorSnapshot(
                    snapshot,
                    condition
                );
            }

            public static Actor FindActor(
                ActorCondition condition,
                int maxInspected = DefaultActorQueryLimit
            )
            {
                if (condition == null)
                {
                    return null;
                }

                List<Actor> matches = GetActors(
                    maxInspected,
                    delegate(Actor actor)
                    {
                        return condition(actor);
                    }
                );

                return matches.Count > 0 ? matches[0] : null;
            }
        }

        /// <summary>
        /// Read-only lifecycle state. Event notifications use the existing
        /// PoliticalWorldAPI event bus:
        /// Events.WorldChanged, Events.WorldReady and Events.WorldUnavailable.
        /// </summary>
        public static class Lifecycle
        {
            public static bool IsWorldAvailable()
            {
                return WorldQuery.IsAvailable();
            }

            public static bool IsWorldReady()
            {
                return WorldQuery.IsReady();
            }

            public static int GetWorldSessionId()
            {
                return WorldQuery.GetSessionId();
            }

            public static int GetWorldYear()
            {
                return WorldQuery.GetYear();
            }
        }

        /// <summary>
        /// Lazy migration registry for addon-owned Actor/City/Kingdom data.
        ///
        /// Migration state is saved inside the same namespaced object data as
        /// the addon's other values. No full-world migration is started
        /// automatically. The addon explicitly applies migrations to the
        /// objects it actually uses.
        /// </summary>
        public static class Migrations
        {
            public static bool RegisterActor(
                string addonId,
                int fromVersion,
                int toVersion,
                ActorMigrationStep handler
            )
            {
                string owner = Trim(addonId);
                if (
                    !IsAddonRegistered(owner) ||
                    !IsValidMigrationRange(fromVersion, toVersion) ||
                    handler == null
                )
                {
                    return false;
                }

                SortedDictionary<int, ActorMigrationRule> rules;
                if (!ActorMigrationRules.TryGetValue(owner, out rules))
                {
                    rules =
                        new SortedDictionary<int, ActorMigrationRule>();
                    ActorMigrationRules[owner] = rules;
                }

                if (rules.ContainsKey(fromVersion))
                {
                    return false;
                }

                rules[fromVersion] = new ActorMigrationRule()
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    Handler = handler
                };

                return true;
            }

            public static bool RegisterCity(
                string addonId,
                int fromVersion,
                int toVersion,
                CityMigrationStep handler
            )
            {
                string owner = Trim(addonId);
                if (
                    !IsAddonRegistered(owner) ||
                    !IsValidMigrationRange(fromVersion, toVersion) ||
                    handler == null
                )
                {
                    return false;
                }

                SortedDictionary<int, CityMigrationRule> rules;
                if (!CityMigrationRules.TryGetValue(owner, out rules))
                {
                    rules =
                        new SortedDictionary<int, CityMigrationRule>();
                    CityMigrationRules[owner] = rules;
                }

                if (rules.ContainsKey(fromVersion))
                {
                    return false;
                }

                rules[fromVersion] = new CityMigrationRule()
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    Handler = handler
                };

                return true;
            }

            public static bool RegisterKingdom(
                string addonId,
                int fromVersion,
                int toVersion,
                KingdomMigrationStep handler
            )
            {
                string owner = Trim(addonId);
                if (
                    !IsAddonRegistered(owner) ||
                    !IsValidMigrationRange(fromVersion, toVersion) ||
                    handler == null
                )
                {
                    return false;
                }

                SortedDictionary<int, KingdomMigrationRule> rules;
                if (!KingdomMigrationRules.TryGetValue(owner, out rules))
                {
                    rules =
                        new SortedDictionary<int, KingdomMigrationRule>();
                    KingdomMigrationRules[owner] = rules;
                }

                if (rules.ContainsKey(fromVersion))
                {
                    return false;
                }

                rules[fromVersion] = new KingdomMigrationRule()
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    Handler = handler
                };

                return true;
            }

            public static int GetVersion(
                Actor actor,
                string addonId
            )
            {
                return Data.GetInt(
                    actor,
                    addonId,
                    MigrationVersionKey,
                    0
                );
            }

            public static int GetVersion(
                City city,
                string addonId
            )
            {
                return Data.GetInt(
                    city,
                    addonId,
                    MigrationVersionKey,
                    0
                );
            }

            public static int GetVersion(
                Kingdom kingdom,
                string addonId
            )
            {
                return Data.GetInt(
                    kingdom,
                    addonId,
                    MigrationVersionKey,
                    0
                );
            }

            public static int GetTargetActorVersion(
                string addonId
            )
            {
                SortedDictionary<int, ActorMigrationRule> rules;
                if (
                    !ActorMigrationRules.TryGetValue(
                        Trim(addonId),
                        out rules
                    )
                )
                {
                    return 0;
                }

                return GetActorMigrationTarget(rules);
            }

            public static int GetTargetCityVersion(
                string addonId
            )
            {
                SortedDictionary<int, CityMigrationRule> rules;
                if (
                    !CityMigrationRules.TryGetValue(
                        Trim(addonId),
                        out rules
                    )
                )
                {
                    return 0;
                }

                return GetCityMigrationTarget(rules);
            }

            public static int GetTargetKingdomVersion(
                string addonId
            )
            {
                SortedDictionary<int, KingdomMigrationRule> rules;
                if (
                    !KingdomMigrationRules.TryGetValue(
                        Trim(addonId),
                        out rules
                    )
                )
                {
                    return 0;
                }

                return GetKingdomMigrationTarget(rules);
            }

            public static bool Apply(
                Actor actor,
                string addonId
            )
            {
                string owner = Trim(addonId);
                if (actor == null || !IsAddonRegistered(owner))
                {
                    return false;
                }

                SortedDictionary<int, ActorMigrationRule> rules;
                if (!ActorMigrationRules.TryGetValue(owner, out rules))
                {
                    return true;
                }

                int current = GetVersion(actor, owner);
                int target = GetActorMigrationTarget(rules);

                if (current >= target)
                {
                    return true;
                }

                int steps = 0;
                while (
                    current < target &&
                    steps < MaxMigrationStepsPerApply
                )
                {
                    ActorMigrationRule rule;
                    if (!rules.TryGetValue(current, out rule) || rule == null)
                    {
                        RecordMigrationProblem(
                            owner,
                            "actor",
                            current,
                            target,
                            "No migration step starts at the current version."
                        );
                        return false;
                    }

                    bool ok = false;
                    try
                    {
                        ok = rule.Handler(
                            actor,
                            rule.FromVersion,
                            rule.ToVersion
                        );
                    }
                    catch (Exception exception)
                    {
                        RecordMigrationProblem(
                            owner,
                            "actor",
                            current,
                            target,
                            "Migration callback threw: " +
                            exception.Message
                        );
                        return false;
                    }

                    if (!ok)
                    {
                        RecordMigrationProblem(
                            owner,
                            "actor",
                            current,
                            target,
                            "Migration callback returned false."
                        );
                        return false;
                    }

                    if (
                        !Data.SetInt(
                            actor,
                            owner,
                            MigrationVersionKey,
                            rule.ToVersion
                        )
                    )
                    {
                        RecordMigrationProblem(
                            owner,
                            "actor",
                            current,
                            target,
                            "Could not save the new schema version."
                        );
                        return false;
                    }

                    current = rule.ToVersion;
                    steps++;
                }

                return current >= target;
            }

            public static bool Apply(
                City city,
                string addonId
            )
            {
                string owner = Trim(addonId);
                if (city == null || !IsAddonRegistered(owner))
                {
                    return false;
                }

                SortedDictionary<int, CityMigrationRule> rules;
                if (!CityMigrationRules.TryGetValue(owner, out rules))
                {
                    return true;
                }

                int current = GetVersion(city, owner);
                int target = GetCityMigrationTarget(rules);

                if (current >= target)
                {
                    return true;
                }

                int steps = 0;
                while (
                    current < target &&
                    steps < MaxMigrationStepsPerApply
                )
                {
                    CityMigrationRule rule;
                    if (!rules.TryGetValue(current, out rule) || rule == null)
                    {
                        RecordMigrationProblem(
                            owner,
                            "city",
                            current,
                            target,
                            "No migration step starts at the current version."
                        );
                        return false;
                    }

                    bool ok = false;
                    try
                    {
                        ok = rule.Handler(
                            city,
                            rule.FromVersion,
                            rule.ToVersion
                        );
                    }
                    catch (Exception exception)
                    {
                        RecordMigrationProblem(
                            owner,
                            "city",
                            current,
                            target,
                            "Migration callback threw: " +
                            exception.Message
                        );
                        return false;
                    }

                    if (!ok)
                    {
                        RecordMigrationProblem(
                            owner,
                            "city",
                            current,
                            target,
                            "Migration callback returned false."
                        );
                        return false;
                    }

                    if (
                        !Data.SetInt(
                            city,
                            owner,
                            MigrationVersionKey,
                            rule.ToVersion
                        )
                    )
                    {
                        RecordMigrationProblem(
                            owner,
                            "city",
                            current,
                            target,
                            "Could not save the new schema version."
                        );
                        return false;
                    }

                    current = rule.ToVersion;
                    steps++;
                }

                return current >= target;
            }

            public static bool Apply(
                Kingdom kingdom,
                string addonId
            )
            {
                string owner = Trim(addonId);
                if (kingdom == null || !IsAddonRegistered(owner))
                {
                    return false;
                }

                SortedDictionary<int, KingdomMigrationRule> rules;
                if (!KingdomMigrationRules.TryGetValue(owner, out rules))
                {
                    return true;
                }

                int current = GetVersion(kingdom, owner);
                int target = GetKingdomMigrationTarget(rules);

                if (current >= target)
                {
                    return true;
                }

                int steps = 0;
                while (
                    current < target &&
                    steps < MaxMigrationStepsPerApply
                )
                {
                    KingdomMigrationRule rule;
                    if (!rules.TryGetValue(current, out rule) || rule == null)
                    {
                        RecordMigrationProblem(
                            owner,
                            "kingdom",
                            current,
                            target,
                            "No migration step starts at the current version."
                        );
                        return false;
                    }

                    bool ok = false;
                    try
                    {
                        ok = rule.Handler(
                            kingdom,
                            rule.FromVersion,
                            rule.ToVersion
                        );
                    }
                    catch (Exception exception)
                    {
                        RecordMigrationProblem(
                            owner,
                            "kingdom",
                            current,
                            target,
                            "Migration callback threw: " +
                            exception.Message
                        );
                        return false;
                    }

                    if (!ok)
                    {
                        RecordMigrationProblem(
                            owner,
                            "kingdom",
                            current,
                            target,
                            "Migration callback returned false."
                        );
                        return false;
                    }

                    if (
                        !Data.SetInt(
                            kingdom,
                            owner,
                            MigrationVersionKey,
                            rule.ToVersion
                        )
                    )
                    {
                        RecordMigrationProblem(
                            owner,
                            "kingdom",
                            current,
                            target,
                            "Could not save the new schema version."
                        );
                        return false;
                    }

                    current = rule.ToVersion;
                    steps++;
                }

                return current >= target;
            }
        }

        /// <summary>
        /// Called from Political World's existing Update method. This is a
        /// constant-time reference/readiness check and does not scan the world.
        /// </summary>
        internal static void InternalTickWorldLifecycle()
        {
            object currentWorld = null;
            object currentKingdomManager = null;

            try
            {
                currentWorld = global::World.world;
                if (global::World.world != null)
                {
                    currentKingdomManager = global::World.world.kingdoms;
                }
            }
            catch
            {
                currentWorld = null;
                currentKingdomManager = null;
            }

            bool worldChanged = !object.ReferenceEquals(
                currentWorld,
                _lastObservedWorld
            );
            bool managerReplaced =
                !worldChanged &&
                currentWorld != null &&
                _lastObservedKingdomManager != null &&
                currentKingdomManager != null &&
                !object.ReferenceEquals(
                    currentKingdomManager,
                    _lastObservedKingdomManager
                );
            bool changed = worldChanged || managerReplaced;

            if (
                !changed &&
                _lastObservedKingdomManager == null &&
                currentKingdomManager != null
            )
            {
                // Normal world initialization may attach the manager after the
                // World object itself appears. Capture that baseline without
                // inventing a second world session.
                _lastObservedKingdomManager = currentKingdomManager;
            }

            if (changed)
            {
                int oldSession = _worldSessionId;

                if (_lastObservedWorld != null && _lastObservedWorldReady)
                {
                    InternalEmitCoreEvent(
                        Events.WorldUnavailable,
                        null,
                        oldNumber: oldSession,
                        newNumber: oldSession,
                        text: "World became unavailable.",
                        sourceAddonId: CoreModId,
                        category: "world.lifecycle",
                        year: WorldQuery.GetYear()
                    );
                }

                _lastObservedWorld = currentWorld;
                _lastObservedKingdomManager = currentKingdomManager;
                _lastObservedWorldReady = false;

                if (currentWorld != null)
                {
                    _worldSessionId++;
                }

                InternalEmitCoreEvent(
                    Events.WorldChanged,
                    null,
                    oldNumber: oldSession,
                    newNumber: _worldSessionId,
                    text:
                        currentWorld == null
                            ? "World reference cleared."
                            : "World reference changed.",
                    sourceAddonId: CoreModId,
                    category: "world.lifecycle",
                    year: WorldQuery.GetYear()
                );
            }

            bool ready = WorldQuery.IsReady();

            if (ready && !_lastObservedWorldReady)
            {
                _lastObservedWorldReady = true;

                InternalEmitCoreEvent(
                    Events.WorldReady,
                    null,
                    oldNumber: _worldSessionId,
                    newNumber: _worldSessionId,
                    text: "World is ready for capped public queries.",
                    sourceAddonId: CoreModId,
                    category: "world.lifecycle",
                    year: WorldQuery.GetYear()
                );
            }
            else if (
                !ready &&
                _lastObservedWorldReady &&
                !changed
            )
            {
                _lastObservedWorldReady = false;

                InternalEmitCoreEvent(
                    Events.WorldUnavailable,
                    null,
                    oldNumber: _worldSessionId,
                    newNumber: _worldSessionId,
                    text: "World became unavailable.",
                    sourceAddonId: CoreModId,
                    category: "world.lifecycle",
                    year: WorldQuery.GetYear()
                );
            }
        }

        private static int NormalizeWorldQueryLimit(
            int requested,
            int defaultValue,
            int maximum
        )
        {
            int value = requested;
            if (value < 0)
            {
                value = defaultValue;
            }
            if (value > maximum)
            {
                value = maximum;
            }
            return value;
        }

        private static void CollectFromOwner<T>(
            object owner,
            string[] preferredNames,
            Dictionary<Type, MemberInfo> cache,
            List<T> result,
            int maxItems,
            Func<T, bool> condition
        ) where T : class
        {
            if (
                owner == null ||
                result == null ||
                maxItems <= 0
            )
            {
                return;
            }

            if (
                AddUniqueItemsFromEnumerable(
                    owner,
                    result,
                    maxItems,
                    condition
                )
            )
            {
                return;
            }

            Type ownerType = owner.GetType();

            MemberInfo cached;
            if (
                cache != null &&
                cache.TryGetValue(ownerType, out cached) &&
                cached != null
            )
            {
                object cachedValue = ReadWorldQueryMember(
                    owner,
                    cached
                );

                if (
                    AddUniqueItemsFromEnumerable(
                        cachedValue,
                        result,
                        maxItems,
                        condition
                    )
                )
                {
                    return;
                }

                cache.Remove(ownerType);
            }

            if (preferredNames != null)
            {
                for (int i = 0; i < preferredNames.Length; i++)
                {
                    MemberInfo member = FindWorldQueryMember(
                        ownerType,
                        preferredNames[i]
                    );

                    if (member == null)
                    {
                        continue;
                    }

                    object value = ReadWorldQueryMember(
                        owner,
                        member
                    );

                    if (
                        AddUniqueItemsFromEnumerable(
                            value,
                            result,
                            maxItems,
                            condition
                        )
                    )
                    {
                        if (cache != null)
                        {
                            cache[ownerType] = member;
                        }
                        return;
                    }
                }
            }

            for (
                Type current = ownerType;
                current != null;
                current = current.BaseType
            )
            {
                FieldInfo[] fields =
                    current.GetFields(WorldQueryMemberFlags);

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

                    if (
                        AddUniqueItemsFromEnumerable(
                            value,
                            result,
                            maxItems,
                            condition
                        )
                    )
                    {
                        if (cache != null)
                        {
                            cache[ownerType] = fields[i];
                        }
                        return;
                    }
                }

                PropertyInfo[] properties =
                    current.GetProperties(WorldQueryMemberFlags);

                for (int i = 0; i < properties.Length; i++)
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

                    if (
                        AddUniqueItemsFromEnumerable(
                            value,
                            result,
                            maxItems,
                            condition
                        )
                    )
                    {
                        if (cache != null)
                        {
                            cache[ownerType] = properties[i];
                        }
                        return;
                    }
                }
            }
        }

        private static MemberInfo FindWorldQueryMember(
            Type type,
            string name
        )
        {
            if (
                type == null ||
                string.IsNullOrEmpty(name)
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
                FieldInfo field = current.GetField(
                    name,
                    WorldQueryMemberFlags
                );

                if (field != null)
                {
                    return field;
                }

                PropertyInfo property = current.GetProperty(
                    name,
                    WorldQueryMemberFlags
                );

                if (
                    property != null &&
                    property.GetIndexParameters().Length == 0
                )
                {
                    return property;
                }
            }

            return null;
        }

        private static object ReadWorldQueryMember(
            object owner,
            MemberInfo member
        )
        {
            if (owner == null || member == null)
            {
                return null;
            }

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    return field.GetValue(owner);
                }

                PropertyInfo property = member as PropertyInfo;
                if (
                    property != null &&
                    property.GetIndexParameters().Length == 0
                )
                {
                    return property.GetValue(owner, null);
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool AddUniqueItemsFromEnumerable<T>(
            object collection,
            List<T> result,
            int maxItems,
            Func<T, bool> condition
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

            IEnumerable enumerable = collection as IEnumerable;
            if (enumerable == null)
            {
                return false;
            }

            bool foundTypedItem = false;
            HashSet<T> seen =
                result.Count == 0
                    ? new HashSet<T>()
                    : new HashSet<T>(result);

            try
            {
                foreach (object item in enumerable)
                {
                    T typed = item as T;
                    if (typed == null)
                    {
                        continue;
                    }

                    foundTypedItem = true;

                    bool accepted = true;
                    if (condition != null)
                    {
                        try
                        {
                            accepted = condition(typed);
                        }
                        catch
                        {
                            accepted = false;
                        }
                    }

                    if (
                        accepted &&
                        seen.Add(typed)
                    )
                    {
                        result.Add(typed);

                        if (result.Count >= maxItems)
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Vanilla collections can change during gameplay. Return the
                // stable snapshot collected up to the point of mutation.
            }

            return foundTypedItem;
        }

        private static List<Kingdom> FilterKingdomSnapshot(
            List<Kingdom> snapshot,
            KingdomCondition condition
        )
        {
            if (snapshot == null)
            {
                return new List<Kingdom>();
            }

            if (condition == null)
            {
                return snapshot;
            }

            List<Kingdom> result = new List<Kingdom>();

            for (int i = 0; i < snapshot.Count; i++)
            {
                Kingdom item = snapshot[i];
                if (item == null)
                {
                    continue;
                }

                bool accepted = false;
                try
                {
                    accepted = condition(item);
                }
                catch
                {
                    accepted = false;
                }

                if (accepted)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static List<City> FilterCitySnapshot(
            List<City> snapshot,
            CityCondition condition
        )
        {
            if (snapshot == null)
            {
                return new List<City>();
            }

            if (condition == null)
            {
                return snapshot;
            }

            List<City> result = new List<City>();

            for (int i = 0; i < snapshot.Count; i++)
            {
                City item = snapshot[i];
                if (item == null)
                {
                    continue;
                }

                bool accepted = false;
                try
                {
                    accepted = condition(item);
                }
                catch
                {
                    accepted = false;
                }

                if (accepted)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static List<Actor> FilterActorSnapshot(
            List<Actor> snapshot,
            ActorCondition condition
        )
        {
            if (snapshot == null)
            {
                return new List<Actor>();
            }

            if (condition == null)
            {
                return snapshot;
            }

            List<Actor> result = new List<Actor>();

            for (int i = 0; i < snapshot.Count; i++)
            {
                Actor item = snapshot[i];
                if (item == null)
                {
                    continue;
                }

                bool accepted = false;
                try
                {
                    accepted = condition(item);
                }
                catch
                {
                    accepted = false;
                }

                if (accepted)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static void AddUniqueItems<T>(
            List<T> source,
            List<T> destination,
            int maxItems,
            Func<T, bool> condition
        ) where T : class
        {
            if (
                source == null ||
                destination == null ||
                maxItems <= 0
            )
            {
                return;
            }

            HashSet<T> seen =
                destination.Count == 0
                    ? new HashSet<T>()
                    : new HashSet<T>(destination);

            for (
                int i = 0;
                i < source.Count &&
                destination.Count < maxItems;
                i++
            )
            {
                T item = source[i];
                if (item == null || !seen.Add(item))
                {
                    continue;
                }

                bool accepted = true;
                if (condition != null)
                {
                    try
                    {
                        accepted = condition(item);
                    }
                    catch
                    {
                        accepted = false;
                    }
                }

                if (accepted)
                {
                    destination.Add(item);
                }
            }
        }

        private static bool IsValidMigrationRange(
            int fromVersion,
            int toVersion
        )
        {
            return
                fromVersion >= 0 &&
                toVersion > fromVersion &&
                toVersion <= 1000000;
        }

        private static int GetActorMigrationTarget(
            SortedDictionary<int, ActorMigrationRule> rules
        )
        {
            int target = 0;
            if (rules == null)
            {
                return target;
            }

            foreach (
                KeyValuePair<int, ActorMigrationRule> pair
                in rules
            )
            {
                if (
                    pair.Value != null &&
                    pair.Value.ToVersion > target
                )
                {
                    target = pair.Value.ToVersion;
                }
            }

            return target;
        }

        private static int GetCityMigrationTarget(
            SortedDictionary<int, CityMigrationRule> rules
        )
        {
            int target = 0;
            if (rules == null)
            {
                return target;
            }

            foreach (
                KeyValuePair<int, CityMigrationRule> pair
                in rules
            )
            {
                if (
                    pair.Value != null &&
                    pair.Value.ToVersion > target
                )
                {
                    target = pair.Value.ToVersion;
                }
            }

            return target;
        }

        private static int GetKingdomMigrationTarget(
            SortedDictionary<int, KingdomMigrationRule> rules
        )
        {
            int target = 0;
            if (rules == null)
            {
                return target;
            }

            foreach (
                KeyValuePair<int, KingdomMigrationRule> pair
                in rules
            )
            {
                if (
                    pair.Value != null &&
                    pair.Value.ToVersion > target
                )
                {
                    target = pair.Value.ToVersion;
                }
            }

            return target;
        }

        private static void RecordMigrationProblem(
            string addonId,
            string objectType,
            int currentVersion,
            int targetVersion,
            string message
        )
        {
            InternalRecordDiagnostic(
                addonId,
                "ERROR",
                "PW1110",
                "Migration failed for " +
                objectType +
                " schema " +
                currentVersion +
                " -> " +
                targetVersion +
                ": " +
                (message ?? "")
            );
        }
    }
}
