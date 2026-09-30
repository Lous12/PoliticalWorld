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
        // Step 9D FAST: ideology IDs, persistence keys, runtime registry state and tuning constants moved from the legacy monolith unchanged.
        private const string MonarchismIdeologyId = "ukiol_ideology_monarchism";
        private const string ConservatismIdeologyId = "ukiol_ideology_conservatism";
        private const string LiberalismIdeologyId = "ukiol_ideology_liberalism";
        private const string DemocracyIdeologyId = "ukiol_ideology_democracy";
        private const string SocialismIdeologyId = "ukiol_ideology_socialism";
        private const string CommunismIdeologyId = "ukiol_ideology_communism";
        private const string FascismIdeologyId = "ukiol_ideology_fascism";
        private const string AnarchismIdeologyId = "ukiol_ideology_anarchism";
        private const string SyndicalismIdeologyId = "ukiol_ideology_syndicalism";

        // Производные идеологические течения v1.2.0.
        // Базовая идеология остаётся у населения, а государство
        // автоматически развивает одно из её течений.
        private const string AbsoluteMonarchyCurrentId = "ukiol_current_absolute_monarchy";
        private const string ConstitutionalMonarchyCurrentId = "ukiol_current_constitutional_monarchy";
        private const string TraditionalismCurrentId = "ukiol_current_traditionalism";
        private const string LiberalConservatismCurrentId = "ukiol_current_liberal_conservatism";
        private const string ClassicalLiberalismCurrentId = "ukiol_current_classical_liberalism";
        private const string SocialLiberalismCurrentId = "ukiol_current_social_liberalism";
        private const string ParliamentaryDemocracyCurrentId = "ukiol_current_parliamentary_democracy";
        private const string RadicalDemocracyCurrentId = "ukiol_current_radical_democracy";
        private const string SocialDemocracyCurrentId = "ukiol_current_social_democracy";
        private const string DemocraticSocialismCurrentId = "ukiol_current_democratic_socialism";
        private const string RevolutionarySocialismCurrentId = "ukiol_current_revolutionary_socialism";
        private const string CentralistCommunismCurrentId = "ukiol_current_centralist_communism";
        private const string CouncilCommunismCurrentId = "ukiol_current_council_communism";
        private const string CorporatistFascismCurrentId = "ukiol_current_corporatist_fascism";
        private const string RadicalFascismCurrentId = "ukiol_current_radical_fascism";
        private const string IndividualistAnarchismCurrentId = "ukiol_current_individualist_anarchism";
        private const string AnarchoCommunismCurrentId = "ukiol_current_anarcho_communism";
        private const string AnarchoSyndicalismCurrentId = "ukiol_current_anarcho_syndicalism";
        private const string IndustrialSyndicalismCurrentId = "ukiol_current_industrial_syndicalism";
        private const string RevolutionarySyndicalismCurrentId = "ukiol_current_revolutionary_syndicalism";


        // v1.5.0-dev10: Full Ideology Tree. Existing current IDs above are
        // preserved as stable branch IDs for old saves; the constants below
        // are deeper branches/leaves selected by the new current resolver.
        // Monarchism
        private const string EnlightenedAbsolutismCurrentId = "ukiol_current_enlightened_absolutism";
        private const string AutocraticMonarchismCurrentId = "ukiol_current_autocratic_monarchism";
        private const string PatrimonialMonarchyCurrentId = "ukiol_current_patrimonial_monarchy";
        private const string ParliamentaryMonarchyCurrentId = "ukiol_current_parliamentary_monarchy";
        private const string DualistMonarchyCurrentId = "ukiol_current_dualist_monarchy";
        private const string ElectiveMonarchyCurrentId = "ukiol_current_elective_monarchy";
        private const string AristocraticElectiveMonarchyCurrentId = "ukiol_current_aristocratic_elective_monarchy";
        private const string PopularElectiveMonarchyCurrentId = "ukiol_current_popular_elective_monarchy";
        // 1.10: fantasy/world-simulation branches. These are still currents,
        // so old root-ideology saves remain 100% compatible.
        private const string KhanismCurrentId = "ukiol_current_khanism";

        // Conservatism
        private const string ReactionaryConservatismCurrentId = "ukiol_current_reactionary_conservatism";
        private const string PaternalisticConservatismCurrentId = "ukiol_current_paternalistic_conservatism";
        private const string NationalConservatismCurrentId = "ukiol_current_national_conservatism";
        private const string FiscalConservatismCurrentId = "ukiol_current_fiscal_conservatism";
        private const string ProgressiveConservatismCurrentId = "ukiol_current_progressive_conservatism";
        private const string AuthoritarianConservatismCurrentId = "ukiol_current_authoritarian_conservatism";
        private const string OrderConservatismCurrentId = "ukiol_current_order_conservatism";
        private const string AgrarianismCurrentId = "ukiol_current_agrarianism";
        private const string TheocraticTraditionalismCurrentId = "ukiol_current_theocratic_traditionalism";

        // Liberalism
        private const string EconomicLiberalismCurrentId = "ukiol_current_economic_liberalism";
        private const string OrdoliberalismCurrentId = "ukiol_current_ordoliberalism";
        private const string LibertarianismCurrentId = "ukiol_current_libertarianism";
        private const string MinarchismCurrentId = "ukiol_current_minarchism";
        private const string ProgressiveLiberalismCurrentId = "ukiol_current_progressive_liberalism";
        private const string WelfareLiberalismCurrentId = "ukiol_current_welfare_liberalism";
        private const string NationalLiberalismCurrentId = "ukiol_current_national_liberalism";
        private const string CivicNationalLiberalismCurrentId = "ukiol_current_civic_national_liberalism";

        // Democracy
        private const string LiberalDemocracyCurrentId = "ukiol_current_liberal_democracy";
        private const string ConsensusDemocracyCurrentId = "ukiol_current_consensus_democracy";
        private const string DirectDemocracyCurrentId = "ukiol_current_direct_democracy";
        private const string ParticipatoryDemocracyCurrentId = "ukiol_current_participatory_democracy";
        private const string PresidentialDemocracyCurrentId = "ukiol_current_presidential_democracy";
        private const string ConstitutionalPresidentialismCurrentId = "ukiol_current_constitutional_presidentialism";
        private const string CouncilDemocracyCurrentId = "ukiol_current_council_democracy";
        private const string DelegativeCouncilDemocracyCurrentId = "ukiol_current_delegative_council_democracy";
        private const string TechnocraticDemocracyCurrentId = "ukiol_current_technocratic_democracy";
        private const string SylvanConcordCurrentId = "ukiol_current_sylvan_concord";

        // Socialism
        private const string ModerateSocialDemocracyCurrentId = "ukiol_current_moderate_social_democracy";
        private const string LeftSocialDemocracyCurrentId = "ukiol_current_left_social_democracy";
        private const string MarketSocialismCurrentId = "ukiol_current_market_socialism";
        private const string LibertarianSocialismCurrentId = "ukiol_current_libertarian_socialism";
        private const string GuildSocialismCurrentId = "ukiol_current_guild_socialism";
        private const string CooperativeSocialismCurrentId = "ukiol_current_cooperative_socialism";
        private const string MarxistSocialismCurrentId = "ukiol_current_marxist_socialism";
        private const string RevolutionaryDemocraticSocialismCurrentId = "ukiol_current_revolutionary_democratic_socialism";
        private const string UtopianSocialismCurrentId = "ukiol_current_utopian_socialism";

        // Communism
        private const string MarxismLeninismCurrentId = "ukiol_current_marxism_leninism";
        private const string OrthodoxMarxismLeninismCurrentId = "ukiol_current_orthodox_marxism_leninism";
        private const string ReformistMarxismLeninismCurrentId = "ukiol_current_reformist_marxism_leninism";
        private const string EurocommunismCurrentId = "ukiol_current_eurocommunism";
        private const string NationalCommunismCurrentId = "ukiol_current_national_communism";
        private const string MaoismCurrentId = "ukiol_current_maoism";
        private const string PartyCommunismCurrentId = "ukiol_current_party_communism";
        private const string WorkersCouncilCommunismCurrentId = "ukiol_current_workers_council_communism";
        private const string LeftCommunismCurrentId = "ukiol_current_left_communism";
        private const string LibertarianCommunismCurrentId = "ukiol_current_libertarian_communism";
        private const string CommunalCommunismCurrentId = "ukiol_current_communal_communism";

        // Fascism
        private const string StateCorporatismCurrentId = "ukiol_current_state_corporatism";
        private const string ClericalFascismCurrentId = "ukiol_current_clerical_fascism";
        private const string FalangismCurrentId = "ukiol_current_falangism";
        private const string NationalSocialismCurrentId = "ukiol_current_national_socialism";
        private const string TotalitarianFascismCurrentId = "ukiol_current_totalitarian_fascism";
        private const string IntegralFascismCurrentId = "ukiol_current_integral_fascism";
        private const string NationalSyndicalismFascistCurrentId = "ukiol_current_national_syndicalism_fascist";
        private const string StratocracyCurrentId = "ukiol_current_stratocracy";
        private const string IronOrderCurrentId = "ukiol_current_iron_order";
        private const string BurgundianSystemCurrentId = "ukiol_current_burgundian_system";

        // Anarchism
        private const string SocialAnarchismCurrentId = "ukiol_current_social_anarchism";
        private const string CollectivistAnarchismCurrentId = "ukiol_current_collectivist_anarchism";
        private const string EgoistAnarchismCurrentId = "ukiol_current_egoist_anarchism";
        private const string MutualismCurrentId = "ukiol_current_mutualism";
        private const string MarketAnarchismCurrentId = "ukiol_current_market_anarchism";
        private const string AnarchoCapitalismCurrentId = "ukiol_current_anarcho_capitalism";
        private const string AgorismCurrentId = "ukiol_current_agorism";
        private const string LeftMarketAnarchismCurrentId = "ukiol_current_left_market_anarchism";
        private const string GreenAnarchismCurrentId = "ukiol_current_green_anarchism";
        private const string EcoAnarchismCurrentId = "ukiol_current_eco_anarchism";
        private const string AnarchoPrimitivismCurrentId = "ukiol_current_anarcho_primitivism";
        private const string AnarchoPacifismCurrentId = "ukiol_current_anarcho_pacifism";
        private const string InsurrectionaryAnarchismCurrentId = "ukiol_current_insurrectionary_anarchism";

        // Syndicalism
        private const string ReformistSyndicalismCurrentId = "ukiol_current_reformist_syndicalism";
        private const string GuildSyndicalismCurrentId = "ukiol_current_guild_syndicalism";
        private const string CooperativeSyndicalismCurrentId = "ukiol_current_cooperative_syndicalism";
        private const string RevolutionaryUnionismCurrentId = "ukiol_current_revolutionary_unionism";
        private const string CouncilSyndicalismCurrentId = "ukiol_current_council_syndicalism";
        private const string DemocraticSyndicalismCurrentId = "ukiol_current_democratic_syndicalism";
        private const string ParliamentarySyndicalismCurrentId = "ukiol_current_parliamentary_syndicalism";
        private const string CooperativeCommonwealthCurrentId = "ukiol_current_cooperative_commonwealth";
        private const string ForgeSyndicalismCurrentId = "ukiol_current_forge_syndicalism";

        private const string MonarchismPowerId = "ukiol_set_ideology_monarchism";
        private const string ConservatismPowerId = "ukiol_set_ideology_conservatism";
        private const string LiberalismPowerId = "ukiol_set_ideology_liberalism";
        private const string DemocracyPowerId = "ukiol_set_ideology_democracy";
        private const string SocialismPowerId = "ukiol_set_ideology_socialism";
        private const string CommunismPowerId = "ukiol_set_ideology_communism";
        private const string FascismPowerId = "ukiol_set_ideology_fascism";
        private const string AnarchismPowerId = "ukiol_set_ideology_anarchism";
        private const string SyndicalismPowerId = "ukiol_set_ideology_syndicalism";

        // v1.6.0-dev1: direct city ideology editor. Unlike the state
        // ideology powers, these rewrite the local population's actual
        // citizen ideology distribution, so the existing party/support
        // simulation immediately reacts to the player's sandbox edit.
        private const string CityMonarchismPowerId = "ukiol_set_city_ideology_monarchism";
        private const string CityConservatismPowerId = "ukiol_set_city_ideology_conservatism";
        private const string CityLiberalismPowerId = "ukiol_set_city_ideology_liberalism";
        private const string CityDemocracyPowerId = "ukiol_set_city_ideology_democracy";
        private const string CitySocialismPowerId = "ukiol_set_city_ideology_socialism";
        private const string CityCommunismPowerId = "ukiol_set_city_ideology_communism";
        private const string CityFascismPowerId = "ukiol_set_city_ideology_fascism";
        private const string CityAnarchismPowerId = "ukiol_set_city_ideology_anarchism";
        private const string CitySyndicalismPowerId = "ukiol_set_city_ideology_syndicalism";

        // v1.6.0-dev3: city political identity. The toolbar keeps one
        // state editor and one settlement editor instead of 18 fixed root
        // ideology buttons. The editor window uses the full ideology registry.
        private const string StateIdeologyEditorPowerId = "ukiol_edit_state_ideology";
        private const string CityIdeologyEditorPowerId = "ukiol_edit_city_ideology";
        private const string IdeologyEditorWindowId = "ukiol_ideology_editor_window";

        private const string StateIdeologyDataKey = "ukiol_state_ideology";
        private const string StateIdeologyCurrentDataKey = "ukiol_state_ideology_current";
        private const string StateIdeologyCurrentCandidateDataKey = "ukiol_state_ideology_current_candidate";
        private const string StateIdeologyCurrentPressureDataKey = "ukiol_state_ideology_current_pressure";

        // v1.5.0-dev12: ideology evolution is no longer a direct jump to a
        // precomputed leaf. Reform and radicalization pressures are persisted
        // per kingdom and branches move through the registry step-by-step.
        private const string IdeologyReformPressureDataKey = "ukiol_ideology_reform_pressure";
        private const string IdeologyRadicalizationPressureDataKey = "ukiol_ideology_radicalization_pressure";
        private const string IdeologyEvolutionDirectionDataKey = "ukiol_ideology_evolution_direction";
        private const string IdeologyEvolutionLastSignalDataKey = "ukiol_ideology_evolution_last_signal";
        private const string IdeologyEvolutionLastSignalYearDataKey = "ukiol_ideology_evolution_last_signal_year";
        private const string IdeologyBranchSplitLastYearPrefix = "ukiol_ideology_branch_split_last_year_";
        private const int IdeologyEvolutionSignalCooldownYears = 4;
        private const int IdeologicalBranchSplitCooldownYears = 10;

        // v1.5.0-dev10: Full Ideology Tree. The active leaf/current still
        // uses StateIdeologyCurrentDataKey for save compatibility, while the
        // registry below now owns parent/child/root relationships. Future
        // builds can insert additional branch levels without changing saves.
        private const string IdeologyFrameworkSchemaDataKey = "ukiol_ideology_framework_schema";
        private const int IdeologyFrameworkSchemaVersion = 3;

        private const string CitizenIdeologyDataKey = "ukiol_citizen_ideology";
        private const string CitizenIdeologyConvictionDataKey = "ukiol_citizen_ideology_conviction";
        // Local fine-grained identity selected by the sandbox editor. Citizens
        // still store root ideologies for compatibility with the existing
        // support simulation; this key preserves the selected branch/current
        // for settlement identity and later city-politics integration.
        private const string CityIdeologyCurrentDataKey = "ukiol_city_ideology_current";
        private const string IdeologyCursorDataKey = "ukiol_ideology_cursor";

        private const string MonarchismIconPath = "ukiol/icons/monarchism";
        private const string ConservatismIconPath = "ukiol/icons/conservatism";
        private const string LiberalismIconPath = "ukiol/icons/liberalism";
        private const string DemocracyIconPath = "ukiol/icons/democracy";
        private const string SocialismIconPath = "ukiol/icons/socialism";
        private const string CommunismIconPath = "ukiol/icons/communism";
        private const string FascismIconPath = "ukiol/icons/fascism";
        private const string AnarchismIconPath = "ukiol/icons/anarchism";
        private const string SyndicalismIconPath = "ukiol/icons/syndicalism";

        private const float IdeologyTickInterval = 12f;

        private const int IdeologyActorsPerCityTick = 80;
        private const int IdeologySupportSamplePerCity = 120;

        private const int DefaultIdeologyConviction = 50;
        private const int IdeologyCurrentEvolutionThreshold = 3;
        private const int IdeologyRegistryMaxDepth = 8;

        private static string[] IdeologyIds =
        {
            MonarchismIdeologyId,
            ConservatismIdeologyId,
            LiberalismIdeologyId,
            DemocracyIdeologyId,
            SocialismIdeologyId,
            CommunismIdeologyId,
            FascismIdeologyId,
            AnarchismIdeologyId,
            SyndicalismIdeologyId
        };

        // Ideology Framework 2.0 registry. Root ideologies and every current
        // are represented by the same node type. In dev10 we can therefore
        // add real branch + sub-branch nodes without another save migration.
        private static readonly Dictionary<string, IdeologyNode>
            IdeologyNodeRegistry = new Dictionary<string, IdeologyNode>();
        private static readonly Dictionary<string, List<string>>
            IdeologyChildrenRegistry = new Dictionary<string, List<string>>();
        // v1.5.0-dev11: derived gameplay profiles for every ideology node.
        // Profiles are computed from the full inherited tag path, so all 99
        // currents receive behaviour without a giant per-current switch.
        private static readonly Dictionary<string, IdeologyBehaviorProfile>
            IdeologyBehaviorRegistry = new Dictionary<string, IdeologyBehaviorProfile>();
        private static bool _ideologyRegistryInitialized;

        private float _nextIdeologyTickTime;
    }
}
