using System;
using System.Collections.Generic;
using System.Text;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// API 1.13: addon ecosystem overview, compatibility checks, event metrics,
    /// framework diagnostics and safe cleanup of runtime-owned registrations.
    ///
    /// Everything here is explicit/on-demand or event-count bookkeeping. No
    /// extra Update loop or automatic world scan is introduced.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public sealed class FrameworkIssue
        {
            public string Level;
            public string Code;
            public string AddonId;
            public string Message;
        }

        public sealed class EventMetric
        {
            public string EventId;
            public long Published;
            public long CallbackDeliveries;
            public int Subscribers;
        }

        public sealed class AddonEcosystemInfo
        {
            public string Id;
            public string Name;
            public string Version;
            public string Author;
            public int RequiredApiMajor;
            public int RequiredApiMinor;
            public string[] RequiredCapabilities;
            public string[] ProvidedCapabilities;
            public string[] MissingCapabilities;
            public bool ApiCompatible;
            public bool CapabilitiesReady;
            public bool Compatible;
            public string CompatibilityMessage;
            public int GenericContentTypes;
            public int GenericContent;
            public int InspectorSections;
            public int ContextActions;
            public int EventSubscriptions;
            public long EventsPublished;
            public long EventCallbacksReceived;
            public int CallbackErrors;
            public int Warnings;
            public int Errors;
        }

        public sealed class FrameworkSnapshot
        {
            public string ApiVersion;
            public int RegisteredAddons;
            public int CustomEvents;
            public int GenericContentTypes;
            public int GenericContent;
            public int InspectorSections;
            public int ContextActions;
            public int EventSubscriptions;
            public long EventsPublished;
            public long EventCallbackDeliveries;
            public int Warnings;
            public int Errors;
            public List<AddonEcosystemInfo> Addons = new List<AddonEcosystemInfo>();
            public List<EventMetric> Events = new List<EventMetric>();
            public List<FrameworkIssue> Issues = new List<FrameworkIssue>();
        }

        public sealed class CleanupResult
        {
            public string AddonId;
            public int EventSubscriptions;
            public int UiRegistrations;
            public int AddonCapabilities;
            public int CustomEvents;
            public int Actions;
            public int RarePoliticalEvents;
            public int GenericContent;
            public int GenericContentTypes;
            public int TotalRemoved;
        }

        private static readonly Dictionary<string, long> EventPublishCounts =
            new Dictionary<string, long>(StringComparer.Ordinal);

        private static readonly Dictionary<string, long> EventCallbackCounts =
            new Dictionary<string, long>(StringComparer.Ordinal);

        private static readonly Dictionary<string, long> AddonPublishCounts =
            new Dictionary<string, long>(StringComparer.Ordinal);

        private static readonly Dictionary<string, long> AddonCallbackCounts =
            new Dictionary<string, long>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string> CustomEventOwners =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly List<FrameworkIssue> FrameworkIssues =
            new List<FrameworkIssue>();

        private const int MaxFrameworkIssues = 64;

        public static class Ecosystem
        {
            public static bool IsCapabilityAvailable(string capability)
            {
                return InternalIsFrameworkCapabilityAvailable(capability);
            }

            public static AddonEcosystemInfo GetAddonStatus(string addonId)
            {
                string owner = Trim(addonId);
                AddonInfo info = GetAddon(owner);
                if (info == null) return null;
                return BuildAddonEcosystemInfo(info);
            }

            public static List<EventMetric> GetEventMetrics()
            {
                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (string id in GetEventIds()) if (!string.IsNullOrEmpty(id)) ids.Add(id);
                foreach (string id in EventPublishCounts.Keys) if (!string.IsNullOrEmpty(id)) ids.Add(id);
                foreach (string id in EventCallbackCounts.Keys) if (!string.IsNullOrEmpty(id)) ids.Add(id);

                List<EventMetric> result = new List<EventMetric>();
                foreach (string id in ids)
                {
                    long published;
                    long callbacks;
                    EventPublishCounts.TryGetValue(id, out published);
                    EventCallbackCounts.TryGetValue(id, out callbacks);
                    result.Add(new EventMetric
                    {
                        EventId = id,
                        Published = published,
                        CallbackDeliveries = callbacks,
                        Subscribers = CountSubscribers(id)
                    });
                }

                result.Sort(delegate(EventMetric a, EventMetric b)
                {
                    long bp = b == null ? 0 : b.Published;
                    long ap = a == null ? 0 : a.Published;
                    int order = bp.CompareTo(ap);
                    if (order != 0) return order;
                    return string.Compare(a == null ? "" : a.EventId, b == null ? "" : b.EventId, StringComparison.Ordinal);
                });
                return result;
            }

            public static List<FrameworkIssue> GetIssues()
            {
                List<FrameworkIssue> result = new List<FrameworkIssue>();
                for (int i = 0; i < FrameworkIssues.Count; i++)
                {
                    FrameworkIssue issue = FrameworkIssues[i];
                    if (issue == null) continue;
                    result.Add(new FrameworkIssue
                    {
                        Level = issue.Level,
                        Code = issue.Code,
                        AddonId = issue.AddonId,
                        Message = issue.Message
                    });
                }
                return result;
            }

            public static FrameworkSnapshot GetSnapshot()
            {
                FrameworkSnapshot snapshot = new FrameworkSnapshot
                {
                    ApiVersion = ApiVersion
                };

                List<AddonInfo> addons = GetRegisteredAddons();
                for (int i = 0; i < addons.Count; i++)
                {
                    AddonInfo addon = addons[i];
                    if (addon == null) continue;
                    snapshot.Addons.Add(BuildAddonEcosystemInfo(addon));
                }

                snapshot.RegisteredAddons = snapshot.Addons.Count;
                snapshot.CustomEvents = RegisteredCustomEventIds.Count;
                snapshot.GenericContentTypes = GenericContentTypes.Count;
                snapshot.GenericContent = GenericContent.Count;
                snapshot.InspectorSections = InspectorSections.Count;
                snapshot.ContextActions = ContextActions.Count;
                snapshot.EventSubscriptions = CountAllSubscriptions();
                snapshot.EventsPublished = SumCounts(EventPublishCounts);
                snapshot.EventCallbackDeliveries = SumCounts(EventCallbackCounts);
                snapshot.Events = GetEventMetrics();
                snapshot.Issues = GetIssues();

                for (int i = 0; i < snapshot.Addons.Count; i++)
                {
                    AddonEcosystemInfo addon = snapshot.Addons[i];
                    if (addon == null) continue;
                    snapshot.Warnings += addon.Warnings;
                    snapshot.Errors += addon.Errors;
                }
                return snapshot;
            }

            public static string GetAddonReport(string addonId)
            {
                AddonEcosystemInfo info = GetAddonStatus(addonId);
                if (info == null) return "Addon is not registered: " + Trim(addonId);

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("[Political World Framework]");
                builder.AppendLine("API: " + ApiVersion);
                builder.AppendLine("Addon: " + (info.Name ?? info.Id) + " [" + (info.Id ?? "") + "]");
                builder.AppendLine("Version: " + (info.Version ?? ""));
                builder.AppendLine("Compatibility: " + (info.Compatible ? "OK" : "CHECK"));
                builder.AppendLine(info.CompatibilityMessage ?? "");
                builder.AppendLine("Provided capabilities: " + info.ProvidedCapabilities.Length);
                builder.AppendLine("Missing capabilities: " + info.MissingCapabilities.Length);
                builder.AppendLine("Generic content: " + info.GenericContent + " (types: " + info.GenericContentTypes + ")");
                builder.AppendLine("Inspector sections: " + info.InspectorSections);
                builder.AppendLine("Context actions: " + info.ContextActions);
                builder.AppendLine("Event subscriptions: " + info.EventSubscriptions);
                builder.AppendLine("Events published: " + info.EventsPublished);
                builder.AppendLine("Callbacks received: " + info.EventCallbacksReceived);
                builder.AppendLine("Callback errors: " + info.CallbackErrors);
                builder.AppendLine("Warnings: " + info.Warnings);
                builder.AppendLine("Errors: " + info.Errors);
                return builder.ToString().TrimEnd();
            }

            public static string GetFrameworkReport()
            {
                FrameworkSnapshot snapshot = GetSnapshot();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("[Political World Framework]");
                builder.AppendLine("API: " + snapshot.ApiVersion);
                builder.AppendLine("Registered addons: " + snapshot.RegisteredAddons);
                builder.AppendLine("Custom events: " + snapshot.CustomEvents);
                builder.AppendLine("Generic content: " + snapshot.GenericContent + " (types: " + snapshot.GenericContentTypes + ")");
                builder.AppendLine("Inspector sections: " + snapshot.InspectorSections);
                builder.AppendLine("Context actions: " + snapshot.ContextActions);
                builder.AppendLine("Event subscriptions: " + snapshot.EventSubscriptions);
                builder.AppendLine("Events published: " + snapshot.EventsPublished);
                builder.AppendLine("Callback deliveries: " + snapshot.EventCallbackDeliveries);
                builder.AppendLine("Warnings: " + snapshot.Warnings);
                builder.AppendLine("Errors: " + snapshot.Errors);

                if (snapshot.Addons.Count > 0)
                {
                    builder.AppendLine("Addons:");
                    for (int i = 0; i < snapshot.Addons.Count; i++)
                    {
                        AddonEcosystemInfo addon = snapshot.Addons[i];
                        if (addon == null) continue;
                        builder.AppendLine(
                            "- " + (addon.Name ?? addon.Id) + " [" + (addon.Id ?? "") + "] " +
                            (addon.Compatible ? "OK" : "CHECK") +
                            "; caps=" + addon.ProvidedCapabilities.Length +
                            "; content=" + addon.GenericContent +
                            "; ui=" + (addon.InspectorSections + addon.ContextActions) +
                            "; subs=" + addon.EventSubscriptions
                        );
                    }
                }
                return builder.ToString().TrimEnd();
            }

            /// <summary>
            /// Removes registrations that can be safely detached at runtime.
            /// Ideologies/governments are deliberately not removed because the
            /// vanilla/core game may already hold references to their assets.
            /// </summary>
            public static CleanupResult CleanupRuntime(string addonId)
            {
                string owner = Trim(addonId);
                CleanupResult result = new CleanupResult { AddonId = owner };
                if (string.IsNullOrEmpty(owner) || string.Equals(owner, CoreModId, StringComparison.Ordinal) || !IsAddonRegistered(owner))
                {
                    return result;
                }

                result.EventSubscriptions = UnsubscribeAll(owner);
                result.UiRegistrations = UI.UnregisterAddonUi(owner);

                string[] features = AddonFeatures.Get(owner);
                for (int i = 0; i < features.Length; i++)
                {
                    if (AddonFeatures.Unregister(owner, features[i])) result.AddonCapabilities++;
                }

                List<string> customEvents = new List<string>();
                foreach (KeyValuePair<string, string> pair in CustomEventOwners)
                {
                    if (string.Equals(pair.Value, owner, StringComparison.Ordinal)) customEvents.Add(pair.Key);
                }
                for (int i = 0; i < customEvents.Count; i++)
                {
                    string eventId = customEvents[i];
                    RemoveAllSubscribersForEvent(eventId);
                    if (RegisteredCustomEventIds.Remove(eventId)) result.CustomEvents++;
                    CustomEventOwners.Remove(eventId);
                }

                AddonContentSummary summary = GetAddonContentSummary(owner);
                if (summary != null)
                {
                    for (int i = 0; i < summary.ActionIds.Length; i++)
                    {
                        if (UnregisterAction(owner, summary.ActionIds[i])) result.Actions++;
                    }
                    for (int i = 0; i < summary.RarePoliticalEventIds.Length; i++)
                    {
                        if (UnregisterRarePoliticalEvent(owner, summary.RarePoliticalEventIds[i])) result.RarePoliticalEvents++;
                    }
                    for (int i = 0; i < summary.GenericContentIds.Length; i++)
                    {
                        if (Content.Unregister(owner, summary.GenericContentIds[i])) result.GenericContent++;
                    }
                    for (int i = 0; i < summary.GenericContentTypeIds.Length; i++)
                    {
                        if (Content.UnregisterType(owner, summary.GenericContentTypeIds[i])) result.GenericContentTypes++;
                    }
                }

                result.TotalRemoved =
                    result.EventSubscriptions +
                    result.UiRegistrations +
                    result.AddonCapabilities +
                    result.CustomEvents +
                    result.Actions +
                    result.RarePoliticalEvents +
                    result.GenericContent +
                    result.GenericContentTypes;

                InternalRecordDiagnostic(owner, "INFO", "PWEC190", "Runtime cleanup removed " + result.TotalRemoved + " registrations.");
                return result;
            }
        }

        internal static bool InternalIsFrameworkCapabilityAvailable(string capability)
        {
            string wanted = Trim(capability);
            if (string.IsNullOrEmpty(wanted)) return false;
            return InternalHasCapabilityFast(wanted) || AddonFeatures.IsProvided(wanted);
        }

        internal static void InternalRecordAddonRequirements(string addonId)
        {
            AddonInfo info = GetAddon(addonId);
            if (info == null || info.RequiredCapabilities == null) return;
            for (int i = 0; i < info.RequiredCapabilities.Length; i++)
            {
                string required = Trim(info.RequiredCapabilities[i]);
                if (string.IsNullOrEmpty(required)) continue;
                if (!InternalIsFrameworkCapabilityAvailable(required))
                {
                    InternalRecordFrameworkIssue(
                        info.Id,
                        "WARN",
                        "PWEC110",
                        "Required capability '" + required + "' is not available yet."
                    );
                }
            }
        }

        internal static void InternalRecordEventPublished(string addonId, string eventId)
        {
            string owner = Trim(addonId);
            string id = Trim(eventId);
            if (string.IsNullOrEmpty(id)) return;
            IncrementCount(EventPublishCounts, id);
            if (!string.IsNullOrEmpty(owner)) IncrementCount(AddonPublishCounts, owner);
        }

        internal static void InternalRecordEventCallback(string addonId, string eventId)
        {
            string owner = Trim(addonId);
            string id = Trim(eventId);
            if (!string.IsNullOrEmpty(id)) IncrementCount(EventCallbackCounts, id);
            if (!string.IsNullOrEmpty(owner)) IncrementCount(AddonCallbackCounts, owner);
        }

        internal static bool InternalRegisterCustomEventOwner(string addonId, string eventId)
        {
            string owner = Trim(addonId);
            string id = Trim(eventId);
            string existing;
            if (CustomEventOwners.TryGetValue(id, out existing))
            {
                if (string.Equals(existing, owner, StringComparison.Ordinal)) return true;
                InternalRecordFrameworkIssue(
                    owner,
                    "WARN",
                    "PWEC220",
                    "Custom event id '" + id + "' is already owned by addon '" + existing + "'."
                );
                return false;
            }
            CustomEventOwners[id] = owner;
            return true;
        }

        internal static void InternalRecordFrameworkIssue(string addonId, string level, string code, string message)
        {
            string normalized = string.IsNullOrWhiteSpace(level) ? "INFO" : level.Trim().ToUpperInvariant();
            FrameworkIssues.Add(new FrameworkIssue
            {
                Level = normalized,
                Code = string.IsNullOrWhiteSpace(code) ? "PWEC000" : code.Trim(),
                AddonId = Trim(addonId),
                Message = message ?? ""
            });
            while (FrameworkIssues.Count > MaxFrameworkIssues) FrameworkIssues.RemoveAt(0);
            InternalRecordDiagnostic(addonId, normalized, code, message);
        }

        private static AddonEcosystemInfo BuildAddonEcosystemInfo(AddonInfo addon)
        {
            string owner = addon == null ? "" : Trim(addon.Id);
            AddonDiagnostics diagnostics = GetAddonDiagnostics(owner);
            AddonContentSummary content = GetAddonContentSummary(owner);
            string[] required = CloneStringArray(addon == null ? null : addon.RequiredCapabilities);
            string[] provided = AddonFeatures.Get(owner);
            List<string> missing = new List<string>();

            for (int i = 0; i < required.Length; i++)
            {
                string cap = Trim(required[i]);
                if (!string.IsNullOrEmpty(cap) && !InternalIsFrameworkCapabilityAvailable(cap)) missing.Add(cap);
            }
            missing.Sort(StringComparer.Ordinal);

            bool apiCompatible = true;
            int reqMajor = addon == null ? 0 : addon.RequiredApiMajor;
            int reqMinor = addon == null ? 0 : addon.RequiredApiMinor;
            if (reqMajor > 0)
            {
                apiCompatible = reqMajor == ApiMajor && reqMinor <= ApiMinor;
            }

            bool capabilitiesReady = missing.Count == 0;
            string message;
            if (!apiCompatible)
            {
                message = "Requires PoliticalWorldAPI " + reqMajor + "." + reqMinor + "+; current API is " + ApiVersion + ".";
            }
            else if (!capabilitiesReady)
            {
                message = "Waiting for capabilities: " + string.Join(", ", missing.ToArray());
            }
            else
            {
                message = "Compatible with the current framework state.";
            }

            long published;
            long callbacks;
            AddonPublishCounts.TryGetValue(owner, out published);
            AddonCallbackCounts.TryGetValue(owner, out callbacks);

            return new AddonEcosystemInfo
            {
                Id = owner,
                Name = addon == null ? owner : addon.Name,
                Version = addon == null ? "" : addon.Version,
                Author = addon == null ? "" : addon.Author,
                RequiredApiMajor = reqMajor,
                RequiredApiMinor = reqMinor,
                RequiredCapabilities = required,
                ProvidedCapabilities = provided,
                MissingCapabilities = missing.ToArray(),
                ApiCompatible = apiCompatible,
                CapabilitiesReady = capabilitiesReady,
                Compatible = apiCompatible && capabilitiesReady,
                CompatibilityMessage = message,
                GenericContentTypes = content == null ? 0 : content.GenericContentTypes,
                GenericContent = content == null ? 0 : content.GenericContent,
                InspectorSections = UI.GetInspectorSectionIds(owner).Length,
                ContextActions = UI.GetContextActionIds(owner).Length,
                EventSubscriptions = CountSubscriptionsByAddon(owner),
                EventsPublished = published,
                EventCallbacksReceived = callbacks,
                CallbackErrors = diagnostics == null ? 0 : diagnostics.CallbackErrors,
                Warnings = diagnostics == null ? 0 : diagnostics.Warnings,
                Errors = diagnostics == null ? 0 : diagnostics.Errors
            };
        }

        private static string[] CloneStringArray(string[] source)
        {
            if (source == null || source.Length == 0) return new string[0];
            string[] result = new string[source.Length];
            for (int i = 0; i < source.Length; i++) result[i] = source[i] == null ? "" : source[i].Trim();
            return result;
        }

        private static void IncrementCount(Dictionary<string, long> target, string key)
        {
            long current;
            target.TryGetValue(key, out current);
            if (current < long.MaxValue) target[key] = current + 1;
        }

        private static long SumCounts(Dictionary<string, long> source)
        {
            long total = 0;
            foreach (long value in source.Values)
            {
                if (value <= 0) continue;
                if (long.MaxValue - total < value) return long.MaxValue;
                total += value;
            }
            return total;
        }

        private static int CountAllSubscriptions()
        {
            int total = 0;
            foreach (List<EventSubscription> list in EventSubscriptions.Values)
            {
                if (list == null) continue;
                total += list.Count;
            }
            return total;
        }

        private static int CountSubscribers(string eventId)
        {
            List<EventSubscription> list;
            return EventSubscriptions.TryGetValue(Trim(eventId), out list) && list != null ? list.Count : 0;
        }

        private static int CountSubscriptionsByAddon(string addonId)
        {
            string owner = Trim(addonId);
            int total = 0;
            foreach (List<EventSubscription> list in EventSubscriptions.Values)
            {
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    EventSubscription subscription = list[i];
                    if (subscription != null && string.Equals(subscription.AddonId, owner, StringComparison.Ordinal)) total++;
                }
            }
            return total;
        }

        private static void RemoveAllSubscribersForEvent(string eventId)
        {
            string id = Trim(eventId);
            List<EventSubscription> list;
            if (!EventSubscriptions.TryGetValue(id, out list) || list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                EventSubscription subscription = list[i];
                if (subscription != null) InternalRecordEventSubscription(subscription.AddonId, id, -1);
            }
            EventSubscriptions.Remove(id);
        }
    }
}
