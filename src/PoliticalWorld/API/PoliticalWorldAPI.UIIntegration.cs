using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// Public UI integration introduced in API 1.12.
    ///
    /// Addons register declarative inspector sections and context actions.
    /// Political World owns the host UI, so addons do not need to clone or
    /// patch internal windows. The same registrations can later be consumed by
    /// Scenario Tools or another compatible host through this public surface.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public enum InspectorTargetKind
        {
            Actor = 1,
            City = 2,
            Kingdom = 3
        }

        public sealed class InspectorContext
        {
            public InspectorTargetKind TargetKind;
            public Actor Actor;
            public City City;
            public Kingdom Kingdom;

            public object Target
            {
                get
                {
                    if (TargetKind == InspectorTargetKind.Actor) return Actor;
                    if (TargetKind == InspectorTargetKind.City) return City;
                    return Kingdom;
                }
            }
        }

        public sealed class InspectorFieldDefinition
        {
            public string Id;
            public string NameKey;
            public string DisplayName;
            public int SortOrder;
            public Func<InspectorContext, string> ValueProvider;
            public Func<InspectorContext, bool> Visible;
        }

        public sealed class InspectorSectionDefinition
        {
            public string Id;
            public InspectorTargetKind TargetKind;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public int SortOrder;
            public Func<InspectorContext, bool> Visible;
            public IList<InspectorFieldDefinition> Fields;
        }

        public sealed class InspectorFieldInfo
        {
            public string Id;
            public string DisplayName;
            public string Value;
            public int SortOrder;
        }

        public sealed class InspectorSectionInfo
        {
            public string Id;
            public string Source;
            public InspectorTargetKind TargetKind;
            public string DisplayName;
            public string Description;
            public int SortOrder;
            public List<InspectorFieldInfo> Fields = new List<InspectorFieldInfo>();
        }

        public sealed class ContextActionDefinition
        {
            public string Id;
            public InspectorTargetKind TargetKind;
            public string NameKey;
            public string DisplayName;
            public string DescriptionKey;
            public string Description;
            public int SortOrder;
            public Func<InspectorContext, bool> Visible;
            public Func<InspectorContext, bool> CanExecute;
            public Func<InspectorContext, bool> Execute;
        }

        public sealed class ContextActionInfo
        {
            public string Id;
            public string Source;
            public InspectorTargetKind TargetKind;
            public string DisplayName;
            public string Description;
            public int SortOrder;
            public bool Enabled;
        }

        private sealed class InspectorSectionRegistration
        {
            public string Source;
            public InspectorSectionDefinition Definition;
        }

        private sealed class ContextActionRegistration
        {
            public string Source;
            public ContextActionDefinition Definition;
        }

        private static readonly Dictionary<string, InspectorSectionRegistration>
            InspectorSections = new Dictionary<string, InspectorSectionRegistration>(StringComparer.Ordinal);

        private static readonly Dictionary<string, ContextActionRegistration>
            ContextActions = new Dictionary<string, ContextActionRegistration>(StringComparer.Ordinal);

        public static partial class UI
        {
            public static bool RegisterInspectorSection(string addonId, InspectorSectionDefinition definition)
            {
                string owner = Trim(addonId);
                if (!IsAddonRegistered(owner) || definition == null) return false;

                string id = Trim(definition.Id);
                if (string.IsNullOrEmpty(id) || !IsOwnedContentId(owner, id)) return false;
                if (!IsSupportedInspectorTarget(definition.TargetKind)) return false;
                if (InspectorSections.ContainsKey(id))
                {
                    InternalRecordFrameworkIssue(owner, "WARN", "PWEC230", "Inspector section id is already registered: " + id);
                    return false;
                }

                InspectorSectionDefinition copy = CopyInspectorSectionDefinition(definition);
                copy.Id = id;
                InspectorSections[id] = new InspectorSectionRegistration
                {
                    Source = owner,
                    Definition = copy
                };
                return true;
            }

            public static bool UnregisterInspectorSection(string addonId, string sectionId)
            {
                string owner = Trim(addonId);
                string id = Trim(sectionId);
                InspectorSectionRegistration registration;
                if (!InspectorSections.TryGetValue(id, out registration) || registration == null) return false;
                if (!string.Equals(registration.Source, owner, StringComparison.Ordinal)) return false;
                return InspectorSections.Remove(id);
            }

            public static bool RegisterContextAction(string addonId, ContextActionDefinition definition)
            {
                string owner = Trim(addonId);
                if (!IsAddonRegistered(owner) || definition == null) return false;

                string id = Trim(definition.Id);
                if (string.IsNullOrEmpty(id) || !IsOwnedContentId(owner, id)) return false;
                if (!IsSupportedInspectorTarget(definition.TargetKind) || definition.Execute == null) return false;
                if (ContextActions.ContainsKey(id))
                {
                    InternalRecordFrameworkIssue(owner, "WARN", "PWEC231", "Context action id is already registered: " + id);
                    return false;
                }

                ContextActionDefinition copy = CopyContextActionDefinition(definition);
                copy.Id = id;
                ContextActions[id] = new ContextActionRegistration
                {
                    Source = owner,
                    Definition = copy
                };
                return true;
            }

            public static bool UnregisterContextAction(string addonId, string actionId)
            {
                string owner = Trim(addonId);
                string id = Trim(actionId);
                ContextActionRegistration registration;
                if (!ContextActions.TryGetValue(id, out registration) || registration == null) return false;
                if (!string.Equals(registration.Source, owner, StringComparison.Ordinal)) return false;
                return ContextActions.Remove(id);
            }

            public static int UnregisterAddonUi(string addonId)
            {
                string owner = Trim(addonId);
                if (string.IsNullOrEmpty(owner)) return 0;

                int removed = 0;
                List<string> ids = new List<string>();
                foreach (KeyValuePair<string, InspectorSectionRegistration> pair in InspectorSections)
                {
                    if (pair.Value != null && string.Equals(pair.Value.Source, owner, StringComparison.Ordinal)) ids.Add(pair.Key);
                }
                for (int i = 0; i < ids.Count; i++) if (InspectorSections.Remove(ids[i])) removed++;

                ids.Clear();
                foreach (KeyValuePair<string, ContextActionRegistration> pair in ContextActions)
                {
                    if (pair.Value != null && string.Equals(pair.Value.Source, owner, StringComparison.Ordinal)) ids.Add(pair.Key);
                }
                for (int i = 0; i < ids.Count; i++) if (ContextActions.Remove(ids[i])) removed++;

                removed += UnregisterAddonPoliticsPagesInternal(owner);
                return removed;
            }

            public static string[] GetInspectorSectionIds(string addonId = null)
            {
                string owner = Trim(addonId);
                List<string> result = new List<string>();
                foreach (KeyValuePair<string, InspectorSectionRegistration> pair in InspectorSections)
                {
                    if (string.IsNullOrEmpty(owner) || (pair.Value != null && string.Equals(pair.Value.Source, owner, StringComparison.Ordinal)))
                    {
                        result.Add(pair.Key);
                    }
                }
                result.Sort(StringComparer.Ordinal);
                return result.ToArray();
            }

            public static string[] GetContextActionIds(string addonId = null)
            {
                string owner = Trim(addonId);
                List<string> result = new List<string>();
                foreach (KeyValuePair<string, ContextActionRegistration> pair in ContextActions)
                {
                    if (string.IsNullOrEmpty(owner) || (pair.Value != null && string.Equals(pair.Value.Source, owner, StringComparison.Ordinal)))
                    {
                        result.Add(pair.Key);
                    }
                }
                result.Sort(StringComparer.Ordinal);
                return result.ToArray();
            }

            public static List<InspectorSectionInfo> GetInspectorSections(Actor actor)
            {
                return BuildInspectorSections(MakeInspectorContext(actor));
            }

            public static List<InspectorSectionInfo> GetInspectorSections(City city)
            {
                return BuildInspectorSections(MakeInspectorContext(city));
            }

            public static List<InspectorSectionInfo> GetInspectorSections(Kingdom kingdom)
            {
                return BuildInspectorSections(MakeInspectorContext(kingdom));
            }

            public static List<ContextActionInfo> GetContextActions(Actor actor)
            {
                return BuildContextActions(MakeInspectorContext(actor));
            }

            public static List<ContextActionInfo> GetContextActions(City city)
            {
                return BuildContextActions(MakeInspectorContext(city));
            }

            public static List<ContextActionInfo> GetContextActions(Kingdom kingdom)
            {
                return BuildContextActions(MakeInspectorContext(kingdom));
            }

            public static OperationResult ExecuteContextAction(string actionId, Actor actor)
            {
                return ExecuteContextActionInternal(Trim(actionId), MakeInspectorContext(actor));
            }

            public static OperationResult ExecuteContextAction(string actionId, City city)
            {
                return ExecuteContextActionInternal(Trim(actionId), MakeInspectorContext(city));
            }

            public static OperationResult ExecuteContextAction(string actionId, Kingdom kingdom)
            {
                return ExecuteContextActionInternal(Trim(actionId), MakeInspectorContext(kingdom));
            }
        }

        private static InspectorSectionDefinition CopyInspectorSectionDefinition(InspectorSectionDefinition definition)
        {
            InspectorSectionDefinition copy = new InspectorSectionDefinition
            {
                Id = Trim(definition.Id),
                TargetKind = definition.TargetKind,
                NameKey = Trim(definition.NameKey),
                DisplayName = Trim(definition.DisplayName),
                DescriptionKey = Trim(definition.DescriptionKey),
                Description = Trim(definition.Description),
                SortOrder = definition.SortOrder,
                Visible = definition.Visible,
                Fields = new List<InspectorFieldDefinition>()
            };

            if (definition.Fields != null)
            {
                for (int i = 0; i < definition.Fields.Count; i++)
                {
                    InspectorFieldDefinition field = definition.Fields[i];
                    if (field == null) continue;
                    ((List<InspectorFieldDefinition>)copy.Fields).Add(new InspectorFieldDefinition
                    {
                        Id = Trim(field.Id),
                        NameKey = Trim(field.NameKey),
                        DisplayName = Trim(field.DisplayName),
                        SortOrder = field.SortOrder,
                        ValueProvider = field.ValueProvider,
                        Visible = field.Visible
                    });
                }
            }
            return copy;
        }

        private static ContextActionDefinition CopyContextActionDefinition(ContextActionDefinition definition)
        {
            return new ContextActionDefinition
            {
                Id = Trim(definition.Id),
                TargetKind = definition.TargetKind,
                NameKey = Trim(definition.NameKey),
                DisplayName = Trim(definition.DisplayName),
                DescriptionKey = Trim(definition.DescriptionKey),
                Description = Trim(definition.Description),
                SortOrder = definition.SortOrder,
                Visible = definition.Visible,
                CanExecute = definition.CanExecute,
                Execute = definition.Execute
            };
        }

        private static bool IsSupportedInspectorTarget(InspectorTargetKind kind)
        {
            return kind == InspectorTargetKind.Actor || kind == InspectorTargetKind.City || kind == InspectorTargetKind.Kingdom;
        }

        private static InspectorContext MakeInspectorContext(Actor actor)
        {
            if (actor == null) return null;
            return new InspectorContext { TargetKind = InspectorTargetKind.Actor, Actor = actor };
        }

        private static InspectorContext MakeInspectorContext(City city)
        {
            if (city == null) return null;
            return new InspectorContext { TargetKind = InspectorTargetKind.City, City = city, Kingdom = GetKingdomFromCitySafe(city) };
        }

        private static InspectorContext MakeInspectorContext(Kingdom kingdom)
        {
            if (kingdom == null) return null;
            return new InspectorContext { TargetKind = InspectorTargetKind.Kingdom, Kingdom = kingdom };
        }

        private static Kingdom GetKingdomFromCitySafe(City city)
        {
            if (city == null) return null;
            try { return city.kingdom; }
            catch { return null; }
        }

        private static bool EvaluateInspectorPredicate(Func<InspectorContext, bool> predicate, InspectorContext context, string owner, string code)
        {
            if (predicate == null) return true;
            try { return predicate(context); }
            catch (Exception exception)
            {
                InternalRecordDiagnostic(owner, "WARN", code, exception.Message);
                return false;
            }
        }

        private static List<InspectorSectionInfo> BuildInspectorSections(InspectorContext context)
        {
            List<InspectorSectionInfo> result = new List<InspectorSectionInfo>();
            if (context == null || context.Target == null) return result;

            foreach (KeyValuePair<string, InspectorSectionRegistration> pair in InspectorSections)
            {
                InspectorSectionRegistration registration = pair.Value;
                InspectorSectionDefinition definition = registration == null ? null : registration.Definition;
                if (definition == null || definition.TargetKind != context.TargetKind) continue;
                if (!EvaluateInspectorPredicate(definition.Visible, context, registration.Source, "PWDIAG120")) continue;

                InspectorSectionInfo info = new InspectorSectionInfo
                {
                    Id = definition.Id,
                    Source = registration.Source,
                    TargetKind = definition.TargetKind,
                    DisplayName = ResolveLocalization(definition.NameKey, definition.DisplayName),
                    Description = ResolveLocalization(definition.DescriptionKey, definition.Description),
                    SortOrder = definition.SortOrder
                };

                if (definition.Fields != null)
                {
                    for (int i = 0; i < definition.Fields.Count; i++)
                    {
                        InspectorFieldDefinition field = definition.Fields[i];
                        if (field == null || field.ValueProvider == null) continue;
                        if (!EvaluateInspectorPredicate(field.Visible, context, registration.Source, "PWDIAG121")) continue;

                        string value = "";
                        try { value = field.ValueProvider(context) ?? ""; }
                        catch (Exception exception)
                        {
                            value = "—";
                            InternalRecordDiagnostic(registration.Source, "WARN", "PWDIAG122", exception.Message);
                        }

                        info.Fields.Add(new InspectorFieldInfo
                        {
                            Id = string.IsNullOrEmpty(field.Id) ? definition.Id + ".field." + i : field.Id,
                            DisplayName = ResolveLocalization(field.NameKey, field.DisplayName),
                            Value = value,
                            SortOrder = field.SortOrder
                        });
                    }
                    info.Fields.Sort(delegate(InspectorFieldInfo a, InspectorFieldInfo b)
                    {
                        int order = (a == null ? 0 : a.SortOrder).CompareTo(b == null ? 0 : b.SortOrder);
                        if (order != 0) return order;
                        return string.Compare(a == null ? "" : a.Id, b == null ? "" : b.Id, StringComparison.Ordinal);
                    });
                }
                result.Add(info);
            }

            result.Sort(delegate(InspectorSectionInfo a, InspectorSectionInfo b)
            {
                int order = (a == null ? 0 : a.SortOrder).CompareTo(b == null ? 0 : b.SortOrder);
                if (order != 0) return order;
                return string.Compare(a == null ? "" : a.Id, b == null ? "" : b.Id, StringComparison.Ordinal);
            });
            return result;
        }

        private static List<ContextActionInfo> BuildContextActions(InspectorContext context)
        {
            List<ContextActionInfo> result = new List<ContextActionInfo>();
            if (context == null || context.Target == null) return result;

            foreach (KeyValuePair<string, ContextActionRegistration> pair in ContextActions)
            {
                ContextActionRegistration registration = pair.Value;
                ContextActionDefinition definition = registration == null ? null : registration.Definition;
                if (definition == null || definition.TargetKind != context.TargetKind) continue;
                if (!EvaluateInspectorPredicate(definition.Visible, context, registration.Source, "PWDIAG123")) continue;

                bool enabled = EvaluateInspectorPredicate(definition.CanExecute, context, registration.Source, "PWDIAG124");
                result.Add(new ContextActionInfo
                {
                    Id = definition.Id,
                    Source = registration.Source,
                    TargetKind = definition.TargetKind,
                    DisplayName = ResolveLocalization(definition.NameKey, definition.DisplayName),
                    Description = ResolveLocalization(definition.DescriptionKey, definition.Description),
                    SortOrder = definition.SortOrder,
                    Enabled = enabled
                });
            }

            result.Sort(delegate(ContextActionInfo a, ContextActionInfo b)
            {
                int order = (a == null ? 0 : a.SortOrder).CompareTo(b == null ? 0 : b.SortOrder);
                if (order != 0) return order;
                return string.Compare(a == null ? "" : a.Id, b == null ? "" : b.Id, StringComparison.Ordinal);
            });
            return result;
        }

        private static OperationResult ExecuteContextActionInternal(string actionId, InspectorContext context)
        {
            if (string.IsNullOrEmpty(actionId) || context == null || context.Target == null)
            {
                return NewOperationResult(false, "PWUI400", "Invalid context action request.");
            }

            ContextActionRegistration registration;
            if (!ContextActions.TryGetValue(actionId, out registration) || registration == null || registration.Definition == null)
            {
                return NewOperationResult(false, "PWUI404", "Context action is not registered: " + actionId);
            }

            ContextActionDefinition definition = registration.Definition;
            if (definition.TargetKind != context.TargetKind)
            {
                return NewOperationResult(false, "PWUI409", "Context action target type does not match.");
            }

            if (!EvaluateInspectorPredicate(definition.Visible, context, registration.Source, "PWDIAG125") ||
                !EvaluateInspectorPredicate(definition.CanExecute, context, registration.Source, "PWDIAG126"))
            {
                return NewOperationResult(false, "PWUI403", "Context action is not available for this target.");
            }

            try
            {
                bool success = definition.Execute(context);
                return NewOperationResult(success, success ? "PWUI200" : "PWUI422", success ? "Context action executed." : "Context action returned false.");
            }
            catch (Exception exception)
            {
                InternalRecordDiagnostic(registration.Source, "ERROR", "PWDIAG127", exception.Message);
                return NewOperationResult(false, "PWUI500", exception.Message);
            }
        }

        private static OperationResult NewOperationResult(bool success, string code, string message)
        {
            return new OperationResult { Success = success, Code = code ?? "", Message = message ?? "" };
        }
    }
}
