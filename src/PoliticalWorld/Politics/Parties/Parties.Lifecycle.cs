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
        // PARTY PIPELINE FIX2: the old implementation processed every kingdom
        // in one rendered frame. On active saves that could create several
        // parties, refresh leaders, rewrite party histories and emit multiple
        // WorldLog events back-to-back. The Player logs repeatedly stopped in
        // the middle of that burst without a managed exception. Keep exactly
        // the same political logic, but spread the world pass across frames.
        private static List<Kingdom> PartyUpdateKingdomSnapshot =
            new List<Kingdom>();
        private static int PartyUpdateKingdomIndex;
        private static bool PartyUpdatePassActive;
        private static int PartyUpdatePassSerial;

        private static void ResetPoliticalPartyUpdatePass()
        {
            PartyUpdateKingdomSnapshot.Clear();
            PartyUpdateKingdomIndex = 0;
            PartyUpdatePassActive = false;
        }

        // Returns true only when the complete world party pass has finished.
        // RunPoliticalSimulationPipelineStep() keeps stage 3 active while this
        // returns false, so one kingdom is handled per rendered frame.
        private static bool UpdatePoliticalPartiesIncremental()
        {
            if (!PartyUpdatePassActive)
            {
                PartyUpdateKingdomSnapshot = GetKingdomsSafe();
                PartyUpdateKingdomIndex = 0;
                PartyUpdatePassActive = true;
                PartyUpdatePassSerial++;

                if (VerbosePoliticalDiagnostics)
                {
                    LogInfo(
                        "[PW-PARTY-GUARD] pass begin serial=" +
                        PartyUpdatePassSerial +
                        " kingdoms=" + PartyUpdateKingdomSnapshot.Count
                    );
                }
            }

            if (IsWorldTopologySettling())
            {
                if (VerbosePoliticalDiagnostics)
                {
                    LogInfo(
                        "[PW-PARTY-GUARD] pass cancelled by topology settle serial=" +
                        PartyUpdatePassSerial
                    );
                }
                ResetPoliticalPartyUpdatePass();
                return true;
            }

            if (PartyUpdateKingdomIndex >= PartyUpdateKingdomSnapshot.Count)
            {
                if (VerbosePoliticalDiagnostics)
                {
                    LogInfo(
                        "[PW-PARTY-GUARD] pass complete serial=" +
                        PartyUpdatePassSerial
                    );
                }
                ResetPoliticalPartyUpdatePass();
                return true;
            }

            int currentIndex = PartyUpdateKingdomIndex;
            Kingdom kingdom = PartyUpdateKingdomSnapshot[currentIndex];
            PartyUpdateKingdomIndex++;

            string kingdomName = "<null>";
            if (kingdom != null)
            {
                try
                {
                    kingdomName = GetWorldObjectDisplayName(kingdom);
                }
                catch
                {
                    kingdomName = "<unreadable>";
                }
            }

            if (VerbosePoliticalDiagnostics)
            {
                LogInfo(
                    "[PW-PARTY-GUARD] kingdom begin serial=" +
                    PartyUpdatePassSerial +
                    " index=" + currentIndex +
                    " name=" + kingdomName
                );
            }

            try
            {
                UpdatePoliticalPartiesForKingdom(kingdom);
            }
            catch (Exception exception)
            {
                // A managed error in one kingdom must never abort the whole
                // Political World simulation. Keep the full exception text in
                // Player.log so the exact operation is visible on the next run.
                LogWarning(
                    "[PW-PARTY-GUARD] kingdom failed serial=" +
                    PartyUpdatePassSerial +
                    " index=" + currentIndex +
                    " name=" + kingdomName +
                    " exception=" + exception
                );
            }

            if (VerbosePoliticalDiagnostics)
            {
                LogInfo(
                    "[PW-PARTY-GUARD] kingdom end serial=" +
                    PartyUpdatePassSerial +
                    " index=" + currentIndex +
                    " name=" + kingdomName
                );
            }

            if (PartyUpdateKingdomIndex >= PartyUpdateKingdomSnapshot.Count)
            {
                if (VerbosePoliticalDiagnostics)
                {
                    LogInfo(
                        "[PW-PARTY-GUARD] pass complete serial=" +
                        PartyUpdatePassSerial
                    );
                }
                ResetPoliticalPartyUpdatePass();
                return true;
            }

            return false;
        }

        // Retained as a synchronous helper for internal callers/tests. Runtime
        // simulation uses UpdatePoliticalPartiesIncremental().
        private static void UpdatePoliticalParties()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();
            for (int k = 0; k < kingdoms.Count; k++)
            {
                UpdatePoliticalPartiesForKingdom(kingdoms[k]);
            }
        }

        private static void UpdatePoliticalPartiesForKingdom(Kingdom kingdom)
        {
            if (
                kingdom == null ||
                kingdom.data == null
            )
            {
                return;
            }

            EnsurePoliticalPartySchema(kingdom);

            List<PoliticalParty> parties =
                LoadPoliticalPartiesInternal(kingdom, false);

            // PARTY AVAILABILITY FIX: competitive and explicit one-party
            // systems must not wait for an opposition movement before the
            // first party can exist. The state ideology is intentionally not
            // represented by a political movement, so the old movement-only
            // creation rule could leave republics and one-party states with
            // zero parties indefinitely. Seed one state-ideology party when
            // the political system structurally requires parties.
            EnsureInstitutionalStateIdeologyParty(
                kingdom,
                parties
            );

            AggregatePartySupportFromCities(
                kingdom,
                parties
            );
            UpdatePartySupportHistorySnapshots(
                kingdom,
                parties
            );

            // Do not found several brand-new parties inside the same kingdom
            // in one frame. Deferred ideologies remain eligible on the next
            // normal political pass, preserving eventual behaviour.
            int formationsThisKingdom = 0;

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string ideology = IdeologyIds[i];
                string suffix = GetMovementKeySuffix(ideology);
                int support = GetKingdomIdeologySupport(
                    kingdom,
                    ideology
                );
                int movementActive = GetKingdomIntData(
                    kingdom,
                    MovementActivePrefix + suffix,
                    0
                );
                int movementRadicalism = GetKingdomIntData(
                    kingdom,
                    MovementRadicalismPrefix + suffix,
                    20
                );

                List<PoliticalParty> ideologyParties =
                    GetPartiesForIdeology(parties, ideology);

                if (ideologyParties.Count == 0)
                {
                    if (
                        formationsThisKingdom == 0 &&
                        movementActive != 0 &&
                        support >= PartyFormationThreshold
                    )
                    {
                        PoliticalParty created = CreatePoliticalParty(
                            kingdom,
                            ideology,
                            movementRadicalism,
                            false
                        );

                        if (created != null)
                        {
                            formationsThisKingdom++;
                            parties.Add(created);

                            PublishPoliticalEvent(
                                string.Format(
                                    LM.Get("ukiol_event_party_formed"),
                                    created.Name,
                                    GetWorldObjectDisplayName(kingdom),
                                    GetIdeologyName(ideology)
                                ),
                                kingdom,
                                null,
                                created.LeaderActor,
                                GetIdeologyIconPath(ideology),
                                "party_formed_" + created.Id,
                                20f
                            );
                        }
                    }

                    continue;
                }

                bool preserveInstitutionalStateParty =
                    ideology == GetStateIdeology(kingdom) &&
                    PoliticalSystemRequiresPartyInstitution(
                        GetPoliticalSystem(kingdom)
                    );

                if (
                    !preserveInstitutionalStateParty &&
                    movementActive == 0 &&
                    support <= PartyDissolutionThreshold
                )
                {
                    int dissolutionYear = GetWorldYearSafe();
                    bool keptFreshSplinter = false;

                    for (int p = 0; p < ideologyParties.Count; p++)
                    {
                        PoliticalParty candidate = ideologyParties[p];

                        if (
                            IsFreshPoliticalPartySplinter(
                                candidate,
                                dissolutionYear
                            )
                        )
                        {
                            keptFreshSplinter = true;
                            continue;
                        }

                        DeactivatePoliticalParty(
                            kingdom,
                            candidate
                        );
                    }

                    if (!keptFreshSplinter)
                    {
                        continue;
                    }
                }

                RefreshPartyLeaders(
                    kingdom,
                    ideologyParties
                );

                TrySplitPoliticalParty(
                    kingdom,
                    ideology,
                    support,
                    movementActive,
                    movementRadicalism,
                    ideologyParties,
                    parties
                );
            }

            parties = LoadPoliticalPartiesInternal(
                kingdom,
                false
            );
            UpdateRegionalPartySupport(
                kingdom,
                parties
            );
            UpdatePartyTraitsAndEvolution(
                kingdom,
                parties
            );
            UpdatePartyInternalFactions(
                kingdom,
                parties
            );
            // Trait changes and party splinters affect the next regional tick.
            // Recalculate the national aggregate now without moving city support twice.
            // the national aggregate now without moving city support twice.
            AggregatePartySupportFromCities(
                kingdom,
                parties
            );
            UpdatePartyCoalitions(
                kingdom,
                parties
            );
            UpdatePartyLeadingHistory(
                kingdom,
                parties
            );
            SyncLegacyPartyCompatibility(
                kingdom,
                parties
            );
        }

        private static void EnsurePoliticalPartySchema(
            Kingdom kingdom
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            int schema = GetKingdomIntData(
                kingdom,
                PartySchemaVersionDataKey,
                0
            );

            if (schema >= PartySchemaVersion)
            {
                return;
            }

            SetKingdomIntData(
                kingdom,
                PartySlotCountDataKey,
                0
            );
            SetKingdomIntData(
                kingdom,
                PartySerialDataKey,
                0
            );

            int migratedParties = 0;

            for (int i = 0; i < IdeologyIds.Length; i++)
            {
                string ideology = IdeologyIds[i];
                string suffix = GetMovementKeySuffix(ideology);

                if (
                    GetKingdomIntData(
                        kingdom,
                        PartyActivePrefix + suffix,
                        0
                    ) == 0
                )
                {
                    continue;
                }

                int variant = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        PartyNameVariantPrefix + suffix,
                        0
                    ),
                    0,
                    5
                );
                int foundedYear = GetKingdomIntData(
                    kingdom,
                    PartyFoundedYearPrefix + suffix,
                    GetWorldYearSafe()
                );
                int radicalism = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        MovementRadicalismPrefix + suffix,
                        20
                    ),
                    0,
                    100
                );

                Actor leader = FindMovementLeaderActor(
                    kingdom,
                    ideology
                );
                string legacyLeaderName = GetKingdomStringData(
                    kingdom,
                    PartyLeaderNamePrefix + suffix,
                    ""
                );

                PoliticalParty migrated =
                    CreatePoliticalPartySlot(
                        kingdom,
                        ideology,
                        radicalism,
                        variant,
                        foundedYear,
                        leader,
                        legacyLeaderName,
                        false
                    );

                if (migrated != null)
                {
                    migratedParties++;
                }

                int migrationYear = GetWorldYearSafe();
                if (migrationYear > 0)
                {
                    SetKingdomIntData(
                        kingdom,
                        PartyLastSplitYearPrefix + suffix,
                        migrationYear
                    );
                }
            }

            SetKingdomIntData(
                kingdom,
                PartySchemaVersionDataKey,
                PartySchemaVersion
            );

            if (migratedParties > 0)
            {
                LogInfo(
                    "PoliticalParty migration: kingdom=" +
                    GetWorldObjectDisplayName(kingdom) +
                    ", migrated=" +
                    migratedParties
                );
            }
        }

        private static string PartySlotKey(
            string prefix,
            int slot
        )
        {
            return prefix + slot;
        }


        private static List<PartyHistoryEntry> ClonePartyHistory(
            List<PartyHistoryEntry> pHistory
        )
        {
            List<PartyHistoryEntry> result =
                new List<PartyHistoryEntry>();

            if (pHistory == null)
            {
                return result;
            }

            for (int i = 0; i < pHistory.Count; i++)
            {
                PartyHistoryEntry source = pHistory[i];
                if (source == null)
                {
                    continue;
                }

                PartyHistoryEntry copy = new PartyHistoryEntry();
                copy.Year = source.Year;
                copy.Type = source.Type ?? "";
                copy.Value = source.Value ?? "";
                copy.Extra = source.Extra ?? "";
                result.Add(copy);
            }

            return result;
        }

        private static string NormalizePartyHistoryValue(
            string pValue
        )
        {
            if (string.IsNullOrEmpty(pValue))
            {
                return "";
            }

            return pValue
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static string SerializePartyHistory(
            List<PartyHistoryEntry> pHistory
        )
        {
            if (pHistory == null || pHistory.Count == 0)
            {
                return "";
            }

            List<string> encoded = new List<string>();
            int start = Math.Max(
                0,
                pHistory.Count - MaxPartyHistoryEntries
            );

            for (int i = start; i < pHistory.Count; i++)
            {
                PartyHistoryEntry entry = pHistory[i];
                if (entry == null || string.IsNullOrEmpty(entry.Type))
                {
                    continue;
                }

                string raw =
                    entry.Year + "\t" +
                    NormalizePartyHistoryValue(entry.Type) + "\t" +
                    NormalizePartyHistoryValue(entry.Value) + "\t" +
                    NormalizePartyHistoryValue(entry.Extra);

                try
                {
                    encoded.Add(
                        Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes(raw)
                        )
                    );
                }
                catch
                {
                }
            }

            return string.Join(";", encoded.ToArray());
        }

        private static List<PartyHistoryEntry> ParsePartyHistory(
            string pSerialized
        )
        {
            List<PartyHistoryEntry> result =
                new List<PartyHistoryEntry>();

            if (string.IsNullOrEmpty(pSerialized))
            {
                return result;
            }

            string[] tokens = pSerialized.Split(';');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (string.IsNullOrEmpty(tokens[i]))
                {
                    continue;
                }

                try
                {
                    string raw = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(tokens[i])
                    );
                    string[] fields = raw.Split(
                        new char[] { '\t' },
                        4
                    );
                    if (fields.Length < 2)
                    {
                        continue;
                    }

                    int year = 0;
                    int.TryParse(fields[0], out year);

                    PartyHistoryEntry entry =
                        new PartyHistoryEntry();
                    entry.Year = Math.Max(0, year);
                    entry.Type = fields[1] ?? "";
                    entry.Value = fields.Length > 2
                        ? fields[2] ?? ""
                        : "";
                    entry.Extra = fields.Length > 3
                        ? fields[3] ?? ""
                        : "";

                    if (!string.IsNullOrEmpty(entry.Type))
                    {
                        result.Add(entry);
                    }
                }
                catch
                {
                }
            }

            if (result.Count > MaxPartyHistoryEntries)
            {
                result.RemoveRange(
                    0,
                    result.Count - MaxPartyHistoryEntries
                );
            }

            return result;
        }

        private static List<PartyHistoryEntry> LoadPartyHistory(
            Kingdom pKingdom,
            PoliticalParty pParty
        )
        {
            if (
                pKingdom == null ||
                pParty == null ||
                pParty.Slot < 0
            )
            {
                return new List<PartyHistoryEntry>();
            }

            string serialized = GetKingdomStringData(
                pKingdom,
                PartySlotKey(
                    PartyV2HistoryPrefix,
                    pParty.Slot
                ),
                ""
            );

            return ParsePartyHistory(serialized);
        }

        private static void SavePartyHistory(
            Kingdom pKingdom,
            PoliticalParty pParty
        )
        {
            if (
                pKingdom == null ||
                pParty == null ||
                pParty.Slot < 0
            )
            {
                return;
            }

            if (pParty.History == null)
            {
                pParty.History = new List<PartyHistoryEntry>();
            }

            while (pParty.History.Count > MaxPartyHistoryEntries)
            {
                pParty.History.RemoveAt(0);
            }

            SetKingdomStringData(
                pKingdom,
                PartySlotKey(
                    PartyV2HistoryPrefix,
                    pParty.Slot
                ),
                SerializePartyHistory(pParty.History)
            );
        }

        private static void RecordPartyHistoryEvent(
            Kingdom pKingdom,
            PoliticalParty pParty,
            string pType,
            string pValue,
            string pExtra,
            int pYear
        )
        {
            if (
                pKingdom == null ||
                pParty == null ||
                string.IsNullOrEmpty(pType)
            )
            {
                return;
            }

            if (pParty.History == null)
            {
                pParty.History = LoadPartyHistory(
                    pKingdom,
                    pParty
                );
            }

            int year = pYear > 0
                ? pYear
                : GetWorldYearSafe();
            year = Math.Max(0, year);

            string value = NormalizePartyHistoryValue(pValue);
            string extra = NormalizePartyHistoryValue(pExtra);

            for (int i = 0; i < pParty.History.Count; i++)
            {
                PartyHistoryEntry existing = pParty.History[i];
                if (
                    existing != null &&
                    existing.Year == year &&
                    existing.Type == pType &&
                    (existing.Value ?? "") == value &&
                    (existing.Extra ?? "") == extra
                )
                {
                    return;
                }
            }

            PartyHistoryEntry entry = new PartyHistoryEntry();
            entry.Year = year;
            entry.Type = pType;
            entry.Value = value;
            entry.Extra = extra;
            pParty.History.Add(entry);

            if (pParty.History.Count > MaxPartyHistoryEntries)
            {
                pParty.History.RemoveRange(
                    0,
                    pParty.History.Count - MaxPartyHistoryEntries
                );
            }

            SavePartyHistory(
                pKingdom,
                pParty
            );
        }

        private static void EnsurePartyHistoryInitialized(
            Kingdom pKingdom,
            PoliticalParty pParty
        )
        {
            if (pKingdom == null || pParty == null)
            {
                return;
            }

            if (pParty.History == null)
            {
                pParty.History = LoadPartyHistory(
                    pKingdom,
                    pParty
                );
            }

            if (pParty.History.Count > 0)
            {
                // dev3 wrote zero-year entries on some 0.51.2 builds because
                // the old mapStats reflection did not expose the calendar
                // year. Repair those already-saved rows once instead of
                // forcing the player to start a new world.
                bool repairedYears = false;
                int currentYear = GetWorldYearSafe();

                for (int i = 0; i < pParty.History.Count; i++)
                {
                    PartyHistoryEntry historyEntry = pParty.History[i];
                    if (historyEntry == null || historyEntry.Year > 0)
                    {
                        continue;
                    }

                    if (
                        pParty.FoundedYear > 0 &&
                        (
                            historyEntry.Type == PartyHistoryFounded ||
                            historyEntry.Type == PartyHistoryFirstLeader ||
                            historyEntry.Type == PartyHistorySplitFrom
                        )
                    )
                    {
                        historyEntry.Year = pParty.FoundedYear;
                    }
                    else if (currentYear > 0)
                    {
                        historyEntry.Year = currentYear;
                    }

                    if (historyEntry.Year > 0)
                    {
                        repairedYears = true;
                    }
                }

                if (repairedYears)
                {
                    SavePartyHistory(pKingdom, pParty);
                }

                return;
            }

            if (pParty.FoundedYear > 0)
            {
                RecordPartyHistoryEvent(
                    pKingdom,
                    pParty,
                    PartyHistoryFounded,
                    "",
                    "",
                    pParty.FoundedYear
                );

                if (!string.IsNullOrEmpty(pParty.FounderName))
                {
                    RecordPartyHistoryEvent(
                        pKingdom,
                        pParty,
                        PartyHistoryFirstLeader,
                        pParty.FounderName,
                        "",
                        pParty.FoundedYear
                    );
                }

                if (!string.IsNullOrEmpty(pParty.ParentPartyName))
                {
                    RecordPartyHistoryEvent(
                        pKingdom,
                        pParty,
                        PartyHistorySplitFrom,
                        pParty.ParentPartyName,
                        "",
                        pParty.FoundedYear
                    );
                }
            }
            else
            {
                RecordPartyHistoryEvent(
                    pKingdom,
                    pParty,
                    PartyHistoryTrackingStarted,
                    "",
                    "",
                    GetWorldYearSafe()
                );
            }
        }

        private static void UpdatePartyLeadingHistory(
            Kingdom pKingdom,
            List<PoliticalParty> pParties
        )
        {
            if (
                pKingdom == null ||
                pParties == null ||
                pParties.Count == 0
            )
            {
                return;
            }

            PoliticalParty leading = null;
            for (int i = 0; i < pParties.Count; i++)
            {
                PoliticalParty candidate = pParties[i];
                if (
                    candidate == null ||
                    !candidate.Active ||
                    candidate.Support <= 0
                )
                {
                    continue;
                }

                if (
                    leading == null ||
                    candidate.Support > leading.Support
                )
                {
                    leading = candidate;
                }
            }

            int worldYear = GetWorldYearSafe();

            for (int i = 0; i < pParties.Count; i++)
            {
                PoliticalParty party = pParties[i];
                if (party == null || !party.Active)
                {
                    continue;
                }

                bool isLeading = party == leading;
                int initialized = GetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2LeadingInitPrefix,
                        party.Slot
                    ),
                    0
                );

                if (initialized == 0)
                {
                    SetKingdomIntData(
                        pKingdom,
                        PartySlotKey(
                            PartyV2LeadingInitPrefix,
                            party.Slot
                        ),
                        1
                    );
                    SetKingdomIntData(
                        pKingdom,
                        PartySlotKey(
                            PartyV2LeadingStatePrefix,
                            party.Slot
                        ),
                        isLeading ? 1 : 0
                    );
                    continue;
                }

                bool wasLeading = GetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2LeadingStatePrefix,
                        party.Slot
                    ),
                    0
                ) != 0;

                if (wasLeading == isLeading)
                {
                    continue;
                }

                SetKingdomIntData(
                    pKingdom,
                    PartySlotKey(
                        PartyV2LeadingStatePrefix,
                        party.Slot
                    ),
                    isLeading ? 1 : 0
                );

                RecordPartyHistoryEvent(
                    pKingdom,
                    party,
                    isLeading
                        ? PartyHistoryBecameLeading
                        : PartyHistoryLostLeading,
                    "",
                    "",
                    worldYear
                );
            }
        }

        private static List<PoliticalParty>
            LoadPoliticalPartiesInternal(
                Kingdom kingdom,
                bool includeInactive,
                bool allowInitialization = true
            )
        {
            List<PoliticalParty> result =
                new List<PoliticalParty>();

            if (kingdom == null || kingdom.data == null)
            {
                return result;
            }

            if (allowInitialization)
            {
                EnsurePoliticalPartySchema(kingdom);
            }
            else
            {
                int schema = GetKingdomIntData(
                    kingdom,
                    PartySchemaVersionDataKey,
                    0
                );
                if (schema < PartySchemaVersion)
                {
                    return result;
                }
            }

            int count = ClampInt(
                GetKingdomIntData(
                    kingdom,
                    PartySlotCountDataKey,
                    0
                ),
                0,
                MaxPoliticalParties
            );

            for (int slot = 0; slot < count; slot++)
            {
                int active = GetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ActivePrefix,
                        slot
                    ),
                    0
                );

                if (!includeInactive && active == 0)
                {
                    continue;
                }

                string ideology = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2IdeologyPrefix,
                        slot
                    ),
                    ""
                );

                if (!IsValidIdeology(ideology))
                {
                    continue;
                }

                PoliticalParty party = new PoliticalParty();
                party.Slot = slot;
                party.Active = active != 0;
                party.Id = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(PartyV2IdPrefix, slot),
                    ""
                );
                party.Ideology = ideology;
                party.NameVariant = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2NameVariantPrefix,
                            slot
                        ),
                        0
                    ),
                    0,
                    5
                );
                party.NameStyle = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2NameStylePrefix,
                            slot
                        ),
                        0
                    ),
                    0,
                    1
                );
                string customPartyName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2CustomNamePrefix,
                        slot
                    ),
                    ""
                );
                party.Name = string.IsNullOrWhiteSpace(customPartyName)
                    ? GetPartyLocalizedName(
                        ideology,
                        party.NameVariant,
                        kingdom,
                        party.NameStyle
                    )
                    : customPartyName.Trim();
                party.LeaderIdentity = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderIdentityPrefix,
                        slot
                    ),
                    ""
                );
                party.LeaderName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderNamePrefix,
                        slot
                    ),
                    ""
                );
                party.FounderIdentity = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2FounderIdentityPrefix,
                        slot
                    ),
                    ""
                );
                party.FounderName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2FounderNamePrefix,
                        slot
                    ),
                    ""
                );
                party.FoundedYear = GetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2FoundedYearPrefix,
                        slot
                    ),
                    0
                );
                party.Radicalism = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2RadicalismPrefix,
                            slot
                        ),
                        20
                    ),
                    0,
                    100
                );
                party.Position = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2PositionPrefix,
                        slot
                    ),
                    DeterminePartyPosition(
                        party.Radicalism
                    )
                );
                party.Strategy = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2StrategyPrefix,
                        slot
                    ),
                    DeterminePartyStrategy(
                        party.Radicalism
                    )
                );
                party.ForeignStance = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ForeignStancePrefix,
                        slot
                    ),
                    DeterminePartyForeignStance(
                        kingdom,
                        party.Id
                    )
                );
                party.SupportBias = ClampInt(
                    GetKingdomIntData(
                        kingdom,
                        PartySlotKey(
                            PartyV2SupportBiasPrefix,
                            slot
                        ),
                        100
                    ),
                    60,
                    140
                );
                party.ColorSeed = GetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ColorSeedPrefix,
                        slot
                    ),
                    -1
                );
                if (
                    party.ColorSeed < 0 ||
                    party.ColorSeed >= PartyColorVariantCount
                )
                {
                    party.ColorSeed = ChooseNewPartyColorSeed(
                        kingdom,
                        party.Ideology,
                        party.Id,
                        slot
                    );
                    if (allowInitialization)
                    {
                        SetKingdomIntData(
                            kingdom,
                            PartySlotKey(
                                PartyV2ColorSeedPrefix,
                                slot
                            ),
                            party.ColorSeed
                        );
                    }
                }
                party.OriginCityId = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2OriginCityIdPrefix,
                        slot
                    ),
                    ""
                );
                party.OriginCityName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2OriginCityNamePrefix,
                        slot
                    ),
                    ""
                );
                party.ParentPartyId = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ParentPartyIdPrefix,
                        slot
                    ),
                    ""
                );
                party.ParentPartyName = GetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ParentPartyNamePrefix,
                        slot
                    ),
                    ""
                );

                if (
                    allowInitialization &&
                    string.IsNullOrEmpty(party.OriginCityId)
                )
                {
                    City origin = ChoosePartyOriginCity(
                        kingdom,
                        null
                    );
                    if (origin != null)
                    {
                        party.OriginCityId =
                            GetStableObjectIdentity(origin);
                        party.OriginCityName =
                            GetWorldObjectDisplayName(origin);
                        SetKingdomStringData(
                            kingdom,
                            PartySlotKey(
                                PartyV2OriginCityIdPrefix,
                                slot
                            ),
                            party.OriginCityId
                        );
                        SetKingdomStringData(
                            kingdom,
                            PartySlotKey(
                                PartyV2OriginCityNamePrefix,
                                slot
                            ),
                            party.OriginCityName
                        );
                    }
                }

                if (allowInitialization)
                {
                    party.Traits = LoadOrInitializePartyTraits(
                        kingdom,
                        party
                    );
                }
                else
                {
                    string serializedTraits = GetKingdomStringData(
                        kingdom,
                        PartySlotKey(
                            PartyV2TraitsPrefix,
                            slot
                        ),
                        ""
                    );
                    party.Traits = ParsePartyTraits(serializedTraits);
                    if (party.Traits.Count == 0)
                    {
                        party.Traits = BuildInitialPartyTraits(
                            kingdom,
                            party.Ideology,
                            party.Radicalism,
                            party.Strategy,
                            party.ForeignStance,
                            party.Id,
                            false
                        );
                    }
                }

                party.History = LoadPartyHistory(
                    kingdom,
                    party
                );
                if (allowInitialization)
                {
                    EnsurePartyHistoryInitialized(
                        kingdom,
                        party
                    );
                }
                party.SupportHistory = LoadPartySupportHistory(
                    kingdom,
                    party
                );

                result.Add(party);
            }

            // Migrate parent names lazily for old 1.3.x saves whenever the
            // parent still exists in the same kingdom. Future splits and
            // successor parties persist the name at creation time.
            for (int i = 0; i < result.Count; i++)
            {
                PoliticalParty child = result[i];
                if (
                    child == null ||
                    string.IsNullOrEmpty(child.ParentPartyId) ||
                    !string.IsNullOrEmpty(child.ParentPartyName)
                )
                {
                    continue;
                }

                for (int j = 0; j < result.Count; j++)
                {
                    PoliticalParty parent = result[j];
                    if (
                        parent != null &&
                        parent.Id == child.ParentPartyId
                    )
                    {
                        child.ParentPartyName = parent.Name;
                        if (allowInitialization)
                        {
                            SetKingdomStringData(
                                kingdom,
                                PartySlotKey(
                                    PartyV2ParentPartyNamePrefix,
                                    child.Slot
                                ),
                                child.ParentPartyName
                            );
                        }
                        break;
                    }
                }
            }

            // dev3 migration: old saves knew the parent ID before party
            // biographies existed. Once the parent name is resolved, backfill
            // the split on both biographies without inventing any other past.
            if (allowInitialization)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    PoliticalParty child = result[i];
                    if (
                        child == null ||
                        string.IsNullOrEmpty(child.ParentPartyId) ||
                        string.IsNullOrEmpty(child.ParentPartyName)
                    )
                    {
                        continue;
                    }

                    // Never invent a split date for migrated parties whose
                    // founding year was not tracked. Otherwise loading the same
                    // old save in a later year would append a new fake split
                    // event every time.
                    if (child.FoundedYear <= 0)
                    {
                        continue;
                    }

                    int splitYear = child.FoundedYear;

                    RecordPartyHistoryEvent(
                        kingdom,
                        child,
                        PartyHistorySplitFrom,
                        child.ParentPartyName,
                        "",
                        splitYear
                    );

                    for (int j = 0; j < result.Count; j++)
                    {
                        PoliticalParty parent = result[j];
                        if (
                            parent != null &&
                            parent.Id == child.ParentPartyId
                        )
                        {
                            RecordPartyHistoryEvent(
                                kingdom,
                                parent,
                                PartyHistorySplitChild,
                                child.Name,
                                "",
                                splitYear
                            );
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private static List<PoliticalParty>
            GetPoliticalParties(
                Kingdom kingdom
            )
        {
            List<PoliticalParty> parties =
                LoadPoliticalPartiesInternal(
                    kingdom,
                    false
                );

            AggregatePartySupportFromCities(
                kingdom,
                parties
            );

            return parties;
        }

        private static List<PoliticalParty>
            GetPoliticalPartiesReadOnly(
                Kingdom kingdom
            )
        {
            List<PoliticalParty> parties =
                LoadPoliticalPartiesInternal(
                    kingdom,
                    false,
                    false
                );

            AggregatePartySupportFromCities(
                kingdom,
                parties
            );

            return parties;
        }

        private static List<PoliticalParty>
            GetPartiesForIdeology(
                List<PoliticalParty> parties,
                string ideology
            )
        {
            List<PoliticalParty> result =
                new List<PoliticalParty>();

            if (parties == null)
            {
                return result;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                if (
                    party != null &&
                    party.Active &&
                    party.Ideology == ideology
                )
                {
                    result.Add(party);
                }
            }

            return result;
        }

        private static bool PoliticalSystemRequiresPartyInstitution(
            string system
        )
        {
            return
                system == PoliticalSystemCompetitiveId ||
                system == PoliticalSystemOnePartyId ||
                system == PoliticalSystemSovietOnePartyId;
        }

        private static void EnsureInstitutionalStateIdeologyParty(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            if (kingdom == null || parties == null)
            {
                return;
            }

            string system = GetPoliticalSystem(kingdom);
            if (!PoliticalSystemRequiresPartyInstitution(system))
            {
                return;
            }

            string stateIdeology = GetStateIdeology(kingdom);
            if (!IsValidIdeology(stateIdeology))
            {
                return;
            }

            List<PoliticalParty> stateParties =
                GetPartiesForIdeology(
                    parties,
                    stateIdeology
                );
            if (stateParties.Count > 0)
            {
                return;
            }

            int radicalism = 20;
            string current = GetStateIdeologyCurrent(kingdom);
            if (!string.IsNullOrEmpty(current))
            {
                radicalism = ClampInt(
                    GetIdeologyRadicalismScore(current),
                    0,
                    100
                );
            }

            PoliticalParty created = CreatePoliticalParty(
                kingdom,
                stateIdeology,
                radicalism,
                false
            );
            if (created == null)
            {
                return;
            }

            parties.Add(created);

            if (VerbosePoliticalDiagnostics)
            {
                LogInfo(
                    "[PW-PARTY-BOOTSTRAP] created state party kingdom=" +
                    GetWorldObjectDisplayName(kingdom) +
                    " ideology=" + stateIdeology +
                    " system=" + system
                );
            }
        }

        private static PoliticalParty CreatePoliticalParty(
            Kingdom kingdom,
            string ideology,
            int radicalism,
            bool isSplit
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology)
            )
            {
                return null;
            }

            EnsurePoliticalPartySchema(kingdom);

            List<PoliticalParty> active =
                LoadPoliticalPartiesInternal(
                    kingdom,
                    false
                );
            List<PoliticalParty> same =
                GetPartiesForIdeology(
                    active,
                    ideology
                );

            if (
                active.Count >= MaxPoliticalParties ||
                same.Count >= MaxPoliticalPartiesPerIdeology
            )
            {
                return null;
            }

            HashSet<string> excludedLeaders =
                new HashSet<string>();

            for (int i = 0; i < same.Count; i++)
            {
                if (
                    !string.IsNullOrEmpty(
                        same[i].LeaderIdentity
                    )
                )
                {
                    excludedLeaders.Add(
                        same[i].LeaderIdentity
                    );
                }
            }

            Actor leader = FindPartyLeaderActor(
                kingdom,
                ideology,
                excludedLeaders
            );
            int variant = ChooseUnusedPartyNameVariant(
                kingdom,
                ideology,
                radicalism
            );

            return CreatePoliticalPartySlot(
                kingdom,
                ideology,
                radicalism,
                variant,
                GetWorldYearSafe(),
                leader,
                "",
                isSplit
            );
        }

        private static PoliticalParty CreatePoliticalPartySlot(
            Kingdom kingdom,
            string ideology,
            int radicalism,
            int nameVariant,
            int foundedYear,
            Actor leader,
            string fallbackLeaderName,
            bool isSplit
        )
        {
            if (
                kingdom == null ||
                kingdom.data == null ||
                !IsValidIdeology(ideology)
            )
            {
                return null;
            }

            int count = ClampInt(
                GetKingdomIntData(
                    kingdom,
                    PartySlotCountDataKey,
                    0
                ),
                0,
                MaxPoliticalParties
            );

            int slot = -1;

            // Preserve inactive parties as historical records while the bounded
            // slot pool still has unused capacity. Only recycle an inactive
            // slot after all MaxPoliticalParties slots have existed at least
            // once; this matches the public API's deactivate-not-delete model.
            if (count < MaxPoliticalParties)
            {
                slot = count;
                count++;
                SetKingdomIntData(
                    kingdom,
                    PartySlotCountDataKey,
                    count
                );
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (
                        GetKingdomIntData(
                            kingdom,
                            PartySlotKey(
                                PartyV2ActivePrefix,
                                i
                            ),
                            0
                        ) == 0
                    )
                    {
                        slot = i;
                        break;
                    }
                }

                if (slot < 0)
                {
                    return null;
                }
            }

            int serial = GetKingdomIntData(
                kingdom,
                PartySerialDataKey,
                0
            ) + 1;
            SetKingdomIntData(
                kingdom,
                PartySerialDataKey,
                serial
            );

            int year = foundedYear;
            if (year < 0)
            {
                year = 0;
            }

            string id =
                "pw_" +
                GetStableObjectIdentity(kingdom) +
                "_" +
                GetMovementKeySuffix(ideology) +
                "_" +
                year +
                "_" +
                serial;

            int finalRadicalism = ClampInt(
                radicalism,
                0,
                100
            );
            int supportBias = 92 + Math.Abs(
                StablePartyHash(id + "|support")
            ) % 19;

            if (isSplit)
            {
                supportBias = Math.Max(
                    72,
                    supportBias - 8
                );
            }

            int colorSeed = ChooseNewPartyColorSeed(
                kingdom,
                ideology,
                id,
                -1
            );

            string initialPosition = DeterminePartyPosition(
                finalRadicalism
            );
            string initialStrategy = DeterminePartyStrategy(
                finalRadicalism
            );
            string initialForeignStance =
                DeterminePartyForeignStance(
                    kingdom,
                    id
                );
            List<string> initialTraits =
                BuildInitialPartyTraits(
                    kingdom,
                    ideology,
                    finalRadicalism,
                    initialStrategy,
                    initialForeignStance,
                    id,
                    isSplit
                );

            string leaderIdentity = "";
            string leaderName = fallbackLeaderName ?? "";

            if (leader != null && leader.isAlive())
            {
                leaderIdentity = GetStableObjectIdentity(
                    leader
                );
                leaderName = GetWorldObjectDisplayName(
                    leader
                );
            }

            City originCity = ChoosePartyOriginCity(
                kingdom,
                leader
            );
            string originCityId = originCity == null
                ? ""
                : GetStableObjectIdentity(originCity);
            string originCityName = originCity == null
                ? ""
                : GetWorldObjectDisplayName(originCity);

            SetKingdomIntData(
                kingdom,
                PartySlotKey(PartyV2ActivePrefix, slot),
                1
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(PartyV2IdPrefix, slot),
                id
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(PartyV2IdeologyPrefix, slot),
                ideology
            );
            int nameStyle = ChoosePartyNameStyle(id);
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2NameVariantPrefix,
                    slot
                ),
                ClampInt(nameVariant, 0, 5)
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2NameStylePrefix,
                    slot
                ),
                nameStyle
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2CustomNamePrefix,
                    slot
                ),
                ""
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2LeaderIdentityPrefix,
                    slot
                ),
                leaderIdentity
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2LeaderNamePrefix,
                    slot
                ),
                leaderName
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2FounderIdentityPrefix,
                    slot
                ),
                leaderIdentity
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2FounderNamePrefix,
                    slot
                ),
                leaderName
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2FoundedYearPrefix,
                    slot
                ),
                year
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2RadicalismPrefix,
                    slot
                ),
                finalRadicalism
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2PositionPrefix,
                    slot
                ),
                initialPosition
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2StrategyPrefix,
                    slot
                ),
                initialStrategy
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ForeignStancePrefix,
                    slot
                ),
                initialForeignStance
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2SupportBiasPrefix,
                    slot
                ),
                supportBias
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2ColorSeedPrefix,
                    slot
                ),
                colorSeed
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2TraitsPrefix,
                    slot
                ),
                SerializePartyTraits(initialTraits)
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2TraitLastYearPrefix,
                    slot
                ),
                year
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2TraitLastSupportPrefix,
                    slot
                ),
                0
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2OriginCityIdPrefix,
                    slot
                ),
                originCityId
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2OriginCityNamePrefix,
                    slot
                ),
                originCityName
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyIdPrefix,
                    slot
                ),
                ""
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2ParentPartyNamePrefix,
                    slot
                ),
                ""
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyFactionPressurePrefix,
                    slot
                ),
                0
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyFactionLastYearPrefix,
                    slot
                ),
                year
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyFactionLastSplitYearPrefix,
                    slot
                ),
                0
            );

            // Slots are reused after a party dissolves. History belongs to the
            // party identity, not the numeric slot, so a successor in the same
            // slot must never inherit the previous organization's biography or
            // support graph/timestamp.
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2HistoryPrefix,
                    slot
                ),
                ""
            );
            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2SupportHistoryPrefix,
                    slot
                ),
                ""
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2SupportHistoryLastYearPrefix,
                    slot
                ),
                0
            );

            PoliticalParty result =
                new PoliticalParty();
            result.Slot = slot;
            result.Active = true;
            result.Id = id;
            result.Ideology = ideology;
            result.NameVariant = ClampInt(
                nameVariant,
                0,
                5
            );
            result.NameStyle = nameStyle;
            result.Name = GetPartyLocalizedName(
                ideology,
                result.NameVariant,
                kingdom,
                result.NameStyle
            );
            result.LeaderIdentity = leaderIdentity;
            result.LeaderName = leaderName;
            result.LeaderActor = leader;
            result.FounderIdentity = leaderIdentity;
            result.FounderName = leaderName;
            result.FoundedYear = year;
            result.Radicalism = finalRadicalism;
            result.Position = initialPosition;
            result.Strategy = initialStrategy;
            result.ForeignStance = initialForeignStance;
            result.SupportBias = supportBias;
            result.Support = 0;
            result.ColorSeed = colorSeed;
            result.Traits = initialTraits;
            result.OriginCityId = originCityId;
            result.OriginCityName = originCityName;
            result.ParentPartyId = "";
            result.ParentPartyName = "";
            result.History = new List<PartyHistoryEntry>();
            result.SupportHistory = new List<PartySupportHistoryEntry>();

            // A newly created party starts non-leading; if it immediately
            // becomes the largest party, the leading-status tracker below
            // records that real political milestone.
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2LeadingInitPrefix,
                    result.Slot
                ),
                1
            );
            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2LeadingStatePrefix,
                    result.Slot
                ),
                0
            );

            EnsurePartyHistoryInitialized(
                kingdom,
                result
            );

            PoliticalWorldAPI.InternalEmitCoreEvent(
                PoliticalWorldAPI.Events.PartyCreated,
                kingdom,
                "",
                ideology,
                0,
                finalRadicalism,
                result.Id ?? ""
            );

            return result;
        }

        private static void DeactivatePoliticalParty(
            Kingdom kingdom,
            PoliticalParty party
        )
        {
            if (
                kingdom == null ||
                party == null ||
                party.Slot < 0
            )
            {
                return;
            }

            bool wasActive = party.Active;

            SetKingdomIntData(
                kingdom,
                PartySlotKey(
                    PartyV2ActivePrefix,
                    party.Slot
                ),
                0
            );
            party.Active = false;

            if (wasActive)
            {
                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.PartyDeactivated,
                    kingdom,
                    party.Ideology ?? "",
                    "",
                    party.Support,
                    0,
                    party.Id ?? ""
                );
            }
        }

        private static void TrySplitPoliticalParty(
            Kingdom kingdom,
            string ideology,
            int support,
            int movementActive,
            int movementRadicalism,
            List<PoliticalParty> ideologyParties,
            List<PoliticalParty> allParties
        )
        {
            if (
                kingdom == null ||
                movementActive == 0 ||
                ideologyParties == null ||
                ideologyParties.Count == 0 ||
                ideologyParties.Count >= MaxPoliticalPartiesPerIdeology ||
                allParties == null ||
                allParties.Count >= MaxPoliticalParties
            )
            {
                return;
            }

            int traitAdjustedSplitThreshold =
                GetPartySplitSupportThreshold(
                    kingdom,
                    ideologyParties
                );
            if (support < traitAdjustedSplitThreshold)
            {
                return;
            }

            int worldYear = GetWorldYearSafe();
            if (worldYear <= 0)
            {
                return;
            }

            int oldestYear = worldYear;
            int radicalismTotal = 0;
            int nearestDistance = 101;

            for (int i = 0; i < ideologyParties.Count; i++)
            {
                PoliticalParty party = ideologyParties[i];
                if (party.FoundedYear > 0)
                {
                    oldestYear = Math.Min(
                        oldestYear,
                        party.FoundedYear
                    );
                }

                radicalismTotal += party.Radicalism;
                nearestDistance = Math.Min(
                    nearestDistance,
                    Math.Abs(
                        party.Radicalism -
                        movementRadicalism
                    )
                );
            }

            if (
                worldYear - oldestYear <
                PartySplitMinAgeYears
            )
            {
                return;
            }

            string splitKey =
                PartyLastSplitYearPrefix +
                GetMovementKeySuffix(ideology);
            int lastSplitYear = GetKingdomIntData(
                kingdom,
                splitKey,
                0
            );

            if (
                lastSplitYear > 0 &&
                worldYear - lastSplitYear <
                PartySplitCooldownYears +
                    GetPartySplitCooldownModifier(
                        ideologyParties
                    )
            )
            {
                return;
            }

            if (
                nearestDistance < 18 &&
                support < PartySplitStrongSupportThreshold
            )
            {
                return;
            }

            int averageRadicalism =
                radicalismTotal /
                Math.Max(1, ideologyParties.Count);

            int newRadicalism;
            if (movementRadicalism >= averageRadicalism)
            {
                newRadicalism = ClampInt(
                    Math.Max(
                        movementRadicalism,
                        averageRadicalism + 20
                    ),
                    5,
                    95
                );
            }
            else
            {
                newRadicalism = ClampInt(
                    Math.Min(
                        movementRadicalism,
                        averageRadicalism - 20
                    ),
                    5,
                    95
                );
            }

            bool tooClose = false;
            for (int i = 0; i < ideologyParties.Count; i++)
            {
                if (
                    Math.Abs(
                        ideologyParties[i].Radicalism -
                        newRadicalism
                    ) < 12
                )
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                newRadicalism = ClampInt(
                    averageRadicalism >= 50
                        ? averageRadicalism - 24
                        : averageRadicalism + 24,
                    5,
                    95
                );
            }

            PoliticalParty created = CreatePoliticalParty(
                kingdom,
                ideology,
                newRadicalism,
                true
            );

            if (created == null)
            {
                return;
            }

            PoliticalParty parentParty = null;
            int parentSupport = -1;
            for (int i = 0; i < ideologyParties.Count; i++)
            {
                PoliticalParty candidateParent = ideologyParties[i];
                if (
                    candidateParent != null &&
                    candidateParent.Support > parentSupport
                )
                {
                    parentSupport = candidateParent.Support;
                    parentParty = candidateParent;
                }
            }

            if (parentParty != null)
            {
                created.ParentPartyId = parentParty.Id;
                created.ParentPartyName = parentParty.Name;
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ParentPartyIdPrefix,
                        created.Slot
                    ),
                    parentParty.Id
                );
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2ParentPartyNamePrefix,
                        created.Slot
                    ),
                    parentParty.Name
                );

                created.ColorSeed = ChooseRelatedPartyColorSeed(
                    kingdom,
                    created.Ideology,
                    parentParty.ColorSeed,
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

                RecordPartyHistoryEvent(
                    kingdom,
                    created,
                    PartyHistorySplitFrom,
                    parentParty.Name,
                    "",
                    worldYear
                );
                RecordPartyHistoryEvent(
                    kingdom,
                    parentParty,
                    PartyHistorySplitChild,
                    created.Name,
                    "",
                    worldYear
                );
            }

            SetKingdomIntData(
                kingdom,
                splitKey,
                worldYear
            );
            allParties.Add(created);

            PublishPoliticalEvent(
                string.Format(
                    LM.Get("ukiol_event_party_split"),
                    created.Name,
                    GetWorldObjectDisplayName(kingdom),
                    GetIdeologyName(ideology)
                ),
                kingdom,
                null,
                created.LeaderActor,
                GetIdeologyIconPath(ideology),
                "party_split_" + created.Id,
                30f
            );
        }

        private static void RefreshPartyLeaders(
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

            HashSet<string> reserved =
                new HashSet<string>();

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                Actor current = FindPartyActorByIdentity(
                    kingdom,
                    party.LeaderIdentity,
                    party.LeaderName
                );

                if (current != null)
                {
                    party.LeaderActor = current;
                    party.LeaderIdentity =
                        GetStableObjectIdentity(current);
                    party.LeaderName =
                        GetWorldObjectDisplayName(current);
                    reserved.Add(
                        party.LeaderIdentity
                    );
                    continue;
                }

                Actor replacement = FindPartyLeaderActor(
                    kingdom,
                    party.Ideology,
                    reserved
                );

                if (replacement == null)
                {
                    continue;
                }

                string previousLeaderName =
                    party.LeaderName ?? "";

                party.LeaderActor = replacement;
                party.LeaderIdentity =
                    GetStableObjectIdentity(replacement);
                party.LeaderName =
                    GetWorldObjectDisplayName(replacement);
                reserved.Add(
                    party.LeaderIdentity
                );

                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderIdentityPrefix,
                        party.Slot
                    ),
                    party.LeaderIdentity
                );
                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2LeaderNamePrefix,
                        party.Slot
                    ),
                    party.LeaderName
                );

                RecordPartyHistoryEvent(
                    kingdom,
                    party,
                    PartyHistoryLeaderChanged,
                    party.LeaderName,
                    previousLeaderName,
                    GetWorldYearSafe()
                );

                PoliticalWorldAPI.InternalEmitCoreEvent(
                    PoliticalWorldAPI.Events.PartyLeaderChanged,
                    kingdom,
                    previousLeaderName,
                    party.LeaderName ?? "",
                    0,
                    0,
                    party.Id ?? ""
                );
            }
        }

        private static Actor FindPartyActorByIdentity(
            Kingdom kingdom,
            string identity,
            string fallbackName
        )
        {
            if (
                kingdom == null ||
                (
                    string.IsNullOrEmpty(identity) &&
                    string.IsNullOrEmpty(fallbackName)
                )
            )
            {
                return null;
            }

            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (
                int c = 0;
                c < cities.Count && inspected < 360;
                c++
            )
            {
                List<Actor> units =
                    GetCityUnitsSafe(cities[c], 360 - inspected);

                for (
                    int i = 0;
                    i < units.Count && inspected < 360;
                    i++
                )
                {
                    Actor actor = units[i];
                    inspected++;

                    if (
                        actor == null ||
                        !actor.isAlive()
                    )
                    {
                        continue;
                    }

                    if (
                        !string.IsNullOrEmpty(identity) &&
                        GetStableObjectIdentity(actor) ==
                        identity
                    )
                    {
                        return actor;
                    }

                    if (
                        string.IsNullOrEmpty(identity) &&
                        !string.IsNullOrEmpty(fallbackName) &&
                        GetWorldObjectDisplayName(actor) ==
                        fallbackName
                    )
                    {
                        return actor;
                    }
                }
            }

            return null;
        }

        private static Actor FindPartyLeaderActor(
            Kingdom kingdom,
            string ideology,
            HashSet<string> excludedIdentities
        )
        {
            if (
                kingdom == null ||
                !IsValidIdeology(ideology)
            )
            {
                return null;
            }

            Actor best = null;
            float bestScore = -100000f;
            int inspected = 0;
            List<City> cities = GetCitiesSafe(kingdom);

            for (
                int c = 0;
                c < cities.Count && inspected < 320;
                c++
            )
            {
                List<Actor> units =
                    GetCityUnitsSafe(cities[c], 320 - inspected);

                for (
                    int i = 0;
                    i < units.Count && inspected < 320;
                    i++
                )
                {
                    Actor actor = units[i];
                    inspected++;

                    if (
                        actor == null ||
                        !actor.isAlive() ||
                        GetCitizenIdeology(actor) != ideology
                    )
                    {
                        continue;
                    }

                    string identity =
                        GetStableObjectIdentity(actor);

                    if (
                        excludedIdentities != null &&
                        excludedIdentities.Contains(identity)
                    )
                    {
                        continue;
                    }

                    float score =
                        GetCitizenIdeologyConviction(actor);

                    try
                    {
                        if (actor.stats != null)
                        {
                            score += actor.stats[S.diplomacy] * 0.55f;
                            score += actor.stats[S.stewardship] * 0.50f;
                            score += actor.stats[S.warfare] * 0.30f;
                        }
                    }
                    catch
                    {
                    }

                    score += (
                        Math.Abs(
                            StablePartyHash(
                                identity + "|" + ideology
                            )
                        ) % 17
                    ) / 10f;

                    if (score > bestScore)
                    {
                        best = actor;
                        bestScore = score;
                    }
                }
            }

            if (best != null)
            {
                return best;
            }

            return FindMovementLeaderActor(
                kingdom,
                ideology
            );
        }

        private static int ChooseUnusedPartyNameVariant(
            Kingdom kingdom,
            string ideology,
            int radicalism
        )
        {
            bool[] used = new bool[6];
            List<PoliticalParty> parties =
                LoadPoliticalPartiesInternal(
                    kingdom,
                    false
                );

            for (int i = 0; i < parties.Count; i++)
            {
                if (
                    parties[i].Ideology == ideology &&
                    parties[i].NameVariant >= 0 &&
                    parties[i].NameVariant < used.Length
                )
                {
                    used[parties[i].NameVariant] = true;
                }
            }

            int band = radicalism >= 65
                ? 2
                : radicalism >= 35
                    ? 1
                    : 0;
            int first = band * 2;
            int hash = Math.Abs(
                StablePartyHash(
                    GetStableObjectIdentity(kingdom) +
                    "|" +
                    ideology +
                    "|" +
                    GetWorldYearSafe()
                )
            );
            int preferred = first + (hash % 2);

            if (!used[preferred])
            {
                return preferred;
            }

            int alternate = first + (
                preferred == first ? 1 : 0
            );
            if (!used[alternate])
            {
                return alternate;
            }

            for (int i = 0; i < used.Length; i++)
            {
                if (!used[i])
                {
                    return i;
                }
            }

            return preferred;
        }

        private static int StablePartyHash(
            string value
        )
        {
            unchecked
            {
                int hash = 23;
                string safe = value ?? "";

                for (int i = 0; i < safe.Length; i++)
                {
                    hash = hash * 31 + safe[i];
                }

                return hash == int.MinValue
                    ? int.MaxValue
                    : Math.Abs(hash);
            }
        }

        private static string DeterminePartyPosition(
            int radicalism
        )
        {
            if (radicalism >= 65)
            {
                return "radical";
            }

            if (radicalism >= 35)
            {
                return "mainstream";
            }

            return "moderate";
        }

        private static string DeterminePartyStrategy(
            int radicalism
        )
        {
            if (radicalism >= 68)
            {
                return "revolutionary";
            }

            if (radicalism >= 45)
            {
                return "protest";
            }

            return "parliamentary";
        }

        private static string DeterminePartyForeignStance(
            Kingdom kingdom,
            string partyId
        )
        {
            string course = GetKingdomCourse(kingdom);
            int jitter = StablePartyHash(
                (partyId ?? "") + "|foreign"
            ) % 5;

            if (course == MilitaristTraitId)
            {
                return jitter == 0
                    ? "pragmatic"
                    : "hawkish";
            }

            if (course == DiplomatTraitId)
            {
                return jitter == 0
                    ? "pragmatic"
                    : "dovish";
            }

            return jitter == 0
                ? "dovish"
                : jitter == 1
                    ? "hawkish"
                    : "pragmatic";
        }

        private static int ChoosePartyNameStyle(string partyId)
        {
            int roll = (StablePartyHash(
                (partyId ?? "") + "|name_style"
            ) & int.MaxValue) % 100;
            return roll < CountryBasedPartyNameChance ? 1 : 0;
        }

        private static string GetPartyLocalizedName(
            string ideology,
            int variant,
            Kingdom kingdom,
            int nameStyle
        )
        {
            if (nameStyle == 1 && kingdom != null)
            {
                // Party templates use the stable base country name. Otherwise
                // we get gems like "Communist Party of People's Republic of X".
                string kingdomName = GetBaseCountryName(kingdom);
                if (!string.IsNullOrWhiteSpace(kingdomName))
                {
                    string countryKey =
                        "ukiol_party_country_name_" +
                        GetMovementKeySuffix(ideology) +
                        "_" +
                        (ClampInt(variant, 0, 5) % 2);
                    string countryTemplate = LM.Get(countryKey);

                    if (
                        !string.IsNullOrEmpty(countryTemplate) &&
                        countryTemplate != countryKey
                    )
                    {
                        try
                        {
                            return string.Format(
                                countryTemplate,
                                kingdomName
                            );
                        }
                        catch
                        {
                            // Fall through to the regular ideology name.
                        }
                    }
                }
            }

            return GetPartyLocalizedName(ideology, variant);
        }

        private static string GetPartyLocalizedName(
            string ideology,
            int variant
        )
        {
            if (!IsValidIdeology(ideology))
            {
                return LM.Get("ukiol_party_none");
            }

            string key =
                "ukiol_party_name_" +
                GetMovementKeySuffix(ideology) +
                "_" +
                ClampInt(variant, 0, 5);

            string localized = LM.Get(key);

            if (
                string.IsNullOrEmpty(localized) ||
                localized == key
            )
            {
                return GetIdeologyName(ideology);
            }

            return localized;
        }


        private static List<string> ParsePartyTraits(
            string serialized
        )
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(serialized))
            {
                return result;
            }

            string[] pieces = serialized.Split(',');

            for (int i = 0; i < pieces.Length; i++)
            {
                string trait = (pieces[i] ?? "").Trim();

                if (
                    IsValidPartyTrait(trait) &&
                    !result.Contains(trait)
                )
                {
                    result.Add(trait);

                    if (result.Count >= MaxPartyTraits)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static string SerializePartyTraits(
            List<string> traits
        )
        {
            if (traits == null || traits.Count == 0)
            {
                return "";
            }

            return string.Join(",", traits.ToArray());
        }

        private static bool IsValidPartyTrait(
            string trait
        )
        {
            return
                trait == PartyTraitMass ||
                trait == PartyTraitElite ||
                trait == PartyTraitDisciplined ||
                trait == PartyTraitFactional ||
                trait == PartyTraitReformist ||
                trait == PartyTraitPopulist ||
                trait == PartyTraitRevolutionary ||
                trait == PartyTraitMilitarized ||
                trait == PartyTraitCorrupt ||
                trait == PartyTraitSplintered;
        }

        private static bool HasPartyTrait(
            PoliticalParty party,
            string trait
        )
        {
            return
                party != null &&
                party.Traits != null &&
                party.Traits.Contains(trait);
        }

        private static bool TryAddPartyTrait(
            PoliticalParty party,
            string trait
        )
        {
            if (
                party == null ||
                !IsValidPartyTrait(trait)
            )
            {
                return false;
            }

            if (party.Traits == null)
            {
                party.Traits = new List<string>();
            }

            if (party.Traits.Contains(trait))
            {
                return false;
            }

            if (party.Traits.Count >= MaxPartyTraits)
            {
                return false;
            }

            party.Traits.Add(trait);
            return true;
        }

        private static bool RemovePartyTrait(
            PoliticalParty party,
            string trait
        )
        {
            return
                party != null &&
                party.Traits != null &&
                party.Traits.Remove(trait);
        }

        private static List<string> BuildInitialPartyTraits(
            Kingdom kingdom,
            string ideology,
            int radicalism,
            string strategy,
            string foreignStance,
            string partyId,
            bool isSplit
        )
        {
            List<string> traits = new List<string>();
            PoliticalParty temporary = new PoliticalParty();
            temporary.Traits = traits;

            // First trait describes the political style of the organization.
            if (radicalism >= 68 || strategy == "revolutionary")
            {
                TryAddPartyTrait(
                    temporary,
                    PartyTraitRevolutionary
                );
            }
            else if (
                radicalism <= 36 &&
                strategy == "parliamentary"
            )
            {
                TryAddPartyTrait(
                    temporary,
                    PartyTraitReformist
                );
            }
            else
            {
                TryAddPartyTrait(
                    temporary,
                    PartyTraitPopulist
                );
            }

            // A newly split organization starts fragile instead of instantly
            // receiving a polished organizational identity.
            if (isSplit)
            {
                TryAddPartyTrait(
                    temporary,
                    PartyTraitSplintered
                );
                return traits;
            }

            int hash = StablePartyHash(
                (partyId ?? "") + "|organization"
            );
            string organizationTrait;

            if (
                foreignStance == "hawkish" &&
                (
                    ideology == FascismIdeologyId ||
                    GetKingdomCourse(kingdom) ==
                        MilitaristTraitId
                )
            )
            {
                organizationTrait = PartyTraitMilitarized;
            }
            else if (
                ideology == AnarchismIdeologyId &&
                hash % 3 != 0
            )
            {
                organizationTrait = PartyTraitFactional;
            }
            else if (
                (
                    ideology == SocialismIdeologyId ||
                    ideology == CommunismIdeologyId ||
                    ideology == SyndicalismIdeologyId ||
                    ideology == DemocracyIdeologyId
                ) &&
                hash % 3 != 0
            )
            {
                organizationTrait = PartyTraitMass;
            }
            else if (
                (
                    ideology == MonarchismIdeologyId ||
                    ideology == ConservatismIdeologyId
                ) &&
                radicalism < 55 &&
                hash % 2 == 0
            )
            {
                organizationTrait = PartyTraitElite;
            }
            else
            {
                int pick = hash % 4;
                organizationTrait =
                    pick == 0
                        ? PartyTraitMass
                        : pick == 1
                            ? PartyTraitElite
                            : pick == 2
                                ? PartyTraitDisciplined
                                : PartyTraitFactional;
            }

            TryAddPartyTrait(
                temporary,
                organizationTrait
            );

            return traits;
        }

        private static List<string> LoadOrInitializePartyTraits(
            Kingdom kingdom,
            PoliticalParty party
        )
        {
            if (
                kingdom == null ||
                party == null ||
                party.Slot < 0
            )
            {
                return new List<string>();
            }

            string serialized = GetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2TraitsPrefix,
                    party.Slot
                ),
                ""
            );
            List<string> traits =
                ParsePartyTraits(serialized);

            if (traits.Count == 0)
            {
                traits = BuildInitialPartyTraits(
                    kingdom,
                    party.Ideology,
                    party.Radicalism,
                    party.Strategy,
                    party.ForeignStance,
                    party.Id,
                    false
                );

                SetKingdomStringData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitsPrefix,
                        party.Slot
                    ),
                    SerializePartyTraits(traits)
                );

                int year = GetWorldYearSafe();
                SetKingdomIntData(
                    kingdom,
                    PartySlotKey(
                        PartyV2TraitLastYearPrefix,
                        party.Slot
                    ),
                    year
                );
            }

            return traits;
        }

        private static void SavePartyTraits(
            Kingdom kingdom,
            PoliticalParty party
        )
        {
            if (
                kingdom == null ||
                party == null ||
                party.Slot < 0
            )
            {
                return;
            }

            SetKingdomStringData(
                kingdom,
                PartySlotKey(
                    PartyV2TraitsPrefix,
                    party.Slot
                ),
                SerializePartyTraits(party.Traits)
            );
        }

        private static string GetPartyTraitLocalizedName(
            string trait
        )
        {
            if (!IsValidPartyTrait(trait))
            {
                return trait ?? "";
            }

            string key = "ukiol_party_trait_" + trait;
            string localized = LM.Get(key);

            return
                string.IsNullOrEmpty(localized) ||
                localized == key
                    ? trait
                    : localized;
        }

        private static string FormatPartyTraits(
            List<string> traits
        )
        {
            if (traits == null || traits.Count == 0)
            {
                return "";
            }

            List<string> names = new List<string>();

            for (int i = 0; i < traits.Count; i++)
            {
                names.Add(
                    GetPartyTraitLocalizedName(
                        traits[i]
                    )
                );
            }

            return string.Join(" · ", names.ToArray());
        }

        private static int GetPartyTraitSupportModifier(
            Kingdom kingdom,
            PoliticalParty party,
            int movementRadicalism
        )
        {
            if (party == null)
            {
                return 0;
            }

            int modifier = 0;
            int stability = GetNationalStability(kingdom);

            if (HasPartyTrait(party, PartyTraitMass))
            {
                modifier += 8;
            }

            if (HasPartyTrait(party, PartyTraitElite))
            {
                modifier -= 3;
            }

            if (HasPartyTrait(party, PartyTraitDisciplined))
            {
                modifier += 6;
            }

            if (HasPartyTrait(party, PartyTraitFactional))
            {
                modifier -= 7;
            }

            if (HasPartyTrait(party, PartyTraitReformist))
            {
                modifier += stability >= 55 ? 7 : -2;
            }

            if (HasPartyTrait(party, PartyTraitPopulist))
            {
                modifier += stability < 50
                    ? Math.Min(14, (50 - stability) / 3 + 4)
                    : 1;
            }

            if (HasPartyTrait(party, PartyTraitRevolutionary))
            {
                modifier += movementRadicalism >= 60
                    ? 9
                    : -5;
            }

            if (HasPartyTrait(party, PartyTraitMilitarized))
            {
                modifier +=
                    GetKingdomCourse(kingdom) ==
                        MilitaristTraitId
                        ? 10
                        : -2;
            }

            if (HasPartyTrait(party, PartyTraitCorrupt))
            {
                modifier -= 14;
            }

            if (HasPartyTrait(party, PartyTraitSplintered))
            {
                modifier -= 10;
            }

            return modifier;
        }

        private static int GetPartySplitSupportThreshold(
            Kingdom kingdom,
            List<PoliticalParty> parties
        )
        {
            int threshold = PartySplitSupportThreshold;

            // dev12: simultaneous pressure for reform and radicalization means
            // that the same ideological family is being pulled in opposite
            // directions, so factional parties split more easily.
            if (kingdom != null)
            {
                int reform = GetKingdomIntData(
                    kingdom,
                    IdeologyReformPressureDataKey,
                    0
                );
                int radical = GetKingdomIntData(
                    kingdom,
                    IdeologyRadicalizationPressureDataKey,
                    0
                );

                if (Math.Min(reform, radical) >= 55)
                {
                    threshold -= 7;
                }
                else if (Math.Min(reform, radical) >= 40)
                {
                    threshold -= 4;
                }
            }

            if (parties == null)
            {
                return threshold;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                if (HasPartyTrait(party, PartyTraitFactional))
                {
                    threshold -= 4;
                }

                if (HasPartyTrait(party, PartyTraitRevolutionary))
                {
                    threshold -= 2;
                }

                if (HasPartyTrait(party, PartyTraitDisciplined))
                {
                    threshold += 4;
                }

                if (HasPartyTrait(party, PartyTraitSplintered))
                {
                    threshold += 5;
                }
            }

            return ClampInt(threshold, 28, 55);
        }

        private static int GetPartySplitCooldownModifier(
            List<PoliticalParty> parties
        )
        {
            int modifier = 0;

            if (parties == null)
            {
                return modifier;
            }

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];

                if (HasPartyTrait(party, PartyTraitFactional))
                {
                    modifier -= 2;
                }

                if (HasPartyTrait(party, PartyTraitDisciplined))
                {
                    modifier += 3;
                }

                if (HasPartyTrait(party, PartyTraitSplintered))
                {
                    modifier += 3;
                }
            }

            return ClampInt(modifier, -4, 8);
        }

    }
}
