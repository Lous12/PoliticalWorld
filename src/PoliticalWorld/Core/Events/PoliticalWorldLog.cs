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
        private static string GetWorldObjectDisplayName(object obj)
        {
            if (obj == null)
            {
                return "?";
            }

            string[] methodNames =
            {
                "getName",
                "getNameFull",
                "GetName",
                "GetNameFull"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                try
                {
                    MethodInfo method = FindMethodByNameAndParameterCount(
                        obj.GetType(),
                        methodNames[i],
                        0
                    );

                    if (method != null && method.ReturnType == typeof(string))
                    {
                        string result = method.Invoke(obj, null) as string;
                        if (!string.IsNullOrEmpty(result))
                        {
                            return result;
                        }
                    }
                }
                catch
                {
                }
            }

            object direct = GetMemberValue(
                obj,
                "name",
                "_name",
                "name_text",
                "display_name"
            );
            if (direct != null && !string.IsNullOrEmpty(direct.ToString()))
            {
                return direct.ToString();
            }

            object data = GetMemberValue(obj, "data", "_data");
            object dataName = GetMemberValue(
                data,
                "name",
                "_name",
                "name_text"
            );
            if (dataName != null && !string.IsNullOrEmpty(dataName.ToString()))
            {
                return dataName.ToString();
            }

            string identity = GetStableObjectIdentity(obj);
            return string.IsNullOrEmpty(identity) ? "?" : identity;
        }

        private static readonly Dictionary<string, WorldLogAsset>
            PoliticalWorldLogAssets = new Dictionary<string, WorldLogAsset>();

        private static string BuildPoliticalWorldLogAssetId(string iconPath)
        {
            string source = string.IsNullOrEmpty(iconPath)
                ? PoliticsIconPath
                : iconPath;
            string normalized = "";
            bool lastWasSeparator = false;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsLetterOrDigit(c))
                {
                    normalized += char.ToLowerInvariant(c);
                    lastWasSeparator = false;
                }
                else if (!lastWasSeparator)
                {
                    normalized += "_";
                    lastWasSeparator = true;
                }
            }

            normalized = normalized.Trim('_');
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = "default";
            }

            return PoliticalWorldLogMessageId + "_" + normalized;
        }

        private static WorldLogAsset GetOrCreatePoliticalWorldLogAsset(
            string iconPath
        )
        {
            string resolvedIcon = string.IsNullOrEmpty(iconPath)
                ? PoliticsIconPath
                : iconPath;
            string assetId = BuildPoliticalWorldLogAssetId(resolvedIcon);

            WorldLogAsset cached;
            if (PoliticalWorldLogAssets.TryGetValue(assetId, out cached) &&
                cached != null)
            {
                return cached;
            }

            WorldLogAsset asset = AssetManager.world_log_library.get(assetId);
            if (asset == null)
            {
                asset = new WorldLogAsset
                {
                    id = assetId,
                    locale_id = PoliticalWorldLogMessageId,
                    group = "politics",
                    path_icon = resolvedIcon,
                    color = Toolbox.color_log_neutral,
                    text_replacer = delegate(
                        WorldLogMessage pMessage,
                        ref string pText
                    )
                    {
                        pText = pMessage == null
                            ? ""
                            : (pMessage.special1 ?? "");
                    }
                };

                AssetManager.world_log_library.add(asset);
            }

            PoliticalWorldLogAssets[assetId] = asset;
            return asset;
        }

        private static readonly Dictionary<string, float>
            PoliticalEventNextAllowedTime = new Dictionary<string, float>();
        private const int PoliticalEventCooldownCacheLimit = 512;

        private static void PrunePoliticalEventCooldownCache()
        {
            if (
                PoliticalEventNextAllowedTime.Count <=
                    PoliticalEventCooldownCacheLimit
            )
            {
                return;
            }

            float now = Time.time;
            List<string> expired = new List<string>();

            foreach (
                KeyValuePair<string, float> pair
                in PoliticalEventNextAllowedTime
            )
            {
                if (pair.Value <= now)
                {
                    expired.Add(pair.Key);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                PoliticalEventNextAllowedTime.Remove(expired[i]);
            }

            // If an extremely event-heavy world still exceeds the cap,
            // drop the oldest remaining entries. They are only UI cooldowns,
            // never simulation state.
            while (
                PoliticalEventNextAllowedTime.Count >
                    PoliticalEventCooldownCacheLimit
            )
            {
                string oldestKey = null;
                float oldestTime = float.MaxValue;

                foreach (
                    KeyValuePair<string, float> pair
                    in PoliticalEventNextAllowedTime
                )
                {
                    if (pair.Value < oldestTime)
                    {
                        oldestTime = pair.Value;
                        oldestKey = pair.Key;
                    }
                }

                if (string.IsNullOrEmpty(oldestKey))
                {
                    break;
                }

                PoliticalEventNextAllowedTime.Remove(oldestKey);
            }
        }

        private static bool ShouldShowPoliticalEventInWorldLog(
            string eventKey
        )
        {
            if (string.IsNullOrEmpty(eventKey))
            {
                return true;
            }

            // v1.3.9.4.1: these are useful simulation/debug events, but they
            // are too noisy for the same compact feed that shows wars and
            // king deaths. Important outcomes are still shown separately.
            return !(
                eventKey.StartsWith("crisis_rejected_") ||
                eventKey.StartsWith("crisis_escalated_") ||
                eventKey.StartsWith("crisis_ended_") ||
                eventKey.StartsWith("rebellion_started_")
            );
        }

        private static string GetPoliticalWorldLogCooldownKey(
            string eventKey,
            string fallbackText
        )
        {
            if (string.IsNullOrEmpty(eventKey))
            {
                return fallbackText ?? "political_event";
            }

            // Group repetitive low/medium priority notifications by type, not
            // by ideology/party id. This prevents several similar messages
            // from the same kingdom filling the feed at high game speed.
            if (eventKey.StartsWith("movement_formed_"))
                return "movement_formed";
            if (eventKey.StartsWith("movement_radical_"))
                return "movement_radical";
            if (eventKey.StartsWith("party_formed_"))
                return "party_formed";
            if (eventKey.StartsWith("crisis_started_"))
                return "crisis_started";
            if (eventKey.StartsWith("crisis_accepted_"))
                return "crisis_accepted";
            if (eventKey.StartsWith("current_changed_"))
                return "current_changed";

            return eventKey;
        }

        private static float GetPoliticalWorldLogCooldownSeconds(
            string eventKey,
            float requestedCooldown
        )
        {
            float result = Math.Max(1f, requestedCooldown);
            if (string.IsNullOrEmpty(eventKey))
            {
                return result;
            }

            if (eventKey.StartsWith("movement_formed_"))
                return Math.Max(result, 90f);
            if (eventKey.StartsWith("movement_radical_"))
                return Math.Max(result, 75f);
            if (eventKey.StartsWith("party_formed_"))
                return Math.Max(result, 45f);
            if (eventKey.StartsWith("crisis_started_"))
                return Math.Max(result, 75f);
            if (eventKey.StartsWith("crisis_accepted_"))
                return Math.Max(result, 75f);
            if (eventKey.StartsWith("current_changed_"))
                return Math.Max(result, 90f);

            // Coup/revolution/succession/party split remain high-priority and
            // keep their original cooldown behaviour.
            return result;
        }

        private static void PublishPoliticalEvent(
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath,
            string eventKey,
            float cooldownSeconds
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Always keep a textual trace for debugging even when a secondary
            // event is intentionally omitted from the compact WorldLog feed.
            LogInfo("[Political Event] " + text);

            PoliticalWorldAPI.InternalEmitCoreEvent(
                PoliticalWorldAPI.Events.PoliticalEventPublished,
                kingdom,
                "",
                "",
                0,
                0,
                "",
                text,
                eventKey
            );

            if (!ShouldShowPoliticalEventInWorldLog(eventKey))
            {
                return;
            }

            PrunePoliticalEventCooldownCache();

            string kingdomId = GetStableObjectIdentity(kingdom);
            string groupedEventKey = GetPoliticalWorldLogCooldownKey(
                eventKey,
                text
            );
            string key = kingdomId + "|" + groupedEventKey;
            float nextAllowed;

            if (
                PoliticalEventNextAllowedTime.TryGetValue(key, out nextAllowed) &&
                Time.time < nextAllowed
            )
            {
                return;
            }

            float resolvedCooldown = GetPoliticalWorldLogCooldownSeconds(
                eventKey,
                cooldownSeconds
            );
            PoliticalEventNextAllowedTime[key] =
                Time.time + resolvedCooldown;

            bool published = TrySendWorldLogEvent(
                text,
                kingdom,
                city,
                actor,
                iconPath
            );

            if (!published)
            {
                // Do not fall back to WorldTip: WorldTip is cursor-anchored,
                // which is exactly the behaviour we do not want for history
                // notifications. If native WorldLog fails, keep the event in
                // the mod log instead of showing a tooltip under the mouse.
                LogWarning(
                    "Political event could not be added to native WorldLog: " +
                    text
                );
            }
        }

        private static bool TrySendWorldLogEvent(
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath
        )
        {
            try
            {
                // WorldBox 0.51.x stores the icon and formatter on the
                // WorldLogAsset, not on WorldLogMessage. Create/reuse a native
                // asset for each Political World icon, then submit the message
                // through WorldLogMessage.add() so HistoryHud handles the same
                // fixed notification feed as vanilla king deaths and wars.
                WorldLogAsset asset = GetOrCreatePoliticalWorldLogAsset(iconPath);
                if (asset == null)
                {
                    return false;
                }

                WorldLogMessage message = new WorldLogMessage(asset, text);

                if (kingdom != null)
                {
                    message.kingdom = kingdom;
                }

                if (actor != null)
                {
                    message.unit = actor;
                }

                WorldTile locationTile = null;
                if (city != null)
                {
                    locationTile = city.getTile();
                }

                if (
                    locationTile == null &&
                    kingdom != null &&
                    kingdom.capital != null
                )
                {
                    locationTile = kingdom.capital.getTile();
                }

                if (locationTile != null)
                {
                    message.location = locationTile.pos;
                }

                message.add();
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Political native WorldLog publish failed: " +
                    exception.Message
                );
                return false;
            }
        }

        private static object CreateWorldLogMessageReflective(
            Type messageType,
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath
        )
        {
            object message = null;

            try
            {
                ConstructorInfo emptyCtor = messageType.GetConstructor(Type.EmptyTypes);
                if (emptyCtor != null)
                {
                    message = emptyCtor.Invoke(null);
                }
            }
            catch
            {
                message = null;
            }

            if (message == null)
            {
                ConstructorInfo[] constructors = messageType.GetConstructors(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                for (int i = 0; i < constructors.Length && message == null; i++)
                {
                    object[] args = BuildNotificationArguments(
                        constructors[i].GetParameters(),
                        null,
                        messageType,
                        text,
                        kingdom,
                        city,
                        actor,
                        iconPath
                    );
                    if (args == null)
                    {
                        continue;
                    }

                    try
                    {
                        message = constructors[i].Invoke(args);
                    }
                    catch
                    {
                        message = null;
                    }
                }
            }

            if (message == null)
            {
                return null;
            }

            TrySetMemberValue(message, text, "text", "message", "message_text", "text_value");
            TrySetMemberValue(message, kingdom, "kingdom", "main_kingdom", "kingdom1");
            TrySetMemberValue(message, city, "city", "main_city");
            TrySetMemberValue(message, actor, "actor", "main_actor");
            TrySetMemberValue(message, iconPath, "icon", "icon_path", "path_icon");

            return message;
        }

        private static bool TryInvokeNotificationMethod(
            object target,
            Type targetType,
            object message,
            Type messageType,
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath
        )
        {
            if (targetType == null)
            {
                return false;
            }

            string[] allowedNames =
            {
                "addMessage",
                "AddMessage",
                "newMessage",
                "NewMessage",
                "createMessage",
                "CreateMessage",
                "pushMessage",
                "PushMessage",
                "showMessage",
                "ShowMessage",
                "addLog",
                "AddLog",
                "add"
            };

            MethodInfo[] methods = targetType.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static
            );

            for (int n = 0; n < allowedNames.Length; n++)
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != allowedNames[n] || method.IsGenericMethod)
                    {
                        continue;
                    }

                    if (!method.IsStatic && target == null)
                    {
                        continue;
                    }

                    object[] args = BuildNotificationArguments(
                        method.GetParameters(),
                        message,
                        messageType,
                        text,
                        kingdom,
                        city,
                        actor,
                        iconPath
                    );
                    if (args == null)
                    {
                        continue;
                    }

                    try
                    {
                        method.Invoke(method.IsStatic ? null : target, args);
                        return true;
                    }
                    catch
                    {
                    }
                }
            }

            return false;
        }

        private static object[] BuildNotificationArguments(
            ParameterInfo[] parameters,
            object message,
            Type messageType,
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath
        )
        {
            object[] args = new object[parameters.Length];
            bool hasUsefulArgument = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                string name = parameters[i].Name == null
                    ? ""
                    : parameters[i].Name.ToLowerInvariant();

                if (
                    message != null &&
                    messageType != null &&
                    type.IsAssignableFrom(messageType)
                )
                {
                    args[i] = message;
                    hasUsefulArgument = true;
                }
                else if (type == typeof(string))
                {
                    args[i] = name.Contains("icon") ? iconPath : text;
                    hasUsefulArgument = true;
                }
                else if (type.IsAssignableFrom(typeof(Kingdom)))
                {
                    args[i] = kingdom;
                }
                else if (type.IsAssignableFrom(typeof(City)))
                {
                    args[i] = city;
                }
                else if (type.IsAssignableFrom(typeof(Actor)))
                {
                    args[i] = actor;
                }
                else if (type == typeof(bool))
                {
                    args[i] = false;
                }
                else if (type == typeof(int))
                {
                    args[i] = 0;
                }
                else if (type == typeof(float))
                {
                    args[i] = 0f;
                }
                else if (type == typeof(double))
                {
                    args[i] = 0d;
                }
                else if (type == typeof(Vector3))
                {
                    args[i] = Vector3.zero;
                }
                else if (type == typeof(Vector2))
                {
                    args[i] = Vector2.zero;
                }
                else if (type.IsEnum)
                {
                    Array values = Enum.GetValues(type);
                    args[i] = values.Length > 0
                        ? values.GetValue(0)
                        : Activator.CreateInstance(type);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }
                else if (!type.IsValueType)
                {
                    args[i] = null;
                }
                else
                {
                    try
                    {
                        args[i] = Activator.CreateInstance(type);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return hasUsefulArgument ? args : null;
        }

        private static bool TryShowWorldTip(string text)
        {
            try
            {
                Type tipType = typeof(World).Assembly.GetType("WorldTip");
                if (tipType == null)
                {
                    return false;
                }

                string[] names =
                {
                    "showNow",
                    "ShowNow",
                    "showMessage",
                    "ShowMessage",
                    "show",
                    "Show"
                };
                MethodInfo[] methods = tipType.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance
                );
                object instance = GetStaticMemberValue(
                    tipType,
                    "instance",
                    "Instance",
                    "main"
                );

                for (int n = 0; n < names.Length; n++)
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.Name != names[n])
                        {
                            continue;
                        }
                        object[] args = BuildNotificationArguments(
                            method.GetParameters(),
                            null,
                            null,
                            text,
                            null,
                            null,
                            null,
                            ""
                        );
                        if (args == null || (!method.IsStatic && instance == null))
                        {
                            continue;
                        }

                        try
                        {
                            method.Invoke(method.IsStatic ? null : instance, args);
                            return true;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static object GetStaticMemberValue(
            Type type,
            params string[] names
        )
        {
            if (type == null || names == null)
            {
                return null;
            }

            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], flags);
                if (field != null)
                {
                    try { return field.GetValue(null); } catch { }
                }

                PropertyInfo property = type.GetProperty(names[i], flags);
                if (
                    property != null &&
                    property.GetIndexParameters().Length == 0
                )
                {
                    try { return property.GetValue(null, null); } catch { }
                }
            }

            return null;
        }

        private static void TrySetMemberValue(
            object owner,
            object value,
            params string[] names
        )
        {
            if (owner == null || names == null)
            {
                return;
            }

            Type type = owner.GetType();

            for (int i = 0; i < names.Length; i++)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(names[i], MemberFlags);
                    if (field != null)
                    {
                        try
                        {
                            if (value == null || field.FieldType.IsInstanceOfType(value))
                            {
                                field.SetValue(owner, value);
                                return;
                            }
                        }
                        catch { }
                    }

                    PropertyInfo property = current.GetProperty(names[i], MemberFlags);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            if (value == null || property.PropertyType.IsInstanceOfType(value))
                            {
                                property.SetValue(owner, value, null);
                                return;
                            }
                        }
                        catch { }
                    }
                }
            }
        }

    }
}
