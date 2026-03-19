namespace Olympus.Localization;

/// <summary>
/// Localization string key constants organized by domain.
/// Keys follow the pattern: category.section.element
/// </summary>
public static class LocalizedStrings
{
    #region Main Window (ui.main.*)

    /// <summary>Keys for the main window UI.</summary>
    public static class Main
    {
        public const string Status = "ui.main.status";
        public const string Active = "ui.main.active";
        public const string Inactive = "ui.main.inactive";
        public const string ActiveRotation = "ui.main.active_rotation";
        public const string None = "ui.main.none";
        public const string SwitchToSupported = "ui.main.switch_to_supported";
        public const string Available = "ui.main.available";
        public const string Enable = "ui.main.enable";
        public const string Disable = "ui.main.disable";
        public const string Settings = "ui.main.settings";
        public const string Overlay = "ui.main.overlay";
        public const string Analytics = "ui.main.analytics";
        public const string Training = "ui.main.training";
        public const string Debug = "ui.main.debug";
        public const string Changelog = "ui.main.changelog";
        public const string Positional = "ui.main.positional";
        public const string PositionalRear = "ui.main.positional_rear";
        public const string PositionalFlank = "ui.main.positional_flank";
        public const string PositionalFront = "ui.main.positional_front";
        public const string PositionalImmune = "ui.main.positional_immune";
        public const string Preset = "ui.main.preset";
    }

    #endregion

    #region Config Window (config.*)

    /// <summary>Keys for the config window header and footer.</summary>
    public static class Config
    {
        public const string WindowTitle = "config.window_title";
        public const string JoinDiscord = "config.join_discord";
        public const string EnableRotation = "config.enable_rotation";
        public const string EnableRotationDesc = "config.enable_rotation_desc";
        public const string ConfigPreset = "config.preset";
        public const string PresetTooltip = "config.preset_tooltip";
        public const string PresetRaid = "config.preset_raid";
        public const string PresetDungeon = "config.preset_dungeon";
        public const string PresetCasual = "config.preset_casual";
        public const string ResetToDefaults = "config.reset_to_defaults";
        public const string ResetConfirmation = "config.reset_confirmation";
        public const string ResetQuestion = "config.reset_question";
        public const string ResetWarning = "config.reset_warning";
        public const string YesReset = "config.yes_reset";
        public const string Cancel = "config.cancel";
        public const string Apply = "config.apply";
        public const string ApplyPreset = "config.apply_preset";
        public const string ApplyPresetConfirmation = "config.apply_preset_confirmation";
        public const string OverwriteWarning = "config.overwrite_warning";
        public const string PreservedSettings = "config.preserved_settings";

        // Search
        public const string SearchPlaceholder = "config.search.placeholder";
        public const string SearchNoResults = "config.search.no_results";
        public const string SearchClearTooltip = "config.search.clear_tooltip";

        // Update checker
        public const string CheckForUpdates = "config.check_for_updates";
        public const string Checking = "config.checking";
        public const string UpToDate = "config.up_to_date";
        public const string UpdateAvailable = "config.update_available";
        public const string CheckFailed = "config.check_failed";

        // Search result count and clear button
        public const string SearchResultCount = "config.search.result_count";
        public const string ClearSearch = "config.search.clear_button";

        // Import / Export
        public const string ExportConfig  = "config.export_config";
        public const string ImportConfig  = "config.import_config";
        public const string ExportSuccess = "config.export_success";
        public const string ImportSuccess = "config.import_success";
        public const string ImportError   = "config.import_error";
    }

    /// <summary>Keys for config sidebar navigation.</summary>
    public static class Sidebar
    {
        public const string General = "config.sidebar.general";
        public const string GeneralItem = "config.sidebar.general_item";
        public const string Targeting = "config.sidebar.targeting";
        public const string RoleActions = "config.sidebar.role_actions";
        public const string DrawHelper = "config.sidebar.draw_helper";
        public const string PartyCoordination = "config.sidebar.party_coordination";
        public const string Healers = "config.sidebar.healers";
        public const string WhiteMage = "config.sidebar.white_mage";
        public const string Scholar = "config.sidebar.scholar";
        public const string Astrologian = "config.sidebar.astrologian";
        public const string Sage = "config.sidebar.sage";
        public const string Tanks = "config.sidebar.tanks";
        public const string Shared = "config.sidebar.shared";
        public const string Paladin = "config.sidebar.paladin";
        public const string Warrior = "config.sidebar.warrior";
        public const string DarkKnight = "config.sidebar.dark_knight";
        public const string Gunbreaker = "config.sidebar.gunbreaker";

        // Melee DPS
        public const string MeleeDps = "config.sidebar.melee_dps";
        public const string Dragoon = "config.sidebar.dragoon";
        public const string Ninja = "config.sidebar.ninja";
        public const string Samurai = "config.sidebar.samurai";
        public const string Monk = "config.sidebar.monk";
        public const string Reaper = "config.sidebar.reaper";
        public const string Viper = "config.sidebar.viper";

        // Ranged Physical DPS
        public const string RangedDps = "config.sidebar.ranged_dps";
        public const string Machinist = "config.sidebar.machinist";
        public const string Bard = "config.sidebar.bard";
        public const string Dancer = "config.sidebar.dancer";

        // Casters
        public const string Casters = "config.sidebar.casters";
        public const string BlackMage = "config.sidebar.black_mage";
        public const string Summoner = "config.sidebar.summoner";
        public const string RedMage = "config.sidebar.red_mage";
        public const string Pictomancer = "config.sidebar.pictomancer";
    }

    /// <summary>Keys for general settings section.</summary>
    public static class General
    {
        public const string Header = "config.general.header";
        public const string MovementTolerance = "config.general.movement_tolerance";
        public const string MovementToleranceDesc = "config.general.movement_tolerance_desc";
        public const string EnableHealing = "config.general.enable_healing";
        public const string EnableDamage = "config.general.enable_damage";
        public const string EnableDoT = "config.general.enable_dot";
        public const string CombatBehaviorHeader = "config.general.combat_behavior_header";
        public const string StartOnAutoAttack = "config.general.start_on_auto_attack";
        public const string StartOnAutoAttackDesc = "config.general.start_on_auto_attack_desc";
    }

    /// <summary>Keys for Draw Helper overlay settings section.</summary>
    public static class DrawHelper
    {
        public const string SectionTitle = "DrawHelper.SectionTitle";
        public const string EnableDrawing = "DrawHelper.EnableDrawing";
        public const string EnableDrawingDisabledHint = "DrawHelper.EnableDrawingDisabledHint";
        public const string RenderingHeader = "DrawHelper.RenderingHeader";
        public const string UsePictomancy = "DrawHelper.UsePictomancy";
        public const string UsePictomancyTooltip = "DrawHelper.UsePictomancyTooltip";
        public const string MaxAlpha = "DrawHelper.MaxAlpha";
        public const string ClipToGameUI = "DrawHelper.ClipToGameUI";
        public const string EnemyHitboxesHeader = "DrawHelper.EnemyHitboxesHeader";
        public const string ShowEnemyHitboxes = "DrawHelper.ShowEnemyHitboxes";
        public const string MeleeRangeHeader = "DrawHelper.MeleeRangeHeader";
        public const string ShowMeleeRange = "DrawHelper.ShowMeleeRange";
        public const string FadeWhenInRange = "DrawHelper.FadeWhenInRange";
        public const string RangedRangeHeader = "DrawHelper.RangedRangeHeader";
        public const string ShowRangedRange = "DrawHelper.ShowRangedRange";
        public const string RangedRangeAutoDetect = "DrawHelper.RangedRangeAutoDetect";
        public const string PositionalsHeader = "DrawHelper.PositionalsHeader";
        public const string ShowPositionals = "DrawHelper.ShowPositionals";
    }

    /// <summary>Keys for targeting settings section.</summary>
    public static class Targeting
    {
        public const string Header = "config.targeting.header";
        public const string TargetingMode = "config.targeting.mode";
        public const string LowestHp = "config.targeting.lowest_hp";
        public const string LowestHpPercent = "config.targeting.lowest_hp_percent";
        public const string Priority = "config.targeting.priority";
        public const string Custom = "config.targeting.custom";
        public const string EnemyStrategy = "config.targeting.enemy_strategy";
        public const string StrategyLowestHp = "config.targeting.strategy.lowest_hp";
        public const string StrategyHighestHp = "config.targeting.strategy.highest_hp";
        public const string StrategyNearest = "config.targeting.strategy.nearest";
        public const string StrategyTankAssist = "config.targeting.strategy.tank_assist";
        public const string StrategyCurrentTarget = "config.targeting.strategy.current_target";
        public const string StrategyFocusTarget = "config.targeting.strategy.focus_target";
        public const string StrategyDescLowestHp = "config.targeting.strategy_desc.lowest_hp";
        public const string StrategyDescHighestHp = "config.targeting.strategy_desc.highest_hp";
        public const string StrategyDescNearest = "config.targeting.strategy_desc.nearest";
        public const string StrategyDescTankAssist = "config.targeting.strategy_desc.tank_assist";
        public const string StrategyDescCurrentTarget = "config.targeting.strategy_desc.current_target";
        public const string StrategyDescFocusTarget = "config.targeting.strategy_desc.focus_target";
        public const string FallbackToLowestHp = "config.targeting.fallback_to_lowest_hp";
        public const string FallbackToLowestHpDesc = "config.targeting.fallback_to_lowest_hp_desc";
        public const string MovementTolerance = "config.targeting.movement_tolerance";
        public const string MovementToleranceDesc = "config.targeting.movement_tolerance_desc";
    }

    /// <summary>Keys for role action settings section.</summary>
    public static class RoleActions
    {
        public const string Header = "config.role_actions.header";
        public const string LucidDreaming = "config.role_actions.lucid_dreaming";
        public const string LucidDreamingThreshold = "config.role_actions.lucid_dreaming_threshold";
        public const string Swiftcast = "config.role_actions.swiftcast";
        public const string Surecast = "config.role_actions.surecast";
        public const string Rescue = "config.role_actions.rescue";
        public const string Esuna = "config.role_actions.esuna";
        public const string EsunaSection = "config.role_actions.esuna_section";
        public const string EnableEsuna = "config.role_actions.enable_esuna";
        public const string EnableEsunaDesc = "config.role_actions.enable_esuna_desc";
        public const string EsunaPriorityThreshold = "config.role_actions.esuna_priority_threshold";
        public const string EsunaPriorityLethal = "config.role_actions.esuna_priority_lethal";
        public const string EsunaPriorityHigh = "config.role_actions.esuna_priority_high";
        public const string EsunaPriorityMedium = "config.role_actions.esuna_priority_medium";
        public const string EsunaPriorityAll = "config.role_actions.esuna_priority_all";
        public const string SurecastSection = "config.role_actions.surecast_section";
        public const string EnableSurecast = "config.role_actions.enable_surecast";
        public const string EnableSurecastDesc = "config.role_actions.enable_surecast_desc";
        public const string SurecastMode = "config.role_actions.surecast_mode";
        public const string SurecastModeManual = "config.role_actions.surecast_mode_manual";
        public const string SurecastModeAuto = "config.role_actions.surecast_mode_auto";
        public const string SurecastModeDesc = "config.role_actions.surecast_mode_desc";
        public const string RescueSection = "config.role_actions.rescue_section";
        public const string EnableRescue = "config.role_actions.enable_rescue";
        public const string EnableRescueDesc = "config.role_actions.enable_rescue_desc";
        public const string RescueWarning = "config.role_actions.rescue_warning";
    }

    /// <summary>Keys for resurrection settings.</summary>
    public static class Resurrection
    {
        public const string Section = "config.resurrection.section";
        public const string EnableRaise = "config.resurrection.enable_raise";
        public const string EnableRaiseDesc = "config.resurrection.enable_raise_desc";
        public const string RaisePriority = "config.resurrection.raise_priority";
        public const string RaiseModeFirst = "config.resurrection.raise_mode_first";
        public const string RaiseModeBalanced = "config.resurrection.raise_mode_balanced";
        public const string RaiseModeHealFirst = "config.resurrection.raise_mode_heal_first";
        public const string RaiseModeDescFirst = "config.resurrection.raise_mode_desc_first";
        public const string RaiseModeDescBalanced = "config.resurrection.raise_mode_desc_balanced";
        public const string RaiseModeDescHealFirst = "config.resurrection.raise_mode_desc_heal_first";
        public const string AllowHardcast = "config.resurrection.allow_hardcast";
        public const string AllowHardcastDesc = "config.resurrection.allow_hardcast_desc";
        public const string MinMpForRaise = "config.resurrection.min_mp_for_raise";
    }

    /// <summary>Keys for privacy settings.</summary>
    public static class Privacy
    {
        public const string Section = "config.privacy.section";
        public const string Telemetry = "config.privacy.telemetry";
        public const string TelemetryDesc = "config.privacy.telemetry_desc";
    }

    /// <summary>Keys for language settings.</summary>
    public static class Language
    {
        public const string Section = "config.language.section";
        public const string Select = "config.language.select";
        public const string SelectDesc = "config.language.select_desc";
        public const string Auto = "config.language.auto";
    }

    #endregion

    #region Healing Settings (config.healing.*)

    /// <summary>Keys for healing configuration.</summary>
    public static class Healing
    {
        public const string Section = "config.healing.section";
        public const string SingleTarget = "config.healing.single_target";
        public const string AoE = "config.healing.aoe";
        public const string Emergency = "config.healing.emergency";
        public const string EmergencyThreshold = "config.healing.emergency_threshold";
        public const string CriticalThreshold = "config.healing.critical_threshold";
        public const string SafeThreshold = "config.healing.safe_threshold";
        public const string OverhealThreshold = "config.healing.overheal_threshold";
        public const string PartyHealThreshold = "config.healing.party_heal_threshold";
        public const string MinTargets = "config.healing.min_targets";
    }

    #endregion

    #region Damage Settings (config.damage.*)

    /// <summary>Keys for damage configuration.</summary>
    public static class Damage
    {
        public const string Section = "config.damage.section";
        public const string Enable = "config.damage.enable";
        public const string MinManaThreshold = "config.damage.min_mana_threshold";
        public const string SafeToAttack = "config.damage.safe_to_attack";
        public const string SafeToAttackDesc = "config.damage.safe_to_attack_desc";
    }

    #endregion

    #region Buff Settings (config.buffs.*)

    /// <summary>Keys for buff/cooldown configuration.</summary>
    public static class Buffs
    {
        public const string Section = "config.buffs.section";
        public const string UseOnCooldown = "config.buffs.use_on_cooldown";
        public const string AlignWithBurst = "config.buffs.align_with_burst";
        public const string SaveForEmergency = "config.buffs.save_for_emergency";
    }

    #endregion

    #region Defensive Settings (config.defensive.*)

    /// <summary>Keys for defensive cooldown configuration.</summary>
    public static class Defensive
    {
        public const string Section = "config.defensive.section";
        public const string Enable = "config.defensive.enable";
        public const string TankBusterThreshold = "config.defensive.tankbuster_threshold";
        public const string RaidwideThreshold = "config.defensive.raidwide_threshold";
    }

    #endregion

    #region Job-Specific (config.job.*)

    /// <summary>Keys for White Mage specific settings.</summary>
    public static class WhiteMage
    {
        public const string Header = "config.job.whm.header";
        public const string LilySection = "config.job.whm.lily_section";
        public const string LilyPriority = "config.job.whm.lily_priority";
        public const string PreserveForMisery = "config.job.whm.preserve_for_misery";
        public const string Benediction = "config.job.whm.benediction";
        public const string BenedictionThreshold = "config.job.whm.benediction_threshold";
        public const string Tetragrammaton = "config.job.whm.tetragrammaton";
        public const string Asylum = "config.job.whm.asylum";
        public const string Assize = "config.job.whm.assize";
        public const string PlenaryIndulgence = "config.job.whm.plenary_indulgence";
        public const string Temperance = "config.job.whm.temperance";
        public const string Aquaveil = "config.job.whm.aquaveil";
        public const string DivineBenison = "config.job.whm.divine_benison";
        public const string LiturgyOfTheBell = "config.job.whm.liturgy_of_the_bell";

        // Section headers
        public const string HealingSection = "config.job.whm.healing_section";
        public const string DefensiveSection = "config.job.whm.defensive_section";
        public const string DamageSection = "config.job.whm.damage_section";
        public const string DoTSection = "config.job.whm.dot_section";

        // Sub-section labels
        public const string SingleTarget = "config.job.whm.single_target";
        public const string AoEHealing = "config.job.whm.aoe_healing";
        public const string LilyHeals = "config.job.whm.lily_heals";
        public const string OgcdHeals = "config.job.whm.ogcd_heals";
        public const string HealingHots = "config.job.whm.healing_hots";
        public const string Buffs = "config.job.whm.buffs";
        public const string EmergencyThresholds = "config.job.whm.emergency_thresholds";
        public const string Shields = "config.job.whm.shields";
        public const string PartyMitigation = "config.job.whm.party_mitigation";
        public const string Advanced = "config.job.whm.advanced";
        public const string StoneProgression = "config.job.whm.stone_progression";
        public const string GlareProgression = "config.job.whm.glare_progression";
        public const string AoEDamage = "config.job.whm.aoe_damage";
        public const string BloodLily = "config.job.whm.blood_lily";
        public const string HealingTriageLabel = "config.job.whm.healing_triage_label";
        public const string AssizeHealingLabel = "config.job.whm.assize_healing_label";
        public const string PreemptiveHealingLabel = "config.job.whm.preemptive_healing_label";
        public const string TimelineIntegrationLabel = "config.job.whm.timeline_integration_label";
        public const string ExperimentalLabel = "config.job.whm.experimental_label";

        // Ability descriptions
        public const string CureIIIDesc = "config.job.whm.cure_iii_desc";
        public const string LilyHealsDesc = "config.job.whm.lily_heals_desc";
        public const string OgcdHealsDesc = "config.job.whm.ogcd_heals_desc";
        public const string HealingHotsDesc = "config.job.whm.healing_hots_desc";
        public const string BuffsDesc = "config.job.whm.buffs_desc";
        public const string AetherialShiftDesc = "config.job.whm.aetherial_shift_desc";
        public const string UseDamageBasedTriage = "config.job.whm.use_damage_based_triage";
        public const string UseDamageBasedTriageDesc = "config.job.whm.use_damage_based_triage_desc";
        public const string TriagePreset = "config.job.whm.triage_preset";
        public const string TriagePresetBalanced = "config.job.whm.triage_preset_balanced";
        public const string TriagePresetTankFocus = "config.job.whm.triage_preset_tank_focus";
        public const string TriagePresetSpreadDamage = "config.job.whm.triage_preset_spread_damage";
        public const string TriagePresetRaidWide = "config.job.whm.triage_preset_raidwide";
        public const string TriagePresetCustom = "config.job.whm.triage_preset_custom";
        public const string EnableAssizeForHealing = "config.job.whm.enable_assize_for_healing";
        public const string EnableAssizeForHealingDesc = "config.job.whm.enable_assize_for_healing_desc";
        public const string EnablePreemptiveHealing = "config.job.whm.enable_preemptive_healing";
        public const string EnablePreemptiveHealingDesc = "config.job.whm.enable_preemptive_healing_desc";
        public const string EnableTimelinePredictions = "config.job.whm.enable_timeline_predictions";
        public const string EnableTimelinePredictionsDesc = "config.job.whm.enable_timeline_predictions_desc";
        public const string EnableScoredHealSelection = "config.job.whm.enable_scored_heal_selection";
        public const string EnableScoredHealSelectionDesc = "config.job.whm.enable_scored_heal_selection_desc";
        public const string ExperimentalWarning = "config.job.whm.experimental_warning";
        public const string ShieldsDesc = "config.job.whm.shields_desc";
        public const string PartyMitigationDesc = "config.job.whm.party_mitigation_desc";
        public const string BellAndCaressDesc = "config.job.whm.bell_and_caress_desc";
        public const string UseWithAoEHeals = "config.job.whm.use_with_aoe_heals";
        public const string UseWithAoEHealsDesc = "config.job.whm.use_with_aoe_heals_desc";
        public const string EnableDamage = "config.job.whm.enable_damage";
        public const string DpsPriority = "config.job.whm.dps_priority";
        public const string HolyDesc = "config.job.whm.holy_desc";
        public const string MiseryDesc = "config.job.whm.misery_desc";
        public const string EnableDoT = "config.job.whm.enable_dot";
    }

    /// <summary>Keys for Scholar specific settings.</summary>
    public static class Scholar
    {
        public const string Header = "config.job.sch.header";
        public const string AetherpactThreshold = "config.job.sch.aetherpact_threshold";
        public const string FairyAbilities = "config.job.sch.fairy_abilities";
        public const string Dissipation = "config.job.sch.dissipation";
        public const string Recitation = "config.job.sch.recitation";
        public const string Expedient = "config.job.sch.expedient";
        public const string Protraction = "config.job.sch.protraction";

        // Section headers
        public const string HealingSection = "config.job.sch.healing_section";
        public const string FairySection = "config.job.sch.fairy_section";
        public const string ShieldsSection = "config.job.sch.shields_section";
        public const string AetherflowSection = "config.job.sch.aetherflow_section";
        public const string DamageSection = "config.job.sch.damage_section";

        // Sub-section labels
        public const string GcdHeals = "config.job.sch.gcd_heals";
        public const string OgcdHeals = "config.job.sch.ogcd_heals";
        public const string SingleTargetThresholds = "config.job.sch.single_target_thresholds";
        public const string AoEHealing = "config.job.sch.aoe_healing";
        public const string RecitationPriorityLabel = "config.job.sch.recitation_priority_label";
        public const string SacredSoilLabel = "config.job.sch.sacred_soil_label";

        // Healing abilities
        public const string EnablePhysick = "config.job.sch.enable_physick";
        public const string EnableAdloquium = "config.job.sch.enable_adloquium";
        public const string EnableSuccor = "config.job.sch.enable_succor";
        public const string EnableLustrate = "config.job.sch.enable_lustrate";
        public const string EnableExcogitation = "config.job.sch.enable_excogitation";
        public const string EnableIndomitability = "config.job.sch.enable_indomitability";
        public const string EnableProtraction = "config.job.sch.enable_protraction";
        public const string EnableRecitation = "config.job.sch.enable_recitation";
        public const string EnableSacredSoil = "config.job.sch.enable_sacred_soil";
        public const string RecitationTarget = "config.job.sch.recitation_target";
        public const string RecitationTargetDesc = "config.job.sch.recitation_target_desc";
        public const string ExcogitationDesc = "config.job.sch.excogitation_desc";

        // Fairy section
        public const string AutoSummonFairy = "config.job.sch.auto_summon_fairy";
        public const string AutoSummonFairyDesc = "config.job.sch.auto_summon_fairy_desc";
        public const string EnableFairyAbilities = "config.job.sch.enable_fairy_abilities";
        public const string EnableFairyAbilitiesDesc = "config.job.sch.enable_fairy_abilities_desc";
        public const string WhisperingDawnLabel = "config.job.sch.whispering_dawn_label";
        public const string FeyBlessingLabel = "config.job.sch.fey_blessing_label";
        public const string FeyUnionLabel = "config.job.sch.fey_union_label";
        public const string SeraphLabel = "config.job.sch.seraph_label";
        public const string SeraphStrategy = "config.job.sch.seraph_strategy";
        public const string EnableConsolation = "config.job.sch.enable_consolation";
        public const string ConsolationDesc = "config.job.sch.consolation_desc";
        public const string SeraphismLabel = "config.job.sch.seraphism_label";
        public const string SeraphismStrategy = "config.job.sch.seraphism_strategy";

        // Shields section
        public const string EmergencyTactics = "config.job.sch.emergency_tactics";
        public const string EmergencyTacticsDesc = "config.job.sch.emergency_tactics_desc";
        public const string DeploymentTactics = "config.job.sch.deployment_tactics";
        public const string DeploymentTacticsDesc = "config.job.sch.deployment_tactics_desc";
        public const string AvoidSageShields = "config.job.sch.avoid_sage_shields";
        public const string AvoidSageShieldsDesc = "config.job.sch.avoid_sage_shields_desc";
        public const string ExpedientLabel = "config.job.sch.expedient_label";
        public const string EnableExpedient = "config.job.sch.enable_expedient";

        // Aetherflow section
        public const string AetherflowStrategy = "config.job.sch.aetherflow_strategy";
        public const string StrategyBalanced = "config.job.sch.strategy_balanced";
        public const string StrategyHealingPriority = "config.job.sch.strategy_healing_priority";
        public const string StrategyAggressiveDps = "config.job.sch.strategy_aggressive_dps";
        public const string StackReserve = "config.job.sch.stack_reserve";
        public const string StackReserveDesc = "config.job.sch.stack_reserve_desc";
        public const string EnableEnergyDrain = "config.job.sch.enable_energy_drain";
        public const string DumpWindow = "config.job.sch.dump_window";
        public const string DumpWindowDesc = "config.job.sch.dump_window_desc";
        public const string DissipationLabel = "config.job.sch.dissipation_label";
        public const string EnableDissipation = "config.job.sch.enable_dissipation";
        public const string DissipationDesc = "config.job.sch.dissipation_desc";
        public const string MaxFairyGauge = "config.job.sch.max_fairy_gauge";
        public const string MaxFairyGaugeDesc = "config.job.sch.max_fairy_gauge_desc";
        public const string SafePartyHp = "config.job.sch.safe_party_hp";
        public const string SafePartyHpDesc = "config.job.sch.safe_party_hp_desc";
        public const string MpManagement = "config.job.sch.mp_management";
        public const string EnableLucidDreaming = "config.job.sch.enable_lucid_dreaming";
        public const string LucidMpThreshold = "config.job.sch.lucid_mp_threshold";

        // Damage section
        public const string SingleTargetDamage = "config.job.sch.single_target_damage";
        public const string EnableBroilRuin = "config.job.sch.enable_broil_ruin";
        public const string BroilRuinDesc = "config.job.sch.broil_ruin_desc";
        public const string EnableRuinII = "config.job.sch.enable_ruin_ii";
        public const string RuinIIDesc = "config.job.sch.ruin_ii_desc";
        public const string DotLabel = "config.job.sch.dot_label";
        public const string EnableBioBiolysis = "config.job.sch.enable_bio_biolysis";
        public const string AoEDamageLabel = "config.job.sch.aoe_damage_label";
        public const string EnableArtOfWar = "config.job.sch.enable_art_of_war";
        public const string AetherflowLabel = "config.job.sch.aetherflow_label";
        public const string EnableAetherflow = "config.job.sch.enable_aetherflow";
        public const string AetherflowDesc = "config.job.sch.aetherflow_desc";
        public const string RaidBuffLabel = "config.job.sch.raid_buff_label";
        public const string EnableChainStratagem = "config.job.sch.enable_chain_stratagem";
        public const string ChainStratagemDesc = "config.job.sch.chain_stratagem_desc";
        public const string EnableBanefulImpaction = "config.job.sch.enable_baneful_impaction";
        public const string BanefulImpactionDesc = "config.job.sch.baneful_impaction_desc";
    }

    /// <summary>Keys for Astrologian specific settings.</summary>
    public static class Astrologian
    {
        public const string Header = "config.job.ast.header";
        public const string CardSystem = "config.job.ast.card_system";
        public const string EarthlyStar = "config.job.ast.earthly_star";
        public const string NeutralSect = "config.job.ast.neutral_sect";
        public const string CelestialIntersection = "config.job.ast.celestial_intersection";
        public const string Exaltation = "config.job.ast.exaltation";
        public const string Macrocosmos = "config.job.ast.macrocosmos";

        // Section headers
        public const string HealingSection = "config.job.ast.healing_section";
        public const string EarthlyStarSection = "config.job.ast.earthly_star_section";
        public const string HoroscopeSection = "config.job.ast.horoscope_section";
        public const string MacrocosmosSection = "config.job.ast.macrocosmos_section";
        public const string NeutralSectSection = "config.job.ast.neutral_sect_section";
        public const string CardsSection = "config.job.ast.cards_section";
        public const string SynastrySection = "config.job.ast.synastry_section";
        public const string LightspeedSection = "config.job.ast.lightspeed_section";
        public const string DamageSection = "config.job.ast.damage_section";

        // Sub-section labels
        public const string GcdHeals = "config.job.ast.gcd_heals";
        public const string AoEHeals = "config.job.ast.aoe_heals";
        public const string OgcdHeals = "config.job.ast.ogcd_heals";
        public const string SingleTargetThresholds = "config.job.ast.single_target_thresholds";
        public const string AoESettings = "config.job.ast.aoe_settings";
        public const string MinorArcanaLabel = "config.job.ast.minor_arcana_label";
        public const string BurstAbilities = "config.job.ast.burst_abilities";
        public const string SingleTargetDamage = "config.job.ast.single_target_damage";
        public const string DotLabel = "config.job.ast.dot_label";
        public const string AoEDamage = "config.job.ast.aoe_damage";
        public const string MpManagement = "config.job.ast.mp_management";
        public const string CollectiveUnconsciousLabel = "config.job.ast.collective_unconscious_label";

        // GCD Heals
        public const string EnableBenefic = "config.job.ast.enable_benefic";
        public const string EnableBeneficII = "config.job.ast.enable_benefic_ii";
        public const string EnableAspectedBenefic = "config.job.ast.enable_aspected_benefic";
        public const string AspectedBeneficDesc = "config.job.ast.aspected_benefic_desc";
        public const string EnableHelios = "config.job.ast.enable_helios";
        public const string EnableAspectedHelios = "config.job.ast.enable_aspected_helios";
        public const string AspectedHeliosDesc = "config.job.ast.aspected_helios_desc";

        // oGCD Heals
        public const string EnableEssentialDignity = "config.job.ast.enable_essential_dignity";
        public const string EssentialDignityDesc = "config.job.ast.essential_dignity_desc";
        public const string EnableCelestialIntersection = "config.job.ast.enable_celestial_intersection";
        public const string CelestialIntersectionDesc = "config.job.ast.celestial_intersection_desc";
        public const string EnableCelestialOpposition = "config.job.ast.enable_celestial_opposition";
        public const string CelestialOppositionDesc = "config.job.ast.celestial_opposition_desc";
        public const string EnableExaltation = "config.job.ast.enable_exaltation";
        public const string ExaltationDesc = "config.job.ast.exaltation_desc";

        // Thresholds
        public const string BeneficThreshold = "config.job.ast.benefic_threshold";
        public const string BeneficIIThreshold = "config.job.ast.benefic_ii_threshold";
        public const string AspectedBeneficThreshold = "config.job.ast.aspected_benefic_threshold";
        public const string EssentialDignityThreshold = "config.job.ast.essential_dignity_threshold";
        public const string EssentialDignityThresholdDesc = "config.job.ast.essential_dignity_threshold_desc";
        public const string CelestialIntersectionThreshold = "config.job.ast.celestial_intersection_threshold";
        public const string ExaltationThreshold = "config.job.ast.exaltation_threshold";
        public const string AoEHpThreshold = "config.job.ast.aoe_hp_threshold";
        public const string AoEMinTargets = "config.job.ast.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.ast.aoe_min_targets_desc";

        // Earthly Star
        public const string EnableEarthlyStar = "config.job.ast.enable_earthly_star";
        public const string EarthlyStarDesc = "config.job.ast.earthly_star_desc";
        public const string Placement = "config.job.ast.placement";
        public const string PlacementOnMainTank = "config.job.ast.placement_on_main_tank";
        public const string PlacementOnSelf = "config.job.ast.placement_on_self";
        public const string PlacementManual = "config.job.ast.placement_manual";
        public const string DetonateThreshold = "config.job.ast.detonate_threshold";
        public const string DetonateThresholdDesc = "config.job.ast.detonate_threshold_desc";
        public const string MinTargetsInRange = "config.job.ast.min_targets_in_range";
        public const string WaitForGiantDominance = "config.job.ast.wait_for_giant_dominance";
        public const string WaitForGiantDominanceDesc = "config.job.ast.wait_for_giant_dominance_desc";
        public const string EmergencyThreshold = "config.job.ast.emergency_threshold";
        public const string EmergencyThresholdDesc = "config.job.ast.emergency_threshold_desc";

        // Horoscope
        public const string EnableHoroscope = "config.job.ast.enable_horoscope";
        public const string HoroscopeDesc = "config.job.ast.horoscope_desc";
        public const string AutoCastHoroscope = "config.job.ast.auto_cast_horoscope";
        public const string AutoCastHoroscopeDesc = "config.job.ast.auto_cast_horoscope_desc";
        public const string HoroscopeThreshold = "config.job.ast.horoscope_threshold";
        public const string HoroscopeThresholdDesc = "config.job.ast.horoscope_threshold_desc";
        public const string HoroscopeMinTargets = "config.job.ast.horoscope_min_targets";

        // Macrocosmos
        public const string EnableMacrocosmos = "config.job.ast.enable_macrocosmos";
        public const string MacrocosmosDesc = "config.job.ast.macrocosmos_desc";
        public const string AutoUseMacrocosmos = "config.job.ast.auto_use_macrocosmos";
        public const string MacrocosmosThreshold = "config.job.ast.macrocosmos_threshold";
        public const string MacrocosmosThresholdDesc = "config.job.ast.macrocosmos_threshold_desc";
        public const string MacrocosmosMinTargets = "config.job.ast.macrocosmos_min_targets";

        // Neutral Sect
        public const string EnableNeutralSect = "config.job.ast.enable_neutral_sect";
        public const string NeutralSectDesc = "config.job.ast.neutral_sect_desc";
        public const string Strategy = "config.job.ast.strategy";
        public const string StrategyOnCooldown = "config.job.ast.strategy_on_cooldown";
        public const string StrategySaveForDamage = "config.job.ast.strategy_save_for_damage";
        public const string StrategyManual = "config.job.ast.strategy_manual";
        public const string NeutralSectThreshold = "config.job.ast.neutral_sect_threshold";
        public const string NeutralSectThresholdDesc = "config.job.ast.neutral_sect_threshold_desc";
        public const string EnableSunSign = "config.job.ast.enable_sun_sign";
        public const string SunSignDesc = "config.job.ast.sun_sign_desc";

        // Cards
        public const string EnableCards = "config.job.ast.enable_cards";
        public const string EnableCardsDesc = "config.job.ast.enable_cards_desc";
        public const string CardStrategy = "config.job.ast.card_strategy";
        public const string CardStrategyDpsFocused = "config.job.ast.card_strategy_dps_focused";
        public const string CardStrategyBalanced = "config.job.ast.card_strategy_balanced";
        public const string CardStrategySafetyFocused = "config.job.ast.card_strategy_safety_focused";
        public const string EnableMinorArcana = "config.job.ast.enable_minor_arcana";
        public const string MinorArcanaStrategy = "config.job.ast.minor_arcana_strategy";
        public const string LadyOfCrownsThreshold = "config.job.ast.lady_of_crowns_threshold";
        public const string LadyOfCrownsThresholdDesc = "config.job.ast.lady_of_crowns_threshold_desc";
        public const string EnableDivination = "config.job.ast.enable_divination";
        public const string DivinationDesc = "config.job.ast.divination_desc";
        public const string EnableAstrodyne = "config.job.ast.enable_astrodyne";
        public const string AstrodyneMinSeals = "config.job.ast.astrodyne_min_seals";
        public const string AstrodyneMinSealsDesc = "config.job.ast.astrodyne_min_seals_desc";
        public const string EnableOracle = "config.job.ast.enable_oracle";
        public const string OracleDesc = "config.job.ast.oracle_desc";

        // Synastry
        public const string EnableSynastry = "config.job.ast.enable_synastry";
        public const string SynastryDesc = "config.job.ast.synastry_desc";
        public const string SynastryThreshold = "config.job.ast.synastry_threshold";
        public const string SynastryThresholdDesc = "config.job.ast.synastry_threshold_desc";

        // Lightspeed
        public const string EnableLightspeed = "config.job.ast.enable_lightspeed";
        public const string LightspeedDesc = "config.job.ast.lightspeed_desc";
        public const string LightspeedStrategyOnCooldown = "config.job.ast.lightspeed_on_cooldown";
        public const string LightspeedStrategySaveForMovement = "config.job.ast.lightspeed_save_for_movement";
        public const string LightspeedStrategySaveForRaise = "config.job.ast.lightspeed_save_for_raise";

        // Damage
        public const string EnableMalefic = "config.job.ast.enable_malefic";
        public const string MaleficDesc = "config.job.ast.malefic_desc";
        public const string EnableCombust = "config.job.ast.enable_combust";
        public const string DotRefreshThreshold = "config.job.ast.dot_refresh_threshold";
        public const string DotRefreshThresholdDesc = "config.job.ast.dot_refresh_threshold_desc";
        public const string EnableGravity = "config.job.ast.enable_gravity";
        public const string AoEMinEnemies = "config.job.ast.aoe_min_enemies";
        public const string EnableLucidDreaming = "config.job.ast.enable_lucid_dreaming";
        public const string LucidMpThreshold = "config.job.ast.lucid_mp_threshold";
        public const string EnableCollectiveUnconscious = "config.job.ast.enable_collective_unconscious";
        public const string CollectiveUnconsciousWarning = "config.job.ast.collective_unconscious_warning";
        public const string CollectiveUnconsciousThreshold = "config.job.ast.collective_unconscious_threshold";
    }

    /// <summary>Keys for Sage specific settings.</summary>
    public static class Sage
    {
        public const string Header = "config.job.sge.header";
        public const string Kardia = "config.job.sge.kardia";
        public const string Eukrasia = "config.job.sge.eukrasia";
        public const string Physis = "config.job.sge.physis";
        public const string Haima = "config.job.sge.haima";
        public const string Panhaima = "config.job.sge.panhaima";
        public const string Holos = "config.job.sge.holos";
        public const string Pneuma = "config.job.sge.pneuma";
        public const string Krasis = "config.job.sge.krasis";
        public const string Pepsis = "config.job.sge.pepsis";

        // Section headers
        public const string KardiaSection = "config.job.sge.kardia_section";
        public const string AddersgallSection = "config.job.sge.addersgall_section";
        public const string HealingSection = "config.job.sge.healing_section";
        public const string ShieldsSection = "config.job.sge.shields_section";
        public const string BuffsSection = "config.job.sge.buffs_section";
        public const string DamageSection = "config.job.sge.damage_section";

        // Sub-section labels
        public const string GcdHeals = "config.job.sge.gcd_heals";
        public const string AddersgallHeals = "config.job.sge.addersgall_heals";
        public const string FreeOgcds = "config.job.sge.free_ogcds";
        public const string SingleTargetThresholds = "config.job.sge.single_target_thresholds";
        public const string AoEThresholds = "config.job.sge.aoe_thresholds";
        public const string SingleTargetDamage = "config.job.sge.single_target_damage";
        public const string DotLabel = "config.job.sge.dot_label";
        public const string AoEDamage = "config.job.sge.aoe_damage";
        public const string SpecialAbilities = "config.job.sge.special_abilities";
        public const string MpManagement = "config.job.sge.mp_management";

        // Kardia section
        public const string AutoApplyKardia = "config.job.sge.auto_apply_kardia";
        public const string AutoApplyKardiaDesc = "config.job.sge.auto_apply_kardia_desc";
        public const string EnableKardiaSwapping = "config.job.sge.enable_kardia_swapping";
        public const string EnableKardiaSwappingDesc = "config.job.sge.enable_kardia_swapping_desc";
        public const string SwapThreshold = "config.job.sge.swap_threshold";
        public const string SwapThresholdDesc = "config.job.sge.swap_threshold_desc";
        public const string EnableSoteria = "config.job.sge.enable_soteria";
        public const string SoteriaDesc = "config.job.sge.soteria_desc";
        public const string SoteriaThreshold = "config.job.sge.soteria_threshold";
        public const string SoteriaThresholdDesc = "config.job.sge.soteria_threshold_desc";

        // Addersgall section
        public const string StackReserve = "config.job.sge.stack_reserve";
        public const string StackReserveDesc = "config.job.sge.stack_reserve_desc";
        public const string PreventAddersgallCap = "config.job.sge.prevent_addersgall_cap";
        public const string PreventAddersgallCapDesc = "config.job.sge.prevent_addersgall_cap_desc";
        public const string CapPreventionWindow = "config.job.sge.cap_prevention_window";
        public const string CapPreventionWindowDesc = "config.job.sge.cap_prevention_window_desc";
        public const string EnableRhizomata = "config.job.sge.enable_rhizomata";
        public const string RhizomataDesc = "config.job.sge.rhizomata_desc";
        public const string RhizomataMinFreeSlots = "config.job.sge.rhizomata_min_free_slots";
        public const string RhizomataMinFreeSlotsDesc = "config.job.sge.rhizomata_min_free_slots_desc";

        // GCD Heals
        public const string EnableDiagnosis = "config.job.sge.enable_diagnosis";
        public const string DiagnosisDesc = "config.job.sge.diagnosis_desc";
        public const string EnableEukrasianDiagnosis = "config.job.sge.enable_eukrasian_diagnosis";
        public const string EukrasianDiagnosisDesc = "config.job.sge.eukrasian_diagnosis_desc";
        public const string EnablePrognosis = "config.job.sge.enable_prognosis";
        public const string PrognosisDesc = "config.job.sge.prognosis_desc";
        public const string EnableEukrasianPrognosis = "config.job.sge.enable_eukrasian_prognosis";
        public const string EukrasianPrognosisDesc = "config.job.sge.eukrasian_prognosis_desc";

        // Addersgall Heals
        public const string EnableDruochole = "config.job.sge.enable_druochole";
        public const string DruocholeDesc = "config.job.sge.druochole_desc";
        public const string EnableTaurochole = "config.job.sge.enable_taurochole";
        public const string TaurocholeDesc = "config.job.sge.taurochole_desc";
        public const string EnableIxochole = "config.job.sge.enable_ixochole";
        public const string IxocholeDesc = "config.job.sge.ixochole_desc";
        public const string EnableKerachole = "config.job.sge.enable_kerachole";
        public const string KeracholeDesc = "config.job.sge.kerachole_desc";

        // Free oGCDs
        public const string EnablePhysisII = "config.job.sge.enable_physis_ii";
        public const string PhysisIIDesc = "config.job.sge.physis_ii_desc";
        public const string EnableHolos = "config.job.sge.enable_holos";
        public const string HolosDesc = "config.job.sge.holos_desc";
        public const string EnablePepsis = "config.job.sge.enable_pepsis";
        public const string PepsisDesc = "config.job.sge.pepsis_desc";
        public const string EnablePneuma = "config.job.sge.enable_pneuma";
        public const string PneumaDesc = "config.job.sge.pneuma_desc";

        // Thresholds
        public const string DiagnosisThreshold = "config.job.sge.diagnosis_threshold";
        public const string EukrasianDiagnosisThreshold = "config.job.sge.eukrasian_diagnosis_threshold";
        public const string DruocholeThreshold = "config.job.sge.druochole_threshold";
        public const string TaurocholeThreshold = "config.job.sge.taurochole_threshold";
        public const string AoEHpThreshold = "config.job.sge.aoe_hp_threshold";
        public const string AoEMinTargets = "config.job.sge.aoe_min_targets";
        public const string KeracholeThreshold = "config.job.sge.kerachole_threshold";
        public const string IxocholeThreshold = "config.job.sge.ixochole_threshold";
        public const string PhysisIIThreshold = "config.job.sge.physis_ii_threshold";
        public const string HolosThreshold = "config.job.sge.holos_threshold";
        public const string PneumaThreshold = "config.job.sge.pneuma_threshold";
        public const string PepsisThreshold = "config.job.sge.pepsis_threshold";
        public const string PepsisThresholdDesc = "config.job.sge.pepsis_threshold_desc";

        // Shields section
        public const string EnableHaima = "config.job.sge.enable_haima";
        public const string HaimaDesc = "config.job.sge.haima_desc";
        public const string HaimaThreshold = "config.job.sge.haima_threshold";
        public const string HaimaThresholdDesc = "config.job.sge.haima_threshold_desc";
        public const string EnablePanhaima = "config.job.sge.enable_panhaima";
        public const string PanhaimaDesc = "config.job.sge.panhaima_desc";
        public const string PanhaimaThreshold = "config.job.sge.panhaima_threshold";
        public const string PanhaimaThresholdDesc = "config.job.sge.panhaima_threshold_desc";
        public const string AvoidOverwritingShields = "config.job.sge.avoid_overwriting_shields";
        public const string AvoidOverwritingShieldsDesc = "config.job.sge.avoid_overwriting_shields_desc";

        // Buffs section
        public const string EnableZoe = "config.job.sge.enable_zoe";
        public const string ZoeDesc = "config.job.sge.zoe_desc";
        public const string ZoeStrategy = "config.job.sge.zoe_strategy";
        public const string ZoeWithPneuma = "config.job.sge.zoe_with_pneuma";
        public const string ZoeWithEukrasianPrognosis = "config.job.sge.zoe_with_eukrasian_prognosis";
        public const string ZoeOnDemand = "config.job.sge.zoe_on_demand";
        public const string ZoeManual = "config.job.sge.zoe_manual";
        public const string EnableKrasis = "config.job.sge.enable_krasis";
        public const string KrasisDesc = "config.job.sge.krasis_desc";
        public const string KrasisThreshold = "config.job.sge.krasis_threshold";
        public const string KrasisThresholdDesc = "config.job.sge.krasis_threshold_desc";
        public const string EnablePhilosophia = "config.job.sge.enable_philosophia";
        public const string PhilosophiaDesc = "config.job.sge.philosophia_desc";
        public const string PhilosophiaThreshold = "config.job.sge.philosophia_threshold";
        public const string PhilosophiaThresholdDesc = "config.job.sge.philosophia_threshold_desc";

        // Damage section
        public const string EnableDosis = "config.job.sge.enable_dosis";
        public const string DosisDesc = "config.job.sge.dosis_desc";
        public const string EnableEukrasianDosis = "config.job.sge.enable_eukrasian_dosis";
        public const string EukrasianDosisDesc = "config.job.sge.eukrasian_dosis_desc";
        public const string DotRefreshThreshold = "config.job.sge.dot_refresh_threshold";
        public const string EnableDyskrasia = "config.job.sge.enable_dyskrasia";
        public const string DyskrasiaDesc = "config.job.sge.dyskrasia_desc";
        public const string AoEMinEnemies = "config.job.sge.aoe_min_enemies";
        public const string EnablePhlegma = "config.job.sge.enable_phlegma";
        public const string PhlegmaDesc = "config.job.sge.phlegma_desc";
        public const string EnableToxikon = "config.job.sge.enable_toxikon";
        public const string ToxikonDesc = "config.job.sge.toxikon_desc";
        public const string EnablePsyche = "config.job.sge.enable_psyche";
        public const string PsycheDesc = "config.job.sge.psyche_desc";
        public const string EnableLucidDreaming = "config.job.sge.enable_lucid_dreaming";
        public const string LucidMpThreshold = "config.job.sge.lucid_mp_threshold";
    }

    /// <summary>Keys for Tank shared settings.</summary>
    public static class Tank
    {
        public const string Header = "config.job.tank.header";
        public const string Mitigation = "config.job.tank.mitigation";
        public const string AutoProvoke = "config.job.tank.auto_provoke";
        public const string AutoShirk = "config.job.tank.auto_shirk";
        public const string DefensiveThreshold = "config.job.tank.defensive_threshold";
        public const string Invulnerability = "config.job.tank.invulnerability";
        public const string ReprisalThreshold = "config.job.tank.reprisal_threshold";

        // Shared tank section headers and labels
        public const string SharedHeader = "config.job.tank.shared_header";
        public const string SharedDescription = "config.job.tank.shared_description";
        public const string EnableMitigation = "config.job.tank.enable_mitigation";
        public const string EnableMitigationDesc = "config.job.tank.enable_mitigation_desc";
        public const string MitigationThreshold = "config.job.tank.mitigation_threshold";
        public const string MitigationThresholdDesc = "config.job.tank.mitigation_threshold_desc";
        public const string UseRampartOnCooldown = "config.job.tank.use_rampart_on_cooldown";
        public const string UseRampartOnCooldownDesc = "config.job.tank.use_rampart_on_cooldown_desc";
        public const string SheltronMinGauge = "config.job.tank.sheltron_min_gauge";
        public const string SheltronMinGaugeDesc = "config.job.tank.sheltron_min_gauge_desc";
        public const string TankStanceSection = "config.job.tank.stance_section";
        public const string AutoTankStance = "config.job.tank.auto_tank_stance";
        public const string AutoTankStanceDesc = "config.job.tank.auto_tank_stance_desc";
        public const string AutoProvokeDesc = "config.job.tank.auto_provoke_desc";
        public const string ProvokeDelay = "config.job.tank.provoke_delay";
        public const string ProvokeDelayDesc = "config.job.tank.provoke_delay_desc";
        public const string AutoShirkDesc = "config.job.tank.auto_shirk_desc";
        public const string DamageSection = "config.job.tank.damage_section";
        public const string EnableDamage = "config.job.tank.enable_damage";
        public const string EnableDamageDesc = "config.job.tank.enable_damage_desc";
        public const string EnableAoEDamage = "config.job.tank.enable_aoe_damage";
        public const string EnableAoEDamageDesc = "config.job.tank.enable_aoe_damage_desc";
        public const string AoEMinTargets = "config.job.tank.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.tank.aoe_min_targets_desc";
        public const string CurrentMinGauge = "config.job.tank.current_min_gauge";
        public const string CurrentMinTargets = "config.job.tank.current_min_targets";
        public const string UsesSharedAoESettings = "config.job.tank.uses_shared_aoe_settings";
        public const string UsesSharedGaugeSetting = "config.job.tank.uses_shared_gauge_setting";
    }

    /// <summary>Keys for Paladin specific settings.</summary>
    public static class Paladin
    {
        public const string MitigationSection = "config.job.pld.mitigation_section";
        public const string MitigationDesc = "config.job.pld.mitigation_desc";
        public const string OathGaugeAbilities = "config.job.pld.oath_gauge_abilities";
        public const string SheltronDesc = "config.job.pld.sheltron_desc";
        public const string AvailableAbilities = "config.job.pld.available_abilities";
        public const string SheltronHolySheltron = "config.job.pld.sheltron_holy_sheltron";
        public const string Intervention = "config.job.pld.intervention";
        public const string Cover = "config.job.pld.cover";
        public const string DivineVeil = "config.job.pld.divine_veil";
        public const string PassageOfArms = "config.job.pld.passage_of_arms";
        public const string HallowedGround = "config.job.pld.hallowed_ground";
        public const string DetailedSettingsWarning = "config.job.pld.detailed_settings_warning";
        public const string SelfHealingSection = "config.job.pld.self_healing_section";
        public const string ClemencyLabel = "config.job.pld.clemency_label";
        public const string ClemencyDesc1 = "config.job.pld.clemency_desc1";
        public const string ClemencyDesc2 = "config.job.pld.clemency_desc2";
        public const string ClemencyWarning = "config.job.pld.clemency_warning";
        public const string DamageSection = "config.job.pld.damage_section";
        public const string RotationFeatures = "config.job.pld.rotation_features";
        public const string FastBladeCombo = "config.job.pld.fast_blade_combo";
        public const string RequiescatWindow = "config.job.pld.requiescat_window";
        public const string FightOrFlightWindow = "config.job.pld.fight_or_flight_window";
        public const string ConfiteorCombo = "config.job.pld.confiteor_combo";
        public const string BladeOfHonor = "config.job.pld.blade_of_honor";
        public const string AoERotation = "config.job.pld.aoe_rotation";
        public const string TotalEclipseCombo = "config.job.pld.total_eclipse_combo";
        public const string CircleOfScorn = "config.job.pld.circle_of_scorn";

        // Job-specific toggles
        public const string EnableCover = "config.job.pld.enable_cover";
        public const string EnableCoverDesc = "config.job.pld.enable_cover_desc";
        public const string EnableDivineVeil = "config.job.pld.enable_divine_veil";
        public const string EnableDivineVeilDesc = "config.job.pld.enable_divine_veil_desc";
        public const string EnableClemency = "config.job.pld.enable_clemency";
        public const string EnableClemencyDesc = "config.job.pld.enable_clemency_desc";
        public const string ClemencyThreshold = "config.job.pld.clemency_threshold";
        public const string ClemencyThresholdDesc = "config.job.pld.clemency_threshold_desc";
    }

    /// <summary>Keys for Warrior specific settings.</summary>
    public static class Warrior
    {
        public const string MitigationSection = "config.job.war.mitigation_section";
        public const string MitigationDesc = "config.job.war.mitigation_desc";
        public const string BeastGaugeAbilities = "config.job.war.beast_gauge_abilities";
        public const string BeastGaugeDesc = "config.job.war.beast_gauge_desc";
        public const string AvailableAbilities = "config.job.war.available_abilities";
        public const string RawIntuitionBloodwhetting = "config.job.war.raw_intuition_bloodwhetting";
        public const string NascentFlash = "config.job.war.nascent_flash";
        public const string ThrillOfBattle = "config.job.war.thrill_of_battle";
        public const string Equilibrium = "config.job.war.equilibrium";
        public const string ShakeItOff = "config.job.war.shake_it_off";
        public const string Holmgang = "config.job.war.holmgang";
        public const string DetailedSettingsWarning = "config.job.war.detailed_settings_warning";
        public const string BeastGaugeSection = "config.job.war.beast_gauge_section";
        public const string GaugeUsage = "config.job.war.gauge_usage";
        public const string GaugeBuilds = "config.job.war.gauge_builds";
        public const string GaugeSpent = "config.job.war.gauge_spent";
        public const string InnerReleaseLabel = "config.job.war.inner_release_label";
        public const string InnerReleaseDesc1 = "config.job.war.inner_release_desc1";
        public const string InnerReleaseDesc2 = "config.job.war.inner_release_desc2";
        public const string BeastGaugeWarning = "config.job.war.beast_gauge_warning";
        public const string DamageSection = "config.job.war.damage_section";
        public const string RotationFeatures = "config.job.war.rotation_features";
        public const string HeavySwingCombo = "config.job.war.heavy_swing_combo";
        public const string InnerReleaseWindow = "config.job.war.inner_release_window";
        public const string FellCleaveSpam = "config.job.war.fell_cleave_spam";
        public const string PrimalRendRuination = "config.job.war.primal_rend_ruination";
        public const string OnslaughtCharges = "config.job.war.onslaught_charges";
        public const string AoERotation = "config.job.war.aoe_rotation";
        public const string OverpowerCombo = "config.job.war.overpower_combo";
        public const string DecimateInnerRelease = "config.job.war.decimate_inner_release";
        public const string Orogeny = "config.job.war.orogeny";

        // Job-specific toggles
        public const string EnableNascentFlash = "config.job.war.enable_nascent_flash";
        public const string EnableNascentFlashDesc = "config.job.war.enable_nascent_flash_desc";
        public const string EnableHolmgang = "config.job.war.enable_holmgang";
        public const string EnableHolmgangDesc = "config.job.war.enable_holmgang_desc";
        public const string BeastGaugeCap = "config.job.war.beast_gauge_cap";
        public const string BeastGaugeCapDesc = "config.job.war.beast_gauge_cap_desc";
    }

    /// <summary>Keys for Dark Knight specific settings.</summary>
    public static class DarkKnight
    {
        public const string MitigationSection = "config.job.drk.mitigation_section";
        public const string MitigationDesc = "config.job.drk.mitigation_desc";
        public const string TBNLabel = "config.job.drk.tbn_label";
        public const string TBNDesc1 = "config.job.drk.tbn_desc1";
        public const string TBNDesc2 = "config.job.drk.tbn_desc2";
        public const string AvailableAbilities = "config.job.drk.available_abilities";
        public const string TheBlackestNight = "config.job.drk.the_blackest_night";
        public const string Oblation = "config.job.drk.oblation";
        public const string DarkMind = "config.job.drk.dark_mind";
        public const string DarkMissionary = "config.job.drk.dark_missionary";
        public const string LivingDead = "config.job.drk.living_dead";
        public const string DetailedSettingsWarning = "config.job.drk.detailed_settings_warning";
        public const string BloodGaugeMPSection = "config.job.drk.blood_gauge_mp_section";
        public const string BloodGaugeLabel = "config.job.drk.blood_gauge_label";
        public const string BloodGaugeDesc1 = "config.job.drk.blood_gauge_desc1";
        public const string BloodGaugeDesc2 = "config.job.drk.blood_gauge_desc2";
        public const string MPManagement = "config.job.drk.mp_management";
        public const string MPDesc1 = "config.job.drk.mp_desc1";
        public const string MPDesc2 = "config.job.drk.mp_desc2";
        public const string MPDesc3 = "config.job.drk.mp_desc3";
        public const string DeliriumLabel = "config.job.drk.delirium_label";
        public const string DeliriumDesc1 = "config.job.drk.delirium_desc1";
        public const string DeliriumDesc2 = "config.job.drk.delirium_desc2";
        public const string BloodGaugeWarning = "config.job.drk.blood_gauge_warning";
        public const string DamageSection = "config.job.drk.damage_section";
        public const string RotationFeatures = "config.job.drk.rotation_features";
        public const string HardSlashCombo = "config.job.drk.hard_slash_combo";
        public const string DeliriumScarletCombo = "config.job.drk.delirium_scarlet_combo";
        public const string EdgeOfShadowWeaves = "config.job.drk.edge_of_shadow_weaves";
        public const string Shadowbringer = "config.job.drk.shadowbringer";
        public const string LivingShadow = "config.job.drk.living_shadow";
        public const string SaltedEarthDarkness = "config.job.drk.salted_earth_darkness";
        public const string AoERotation = "config.job.drk.aoe_rotation";
        public const string UnleashCombo = "config.job.drk.unleash_combo";
        public const string QuietusDelirium = "config.job.drk.quietus_delirium";
        public const string FloodOfShadow = "config.job.drk.flood_of_shadow";
        public const string AbyssalDrain = "config.job.drk.abyssal_drain";

        // Job-specific toggles
        public const string EnableLivingDead = "config.job.drk.enable_living_dead";
        public const string EnableLivingDeadDesc = "config.job.drk.enable_living_dead_desc";
        public const string EnableDarkMissionary = "config.job.drk.enable_dark_missionary";
        public const string EnableDarkMissionaryDesc = "config.job.drk.enable_dark_missionary_desc";
        public const string EnableTheBlackestNight = "config.job.drk.enable_the_blackest_night";
        public const string EnableTheBlackestNightDesc = "config.job.drk.enable_the_blackest_night_desc";
        public const string TBNThreshold = "config.job.drk.tbn_threshold";
        public const string TBNThresholdDesc = "config.job.drk.tbn_threshold_desc";
        public const string BloodGaugeCap = "config.job.drk.blood_gauge_cap";
        public const string BloodGaugeCapDesc = "config.job.drk.blood_gauge_cap_desc";
    }

    /// <summary>Keys for Gunbreaker specific settings.</summary>
    public static class Gunbreaker
    {
        public const string MitigationSection = "config.job.gnb.mitigation_section";
        public const string MitigationDesc = "config.job.gnb.mitigation_desc";
        public const string HeartOfCorundumLabel = "config.job.gnb.heart_of_corundum_label";
        public const string HeartOfCorundumDesc1 = "config.job.gnb.heart_of_corundum_desc1";
        public const string HeartOfCorundumDesc2 = "config.job.gnb.heart_of_corundum_desc2";
        public const string AvailableAbilities = "config.job.gnb.available_abilities";
        public const string HeartOfStoneCorundum = "config.job.gnb.heart_of_stone_corundum";
        public const string Aurora = "config.job.gnb.aurora";
        public const string Camouflage = "config.job.gnb.camouflage";
        public const string HeartOfLight = "config.job.gnb.heart_of_light";
        public const string Superbolide = "config.job.gnb.superbolide";
        public const string DetailedSettingsWarning = "config.job.gnb.detailed_settings_warning";
        public const string CartridgeSection = "config.job.gnb.cartridge_section";
        public const string PowderGaugeLabel = "config.job.gnb.powder_gauge_label";
        public const string PowderGaugeDesc1 = "config.job.gnb.powder_gauge_desc1";
        public const string PowderGaugeDesc2 = "config.job.gnb.powder_gauge_desc2";
        public const string PowderGaugeDesc3 = "config.job.gnb.powder_gauge_desc3";
        public const string CartridgeUsage = "config.job.gnb.cartridge_usage";
        public const string GnashingFangCombo = "config.job.gnb.gnashing_fang_combo";
        public const string BurstStrike = "config.job.gnb.burst_strike";
        public const string DoubleDown = "config.job.gnb.double_down";
        public const string FatedCircleAoE = "config.job.gnb.fated_circle_aoe";
        public const string CartridgeWarning = "config.job.gnb.cartridge_warning";
        public const string DamageSection = "config.job.gnb.damage_section";
        public const string RotationFeatures = "config.job.gnb.rotation_features";
        public const string KeenEdgeCombo = "config.job.gnb.keen_edge_combo";
        public const string NoMercyWindow = "config.job.gnb.no_mercy_window";
        public const string GnashingFangContinuation = "config.job.gnb.gnashing_fang_continuation";
        public const string DoubleDownDamage = "config.job.gnb.double_down_damage";
        public const string BurstStrikeHypervelocity = "config.job.gnb.burst_strike_hypervelocity";
        public const string SonicBreakBowShock = "config.job.gnb.sonic_break_bow_shock";
        public const string ReignOfBeastsCombo = "config.job.gnb.reign_of_beasts_combo";
        public const string AoERotation = "config.job.gnb.aoe_rotation";
        public const string DemonSliceCombo = "config.job.gnb.demon_slice_combo";
        public const string FatedCircle = "config.job.gnb.fated_circle";
        public const string BowShock = "config.job.gnb.bow_shock";

        // Job-specific toggles
        public const string EnableHeartOfLight = "config.job.gnb.enable_heart_of_light";
        public const string EnableHeartOfLightDesc = "config.job.gnb.enable_heart_of_light_desc";
        public const string EnableHeartOfCorundum = "config.job.gnb.enable_heart_of_corundum";
        public const string EnableHeartOfCorundumDesc = "config.job.gnb.enable_heart_of_corundum_desc";
        public const string HeartOfCorundumThreshold = "config.job.gnb.heart_of_corundum_threshold";
        public const string HeartOfCorundumThresholdDesc = "config.job.gnb.heart_of_corundum_threshold_desc";
    }

    // ===== MELEE DPS =====

    /// <summary>Keys for shared melee DPS settings.</summary>
    public static class MeleeDps
    {
        public const string Header = "config.melee_dps.header";
        public const string PositionalSection = "config.melee_dps.positional_section";
        public const string PositionalDesc = "config.melee_dps.positional_desc";
        public const string PositionalFlank = "config.melee_dps.positional_flank";
        public const string PositionalRear = "config.melee_dps.positional_rear";
        public const string PositionalNote = "config.melee_dps.positional_note";
        public const string AoESection = "config.melee_dps.aoe_section";
        public const string AoEDesc = "config.melee_dps.aoe_desc";
        public const string AoEDefault = "config.melee_dps.aoe_default";
        public const string AoENote = "config.melee_dps.aoe_note";
        public const string BurstSection = "config.melee_dps.burst_section";
        public const string BurstDesc = "config.melee_dps.burst_desc";
        public const string BurstNote = "config.melee_dps.burst_note";
        public const string PartyBuffs = "config.melee_dps.party_buffs";
        public const string BattleLitany = "config.melee_dps.battle_litany";
        public const string Brotherhood = "config.melee_dps.brotherhood";
        public const string ArcaneCircle = "config.melee_dps.arcane_circle";
    }

    /// <summary>Keys for Dragoon specific settings.</summary>
    public static class Dragoon
    {
        public const string DamageSection = "config.job.drg.damage_section";
        public const string EnableGeirskogul = "config.job.drg.enable_geirskogul";
        public const string EnableGeirskogulDesc = "config.job.drg.enable_geirskogul_desc";
        public const string EnableStardiver = "config.job.drg.enable_stardiver";
        public const string EnableStardiverDesc = "config.job.drg.enable_stardiver_desc";
        public const string EnableJumps = "config.job.drg.enable_jumps";
        public const string EnableJumpsDesc = "config.job.drg.enable_jumps_desc";
        public const string EnableSpineshatterDive = "config.job.drg.enable_spineshatter_dive";
        public const string EnableSpineshatterDiveDesc = "config.job.drg.enable_spineshatter_dive_desc";
        public const string EnableLifeSurge = "config.job.drg.enable_life_surge";
        public const string EnableLifeSurgeDesc = "config.job.drg.enable_life_surge_desc";
        public const string EnableAoERotation = "config.job.drg.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.drg.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.drg.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.drg.aoe_min_targets_desc";
        public const string GeirskogulMinEyes = "config.job.drg.geirskogul_min_eyes";
        public const string GeirskogulMinEyesDesc = "config.job.drg.geirskogul_min_eyes_desc";
        public const string BuffSection = "config.job.drg.buff_section";
        public const string EnableLanceCharge = "config.job.drg.enable_lance_charge";
        public const string EnableLanceChargeDesc = "config.job.drg.enable_lance_charge_desc";
        public const string EnableBattleLitany = "config.job.drg.enable_battle_litany";
        public const string EnableBattleLitanyDesc = "config.job.drg.enable_battle_litany_desc";
        public const string BurstSection = "config.job.drg.burst_section";
        public const string BattleLitanyHoldTime = "config.job.drg.battle_litany_hold_time";
        public const string BattleLitanyHoldTimeDesc = "config.job.drg.battle_litany_hold_time_desc";
        public const string PositionalSection = "config.job.drg.positional_section";
        public const string EnforcePositionals = "config.job.drg.enforce_positionals";
        public const string EnforcePositionalsDesc = "config.job.drg.enforce_positionals_desc";
        public const string AllowPositionalLoss = "config.job.drg.allow_positional_loss";
        public const string AllowPositionalLossDesc = "config.job.drg.allow_positional_loss_desc";
        public const string EnableDragonfireDive = "config.job.drg.enable_dragonfire_dive";
        public const string EnableNastrond = "config.job.drg.enable_nastrond";
        public const string EnableWyrmwindThrust = "config.job.drg.enable_wyrmwind_thrust";
        public const string EnableMirageDive = "config.job.drg.enable_mirage_dive";
        public const string EnableBurstPooling = "config.job.drg.enable_burst_pooling";
        public const string EnableBurstPoolingDesc = "config.job.drg.enable_burst_pooling_desc";
    }

    /// <summary>Keys for Ninja specific settings.</summary>
    public static class Ninja
    {
        public const string DamageSection = "config.job.nin.damage_section";
        public const string EnableNinjutsu = "config.job.nin.enable_ninjutsu";
        public const string EnableNinjutsuDesc = "config.job.nin.enable_ninjutsu_desc";
        public const string EnableRaiju = "config.job.nin.enable_raiju";
        public const string EnableRaijuDesc = "config.job.nin.enable_raiju_desc";
        public const string EnablePhantomKamaitachi = "config.job.nin.enable_phantom_kamaitachi";
        public const string EnablePhantomKamaitachiDesc = "config.job.nin.enable_phantom_kamaitachi_desc";
        public const string EnableAoERotation = "config.job.nin.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.nin.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.nin.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.nin.aoe_min_targets_desc";
        public const string NinkiSection = "config.job.nin.ninki_section";
        public const string EnableBhavacakra = "config.job.nin.enable_bhavacakra";
        public const string EnableBhavacakraDesc = "config.job.nin.enable_bhavacakra_desc";
        public const string NinkiMinGauge = "config.job.nin.ninki_min_gauge";
        public const string NinkiMinGaugeDesc = "config.job.nin.ninki_min_gauge_desc";
        public const string NinkiOvercapThreshold = "config.job.nin.ninki_overcap_threshold";
        public const string NinkiOvercapThresholdDesc = "config.job.nin.ninki_overcap_threshold_desc";
        public const string MudraSection = "config.job.nin.mudra_section";
        public const string NinjutsuPriority = "config.job.nin.ninjutsu_priority";
        public const string NinjutsuPriorityDesc = "config.job.nin.ninjutsu_priority_desc";
        public const string UseDotonForAoE = "config.job.nin.use_doton_for_aoe";
        public const string UseDotonForAoEDesc = "config.job.nin.use_doton_for_aoe_desc";
        public const string DotonMinTargets = "config.job.nin.doton_min_targets";
        public const string DotonMinTargetsDesc = "config.job.nin.doton_min_targets_desc";
        public const string BurstSection = "config.job.nin.burst_section";
        public const string EnableKunaisBane = "config.job.nin.enable_kunais_bane";
        public const string EnableKunaisBaneDesc = "config.job.nin.enable_kunais_bane_desc";
        public const string KunaisBaneHoldTime = "config.job.nin.kunais_bane_hold_time";
        public const string KunaisBaneHoldTimeDesc = "config.job.nin.kunais_bane_hold_time_desc";
    }

    /// <summary>Keys for Samurai specific settings.</summary>
    public static class Samurai
    {
        public const string DamageSection = "config.job.sam.damage_section";
        public const string EnableIaijutsu = "config.job.sam.enable_iaijutsu";
        public const string EnableIaijutsuDesc = "config.job.sam.enable_iaijutsu_desc";
        public const string EnableTsubamegaeshi = "config.job.sam.enable_tsubamegaeshi";
        public const string EnableTsubamegaeshiDesc = "config.job.sam.enable_tsubamegaeshi_desc";
        public const string EnableOgiNamikiri = "config.job.sam.enable_ogi_namikiri";
        public const string EnableOgiNamikiriDesc = "config.job.sam.enable_ogi_namikiri_desc";
        public const string EnableAoERotation = "config.job.sam.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.sam.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.sam.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.sam.aoe_min_targets_desc";
        public const string KenkiSection = "config.job.sam.kenki_section";
        public const string EnableShinten = "config.job.sam.enable_shinten";
        public const string EnableShintenDesc = "config.job.sam.enable_shinten_desc";
        public const string EnableSenei = "config.job.sam.enable_senei";
        public const string EnableSeneiDesc = "config.job.sam.enable_senei_desc";
        public const string KenkiMinGauge = "config.job.sam.kenki_min_gauge";
        public const string KenkiMinGaugeDesc = "config.job.sam.kenki_min_gauge_desc";
        public const string KenkiOvercapThreshold = "config.job.sam.kenki_overcap_threshold";
        public const string KenkiOvercapThresholdDesc = "config.job.sam.kenki_overcap_threshold_desc";
        public const string SenSection = "config.job.sam.sen_section";
        public const string MaintainHiganbana = "config.job.sam.maintain_higanbana";
        public const string MaintainHiganbanaDesc = "config.job.sam.maintain_higanbana_desc";
        public const string HiganbanaRefreshThreshold = "config.job.sam.higanbana_refresh_threshold";
        public const string HiganbanaRefreshThresholdDesc = "config.job.sam.higanbana_refresh_threshold_desc";
        public const string HiganbanaMinTargetHp = "config.job.sam.higanbana_min_target_hp";
        public const string HiganbanaMinTargetHpDesc = "config.job.sam.higanbana_min_target_hp_desc";
        public const string BurstSection = "config.job.sam.burst_section";
        public const string EnableIkishoten = "config.job.sam.enable_ikishoten";
        public const string EnableIkishotenDesc = "config.job.sam.enable_ikishoten_desc";
        public const string IkishotenHoldTime = "config.job.sam.ikishoten_hold_time";
        public const string IkishotenHoldTimeDesc = "config.job.sam.ikishoten_hold_time_desc";
    }

    /// <summary>Keys for Monk specific settings.</summary>
    public static class Monk
    {
        public const string DamageSection = "config.job.mnk.damage_section";
        public const string EnableMasterfulBlitz = "config.job.mnk.enable_masterful_blitz";
        public const string EnableMasterfulBlitzDesc = "config.job.mnk.enable_masterful_blitz_desc";
        public const string EnableSixSidedStar = "config.job.mnk.enable_six_sided_star";
        public const string EnableSixSidedStarDesc = "config.job.mnk.enable_six_sided_star_desc";
        public const string EnableAoERotation = "config.job.mnk.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.mnk.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.mnk.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.mnk.aoe_min_targets_desc";
        public const string ChakraSection = "config.job.mnk.chakra_section";
        public const string EnableChakraSpenders = "config.job.mnk.enable_chakra_spenders";
        public const string EnableChakraSpendersDesc = "config.job.mnk.enable_chakra_spenders_desc";
        public const string ChakraMinGauge = "config.job.mnk.chakra_min_gauge";
        public const string ChakraMinGaugeDesc = "config.job.mnk.chakra_min_gauge_desc";
        public const string PositionalSection = "config.job.mnk.positional_section";
        public const string PositionalStrictness = "config.job.mnk.positional_strictness";
        public const string PositionalStrictnessDesc = "config.job.mnk.positional_strictness_desc";
        public const string AllowPositionalLoss = "config.job.mnk.allow_positional_loss";
        public const string AllowPositionalLossDesc = "config.job.mnk.allow_positional_loss_desc";
        public const string BuffSection = "config.job.mnk.buff_section";
        public const string EnableRiddleOfFire = "config.job.mnk.enable_riddle_of_fire";
        public const string EnableRiddleOfFireDesc = "config.job.mnk.enable_riddle_of_fire_desc";
        public const string EnableBrotherhood = "config.job.mnk.enable_brotherhood";
        public const string EnableBrotherhoodDesc = "config.job.mnk.enable_brotherhood_desc";
        public const string BrotherhoodHoldTime = "config.job.mnk.brotherhood_hold_time";
        public const string BrotherhoodHoldTimeDesc = "config.job.mnk.brotherhood_hold_time_desc";
        public const string EnableBurstPooling = "config.job.mnk.enable_burst_pooling";
        public const string EnableBurstPoolingDesc = "config.job.mnk.enable_burst_pooling_desc";
    }

    /// <summary>Keys for Reaper specific settings.</summary>
    public static class Reaper
    {
        public const string DamageSection = "config.job.rpr.damage_section";
        public const string EnableSoulReaver = "config.job.rpr.enable_soul_reaver";
        public const string EnableSoulReaverDesc = "config.job.rpr.enable_soul_reaver_desc";
        public const string EnableCommunio = "config.job.rpr.enable_communio";
        public const string EnableCommunioDesc = "config.job.rpr.enable_communio_desc";
        public const string EnablePerfectio = "config.job.rpr.enable_perfectio";
        public const string EnablePerfectioDesc = "config.job.rpr.enable_perfectio_desc";
        public const string EnableAoERotation = "config.job.rpr.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.rpr.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.rpr.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.rpr.aoe_min_targets_desc";
        public const string GaugeSection = "config.job.rpr.gauge_section";
        public const string SoulMinGauge = "config.job.rpr.soul_min_gauge";
        public const string SoulMinGaugeDesc = "config.job.rpr.soul_min_gauge_desc";
        public const string SoulOvercapThreshold = "config.job.rpr.soul_overcap_threshold";
        public const string SoulOvercapThresholdDesc = "config.job.rpr.soul_overcap_threshold_desc";
        public const string ShroudMinGauge = "config.job.rpr.shroud_min_gauge";
        public const string ShroudMinGaugeDesc = "config.job.rpr.shroud_min_gauge_desc";
        public const string EnshroudSection = "config.job.rpr.enshroud_section";
        public const string EnableEnshroud = "config.job.rpr.enable_enshroud";
        public const string EnableEnshroudDesc = "config.job.rpr.enable_enshroud_desc";
        public const string EnableLemureAbilities = "config.job.rpr.enable_lemure_abilities";
        public const string EnableLemureAbilitiesDesc = "config.job.rpr.enable_lemure_abilities_desc";
        public const string SaveShroudForBurst = "config.job.rpr.save_shroud_for_burst";
        public const string SaveShroudForBurstDesc = "config.job.rpr.save_shroud_for_burst_desc";
        public const string BurstSection = "config.job.rpr.burst_section";
        public const string EnableArcaneCircle = "config.job.rpr.enable_arcane_circle";
        public const string EnableArcaneCircleDesc = "config.job.rpr.enable_arcane_circle_desc";
        public const string ArcaneCircleHoldTime = "config.job.rpr.arcane_circle_hold_time";
        public const string ArcaneCircleHoldTimeDesc = "config.job.rpr.arcane_circle_hold_time_desc";
    }

    /// <summary>Keys for Viper specific settings.</summary>
    public static class Viper
    {
        public const string DamageSection = "config.job.vpr.damage_section";
        public const string EnableTwinbladeCombo = "config.job.vpr.enable_twinblade_combo";
        public const string EnableTwinbladeComboDesc = "config.job.vpr.enable_twinblade_combo_desc";
        public const string EnableUncoiledFury = "config.job.vpr.enable_uncoiled_fury";
        public const string EnableUncoiledFuryDesc = "config.job.vpr.enable_uncoiled_fury_desc";
        public const string EnableAoERotation = "config.job.vpr.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.vpr.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.vpr.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.vpr.aoe_min_targets_desc";
        public const string ReawakenSection = "config.job.vpr.reawaken_section";
        public const string EnableReawaken = "config.job.vpr.enable_reawaken";
        public const string EnableReawakenDesc = "config.job.vpr.enable_reawaken_desc";
        public const string EnableOuroboros = "config.job.vpr.enable_ouroboros";
        public const string EnableOuroborosDesc = "config.job.vpr.enable_ouroboros_desc";
        public const string AnguineMinStacks = "config.job.vpr.anguine_min_stacks";
        public const string AnguineMinStacksDesc = "config.job.vpr.anguine_min_stacks_desc";
        public const string SaveAnguineForBurst = "config.job.vpr.save_anguine_for_burst";
        public const string SaveAnguineForBurstDesc = "config.job.vpr.save_anguine_for_burst_desc";
        public const string BurstSection = "config.job.vpr.burst_section";
        public const string EnableSerpentsIre = "config.job.vpr.enable_serpents_ire";
        public const string EnableSerpentsIreDesc = "config.job.vpr.enable_serpents_ire_desc";
        public const string SerpentsIreHoldTime = "config.job.vpr.serpents_ire_hold_time";
        public const string SerpentsIreHoldTimeDesc = "config.job.vpr.serpents_ire_hold_time_desc";
        public const string PositionalSection = "config.job.vpr.positional_section";
        public const string EnforcePositionals = "config.job.vpr.enforce_positionals";
        public const string EnforcePositionalsDesc = "config.job.vpr.enforce_positionals_desc";
        public const string OptimizeVenomPositionals = "config.job.vpr.optimize_venom_positionals";
        public const string OptimizeVenomPositionalsDesc = "config.job.vpr.optimize_venom_positionals_desc";
        public const string RoleActionsSection = "config.job.vpr.role_actions_section";
        public const string EnableSecondWind = "config.job.vpr.enable_second_wind";
        public const string SecondWindHpThreshold = "config.job.vpr.second_wind_hp_threshold";
        public const string EnableBloodbath = "config.job.vpr.enable_bloodbath";
        public const string BloodbathHpThreshold = "config.job.vpr.bloodbath_hp_threshold";
        public const string EnableFeint = "config.job.vpr.enable_feint";
        public const string EnableTrueNorth = "config.job.vpr.enable_true_north";
        public const string EnableBurstPooling = "config.job.vpr.enable_burst_pooling";
        public const string EnableBurstPoolingDesc = "config.job.vpr.enable_burst_pooling_desc";
    }

    // ===== RANGED PHYSICAL DPS =====

    /// <summary>Keys for shared ranged physical DPS settings.</summary>
    public static class RangedDps
    {
        public const string Header = "config.ranged_dps.header";
        public const string UtilitySection = "config.ranged_dps.utility_section";
        public const string UtilityDesc = "config.ranged_dps.utility_desc";
        public const string UtilityNote = "config.ranged_dps.utility_note";
        public const string InterruptLabel = "config.ranged_dps.interrupt_label";
        public const string HeadGraze = "config.ranged_dps.head_graze";
        public const string PartyMitLabel = "config.ranged_dps.party_mit_label";
        public const string Tactician = "config.ranged_dps.tactician";
        public const string ShieldSamba = "config.ranged_dps.shield_samba";
        public const string Troubadour = "config.ranged_dps.troubadour";
        public const string AoESection = "config.ranged_dps.aoe_section";
        public const string AoEDesc = "config.ranged_dps.aoe_desc";
        public const string AoEDefault = "config.ranged_dps.aoe_default";
        public const string AoENote = "config.ranged_dps.aoe_note";
        public const string BurstSection = "config.ranged_dps.burst_section";
        public const string BurstDesc = "config.ranged_dps.burst_desc";
        public const string BurstNote = "config.ranged_dps.burst_note";
        public const string PartyBuffs = "config.ranged_dps.party_buffs";
        public const string BattleVoice = "config.ranged_dps.battle_voice";
        public const string TechnicalFinish = "config.ranged_dps.technical_finish";
    }

    /// <summary>Keys for Machinist specific settings.</summary>
    public static class Machinist
    {
        public const string DamageSection = "config.job.mch.damage_section";
        public const string EnableDrill = "config.job.mch.enable_drill";
        public const string EnableDrillDesc = "config.job.mch.enable_drill_desc";
        public const string EnableChainSaw = "config.job.mch.enable_chain_saw";
        public const string EnableChainSawDesc = "config.job.mch.enable_chain_saw_desc";
        public const string EnableAirAnchor = "config.job.mch.enable_air_anchor";
        public const string EnableAirAnchorDesc = "config.job.mch.enable_air_anchor_desc";
        public const string EnableAoERotation = "config.job.mch.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.mch.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.mch.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.mch.aoe_min_targets_desc";
        public const string GaugeSection = "config.job.mch.gauge_section";
        public const string HeatLabel = "config.job.mch.heat_label";
        public const string HeatMinGauge = "config.job.mch.heat_min_gauge";
        public const string HeatMinGaugeDesc = "config.job.mch.heat_min_gauge_desc";
        public const string HeatOvercapThreshold = "config.job.mch.heat_overcap_threshold";
        public const string HeatOvercapThresholdDesc = "config.job.mch.heat_overcap_threshold_desc";
        public const string BatteryLabel = "config.job.mch.battery_label";
        public const string BatteryMinGauge = "config.job.mch.battery_min_gauge";
        public const string BatteryMinGaugeDesc = "config.job.mch.battery_min_gauge_desc";
        public const string BatteryOvercapThreshold = "config.job.mch.battery_overcap_threshold";
        public const string BatteryOvercapThresholdDesc = "config.job.mch.battery_overcap_threshold_desc";
        public const string QueenSection = "config.job.mch.queen_section";
        public const string EnableAutomatonQueen = "config.job.mch.enable_automaton_queen";
        public const string EnableAutomatonQueenDesc = "config.job.mch.enable_automaton_queen_desc";
        public const string EnableQueenOverdrive = "config.job.mch.enable_queen_overdrive";
        public const string EnableQueenOverdriveDesc = "config.job.mch.enable_queen_overdrive_desc";
        public const string SaveBatteryForBurst = "config.job.mch.save_battery_for_burst";
        public const string SaveBatteryForBurstDesc = "config.job.mch.save_battery_for_burst_desc";
        public const string BurstSection = "config.job.mch.burst_section";
        public const string EnableWildfire = "config.job.mch.enable_wildfire";
        public const string EnableWildfireDesc = "config.job.mch.enable_wildfire_desc";
        public const string WildfireHoldTime = "config.job.mch.wildfire_hold_time";
        public const string WildfireHoldTimeDesc = "config.job.mch.wildfire_hold_time_desc";
    }

    /// <summary>Keys for Bard specific settings.</summary>
    public static class Bard
    {
        public const string DamageSection = "config.job.brd.damage_section";
        public const string EnableApexArrow = "config.job.brd.enable_apex_arrow";
        public const string EnableApexArrowDesc = "config.job.brd.enable_apex_arrow_desc";
        public const string ApexArrowMinGauge = "config.job.brd.apex_arrow_min_gauge";
        public const string ApexArrowMinGaugeDesc = "config.job.brd.apex_arrow_min_gauge_desc";
        public const string EnableBlastArrow = "config.job.brd.enable_blast_arrow";
        public const string EnableBlastArrowDesc = "config.job.brd.enable_blast_arrow_desc";
        public const string EnableAoERotation = "config.job.brd.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.brd.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.brd.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.brd.aoe_min_targets_desc";
        public const string SongSection = "config.job.brd.song_section";
        public const string SongRotation = "config.job.brd.song_rotation";
        public const string SongRotationDesc = "config.job.brd.song_rotation_desc";
        public const string EnablePitchPerfect = "config.job.brd.enable_pitch_perfect";
        public const string EnablePitchPerfectDesc = "config.job.brd.enable_pitch_perfect_desc";
        public const string PitchPerfectMinStacks = "config.job.brd.pitch_perfect_min_stacks";
        public const string PitchPerfectMinStacksDesc = "config.job.brd.pitch_perfect_min_stacks_desc";
        public const string UsePitchPerfectEarly = "config.job.brd.use_pitch_perfect_early";
        public const string UsePitchPerfectEarlyDesc = "config.job.brd.use_pitch_perfect_early_desc";
        public const string DotSection = "config.job.brd.dot_section";
        public const string EnableCausticBite = "config.job.brd.enable_caustic_bite";
        public const string EnableCausticBiteDesc = "config.job.brd.enable_caustic_bite_desc";
        public const string EnableStormbite = "config.job.brd.enable_stormbite";
        public const string EnableStormBiteDesc = "config.job.brd.enable_stormbite_desc";
        public const string SpreadDots = "config.job.brd.spread_dots";
        public const string SpreadDotsDesc = "config.job.brd.spread_dots_desc";
        public const string DotRefreshThreshold = "config.job.brd.dot_refresh_threshold";
        public const string DotRefreshThresholdDesc = "config.job.brd.dot_refresh_threshold_desc";
        public const string BurstSection = "config.job.brd.burst_section";
        public const string EnableBattleVoice = "config.job.brd.enable_battle_voice";
        public const string EnableBattleVoiceDesc = "config.job.brd.enable_battle_voice_desc";
        public const string EnableRadiantFinale = "config.job.brd.enable_radiant_finale";
        public const string EnableRadiantFinaleDesc = "config.job.brd.enable_radiant_finale_desc";
        public const string BuffHoldTime = "config.job.brd.buff_hold_time";
        public const string BuffHoldTimeDesc = "config.job.brd.buff_hold_time_desc";
    }

    /// <summary>Keys for Dancer specific settings.</summary>
    public static class Dancer
    {
        public const string DamageSection = "config.job.dnc.damage_section";
        public const string EnableProcs = "config.job.dnc.enable_procs";
        public const string EnableProcsDesc = "config.job.dnc.enable_procs_desc";
        public const string EnableStarfallDance = "config.job.dnc.enable_starfall_dance";
        public const string EnableStarfallDanceDesc = "config.job.dnc.enable_starfall_dance_desc";
        public const string EnableTillana = "config.job.dnc.enable_tillana";
        public const string EnableTillanaDesc = "config.job.dnc.enable_tillana_desc";
        public const string EnableAoERotation = "config.job.dnc.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.dnc.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.dnc.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.dnc.aoe_min_targets_desc";
        public const string DanceSection = "config.job.dnc.dance_section";
        public const string EnableStandardStep = "config.job.dnc.enable_standard_step";
        public const string EnableStandardStepDesc = "config.job.dnc.enable_standard_step_desc";
        public const string EnableTechnicalStep = "config.job.dnc.enable_technical_step";
        public const string EnableTechnicalStepDesc = "config.job.dnc.enable_technical_step_desc";
        public const string DelayStandardForTechnical = "config.job.dnc.delay_standard_for_technical";
        public const string DelayStandardForTechnicalDesc = "config.job.dnc.delay_standard_for_technical_desc";
        public const string GaugeSection = "config.job.dnc.gauge_section";
        public const string EnableSaberDance = "config.job.dnc.enable_saber_dance";
        public const string EnableSaberDanceDesc = "config.job.dnc.enable_saber_dance_desc";
        public const string SaberDanceMinGauge = "config.job.dnc.saber_dance_min_gauge";
        public const string SaberDanceMinGaugeDesc = "config.job.dnc.saber_dance_min_gauge_desc";
        public const string EnableFanDance = "config.job.dnc.enable_fan_dance";
        public const string EnableFanDanceDesc = "config.job.dnc.enable_fan_dance_desc";
        public const string FanDanceMinFeathers = "config.job.dnc.fan_dance_min_feathers";
        public const string FanDanceMinFeathersDesc = "config.job.dnc.fan_dance_min_feathers_desc";
        public const string BurstSection = "config.job.dnc.burst_section";
        public const string EnableDevilment = "config.job.dnc.enable_devilment";
        public const string EnableDevilmentDesc = "config.job.dnc.enable_devilment_desc";
        public const string TechnicalHoldTime = "config.job.dnc.technical_hold_time";
        public const string TechnicalHoldTimeDesc = "config.job.dnc.technical_hold_time_desc";
    }

    // ===== CASTERS =====

    /// <summary>Keys for shared caster settings.</summary>
    public static class Caster
    {
        public const string Header = "config.caster.header";
        public const string MpSection = "config.caster.mp_section";
        public const string MpDesc = "config.caster.mp_desc";
        public const string LucidNote = "config.caster.lucid_note";
        public const string LucidDefault = "config.caster.lucid_default";
        public const string UtilitySection = "config.caster.utility_section";
        public const string UtilityDesc = "config.caster.utility_desc";
        public const string RaiseLabel = "config.caster.raise_label";
        public const string SummonerRaise = "config.caster.summoner_raise";
        public const string RedMageRaise = "config.caster.red_mage_raise";
        public const string PartyMitLabel = "config.caster.party_mit_label";
        public const string Addle = "config.caster.addle";
        public const string MagickBarrier = "config.caster.magick_barrier";
        public const string TemperaGrassa = "config.caster.tempera_grassa";
        public const string UtilityNote = "config.caster.utility_note";
        public const string BurstSection = "config.caster.burst_section";
        public const string BurstDesc = "config.caster.burst_desc";
        public const string PartyBuffs = "config.caster.party_buffs";
        public const string SearingLight = "config.caster.searing_light";
        public const string Embolden = "config.caster.embolden";
        public const string StarryMuse = "config.caster.starry_muse";
        public const string BurstNote = "config.caster.burst_note";
    }

    /// <summary>Keys for Black Mage specific settings.</summary>
    public static class BlackMage
    {
        public const string DamageSection = "config.job.blm.damage_section";
        public const string EnableXenoglossy = "config.job.blm.enable_xenoglossy";
        public const string EnableXenoglossyDesc = "config.job.blm.enable_xenoglossy_desc";
        public const string EnableDespair = "config.job.blm.enable_despair";
        public const string EnableDespairDesc = "config.job.blm.enable_despair_desc";
        public const string EnableFlareStar = "config.job.blm.enable_flare_star";
        public const string EnableFlareStarDesc = "config.job.blm.enable_flare_star_desc";
        public const string EnableAoERotation = "config.job.blm.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.blm.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.blm.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.blm.aoe_min_targets_desc";
        public const string PhaseSection = "config.job.blm.phase_section";
        public const string FireIVsBeforeDespair = "config.job.blm.fire_ivs_before_despair";
        public const string FireIVsBeforeDespairDesc = "config.job.blm.fire_ivs_before_despair_desc";
        public const string FireIVMinMp = "config.job.blm.fire_iv_min_mp";
        public const string FireIVMinMpDesc = "config.job.blm.fire_iv_min_mp_desc";
        public const string MovementSection = "config.job.blm.movement_section";
        public const string MovementPriority = "config.job.blm.movement_priority";
        public const string MovementPriorityDesc = "config.job.blm.movement_priority_desc";
        public const string SavePolyglotForMovement = "config.job.blm.save_polyglot_for_movement";
        public const string SavePolyglotForMovementDesc = "config.job.blm.save_polyglot_for_movement_desc";
        public const string PolyglotMovementReserve = "config.job.blm.polyglot_movement_reserve";
        public const string PolyglotMovementReserveDesc = "config.job.blm.polyglot_movement_reserve_desc";
        public const string EnableLeyLines = "config.job.blm.enable_ley_lines";
        public const string EnableLeyLinesDesc = "config.job.blm.enable_ley_lines_desc";
        public const string ThunderSection = "config.job.blm.thunder_section";
        public const string MaintainThunder = "config.job.blm.maintain_thunder";
        public const string MaintainThunderDesc = "config.job.blm.maintain_thunder_desc";
        public const string ThunderRefreshThreshold = "config.job.blm.thunder_refresh_threshold";
        public const string ThunderRefreshThresholdDesc = "config.job.blm.thunder_refresh_threshold_desc";
        public const string UseThunderheadImmediately = "config.job.blm.use_thunderhead_immediately";
        public const string UseThunderheadImmediatelyDesc = "config.job.blm.use_thunderhead_immediately_desc";
    }

    /// <summary>Keys for Summoner specific settings.</summary>
    public static class Summoner
    {
        public const string DamageSection = "config.job.smn.damage_section";
        public const string EnableRuinIV = "config.job.smn.enable_ruin_iv";
        public const string EnableRuinIVDesc = "config.job.smn.enable_ruin_iv_desc";
        public const string EnablePrimalAbilities = "config.job.smn.enable_primal_abilities";
        public const string EnablePrimalAbilitiesDesc = "config.job.smn.enable_primal_abilities_desc";
        public const string EnableAoERotation = "config.job.smn.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.smn.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.smn.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.smn.aoe_min_targets_desc";
        public const string PrimalSection = "config.job.smn.primal_section";
        public const string PrimalOrder = "config.job.smn.primal_order";
        public const string PrimalOrderDesc = "config.job.smn.primal_order_desc";
        public const string AdaptOrderForMovement = "config.job.smn.adapt_order_for_movement";
        public const string AdaptOrderForMovementDesc = "config.job.smn.adapt_order_for_movement_desc";
        public const string PrimalToggles = "config.job.smn.primal_toggles";
        public const string EnableIfrit = "config.job.smn.enable_ifrit";
        public const string EnableTitan = "config.job.smn.enable_titan";
        public const string EnableGaruda = "config.job.smn.enable_garuda";
        public const string DemiSection = "config.job.smn.demi_section";
        public const string EnableBahamut = "config.job.smn.enable_bahamut";
        public const string EnableBahamutDesc = "config.job.smn.enable_bahamut_desc";
        public const string EnablePhoenix = "config.job.smn.enable_phoenix";
        public const string EnablePhoenixDesc = "config.job.smn.enable_phoenix_desc";
        public const string EnableSolarBahamut = "config.job.smn.enable_solar_bahamut";
        public const string EnableSolarBahamutDesc = "config.job.smn.enable_solar_bahamut_desc";
        public const string EnableEnkindle = "config.job.smn.enable_enkindle";
        public const string EnableEnkindleDesc = "config.job.smn.enable_enkindle_desc";
        public const string BurstSection = "config.job.smn.burst_section";
        public const string EnableSearingLight = "config.job.smn.enable_searing_light";
        public const string EnableSearingLightDesc = "config.job.smn.enable_searing_light_desc";
        public const string AlignWithParty = "config.job.smn.align_with_party";
        public const string AlignWithPartyDesc = "config.job.smn.align_with_party_desc";
        public const string SearingLightHoldTime = "config.job.smn.searing_light_hold_time";
        public const string SearingLightHoldTimeDesc = "config.job.smn.searing_light_hold_time_desc";
    }

    /// <summary>Keys for Red Mage specific settings.</summary>
    public static class RedMage
    {
        public const string DamageSection = "config.job.rdm.damage_section";
        public const string EnableProcs = "config.job.rdm.enable_procs";
        public const string EnableProcsDesc = "config.job.rdm.enable_procs_desc";
        public const string EnableGrandImpact = "config.job.rdm.enable_grand_impact";
        public const string EnableGrandImpactDesc = "config.job.rdm.enable_grand_impact_desc";
        public const string EnableAoERotation = "config.job.rdm.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.rdm.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.rdm.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.rdm.aoe_min_targets_desc";
        public const string ManaSection = "config.job.rdm.mana_section";
        public const string StrictManaBalance = "config.job.rdm.strict_mana_balance";
        public const string StrictManaBalanceDesc = "config.job.rdm.strict_mana_balance_desc";
        public const string ManaImbalanceThreshold = "config.job.rdm.mana_imbalance_threshold";
        public const string ManaImbalanceThresholdDesc = "config.job.rdm.mana_imbalance_threshold_desc";
        public const string MeleeSection = "config.job.rdm.melee_section";
        public const string EnableMeleeCombo = "config.job.rdm.enable_melee_combo";
        public const string EnableMeleeComboDesc = "config.job.rdm.enable_melee_combo_desc";
        public const string EnableFinisherCombo = "config.job.rdm.enable_finisher_combo";
        public const string EnableFinisherComboDesc = "config.job.rdm.enable_finisher_combo_desc";
        public const string MeleeComboMinMana = "config.job.rdm.melee_combo_min_mana";
        public const string MeleeComboMinManaDesc = "config.job.rdm.melee_combo_min_mana_desc";
        public const string FinisherPreference = "config.job.rdm.finisher_preference";
        public const string FinisherPreferenceDesc = "config.job.rdm.finisher_preference_desc";
        public const string BurstSection = "config.job.rdm.burst_section";
        public const string EnableEmbolden = "config.job.rdm.enable_embolden";
        public const string EnableEmboldenDesc = "config.job.rdm.enable_embolden_desc";
        public const string EnableManafication = "config.job.rdm.enable_manafication";
        public const string EnableManaficationDesc = "config.job.rdm.enable_manafication_desc";
        public const string AlignWithParty = "config.job.rdm.align_with_party";
        public const string AlignWithPartyDesc = "config.job.rdm.align_with_party_desc";
        public const string EmboldenHoldTime = "config.job.rdm.embolden_hold_time";
        public const string EmboldenHoldTimeDesc = "config.job.rdm.embolden_hold_time_desc";
    }

    /// <summary>Keys for Pictomancer specific settings.</summary>
    public static class Pictomancer
    {
        public const string DamageSection = "config.job.pct.damage_section";
        public const string EnableHolyInWhite = "config.job.pct.enable_holy_in_white";
        public const string EnableHolyInWhiteDesc = "config.job.pct.enable_holy_in_white_desc";
        public const string EnableCometInBlack = "config.job.pct.enable_comet_in_black";
        public const string EnableCometInBlackDesc = "config.job.pct.enable_comet_in_black_desc";
        public const string EnableStarPrism = "config.job.pct.enable_star_prism";
        public const string EnableStarPrismDesc = "config.job.pct.enable_star_prism_desc";
        public const string EnableAoERotation = "config.job.pct.enable_aoe_rotation";
        public const string EnableAoERotationDesc = "config.job.pct.enable_aoe_rotation_desc";
        public const string AoEMinTargets = "config.job.pct.aoe_min_targets";
        public const string AoEMinTargetsDesc = "config.job.pct.aoe_min_targets_desc";
        public const string CanvasSection = "config.job.pct.canvas_section";
        public const string EnableCreatureMotif = "config.job.pct.enable_creature_motif";
        public const string EnableCreatureMotifDesc = "config.job.pct.enable_creature_motif_desc";
        public const string EnableWeaponMotif = "config.job.pct.enable_weapon_motif";
        public const string EnableWeaponMotifDesc = "config.job.pct.enable_weapon_motif_desc";
        public const string EnableLandscapeMotif = "config.job.pct.enable_landscape_motif";
        public const string EnableLandscapeMotifDesc = "config.job.pct.enable_landscape_motif_desc";
        public const string PrepaintMotifs = "config.job.pct.prepaint_motifs";
        public const string PrepaintMotifsDesc = "config.job.pct.prepaint_motifs_desc";
        public const string PrepaintOption = "config.job.pct.prepaint_option";
        public const string PrepaintOptionDesc = "config.job.pct.prepaint_option_desc";
        public const string MuseSection = "config.job.pct.muse_section";
        public const string EnableLivingMuse = "config.job.pct.enable_living_muse";
        public const string EnableLivingMuseDesc = "config.job.pct.enable_living_muse_desc";
        public const string EnableSteelMuse = "config.job.pct.enable_steel_muse";
        public const string EnableSteelMuseDesc = "config.job.pct.enable_steel_muse_desc";
        public const string EnableScenicMuse = "config.job.pct.enable_scenic_muse";
        public const string EnableScenicMuseDesc = "config.job.pct.enable_scenic_muse_desc";
        public const string BurstSection = "config.job.pct.burst_section";
        public const string EnableStarryMuse = "config.job.pct.enable_starry_muse";
        public const string EnableStarryMuseDesc = "config.job.pct.enable_starry_muse_desc";
        public const string AlignWithParty = "config.job.pct.align_with_party";
        public const string AlignWithPartyDesc = "config.job.pct.align_with_party_desc";
        public const string StarryMuseHoldTime = "config.job.pct.starry_muse_hold_time";
        public const string StarryMuseHoldTimeDesc = "config.job.pct.starry_muse_hold_time_desc";
        public const string EnableSubtractiveCombo = "config.job.pct.enable_subtractive_combo";
        public const string EnableSubtractiveComboDesc = "config.job.pct.enable_subtractive_combo_desc";
        public const string EnableRainbowDrip = "config.job.pct.enable_rainbow_drip";
        public const string EnableRainbowDripDesc = "config.job.pct.enable_rainbow_drip_desc";
        public const string EnableBurstPooling = "config.job.pct.enable_burst_pooling";
        public const string EnableBurstPoolingDesc = "config.job.pct.enable_burst_pooling_desc";
        public const string UseHammerDuringBurst = "config.job.pct.use_hammer_during_burst";
        public const string UseHammerDuringBurstDesc = "config.job.pct.use_hammer_during_burst_desc";
        public const string PaletteSection = "config.job.pct.palette_section";
        public const string HolyMinPalette = "config.job.pct.holy_min_palette";
        public const string HolyMinPaletteDesc = "config.job.pct.holy_min_palette_desc";
        public const string SavePaletteForComet = "config.job.pct.save_palette_for_comet";
        public const string SavePaletteForCometDesc = "config.job.pct.save_palette_for_comet_desc";
        public const string CreatureMotifOrder = "config.job.pct.creature_motif_order";
        public const string CreatureMotifOrderDesc = "config.job.pct.creature_motif_order_desc";
        public const string UtilitySection = "config.job.pct.utility_section";
        public const string EnableTemperaCoat = "config.job.pct.enable_tempera_coat";
        public const string EnableTemperaCoatDesc = "config.job.pct.enable_tempera_coat_desc";
        public const string EnableTemperaGrassa = "config.job.pct.enable_tempera_grassa";
        public const string EnableTemperaGrassaDesc = "config.job.pct.enable_tempera_grassa_desc";
        public const string MpManagementSection = "config.job.pct.mp_management_section";
        public const string EnableLucidDreaming = "config.job.pct.enable_lucid_dreaming";
        public const string EnableLucidDreamingDesc = "config.job.pct.enable_lucid_dreaming_desc";
        public const string LucidDreamingThreshold = "config.job.pct.lucid_dreaming_threshold";
        public const string LucidDreamingThresholdDesc = "config.job.pct.lucid_dreaming_threshold_desc";
    }

    #endregion

    #region Analytics Window (analytics.*)

    /// <summary>Keys for analytics window.</summary>
    public static class Analytics
    {
        // Window
        public const string WindowTitle = "analytics.window_title";

        // Tabs
        public const string RealtimeTab = "analytics.tab.realtime";
        public const string FightSummaryTab = "analytics.tab.fight_summary";
        public const string HistoryTab = "analytics.tab.history";
        public const string FFlogsTab = "analytics.tab.fflogs";

        // Legacy keys (kept for compatibility)
        public const string Realtime = "analytics.realtime";
        public const string History = "analytics.history";
        public const string FightSummary = "analytics.fight_summary";
        public const string FFLogs = "analytics.fflogs";

        // Settings dropdown
        public const string SectionVisibility = "analytics.section_visibility";
        public const string RealtimeTabLabel = "analytics.realtime_tab_label";
        public const string FightSummaryTabLabel = "analytics.fight_summary_tab_label";
        public const string HistoryTabLabel = "analytics.history_tab_label";
        public const string CombatStatus = "analytics.combat_status";
        public const string Metrics = "analytics.metrics";
        public const string Cooldowns = "analytics.cooldowns";
        public const string Scores = "analytics.scores";
        public const string Breakdown = "analytics.breakdown";
        public const string DowntimeAnalysis = "analytics.downtime_analysis";
        public const string Issues = "analytics.issues";
        public const string Sessions = "analytics.sessions";
        public const string Trends = "analytics.trends";
        public const string EnableTracking = "analytics.enable_tracking";

        // Realtime tab content
        public const string NotInCombat = "analytics.not_in_combat";
        public const string Status = "analytics.status";
        public const string Tracking = "analytics.tracking";
        public const string Idle = "analytics.idle";
        public const string Combat = "analytics.combat";
        public const string GcdUptime = "analytics.gcd_uptime";
        public const string ActionsPerMinute = "analytics.apm";
        public const string Dps = "analytics.dps";
        public const string Healing = "analytics.healing";
        public const string OverhealFormat = "analytics.overheal_format";
        public const string Deaths = "analytics.deaths";
        public const string NearDeaths = "analytics.near_deaths";
        public const string DamageDealt = "analytics.damage_dealt";
        public const string HealingDone = "analytics.healing_done";
        public const string Overheal = "analytics.overheal";
        public const string NoCooldowns = "analytics.no_cooldowns";
        public const string Ability = "analytics.ability";
        public const string Uses = "analytics.uses";
        public const string Efficiency = "analytics.efficiency";
        public const string Drift = "analytics.drift";
        public const string NA = "analytics.na";

        // Fight Summary tab
        public const string NoFightData = "analytics.no_fight_data";
        public const string CompleteCombat = "analytics.complete_combat";
        public const string LastFight = "analytics.last_fight";
        public const string Score = "analytics.score";
        public const string PerformanceScores = "analytics.performance_scores";
        public const string Category = "analytics.category";
        public const string Grade = "analytics.grade";
        public const string GcdUptimeScore = "analytics.gcd_uptime_score";
        public const string CooldownEff = "analytics.cooldown_eff";
        public const string HealingEff = "analytics.healing_eff";
        public const string Survival = "analytics.survival";
        public const string Overall = "analytics.overall";
        public const string DetailedBreakdown = "analytics.detailed_breakdown";
        public const string Metric = "analytics.metric";
        public const string Value = "analytics.value";
        public const string Duration = "analytics.duration";
        public const string TotalHealing = "analytics.total_healing";

        // Downtime Analysis
        public const string NoDowntime = "analytics.no_downtime";
        public const string TotalDowntime = "analytics.total_downtime";
        public const string Movement = "analytics.movement";
        public const string Mechanics = "analytics.mechanics";
        public const string Death = "analytics.death";
        public const string Unexplained = "analytics.unexplained";
        public const string MovementTooltip = "analytics.movement_tooltip";
        public const string MechanicsTooltip = "analytics.mechanics_tooltip";
        public const string DeathTooltip = "analytics.death_tooltip";
        public const string UnexplainedTooltip = "analytics.unexplained_tooltip";
        public const string TipUnexplained = "analytics.tip_unexplained";
        public const string TipMovement = "analytics.tip_movement";
        public const string Tip = "analytics.tip";

        // Cooldown Analysis
        public const string CooldownAnalysis = "analytics.cooldown_analysis";
        public const string NoCooldownData = "analytics.no_cooldown_data";
        public const string UsesFormat = "analytics.uses_format";
        public const string AvgDrift = "analytics.avg_drift";
        public const string MissedFormat = "analytics.missed_format";
        public const string Opener = "analytics.opener";
        public const string Burst = "analytics.burst";
        public const string Sustained = "analytics.sustained";
        public const string PerfectUsage = "analytics.perfect_usage";

        // Issues
        public const string IssuesFormat = "analytics.issues_format";
        public const string NoIssues = "analytics.no_issues";

        // History tab
        public const string PerformanceTrends = "analytics.performance_trends";
        public const string NeedSessionsForTrends = "analytics.need_sessions_for_trends";
        public const string SessionsCount = "analytics.sessions_count";
        public const string AvgScore = "analytics.avg_score";
        public const string AvgGcdUptime = "analytics.avg_gcd_uptime";
        public const string Trend = "analytics.trend";
        public const string SessionHistoryFormat = "analytics.session_history_format";
        public const string NoSessionsRecorded = "analytics.no_sessions_recorded";
        public const string BuildHistory = "analytics.build_history";
        public const string ClearHistory = "analytics.clear_history";
        public const string Time = "analytics.time";
        public const string GcdPercent = "analytics.gcd_percent";

        // FFLogs tab
        public const string FFlogsServiceNotInit = "analytics.fflogs_service_not_init";
        public const string FFlogsSetup = "analytics.fflogs_setup";
        public const string FFlogsIntro = "analytics.fflogs_intro";
        public const string FFlogsStep1 = "analytics.fflogs_step1";
        public const string FFlogsStep1Url = "analytics.fflogs_step1_url";
        public const string FFlogsStep1Create = "analytics.fflogs_step1_create";
        public const string FFlogsStep1Name = "analytics.fflogs_step1_name";
        public const string FFlogsStep1Redirect = "analytics.fflogs_step1_redirect";
        public const string FFlogsStep1Public = "analytics.fflogs_step1_public";
        public const string FFlogsStep2 = "analytics.fflogs_step2";
        public const string ClientId = "analytics.client_id";
        public const string ClientSecret = "analytics.client_secret";
        public const string SaveCredentials = "analytics.save_credentials";
        public const string CredentialsSaved = "analytics.credentials_saved";
        public const string CharacterBinding = "analytics.character_binding";
        public const string CharacterBindingIntro = "analytics.character_binding_intro";
        public const string CharacterName = "analytics.character_name";
        public const string Server = "analytics.server";
        public const string Region = "analytics.region";
        public const string BindCharacter = "analytics.bind_character";
        public const string LookingUpCharacter = "analytics.looking_up_character";
        public const string ChangeCredentials = "analytics.change_credentials";
        public const string Loading = "analytics.loading";
        public const string FFlogsIntegration = "analytics.fflogs_integration";
        public const string CharacterFormat = "analytics.character_format";
        public const string StatusConnectedPoints = "analytics.status_connected_points";
        public const string StatusConnected = "analytics.status_connected";
        public const string StatusNotConnected = "analytics.status_not_connected";
        public const string Refresh = "analytics.refresh";
        public const string ChangeCharacter = "analytics.change_character";
        public const string LoadingRankings = "analytics.loading_rankings";
        public const string NoRankingsData = "analytics.no_rankings_data";
        public const string CurrentZone = "analytics.current_zone";
        public const string AllStarsFormat = "analytics.all_stars_format";
        public const string NoAllStars = "analytics.no_all_stars";
        public const string EncounterRankings = "analytics.encounter_rankings";
        public const string NoEncounterData = "analytics.no_encounter_data";
        public const string Boss = "analytics.boss";
        public const string Best = "analytics.best";
        public const string Median = "analytics.median";
        public const string Kills = "analytics.kills";
        public const string ErrorPrefix = "analytics.error_prefix";
        public const string ConnectionSuccessful = "analytics.connection_successful";
        public const string ConnectionFailed = "analytics.connection_failed";
        public const string CharacterFound = "analytics.character_found";
        public const string CharacterNotFound = "analytics.character_not_found";
    }

    #endregion

    #region Training Window (training.*)

    /// <summary>Keys for training window.</summary>
    public static class Training
    {
        public const string WindowTitle = "training.window_title";
        public const string LiveCoaching = "training.live_coaching";
        public const string Lessons = "training.lessons";
        public const string Quizzes = "training.quizzes";
        public const string SkillProgress = "training.skill_progress";
        public const string Recommendations = "training.recommendations";
        public const string Settings = "training.settings";
        public const string EnableHints = "training.enable_hints";
        public const string Personality = "training.personality";
        public const string Encouraging = "training.personality.encouraging";
        public const string Analytical = "training.personality.analytical";
        public const string Strict = "training.personality.strict";
        public const string Silent = "training.personality.silent";

        // Tab names
        public const string LiveCoachingTab = "training.tab.live_coaching";
        public const string RecommendedTab = "training.tab.recommended";
        public const string LessonsTab = "training.tab.lessons";
        public const string QuizzesTab = "training.tab.quizzes";
        public const string ProgressTab = "training.tab.progress";
        public const string SkillLevelTab = "training.tab.skill_level";

        // Header
        public const string EnableTrainingMode = "training.enable_training_mode";
        public const string EnableTrainingModeTooltip = "training.enable_training_mode_tooltip";
        public const string Clear = "training.clear";
        public const string ClearTooltip = "training.clear_tooltip";

        // Settings dropdown
        public const string DisplayOptions = "training.display_options";
        public const string ShowAlternatives = "training.show_alternatives";
        public const string ShowTips = "training.show_tips";
        public const string Verbosity = "training.verbosity";
        public const string VerbosityMinimal = "training.verbosity.minimal";
        public const string VerbosityNormal = "training.verbosity.normal";
        public const string VerbosityDetailed = "training.verbosity.detailed";
        public const string PriorityFilter = "training.priority_filter";
        public const string PriorityAll = "training.priority.all";
        public const string PriorityNormalPlus = "training.priority.normal_plus";
        public const string PriorityHighPlus = "training.priority.high_plus";
        public const string PriorityCriticalOnly = "training.priority.critical_only";
        public const string Sections = "training.sections";
        public const string SectionCurrentAction = "training.section.current_action";
        public const string SectionDecisionFactors = "training.section.decision_factors";
        public const string SectionAlternatives = "training.section.alternatives";
        public const string SectionTips = "training.section.tips";
        public const string SectionRecentHistory = "training.section.recent_history";

        // Coaching Hints
        public const string CoachingHintsHeader = "training.coaching_hints_header";
        public const string ShowCoachingHints = "training.show_coaching_hints";
        public const string ShowCoachingHintsTooltip = "training.show_coaching_hints_tooltip";
        public const string HintCooldown = "training.hint_cooldown";
        public const string HintDuration = "training.hint_duration";
        public const string CoachingPersonalityHeader = "training.coaching_personality_header";
        public const string PersonalityTooltip = "training.personality_tooltip";

        // Progress tab
        public const string LearningProgress = "training.learning_progress";
        public const string ConceptsFormat = "training.concepts_format";
        public const string ProgressFormat = "training.progress_format";
        public const string RecentlySeen = "training.recently_seen";
        public const string Learned = "training.learned";
        public const string MarkLearned = "training.mark_learned";
        public const string NeedsReview = "training.needs_review";

        // Live Coaching tab
        public const string InCombatActive = "training.in_combat_active";
        public const string WaitingForCombat = "training.waiting_for_combat";
        public const string DecisionsLabel = "training.decisions_label";
        public const string DecisionsCapturedFormat = "training.decisions_captured_format";
        public const string ValidationLabel = "training.validation_label";
        public const string ValidationTooltipFormat = "training.validation_tooltip_format";
        public const string SkillLevelLabel = "training.skill_level_label";
        public const string SkillLevelTooltip = "training.skill_level_tooltip";
        public const string CurrentDecision = "training.current_decision";
        public const string OptimalDecision = "training.optimal_decision";
        public const string AcceptableFormat = "training.acceptable_format";
        public const string SuboptimalFormat = "training.suboptimal_format";
        public const string Adaptive = "training.adaptive";
        public const string AdaptiveTooltipFormat = "training.adaptive_tooltip_format";
        public const string DecisionFactorsLabel = "training.decision_factors_label";
        public const string DetailsLabel = "training.details_label";
        public const string AlternativesConsidered = "training.alternatives_considered";
        public const string TipPrefix = "training.tip_prefix";
        public const string BetterFormat = "training.better_format";
        public const string RecentDecisions = "training.recent_decisions";
        public const string NoDecisionsYet = "training.no_decisions_yet";
        public const string WaitingForMoreDecisions = "training.waiting_for_more_decisions";
        public const string TimeColumn = "training.time_column";
        public const string CategoryColumn = "training.category_column";
        public const string ActionColumn = "training.action_column";
        public const string ReasonColumn = "training.reason_column";
        public const string SecondsAgo = "training.seconds_ago";
        public const string MinutesAgo = "training.minutes_ago";

        // Recommendations tab
        public const string BasedOnBoth = "training.based_on_both";
        public const string BasedOnMastery = "training.based_on_mastery";
        public const string BasedOnPerformance = "training.based_on_performance";
        public const string DismissedCountFormat = "training.dismissed_count_format";
        public const string ClearDismissed = "training.clear_dismissed";
        public const string ClearDismissedTooltip = "training.clear_dismissed_tooltip";
        public const string EnableRecommendations = "training.enable_recommendations";
        public const string EnableRecommendationsTooltip = "training.enable_recommendations_tooltip";
        public const string MaxRecommendations = "training.max_recommendations";
        public const string MaxRecommendationsTooltip = "training.max_recommendations_tooltip";
        public const string RecommendationsDisabled = "training.recommendations_disabled";
        public const string EnableAbove = "training.enable_above";
        public const string NoRecommendationsYet = "training.no_recommendations_yet";
        public const string CompleteForRecs = "training.complete_for_recs";
        public const string RecsTip = "training.recs_tip";
        public const string GenerateFromMastery = "training.generate_from_mastery";
        public const string Job = "training.job";
        public const string Generate = "training.generate";
        public const string GenerateTooltip = "training.generate_tooltip";
        public const string GenerateNoData = "training.generate_no_data";
        public const string PriorityHigh = "training.priority_high";
        public const string PriorityMedium = "training.priority_medium";
        public const string PriorityLow = "training.priority_low";
        public const string MasteryBadge = "training.mastery_badge";
        public const string IssuesPrefix = "training.issues_prefix";
        public const string StrugglingPrefix = "training.struggling_prefix";
        public const string ViewLesson = "training.view_lesson";
        public const string LessonTooltipFormat = "training.lesson_tooltip_format";
        public const string Complete = "training.complete";
        public const string CompleteTooltip = "training.complete_tooltip";
        public const string Dismiss = "training.dismiss";
        public const string DismissTooltip = "training.dismiss_tooltip";
        public const string IssueDeaths = "training.issue_deaths";
        public const string IssueNearDeaths = "training.issue_near_deaths";
        public const string IssueUnusedAbilities = "training.issue_unused_abilities";
        public const string IssueGcdDowntime = "training.issue_gcd_downtime";
        public const string IssueCooldownDrift = "training.issue_cooldown_drift";
        public const string IssueHighOverheal = "training.issue_high_overheal";
        public const string IssueCappedResources = "training.issue_capped_resources";

        // Lessons tab
        public const string NoLessonsAvailable = "training.no_lessons_available";
        public const string LessonsHeader = "training.lessons_header";
        public const string PrerequisitesNotMet = "training.prerequisites_not_met";
        public const string CompleteFirstFormat = "training.complete_first_format";
        public const string SelectLessonToView = "training.select_lesson_to_view";
        public const string LessonTitleFormat = "training.lesson_title_format";
        public const string Completed = "training.completed";
        public const string Locked = "training.locked";
        public const string ConceptProgressFormat = "training.concept_progress_format";
        public const string CompletePrerequisites = "training.complete_prerequisites";
        public const string Prerequisites = "training.prerequisites";
        public const string KeyPoints = "training.key_points";
        public const string Abilities = "training.abilities";
        public const string Tips = "training.tips";
        public const string ConceptsCovered = "training.concepts_covered";
        public const string Learn = "training.learn";
        public const string MarkIncomplete = "training.mark_incomplete";
        public const string MarkIncompleteTooltip = "training.mark_incomplete_tooltip";
        public const string MarkComplete = "training.mark_complete";
        public const string MarkCompleteTooltip = "training.mark_complete_tooltip";
        public const string LearnAllConceptsFormat = "training.learn_all_concepts_format";
        public const string LearningPath = "training.learning_path";
        public const string CompletedProgressFormat = "training.completed_progress_format";
        public const string RecommendedNext = "training.recommended_next";
        public const string StartThisLesson = "training.start_this_lesson";
        public const string AllLessonsComplete = "training.all_lessons_complete";
        public const string SkillBeginner = "training.skill_beginner";
        public const string SkillIntermediate = "training.skill_intermediate";
        public const string SkillAdvanced = "training.skill_advanced";
        public const string SkillUnknown = "training.skill_unknown";

        // Quizzes tab
        public const string NoQuizzesAvailable = "training.no_quizzes_available";
        public const string QuizzesHeader = "training.quizzes_header";
        public const string BestScoreFormat = "training.best_score_format";
        public const string Passed = "training.passed";
        public const string NotPassed = "training.not_passed";
        public const string LessonFormat = "training.lesson_format";
        public const string SelectAQuiz = "training.select_a_quiz";
        public const string QuizInstructions = "training.quiz_instructions";
        public const string Recommended = "training.recommended";
        public const string StartFormat = "training.start_format";
        public const string AllQuizzesPassed = "training.all_quizzes_passed";
        public const string QuestionOfFormat = "training.question_of_format";
        public const string Scenario = "training.scenario";
        public const string Question = "training.question";
        public const string Previous = "training.previous";
        public const string Next = "training.next";
        public const string SubmitQuiz = "training.submit_quiz";
        public const string AnswerAllFirst = "training.answer_all_first";
        public const string Cancel = "training.cancel";
        public const string AnsweredFormat = "training.answered_format";
        public const string QuizPassed = "training.quiz_passed";
        public const string QuizNotPassed = "training.quiz_not_passed";
        public const string ScoreFormat = "training.score_format";
        public const string RequiredFormat = "training.required_format";
        public const string Results = "training.results";
        public const string ReviewAnswers = "training.review_answers";
        public const string RetryQuiz = "training.retry_quiz";
        public const string BackToList = "training.back_to_list";
        public const string ReviewFormat = "training.review_format";
        public const string Correct = "training.correct";
        public const string Incorrect = "training.incorrect";
        public const string Explanation = "training.explanation";
        public const string BackToResults = "training.back_to_results";
        public const string ExitQuiz = "training.exit_quiz";

        // Skill Progress tab - Adaptive Explanations
        public const string AdaptiveExplanations = "training.adaptive_explanations";
        public const string EnableAdaptiveVerbosity = "training.enable_adaptive_verbosity";
        public const string AdaptiveVerbosityTooltip = "training.adaptive_verbosity_tooltip";
        public const string SkillLevelOverride = "training.skill_level_override";
        public const string AutoDetect = "training.auto_detect";
        public const string Beginner = "training.beginner";
        public const string Intermediate = "training.intermediate";
        public const string Advanced = "training.advanced";
        public const string SkillOverrideTooltip = "training.skill_override_tooltip";

        // Skill Progress tab - Focus Areas
        public const string FocusAreasFormat = "training.focus_areas_format";
        public const string ConceptSuccessFormat = "training.concept_success_format";
        public const string Study = "training.study";
        public const string OpenLessonTooltip = "training.open_lesson_tooltip";
        public const string AndMoreFormat = "training.and_more_format";
        public const string RecentlyMasteredFormat = "training.recently_mastered_format";
        public const string PlayToBuildMastery = "training.play_to_build_mastery";

        // Skill Progress tab - Job Skill Levels
        public const string SkillLevelsByJob = "training.skill_levels_by_job";
        public const string EnableAdaptiveToSeeSkills = "training.enable_adaptive_to_see_skills";
        public const string NoProgressYet = "training.no_progress_yet";
        public const string GoToLessonsTab = "training.go_to_lessons_tab";
        public const string OtherJobsNoProgress = "training.other_jobs_no_progress";
        public const string LevelFormat = "training.level_format";
        public const string ScoreValueFormat = "training.score_value_format";
        public const string ScoreBreakdown = "training.score_breakdown";
        public const string QuizPassRate = "training.quiz_pass_rate";
        public const string QuizPassRateTooltip = "training.quiz_pass_rate_tooltip";
        public const string QuizQuality = "training.quiz_quality";
        public const string QuizQualityTooltip = "training.quiz_quality_tooltip";
        public const string LessonsCompletedScore = "training.lessons_completed_score";
        public const string LessonsCompletedTooltip = "training.lessons_completed_tooltip";
        public const string ConceptsLearnedScore = "training.concepts_learned_score";
        public const string ConceptsLearnedTooltip = "training.concepts_learned_tooltip";
        public const string ConceptMasteryScore = "training.concept_mastery_score";
        public const string ConceptMasteryTooltip = "training.concept_mastery_tooltip";
        public const string EngagementPenalty = "training.engagement_penalty";
        public const string EngagementPenaltyTooltip = "training.engagement_penalty_tooltip";
        public const string WeightFormat = "training.weight_format";
        public const string StrugglingFormat = "training.struggling_format";
        public const string MasteredFormat = "training.mastered_format";

        // Skill Progress tab - Mastery Details
        public const string ConceptMasteryDetails = "training.concept_mastery_details";
        public const string MasteredCountFormat = "training.mastered_count_format";
        public const string NeedsPracticeCountFormat = "training.needs_practice_count_format";
        public const string StudyThis = "training.study_this";
        public const string DevelopingFormat = "training.developing_format";
        public const string DevelopingTooltip = "training.developing_tooltip";
        public const string PlayMoreToBuildMastery = "training.play_more_to_build_mastery";

        // Skill Progress tab - Knowledge Retention
        public const string KnowledgeRetention = "training.knowledge_retention";
        public const string TrackKnowledgeRetention = "training.track_knowledge_retention";
        public const string RetentionTooltip = "training.retention_tooltip";
        public const string NeedsRelearningFormat = "training.needs_relearning_format";
        public const string RetentionFormat = "training.retention_format";
        public const string Review = "training.review";
        public const string LastPracticedTooltip = "training.last_practiced_tooltip";
        public const string DueForReviewFormat = "training.due_for_review_format";
        public const string RetentionDecliningTooltip = "training.retention_declining_tooltip";
        public const string FreshInMemory = "training.fresh_in_memory";
        public const string DaysUntilReview = "training.days_until_review";
        public const string ReviewSoon = "training.review_soon";
        public const string FreshConceptFormat = "training.fresh_concept_format";
        public const string SuggestedReviewQuizzes = "training.suggested_review_quizzes";
        public const string JobQuizFormat = "training.job_quiz_format";
        public const string TakeQuiz = "training.take_quiz";

        // Hint overlay
        public const string HintDismiss = "training.hint_dismiss";
        public const string HintEscToCloseAll = "training.hint_esc_to_close_all";
    }

    #endregion

    #region UI Helpers (ui.helpers.*)

    /// <summary>Keys for UI helper methods.</summary>
    public static class Helpers
    {
        public const string JobHeaderFormat = "ui.helpers.job_header_format";
        public const string Settings = "ui.helpers.settings";
    }

    #endregion

    #region Common (common.*)

    /// <summary>Keys for common UI elements.</summary>
    public static class Common
    {
        public const string Yes = "common.yes";
        public const string No = "common.no";
        public const string Ok = "common.ok";
        public const string Cancel = "common.cancel";
        public const string Save = "common.save";
        public const string Close = "common.close";
        public const string Apply = "common.apply";
        public const string Reset = "common.reset";
        public const string Default = "common.default";
        public const string Enabled = "common.enabled";
        public const string Disabled = "common.disabled";
        public const string On = "common.on";
        public const string Off = "common.off";
        public const string None = "common.none";
        public const string All = "common.all";
        public const string Auto = "common.auto";
        public const string Manual = "common.manual";
        public const string Loading = "common.loading";
        public const string Error = "common.error";
        public const string Warning = "common.warning";
        public const string Info = "common.info";
        public const string Success = "common.success";
    }

    #endregion

    #region Debug Window (debug.*)

    /// <summary>Keys for the debug window.</summary>
    public static class Debug
    {
        // Window
        public const string WindowTitle = "debug.window_title";
        public const string SectionVisibility = "debug.section_visibility";

        // Tab names
        public const string TabOverview = "debug.tab.overview";
        public const string TabWhyStuck = "debug.tab.why_stuck";
        public const string TabHealing = "debug.tab.healing";
        public const string TabOverheal = "debug.tab.overheal";
        public const string TabActions = "debug.tab.actions";
        public const string TabPerformance = "debug.tab.performance";
        public const string TabJobDetails = "debug.tab.job_details";
        public const string TabTimeline = "debug.tab.timeline";
        public const string TabChecklist = "debug.tab.checklist";
        public const string TabSmartAoE = "debug.tab.smart_aoe";

        // Job details fallbacks
        public const string NoDebugInfoForJob = "debug.job.no_debug_info";

        // Checklist tab
        public const string ResetCounts = "debug.reset_counts";

        // Section visibility labels
        public const string OverviewTabLabel = "debug.visibility.overview_tab";
        public const string WhyStuckTabLabel = "debug.visibility.why_stuck_tab";
        public const string HealingTabLabel = "debug.visibility.healing_tab";
        public const string OverhealTabLabel = "debug.visibility.overheal_tab";
        public const string ActionsTabLabel = "debug.visibility.actions_tab";
        public const string PerformanceTabLabel = "debug.visibility.performance_tab";

        // Section names (for visibility toggles)
        public const string GcdPlanning = "debug.section.gcd_planning";
        public const string QuickStats = "debug.section.quick_stats";
        public const string GcdPriorityChain = "debug.section.gcd_priority_chain";
        public const string OgcdState = "debug.section.ogcd_state";
        public const string DpsDetails = "debug.section.dps_details";
        public const string ResourcesSection = "debug.section.resources";
        public const string SpellStatus = "debug.section.spell_status";
        public const string SpellSelection = "debug.section.spell_selection";
        public const string HpPrediction = "debug.section.hp_prediction";
        public const string AoEHealing = "debug.section.aoe_healing";
        public const string RecentHeals = "debug.section.recent_heals";
        public const string ShadowHp = "debug.section.shadow_hp";
        public const string OverhealSummary = "debug.section.summary";
        public const string OverhealBySpell = "debug.section.by_spell";
        public const string OverhealByTarget = "debug.section.by_target";
        public const string OverhealTimeline = "debug.section.timeline";
        public const string OverhealControls = "debug.section.controls";
        public const string GcdDetails = "debug.section.gcd_details";
        public const string SpellUsage = "debug.section.spell_usage";
        public const string ActionHistory = "debug.section.action_history";
        public const string Statistics = "debug.section.statistics";
        public const string Downtime = "debug.section.downtime";

        // Overview tab
        public const string State = "debug.overview.state";
        public const string PlannedAction = "debug.overview.planned_action";
        public const string Dps = "debug.overview.dps";
        public const string Target = "debug.overview.target";
        public const string Attempts = "debug.overview.attempts";
        public const string SuccessLabel = "debug.overview.success";
        public const string Rate = "debug.overview.rate";
        public const string Gcd = "debug.overview.gcd";
        public const string Uptime = "debug.overview.uptime";
        public const string Gap = "debug.overview.gap";
        public const string Weave = "debug.overview.weave";
        public const string Last = "debug.overview.last";

        // Overview tab format strings
        public const string AttemptsFormat = "debug.overview.attempts_format";
        public const string SuccessFormat = "debug.overview.success_format";
        public const string RateFormat = "debug.overview.rate_format";
        public const string GcdFormat = "debug.overview.gcd_format";
        public const string UptimeFormat = "debug.overview.uptime_format";
        public const string GapFormat = "debug.overview.gap_format";
        public const string WeaveFormat = "debug.overview.weave_format";
        public const string LastFormat = "debug.overview.last_format";
        public const string Yes = "debug.common.yes";
        public const string No = "debug.common.no";

        // Why Stuck tab
        public const string CurrentState = "debug.why_stuck.current_state";
        public const string GcdReady = "debug.why_stuck.gcd_ready";
        public const string GcdStateFormat = "debug.why_stuck.gcd_state_format";
        public const string Priority = "debug.why_stuck.priority";
        public const string GcdPriorityChainHeader = "debug.why_stuck.gcd_priority_chain_header";
        public const string Category = "debug.why_stuck.category";
        public const string Esuna = "debug.why_stuck.esuna";
        public const string Raise = "debug.why_stuck.raise";
        public const string AoEHeal = "debug.why_stuck.aoe_heal";
        public const string SingleHeal = "debug.why_stuck.single_heal";
        public const string Regen = "debug.why_stuck.regen";
        public const string WeaveWindowOpen = "debug.why_stuck.weave_window_open";
        public const string NoWeaveWindow = "debug.why_stuck.no_weave_window";
        public const string Slots = "debug.why_stuck.slots";
        public const string ThinAir = "debug.why_stuck.thin_air";
        public const string Asylum = "debug.why_stuck.asylum";
        public const string Temperance = "debug.why_stuck.temperance";
        public const string Defensives = "debug.why_stuck.defensives";
        public const string Surecast = "debug.why_stuck.surecast";
        public const string SingleTarget = "debug.why_stuck.single_target";
        public const string AoE = "debug.why_stuck.aoe";
        public const string AfflatusMisery = "debug.why_stuck.afflatus_misery";
        public const string WhmResources = "debug.why_stuck.whm_resources";
        public const string Lily = "debug.why_stuck.lily";
        public const string SolaceRaptureAvailable = "debug.why_stuck.solace_rapture_available";
        public const string BloodLily = "debug.why_stuck.blood_lily";
        public const string MiseryReady = "debug.why_stuck.misery_ready";
        public const string LilyStrategy = "debug.why_stuck.lily_strategy";
        public const string SacredSight = "debug.why_stuck.sacred_sight";
        public const string SacredSightStacks = "debug.why_stuck.sacred_sight_stacks";
        public const string PriorityFormat = "debug.why_stuck.priority_format";
        public const string InjuredCountFormat = "debug.why_stuck.injured_count_format";
        public const string ActiveLabel = "debug.why_stuck.active";
        public const string CheckingLabel = "debug.why_stuck.checking";
        public const string DpsLabel = "debug.why_stuck.dps_label";
        public const string OgcdHeader = "debug.why_stuck.ogcd_header";
        public const string TypeHeader = "debug.why_stuck.type_header";
        public const string AoEEnemiesFormat = "debug.why_stuck.aoe_enemies_format";
        public const string LilyFormat = "debug.why_stuck.lily_format";
        public const string BloodLilyFormat = "debug.why_stuck.blood_lily_format";
        public const string LilyStrategyFormat = "debug.why_stuck.lily_strategy_format";
        public const string SacredSightFormat = "debug.why_stuck.sacred_sight_format";
        public const string SacredSightZero = "debug.why_stuck.sacred_sight_zero";

        // Actions tab
        public const string GcdStateDetails = "debug.actions.gcd_state_details";
        public const string CurrentStateLabel = "debug.actions.current_state";
        public const string GcdRemaining = "debug.actions.gcd_remaining";
        public const string AnimationLock = "debug.actions.animation_lock";
        public const string IsCasting = "debug.actions.is_casting";
        public const string CanExecuteGcd = "debug.actions.can_execute_gcd";
        public const string CanExecuteOgcd = "debug.actions.can_execute_ogcd";
        public const string WeaveSlots = "debug.actions.weave_slots";
        public const string LastAction = "debug.actions.last_action";
        public const string DebugFlags = "debug.actions.debug_flags";
        public const string GcdReadyDowntime = "debug.actions.gcd_ready_downtime";
        public const string IsActive = "debug.actions.is_active";
        public const string NoSpellsCastYet = "debug.actions.no_spells_cast_yet";
        public const string Filters = "debug.actions.filters";
        public const string SuccessFilter = "debug.actions.success_filter";
        public const string Failures = "debug.actions.failures";
        public const string Skips = "debug.actions.skips";
        public const string Clear = "debug.actions.clear";
        public const string Export = "debug.actions.export";
        public const string PropertyHeader = "debug.actions.property_header";
        public const string ValueHeader = "debug.actions.value_header";
        public const string YesUpper = "debug.common.yes_upper";

        // Healing tab
        public const string NotLoggedIn = "debug.healing.not_logged_in";
        public const string LevelFormat = "debug.healing.level_format";
        public const string GcdHealsSingle = "debug.healing.gcd_heals_single";
        public const string GcdHealsAoE = "debug.healing.gcd_heals_aoe";
        public const string GcdHealsHoT = "debug.healing.gcd_heals_hot";
        public const string OgcdHealsSingle = "debug.healing.ogcd_heals_single";
        public const string OgcdHealsAoE = "debug.healing.ogcd_heals_aoe";
        public const string GcdDamageSingle = "debug.healing.gcd_damage_single";
        public const string GcdDamageAoE = "debug.healing.gcd_damage_aoe";
        public const string GcdDoT = "debug.healing.gcd_dot";
        public const string Utility = "debug.healing.utility";
        public const string NoSelectionYet = "debug.healing.no_selection_yet";
        public const string TargetLabel = "debug.healing.target_label";
        public const string Missing = "debug.healing.missing";
        public const string Lilies = "debug.healing.lilies";
        public const string Selected = "debug.healing.selected";
        public const string Reason = "debug.healing.reason";
        public const string NoSpellSelected = "debug.healing.no_spell_selected";
        public const string Candidates = "debug.healing.candidates";
        public const string Spell = "debug.healing.spell";
        public const string Heal = "debug.healing.heal";
        public const string Eff = "debug.healing.eff";
        public const string Score = "debug.healing.score";
        public const string Status = "debug.healing.status";
        public const string Stats = "debug.healing.stats";
        public const string Rem = "debug.healing.rem";
        public const string Anim = "debug.healing.anim";
        public const string Casting = "debug.healing.casting";
        public const string LastCalc = "debug.healing.last_calc";
        public const string PendingHeals = "debug.healing.pending_heals";
        public const string Total = "debug.healing.total";
        public const string Injured = "debug.healing.injured";
        public const string SpellLabel = "debug.healing.spell_label";
        public const string PlayerHp = "debug.healing.player_hp";
        public const string Party = "debug.healing.party";
        public const string Valid = "debug.healing.valid";
        public const string Npcs = "debug.healing.npcs";
        public const string TotalLabel = "debug.healing.total_label";
        public const string NoHealsYet = "debug.healing.no_heals_yet";
        public const string ShadowHpTracking = "debug.healing.shadow_hp_tracking";
        public const string NoEntitiesTrackedYet = "debug.healing.no_entities_tracked_yet";
        public const string Entity = "debug.healing.entity";
        public const string GameHp = "debug.healing.game_hp";
        public const string ShadowHpLabel = "debug.healing.shadow_hp_label";
        public const string Delta = "debug.healing.delta";
        public const string NoneLabel = "debug.common.none";
        public const string SlotsFormat = "debug.healing.slots_format";
        public const string WeaveLabel = "debug.healing.weave_label";

        // Overheal tab
        public const string Summary = "debug.overheal.summary";
        public const string SessionDuration = "debug.overheal.session_duration";
        public const string TotalHealing = "debug.overheal.total_healing";
        public const string EffectiveHealing = "debug.overheal.effective_healing";
        public const string TotalOverheal = "debug.overheal.total_overheal";
        public const string OverhealPercent = "debug.overheal.overheal_percent";
        public const string BySpell = "debug.overheal.by_spell";
        public const string NoHealingDataYet = "debug.overheal.no_healing_data_yet";
        public const string Casts = "debug.overheal.casts";
        public const string TotalHeal = "debug.overheal.total_heal";
        public const string Overheal = "debug.overheal.overheal";
        public const string ByTarget = "debug.overheal.by_target";
        public const string Heals = "debug.overheal.heals";
        public const string RecentOverheals = "debug.overheal.recent_overheals";
        public const string NoOverhealsRecordedYet = "debug.overheal.no_overheals_recorded_yet";
        public const string Time = "debug.overheal.time";
        public const string ResetStatistics = "debug.overheal.reset_statistics";
        public const string ResetStatisticsDesc = "debug.overheal.reset_statistics_desc";

        // Performance tab
        public const string TopFail = "debug.performance.top_fail";
        public const string GcdReadyDowntimeLabel = "debug.performance.gcd_ready_downtime";
        public const string GcdActive = "debug.performance.gcd_active";
        public const string GcdStatusRow = "debug.performance.gcd_status_row";
        public const string RemShort = "debug.performance.rem";
        public const string CastShort = "debug.performance.cast";
        public const string AnimShort = "debug.performance.anim";
        public const string ActShort = "debug.performance.act";
        public const string DowntimeTracking = "debug.performance.downtime_tracking";
        public const string NoDowntimeEventsRecorded = "debug.performance.no_downtime_events_recorded";
        public const string DowntimeEvents = "debug.performance.downtime_events";
        public const string LastOccurrence = "debug.performance.last_occurrence";
        public const string SecondsAgoFormat = "debug.performance.seconds_ago";
        public const string LastReason = "debug.performance.last_reason";
        public const string CopyDebugInfo = "debug.performance.copy_debug_info";
        public const string DebugInfoHeader = "debug.performance.debug_info_header";
        public const string Timestamp = "debug.performance.timestamp";
        public const string StatisticsHeader = "debug.performance.statistics_header";
        public const string TotalAttempts = "debug.performance.total_attempts";
        public const string SuccessCount = "debug.performance.success_count";
        public const string SuccessRate = "debug.performance.success_rate";
        public const string GcdUptimeLabel = "debug.performance.gcd_uptime";
        public const string AvgCastGap = "debug.performance.avg_cast_gap";
        public const string GcdStateHeader = "debug.performance.gcd_state_header";
        public const string RotationStateHeader = "debug.performance.rotation_state_header";
        public const string PlanningState = "debug.performance.planning_state";
        public const string DpsState = "debug.performance.dps_state";
        public const string TargetInfo = "debug.performance.target_info";
        public const string HealingStateHeader = "debug.performance.healing_state_header";
        public const string AoEStatus = "debug.performance.aoe_status";
        public const string InjuredCount = "debug.performance.injured_count";
        public const string PartyList = "debug.performance.party_list";
        public const string ValidMembers = "debug.performance.valid_members";
        public const string ShadowHpHeader = "debug.performance.shadow_hp_header";

        // Timeline tab
        public const string TimelineNotAvailable = "debug.timeline.not_available";
        public const string SimulationControls = "debug.timeline.simulation_controls";
        public const string SimulationActive = "debug.timeline.simulation_active";
        public const string StopSimulation = "debug.timeline.stop_simulation";
        public const string TimeControls = "debug.timeline.time_controls";
        public const string ResetButton = "debug.timeline.reset";
        public const string SimulationInactive = "debug.timeline.simulation_inactive";
        public const string StartSimulation = "debug.timeline.start_simulation";
        public const string SimulationDescription = "debug.timeline.simulation_description";
        public const string TimelineState = "debug.timeline.timeline_state";
        public const string StatusLabel = "debug.timeline.status_label";
        public const string ActiveSimulating = "debug.timeline.active_simulating";
        public const string Active = "debug.timeline.active";
        public const string Inactive = "debug.timeline.inactive";
        public const string Fight = "debug.timeline.fight";
        public const string TimeLabel = "debug.timeline.time_label";
        public const string Phase = "debug.timeline.phase";
        public const string Confidence = "debug.timeline.confidence";
        public const string CurrentPredictions = "debug.timeline.current_predictions";
        public const string NoActiveTimeline = "debug.timeline.no_active_timeline";
        public const string Type = "debug.timeline.type";
        public const string Name = "debug.timeline.name";
        public const string In = "debug.timeline.in";
        public const string StatusHeader = "debug.timeline.status_header";
        public const string Raidwide = "debug.timeline.raidwide";
        public const string TankBuster = "debug.timeline.tank_buster";
        public const string UpcomingMechanics = "debug.timeline.upcoming_mechanics";
        public const string NoMechanicsInNext30s = "debug.timeline.no_mechanics_in_next_30s";
        public const string Imminent = "debug.timeline.imminent";
        public const string PreShield = "debug.timeline.pre_shield";
        public const string Prepare = "debug.timeline.prepare";
        public const string Upcoming = "debug.timeline.upcoming";

        // Job tab common
        public const string RotationNotActive = "debug.job.rotation_not_active";
        public const string SwitchToJob = "debug.job.switch_to_job";
        public const string Gauge = "debug.job.gauge";
        public const string Combo = "debug.job.combo";
        public const string Buffs = "debug.job.buffs";
        public const string Procs = "debug.job.procs";
        public const string Positional = "debug.job.positional";
        public const string Cooldowns = "debug.job.cooldowns";
        public const string Resources = "debug.job.resources";
        public const string ComboState = "debug.job.combo_state";
        public const string ComboTimer = "debug.job.combo_timer";
        public const string DoT = "debug.job.dot";
        public const string NotApplied = "debug.job.not_applied";
        public const string JobActiveLabel = "debug.job.active";
        public const string JobInactiveLabel = "debug.job.inactive";
        public const string Ready = "debug.job.ready";
        public const string OnCd = "debug.job.on_cd";
        public const string Position = "debug.job.position";
        public const string ImmuneOmni = "debug.job.immune_omni";
        public const string Rear = "debug.job.rear";
        public const string Flank = "debug.job.flank";
        public const string Front = "debug.job.front";
        public const string TrueNorth = "debug.job.true_north";
        public const string NearbyEnemies = "debug.job.nearby_enemies";
        public const string Enemies = "debug.job.enemies";
        public const string ValidLabel = "debug.job.valid";
        public const string PlayerHpLabel = "debug.job.player_hp";
        public const string PartyLabel = "debug.job.party";
        public const string SingleHealLabel = "debug.job.single_heal";
        public const string AoEHealLabel = "debug.job.aoe_heal";
        public const string PlannedActionLabel = "debug.job.planned_action";
        public const string DpsStateLabel = "debug.job.dps_state";
        public const string AoEDpsLabel = "debug.job.aoe_dps";
        public const string RaiseLabel = "debug.job.raise";
        public const string EsunaLabel = "debug.job.esuna";
        public const string InjuredFormat = "debug.job.injured_format";
        public const string EnemiesFormat = "debug.job.enemies_format";
        public const string HpFormat = "debug.job.hp_format";

        // Scholar tab
        public const string ScholarNotActive = "debug.scholar.not_active";
        public const string SwitchToScholar = "debug.scholar.switch_to";
        public const string Aetherflow = "debug.scholar.aetherflow";
        public const string FairyGauge = "debug.scholar.fairy_gauge";
        public const string Fairy = "debug.scholar.fairy";
        public const string FairyState = "debug.scholar.fairy_state";
        public const string FeyUnion = "debug.scholar.fey_union";
        public const string Seraph = "debug.scholar.seraph";
        public const string Dissipation = "debug.scholar.dissipation";
        public const string Healing = "debug.scholar.healing";
        public const string Lustrate = "debug.scholar.lustrate";
        public const string Indomitability = "debug.scholar.indomitability";
        public const string Excogitation = "debug.scholar.excogitation";
        public const string SacredSoil = "debug.scholar.sacred_soil";
        public const string Shields = "debug.scholar.shields";
        public const string EmergencyTactics = "debug.scholar.emergency_tactics";
        public const string Deployment = "debug.scholar.deployment";
        public const string Recitation = "debug.scholar.recitation";
        public const string LastHeal = "debug.scholar.last_heal";
        public const string DpsSection = "debug.scholar.dps";
        public const string ChainStratagem = "debug.scholar.chain_stratagem";
        public const string EnergyDrain = "debug.scholar.energy_drain";
        public const string LucidDreaming = "debug.scholar.lucid_dreaming";

        // Astrologian tab
        public const string AstrologianNotActive = "debug.astrologian.not_active";
        public const string SwitchToAstrologian = "debug.astrologian.switch_to";
        public const string Cards = "debug.astrologian.cards";
        public const string CardsInHand = "debug.astrologian.cards_in_hand";
        public const string DrawState = "debug.astrologian.draw_state";
        public const string PlayState = "debug.astrologian.play_state";
        public const string CurrentCard = "debug.astrologian.current_card";
        public const string MinorArcana = "debug.astrologian.minor_arcana";
        public const string Divination = "debug.astrologian.divination";
        public const string Oracle = "debug.astrologian.oracle";
        public const string EarthlyStar = "debug.astrologian.earthly_star";
        public const string StarState = "debug.astrologian.star_state";
        public const string TimeLeft = "debug.astrologian.time_left";
        public const string TargetsInRange = "debug.astrologian.targets_in_range";
        public const string EssentialDignity = "debug.astrologian.essential_dignity";
        public const string CelestialIntersection = "debug.astrologian.celestial_intersection";
        public const string CelestialOpposition = "debug.astrologian.celestial_opposition";
        public const string Exaltation = "debug.astrologian.exaltation";
        public const string Horoscope = "debug.astrologian.horoscope";
        public const string Macrocosmos = "debug.astrologian.macrocosmos";
        public const string NeutralSect = "debug.astrologian.neutral_sect";
        public const string Synastry = "debug.astrologian.synastry";
        public const string Lightspeed = "debug.astrologian.lightspeed";

        // Dragoon tab
        public const string DragoonNotActive = "debug.dragoon.not_active";
        public const string SwitchToDragoon = "debug.dragoon.switch_to";
        public const string DragonState = "debug.dragoon.dragon_state";
        public const string DragonEyes = "debug.dragoon.dragon_eyes";
        public const string FirstmindsFocus = "debug.dragoon.firstminds_focus";
        public const string PowerSurge = "debug.dragoon.power_surge";
        public const string LanceCharge = "debug.dragoon.lance_charge";
        public const string LifeSurge = "debug.dragoon.life_surge";
        public const string BattleLitany = "debug.dragoon.battle_litany";
        public const string RightEye = "debug.dragoon.right_eye";
        public const string DiveReady = "debug.dragoon.dive_ready";
        public const string NastrondReady = "debug.dragoon.nastrond_ready";
        public const string StardiverReady = "debug.dragoon.stardiver_ready";
        public const string StarcrossReady = "debug.dragoon.starcross_ready";
        public const string FangAndClaw = "debug.dragoon.fang_and_claw";
        public const string WheelInMotion = "debug.dragoon.wheel_in_motion";
        public const string DraconianFire = "debug.dragoon.draconian_fire";

        // Black Mage tab
        public const string BlackMageNotActive = "debug.black_mage.not_active";
        public const string SwitchToBlackMage = "debug.black_mage.switch_to";
        public const string ElementState = "debug.black_mage.element_state";
        public const string PhaseLabel = "debug.black_mage.phase";
        public const string Element = "debug.black_mage.element";
        public const string AstralFire = "debug.black_mage.astral_fire";
        public const string UmbralIce = "debug.black_mage.umbral_ice";
        public const string ElementTimer = "debug.black_mage.element_timer";
        public const string Enochian = "debug.black_mage.enochian";
        public const string Mp = "debug.black_mage.mp";
        public const string UmbralHearts = "debug.black_mage.umbral_hearts";
        public const string Polyglot = "debug.black_mage.polyglot";
        public const string AstralSoul = "debug.black_mage.astral_soul";
        public const string Paradox = "debug.black_mage.paradox";
        public const string Firestarter = "debug.black_mage.firestarter";
        public const string Thunderhead = "debug.black_mage.thunderhead";
        public const string LeyLines = "debug.black_mage.ley_lines";
        public const string Triplecast = "debug.black_mage.triplecast";
        public const string Swiftcast = "debug.black_mage.swiftcast";
        public const string TriplecastCharges = "debug.black_mage.triplecast_charges";
        public const string Manafont = "debug.black_mage.manafont";
        public const string Amplifier = "debug.black_mage.amplifier";
        public const string LeyLinesCd = "debug.black_mage.ley_lines_cd";
        public const string ThunderDoT = "debug.black_mage.thunder_dot";
        public const string TargetSection = "debug.black_mage.target_section";
        public const string StacksFormat = "debug.black_mage.stacks_format";

        // Ninja tab
        public const string NinjaNotActive = "debug.ninja.not_active";
        public const string SwitchToNinja = "debug.ninja.switch_to";
        public const string Mudra = "debug.ninja.mudra";
        public const string MudraActive = "debug.ninja.mudra_active";
        public const string InSequenceFormat = "debug.ninja.in_sequence_format";
        public const string Sequence = "debug.ninja.sequence";
        public const string PendingNinjutsu = "debug.ninja.pending_ninjutsu";
        public const string Kassatsu = "debug.ninja.kassatsu";
        public const string TenChiJin = "debug.ninja.ten_chi_jin";
        public const string TcjStacksFormat = "debug.ninja.tcj_stacks_format";
        public const string Ninki = "debug.ninja.ninki";
        public const string Kazematoi = "debug.ninja.kazematoi";
        public const string Suiton = "debug.ninja.suiton";
        public const string Bunshin = "debug.ninja.bunshin";
        public const string PhantomReady = "debug.ninja.phantom_ready";
        public const string RaijuReady = "debug.ninja.raiju_ready";
        public const string TenriJindoReady = "debug.ninja.tenri_jindo_ready";
        public const string TargetDebuffs = "debug.ninja.target_debuffs";
        public const string KunaisBane = "debug.ninja.kunais_bane";
        public const string Dokumori = "debug.ninja.dokumori";

        // Samurai tab
        public const string SamuraiNotActive = "debug.samurai.not_active";
        public const string SwitchToSamurai = "debug.samurai.switch_to";
        public const string Sen = "debug.samurai.sen";
        public const string Kenki = "debug.samurai.kenki";
        public const string Meditation = "debug.samurai.meditation";
        public const string Fugetsu = "debug.samurai.fugetsu";
        public const string Fuka = "debug.samurai.fuka";
        public const string MeikyoShisui = "debug.samurai.meikyo_shisui";
        public const string LastIaijutsu = "debug.samurai.last_iaijutsu";
        public const string TsubameReady = "debug.samurai.tsubame_ready";
        public const string OgiNamikiriReady = "debug.samurai.ogi_namikiri_ready";
        public const string KaeshiReady = "debug.samurai.kaeshi_ready";
        public const string ZanshinReady = "debug.samurai.zanshin_ready";
        public const string Higanbana = "debug.samurai.higanbana";
        public const string RemainingFormat = "debug.samurai.remaining_format";

        // Monk tab
        public const string MonkNotActive = "debug.monk.not_active";
        public const string SwitchToMonk = "debug.monk.switch_to";
        public const string Form = "debug.monk.form";
        public const string CurrentForm = "debug.monk.current_form";
        public const string PerfectBalance = "debug.monk.perfect_balance";
        public const string FormlessFist = "debug.monk.formless_fist";
        public const string ChakraLabel = "debug.monk.chakra";
        public const string BeastChakra = "debug.monk.beast_chakra";
        public const string LunarNadi = "debug.monk.lunar_nadi";
        public const string SolarNadi = "debug.monk.solar_nadi";
        public const string DisciplinedFist = "debug.monk.disciplined_fist";
        public const string LeadenFist = "debug.monk.leaden_fist";
        public const string RiddleOfFire = "debug.monk.riddle_of_fire";
        public const string Brotherhood = "debug.monk.brotherhood";
        public const string RiddleOfWind = "debug.monk.riddle_of_wind";
        public const string RaptorsFury = "debug.monk.raptors_fury";
        public const string CoeurlsFury = "debug.monk.coeurls_fury";
        public const string OpooposFury = "debug.monk.opoopos_fury";
        public const string FiresRumination = "debug.monk.fires_rumination";
        public const string WindsRumination = "debug.monk.winds_rumination";
        public const string Demolish = "debug.monk.demolish";

        // Reaper tab
        public const string ReaperNotActive = "debug.reaper.not_active";
        public const string SwitchToReaper = "debug.reaper.switch_to";
        public const string Soul = "debug.reaper.soul";
        public const string Shroud = "debug.reaper.shroud";
        public const string Enshroud = "debug.reaper.enshroud";
        public const string LemureShroud = "debug.reaper.lemure_shroud";
        public const string VoidShroud = "debug.reaper.void_shroud";
        public const string SoulReaver = "debug.reaper.soul_reaver";
        public const string DeathsDesign = "debug.reaper.deaths_design";
        public const string ArcaneCircle = "debug.reaper.arcane_circle";
        public const string ImmortalSacrifice = "debug.reaper.immortal_sacrifice";
        public const string BloodsownCircle = "debug.reaper.bloodsown_circle";
        public const string EnhancedGibbet = "debug.reaper.enhanced_gibbet";
        public const string EnhancedGallows = "debug.reaper.enhanced_gallows";
        public const string EnhancedVoid = "debug.reaper.enhanced_void";
        public const string EnhancedCross = "debug.reaper.enhanced_cross";
        public const string PerfectioParata = "debug.reaper.perfectio_parata";
        public const string Oblatio = "debug.reaper.oblatio";
        public const string Soulsow = "debug.reaper.soulsow";

        // Viper tab
        public const string ViperNotActive = "debug.viper.not_active";
        public const string SwitchToViper = "debug.viper.switch_to";
        public const string SerpentOffering = "debug.viper.serpent_offering";
        public const string RattlingCoils = "debug.viper.rattling_coils";
        public const string DreadCombo = "debug.viper.dread_combo";
        public const string Reawaken = "debug.viper.reawaken";
        public const string AnguineTribute = "debug.viper.anguine_tribute";
        public const string ReadyToReawaken = "debug.viper.ready_to_reawaken";
        public const string HuntersInstinct = "debug.viper.hunters_instinct";
        public const string Swiftscaled = "debug.viper.swiftscaled";
        public const string NoxiousGnash = "debug.viper.noxious_gnash";
        public const string HonedSteel = "debug.viper.honed_steel";
        public const string HonedReavers = "debug.viper.honed_reavers";
        public const string Venom = "debug.viper.venom";
        public const string ActiveVenom = "debug.viper.active_venom";
        public const string TwinfangReady = "debug.viper.twinfang_ready";
        public const string TwinbloodReady = "debug.viper.twinblood_ready";

        // Machinist tab
        public const string MachinistNotActive = "debug.machinist.not_active";
        public const string SwitchToMachinist = "debug.machinist.switch_to";
        public const string Heat = "debug.machinist.heat";
        public const string Battery = "debug.machinist.battery";
        public const string OverheatQueen = "debug.machinist.overheat_queen";
        public const string Overheated = "debug.machinist.overheated";
        public const string OverheatRemainingFormat = "debug.machinist.overheat_remaining_format";
        public const string QueenActive = "debug.machinist.queen_active";
        public const string QueenFormat = "debug.machinist.queen_format";
        public const string Reassemble = "debug.machinist.reassemble";
        public const string Hypercharged = "debug.machinist.hypercharged";
        public const string FullMetal = "debug.machinist.full_metal";
        public const string ExcavatorReady = "debug.machinist.excavator_ready";
        public const string Charges = "debug.machinist.charges";
        public const string Drill = "debug.machinist.drill";
        public const string GaussRound = "debug.machinist.gauss_round";
        public const string Ricochet = "debug.machinist.ricochet";
        public const string Wildfire = "debug.machinist.wildfire";
        public const string Bioblaster = "debug.machinist.bioblaster";

        // Bard tab
        public const string BardNotActive = "debug.bard.not_active";
        public const string SwitchToBard = "debug.bard.switch_to";
        public const string Song = "debug.bard.song";
        public const string CurrentSong = "debug.bard.current_song";
        public const string SongTimer = "debug.bard.song_timer";
        public const string Repertoire = "debug.bard.repertoire";
        public const string Coda = "debug.bard.coda";
        public const string SoulVoice = "debug.bard.soul_voice";
        public const string Bloodletter = "debug.bard.bloodletter";
        public const string HawksEye = "debug.bard.hawks_eye";
        public const string RagingStrikes = "debug.bard.raging_strikes";
        public const string BattleVoice = "debug.bard.battle_voice";
        public const string Barrage = "debug.bard.barrage";
        public const string RadiantFinale = "debug.bard.radiant_finale";
        public const string BlastArrow = "debug.bard.blast_arrow";
        public const string ResonantArrow = "debug.bard.resonant_arrow";
        public const string RadiantEncore = "debug.bard.radiant_encore";
        public const string DoTs = "debug.bard.dots";
        public const string CausticBite = "debug.bard.caustic_bite";
        public const string Stormbite = "debug.bard.stormbite";

        // Dancer tab
        public const string DancerNotActive = "debug.dancer.not_active";
        public const string SwitchToDancer = "debug.dancer.switch_to";
        public const string Dance = "debug.dancer.dance";
        public const string Dancing = "debug.dancer.dancing";
        public const string DancingFormat = "debug.dancer.dancing_format";
        public const string NextStep = "debug.dancer.next_step";
        public const string StandardFinish = "debug.dancer.standard_finish";
        public const string TechnicalFinish = "debug.dancer.technical_finish";
        public const string Esprit = "debug.dancer.esprit";
        public const string Feathers = "debug.dancer.feathers";
        public const string SilkenSymmetry = "debug.dancer.silken_symmetry";
        public const string SilkenFlow = "debug.dancer.silken_flow";
        public const string ThreefoldFan = "debug.dancer.threefold_fan";
        public const string FourfoldFan = "debug.dancer.fourfold_fan";
        public const string FlourishingFinish = "debug.dancer.flourishing_finish";
        public const string FlourishingStarfall = "debug.dancer.flourishing_starfall";
        public const string LastDance = "debug.dancer.last_dance";
        public const string FinishingMove = "debug.dancer.finishing_move";
        public const string DanceOfDawn = "debug.dancer.dance_of_dawn";
        public const string Devilment = "debug.dancer.devilment";
        public const string TargetPartner = "debug.dancer.target_partner";
        public const string DancePartner = "debug.dancer.dance_partner";

        // Summoner tab
        public const string SummonerNotActive = "debug.summoner.not_active";
        public const string SwitchToSummoner = "debug.summoner.switch_to";
        public const string DemiSummon = "debug.summoner.demi_summon";
        public const string SummonerPhase = "debug.summoner.phase";
        public const string ActiveDemi = "debug.summoner.active_demi";
        public const string BahamutFormat = "debug.summoner.bahamut_format";
        public const string PhoenixFormat = "debug.summoner.phoenix_format";
        public const string SolarBahamutFormat = "debug.summoner.solar_bahamut_format";
        public const string GcdsRemaining = "debug.summoner.gcds_remaining";
        public const string EnkindleUsed = "debug.summoner.enkindle_used";
        public const string AstralFlowUsed = "debug.summoner.astral_flow_used";
        public const string PrimalAttunement = "debug.summoner.primal_attunement";
        public const string Attunement = "debug.summoner.attunement";
        public const string AttunementFormat = "debug.summoner.attunement_format";
        public const string Available = "debug.summoner.available";
        public const string SearingLight = "debug.summoner.searing_light";
        public const string FurtherRuin = "debug.summoner.further_ruin";
        public const string IfritsFavor = "debug.summoner.ifrits_favor";
        public const string TitansFavor = "debug.summoner.titans_favor";
        public const string GarudasFavor = "debug.summoner.garudas_favor";
        public const string SummonerMp = "debug.summoner.mp";
        public const string SummonerAetherflow = "debug.summoner.aetherflow";
        public const string SearingLightCd = "debug.summoner.searing_light_cd";
        public const string SummonerEnergyDrain = "debug.summoner.energy_drain";
        public const string Enkindle = "debug.summoner.enkindle";
        public const string AstralFlow = "debug.summoner.astral_flow";
        public const string RadiantAegis = "debug.summoner.radiant_aegis";

        // Red Mage tab
        public const string RedMageNotActive = "debug.red_mage.not_active";
        public const string SwitchToRedMage = "debug.red_mage.switch_to";
        public const string Mana = "debug.red_mage.mana";
        public const string BlackMana = "debug.red_mage.black_mana";
        public const string WhiteMana = "debug.red_mage.white_mana";
        public const string Imbalance = "debug.red_mage.imbalance";
        public const string Balanced = "debug.red_mage.balanced";
        public const string ManaStacks = "debug.red_mage.mana_stacks";
        public const string MeleeReady = "debug.red_mage.melee_ready";
        public const string MeleeCombo = "debug.red_mage.melee_combo";
        public const string InCombo = "debug.red_mage.in_combo";
        public const string InComboFormat = "debug.red_mage.in_combo_format";
        public const string FinisherReady = "debug.red_mage.finisher_ready";
        public const string ScorchReady = "debug.red_mage.scorch_ready";
        public const string ResolutionReady = "debug.red_mage.resolution_ready";
        public const string Dualcast = "debug.red_mage.dualcast";
        public const string VerfireReady = "debug.red_mage.verfire_ready";
        public const string VerstoneReady = "debug.red_mage.verstone_ready";
        public const string Embolden = "debug.red_mage.embolden";
        public const string Manafication = "debug.red_mage.manafication";
        public const string Acceleration = "debug.red_mage.acceleration";
        public const string GrandImpact = "debug.red_mage.grand_impact";
        public const string Prefulgence = "debug.red_mage.prefulgence";
        public const string ThornedFlourish = "debug.red_mage.thorned_flourish";
        public const string Fleche = "debug.red_mage.fleche";
        public const string ContreSixte = "debug.red_mage.contre_sixte";
        public const string CorpsACorps = "debug.red_mage.corps_a_corps";
        public const string Engagement = "debug.red_mage.engagement";

        // Pictomancer tab
        public const string PictomancerNotActive = "debug.pictomancer.not_active";
        public const string SwitchToPictomancer = "debug.pictomancer.switch_to";
        public const string Canvas = "debug.pictomancer.canvas";
        public const string CreatureMotif = "debug.pictomancer.creature_motif";
        public const string CreatureCanvas = "debug.pictomancer.creature_canvas";
        public const string WeaponCanvas = "debug.pictomancer.weapon_canvas";
        public const string LandscapeCanvas = "debug.pictomancer.landscape_canvas";
        public const string MogPortrait = "debug.pictomancer.mog_portrait";
        public const string MadeenPortrait = "debug.pictomancer.madeen_portrait";
        public const string PalettePaint = "debug.pictomancer.palette_paint";
        public const string PaletteGauge = "debug.pictomancer.palette_gauge";
        public const string WhitePaint = "debug.pictomancer.white_paint";
        public const string BlackPaint = "debug.pictomancer.black_paint";
        public const string SubtractiveReady = "debug.pictomancer.subtractive_ready";
        public const string HammerCombo = "debug.pictomancer.hammer_combo";
        public const string HammerFormat = "debug.pictomancer.hammer_format";
        public const string NotInCombo = "debug.pictomancer.not_in_combo";
        public const string BaseCombo = "debug.pictomancer.base_combo";
        public const string SubtractiveFormat = "debug.pictomancer.subtractive_format";
        public const string StarryMuse = "debug.pictomancer.starry_muse";
        public const string Hyperphantasia = "debug.pictomancer.hyperphantasia";
        public const string Inspiration = "debug.pictomancer.inspiration";
        public const string SubtractiveSpectrum = "debug.pictomancer.subtractive_spectrum";
        public const string RainbowBright = "debug.pictomancer.rainbow_bright";
        public const string Starstruck = "debug.pictomancer.starstruck";
        public const string HammerTime = "debug.pictomancer.hammer_time";
        public const string StarryMuseCd = "debug.pictomancer.starry_muse_cd";
        public const string LivingMuse = "debug.pictomancer.living_muse";
        public const string StrikingMuse = "debug.pictomancer.striking_muse";
        public const string SubtractivePalette = "debug.pictomancer.subtractive_palette";
        public const string TemperaCoat = "debug.pictomancer.tempera_coat";
        public const string Smudge = "debug.pictomancer.smudge";

        // White Mage tab
        public const string WhiteMageNotActive = "debug.white_mage.not_active";
        public const string SwitchToWhiteMage = "debug.white_mage.switch_to";
        public const string WhmPlanningState = "debug.white_mage.planning_state";
        public const string WhmLilies = "debug.white_mage.lilies";
        public const string WhmBloodLily = "debug.white_mage.blood_lily";
        public const string WhmLilyStrategy = "debug.white_mage.lily_strategy";
        public const string WhmSacredSight = "debug.white_mage.sacred_sight";
        public const string WhmTemperance = "debug.white_mage.temperance";
        public const string WhmAssizes = "debug.white_mage.assizes";
        public const string WhmAsylum = "debug.white_mage.asylum";
        public const string WhmPoM = "debug.white_mage.pom";
        public const string WhmThinAir = "debug.white_mage.thin_air";
        public const string WhmSurecast = "debug.white_mage.surecast";
        public const string WhmDefensive = "debug.white_mage.defensive";
        public const string WhmMisery = "debug.white_mage.misery";

        // Sage tab
        public const string SageNotActive = "debug.sage.not_active";
        public const string SwitchToSage = "debug.sage.switch_to";
        public const string SgeAddersgall = "debug.sage.addersgall";
        public const string SgeAddersting = "debug.sage.addersting";
        public const string SgeStrategy = "debug.sage.strategy";
        public const string SgeKardia = "debug.sage.kardia";
        public const string SgeKardiaState = "debug.sage.kardia_state";
        public const string SgeSoteria = "debug.sage.soteria";
        public const string SgePhilosophia = "debug.sage.philosophia";
        public const string SgeEukrasia = "debug.sage.eukrasia";
        public const string SgeEukrasiaActive = "debug.sage.eukrasia_active";
        public const string SgeZoe = "debug.sage.zoe";
        public const string SgeEukrasiaState = "debug.sage.eukrasia_state";
        public const string SgeDruochole = "debug.sage.druochole";
        public const string SgeTaurochole = "debug.sage.taurochole";
        public const string SgeIxochole = "debug.sage.ixochole";
        public const string SgeKerachole = "debug.sage.kerachole";
        public const string SgePneuma = "debug.sage.pneuma";
        public const string SgeShields = "debug.sage.shields";
        public const string SgeHaima = "debug.sage.haima";
        public const string SgePanhaima = "debug.sage.panhaima";
        public const string SgeEukrasianDiagnosis = "debug.sage.eukrasian_diagnosis";
        public const string SgeEukrasianPrognosis = "debug.sage.eukrasian_prognosis";
        public const string SgePhysisII = "debug.sage.physis_ii";
        public const string SgeHolos = "debug.sage.holos";
        public const string SgeKrasis = "debug.sage.krasis";
        public const string SgePepsis = "debug.sage.pepsis";
        public const string SgeRhizomata = "debug.sage.rhizomata";
        public const string SgePhlegma = "debug.sage.phlegma";
        public const string SgeToxikon = "debug.sage.toxikon";
        public const string SgePsyche = "debug.sage.psyche";

        // Tank common
        public const string TankStatus = "debug.tank.status";
        public const string IsMainTankLabel = "debug.tank.is_main_tank";
        public const string MainTankValue = "debug.tank.main_tank";
        public const string OffTankValue = "debug.tank.off_tank";
        public const string TankStance = "debug.tank.stance";
        public const string ActiveMitigationsLabel = "debug.tank.active_mitigations";
        public const string MitigationHeader = "debug.tank.mitigation";
        public const string ModuleStatesHeader = "debug.tank.module_states";
        public const string DamageStateLabel = "debug.tank.damage_state";
        public const string MitigationStateLabel = "debug.tank.mitigation_state";
        public const string BuffStateLabel = "debug.tank.buff_state";
        public const string EnmityStateLabel = "debug.tank.enmity_state";
        public const string ExecutionFlowHeader = "debug.tank.execution_flow";

        // Warrior tab
        public const string WarriorNotActive = "debug.warrior.not_active";
        public const string SwitchToWarrior = "debug.warrior.switch_to";
        public const string BeastGaugeLabel = "debug.warrior.beast_gauge";
        public const string Defiance = "debug.warrior.defiance";
        public const string SurgingTempest = "debug.warrior.surging_tempest";
        public const string InnerRelease = "debug.warrior.inner_release";
        public const string InnerReleaseStacksLabel = "debug.warrior.inner_release_stacks";
        public const string NascentChaos = "debug.warrior.nascent_chaos";
        public const string PrimalRendReady = "debug.warrior.primal_rend_ready";
        public const string PrimalRuinationReady = "debug.warrior.primal_ruination_ready";

        // Dark Knight tab
        public const string DarkKnightNotActive = "debug.dark_knight.not_active";
        public const string SwitchToDarkKnight = "debug.dark_knight.switch_to";
        public const string BloodGaugeLabel = "debug.dark_knight.blood_gauge";
        public const string MpLabel = "debug.dark_knight.mp";
        public const string Grit = "debug.dark_knight.grit";
        public const string Darkside = "debug.dark_knight.darkside";
        public const string DarksideTimer = "debug.dark_knight.darkside_timer";
        public const string BloodWeapon = "debug.dark_knight.blood_weapon";
        public const string Delirium = "debug.dark_knight.delirium";
        public const string DeliriumStacksLabel = "debug.dark_knight.delirium_stacks";
        public const string ScornfulEdge = "debug.dark_knight.scornful_edge";
        public const string LivingShadow = "debug.dark_knight.living_shadow";
        public const string DarkArts = "debug.dark_knight.dark_arts";
        public const string LivingDead = "debug.dark_knight.living_dead";
        public const string WalkingDead = "debug.dark_knight.walking_dead";
        public const string TheBlackestNight = "debug.dark_knight.the_blackest_night";
        public const string ShadowWall = "debug.dark_knight.shadow_wall";
        public const string DarkMindBuff = "debug.dark_knight.dark_mind";
        public const string DrkOblation = "debug.dark_knight.oblation";
        public const string SaltedEarth = "debug.dark_knight.salted_earth";

        // Paladin tab
        public const string PaladinNotActive = "debug.paladin.not_active";
        public const string SwitchToPaladin = "debug.paladin.switch_to";
        public const string OathGaugeLabel = "debug.paladin.oath_gauge";
        public const string AtonementStepLabel = "debug.paladin.atonement_step";
        public const string ConfiteorStepLabel = "debug.paladin.confiteor_step";
        public const string FightOrFlight = "debug.paladin.fight_or_flight";
        public const string Requiescat = "debug.paladin.requiescat";
        public const string RequiescatStacksLabel = "debug.paladin.requiescat_stacks";
        public const string SwordOathStacksLabel = "debug.paladin.sword_oath_stacks";
        public const string GoringBlade = "debug.paladin.goring_blade";
        public const string InCombatLabel = "debug.paladin.in_combat";
        public const string CanExecuteGcdLabel = "debug.paladin.can_execute_gcd";
        public const string CanExecuteOgcdLabel = "debug.paladin.can_execute_ogcd";
        public const string GcdStateLabel = "debug.paladin.gcd_state";
        public const string GcdRemainingLabel = "debug.paladin.gcd_remaining";
        public const string ExecutionFlowLabel = "debug.paladin.execution_flow";

        // Gunbreaker tab
        public const string GunbreakerNotActive = "debug.gunbreaker.not_active";
        public const string SwitchToGunbreaker = "debug.gunbreaker.switch_to";
        public const string CartridgesLabel = "debug.gunbreaker.cartridges";
        public const string RoyalGuard = "debug.gunbreaker.royal_guard";
        public const string GnashingFangStepLabel = "debug.gunbreaker.gnashing_fang_step";
        public const string InGnashingFangCombo = "debug.gunbreaker.in_gnashing_fang_combo";
        public const string ReadyToRip = "debug.gunbreaker.ready_to_rip";
        public const string ReadyToTear = "debug.gunbreaker.ready_to_tear";
        public const string ReadyToGouge = "debug.gunbreaker.ready_to_gouge";
        public const string ReadyToBlast = "debug.gunbreaker.ready_to_blast";
        public const string ReadyToReign = "debug.gunbreaker.ready_to_reign";
        public const string NoMercy = "debug.gunbreaker.no_mercy";
        public const string Superbolide = "debug.gunbreaker.superbolide";
        public const string Nebula = "debug.gunbreaker.nebula";
        public const string HeartOfCorundum = "debug.gunbreaker.heart_of_corundum";
        public const string Camouflage = "debug.gunbreaker.camouflage";
        public const string Aurora = "debug.gunbreaker.aurora";
        public const string SonicBreak = "debug.gunbreaker.sonic_break";
        public const string BowShock = "debug.gunbreaker.bow_shock";
    }

    #endregion

    #region Overlay Window (ui.overlay.*)

    /// <summary>Keys for the overlay HUD window.</summary>
    public static class Overlay
    {
        public const string NoRotation = "ui.overlay.no_rotation";
        public const string StatusActive = "ui.overlay.status_active";
        public const string StatusInactive = "ui.overlay.status_inactive";
        public const string NextActionLabel = "ui.overlay.next_action";
        public const string NoAction = "ui.overlay.no_action";
        public const string HpLabel = "ui.overlay.hp_label";
        public const string PartyLabel = "ui.overlay.party_label";
        public const string PartyInjured = "ui.overlay.party_injured";
        public const string RaiseAlert = "ui.overlay.raise_alert";
        public const string PositionalLabel = "ui.overlay.positional";
        public const string Rear = "ui.overlay.rear";
        public const string Flank = "ui.overlay.flank";
        public const string Front = "ui.overlay.front";
        public const string Immune = "ui.overlay.immune";
        public const string HealingToggle = "ui.overlay.healing";
        public const string DamageToggle = "ui.overlay.damage";
    }

    #endregion

    #region Welcome Window (ui.welcome.*)

    /// <summary>Keys for the first-run welcome/onboarding window.</summary>
    public static class Welcome
    {
        public const string Title = "ui.welcome.title";
        public const string Subtitle = "ui.welcome.subtitle";
        public const string PageIndicator = "ui.welcome.page";
        public const string FeatureHealing = "ui.welcome.feature_healing";
        public const string FeatureDps = "ui.welcome.feature_dps";
        public const string FeaturePositionals = "ui.welcome.feature_positionals";
        public const string FeatureCoordination = "ui.welcome.feature_coordination";
        public const string SetupTitle = "ui.welcome.setup_title";
        public const string SetupNote = "ui.welcome.setup_note";
        public const string EnableRotation = "ui.welcome.enable_rotation";
        public const string EnableRotationDesc = "ui.welcome.enable_rotation_desc";
        public const string PresetLabel = "ui.welcome.preset_label";
        public const string PresetDesc = "ui.welcome.preset_desc";
        public const string ReadyTitle = "ui.welcome.ready_title";
        public const string ReadySubtitle = "ui.welcome.ready_subtitle";
        public const string TipCommand = "ui.welcome.tip_command";
        public const string TipOverlay = "ui.welcome.tip_overlay";
        public const string TipSettings = "ui.welcome.tip_settings";
        public const string OpenSettings = "ui.welcome.open_settings";
        public const string LetsGo = "ui.welcome.lets_go";
        public const string Back = "ui.welcome.back";
        public const string Next = "ui.welcome.next";
        public const string JoinDiscord = "ui.welcome.join_discord";
    }

    #endregion

    #region Party Coordination Settings (config.party_coord.*)

    /// <summary>Keys for Party Coordination settings.</summary>
    public static class PartyCoordination
    {
        public const string EnablePartyCoordination = "config.party_coord.enable";
        public const string EnablePartyCoordinationDesc = "config.party_coord.enable_desc";
        public const string CoordinationSection = "config.party_coord.coordination_section";
        public const string EnableCooldownCoordination = "config.party_coord.enable_cooldown_coordination";
        public const string EnableCooldownCoordinationDesc = "config.party_coord.enable_cooldown_coordination_desc";
        public const string EnableAoEHealCoordination = "config.party_coord.enable_aoe_heal_coordination";
        public const string EnableAoEHealCoordinationDesc = "config.party_coord.enable_aoe_heal_coordination_desc";
        public const string BroadcastMajorCooldowns = "config.party_coord.broadcast_major_cooldowns";
        public const string BroadcastMajorCooldownsDesc = "config.party_coord.broadcast_major_cooldowns_desc";
        public const string ConnectionSection = "config.party_coord.connection_section";
        public const string HeartbeatInterval = "config.party_coord.heartbeat_interval";
        public const string HeartbeatIntervalDesc = "config.party_coord.heartbeat_interval_desc";
        public const string InstanceTimeout = "config.party_coord.instance_timeout";
        public const string InstanceTimeoutDesc = "config.party_coord.instance_timeout_desc";
        public const string HealReservationExpiry = "config.party_coord.heal_reservation_expiry";
        public const string HealReservationExpiryDesc = "config.party_coord.heal_reservation_expiry_desc";
    }

    /// <summary>Keys for Changelog window.</summary>
    public static class Changelog
    {
        public const string NoChangelog = "Changelog.NoChangelog";
    }

    #endregion
}
