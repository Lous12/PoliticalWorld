using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // WorldBox 0.50.5+ persists map_stats.custom_data inside the world
        // save. Chronicle uses a small ring of independent string entries
        // instead of one giant blob: adding one event only rewrites one slot
        // plus tiny metadata keys.
        private const int PoliticalChroniclePersistenceSchema = 1;
        private const string PoliticalChroniclePersistencePrefix =
            "ukiol_chronicle_v1_";
        private const string PoliticalChronicleSchemaKey =
            PoliticalChroniclePersistencePrefix + "schema";
        private const string PoliticalChronicleCountKey =
            PoliticalChroniclePersistencePrefix + "count";
        private const string PoliticalChronicleNextIdKey =
            PoliticalChroniclePersistencePrefix + "next_id";
        private const string PoliticalChronicleEntryKeyPrefix =
            PoliticalChroniclePersistencePrefix + "entry_";

        private static bool _politicalChroniclePersistenceLoadPending;
        private static bool _politicalChroniclePersistenceLoadedForWorld;
        private static bool _politicalChroniclePersistenceDirty;
        private static bool _politicalChroniclePersistenceUnsupportedSchema;
        private static bool _politicalChroniclePersistenceUnavailableLogged;
        private static int _politicalChroniclePersistenceLoadAttempts;
        private static float _politicalChroniclePersistenceNextRetryAt;
        private static object _politicalChronicleCustomDataCache;
        private static Type _politicalChronicleCustomDataType;
        private static MethodInfo _politicalChronicleSetStringMethod;
        private static MethodInfo _politicalChronicleGetStringMethod;

        private static void ResetPoliticalChroniclePersistenceForWorld()
        {
            _politicalChroniclePersistenceLoadPending = true;
            _politicalChroniclePersistenceLoadedForWorld = false;
            _politicalChroniclePersistenceDirty = false;
            _politicalChroniclePersistenceUnsupportedSchema = false;
            _politicalChroniclePersistenceUnavailableLogged = false;
            _politicalChroniclePersistenceLoadAttempts = 0;
            _politicalChroniclePersistenceNextRetryAt = 0f;
            _politicalChronicleCustomDataCache = null;
            _politicalChronicleCustomDataType = null;
            _politicalChronicleSetStringMethod = null;
            _politicalChronicleGetStringMethod = null;
        }

        private static void UpdatePoliticalChroniclePersistenceAfterLoad()
        {
            if (!_politicalChroniclePersistenceLoadPending)
            {
                if (
                    _politicalChroniclePersistenceDirty &&
                    Time.unscaledTime >=
                        _politicalChroniclePersistenceNextRetryAt
                )
                {
                    _politicalChroniclePersistenceNextRetryAt =
                        Time.unscaledTime + 5f;
                    TryPersistFullPoliticalChronicle();
                }
                return;
            }

            // During the first load frames WorldBox can already expose the
            // world object while its save metadata is still being restored.
            // Wait until Political World's topology bootstrap has completed.
            if (_worldLoadBootstrapPending)
            {
                return;
            }

            _politicalChroniclePersistenceLoadAttempts++;
            bool storageReady = RestorePoliticalChronicleFromWorldData();
            if (!storageReady)
            {
                // A stable loaded world should already expose map stats. Give
                // late modded builds a short grace period, then stop probing
                // every frame and fall back to runtime-only Chronicle.
                if (_politicalChroniclePersistenceLoadAttempts < 180)
                {
                    return;
                }

                _politicalChroniclePersistenceLoadPending = false;
                _politicalChroniclePersistenceLoadedForWorld = true;
                LogChroniclePersistenceUnavailableOnce();
                return;
            }

            _politicalChroniclePersistenceLoadPending = false;
            _politicalChroniclePersistenceLoadedForWorld = true;

            try
            {
                PoliticalChronicleWindow.RefreshAfterPersistenceLoad();
            }
            catch
            {
            }
        }

        private static void PersistPoliticalChronicleEntry(
            PoliticalChronicleEntry entry
        )
        {
            if (
                entry == null ||
                _politicalChroniclePersistenceUnsupportedSchema
            )
            {
                return;
            }

            // If the world is still being loaded, do not overwrite its saved
            // chronicle with an empty/new runtime view. Autonomous simulation
            // is blocked during this phase, but this guard also protects
            // against addon-triggered publications.
            if (
                _politicalChroniclePersistenceLoadPending ||
                !_politicalChroniclePersistenceLoadedForWorld
            )
            {
                _politicalChroniclePersistenceDirty = true;
                return;
            }

            object customData = GetMapStatsCustomDataObject();
            if (customData == null)
            {
                _politicalChroniclePersistenceDirty = true;
                _politicalChroniclePersistenceNextRetryAt =
                    Time.unscaledTime + 5f;
                LogChroniclePersistenceUnavailableOnce();
                return;
            }

            string slotKey = GetPoliticalChronicleSlotKey(entry.Id);
            string serialized = SerializePoliticalChronicleEntry(entry);
            if (string.IsNullOrEmpty(serialized))
            {
                _politicalChroniclePersistenceDirty = true;
                return;
            }

            // Commit order matters. The slot is written first; next_id and
            // count only advance after the entry itself exists.
            bool ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleSchemaKey,
                PoliticalChroniclePersistenceSchema.ToString()
            );
            ok = TrySetCustomDataString(customData, slotKey, serialized) && ok;
            ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleNextIdKey,
                _nextPoliticalChronicleEntryId.ToString()
            ) && ok;
            ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleCountKey,
                PoliticalChronicleEntries.Count.ToString()
            ) && ok;

            _politicalChroniclePersistenceDirty = !ok;
            if (!ok)
            {
                _politicalChroniclePersistenceNextRetryAt =
                    Time.unscaledTime + 5f;
                LogChroniclePersistenceUnavailableOnce();
            }
            else
            {
                _politicalChroniclePersistenceNextRetryAt = 0f;
            }
        }

        private static bool TryPersistFullPoliticalChronicle()
        {
            if (
                _politicalChroniclePersistenceUnsupportedSchema ||
                !_politicalChroniclePersistenceLoadedForWorld
            )
            {
                return false;
            }

            object customData = GetMapStatsCustomDataObject();
            if (customData == null)
            {
                LogChroniclePersistenceUnavailableOnce();
                return false;
            }

            bool ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleSchemaKey,
                PoliticalChroniclePersistenceSchema.ToString()
            );

            for (int i = 0; i < PoliticalChronicleEntries.Count; i++)
            {
                PoliticalChronicleEntry entry = PoliticalChronicleEntries[i];
                if (entry == null)
                {
                    continue;
                }

                string serialized = SerializePoliticalChronicleEntry(entry);
                if (string.IsNullOrEmpty(serialized))
                {
                    ok = false;
                    continue;
                }

                ok = TrySetCustomDataString(
                    customData,
                    GetPoliticalChronicleSlotKey(entry.Id),
                    serialized
                ) && ok;
            }

            ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleNextIdKey,
                _nextPoliticalChronicleEntryId.ToString()
            ) && ok;
            ok = TrySetCustomDataString(
                customData,
                PoliticalChronicleCountKey,
                PoliticalChronicleEntries.Count.ToString()
            ) && ok;

            _politicalChroniclePersistenceDirty = !ok;
            if (ok)
            {
                _politicalChroniclePersistenceNextRetryAt = 0f;
                LogInfo(
                    "[PW-CHRONICLE] persisted " +
                    PoliticalChronicleEntries.Count + " entries to map stats."
                );
            }
            else
            {
                _politicalChroniclePersistenceNextRetryAt =
                    Time.unscaledTime + 5f;
                LogChroniclePersistenceUnavailableOnce();
            }

            return ok;
        }

        private static bool RestorePoliticalChronicleFromWorldData()
        {
            object customData = GetMapStatsCustomDataObject();
            if (customData == null)
            {
                return false;
            }

            string schemaText;
            if (!TryGetCustomDataString(
                customData,
                PoliticalChronicleSchemaKey,
                out schemaText
            ))
            {
                // Old saves simply do not have Chronicle persistence yet.
                return true;
            }

            int schema;
            if (!int.TryParse(schemaText, out schema))
            {
                return true;
            }

            if (schema > PoliticalChroniclePersistenceSchema)
            {
                _politicalChroniclePersistenceUnsupportedSchema = true;
                LogWarning(
                    "[PW-CHRONICLE] save uses newer chronicle schema " +
                    schema + "; leaving its persisted data untouched."
                );
                return true;
            }

            string countText;
            string nextIdText;
            if (
                !TryGetCustomDataString(
                    customData,
                    PoliticalChronicleCountKey,
                    out countText
                ) ||
                !TryGetCustomDataString(
                    customData,
                    PoliticalChronicleNextIdKey,
                    out nextIdText
                )
            )
            {
                return true;
            }

            int count;
            long nextId;
            if (
                !int.TryParse(countText, out count) ||
                !long.TryParse(nextIdText, out nextId)
            )
            {
                return true;
            }

            count = Math.Max(0, count);
            nextId = Math.Max(1L, nextId);

            long firstId = Math.Max(1L, nextId - count);
            List<PoliticalChronicleEntry> restored =
                new List<PoliticalChronicleEntry>(count);

            for (long expectedId = firstId; expectedId < nextId; expectedId++)
            {
                string payload;
                if (!TryGetCustomDataString(
                    customData,
                    GetPoliticalChronicleSlotKey(expectedId),
                    out payload
                ))
                {
                    continue;
                }

                PoliticalChronicleEntry entry =
                    DeserializePoliticalChronicleEntry(payload);
                if (entry == null || entry.Id != expectedId)
                {
                    continue;
                }

                restored.Add(entry);
            }

            PoliticalChronicleEntries.Clear();
            PoliticalChronicleEntries.AddRange(restored);
            _nextPoliticalChronicleEntryId = nextId;
            _politicalChroniclePersistenceDirty = false;

            LogInfo(
                "[PW-CHRONICLE] restored " + restored.Count +
                " persisted entries (schema " + schema + ")."
            );
            return true;
        }

        private static string GetPoliticalChronicleSlotKey(long entryId)
        {
            long normalized = Math.Max(1L, entryId) - 1L;
            return PoliticalChronicleEntryKeyPrefix +
                normalized.ToString("D4");
        }

        private static string SerializePoliticalChronicleEntry(
            PoliticalChronicleEntry entry
        )
        {
            if (entry == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder(512);
            AppendChronicleField(builder, "E1");
            AppendChronicleField(builder, entry.Id.ToString());
            AppendChronicleField(builder, entry.Year.ToString());
            AppendChronicleField(
                builder,
                ((int)entry.Importance).ToString()
            );
            AppendChronicleField(builder, EncodeChronicleString(entry.EventKey));
            AppendChronicleField(builder, EncodeChronicleString(entry.Category));
            AppendChronicleField(builder, EncodeChronicleString(entry.Text));
            AppendChronicleField(builder, EncodeChronicleString(entry.IconPath));
            AppendChronicleField(builder, EncodeChronicleString(entry.KingdomId));
            AppendChronicleField(builder, EncodeChronicleString(entry.KingdomName));
            AppendChronicleField(builder, EncodeChronicleString(entry.CityId));
            AppendChronicleField(builder, EncodeChronicleString(entry.CityName));
            AppendChronicleField(builder, EncodeChronicleString(entry.ActorId));
            AppendChronicleField(builder, EncodeChronicleString(entry.ActorName));

            int causeCount = entry.Causes == null
                ? 0
                : Math.Min(32, entry.Causes.Count);
            AppendChronicleField(builder, causeCount.ToString());
            for (int i = 0; i < causeCount; i++)
            {
                AppendChronicleField(
                    builder,
                    EncodeChronicleString(entry.Causes[i])
                );
            }

            int consequenceCount = entry.Consequences == null
                ? 0
                : Math.Min(32, entry.Consequences.Count);
            AppendChronicleField(builder, consequenceCount.ToString());
            for (int i = 0; i < consequenceCount; i++)
            {
                AppendChronicleField(
                    builder,
                    EncodeChronicleString(entry.Consequences[i])
                );
            }

            return builder.ToString();
        }

        private static PoliticalChronicleEntry DeserializePoliticalChronicleEntry(
            string payload
        )
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            try
            {
                string[] fields = payload.Split('|');
                int index = 0;
                if (ReadChronicleField(fields, ref index) != "E1")
                {
                    return null;
                }

                long id;
                int year;
                int importance;
                if (
                    !long.TryParse(
                        ReadChronicleField(fields, ref index),
                        out id
                    ) ||
                    !int.TryParse(
                        ReadChronicleField(fields, ref index),
                        out year
                    ) ||
                    !int.TryParse(
                        ReadChronicleField(fields, ref index),
                        out importance
                    )
                )
                {
                    return null;
                }

                PoliticalChronicleEntry entry = new PoliticalChronicleEntry();
                entry.Id = Math.Max(1L, id);
                entry.Year = Math.Max(0, year);
                importance = Math.Max(
                    (int)PoliticalChronicleImportance.Low,
                    Math.Min(
                        (int)PoliticalChronicleImportance.Historic,
                        importance
                    )
                );
                entry.Importance = (PoliticalChronicleImportance)importance;
                entry.EventKey = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.Category = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.Text = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.IconPath = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.KingdomId = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.KingdomName = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.CityId = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.CityName = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.ActorId = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );
                entry.ActorName = DecodeChronicleString(
                    ReadChronicleField(fields, ref index)
                );

                int causeCount;
                if (!int.TryParse(
                    ReadChronicleField(fields, ref index),
                    out causeCount
                ))
                {
                    return null;
                }
                causeCount = Math.Max(0, Math.Min(32, causeCount));
                for (int i = 0; i < causeCount; i++)
                {
                    string value = DecodeChronicleString(
                        ReadChronicleField(fields, ref index)
                    );
                    if (!string.IsNullOrEmpty(value))
                    {
                        entry.Causes.Add(value);
                    }
                }

                int consequenceCount;
                if (!int.TryParse(
                    ReadChronicleField(fields, ref index),
                    out consequenceCount
                ))
                {
                    return null;
                }
                consequenceCount = Math.Max(
                    0,
                    Math.Min(32, consequenceCount)
                );
                for (int i = 0; i < consequenceCount; i++)
                {
                    string value = DecodeChronicleString(
                        ReadChronicleField(fields, ref index)
                    );
                    if (!string.IsNullOrEmpty(value))
                    {
                        entry.Consequences.Add(value);
                    }
                }

                if (string.IsNullOrEmpty(entry.Category))
                {
                    entry.Category = ResolvePoliticalChronicleCategory(
                        entry.EventKey
                    );
                }
                if (string.IsNullOrEmpty(entry.IconPath))
                {
                    entry.IconPath = PoliticsIconPath;
                }

                return entry;
            }
            catch
            {
                return null;
            }
        }

        private static void AppendChronicleField(
            StringBuilder builder,
            string value
        )
        {
            if (builder.Length > 0)
            {
                builder.Append('|');
            }
            builder.Append(value ?? "");
        }

        private static string ReadChronicleField(
            string[] fields,
            ref int index
        )
        {
            if (fields == null || index < 0 || index >= fields.Length)
            {
                throw new IndexOutOfRangeException();
            }
            return fields[index++];
        }

        private static string EncodeChronicleString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string DecodeChronicleString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        private static object GetMapStatsCustomDataObject()
        {
            if (_politicalChronicleCustomDataCache != null)
            {
                return _politicalChronicleCustomDataCache;
            }

            object world = null;
            try
            {
                world = World.world;
            }
            catch
            {
                return null;
            }

            if (world == null)
            {
                return null;
            }

            object mapStats = GetMemberValue(
                world,
                "map_stats",
                "mapStats",
                "stats"
            );
            if (mapStats == null)
            {
                return null;
            }

            object customData = GetMemberValue(
                mapStats,
                "custom_data",
                "customData"
            );

            if (
                customData != null &&
                !SupportsChronicleStringStorage(customData)
            )
            {
                customData = null;
            }

            if (customData == null && SupportsChronicleStringStorage(mapStats))
            {
                customData = mapStats;
            }

            if (customData == null)
            {
                // Compatibility fallback for modded/renamed builds. Only
                // inspect members whose names explicitly mention custom data;
                // do not guess arbitrary storage fields.
                Type current = mapStats.GetType();
                while (current != null && customData == null)
                {
                    FieldInfo[] fields = current.GetFields(MemberFlags);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        if (
                            field == null ||
                            field.Name.IndexOf(
                                "custom",
                                StringComparison.OrdinalIgnoreCase
                            ) < 0
                        )
                        {
                            continue;
                        }

                        try
                        {
                            object candidate = field.GetValue(mapStats);
                            if (SupportsChronicleStringStorage(candidate))
                            {
                                customData = candidate;
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }

                    if (customData == null)
                    {
                        PropertyInfo[] properties = current.GetProperties(
                            MemberFlags
                        );
                        for (int i = 0; i < properties.Length; i++)
                        {
                            PropertyInfo property = properties[i];
                            if (
                                property == null ||
                                !property.CanRead ||
                                property.GetIndexParameters().Length != 0 ||
                                property.Name.IndexOf(
                                    "custom",
                                    StringComparison.OrdinalIgnoreCase
                                ) < 0
                            )
                            {
                                continue;
                            }

                            try
                            {
                                object candidate = property.GetValue(
                                    mapStats,
                                    null
                                );
                                if (SupportsChronicleStringStorage(candidate))
                                {
                                    customData = candidate;
                                    break;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    current = current.BaseType;
                }
            }

            if (!SupportsChronicleStringStorage(customData))
            {
                return null;
            }

            _politicalChronicleCustomDataCache = customData;
            return customData;
        }

        private static bool SupportsChronicleStringStorage(object storage)
        {
            if (storage == null)
            {
                return false;
            }

            EnsureChronicleStringStorageMethods(storage);
            return
                _politicalChronicleSetStringMethod != null &&
                _politicalChronicleGetStringMethod != null;
        }

        private static void EnsureChronicleStringStorageMethods(object storage)
        {
            if (storage == null)
            {
                return;
            }

            Type storageType = storage.GetType();
            if (
                _politicalChronicleCustomDataType == storageType &&
                _politicalChronicleSetStringMethod != null &&
                _politicalChronicleGetStringMethod != null
            )
            {
                return;
            }

            _politicalChronicleCustomDataType = storageType;
            _politicalChronicleSetStringMethod = null;
            _politicalChronicleGetStringMethod = null;

            MethodInfo[] methods = storageType.GetMethods(MemberFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (
                    _politicalChronicleSetStringMethod == null &&
                    string.Equals(
                        method.Name,
                        "set",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(string)
                )
                {
                    _politicalChronicleSetStringMethod = method;
                    continue;
                }

                if (
                    _politicalChronicleGetStringMethod == null &&
                    string.Equals(
                        method.Name,
                        "get",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType.IsByRef &&
                    parameters[1].ParameterType.GetElementType() ==
                        typeof(string) &&
                    parameters[2].ParameterType == typeof(string)
                )
                {
                    _politicalChronicleGetStringMethod = method;
                }
            }
        }

        private static bool TrySetCustomDataString(
            object customData,
            string key,
            string value
        )
        {
            if (customData == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            try
            {
                EnsureChronicleStringStorageMethods(customData);
                if (_politicalChronicleSetStringMethod == null)
                {
                    return false;
                }

                _politicalChronicleSetStringMethod.Invoke(
                    customData,
                    new object[] { key, value ?? "" }
                );
                return true;
            }
            catch
            {
                _politicalChronicleCustomDataCache = null;
                return false;
            }
        }

        private static bool TryGetCustomDataString(
            object customData,
            string key,
            out string value
        )
        {
            value = "";
            if (customData == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            try
            {
                EnsureChronicleStringStorageMethods(customData);
                if (_politicalChronicleGetStringMethod == null)
                {
                    return false;
                }

                const string Missing = "\u0001PW_CHRONICLE_MISSING\u0001";
                object[] args = new object[] { key, Missing, Missing };
                _politicalChronicleGetStringMethod.Invoke(customData, args);
                string found = args[1] as string;
                if (string.Equals(found, Missing, StringComparison.Ordinal))
                {
                    return false;
                }

                value = found ?? "";
                return true;
            }
            catch
            {
                _politicalChronicleCustomDataCache = null;
                return false;
            }
        }

        private static void LogChroniclePersistenceUnavailableOnce()
        {
            if (_politicalChroniclePersistenceUnavailableLogged)
            {
                return;
            }
            _politicalChroniclePersistenceUnavailableLogged = true;
            LogWarning(
                "[PW-CHRONICLE] map stats custom save data is unavailable; " +
                "chronicle will remain runtime-only for this world."
            );
        }
    }
}
