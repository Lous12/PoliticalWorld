using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private static void UpdateGovernmentForms()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                string desired = DetermineGovernmentForm(kingdom);
                if (string.IsNullOrEmpty(desired))
                {
                    continue;
                }

                string current = GetKingdomStringData(
                    kingdom,
                    GovernmentFormDataKey,
                    ""
                );

                // Migration/new kingdoms: establish a regime immediately and
                // quietly. We do not fabricate a historical government event.
                if (!IsValidGovernmentForm(current))
                {
                    SetGovernmentForm(kingdom, desired, false);
                    ResetGovernmentCandidate(kingdom);
                    continue;
                }

                if (current == desired)
                {
                    ResetGovernmentCandidate(kingdom);
                    continue;
                }

                string candidate = GetKingdomStringData(
                    kingdom,
                    GovernmentCandidateDataKey,
                    ""
                );
                int pressure = GetKingdomIntData(
                    kingdom,
                    GovernmentPressureDataKey,
                    0
                );

                if (candidate == desired)
                {
                    pressure++;
                }
                else
                {
                    candidate = desired;
                    pressure = 1;
                }

                if (pressure >= GovernmentEvolutionThreshold)
                {
                    SetGovernmentForm(kingdom, desired, true);
                    ResetGovernmentCandidate(kingdom);
                    continue;
                }

                SetKingdomStringData(
                    kingdom,
                    GovernmentCandidateDataKey,
                    candidate
                );
                SetKingdomIntData(
                    kingdom,
                    GovernmentPressureDataKey,
                    pressure
                );
            }
        }

        private static string DetermineGovernmentForm(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return GovernmentOligarchyId;
            }

            string ideology = GetStateIdeology(kingdom);
            string current = GetStateIdeologyCurrent(kingdom);
            string course = GetKingdomCourse(kingdom);
            int stability = GetNationalStability(kingdom);
            int crisis = GetPoliticalCrisisPressure(kingdom);
            Actor ruler = GetLivingRuler(kingdom);
            string rulerTrait = GetPoliticalTrait(ruler);

            // Severe instability plus a militarist political center can turn
            // many ideological systems into military rule. This override is
            // deliberately narrow so normal militarist governments do not
            // instantly become dictatorships.
            if (
                (course == MilitaristTraitId || rulerTrait == MilitaristTraitId) &&
                (stability < 38 || crisis >= 60)
            )
            {
                return GovernmentMilitaryDictatorshipId;
            }

            if (ideology == MonarchismIdeologyId)
            {
                if (IsIdeologyNodeDescendantOf(current, ConstitutionalMonarchyCurrentId))
                {
                    return GovernmentConstitutionalMonarchyId;
                }
                return GovernmentAbsoluteMonarchyId;
            }

            if (ideology == DemocracyIdeologyId)
            {
                if (IsIdeologyNodeDescendantOf(current, CouncilDemocracyCurrentId))
                {
                    return GovernmentCouncilRepublicId;
                }
                if (IsIdeologyNodeDescendantOf(current, ParliamentaryDemocracyCurrentId))
                {
                    return GovernmentParliamentaryRepublicId;
                }
                return GovernmentPresidentialRepublicId;
            }

            if (ideology == LiberalismIdeologyId)
            {
                int democracy = GetKingdomIdeologySupport(
                    kingdom,
                    DemocracyIdeologyId
                );
                if (IsIdeologyNodeDescendantOf(current, SocialLiberalismCurrentId) || democracy >= 24)
                {
                    return GovernmentParliamentaryRepublicId;
                }
                return GovernmentOligarchyId;
            }

            if (ideology == ConservatismIdeologyId)
            {
                int monarchy = GetKingdomIdeologySupport(
                    kingdom,
                    MonarchismIdeologyId
                );
                int democracy = GetKingdomIdeologySupport(
                    kingdom,
                    DemocracyIdeologyId
                );

                if (monarchy >= 35)
                {
                    return IsIdeologyNodeDescendantOf(current, LiberalConservatismCurrentId)
                        ? GovernmentConstitutionalMonarchyId
                        : GovernmentAbsoluteMonarchyId;
                }
                if (IsIdeologyNodeDescendantOf(current, LiberalConservatismCurrentId) || democracy >= 26)
                {
                    return GovernmentPresidentialRepublicId;
                }
                return GovernmentOligarchyId;
            }

            if (ideology == SocialismIdeologyId)
            {
                if (
                    IsIdeologyNodeDescendantOf(current, SocialDemocracyCurrentId) ||
                    IsIdeologyNodeDescendantOf(current, DemocraticSocialismCurrentId)
                )
                {
                    return GovernmentParliamentaryRepublicId;
                }

                int councilInfluence =
                    GetKingdomIdeologySupport(kingdom, SyndicalismIdeologyId) +
                    GetKingdomIdeologySupport(kingdom, AnarchismIdeologyId);
                return councilInfluence >= 30
                    ? GovernmentCouncilRepublicId
                    : GovernmentOnePartyStateId;
            }

            if (ideology == CommunismIdeologyId)
            {
                // Communist states use a council-republic state structure.
                // The political-system layer below decides whether those
                // councils are pluralistic or dominated by one ruling party.
                return GovernmentCouncilRepublicId;
            }

            if (ideology == FascismIdeologyId)
            {
                return GovernmentOnePartyStateId;
            }

            if (
                ideology == AnarchismIdeologyId ||
                ideology == SyndicalismIdeologyId
            )
            {
                return GovernmentCouncilRepublicId;
            }

            return GovernmentOligarchyId;
        }

        private static string GetGovernmentForm(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return GovernmentOligarchyId;
            }

            string form = GetKingdomStringData(
                kingdom,
                GovernmentFormDataKey,
                ""
            );
            if (IsValidGovernmentForm(form))
            {
                return form;
            }

            form = DetermineGovernmentForm(kingdom);
            if (!string.IsNullOrEmpty(form))
            {
                SetGovernmentForm(kingdom, form, false);
            }
            return form;
        }

        private static string GetGovernmentPublicId(Kingdom kingdom)
        {
            string baseForm = GetGovernmentForm(kingdom);
            if (kingdom == null || kingdom.data == null) return baseForm;
            string custom = GetKingdomStringData(kingdom, CustomGovernmentFormDataKey, "");
            if (!string.IsNullOrEmpty(custom) && PoliticalWorldAPI.InternalIsRegisteredGovernment(custom))
            {
                string registeredBase = PoliticalWorldAPI.InternalGetRegisteredGovernmentBaseId(custom);
                if (string.Equals(baseForm, registeredBase, StringComparison.Ordinal)) return custom;
            }
            return baseForm;
        }

        private static string GetGovernmentPublicName(Kingdom kingdom)
        {
            string id = GetGovernmentPublicId(kingdom);
            if (PoliticalWorldAPI.InternalIsRegisteredGovernment(id))
            {
                string customName = PoliticalWorldAPI.InternalGetGovernmentDisplayName(id);
                if (!string.IsNullOrEmpty(customName)) return customName;
            }
            return GetGovernmentFormName(GetGovernmentForm(kingdom));
        }

        private static void SetCustomGovernmentIdentity(Kingdom kingdom, string governmentId)
        {
            if (kingdom == null || kingdom.data == null) return;
            SetKingdomStringData(kingdom, CustomGovernmentFormDataKey, governmentId ?? "");
        }

        private static bool IsValidGovernmentForm(string form)
        {
            return
                form == GovernmentAbsoluteMonarchyId ||
                form == GovernmentConstitutionalMonarchyId ||
                form == GovernmentParliamentaryRepublicId ||
                form == GovernmentPresidentialRepublicId ||
                form == GovernmentOnePartyStateId ||
                form == GovernmentMilitaryDictatorshipId ||
                form == GovernmentCouncilRepublicId ||
                form == GovernmentOligarchyId;
        }

        private static void SetGovernmentForm(
            Kingdom kingdom,
            string form,
            bool publishEvent,
            bool suppressCoreEvent = false
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidGovernmentForm(form)
            )
            {
                return;
            }

            string previousPublic = GetGovernmentPublicId(kingdom);
            SetKingdomStringData(kingdom, GovernmentFormDataKey, form);
            SetCustomGovernmentIdentity(kingdom, "");
            SetKingdomIntData(
                kingdom,
                GovernmentSinceYearDataKey,
                GetWorldYearSafe()
            );

            string currentPublic = form;
            if (!suppressCoreEvent && !string.Equals(previousPublic, currentPublic, StringComparison.Ordinal))
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.GovernmentChanged,
                    kingdom,
                    previousPublic,
                    currentPublic
                );
            }

            if (publishEvent && !string.Equals(previousPublic, currentPublic, StringComparison.Ordinal))
            {
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_government_changed"),
                        GetWorldObjectDisplayName(kingdom),
                        GetGovernmentFormName(form)
                    ),
                    kingdom,
                    null,
                    GetLivingRuler(kingdom),
                    OverviewIconPath,
                    "government_changed_" + form,
                    35f
                );
            }
        }

        private static void ResetGovernmentCandidate(Kingdom kingdom)
        {
            SetKingdomStringData(
                kingdom,
                GovernmentCandidateDataKey,
                ""
            );
            SetKingdomIntData(
                kingdom,
                GovernmentPressureDataKey,
                0
            );
        }

        private static string GetGovernmentFormName(string form)
        {
            if (PoliticalWorldAPI.InternalIsRegisteredGovernment(form))
            {
                string customName = PoliticalWorldAPI.InternalGetGovernmentDisplayName(form);
                if (!string.IsNullOrEmpty(customName)) return customName;
            }
            if (!IsValidGovernmentForm(form))
            {
                return LM.Get("ukiol_government_unknown");
            }
            return LM.Get(form);
        }

        private static bool GovernmentUsesCompetitiveElections(string form)
        {
            return
                form == GovernmentConstitutionalMonarchyId ||
                form == GovernmentParliamentaryRepublicId ||
                form == GovernmentPresidentialRepublicId;
        }

        private static int GetGovernmentElectionTermYears(string form)
        {
            if (form == GovernmentPresidentialRepublicId)
            {
                return 5;
            }
            if (
                form == GovernmentConstitutionalMonarchyId ||
                form == GovernmentParliamentaryRepublicId
            )
            {
                return 4;
            }
            return 0;
        }

        private static string DeterminePoliticalSystem(
            Kingdom kingdom,
            string form
        )
        {
            if (form == GovernmentCouncilRepublicId)
            {
                string ideology = GetStateIdeology(kingdom);
                if (
                    ideology == AnarchismIdeologyId ||
                    ideology == SyndicalismIdeologyId
                )
                {
                    return PoliticalSystemDecentralizedId;
                }
                if (ideology == CommunismIdeologyId)
                {
                    string current = GetStateIdeologyCurrent(kingdom);
                    if (IsIdeologyNodeDescendantOf(current, LibertarianCommunismCurrentId))
                    {
                        return PoliticalSystemDecentralizedId;
                    }
                    if (!IsIdeologyNodeDescendantOf(current, CouncilCommunismCurrentId))
                    {
                        return PoliticalSystemSovietOnePartyId;
                    }
                }
                return PoliticalSystemSovietId;
            }

            if (form == GovernmentOnePartyStateId)
            {
                return PoliticalSystemOnePartyId;
            }

            if (GovernmentUsesCompetitiveElections(form))
            {
                return PoliticalSystemCompetitiveId;
            }

            return PoliticalSystemNonElectoralId;
        }

        private static string GetPoliticalSystem(Kingdom kingdom)
        {
            if (kingdom == null || kingdom.data == null)
            {
                return PoliticalSystemNonElectoralId;
            }

            string form = GetGovernmentForm(kingdom);
            string expected = DeterminePoliticalSystem(kingdom, form);
            string stored = GetKingdomStringData(
                kingdom,
                PoliticalSystemDataKey,
                ""
            );

            if (stored != expected)
            {
                SetKingdomStringData(
                    kingdom,
                    PoliticalSystemDataKey,
                    expected
                );
                return expected;
            }

            return stored;
        }

        private static string GetPoliticalSystemName(string system)
        {
            if (string.IsNullOrEmpty(system))
            {
                return LM.Get("ukiol_political_system_unknown");
            }
            return LM.Get(system);
        }

        private static Color GetPoliticalSystemColor(string system)
        {
            if (system == PoliticalSystemCompetitiveId)
            {
                return new Color(0.50f, 0.90f, 0.70f, 1f);
            }
            if (
                system == PoliticalSystemSovietId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                return new Color(0.91f, 0.63f, 0.45f, 1f);
            }
            if (system == PoliticalSystemOnePartyId)
            {
                return new Color(0.92f, 0.47f, 0.38f, 1f);
            }
            if (system == PoliticalSystemDecentralizedId)
            {
                return new Color(0.58f, 0.84f, 0.58f, 1f);
            }
            return new Color(0.76f, 0.73f, 0.62f, 1f);
        }

        private static void UpdatePoliticalSystems()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            int currentYear = GetWorldYearSafe();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                Kingdom kingdom = kingdoms[i];
                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                string form = GetGovernmentForm(kingdom);
                string system = DeterminePoliticalSystem(kingdom, form);
                string previous = GetKingdomStringData(
                    kingdom,
                    PoliticalSystemDataKey,
                    ""
                );

                if (previous != system)
                {
                    SetKingdomStringData(
                        kingdom,
                        PoliticalSystemDataKey,
                        system
                    );
                    ResetPoliticalSystemSchedule(
                        kingdom,
                        system,
                        currentYear
                    );
                }

                if (
                    system == PoliticalSystemSovietId ||
                    system == PoliticalSystemSovietOnePartyId
                )
                {
                    UpdateCouncilSystem(kingdom, currentYear);
                }
                else
                {
                    ClearCouncilState(kingdom);
                }

                if (UsesCommunistPartyCongress(kingdom, system))
                {
                    UpdatePartyCongressFoundation(
                        kingdom,
                        currentYear
                    );
                }
                else
                {
                    ClearPartyCongressFoundation(kingdom);
                }
            }
        }

        private static void ResetPoliticalSystemSchedule(
            Kingdom kingdom,
            string system,
            int currentYear
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            if (system == PoliticalSystemCompetitiveId)
            {
                SetKingdomIntData(
                    kingdom,
                    ElectionNextYearDataKey,
                    Math.Max(1, currentYear + 1)
                );
                ClearCouncilState(kingdom);
                return;
            }

            SetKingdomIntData(kingdom, ElectionNextYearDataKey, 0);

            if (
                system == PoliticalSystemOnePartyId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                EnsureNonElectiveRulingParty(
                    kingdom,
                    GetGovernmentForm(kingdom)
                );
            }
            else
            {
                ClearRulingParty(kingdom);
            }

            if (
                system == PoliticalSystemSovietId ||
                system == PoliticalSystemSovietOnePartyId
            )
            {
                SetKingdomIntData(
                    kingdom,
                    CouncilNextYearDataKey,
                    Math.Max(1, currentYear + 1)
                );
            }
            else
            {
                ClearCouncilState(kingdom);
            }
        }

        private static bool UsesCommunistPartyCongress(
            Kingdom kingdom,
            string system
        )
        {
            if (
                kingdom == null ||
                (system != PoliticalSystemOnePartyId &&
                    system != PoliticalSystemSovietOnePartyId)
            )
            {
                return false;
            }

            string ideology = GetStateIdeology(kingdom);
            return
                ideology == CommunismIdeologyId ||
                ideology == SocialismIdeologyId;
        }

    }
}
