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
            public const string PoliticalCrisisStarted = "kingdom.crisis.started";
            public const string PoliticalCrisisEnded = "kingdom.crisis.ended";
            public const string LeadershipCrisisStarted = "kingdom.leadership-crisis.started";
            public const string LeadershipCrisisResolved = "kingdom.leadership-crisis.resolved";
            public const string RarePoliticalEventFired = "kingdom.rare-political-event.fired";
            public const string PoliticalEventPublished = "political.event.published";
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
            Events.PoliticalCrisisStarted,
            Events.PoliticalCrisisEnded,
            Events.LeadershipCrisisStarted,
            Events.LeadershipCrisisResolved,
            Events.RarePoliticalEventFired,
            Events.PoliticalEventPublished
        };

        private static readonly Dictionary<string, List<EventSubscription>> EventSubscriptions =
            new Dictionary<string, List<EventSubscription>>(StringComparer.Ordinal);

        private const int MaxEventDispatchDepth = 16;
        private static int _eventDispatchDepth;

        public static string[] GetEventIds()
        {
            return (string[])KnownEventIds.Clone();
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
            if (!IsKnownEventId(wanted) && wanted != Events.All)
            {
                InternalRecordDiagnostic(
                    owner,
                    "ERROR",
                    "PW401",
                    "Unknown Political World event id '" + wanted + "'. Use GetEventIds() for supported events."
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
            int year = -1
        )
        {
            if (!IsKnownEventId(eventId))
            {
                return;
            }

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
                Actor = actor,
                ActorIdentity = actorIdentity ?? "",
                ActorName = actorName ?? "",
                OldName = oldName ?? "",
                NewName = newName ?? "",
                SourceAddonId = sourceAddonId ?? "",
                Category = category ?? "",
                Year = year,
                Text = text ?? "",
                EventKey = eventKey ?? ""
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
                Actor = source.Actor,
                ActorIdentity = source.ActorIdentity,
                ActorName = source.ActorName,
                OldName = source.OldName,
                NewName = source.NewName,
                SourceAddonId = source.SourceAddonId,
                Category = source.Category,
                Year = source.Year,
                Text = source.Text,
                EventKey = source.EventKey
            };
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
