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
    public partial class Main : BasicMod<Main>
    {
        private const string TraitGroupId = "ukiol_politics";

        private const string ReformerTraitId = "ukiol_reformer";
        private const string MilitaristTraitId = "ukiol_militarist";
        private const string DiplomatTraitId = "ukiol_diplomat";

        private const string ReformerPowerId = "ukiol_give_reformer";
        private const string MilitaristPowerId = "ukiol_give_militarist";
        private const string DiplomatPowerId = "ukiol_give_diplomat";

        private const string ReformerCoursePowerId = "ukiol_set_course_reformer";
        private const string MilitaristCoursePowerId = "ukiol_set_course_militarist";
        private const string DiplomatCoursePowerId = "ukiol_set_course_diplomat";

        private const string StabilizeCityPowerId = "ukiol_stabilize_city";
        private const string DestabilizeCityPowerId = "ukiol_destabilize_city";

        // v1.6.0-dev5: direct sandbox controls for the existing dev12
        // ideology-evolution pressure system. These do not invent a second
        // radicalization model; they modify the same persisted values used by
        // automatic reform/radicalization and party-split calculations.
        private const string EncourageReformPowerId = "ukiol_encourage_reform";
        private const string IncreaseRadicalizationPowerId = "ukiol_increase_radicalization";
        private const string ReduceRadicalizationPowerId = "ukiol_reduce_radicalization";
        private const string PoliticalOverviewPowerId = "ukiol_open_political_overview";
        private const string PoliticalOverviewWindowId = "ukiol_political_overview_window";


        // v1.5.0-dev1: persistent forms of government. These describe the
        // political structure of a kingdom and are intentionally separate
        // from ideology: the same ideology may support different regimes.
        private const string GovernmentAbsoluteMonarchyId = "ukiol_government_absolute_monarchy";
        private const string GovernmentConstitutionalMonarchyId = "ukiol_government_constitutional_monarchy";
        private const string GovernmentParliamentaryRepublicId = "ukiol_government_parliamentary_republic";
        private const string GovernmentPresidentialRepublicId = "ukiol_government_presidential_republic";
        private const string GovernmentOnePartyStateId = "ukiol_government_one_party_state";
        private const string GovernmentMilitaryDictatorshipId = "ukiol_government_military_dictatorship";
        private const string GovernmentCouncilRepublicId = "ukiol_government_council_republic";
        private const string GovernmentOligarchyId = "ukiol_government_oligarchy";

        private const string PartyRenameWindowId = "ukiol_party_rename_window";

        private const string StateCourseDataKey = "ukiol_state_course";
        private const string NationalStabilityDataKey = "ukiol_national_stability";
        private const string LocalStabilityDataKey = "ukiol_local_stability";
        private const string LocalStabilityOwnerDataKey = "ukiol_local_stability_owner";
        private const string LastRulerIdentityDataKey = "ukiol_last_ruler_identity";

        // v1.5.0-dev1 government persistence. Candidate/pressure prevent a
        // kingdom from changing regime every simulation tick when politics
        // briefly fluctuates around a threshold.
        private const string GovernmentFormDataKey = "ukiol_government_form";
        private const string GovernmentCandidateDataKey = "ukiol_government_candidate";
        private const string GovernmentPressureDataKey = "ukiol_government_pressure";
        private const string GovernmentSinceYearDataKey = "ukiol_government_since_year";
        private const string CustomGovernmentFormDataKey = "ukiol_custom_government_form";
        private const int GovernmentEvolutionThreshold = 4;

        // v1.5.0-dev2: persistent election cycle. Election state is stored on
        // the kingdom so saving/loading does not reset terms or the incumbent
        // party. The compact history feeds the native Politics/History page.
        private const string ElectionGovernmentFormDataKey = "ukiol_election_government_form";
        private const string ElectionLastYearDataKey = "ukiol_election_last_year";
        private const string ElectionNextYearDataKey = "ukiol_election_next_year";
        private const string ElectionRulingPartyIdDataKey = "ukiol_election_ruling_party_id";
        private const string ElectionRulingPartyNameDataKey = "ukiol_election_ruling_party_name";
        private const string ElectionRulingPartyIdeologyDataKey = "ukiol_election_ruling_party_ideology";
        private const string ElectionRulingPartySupportDataKey = "ukiol_election_ruling_party_support";
        private const string ElectionHistoryDataKey = "ukiol_election_history";
        private const int MaxElectionHistoryEntries = 12;

        // v1.5.0-dev4: political systems are deliberately separate from
        // government form. A Council Republic can be a Soviet system or a
        // decentralized anarchist federation, while a one-party state uses
        // internal party politics instead of competitive national elections.
        private const string PoliticalSystemDataKey = "ukiol_political_system";
        private const string PoliticalSystemCompetitiveId = "ukiol_political_system_competitive";
        private const string PoliticalSystemOnePartyId = "ukiol_political_system_one_party";
        private const string PoliticalSystemSovietId = "ukiol_political_system_soviet";
        private const string PoliticalSystemSovietOnePartyId = "ukiol_political_system_soviet_one_party";
        private const string PoliticalSystemNonElectoralId = "ukiol_political_system_non_electoral";
        private const string PoliticalSystemDecentralizedId = "ukiol_political_system_decentralized";

        private const string CouncilLastYearDataKey = "ukiol_council_last_year";
        private const string CouncilNextYearDataKey = "ukiol_council_next_year";
        private const string CouncilDelegateCountDataKey = "ukiol_council_delegate_count";
        private const string CouncilDelegateIdsDataKey = "ukiol_council_delegate_ids";
        private const string CouncilDelegateNamesDataKey = "ukiol_council_delegate_names";
        private const string CouncilDelegateCityNamesDataKey = "ukiol_council_delegate_city_names";
        private const string CouncilDominantPartyIdDataKey = "ukiol_council_dominant_party_id";
        private const string CouncilDominantPartyNameDataKey = "ukiol_council_dominant_party_name";
        private const string CouncilDominantPartyIdeologyDataKey = "ukiol_council_dominant_party_ideology";
        private const string CouncilDominantPartySupportDataKey = "ukiol_council_dominant_party_support";
        private const int CouncilTermYears = 3;

        // v1.5.0-dev5: real party leadership. The party congress now elects
        // concrete living actors to the Central Committee. A smaller
        // Presidium/Politburo is selected from that committee and the ruling
        // party leader is synchronized with the elected General Secretary.
        // Internal currents are deliberately party factions, not new parties.
        private const string PartyCongressLastYearDataKey = "ukiol_party_congress_last_year";
        private const string PartyCongressNextYearDataKey = "ukiol_party_congress_next_year";
        private const string CentralCommitteeSizeDataKey = "ukiol_central_committee_size";
        private const string CentralCommitteeMemberIdsDataKey = "ukiol_central_committee_member_ids";
        private const string CentralCommitteeMemberNamesDataKey = "ukiol_central_committee_member_names";
        private const string CentralCommitteeMemberCurrentsDataKey = "ukiol_central_committee_member_currents";
        private const string PolitburoMemberIdsDataKey = "ukiol_politburo_member_ids";
        private const string PolitburoMemberNamesDataKey = "ukiol_politburo_member_names";
        private const string GeneralSecretaryIdentityDataKey = "ukiol_general_secretary_id";
        private const string GeneralSecretaryNameDataKey = "ukiol_general_secretary_name";
        private const string GeneralSecretaryPartyIdDataKey = "ukiol_general_secretary_party_id";
        private const string PartyLeadershipLastCheckYearDataKey = "ukiol_party_leadership_last_check_year";
        private const string PartyLeadershipSchemaVersionDataKey = "ukiol_party_leadership_schema";
        private const string PartyCurrentOrthodoxId = "orthodox";
        private const string PartyCurrentReformistId = "reformist";
        private const string PartyCurrentMilitaristId = "militarist";
        private const string PartyCurrentNationalId = "national";
        private const char PartyLeadershipListSeparator = '\u001F';
        private const int PartyCongressIntervalYears = 5;
        private const int PartyLeadershipSchemaVersion = 1;

        // v1.5.0-dev6.1: government leadership and succession. These are
        // Political World offices layered on top of WorldBox's technical
        // kingdom.king. We deliberately do not replace the vanilla king
        // object yet; doing so without a stable game API could break city,
        // clan and diplomacy internals.
        private const string GovernmentLeadershipSchemaDataKey = "ukiol_government_leadership_schema";
        private const string HeadOfStateIdentityDataKey = "ukiol_head_of_state_id";
        private const string HeadOfStateNameDataKey = "ukiol_head_of_state_name";
        private const string HeadOfStateTitleDataKey = "ukiol_head_of_state_title";
        private const string HeadOfStateSinceYearDataKey = "ukiol_head_of_state_since_year";
        private const string HeadOfGovernmentIdentityDataKey = "ukiol_head_of_government_id";
        private const string HeadOfGovernmentNameDataKey = "ukiol_head_of_government_name";
        private const string HeadOfGovernmentTitleDataKey = "ukiol_head_of_government_title";
        private const string HeadOfGovernmentSinceYearDataKey = "ukiol_head_of_government_since_year";
        private const string LeadershipCrisisDataKey = "ukiol_leadership_crisis";
        private const string LeadershipCrisisSinceYearDataKey = "ukiol_leadership_crisis_since_year";
        private const string LeadershipHistoryDataKey = "ukiol_leadership_history";
        private const int GovernmentLeadershipSchemaVersion = 1;
        private const int MaxLeadershipHistoryEntries = 16;

        private const string LeadershipTitleMonarch = "monarch";
        private const string LeadershipTitlePresident = "president";
        private const string LeadershipTitlePrimeMinister = "prime_minister";
        private const string LeadershipTitleCouncilChair = "council_chair";
        private const string LeadershipTitleCouncilGovernmentChair = "council_government_chair";
        private const string LeadershipTitleGeneralSecretary = "general_secretary";
        private const string LeadershipTitlePartyLeader = "party_leader";
        private const string LeadershipTitleMilitaryRuler = "military_ruler";
        private const string LeadershipTitleOligarchicChair = "oligarchic_chair";
        private const string LeadershipTitleActingPresident = "acting_president";
        private const string LeadershipTitleActingPrimeMinister = "acting_prime_minister";
        private const string LeadershipRoleState = "state";
        private const string LeadershipRoleGovernment = "government";



        // v1.5.0-dev3: race-aware leader longevity foundation. These values
        // deliberately do not turn a ruler into an "eternal leader" yet.
        // They persist the facts future personality-cult / succession systems
        // need without assuming that every race has a human lifespan.
        private const string LeaderTrackedIdentityDataKey = "ukiol_leader_track_identity";
        private const string LeaderTrackedNameDataKey = "ukiol_leader_track_name";
        private const string LeaderReignStartYearDataKey = "ukiol_leader_reign_start_year";
        private const string LeaderTenureYearsDataKey = "ukiol_leader_tenure_years";
        private const string LeaderRaceIdDataKey = "ukiol_leader_race_id";
        private const string LeaderAgeYearsDataKey = "ukiol_leader_age_years";
        private const string LeaderExpectedLifespanDataKey = "ukiol_leader_expected_lifespan";
        private const string LeaderSubjectLifespanDataKey = "ukiol_leader_subject_lifespan";
        private const string LeaderAgeRatioPermilleDataKey = "ukiol_leader_age_ratio_permille";
        private const string LeaderTenureRatioPermilleDataKey = "ukiol_leader_tenure_ratio_permille";
        private const string LeaderSubjectGenerationPermilleDataKey = "ukiol_leader_subject_generation_permille";
        private const string LeaderImmortalTraitDataKey = "ukiol_leader_trait_immortal";
        private const string LeaderLongLivedTraitDataKey = "ukiol_leader_trait_long_lived";
        private const string LeaderFoundationLastYearDataKey = "ukiol_leader_foundation_last_year";
        private const int LeaderSubjectLifespanSampleLimit = 120;

        // Hidden portrait easter egg. The deliberately generic resource name
        // keeps it out of normal UI discovery; it has no gameplay effect.
        private const string HiddenPortraitResourcePath = "ukiol/secret/portrait_07";
        private const int HiddenPortraitClickCount = 7;

        // Политические движения v1.2.1. Движения вычисляются на основе
        // реальной поддержки идеологий населения и сохраняют радикальность.
        private const string MovementInitializedPrefix = "ukiol_movement_initialized_";
        private const string MovementActivePrefix = "ukiol_movement_active_";
        private const string MovementRadicalismPrefix = "ukiol_movement_radicalism_";
        private const string MovementRadicalizedPrefix = "ukiol_movement_radicalized_";
        private const string MovementLeaderNamePrefix = "ukiol_movement_leader_name_";
        private const int MovementFormationThreshold = 18;
        private const int MovementDissolutionThreshold = 10;
        private const int MovementRadicalThreshold = 65;
        private const int MovementDeradicalizeThreshold = 45;

        // Политические партии.
        // Legacy-ключи v1.3.3 сохраняются только для автоматической миграции
        // старых сохранений и обратной совместимости с уже созданными мирами.
        private const string PartyActivePrefix = "ukiol_party_active_";
        private const string PartyNameVariantPrefix = "ukiol_party_name_variant_";
        private const string PartyLeaderNamePrefix = "ukiol_party_leader_name_";
        private const string PartyFoundedYearPrefix = "ukiol_party_founded_year_";

        // v1.3.7: полноценная сущность PoliticalParty. Партии теперь хранятся
        // отдельными слотами, поэтому одна идеология может иметь несколько
        // конкурирующих организаций с собственными лидерами и профилями.
        private const string PartySchemaVersionDataKey = "ukiol_party_schema_version";
        private const string PartySlotCountDataKey = "ukiol_party_slot_count";
        private const string PartySerialDataKey = "ukiol_party_serial";
        private const string PartyV2ActivePrefix = "ukiol_party2_active_";
        private const string PartyV2IdPrefix = "ukiol_party2_id_";
        private const string PartyV2IdeologyPrefix = "ukiol_party2_ideology_";
        private const string PartyV2NameVariantPrefix = "ukiol_party2_name_variant_";
        private const string PartyV2LeaderIdentityPrefix = "ukiol_party2_leader_id_";
        private const string PartyV2LeaderNamePrefix = "ukiol_party2_leader_name_";
        private const string PartyV2FounderIdentityPrefix = "ukiol_party2_founder_id_";
        private const string PartyV2FounderNamePrefix = "ukiol_party2_founder_name_";
        private const string PartyV2FoundedYearPrefix = "ukiol_party2_founded_year_";
        private const string PartyV2RadicalismPrefix = "ukiol_party2_radicalism_";
        private const string PartyV2PositionPrefix = "ukiol_party2_position_";
        private const string PartyV2StrategyPrefix = "ukiol_party2_strategy_";
        private const string PartyV2ForeignStancePrefix = "ukiol_party2_foreign_";
        private const string PartyV2SupportBiasPrefix = "ukiol_party2_support_bias_";
        private const string PartyV2TraitsPrefix = "ukiol_party2_traits_";
        private const string PartyV2TraitLastYearPrefix = "ukiol_party2_trait_last_year_";
        private const string PartyV2TraitLastSupportPrefix = "ukiol_party2_trait_last_support_";

        // v1.3.9: persistent regional party identity and local support.
        private const string PartyV2OriginCityIdPrefix = "ukiol_party2_origin_city_id_";
        private const string PartyV2OriginCityNamePrefix = "ukiol_party2_origin_city_name_";
        private const string PartyV2ParentPartyIdPrefix = "ukiol_party2_parent_party_id_";
        // v1.4.0-dev1: keep the parent name as historical data as well as
        // the parent ID. Parent parties can disappear or live in another
        // successor kingdom, so the profile must not depend on a live lookup.
        private const string PartyV2ParentPartyNamePrefix = "ukiol_party2_parent_party_name_";

        // v1.4.0-dev3: compact persistent party biography. History is kept
        // per party slot and capped, so old worlds do not grow without limit.
        private const string PartyV2HistoryPrefix = "ukiol_party2_history_";
        private const string PartyV2LeadingStatePrefix = "ukiol_party2_leading_state_";
        private const string PartyV2LeadingInitPrefix = "ukiol_party2_leading_init_";
        private const int MaxPartyHistoryEntries = 25;

        // v1.4.0-dev4: compact national-support timeline. A truthful
        // snapshot is persisted every five world years; only the latest
        // twelve are retained (roughly sixty years).
        private const string PartyV2SupportHistoryPrefix =
            "ukiol_party2_support_history_";
        private const string PartyV2SupportHistoryLastYearPrefix =
            "ukiol_party2_support_history_last_year_";
        private const int PartySupportHistoryIntervalYears = 5;
        private const int MaxPartySupportHistoryEntries = 12;

        // v1.4.0-dev5: each party owns a persistent visual identity color.
        // We store a compact variant index instead of raw RGB so old saves
        // remain small and the palette can stay visually related to ideology.
        private const string PartyV2ColorSeedPrefix =
            "ukiol_party2_color_seed_";
        // v1.6.0-dev6: optional player-defined party display name.
        private const string PartyV2CustomNamePrefix =
            "ukiol_party2_custom_name_";
        private const int PartyCustomNameMaxLength = 48;
        private const int PartyColorVariantCount = 12;

        private const string PartyHistoryFounded = "founded";
        private const string PartyHistoryFirstLeader = "first_leader";
        private const string PartyHistoryLeaderChanged = "leader_changed";
        private const string PartyHistorySplitFrom = "split_from";
        private const string PartyHistorySplitChild = "split_child";
        private const string PartyHistoryTraitGained = "trait_gained";
        private const string PartyHistoryTraitLost = "trait_lost";
        private const string PartyHistoryBecameLeading = "became_leading";
        private const string PartyHistoryLostLeading = "lost_leading";
        private const string PartyHistorySuccession = "succession";
        private const string PartyHistoryTrackingStarted = "tracking_started";

        private const string LocalPartySupportPrefix = "ukiol_local_party_support_";
        private const string LocalPartySupportInitPrefix = "ukiol_local_party_support_init_";

        // v1.3.9: Political Memory is a slow historical layer on top of
        // citizen ideologies. Citizen ideology remains the source of truth.
        private const string PoliticalMemorySchemaDataKey = "ukiol_political_memory_schema";
        private const string PoliticalMemoryLastYearDataKey = "ukiol_political_memory_last_year";
        private const string PoliticalMemoryOwnerDataKey = "ukiol_political_memory_owner";
        private const string PoliticalMemoryOwnerSinceDataKey = "ukiol_political_memory_owner_since";
        private const string PoliticalMemoryRegimeDataKey = "ukiol_political_memory_regime";
        private const string PoliticalMemoryRegimeSinceDataKey = "ukiol_political_memory_regime_since";
        private const string PoliticalMemoryPrefix = "ukiol_political_memory_";
        private const int PoliticalMemorySchemaVersion = 1;
        // v1.3.9.4 final balance: keep history and regional support gradual.
        private const int PoliticalMemoryMaxYearsPerTick = 4;
        private const int LocalPartySupportStep = 2;
        private const int LocalPartyBaseOrganization = 85;
        private const int LocalPartyOriginBonus = 24;
        private const int LocalPartyRegionalAffinityRange = 10;
        private const int PartyStrongholdMinimumSupport = 15;

        // v1.3.9.2: Political Succession. Newly independent states inherit
        // the political character of the cities that actually separated,
        // instead of receiving a nearly random ideology and fresh parties.
        private const string SuccessionSchemaDataKey = "ukiol_succession_schema";
        private const string SuccessionParentKingdomDataKey = "ukiol_succession_parent_kingdom";
        private const string SuccessionOriginCityDataKey = "ukiol_succession_origin_city";
        private const string SuccessionYearDataKey = "ukiol_succession_year";
        private const string SuccessionIdeologyDataKey = "ukiol_succession_ideology";
        private const int SuccessionSchemaVersion = 1;
        private const int SuccessionPartyMinimumLocalSupport = 12;
        private const int SuccessionMaxInheritedParties = 5;

        // v1.3.9.3: political events use the native WorldBox HistoryHud/WorldLog
        // pipeline, exactly like king deaths, wars and other vanilla history messages.
        private const string PoliticalWorldLogMessageId =
            "ukiol_worldlog_political_event";

        private const string PartyLastSplitYearPrefix = "ukiol_party_last_split_year_";

        // v1.3.8: traits are stored as a small comma-separated set per party.
        // We deliberately keep PartySchemaVersion at 2: changing it would run
        // the old v1.3.7 migration again and could reset existing party slots.
        private const int PartySchemaVersion = 2;
        private const int MaxPartyTraits = 3;
        private const int PartyTraitEvolutionIntervalYears = 5;

        private const string PartyTraitMass = "mass";
        private const string PartyTraitElite = "elite";
        private const string PartyTraitDisciplined = "disciplined";
        private const string PartyTraitFactional = "factional";
        private const string PartyTraitReformist = "reformist";
        private const string PartyTraitPopulist = "populist";
        private const string PartyTraitRevolutionary = "revolutionary";
        private const string PartyTraitMilitarized = "militarized";
        private const string PartyTraitCorrupt = "corrupt";
        private const string PartyTraitSplintered = "splintered";
        private const int PartyFormationThreshold = 18;
        private const int PartyDissolutionThreshold = 8;
        private const int PartySplitSupportThreshold = 40;
        private const int PartySplitStrongSupportThreshold = 64;
        private const int PartySplitMinAgeYears = 10;
        private const int PartySplitCooldownYears = 14;
        private const int MaxPoliticalParties = 18;
        private const int MaxPoliticalPartiesPerIdeology = 3;

        // Политические кризисы v1.3.0. Одновременно в государстве
        // активен только один крупный кризис, связанный с главным движением.
        private const string CrisisInitializedDataKey = "ukiol_crisis_initialized";
        private const string CrisisActiveDataKey = "ukiol_crisis_active";
        private const string CrisisIdeologyDataKey = "ukiol_crisis_ideology";
        private const string CrisisDemandDataKey = "ukiol_crisis_demand";
        private const string CrisisPressureDataKey = "ukiol_crisis_pressure";
        private const string CrisisStageDataKey = "ukiol_crisis_stage";
        private const string CrisisAgeDataKey = "ukiol_crisis_age";
        private const string CrisisResponseMadeDataKey = "ukiol_crisis_response_made";
        private const string CrisisCooldownDataKey = "ukiol_crisis_cooldown";

        private const string CrisisDemandReformsId = "reforms";
        private const string CrisisDemandMilitarizationId = "militarization";
        private const string CrisisDemandIdeologyId = "ideology";

        private const int CrisisFormationSupportThreshold = 24;
        private const int CrisisFormationRadicalismThreshold = 55;
        private const int CrisisFormationStabilityCeiling = 60;
        private const int CrisisSeverePressureThreshold = 78;
        private const int CrisisRegimeChangePressureThreshold = 88;
        private const int CrisisRegimeChangeMinAge = 5;
        private const int CrisisCooldownTicks = 8;

        private const string ReformerIconPath = "ukiol/icons/reformer";
        private const string MilitaristIconPath = "ukiol/icons/militarist";
        private const string DiplomatIconPath = "ukiol/icons/diplomat";


        // Native Politics UI icons v1.3.5. These are intentionally
        // separate from ideology icons so every future native section
        // has its own stable visual identity.
        private const string PoliticsIconPath = "ukiol/icons/politics";
        private const string OverviewIconPath = "ukiol/icons/overview";
        private const string PartiesIconPath = "ukiol/icons/parties";
        private const string SocietyIconPath = "ukiol/icons/society";
        private const string LawsIconPath = "ukiol/icons/laws";
        private const string HistoryIconPath = "ukiol/icons/history";

        private const float PoliticsCheckInterval = 3f;
        private const float EconomyTickInterval = 10f;
        private const float StabilityTickInterval = 10f;

        // dev17 performance pass: UI discovery and real-time diplomatic
        // sequences do not need to run every rendered frame. The native
        // Politics tab is persistent once injected, so a sub-second scan is
        // more than responsive enough while avoiding Resources-wide object
        // searches dozens of times per second.
        private const float NativeUiScanIntervalSearching = 0.20f;
        private const float NativeUiScanIntervalInstalled = 0.75f;
        private const float KingdomWindowCacheRefreshInterval = 6f;
        private const float RuntimeDiplomacyUpdateInterval = 0.10f;
        private const float RuntimeSummitUpdateInterval = 0.20f;
        private const float ArmyLimitModifierCacheSeconds = 1.0f;


        private const int DefaultNationalStability = 50;
        private const int DefaultLocalStability = 50;
        private const int NationalStabilityStep = 2;
        private const int LocalStabilityStep = 3;

        private const int RebellionThreshold = 15;
        private const int RebellionJoinThreshold = 18;
        private const float RebellionAttemptCooldown = 20f;

        private const int ReformerGoldPerCity = 1;
        private const int ReformerBreadPerCity = 2;
        private const int MilitaristGoldPerCity = -1;
        private const int DiplomatGoldPerCity = 2;

        private const float ReformerArmyLimitBonus = 0.03f;
        private const float MilitaristArmyLimitBonus = 0.15f;
        private const float DiplomatArmyLimitPenalty = 0.05f;

        private const float MilitaristSoldierDamageBonus = 5f;
        private const float MilitaristSoldierSpeedBonus = 2f;
        private const float MilitaristSoldierArmorBonus = 2f;

        private static readonly BindingFlags MemberFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;


        private static readonly Dictionary<Kingdom, string>
            LastKnownKingdomCourses =
                new Dictionary<Kingdom, string>();

        private static readonly Dictionary<City, float>
            NextRebellionAttemptTime =
                new Dictionary<City, float>();

        private static PowersTab _politicsTab;
        private static Harmony _harmony;
        private static bool _armyLimitPatchInstalled;
        private static bool _soldierStatsPatchInstalled;
        private static bool _kingdomWindowPatchInstalled;
        private static bool _cityWindowPatchInstalled;
        private static bool _loyaltyBridgePatchInstalled;
        // Native kingdom Politics tab state (v1.3.6).
        // The game currently exposes the existing kingdom tabs but does not
        // provide a public "add kingdom sub-tab" helper, so we clone one of
        // the vanilla side-tab buttons and reuse the vanilla stats scroll.
        private static readonly HashSet<int> _kingdomPoliticsWindows =
            new HashSet<int>();
        private static readonly Dictionary<int, int>
            _kingdomPoliticsPages = new Dictionary<int, int>();
        // v1.4.0-dev1: a party profile is an internal view of the Parties
        // page, not a separate popup or a new top-level Politics tab.
        private static readonly Dictionary<int, string>
            _kingdomPoliticsSelectedPartyIds =
                new Dictionary<int, string>();
        private static readonly Dictionary<int, string>
            _kingdomPoliticsSelectedPartyKingdomIds =
                new Dictionary<int, string>();
        private static readonly HashSet<int> _kingdomTabButtonsHooked =
            new HashSet<int>();
        private static readonly HashSet<int> _kingdomPoliticsUiScanLogged =
            new HashSet<int>();
        private static bool _openingNativeKingdomPolitics;
        private static Button _kingdomPoliticsOverlayButton;

        // v1.4.0-dev4: accepted true native KingdomWindow tab integration.
        // WorldBox already has a real WindowMetaTab system. Keep references
        // to our cloned vanilla tab so the game itself controls its selected
        // sprite, layout, show/hide lifecycle and tab-content switching.
        private static WindowMetaTab _kingdomPoliticsTrueNativeTab;
        private static WindowMetaTabButtonsContainer _kingdomPoliticsTrueNativeContainer;
        private static int _kingdomPoliticsTrueNativeWindowId = -1;

        // v1.4.0-dev3.2.1: the Politics tab lives on its own overlay canvas,
        // so it must not chase the KingdomWindow while the vanilla opening /
        // closing animation is still moving. Track the screen rectangle and
        // only show the tab after the window has been stationary briefly.
        private static int _politicsStableWindowId = -1;
        private static Rect _politicsLastWindowScreenRect;
        private static bool _politicsHasLastWindowScreenRect;
        private static float _politicsWindowStableSince = -1f;


        // v1.3.7.2: while the embedded Politics panel is open, the visible
        // vanilla KingdomWindow scrollbar is temporarily bridged to our
        // inner Politics ScrollRect. This makes dragging the red scrollbar
        // behave exactly like mouse-wheel scrolling.
        private static readonly Dictionary<int, ScrollRect>
            _kingdomPoliticsHostScrolls =
                new Dictionary<int, ScrollRect>();
        private static readonly Dictionary<int, bool>
            _kingdomPoliticsHostScrollEnabled =
                new Dictionary<int, bool>();

        private const string NativePoliticsButtonName =
            "ukiol_native_kingdom_politics_tab";
        private const string NativePoliticsPanelName =
            "ukiol_native_kingdom_politics_panel";
        private const string NativePoliticsPageBarName =
            "ukiol_native_kingdom_politics_pages";
        private const int NativePoliticsPageOverview = 0;
        private const int NativePoliticsPageParties = 1;
        private const int NativePoliticsPageSociety = 2;
        private const int NativePoliticsPageLaws = 3;
        private const int NativePoliticsPageHistory = 4;

        // v1.6.0-dev9 RC: this release candidate is audited against the
        // user's current Steam build. NeoModLoader exposes targetGameBuild in
        // mod.json but does not enforce it yet, so keep a lightweight runtime
        // diagnostic as well. It warns only; it never blocks loading.
        private const string TargetWorldBoxVersion = "0.51.2";
        private const string TargetWorldBoxBuildCode = "719";
        private const string TargetWorldBoxGitCode = "build-719@5dec";

        private float _nextNativeUiScanTime;
        private float _nextPoliticsCheckTime;
        private float _nextEconomyTickTime;
        private float _nextStabilityTickTime;
        private float _nextRuntimeDiplomacyUpdateTime;
        private float _nextRuntimeSummitUpdateTime;
        private int _politicalPipelineStage = -1;

        private static KingdomWindow[] _cachedKingdomWindows =
            new KingdomWindow[0];
        private static float _nextKingdomWindowCacheRefreshTime;

        // Hot Harmony callbacks (army limit / actor stat rebuild) used to
        // rediscover kingdom fields and recalculate ideology/bloc modifiers
        // on every call. Keep tiny short-lived caches instead.
        private static readonly Dictionary<Type, FieldInfo>
            KingdomFieldAccessorCache = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, PropertyInfo>
            KingdomPropertyAccessorCache = new Dictionary<Type, PropertyInfo>();
        private static readonly HashSet<Type> KingdomAccessorMissingCache =
            new HashSet<Type>();
        private static readonly Dictionary<Kingdom, string>
            FastKingdomCourseCache = new Dictionary<Kingdom, string>();
        private static readonly Dictionary<Kingdom, float>
            CachedArmyLimitPoliticalDelta = new Dictionary<Kingdom, float>();
        private static readonly Dictionary<Kingdom, float>
            CachedArmyLimitPoliticalDeltaUntil = new Dictionary<Kingdom, float>();
    }
}
