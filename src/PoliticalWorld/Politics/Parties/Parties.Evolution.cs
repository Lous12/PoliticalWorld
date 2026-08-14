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
        private static void UpdatePartyTraitsAndEvolution(
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
            int stability = GetNationalStability(kingdom);

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                if (party == null || !party.Active)
                {
                    continue;
                }

                if (party.Traits == null)
                {
                    party.Traits =
                        LoadOrInitializePartyTraits(
                            kingdom,
                            party
                        );
                }

                int age =
                    worldYear > 0 &&
                    party.FoundedYear > 0
                        ? Math.Max(
                            0,
                            worldYear - party.FoundedYear
                        )
                        : 0;

                bool traitsChanged = false;

                if (
                    age >= 10 &&
                    HasPartyTrait(
                        party,
                        PartyTraitSplintered
                    )
                )
                {
                    if (RemovePartyTrait(
                        party,
                        PartyTraitSplintered
                    ))
                    {
                        traitsChanged = true;
                        RecordPartyHistoryEvent(
                            kingdom,
                            party,
                            PartyHistoryTraitLost,
                            PartyTraitSplintered,
                            "",
                            worldYear
                        );
                    }
                }

                List<PoliticalParty> same =
                    GetPartiesForIdeology(
                        parties,
                        party.Ideology
                    );

                if (
                    same.Count <= 1 &&
                    age >= 12 &&
                    HasPartyTrait(
                        party,
                        PartyTraitFactional
                    ) &&
                    stability >= 55
                )
                {
                    if (RemovePartyTrait(
                        party,
                        PartyTraitFactional
                    ))
                    {
                        traitsChanged = true;
                        RecordPartyHistoryEvent(
                            kingdom,
                            party,
                            PartyHistoryTraitLost,
                            PartyTraitFactional,
                            "",
                            worldYear
                        );
                    }
                }

                int lastYear = GetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitLastYearPrefix,
                        party.Slot
                    ),
                    party.FoundedYear
                );

                if (worldYear <= 0)
                {
                    if (traitsChanged)
                    {
                        SavePartyTraits(
                            kingdom,
                            party
                        );
                    }

                    continue;
                }

                if (lastYear <= 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2TraitLastYearPrefix,
                            party.Slot
                        ),
                        worldYear
                    );
                    SetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2TraitLastSupportPrefix,
                            party.Slot
                        ),
                        party.Support
                    );

                    if (traitsChanged)
                    {
                        SavePartyTraits(
                            kingdom,
                            party
                        );
                    }

                    continue;
                }

                if (
                    worldYear - lastYear <
                    PartyTraitEvolutionIntervalYears
                )
                {
                    if (traitsChanged)
                    {
                        SavePartyTraits(
                            kingdom,
                            party
                        );
                    }

                    continue;
                }

                int lastSupport = GetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitLastSupportPrefix,
                        party.Slot
                    ),
                    party.Support
                );
                int supportDelta =
                    party.Support - lastSupport;

                // Historical acquisition. Parties normally begin with two
                // traits, leaving one slot for something they earn later.
                if (
                    party.Traits.Count < MaxPartyTraits
                )
                {
                    string gainedTrait = "";

                    int corruptionChance =
                        12 +
                        Math.Max(
                            0,
                            party.Support - 25
                        ) / 2;
                    int corruptionRoll =
                        StablePartyHash(
                            party.Id +
                            "|" +
                            (worldYear / 4) +
                            "|corruption"
                        ) % 100;

                    if (
                        age >= 25 &&
                        party.Support >= 25 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitCorrupt
                        ) &&
                        corruptionRoll <
                            corruptionChance
                    )
                    {
                        gainedTrait = PartyTraitCorrupt;
                    }
                    else if (
                        same.Count >= 2 &&
                        age >= 8 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitFactional
                        )
                    )
                    {
                        gainedTrait = PartyTraitFactional;
                    }
                    else if (
                        party.Support >= 38 &&
                        age >= 8 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitMass
                        )
                    )
                    {
                        gainedTrait = PartyTraitMass;
                    }
                    else if (
                        supportDelta >= 8 &&
                        stability < 60 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitPopulist
                        )
                    )
                    {
                        gainedTrait = PartyTraitPopulist;
                    }
                    else if (
                        party.Radicalism >= 70 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitRevolutionary
                        )
                    )
                    {
                        gainedTrait =
                            PartyTraitRevolutionary;
                    }
                    else if (
                        GetKingdomCourse(kingdom) ==
                            MilitaristTraitId &&
                        !HasPartyTrait(
                            party,
                            PartyTraitMilitarized
                        ) &&
                        (
                            party.ForeignStance ==
                                "hawkish" ||
                            party.Radicalism >= 50
                        )
                    )
                    {
                        gainedTrait =
                            PartyTraitMilitarized;
                    }
                    else if (
                        party.Strategy ==
                            "parliamentary" &&
                        party.Radicalism <= 42 &&
                        stability >= 50 &&
                        age >= 10 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitReformist
                        )
                    )
                    {
                        gainedTrait =
                            PartyTraitReformist;
                    }
                    else if (
                        party.Support <= 16 &&
                        age >= 12 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitElite
                        )
                    )
                    {
                        gainedTrait = PartyTraitElite;
                    }
                    else if (
                        age >= 16 &&
                        party.Support >= 20 &&
                        same.Count == 1 &&
                        !HasPartyTrait(
                            party,
                            PartyTraitDisciplined
                        )
                    )
                    {
                        gainedTrait =
                            PartyTraitDisciplined;
                    }

                    if (!string.IsNullOrEmpty(gainedTrait))
                    {
                        if (TryAddPartyTrait(
                            party,
                            gainedTrait
                        ))
                        {
                            traitsChanged = true;
                            RecordPartyHistoryEvent(
                                kingdom,
                                party,
                                PartyHistoryTraitGained,
                                gainedTrait,
                                "",
                                worldYear
                            );
                        }
                    }
                }

                int newRadicalism = party.Radicalism;

                if (
                    HasPartyTrait(
                        party,
                        PartyTraitReformist
                    ) &&
                    newRadicalism > 15
                )
                {
                    newRadicalism--;
                }

                if (
                    HasPartyTrait(
                        party,
                        PartyTraitRevolutionary
                    ) &&
                    newRadicalism < 95
                )
                {
                    newRadicalism++;
                }

                if (
                    HasPartyTrait(
                        party,
                        PartyTraitMilitarized
                    ) &&
                    GetKingdomCourse(kingdom) ==
                        MilitaristTraitId &&
                    newRadicalism < 95
                )
                {
                    newRadicalism++;
                }

                if (newRadicalism != party.Radicalism)
                {
                    party.Radicalism = ClampInt(
                        newRadicalism,
                        0,
                        100
                    );
                    party.Position =
                        DeterminePartyPosition(
                            party.Radicalism
                        );
                    party.Strategy =
                        DeterminePartyStrategy(
                            party.Radicalism
                        );

                    SetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2RadicalismPrefix,
                            party.Slot
                        ),
                        party.Radicalism
                    );
                    SetKingdomStringData(
                        kingdom,
                        PartySlotKey(
                            PartyV2PositionPrefix,
                            party.Slot
                        ),
                        party.Position
                    );
                    SetKingdomStringData(
                        kingdom,
                        PartySlotKey(
                            PartyV2StrategyPrefix,
                            party.Slot
                        ),
                        party.Strategy
                    );
                }

                if (traitsChanged)
                {
                    SavePartyTraits(
                        kingdom,
                        party
                    );
                }

                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitLastYearPrefix,
                        party.Slot
                    ),
                    worldYear
                );
                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitLastSupportPrefix,
                        party.Slot
                    ),
                    party.Support
                );
            }
        }

    }
}
