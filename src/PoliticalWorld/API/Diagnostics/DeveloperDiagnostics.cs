using System;
using System.Collections.Generic;
using System.Text;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Lightweight developer diagnostics for addon authors.
    /// Registration/callback bookkeeping only; no Update loop.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public sealed class DiagnosticEntry
        {
            public string Level;
            public string Code;
            public string Message;
        }

        public sealed class AddonDiagnostics
        {
            public string AddonId;
            public int RegisteredIdeologies;
            public int RegisteredGovernments;
            public int RegisteredActions;
            public int RegisteredRarePoliticalEvents;
            public int EventSubscriptions;
            public int CallbackErrors;
            public int Warnings;
            public int Errors;
            public List<DiagnosticEntry> RecentEntries =
                new List<DiagnosticEntry>();
        }

        private sealed class MutableAddonDiagnostics
        {
            public string AddonId;
            public int RegisteredIdeologies;
            public int RegisteredGovernments;
            public int RegisteredActions;
            public int RegisteredRarePoliticalEvents;
            public int EventSubscriptions;
            public int CallbackErrors;
            public int Warnings;
            public int Errors;
            public List<DiagnosticEntry> RecentEntries =
                new List<DiagnosticEntry>();
        }

        private static readonly Dictionary<string, MutableAddonDiagnostics> DiagnosticsByAddon =
            new Dictionary<string, MutableAddonDiagnostics>(StringComparer.Ordinal);

        private const int MaxRecentDiagnosticEntries = 32;

        public static AddonDiagnostics GetAddonDiagnostics(string addonId)
        {
            string owner = addonId == null ? "" : addonId.Trim();
            MutableAddonDiagnostics source;
            if (!DiagnosticsByAddon.TryGetValue(owner, out source) || source == null)
            {
                return new AddonDiagnostics()
                {
                    AddonId = owner
                };
            }

            AddonDiagnostics result = new AddonDiagnostics()
            {
                AddonId = source.AddonId,
                RegisteredIdeologies = source.RegisteredIdeologies,
                RegisteredGovernments = source.RegisteredGovernments,
                RegisteredActions = source.RegisteredActions,
                RegisteredRarePoliticalEvents = source.RegisteredRarePoliticalEvents,
                EventSubscriptions = source.EventSubscriptions,
                CallbackErrors = source.CallbackErrors,
                Warnings = source.Warnings,
                Errors = source.Errors
            };

            for (int i = 0; i < source.RecentEntries.Count; i++)
            {
                DiagnosticEntry item = source.RecentEntries[i];
                if (item == null) continue;
                result.RecentEntries.Add(new DiagnosticEntry()
                {
                    Level = item.Level,
                    Code = item.Code,
                    Message = item.Message
                });
            }
            return result;
        }

        public static string GetDiagnosticsReport(string addonId)
        {
            string owner = addonId == null ? "" : addonId.Trim();
            AddonDiagnostics info = GetAddonDiagnostics(owner);
            AddonInfo addon = FindRegisteredAddon(owner);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[Political World API]");
            builder.AppendLine("API: " + ApiVersion);
            builder.AppendLine(
                "Addon: " +
                (addon == null ? owner : ((addon.Name ?? owner) + " [" + owner + "]"))
            );
            builder.AppendLine("Registered ideologies: " + info.RegisteredIdeologies);
            builder.AppendLine("Registered governments: " + info.RegisteredGovernments);
            builder.AppendLine("Registered actions: " + info.RegisteredActions);
            builder.AppendLine("Registered rare political events: " + info.RegisteredRarePoliticalEvents);
            builder.AppendLine("Event subscriptions: " + info.EventSubscriptions);
            builder.AppendLine("Callback errors: " + info.CallbackErrors);
            builder.AppendLine("Warnings: " + info.Warnings);
            builder.AppendLine("Errors: " + info.Errors);

            if (info.RecentEntries != null && info.RecentEntries.Count > 0)
            {
                builder.AppendLine("Recent diagnostics:");
                for (int i = 0; i < info.RecentEntries.Count; i++)
                {
                    DiagnosticEntry entry = info.RecentEntries[i];
                    if (entry == null) continue;
                    builder.AppendLine(
                        "- " +
                        (entry.Level ?? "INFO") +
                        " " +
                        (entry.Code ?? "PWDIAG000") +
                        ": " +
                        (entry.Message ?? "")
                    );
                }
            }

            return builder.ToString().TrimEnd();
        }

        public static string[] GetAllDiagnosticsReports()
        {
            List<string> owners = new List<string>();
            foreach (KeyValuePair<string, AddonInfo> pair in RegisteredAddons)
            {
                if (string.Equals(pair.Key, CoreModId, StringComparison.Ordinal))
                {
                    continue;
                }
                owners.Add(pair.Key);
            }
            owners.Sort(StringComparer.Ordinal);

            string[] result = new string[owners.Count];
            for (int i = 0; i < owners.Count; i++)
            {
                result[i] = GetDiagnosticsReport(owners[i]);
            }
            return result;
        }

        public static void LogDiagnosticsReport(string addonId)
        {
            string report = GetDiagnosticsReport(addonId);
            try
            {
                NeoModLoader.services.LogService.LogInfo(report);
            }
            catch
            {
                try
                {
                    UnityEngine.Debug.Log(report);
                }
                catch
                {
                }
            }
        }

        internal static void InternalEnsureDiagnostics(string addonId)
        {
            string owner = addonId == null ? "" : addonId.Trim();
            if (string.IsNullOrEmpty(owner))
            {
                return;
            }

            if (!DiagnosticsByAddon.ContainsKey(owner))
            {
                DiagnosticsByAddon[owner] = new MutableAddonDiagnostics()
                {
                    AddonId = owner
                };
            }
        }

        internal static void InternalRecordIdeologyRegistered(
            string addonId,
            string ideologyId
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredIdeologies++;
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG010",
                "Registered ideology '" + (ideologyId ?? "") + "'."
            );
        }

        internal static void InternalRecordGovernmentRegistered(
            string addonId,
            string governmentId
        )
        {
            MutableAddonDiagnostics diagnostics = GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredGovernments++;
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG015",
                "Registered government '" + (governmentId ?? "") + "'."
            );
        }

        internal static void InternalRecordRarePoliticalEventRegistered(
            string addonId,
            string eventId
        )
        {
            MutableAddonDiagnostics diagnostics = GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredRarePoliticalEvents++;
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG016",
                "Registered rare political event '" + (eventId ?? "") + "'."
            );
        }

        internal static void InternalRecordRarePoliticalEventUnregistered(
            string addonId,
            string eventId
        )
        {
            MutableAddonDiagnostics diagnostics = GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredRarePoliticalEvents =
                Math.Max(0, diagnostics.RegisteredRarePoliticalEvents - 1);
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG017",
                "Unregistered rare political event '" + (eventId ?? "") + "'."
            );
        }

        internal static void InternalRecordActionRegistered(
            string addonId,
            string actionId
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredActions++;
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG020",
                "Registered action '" + (actionId ?? "") + "'."
            );
        }

        internal static void InternalRecordActionUnregistered(
            string addonId,
            string actionId
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.RegisteredActions =
                Math.Max(0, diagnostics.RegisteredActions - 1);
            AddRecentDiagnostic(
                diagnostics,
                "INFO",
                "PWDIAG021",
                "Unregistered action '" + (actionId ?? "") + "'."
            );
        }

        internal static void InternalRecordEventSubscription(
            string addonId,
            string eventId,
            int delta
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.EventSubscriptions =
                Math.Max(0, diagnostics.EventSubscriptions + delta);
            if (delta > 0)
            {
                AddRecentDiagnostic(
                    diagnostics,
                    "INFO",
                    "PWDIAG030",
                    "Subscribed to '" + (eventId ?? "") + "'."
                );
            }
        }

        internal static void InternalRecordCallbackError(
            string addonId,
            string eventId,
            Exception exception
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;
            diagnostics.CallbackErrors++;
            diagnostics.Errors++;
            AddRecentDiagnostic(
                diagnostics,
                "ERROR",
                "PWDIAG040",
                "Callback for '" +
                (eventId ?? "") +
                "' failed: " +
                (exception == null ? "unknown exception" : exception.Message)
            );
        }

        internal static void InternalRecordDiagnostic(
            string addonId,
            string level,
            string code,
            string message
        )
        {
            MutableAddonDiagnostics diagnostics =
                GetMutableDiagnostics(addonId);
            if (diagnostics == null) return;

            string normalized = string.IsNullOrWhiteSpace(level)
                ? "INFO"
                : level.Trim().ToUpperInvariant();

            if (normalized == "WARN" || normalized == "WARNING")
            {
                diagnostics.Warnings++;
                normalized = "WARN";
            }
            else if (normalized == "ERROR")
            {
                diagnostics.Errors++;
            }
            else
            {
                normalized = "INFO";
            }

            AddRecentDiagnostic(
                diagnostics,
                normalized,
                code,
                message
            );
        }

        private static MutableAddonDiagnostics GetMutableDiagnostics(
            string addonId
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            if (string.IsNullOrEmpty(owner))
            {
                return null;
            }

            InternalEnsureDiagnostics(owner);
            MutableAddonDiagnostics diagnostics;
            DiagnosticsByAddon.TryGetValue(owner, out diagnostics);
            return diagnostics;
        }

        private static AddonInfo FindRegisteredAddon(string addonId)
        {
            if (string.IsNullOrWhiteSpace(addonId))
            {
                return null;
            }
            AddonInfo info;
            RegisteredAddons.TryGetValue(addonId.Trim(), out info);
            return info;
        }

        private static void AddRecentDiagnostic(
            MutableAddonDiagnostics diagnostics,
            string level,
            string code,
            string message
        )
        {
            if (diagnostics == null)
            {
                return;
            }

            diagnostics.RecentEntries.Add(new DiagnosticEntry()
            {
                Level = level ?? "INFO",
                Code = code ?? "PWDIAG000",
                Message = message ?? ""
            });

            while (diagnostics.RecentEntries.Count > MaxRecentDiagnosticEntries)
            {
                diagnostics.RecentEntries.RemoveAt(0);
            }
        }
    }
}
