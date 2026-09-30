using System;
using System.Collections.Generic;
using NeoModLoader.General;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        // 1.11-dev4: internal party wings.
        //
        // The old split mechanic needed an already-active ideological movement.
        // That was fine for opposition politics, but it meant a broad ruling
        // party could sit alone for centuries without ever producing a rival.
        // Internal faction pressure gives old/big parties a political life of
        // their own: first a factional wing appears, then under enough pressure
        // it can break away as a new party.
        private static void UpdatePartyInternalFactions(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (
                kingdom == null ||
                parties == null ||
                parties.Count == 0
            )
            {
                return;
            }

            int worldYear = GetWorldYearSafe();
            if (worldYear <= 0)
            {
                return;
            }

            int stability = GetNationalStability(kingdom);
            int activeCount = 0;
            for (int i = 0; i < parties.Count; i++)
            {
                if (parties[i] != null && parties[i].Active)
                {
                    activeCount++;
                }
            }

            string rulingPartyId = GetKingdomStringData(
                kingdom,
                ElectionRulingPartyIdDataKey,
                ""
            );

            // Do not process parties created during this pass until the next
            // political tick.
            int initialCount = parties.Count;

            for (int i = 0; i < initialCount; i++)
            {
                PoliticalParty party = parties[i];

                if (
                    party == null ||
                    !party.Active ||
                    party.Slot < 0 ||
                    string.IsNullOrEmpty(party.Id) ||
                    !IsValidIdeology(party.Ideology)
                )
                {
                    continue;
                }

                int age =
                    party.FoundedYear > 0
                        ? Math.Max(0, worldYear - party.FoundedYear)
                        : 0;

                if (age < 8)
                {
                    continue;
                }

                string lastYearKey = PartySlotKey(
                    PartyFactionLastYearPrefix,
                    party.Slot
                );
                int lastYear = GetKingdomIntData(
                    kingdom,
                    lastYearKey,
                    party.FoundedYear > 0
                        ? party.FoundedYear
                        : worldYear
                );

                if (lastYear <= 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        lastYearKey,
                        worldYear
                    );
                    continue;
                }

                if (
                    worldYear - lastYear <
                    PartyFactionTickYears
                )
                {
                    continue;
                }

                SetKingdomIntData(
                    kingdom,
                    lastYearKey,
                    worldYear
                );

                string pressureKey = PartySlotKey(
                    PartyFactionPressurePrefix,
                    party.Slot
                );
                int pressure = GetKingdomIntData(
                    kingdom,
                    pressureKey,
                    0
                );

                int delta = 3;

                // A one-party landscape naturally produces internal wings:
                // political disagreement has nowhere else to organize.
                if (activeCount <= 1)
                {
                    delta += 8;
                }
                else if (activeCount == 2)
                {
                    delta += 3;
                }

                if (party.Support >= 45)
                {
                    delta += 4;
                }
                else if (party.Support <= 12)
                {
                    delta -= 3;
                }

                if (
                    !string.IsNullOrEmpty(rulingPartyId) &&
                    party.Id == rulingPartyId &&
                    age >= 16
                )
                {
                    delta += 2;
                }

                if (stability < 45)
                {
                    delta += 5;
                }
                else if (stability >= 72)
                {
                    delta -= 3;
                }

                if (HasPartyTrait(party, PartyTraitFactional))
                {
                    delta += 8;
                }
                if (HasPartyTrait(party, PartyTraitRevolutionary))
                {
                    delta += 3;
                }
                if (HasPartyTrait(party, PartyTraitReformist))
                {
                    delta += 2;
                }
                if (HasPartyTrait(party, PartyTraitCorrupt))
                {
                    delta += 3;
                }
                if (HasPartyTrait(party, PartyTraitDisciplined))
                {
                    delta -= 7;
                }
                if (HasPartyTrait(party, PartyTraitSplintered))
                {
                    delta -= 5;
                }

                // Small deterministic historical noise. It is stable for a
                // given party/tick and therefore does not turn faction growth
                // into a per-frame random lottery.
                delta +=
                    Math.Abs(
                        StablePartyHash(
                            party.Id +
                            "|faction|" +
                            (worldYear / PartyFactionTickYears)
                        )
                    ) % 4;

                pressure = ClampInt(
                    pressure + delta,
                    0,
                    100
                );

                // "Factional" is now the visible sign that meaningful internal
                // wings exist. If all three trait slots are occupied, the
                // hidden pressure still works and can eventually cause a split.
                if (
                    pressure >= PartyFactionWingThreshold &&
                    !HasPartyTrait(
                        party,
                        PartyTraitFactional
                    )
                )
                {
                    if (
                        TryAddPartyTrait(
                            party,
                            PartyTraitFactional
                        )
                    )
                    {
                        SavePartyTraits(
                            kingdom,
                            party
                        );
                        RecordPartyHistoryEvent(
                            kingdom,
                            party,
                            PartyHistoryTraitGained,
                            PartyTraitFactional,
                            "internal_wing",
                            worldYear
                        );
                    }
                }

                if (
                    pressure >= PartyFactionSplitThreshold &&
                    age >= PartyFactionSplitMinAgeYears
                )
                {
                    string splitYearKey = PartySlotKey(
                        PartyFactionLastSplitYearPrefix,
                        party.Slot
                    );
                    int lastSplitYear = GetKingdomIntData(
                        kingdom,
                        splitYearKey,
                        0
                    );

                    if (
                        lastSplitYear <= 0 ||
                        worldYear - lastSplitYear >=
                            PartyFactionSplitCooldownYears
                    )
                    {
                        if (
                            TryCreateInternalPartySplinter(
                                kingdom,
                                party,
                                parties,
                                pressure,
                                stability,
                                worldYear
                            )
                        )
                        {
                            pressure = 20;
                            SetKingdomIntData(
                                kingdom,
                                splitYearKey,
                                worldYear
                            );
                            activeCount++;
                        }
                    }
                }

                SetKingdomIntData(
                    kingdom,
                    pressureKey,
                    pressure
                );
            }
        }

        private static bool TryCreateInternalPartySplinter(
            Kingdom kingdom,
            PoliticalParty parent,
            List<PoliticalParty> parties,
            int factionPressure,
            int stability,
            int worldYear
        )
        {
            if (
                kingdom == null ||
                parent == null ||
                parties == null ||
                parties.Count >= MaxPoliticalParties
            )
            {
                return false;
            }

            int radicalPressure = GetKingdomIntData(
                kingdom,
                IdeologyRadicalizationPressureDataKey,
                0
            );
            int reformPressure = GetKingdomIntData(
                kingdom,
                IdeologyReformPressureDataKey,
                0
            );

            bool severeCrisis =
                stability <= 32 ||
                radicalPressure >= 72 ||
                (
                    radicalPressure >= 60 &&
                    reformPressure >= 60
                );

            int modeRoll = Math.Abs(
                StablePartyHash(
                    parent.Id +
                    "|wing_mode|" +
                    (worldYear / PartyFactionTickYears)
                )
            ) % 100;

            // Most splinters move to a nearby ideological family. A complete
            // ideological break is possible, but only under severe political
            // pressure. Some wings remain inside the parent ideology family.
            int splitMode = 0; // 0 = same family, 1 = related, 2 = opposite
            if (severeCrisis && modeRoll < 16)
            {
                splitMode = 2;
            }
            else if (modeRoll < 78)
            {
                splitMode = 1;
            }

            string targetIdeology = parent.Ideology;

            if (splitMode != 0)
            {
                targetIdeology = ChoosePartySplinterIdeology(
                    kingdom,
                    parent.Ideology,
                    splitMode == 2,
                    parties,
                    worldYear
                );

                if (!IsValidIdeology(targetIdeology))
                {
                    targetIdeology = parent.Ideology;
                    splitMode = 0;
                }
            }

            List<PoliticalParty> targetParties =
                GetPartiesForIdeology(
                    parties,
                    targetIdeology
                );

            if (
                targetParties.Count >=
                    MaxPoliticalPartiesPerIdeology
            )
            {
                if (targetIdeology != parent.Ideology)
                {
                    targetIdeology = parent.Ideology;
                    splitMode = 0;
                    targetParties = GetPartiesForIdeology(
                        parties,
                        targetIdeology
                    );
                }

                if (
                    targetParties.Count >=
                        MaxPoliticalPartiesPerIdeology
                )
                {
                    return false;
                }
            }

            int newRadicalism;
            if (splitMode == 2)
            {
                newRadicalism = ClampInt(
                    100 - parent.Radicalism,
                    10,
                    90
                );
            }
            else
            {
                int direction =
                    Math.Abs(
                        StablePartyHash(
                            parent.Id +
                            "|wing_radicalism|" +
                            worldYear
                        )
                    ) % 2 == 0
                        ? -1
                        : 1;

                int shift = splitMode == 1
                    ? 12
                    : 20;

                newRadicalism = ClampInt(
                    parent.Radicalism +
                        direction * shift,
                    5,
                    95
                );
            }

            PoliticalParty created = CreatePoliticalParty(
                kingdom,
                targetIdeology,
                newRadicalism,
                true
            );

            if (created == null)
            {
                return false;
            }

            created.ParentPartyId = parent.Id;
            created.ParentPartyName = parent.Name;

            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyIdPrefix,
                    created.Slot
                ),
                parent.Id
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyNamePrefix,
                    created.Slot
                ),
                parent.Name
            );

            created.ColorSeed = ChooseRelatedPartyColorSeed(
                kingdom,
                created.Ideology,
                parent.ColorSeed,
                created.Slot
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2ColorSeedPrefix,
                    created.Slot
                ),
                created.ColorSeed
            );

            if (targetIdeology != parent.Ideology)
            {
                SeedPartySplinterIdeologySupport(
                    kingdom,
                    parent,
                    targetIdeology,
                    splitMode == 2,
                    factionPressure,
                    worldYear
                );
            }

            string historyDetail =
                targetIdeology == parent.Ideology
                    ? ""
                    : GetIdeologyName(targetIdeology);

            RecordPartyHistoryEvent(
                kingdom,
                created,
                PartyHistorySplitFrom,
                parent.Name,
                historyDetail,
                worldYear
            );
            RecordPartyHistoryEvent(
                kingdom,
                parent,
                PartyHistorySplitChild,
                created.Name,
                historyDetail,
                worldYear
            );

            parties.Add(created);

            if (targetIdeology == parent.Ideology)
            {
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_party_split"),
                        created.Name,
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyName(targetIdeology)
                    ),
                    kingdom,
                    null,
                    created.LeaderActor,
                    GetIdeologyIconPath(targetIdeology),
                    "party_internal_split_" + created.Id,
                    30f
                );
            }
            else
            {
                PublishPoliticalEvent(
                    string.Format(
                        LM.Get("ukiol_event_party_wing_split"),
                        parent.Name,
                        created.Name,
                        GetWorldObjectDisplayName(kingdom),
                        GetIdeologyName(targetIdeology)
                    ),
                    kingdom,
                    null,
                    created.LeaderActor,
                    GetIdeologyIconPath(targetIdeology),
                    "party_wing_split_" + created.Id,
                    splitMode == 2 ? 45f : 35f
                );
            }

            return true;
        }

        private static string ChoosePartySplinterIdeology(
            Kingdom kingdom,
            string parentIdeology,
            bool opposite,
            List<PoliticalParty> parties,
            int worldYear
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(parentIdeology)
            )
            {
                return parentIdeology;
            }

            IdeologyBehaviorProfile parentProfile =
                GetIdeologyBehaviorProfile(
                    parentIdeology
                );

            string bestId = "";
            int bestScore = int.MinValue;

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string candidate = IdeologyIds[i];

                if (
                    candidate == parentIdeology ||
                    !IsValidIdeology(candidate)
                )
                {
                    continue;
                }

                List<PoliticalParty> existing =
                    GetPartiesForIdeology(
                        parties,
                        candidate
                    );
                if (
                    existing.Count >=
                        MaxPoliticalPartiesPerIdeology
                )
                {
                    continue;
                }

                IdeologyBehaviorProfile candidateProfile =
                    GetIdeologyBehaviorProfile(
                        candidate
                    );

                int distance = GetIdeologyBehaviorDistance(
                    parentProfile,
                    candidateProfile
                );
                int support = GetKingdomIdeologySupport(
                    kingdom,
                    candidate
                );

                // Related wings prefer a nearby ideology which already has a
                // little social base. Opposite wings prefer maximum ideological
                // distance, but existing support still makes a successful
                // breakaway more plausible.
                int score = opposite
                    ? distance * 3 + support * 2
                    : 500 - distance * 3 + support * 3;

                float raceWeight =
                    GetRacePoliticalIdeologyWeight(
                        kingdom,
                        candidate
                    );
                score += (int)Math.Round(
                    (raceWeight - 1f) * 25f
                );

                score +=
                    Math.Abs(
                        StablePartyHash(
                            parentIdeology +
                            "|" +
                            candidate +
                            "|" +
                            worldYear
                        )
                    ) % 13;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = candidate;
                }
            }

            return string.IsNullOrEmpty(bestId)
                ? parentIdeology
                : bestId;
        }

        private static int GetIdeologyBehaviorDistance(
            IdeologyBehaviorProfile a,
            IdeologyBehaviorProfile b
        )
        {
            if (a == null || b == null)
            {
                return 500;
            }

            return
                Math.Abs(a.Market - b.Market) +
                Math.Abs(a.Welfare - b.Welfare) +
                Math.Abs(a.Centralization - b.Centralization) +
                Math.Abs(a.Pluralism - b.Pluralism) +
                Math.Abs(a.Militarism - b.Militarism);
        }

        private static void SeedPartySplinterIdeologySupport(
            Kingdom kingdom,
            PoliticalParty parent,
            string targetIdeology,
            bool opposite,
            int factionPressure,
            int worldYear
        )
        {
            if (
                kingdom == null ||
                parent == null ||
                !IsValidIdeology(parent.Ideology) ||
                !IsValidIdeology(targetIdeology) ||
                parent.Ideology == targetIdeology
            )
            {
                return;
            }

            int transferChance =
                (opposite ? 10 : 16) +
                Math.Max(
                    0,
                    factionPressure -
                    PartyFactionSplitThreshold
                ) / 5;
            transferChance = ClampInt(
                transferChance,
                opposite ? 8 : 12,
                opposite ? 16 : 24
            );

            int converted = 0;
            int maxConverted = 90;
            Actor fallback = null;

            List<City> cities = GetCitiesSafe(kingdom);

            for (
                int c = 0;
                c < cities.Count && converted < maxConverted;
                c++
            )
            {
                List<Actor> units =
                    GetCityUnitsSafe(
                        cities[c]
                    );

                for (
                    int i = 0;
                    i < units.Count && converted < maxConverted;
                    i++
                )
                {
                    Actor actor = units[i];

                    if (
                        actor == null ||
                        actor.data == null ||
                        !actor.isAlive() ||
                        GetCitizenIdeology(actor) !=
                            parent.Ideology
                    )
                    {
                        continue;
                    }

                    int conviction =
                        GetCitizenIdeologyConviction(actor);

                    if (
                        opposite
                            ? conviction > 58
                            : conviction > 78
                    )
                    {
                        continue;
                    }

                    if (fallback == null)
                    {
                        fallback = actor;
                    }

                    int roll =
                        Math.Abs(
                            StablePartyHash(
                                parent.Id +
                                "|wing_supporter|" +
                                GetStableObjectIdentity(actor) +
                                "|" +
                                worldYear
                            )
                        ) % 100;

                    if (roll >= transferChance)
                    {
                        continue;
                    }

                    SetCitizenIdeology(
                        actor,
                        targetIdeology
                    );
                    SetCitizenIdeologyConviction(
                        actor,
                        opposite ? 34 : 44
                    );
                    converted++;
                }
            }

            // A split with zero human supporters is just paperwork. Guarantee
            // at least one low-conviction founder when such an actor exists.
            if (converted == 0 && fallback != null)
            {
                SetCitizenIdeology(
                    fallback,
                    targetIdeology
                );
                SetCitizenIdeologyConviction(
                    fallback,
                    opposite ? 32 : 42
                );
            }
        }

        private static bool IsFreshPoliticalPartySplinter(
            PoliticalParty party,
            int worldYear
        )
        {
            if (
                party == null ||
                string.IsNullOrEmpty(party.ParentPartyId) ||
                party.FoundedYear <= 0 ||
                worldYear <= 0
            )
            {
                return false;
            }

            return
                worldYear - party.FoundedYear <
                PartyFreshSplinterGraceYears;
        }
    }
}
