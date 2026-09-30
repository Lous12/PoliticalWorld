using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private static readonly Dictionary<string, float>
            WarStartEventLastRuntimeTime = new Dictionary<string, float>();
        private static PoliticalWorldAPI.WarStartSource ApiWarStartSourceContext =
            PoliticalWorldAPI.WarStartSource.Unknown;

        private static bool IsExplicitPlayerWarPowerCall()
        {
            // Vanilla's force-war god power eventually reaches the exact same
            // DiplomacyManager.startWar method as AI diplomacy. The old patch
            // therefore politely negotiated away a war the player explicitly
            // clicked into existence. Fuck that: a god-power order is final.
            try
            {
                StackTrace trace = new StackTrace(false);
                int count = Math.Min(trace.FrameCount, 24);
                for (int i = 1; i < count; i++)
                {
                    MethodBase method = trace.GetFrame(i).GetMethod();
                    if (method == null) continue;
                    string methodName = (method.Name ?? "").ToLowerInvariant();
                    string typeName = method.DeclaringType == null
                        ? ""
                        : (method.DeclaringType.FullName ?? method.DeclaringType.Name ?? "").ToLowerInvariant();

                    // We are already inside startWar(), so a power-ish
                    // ancestor frame is enough to identify an explicit player
                    // action. Keep the names broad enough for WorldBox builds
                    // that rename PowerAction helpers.
                    bool powerContext =
                        typeName.Contains("godpower") ||
                        typeName.Contains("poweraction") ||
                        typeName.Contains("powerlibrary") ||
                        typeName.Contains("powerbutton") ||
                        typeName.Contains("worldaction") ||
                        typeName.Contains("whisper") ||
                        methodName.Contains("godpower") ||
                        methodName.Contains("poweraction") ||
                        methodName.Contains("forcewar") ||
                        methodName.Contains("startwarpower") ||
                        methodName.Contains("whisper");

                    if (powerContext)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsForcedPlayerWarType(object[] args)
        {
            // WorldBox 0.51.x gives Whisper of War its own forced war type.
            // That is a much more reliable signal than trying to guess the
            // caller from the stack, because the power can pass through
            // generic action/plot helpers before DiplomacyManager.startWar.
            if (args == null)
            {
                return false;
            }

            for (int i = 2; i < args.Length; i++)
            {
                object value = args[i];
                if (value == null || value is bool || value is int || value is float)
                {
                    continue;
                }

                try
                {
                    object idValue = GetMemberValue(
                        value,
                        "id",
                        "_id",
                        "name",
                        "type",
                        "war_type"
                    );
                    string id = idValue == null
                        ? ""
                        : idValue.ToString().ToLowerInvariant();

                    // Whisper and Spite are vanilla player-forced wars. They
                    // must never enter Political World's crisis/concession
                    // pipeline. Player clicked war; player gets war.
                    if (id.Contains("whisper") || id.Contains("spite"))
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

        private static PoliticalWorldAPI.WarStartSource DetermineWarStartSource(
            object[] args = null
        )
        {
            if (ApiWarStartSourceContext != PoliticalWorldAPI.WarStartSource.Unknown)
            {
                return ApiWarStartSourceContext;
            }
            if (IsForcedPlayerWarType(args) || IsExplicitPlayerWarPowerCall())
            {
                return PoliticalWorldAPI.WarStartSource.Player;
            }
            if (_allowPoliticalWarStart)
            {
                return PoliticalWorldAPI.WarStartSource.PoliticalWorld;
            }
            return PoliticalWorldAPI.WarStartSource.AI;
        }

        private static void WarStartPostfix(object[] __args)
        {
            try
            {
                Kingdom attacker;
                Kingdom defender;
                if (!TryExtractDirectWarKingdoms(__args, out attacker, out defender))
                {
                    return;
                }
                if (attacker == null || defender == null || attacker == defender)
                {
                    return;
                }
                if (!IsKingdomPairAtWarSafe(attacker, defender))
                {
                    return;
                }

                string pairKey = GetWarPairKey(attacker, defender);
                float last;
                if (
                    WarStartEventLastRuntimeTime.TryGetValue(pairKey, out last) &&
                    UnityEngine.Time.time - last < 2f
                )
                {
                    return;
                }
                WarStartEventLastRuntimeTime[pairKey] = UnityEngine.Time.time;

                PoliticalWorldAPI.WarStartSource source = DetermineWarStartSource(__args);
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.WarStarted,
                    attacker,
                    newValue: GetWorldObjectDisplayName(defender),
                    category: "war",
                    year: GetWorldYearSafe(),
                    ideologyId: GetStateIdeology(attacker) ?? "",
                    currentId: GetStateIdeologyCurrent(attacker) ?? "",
                    governmentId: GetGovernmentPublicId(attacker) ?? "",
                    targetKingdom: defender,
                    warSource: source.ToString().ToLowerInvariant()
                );
            }
            catch
            {
            }
        }

        private static bool TryStartWarFromApi(
            Kingdom attacker,
            Kingdom defender,
            object warType,
            bool bypassPolitics
        )
        {
            if (
                attacker == null ||
                defender == null ||
                attacker == defender ||
                World.world == null
            )
            {
                return false;
            }

            if (IsKingdomPairAtWarSafe(attacker, defender))
            {
                return true;
            }

            EnsureWarDiplomacyPatches();
            object diplomacy = _patchedDiplomacyInstance;
            if (diplomacy == null)
            {
                diplomacy = GetMemberValue(World.world, "diplomacy", "_diplomacy");
            }
            if (diplomacy == null)
            {
                return false;
            }

            MethodInfo method = _patchedWarStartMethod ?? FindWarStartMethod(diplomacy.GetType());
            if (method == null)
            {
                return false;
            }

            object[] args = BuildWarStartArguments(method, attacker, defender, warType);
            if (args == null)
            {
                return false;
            }

            bool previousBypass = _allowPoliticalWarStart;
            PoliticalWorldAPI.WarStartSource previousSource = ApiWarStartSourceContext;
            try
            {
                ApiWarStartSourceContext = PoliticalWorldAPI.WarStartSource.Addon;
                if (bypassPolitics)
                {
                    _allowPoliticalWarStart = true;
                }
                method.Invoke(diplomacy, args);
            }
            catch (Exception exception)
            {
                LogWarning("API war start failed: " + exception.Message);
                return false;
            }
            finally
            {
                _allowPoliticalWarStart = previousBypass;
                ApiWarStartSourceContext = previousSource;
            }

            if (IsKingdomPairAtWarSafe(attacker, defender))
            {
                return true;
            }

            string pairKey = GetWarPairKey(attacker, defender);
            return
                PendingWarDeclarations.ContainsKey(pairKey) ||
                PendingDiplomaticCrises.ContainsKey(pairKey);
        }

        private static object[] BuildWarStartArguments(
            MethodInfo method,
            Kingdom attacker,
            Kingdom defender,
            object warType
        )
        {
            if (method == null) return null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length < 2) return null;

            object[] args = new object[parameters.Length];
            args[0] = attacker;
            args[1] = defender;

            for (int i = 2; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (i == 2 && warType != null && parameter.ParameterType.IsInstanceOfType(warType))
                {
                    args[i] = warType;
                    continue;
                }
                if (i == 2 && !parameter.ParameterType.IsValueType)
                {
                    object defaultWarType = FindDefaultWarTypeAsset(parameter.ParameterType);
                    args[i] = defaultWarType;
                    continue;
                }
                if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                    continue;
                }
                if (parameter.ParameterType == typeof(bool))
                {
                    args[i] = false;
                    continue;
                }
                args[i] = parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null;
            }
            return args;
        }

        private static object FindDefaultWarTypeAsset(Type expectedType)
        {
            if (expectedType == null) return null;
            try
            {
                Type library = Type.GetType("WarTypeLibrary, Assembly-CSharp");
                if (library == null) return null;

                string[] preferred = new string[]
                {
                    "normal", "standard", "regular", "conquest", "war"
                };
                for (int n = 0; n < preferred.Length; n++)
                {
                    FieldInfo field = library.GetField(
                        preferred[n],
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase
                    );
                    if (field != null && expectedType.IsAssignableFrom(field.FieldType))
                    {
                        object value = field.GetValue(null);
                        if (value != null) return value;
                    }
                    PropertyInfo property = library.GetProperty(
                        preferred[n],
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase
                    );
                    if (property != null && expectedType.IsAssignableFrom(property.PropertyType))
                    {
                        object value = property.GetValue(null, null);
                        if (value != null) return value;
                    }
                }

                FieldInfo[] fields = library.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field != null && expectedType.IsAssignableFrom(field.FieldType))
                    {
                        object value = field.GetValue(null);
                        if (value != null) return value;
                    }
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
