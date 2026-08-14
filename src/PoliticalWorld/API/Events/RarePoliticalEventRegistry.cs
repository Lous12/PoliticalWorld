using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Lightweight registry for rare kingdom-level political events.
    /// Events are checked from Political World's existing yearly simulation
    /// pipeline, so addons do not need their own Update loop or world scan.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        private sealed class RegisteredRarePoliticalEvent
        {
            public string Owner;
            public string Id;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public int CheckIntervalYears;
            public int CooldownYears;
            public int ChancePermille;
            public bool CheckImmediately;
            public KingdomCondition Condition;
            public KingdomAction Handler;
        }

        private static readonly Dictionary<string, RegisteredRarePoliticalEvent> RegisteredRarePoliticalEvents =
            new Dictionary<string, RegisteredRarePoliticalEvent>(StringComparer.Ordinal);

        private static int _lastRarePoliticalEventEvaluationYear = int.MinValue;
        private static object _lastRarePoliticalEventWorld;

        public static ValidationResult ValidateRarePoliticalEvent(
            string addonId,
            RarePoliticalEventDefinition definition
        )
        {
            ValidationResult result = NewValidationResult();
            string owner = addonId == null ? "" : addonId.Trim();

            if (!IsAddonRegistered(owner))
            {
                AddValidationIssue(
                    result,
                    "PW500",
                    "Addon '" + owner + "' is not registered. Call RegisterAddon first.",
                    true
                );
            }

            if (definition == null)
            {
                AddValidationIssue(result, "PW501", "Rare political event definition is null.", true);
                return FinishValidation(result);
            }

            string id = definition.Id == null ? "" : definition.Id.Trim();
            if (string.IsNullOrEmpty(id))
            {
                AddValidationIssue(result, "PW502", "Rare political event Id is required.", true);
            }
            else if (!IsOwnedContentId(owner, id))
            {
                AddValidationIssue(
                    result,
                    "PW503",
                    "Rare political event Id must be owned by the addon. Example: " + owner + ".event_palace_crisis",
                    true
                );
            }
            else if (RegisteredRarePoliticalEvents.ContainsKey(id))
            {
                AddValidationIssue(result, "PW504", "Rare political event Id '" + id + "' is already registered.", true);
            }

            if (definition.Handler == null)
            {
                AddValidationIssue(result, "PW505", "Rare political event Handler is required.", true);
            }
            if (definition.CheckIntervalYears < 1)
            {
                AddValidationIssue(result, "PW506", "CheckIntervalYears must be at least 1.", true);
            }
            if (definition.CooldownYears < 0)
            {
                AddValidationIssue(result, "PW507", "CooldownYears cannot be negative.", true);
            }
            if (definition.ChancePermille < 0 || definition.ChancePermille > 1000)
            {
                AddValidationIssue(result, "PW508", "ChancePermille must be between 0 and 1000.", true);
            }
            if (definition.ChancePermille == 0)
            {
                AddValidationIssue(
                    result,
                    "PW509",
                    "ChancePermille is 0, so the event will never fire until re-registered with a positive chance.",
                    false
                );
            }
            if (string.IsNullOrWhiteSpace(definition.DisplayName) && string.IsNullOrWhiteSpace(definition.NameKey))
            {
                AddValidationIssue(
                    result,
                    "PW510",
                    "Rare political event has no DisplayName or NameKey; its Id will be shown in developer tools.",
                    false
                );
            }

            return FinishValidation(result);
        }

        public static bool RegisterRarePoliticalEvent(
            string addonId,
            RarePoliticalEventDefinition definition
        )
        {
            ValidationResult validation = ValidateRarePoliticalEvent(addonId, definition);
            if (!validation.IsValid)
            {
                InternalRecordDiagnostic(
                    addonId,
                    "ERROR",
                    "PWDIAG060",
                    "Rare political event registration failed: " + validation.Summary
                );
                LogValidationFailure("RegisterRarePoliticalEvent", validation);
                return false;
            }

            string owner = addonId.Trim();
            string id = definition.Id.Trim();
            RegisteredRarePoliticalEvents[id] = new RegisteredRarePoliticalEvent()
            {
                Owner = owner,
                Id = id,
                NameKey = definition.NameKey == null ? "" : definition.NameKey.Trim(),
                DisplayName = definition.DisplayName == null ? "" : definition.DisplayName.Trim(),
                DescriptionKey = definition.DescriptionKey == null ? "" : definition.DescriptionKey.Trim(),
                Description = definition.Description == null ? "" : definition.Description.Trim(),
                CheckIntervalYears = Math.Max(1, definition.CheckIntervalYears),
                CooldownYears = Math.Max(0, definition.CooldownYears),
                ChancePermille = Math.Max(0, Math.Min(1000, definition.ChancePermille)),
                CheckImmediately = definition.CheckImmediately,
                Condition = definition.Condition,
                Handler = definition.Handler
            };

            InternalRecordRarePoliticalEventRegistered(owner, id);
            return true;
        }

        public static bool UnregisterRarePoliticalEvent(string addonId, string eventId)
        {
            string owner = addonId == null ? "" : addonId.Trim();
            string id = eventId == null ? "" : eventId.Trim();
            if (!IsAddonRegistered(owner) || !IsOwnedContentId(owner, id))
            {
                return false;
            }

            RegisteredRarePoliticalEvent existing;
            if (!RegisteredRarePoliticalEvents.TryGetValue(id, out existing) || existing == null)
            {
                return false;
            }
            if (!string.Equals(existing.Owner, owner, StringComparison.Ordinal))
            {
                return false;
            }

            bool removed = RegisteredRarePoliticalEvents.Remove(id);
            if (removed)
            {
                InternalRecordRarePoliticalEventUnregistered(owner, id);
            }
            return removed;
        }

        public static RarePoliticalEventInfo GetRarePoliticalEvent(string eventId)
        {
            RegisteredRarePoliticalEvent item;
            if (string.IsNullOrWhiteSpace(eventId) ||
                !RegisteredRarePoliticalEvents.TryGetValue(eventId.Trim(), out item))
            {
                return null;
            }
            return ConvertRarePoliticalEvent(item);
        }

        public static List<RarePoliticalEventInfo> GetRarePoliticalEvents()
        {
            List<RarePoliticalEventInfo> result = new List<RarePoliticalEventInfo>();
            foreach (KeyValuePair<string, RegisteredRarePoliticalEvent> pair in RegisteredRarePoliticalEvents)
            {
                RarePoliticalEventInfo info = ConvertRarePoliticalEvent(pair.Value);
                if (info != null) result.Add(info);
            }
            result.Sort((a, b) => string.Compare(
                a == null ? "" : a.Id,
                b == null ? "" : b.Id,
                StringComparison.Ordinal
            ));
            return result;
        }

        /// <summary>
        /// Called once from Political World's existing political pipeline.
        /// The method itself refuses to evaluate more than once per world year.
        /// </summary>
        internal static void InternalEvaluateRarePoliticalEvents(
            List<Kingdom> kingdoms,
            int currentYear
        )
        {
            if (kingdoms == null || kingdoms.Count == 0 || RegisteredRarePoliticalEvents.Count == 0)
            {
                return;
            }

            object currentWorld = World.world;
            if (!object.ReferenceEquals(_lastRarePoliticalEventWorld, currentWorld))
            {
                _lastRarePoliticalEventWorld = currentWorld;
                _lastRarePoliticalEventEvaluationYear = int.MinValue;
            }

            if (currentYear < 0 || currentYear == _lastRarePoliticalEventEvaluationYear)
            {
                return;
            }

            _lastRarePoliticalEventEvaluationYear = currentYear;

            List<RegisteredRarePoliticalEvent> events =
                new List<RegisteredRarePoliticalEvent>(RegisteredRarePoliticalEvents.Values);
            events.Sort((a, b) => string.Compare(
                a == null ? "" : a.Id,
                b == null ? "" : b.Id,
                StringComparison.Ordinal
            ));

            for (int k = 0; k < kingdoms.Count; k++)
            {
                Kingdom kingdom = kingdoms[k];
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                for (int i = 0; i < events.Count; i++)
                {
                    RegisteredRarePoliticalEvent definition = events[i];
                    if (definition == null || definition.Handler == null)
                    {
                        continue;
                    }
                    EvaluateRarePoliticalEventForKingdom(definition, kingdom, currentYear);
                }
            }
        }

        private static void EvaluateRarePoliticalEventForKingdom(
            RegisteredRarePoliticalEvent definition,
            Kingdom kingdom,
            int currentYear
        )
        {
            string lastCheckKey = "__pw_rare_last_check:" + definition.Id;
            string lastFireKey = "__pw_rare_last_fire:" + definition.Id;
            const int missing = int.MinValue;

            int lastCheck = GetKingdomInt(
                kingdom,
                definition.Owner,
                lastCheckKey,
                missing
            );

            if (lastCheck == missing)
            {
                SetKingdomInt(kingdom, definition.Owner, lastCheckKey, currentYear);
                if (!definition.CheckImmediately)
                {
                    return;
                }
            }
            else if (currentYear - lastCheck < definition.CheckIntervalYears)
            {
                return;
            }
            else
            {
                SetKingdomInt(kingdom, definition.Owner, lastCheckKey, currentYear);
            }

            int lastFire = GetKingdomInt(
                kingdom,
                definition.Owner,
                lastFireKey,
                missing
            );
            if (lastFire != missing && currentYear - lastFire < definition.CooldownYears)
            {
                return;
            }

            if (definition.Condition != null)
            {
                bool eligible;
                try
                {
                    eligible = definition.Condition(kingdom);
                }
                catch (Exception exception)
                {
                    InternalRecordCallbackError(
                        definition.Owner,
                        "rare.condition:" + definition.Id,
                        exception
                    );
                    LogRareEventCallbackFailure(
                        definition.Owner,
                        definition.Id,
                        "condition",
                        exception
                    );
                    return;
                }
                if (!eligible)
                {
                    return;
                }
            }

            if (definition.ChancePermille <= 0)
            {
                return;
            }
            if (definition.ChancePermille < 1000 && UnityEngine.Random.Range(0, 1000) >= definition.ChancePermille)
            {
                return;
            }

            // Record the attempt before invoking addon code. A broken callback
            // therefore respects its cooldown instead of throwing every year.
            SetKingdomInt(kingdom, definition.Owner, lastFireKey, currentYear);

            try
            {
                definition.Handler(kingdom);
            }
            catch (Exception exception)
            {
                InternalRecordCallbackError(
                    definition.Owner,
                    "rare.handler:" + definition.Id,
                    exception
                );
                LogRareEventCallbackFailure(
                    definition.Owner,
                    definition.Id,
                    "handler",
                    exception
                );
                return;
            }

            InternalEmitCoreEvent(
                Events.RarePoliticalEventFired,
                kingdom,
                "",
                definition.Id,
                0,
                definition.ChancePermille,
                "",
                "",
                definition.Id,
                null,
                "",
                "",
                "",
                "",
                definition.Owner,
                "rare-political-event",
                currentYear
            );
        }

        private static RarePoliticalEventInfo ConvertRarePoliticalEvent(
            RegisteredRarePoliticalEvent item
        )
        {
            if (item == null) return null;
            return new RarePoliticalEventInfo()
            {
                Id = item.Id,
                NameKey = item.NameKey,
                DisplayName = ResolveRarePoliticalEventName(item),
                DescriptionKey = item.DescriptionKey,
                Description = ResolveRarePoliticalEventDescription(item),
                Source = item.Owner,
                CheckIntervalYears = item.CheckIntervalYears,
                CooldownYears = item.CooldownYears,
                ChancePermille = item.ChancePermille,
                CheckImmediately = item.CheckImmediately
            };
        }

        private static string ResolveRarePoliticalEventName(RegisteredRarePoliticalEvent item)
        {
            if (item == null) return "";
            string localized = ResolveRarePoliticalEventLocale(item.NameKey);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
            if (!string.IsNullOrWhiteSpace(item.DisplayName)) return item.DisplayName;
            if (!string.IsNullOrWhiteSpace(item.NameKey)) return item.NameKey;
            return item.Id ?? "";
        }

        private static string ResolveRarePoliticalEventDescription(RegisteredRarePoliticalEvent item)
        {
            if (item == null) return "";
            string localized = ResolveRarePoliticalEventLocale(item.DescriptionKey);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
            if (!string.IsNullOrWhiteSpace(item.Description)) return item.Description;
            if (!string.IsNullOrWhiteSpace(item.DescriptionKey)) return item.DescriptionKey;
            return "";
        }

        private static string ResolveRarePoliticalEventLocale(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            try
            {
                string localized = NeoModLoader.General.LM.Get(key.Trim());
                if (!string.IsNullOrWhiteSpace(localized) &&
                    !string.Equals(localized, key.Trim(), StringComparison.Ordinal))
                {
                    return localized;
                }
            }
            catch
            {
            }
            return "";
        }

        private static void LogRareEventCallbackFailure(
            string addonId,
            string eventId,
            string phase,
            Exception exception
        )
        {
            try
            {
                NeoModLoader.services.LogService.LogError(
                    "[Political World API] Addon '" + (addonId ?? "") +
                    "' rare event '" + (eventId ?? "") +
                    "' " + (phase ?? "callback") + " failed: " +
                    (exception == null ? "unknown exception" : exception.Message)
                );
            }
            catch
            {
            }
        }
    }
}
