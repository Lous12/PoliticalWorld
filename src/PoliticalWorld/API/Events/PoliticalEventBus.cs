using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Event-driven extension surface for Political World addons.
    /// The core emits audited political transitions without adding polling.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public delegate void PoliticalEventHandler(PoliticalEventData data);

        public static class Events
        {
            public const string All = "*";
            public const string IdeologyChanged = "kingdom.ideology.changed";
            public const string CurrentChanged = "kingdom.current.changed";
            public const string GovernmentChanged = "kingdom.government.changed";
            public const string PartyCreated = "party.created";
            public const string PartyActivated = "party.activated";
            public const string PartyDeactivated = "party.deactivated";
            public const string PartyRenamed = "party.renamed";
            public const string PartyIdeologyChanged = "party.ideology.changed";
            public const string PartyLeaderChanged = "party.leader.changed";
            public const string RulingPartyChanged = "kingdom.ruling-party.changed";
            public const string PartyRadicalismChanged = "party.radicalism.changed";
            public const string PartySupportChanged = "party.support.changed";
            public const string RulerChanged = "kingdom.ruler.changed";
            public const string ElectionFinished = "kingdom.election.finished";
            public const string CountryNameChanged = "kingdom.country-name.changed";
            public const string WarStarted = "kingdom.war.started";
            public const string WarEnded = "kingdom.war.ended";
            public const string PoliticalCrisisStarted = "kingdom.crisis.started";
            public const string PoliticalCrisisEnded = "kingdom.crisis.ended";
            public const string SettlementSeparatistMovementStarted = "settlement.separatism.movement-started";
            public const string SettlementAutonomyDemanded = "settlement.separatism.autonomy-demanded";
            public const string SettlementSecessionCrisisStarted = "settlement.separatism.secession-crisis-started";
            public const string SettlementSeparatismEnded = "settlement.separatism.ended";
            public const string LeadershipCrisisStarted = "kingdom.leadership-crisis.started";
            public const string LeadershipCrisisResolved = "kingdom.leadership-crisis.resolved";
            public const string RarePoliticalEventFired = "kingdom.rare-political-event.fired";
            public const string PoliticalEventPublished = "political.event.published";
            // API 1.11 world lifecycle hooks. These are emitted from the
            // existing Political World runtime loop and therefore add no new
            // polling coroutine or Update method.
            public const string WorldChanged = "world.changed";
            public const string WorldReady = "world.ready";
            public const string WorldUnavailable = "world.unavailable";
        }

        public sealed class PoliticalEventData
        {
            public string EventId;
            public Kingdom Kingdom;
            public string KingdomName;
            public string OldValue;
            public string NewValue;
            public int OldNumber;
            public int NewNumber;
            public string PartyId;
            // API 1.15 typed political context. Addons no longer need to
            // reverse-engineer winner/current/government IDs from EventKey.
            public string IdeologyId;
            public string CurrentId;
            public string GovernmentId;
            public Kingdom TargetKingdom;
            public string TargetKingdomName;
            public string WarSource;
            public Actor Actor;
            public string ActorIdentity;
            public string ActorName;
            public string OldName;
            public string NewName;
            public string SourceAddonId;
            public string Category;
            public int Year;
            public string Text;
            public string EventKey;
            // API 1.10 general-framework additions. These fields are optional
            // for existing political events and populated by custom addon events.
            public City City;
            public Dictionary<string, string> Payload;
        }

        private sealed class EventSubscription
        {
            public string AddonId;
            public string EventId;
            public PoliticalEventHandler Handler;
        }

        private static readonly string[] KnownEventIds = new string[]
        {
            Events.IdeologyChanged,
            Events.CurrentChanged,
            Events.GovernmentChanged,
            Events.PartyCreated,
            Events.PartyActivated,
            Events.PartyDeactivated,
            Events.PartyRenamed,
            Events.PartyIdeologyChanged,
            Events.PartyLeaderChanged,
            Events.RulingPartyChanged,
            Events.PartyRadicalismChanged,
            Events.PartySupportChanged,
            Events.RulerChanged,
            Events.ElectionFinished,
            Events.CountryNameChanged,
            Events.WarStarted,
            Events.WarEnded,
            Events.PoliticalCrisisStarted,
            Events.PoliticalCrisisEnded,
            Events.SettlementSeparatistMovementStarted,
            Events.SettlementAutonomyDemanded,
            Events.SettlementSecessionCrisisStarted,
            Events.SettlementSeparatismEnded,
            Events.LeadershipCrisisStarted,
            Events.LeadershipCrisisResolved,
            Events.RarePoliticalEventFired,
            Events.PoliticalEventPublished,
            Events.WorldChanged,
            Events.WorldReady,
            Events.WorldUnavailable
        };

        private static readonly Dictionary<string, List<EventSubscription>> EventSubscriptions =
            new Dictionary<string, List<EventSubscription>>(StringComparer.Ordinal);

        private static readonly HashSet<string> RegisteredCustomEventIds =
            new HashSet<string>(StringComparer.Ordinal);

        private const int MaxEventDispatchDepth = 16;
        private static int _eventDispatchDepth;

        public static string[] GetEventIds()
        {
            List<string> result = new List<string>(KnownEventIds);
            foreach (string eventId in RegisteredCustomEventIds)
            {
                if (!string.IsNullOrEmpty(eventId)) result.Add(eventId);
            }
            result.Sort(StringComparer.Ordinal);
            return result.ToArray();
        }

        public static bool Subscribe(
            string addonId,
            string eventId,
            PoliticalEventHandler handler
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string wanted = eventId == null ? "" : eventId.Trim();

            if (!IsAddonRegistered(owner))
            {
                return false;
            }
            if (handler == null)
            {
                InternalRecordDiagnostic(
                    owner,
                    "ERROR",
                    "PW400",
                    "Cannot subscribe to '" + wanted + "': handler is null."
                );
                return false;
            }
            if (!IsSubscribableEventId(wanted) && wanted != Events.All)
            {
                InternalRecordDiagnostic(
                    owner,
                    "ERROR",
                    "PW401",
                    "Invalid event id '" + wanted + "'. Core events use GetEventIds(); custom events must be namespaced."
                );
                return false;
            }

            List<EventSubscription> list;
            if (!EventSubscriptions.TryGetValue(wanted, out list))
            {
                list = new List<EventSubscription>();
                EventSubscriptions[wanted] = list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                EventSubscription existing = list[i];
                if (
                    existing != null &&
                    string.Equals(existing.AddonId, owner, StringComparison.Ordinal) &&
                    object.Equals(existing.Handler, handler)
                )
                {
                    InternalRecordDiagnostic(
                        owner,
                        "WARN",
                        "PW402",
                        "Duplicate subscription ignored for event '" + wanted + "'."
                    );
                    return false;
                }
            }

            list.Add(new EventSubscription()
            {
                AddonId = owner,
                EventId = wanted,
                Handler = handler
            });

            InternalRecordEventSubscription(owner, wanted, 1);
            return true;
        }

        public static bool Unsubscribe(
            string addonId,
            string eventId,
            PoliticalEventHandler handler
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string wanted = eventId == null ? "" : eventId.Trim();
            if (!IsAddonRegistered(owner) || handler == null)
            {
                return false;
            }

            List<EventSubscription> list;
            if (!EventSubscriptions.TryGetValue(wanted, out list))
            {
                return false;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                EventSubscription item = list[i];
                if (
                    item != null &&
                    string.Equals(item.AddonId, owner, StringComparison.Ordinal) &&
                    object.Equals(item.Handler, handler)
                )
                {
                    list.RemoveAt(i);
                    if (list.Count == 0)
                    {
                        EventSubscriptions.Remove(wanted);
                    }
                    InternalRecordEventSubscription(owner, wanted, -1);
                    return true;
                }
            }
            return false;
        }

        public static int UnsubscribeAll(string addonId)
        {
            string owner = addonId == null ? "" : addonId.Trim();
            if (!IsAddonRegistered(owner))
            {
                return 0;
            }

            int removed = 0;
            List<string> keys = new List<string>(EventSubscriptions.Keys);
            for (int k = 0; k < keys.Count; k++)
            {
                string key = keys[k];
                List<EventSubscription> list;
                if (!EventSubscriptions.TryGetValue(key, out list))
                {
                    continue;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    EventSubscription item = list[i];
                    if (item != null && string.Equals(item.AddonId, owner, StringComparison.Ordinal))
                    {
                        list.RemoveAt(i);
                        removed++;
                    }
                }

                if (list.Count == 0)
                {
                    EventSubscriptions.Remove(key);
                }
            }

            if (removed > 0)
            {
                InternalRecordEventSubscription(owner, Events.All, -removed);
            }
            return removed;
        }

        internal static bool InternalRegisterCustomEvent(
            string addonId,
            string eventId
        )
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string wanted = eventId == null ? "" : eventId.Trim();
            if (!IsAddonRegistered(owner) || !IsOwnedContentId(owner, wanted) || !IsSafeCustomEventId(wanted))
            {
                return false;
            }
            if (!InternalRegisterCustomEventOwner(owner, wanted))
            {
                return false;
            }
            RegisteredCustomEventIds.Add(wanted);
            return true;
        }

        internal static bool InternalEmitAddonEvent(
            string sourceAddonId,
            string eventId,
            IDictionary<string, string> payload,
            Kingdom kingdom,
            City city,
            Actor actor,
            string category
        )
        {
            string source = sourceAddonId == null ? "" : sourceAddonId.Trim();
            string wanted = eventId == null ? "" : eventId.Trim();
            if (!IsAddonRegistered(source) || !RegisteredCustomEventIds.Contains(wanted))
            {
                return false;
            }

            InternalRecordEventPublished(source, wanted);

            // Publishing an event with no listeners is still considered a
            // successful publish. This keeps producers independent from load
            // order and from whether any consumer is installed.
            if (!HasSubscribers(wanted))
            {
                return true;
            }

            if (_eventDispatchDepth >= MaxEventDispatchDepth)
            {
                try
                {
                    NeoModLoader.services.LogService.LogError(
                        "[Political World API] Event dispatch depth limit reached for '" +
                        wanted +
                        "'. A callback may be causing a recursive event loop."
                    );
                }
                catch
                {
                }
                return false;
            }

            string kingdomName = "";
            if (kingdom != null)
            {
                try { kingdomName = Main.ScenarioBridge.GetKingdomDisplayName(kingdom); }
                catch { kingdomName = ""; }
            }

            PoliticalEventData data = new PoliticalEventData()
            {
                EventId = wanted,
                Kingdom = kingdom,
                KingdomName = kingdomName,
                Actor = actor,
                City = city,
                SourceAddonId = source,
                Category = category ?? "",
                Payload = ClonePayload(payload)
            };

            _eventDispatchDepth++;
            try
            {
                DispatchToSubscribers(wanted, data);
                DispatchToSubscribers(Events.All, data);
            }
            finally
            {
                _eventDispatchDepth--;
            }
            return true;
        }

        internal static void InternalEmitCoreEvent(
            string eventId,
            Kingdom kingdom,
            string oldValue = "",
            string newValue = "",
            int oldNumber = 0,
            int newNumber = 0,
            string partyId = "",
            string text = "",
            string eventKey = "",
            Actor actor = null,
            string actorIdentity = "",
            string actorName = "",
            string oldName = "",
            string newName = "",
            string sourceAddonId = "",
            string category = "",
            int year = -1,
            string ideologyId = "",
            string currentId = "",
            string governmentId = "",
            Kingdom targetKingdom = null,
            string warSource = "",
            IDictionary<string, string> payload = null,
            City city = null
        )
        {
            if (!IsKnownEventId(eventId))
            {
                return;
            }

            InternalRecordEventPublished(
                string.IsNullOrEmpty(sourceAddonId) ? CoreModId : sourceAddonId,
                eventId
            );

            if (!HasSubscribers(eventId))
            {
                return;
            }

            if (_eventDispatchDepth >= MaxEventDispatchDepth)
            {
                try
                {
                    NeoModLoader.services.LogService.LogError(
                        "[Political World API] Event dispatch depth limit reached for '" +
                        eventId +
                        "'. A callback may be causing a recursive event loop."
                    );
                }
                catch
                {
                }
                return;
            }

            string kingdomName = "";
            if (kingdom != null)
            {
                try
                {
                    kingdomName = Main.ScenarioBridge.GetKingdomDisplayName(kingdom);
                }
                catch
                {
                    kingdomName = "";
                }
            }

            string targetKingdomName = "";
            if (targetKingdom != null)
            {
                try
                {
                    targetKingdomName = Main.ScenarioBridge.GetKingdomDisplayName(targetKingdom);
                }
                catch
                {
                    targetKingdomName = "";
                }
            }

            PoliticalEventData data = new PoliticalEventData()
            {
                EventId = eventId ?? "",
                Kingdom = kingdom,
                KingdomName = kingdomName,
                OldValue = oldValue ?? "",
                NewValue = newValue ?? "",
                OldNumber = oldNumber,
                NewNumber = newNumber,
                PartyId = partyId ?? "",
                IdeologyId = ideologyId ?? "",
                CurrentId = currentId ?? "",
                GovernmentId = governmentId ?? "",
                TargetKingdom = targetKingdom,
                TargetKingdomName = targetKingdomName,
                WarSource = warSource ?? "",
                Actor = actor,
                ActorIdentity = actorIdentity ?? "",
                ActorName = actorName ?? "",
                OldName = oldName ?? "",
                NewName = newName ?? "",
                SourceAddonId = sourceAddonId ?? "",
                Category = category ?? "",
                Year = year,
                Text = text ?? "",
                EventKey = eventKey ?? "",
                City = city,
                Payload = ClonePayload(payload)
            };

            _eventDispatchDepth++;
            try
            {
                DispatchToSubscribers(eventId, data);
                DispatchToSubscribers(Events.All, data);
            }
            finally
            {
                _eventDispatchDepth--;
            }
        }

        private static void DispatchToSubscribers(
            string eventId,
            PoliticalEventData data
        )
        {
            List<EventSubscription> list;
            if (!EventSubscriptions.TryGetValue(eventId, out list) || list == null || list.Count == 0)
            {
                return;
            }

            List<EventSubscription> snapshot =
                new List<EventSubscription>(list);

            for (int i = 0; i < snapshot.Count; i++)
            {
                EventSubscription subscription = snapshot[i];
                if (subscription == null || subscription.Handler == null)
                {
                    continue;
                }

                try
                {
                    InternalRecordEventCallback(
                        subscription.AddonId,
                        data == null ? eventId : data.EventId
                    );
                    subscription.Handler(CloneEventData(data));
                }
                catch (Exception exception)
                {
                    InternalRecordCallbackError(
                        subscription.AddonId,
                        data == null ? eventId : data.EventId,
                        exception
                    );

                    try
                    {
                        NeoModLoader.services.LogService.LogError(
                            "[Political World API] Addon '" +
                            (subscription.AddonId ?? "") +
                            "' callback failed for '" +
                            (data == null ? eventId : data.EventId) +
                            "': " +
                            exception.Message
                        );
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool HasSubscribers(string eventId)
        {
            List<EventSubscription> exact;
            if (
                EventSubscriptions.TryGetValue(eventId, out exact) &&
                exact != null &&
                exact.Count > 0
            )
            {
                return true;
            }

            List<EventSubscription> wildcard;
            return
                EventSubscriptions.TryGetValue(Events.All, out wildcard) &&
                wildcard != null &&
                wildcard.Count > 0;
        }

        private static PoliticalEventData CloneEventData(PoliticalEventData source)
        {
            if (source == null)
            {
                return null;
            }

            return new PoliticalEventData()
            {
                EventId = source.EventId,
                Kingdom = source.Kingdom,
                KingdomName = source.KingdomName,
                OldValue = source.OldValue,
                NewValue = source.NewValue,
                OldNumber = source.OldNumber,
                NewNumber = source.NewNumber,
                PartyId = source.PartyId,
                IdeologyId = source.IdeologyId,
                CurrentId = source.CurrentId,
                GovernmentId = source.GovernmentId,
                TargetKingdom = source.TargetKingdom,
                TargetKingdomName = source.TargetKingdomName,
                WarSource = source.WarSource,
                Actor = source.Actor,
                ActorIdentity = source.ActorIdentity,
                ActorName = source.ActorName,
                OldName = source.OldName,
                NewName = source.NewName,
                SourceAddonId = source.SourceAddonId,
                Category = source.Category,
                Year = source.Year,
                Text = source.Text,
                EventKey = source.EventKey,
                City = source.City,
                Payload = ClonePayload(source.Payload)
            };
        }

        private static Dictionary<string, string> ClonePayload(
            IDictionary<string, string> payload
        )
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.Ordinal);
            if (payload == null) return result;

            foreach (KeyValuePair<string, string> pair in payload)
            {
                string key = pair.Key == null ? "" : pair.Key.Trim();
                if (key.Length == 0 || key.Length > 128) continue;
                result[key] = pair.Value ?? "";
            }
            return result;
        }

        private static bool IsSubscribableEventId(string eventId)
        {
            return IsKnownEventId(eventId) ||
                RegisteredCustomEventIds.Contains(eventId == null ? "" : eventId.Trim()) ||
                IsSafeCustomEventId(eventId);
        }

        private static bool IsSafeCustomEventId(string eventId)
        {
            string value = eventId == null ? "" : eventId.Trim();
            if (value.Length < 3 || value.Length > 192 || value.IndexOf('.') < 1)
            {
                return false;
            }
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool safe = char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '_' || c == '-';
                if (!safe) return false;
            }
            return true;
        }

        private static bool IsKnownEventId(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            string wanted = eventId.Trim();
            for (int i = 0; i < KnownEventIds.Length; i++)
            {
                if (string.Equals(KnownEventIds[i], wanted, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
