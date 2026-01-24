namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Services.Analytics;

/// <summary>
/// Represents a single action decision with its explanation.
/// </summary>
public sealed class ActionExplanation
{
    /// <summary>
    /// When this action was taken.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The action ID that was executed.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Human-readable name of the action.
    /// </summary>
    public string ActionName { get; init; } = string.Empty;

    /// <summary>
    /// Category of action (e.g., "Healing", "Damage", "Defensive", "Utility").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Target of the action (if applicable).
    /// </summary>
    public string? TargetName { get; init; }

    /// <summary>
    /// Brief reason for taking this action (shown in history list).
    /// Example: "Emergency heal - tank critical"
    /// </summary>
    public string ShortReason { get; init; } = string.Empty;

    /// <summary>
    /// Full explanation with numbers and context (shown when expanded).
    /// Example: "Tank was at 22% HP with high damage intake. Benediction was used as emergency heal."
    /// </summary>
    public string DetailedReason { get; init; } = string.Empty;

    /// <summary>
    /// Key factors that influenced this decision.
    /// Example: ["Tank HP: 22%", "Damage intake: 1200 DPS", "No other emergency heal available"]
    /// </summary>
    public string[] Factors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Alternative actions that were considered but not chosen.
    /// Example: ["Tetragrammaton (but slower)", "Cure II (but GCD)"]
    /// </summary>
    public string[] Alternatives { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Learning tip for this type of scenario.
    /// Example: "Benediction is best saved for emergencies, but don't let it sit unused!"
    /// </summary>
    public string? Tip { get; init; }

    /// <summary>
    /// Concept ID for tracking learning progress.
    /// Example: "whm.emergency_healing", "whm.ogcd_weaving"
    /// </summary>
    public string? ConceptId { get; init; }

    /// <summary>
    /// How important this explanation is to show.
    /// </summary>
    public ExplanationPriority Priority { get; init; } = ExplanationPriority.Normal;
}

/// <summary>
/// Tracks learning progress across concepts.
/// </summary>
public sealed class LearningProgress
{
    /// <summary>
    /// Total number of unique concepts available to learn.
    /// </summary>
    public int TotalConcepts { get; init; }

    /// <summary>
    /// Number of concepts marked as learned.
    /// </summary>
    public int LearnedConcepts { get; init; }

    /// <summary>
    /// Concepts that have been seen many times but not marked as learned.
    /// These may need extra attention.
    /// </summary>
    public string[] ConceptsNeedingAttention { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Recent concepts that were demonstrated in gameplay.
    /// </summary>
    public string[] RecentlyDemonstratedConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public float ProgressPercent => TotalConcepts > 0 ? (float)LearnedConcepts / TotalConcepts * 100f : 0f;
}

/// <summary>
/// Well-known concept IDs for WHM healing.
/// </summary>
public static class WhmConcepts
{
    // Emergency Healing
    public const string EmergencyHealing = "whm.emergency_healing";
    public const string BenedictionUsage = "whm.benediction_usage";
    public const string TetragrammatonUsage = "whm.tetragrammaton_usage";

    // Healing Priority
    public const string HealingPriority = "whm.healing_priority";
    public const string TankPriority = "whm.tank_priority";
    public const string PartyWideDamage = "whm.party_wide_damage";

    // oGCD Weaving
    public const string OgcdWeaving = "whm.ogcd_weaving";
    public const string AssizeUsage = "whm.assize_usage";
    public const string DivineBenisonUsage = "whm.divine_benison_usage";

    // Lily System
    public const string LilyManagement = "whm.lily_management";
    public const string AfflatusRaptureUsage = "whm.afflatus_rapture_usage";
    public const string AfflatusSolaceUsage = "whm.afflatus_solace_usage";
    public const string AfflatusMiseryTiming = "whm.afflatus_misery_timing";
    public const string BloodLilyBuilding = "whm.blood_lily_building";

    // Defensive Cooldowns
    public const string TemperanceUsage = "whm.temperance_usage";
    public const string AquaveilUsage = "whm.aquaveil_usage";
    public const string LiturgyOfTheBellUsage = "whm.liturgy_usage";

    // Proactive Healing
    public const string ProactiveHealing = "whm.proactive_healing";
    public const string RegenMaintenance = "whm.regen_maintenance";
    public const string ShieldTiming = "whm.shield_timing";

    // Damage Optimization
    public const string DpsOptimization = "whm.dps_optimization";
    public const string GlarePriority = "whm.glare_priority";
    public const string DotMaintenance = "whm.dot_maintenance";

    // Utility
    public const string EsunaUsage = "whm.esuna_usage";
    public const string RaiseDecision = "whm.raise_decision";

    // Coordination
    public const string CoHealerAwareness = "whm.cohealer_awareness";
    public const string PartyCoordination = "whm.party_coordination";

    /// <summary>
    /// All WHM concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        EmergencyHealing, BenedictionUsage, TetragrammatonUsage,
        HealingPriority, TankPriority, PartyWideDamage,
        OgcdWeaving, AssizeUsage, DivineBenisonUsage,
        LilyManagement, AfflatusRaptureUsage, AfflatusSolaceUsage, AfflatusMiseryTiming, BloodLilyBuilding,
        TemperanceUsage, AquaveilUsage, LiturgyOfTheBellUsage,
        ProactiveHealing, RegenMaintenance, ShieldTiming,
        DpsOptimization, GlarePriority, DotMaintenance,
        EsunaUsage, RaiseDecision,
        CoHealerAwareness, PartyCoordination,
    };
}

/// <summary>
/// Well-known concept IDs for SCH healing.
/// </summary>
public static class SchConcepts
{
    // Emergency Healing
    public const string EmergencyHealing = "sch.emergency_healing";
    public const string LustrateUsage = "sch.lustrate_usage";
    public const string ExcogitationUsage = "sch.excogitation_usage";

    // Aetherflow Management
    public const string AetherflowManagement = "sch.aetherflow_management";
    public const string AetherflowRefresh = "sch.aetherflow_refresh";
    public const string EnergyDrainUsage = "sch.energy_drain_usage";

    // Fairy Management
    public const string FairyManagement = "sch.fairy_management";
    public const string SeraphUsage = "sch.seraph_usage";
    public const string DissipationUsage = "sch.dissipation_usage";
    public const string FeyUnionUsage = "sch.fey_union_usage";
    public const string WhisperingDawnUsage = "sch.whispering_dawn_usage";
    public const string FeyIlluminationUsage = "sch.fey_illumination_usage";
    public const string FeyBlessingUsage = "sch.fey_blessing_usage";

    // Shield Economy
    public const string ShieldTiming = "sch.shield_timing";
    public const string AdloquiumUsage = "sch.adloquium_usage";
    public const string SuccorUsage = "sch.succor_usage";
    public const string DeploymentTactics = "sch.deployment_tactics";
    public const string EmergencyTacticsUsage = "sch.emergency_tactics_usage";
    public const string RecitationUsage = "sch.recitation_usage";

    // oGCD Healing
    public const string IndomitabilityUsage = "sch.indomitability_usage";
    public const string SacredSoilUsage = "sch.sacred_soil_usage";

    // Damage Optimization
    public const string DpsOptimization = "sch.dps_optimization";
    public const string ChainStratagemTiming = "sch.chain_stratagem_timing";
    public const string DotMaintenance = "sch.dot_maintenance";

    // Utility & Coordination
    public const string ExpedientUsage = "sch.expedient_usage";
    public const string RaiseDecision = "sch.raise_decision";
    public const string CoHealerAwareness = "sch.cohealer_awareness";
    public const string EsunaUsage = "sch.esuna_usage";

    /// <summary>
    /// All SCH concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        EmergencyHealing, LustrateUsage, ExcogitationUsage,
        AetherflowManagement, AetherflowRefresh, EnergyDrainUsage,
        FairyManagement, SeraphUsage, DissipationUsage, FeyUnionUsage,
        WhisperingDawnUsage, FeyIlluminationUsage, FeyBlessingUsage,
        ShieldTiming, AdloquiumUsage, SuccorUsage, DeploymentTactics,
        EmergencyTacticsUsage, RecitationUsage,
        IndomitabilityUsage, SacredSoilUsage,
        DpsOptimization, ChainStratagemTiming, DotMaintenance,
        ExpedientUsage, RaiseDecision, CoHealerAwareness, EsunaUsage,
    };
}

/// <summary>
/// Well-known concept IDs for AST healing.
/// </summary>
public static class AstConcepts
{
    // Emergency Healing
    public const string EmergencyHealing = "ast.emergency_healing";
    public const string EssentialDignityUsage = "ast.essential_dignity_usage";
    public const string MacrocosmosUsage = "ast.macrocosmos_usage";

    // Card Management
    public const string CardManagement = "ast.card_management";
    public const string DrawTiming = "ast.draw_timing";
    public const string MinorArcanaUsage = "ast.minor_arcana_usage";
    public const string AstrodyneBuilding = "ast.astrodyne_building";
    public const string DivinationTiming = "ast.divination_timing";
    public const string OracleUsage = "ast.oracle_usage";

    // HoT Economy
    public const string HotManagement = "ast.hot_management";
    public const string AspectedBeneficUsage = "ast.aspected_benefic_usage";
    public const string AspectedHeliosUsage = "ast.aspected_helios_usage";
    public const string CelestialOppositionUsage = "ast.celestial_opposition_usage";

    // Earthly Star
    public const string EarthlyStarPlacement = "ast.earthly_star_placement";
    public const string EarthlyStarMaturation = "ast.earthly_star_maturation";

    // oGCD Healing
    public const string CelestialIntersectionUsage = "ast.celestial_intersection_usage";
    public const string ExaltationUsage = "ast.exaltation_usage";
    public const string HoroscopeUsage = "ast.horoscope_usage";
    public const string SunSignUsage = "ast.sun_sign_usage";

    // Defensive Cooldowns
    public const string NeutralSectUsage = "ast.neutral_sect_usage";
    public const string CollectiveUnconsciousUsage = "ast.collective_unconscious_usage";

    // Damage & Utility
    public const string DpsOptimization = "ast.dps_optimization";
    public const string DotMaintenance = "ast.dot_maintenance";
    public const string RaiseDecision = "ast.raise_decision";
    public const string CoHealerAwareness = "ast.cohealer_awareness";
    public const string EsunaUsage = "ast.esuna_usage";
    public const string SynastryUsage = "ast.synastry_usage";
    public const string LightspeedUsage = "ast.lightspeed_usage";

    /// <summary>
    /// All AST concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        EmergencyHealing, EssentialDignityUsage, MacrocosmosUsage,
        CardManagement, DrawTiming, MinorArcanaUsage, AstrodyneBuilding,
        DivinationTiming, OracleUsage,
        HotManagement, AspectedBeneficUsage, AspectedHeliosUsage, CelestialOppositionUsage,
        EarthlyStarPlacement, EarthlyStarMaturation,
        CelestialIntersectionUsage, ExaltationUsage, HoroscopeUsage, SunSignUsage,
        NeutralSectUsage, CollectiveUnconsciousUsage,
        DpsOptimization, DotMaintenance, RaiseDecision, CoHealerAwareness,
        EsunaUsage, SynastryUsage, LightspeedUsage,
    };
}

/// <summary>
/// Well-known concept IDs for SGE healing.
/// </summary>
public static class SgeConcepts
{
    // Emergency Healing
    public const string EmergencyHealing = "sge.emergency_healing";
    public const string HaimaUsage = "sge.haima_usage";
    public const string PanhaimaUsage = "sge.panhaima_usage";
    public const string PepsisUsage = "sge.pepsis_usage";

    // Kardia Management
    public const string KardiaManagement = "sge.kardia_management";
    public const string KardiaTargetSelection = "sge.kardia_target_selection";
    public const string SoteriaUsage = "sge.soteria_usage";
    public const string PhilosophiaUsage = "sge.philosophia_usage";

    // Addersgall Economy
    public const string AddersgallManagement = "sge.addersgall_management";
    public const string KeracholeUsage = "sge.kerachole_usage";
    public const string IxocholeUsage = "sge.ixochole_usage";
    public const string TaurocholeUsage = "sge.taurochole_usage";
    public const string DruocholeUsage = "sge.druochole_usage";

    // Eukrasia Decisions
    public const string EukrasiaDecisions = "sge.eukrasia_decisions";
    public const string EukrasianDiagnosisUsage = "sge.eukrasian_diagnosis_usage";
    public const string EukrasianPrognosisUsage = "sge.eukrasian_prognosis_usage";
    public const string EukrasianDosisUsage = "sge.eukrasian_dosis_usage";

    // oGCD Healing
    public const string PhysisUsage = "sge.physis_usage";
    public const string HolosUsage = "sge.holos_usage";
    public const string PneumaUsage = "sge.pneuma_usage";
    public const string KrasisUsage = "sge.krasis_usage";

    // Defensive Cooldowns
    public const string ZoeUsage = "sge.zoe_usage";
    public const string RhizomataUsage = "sge.rhizomata_usage";

    // Damage & Utility
    public const string DpsOptimization = "sge.dps_optimization";
    public const string DotMaintenance = "sge.dot_maintenance";
    public const string PhlegmaUsage = "sge.phlegma_usage";
    public const string ToxikonUsage = "sge.toxikon_usage";
    public const string PsycheUsage = "sge.psyche_usage";
    public const string RaiseDecision = "sge.raise_decision";
    public const string CoHealerAwareness = "sge.cohealer_awareness";
    public const string EsunaUsage = "sge.esuna_usage";

    /// <summary>
    /// All SGE concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        EmergencyHealing, HaimaUsage, PanhaimaUsage, PepsisUsage,
        KardiaManagement, KardiaTargetSelection, SoteriaUsage, PhilosophiaUsage,
        AddersgallManagement, KeracholeUsage, IxocholeUsage, TaurocholeUsage, DruocholeUsage,
        EukrasiaDecisions, EukrasianDiagnosisUsage, EukrasianPrognosisUsage, EukrasianDosisUsage,
        PhysisUsage, HolosUsage, PneumaUsage, KrasisUsage,
        ZoeUsage, RhizomataUsage,
        DpsOptimization, DotMaintenance, PhlegmaUsage, ToxikonUsage, PsycheUsage,
        RaiseDecision, CoHealerAwareness, EsunaUsage,
    };
}

/// <summary>
/// Well-known concept IDs for PLD tanking.
/// </summary>
public static class PldConcepts
{
    // Core Mechanics (5)
    public const string OathGauge = "pld.oath_gauge";
    public const string FightOrFlight = "pld.fight_or_flight";
    public const string Requiescat = "pld.requiescat";
    public const string GoringBlade = "pld.goring_blade";
    public const string AtonementChain = "pld.atonement_chain";

    // Defensive (8)
    public const string HallowedGround = "pld.hallowed_ground";
    public const string Sentinel = "pld.sentinel";
    public const string Sheltron = "pld.sheltron";
    public const string Bulwark = "pld.bulwark";
    public const string DivineVeil = "pld.divine_veil";
    public const string Cover = "pld.cover";
    public const string PassageOfArms = "pld.passage_of_arms";
    public const string Clemency = "pld.clemency";

    // Damage (6)
    public const string HolySpirit = "pld.holy_spirit";
    public const string Confiteor = "pld.confiteor";
    public const string BladeCombo = "pld.blade_combo";
    public const string Expiacion = "pld.expiacion";
    public const string CircleOfScorn = "pld.circle_of_scorn";
    public const string Intervene = "pld.intervene";

    // Advanced (6)
    public const string MagicPhase = "pld.magic_phase";
    public const string BurstWindow = "pld.burst_window";
    public const string MitigationStacking = "pld.mitigation_stacking";
    public const string PartyProtection = "pld.party_protection";
    public const string TankSwap = "pld.tank_swap";
    public const string InvulnTiming = "pld.invuln_timing";

    /// <summary>
    /// All PLD concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        OathGauge, FightOrFlight, Requiescat, GoringBlade, AtonementChain,
        HallowedGround, Sentinel, Sheltron, Bulwark, DivineVeil, Cover, PassageOfArms, Clemency,
        HolySpirit, Confiteor, BladeCombo, Expiacion, CircleOfScorn, Intervene,
        MagicPhase, BurstWindow, MitigationStacking, PartyProtection, TankSwap, InvulnTiming,
    };
}

/// <summary>
/// Well-known concept IDs for WAR tanking.
/// </summary>
public static class WarConcepts
{
    // Core Mechanics (5)
    public const string BeastGauge = "war.beast_gauge";
    public const string SurgingTempest = "war.surging_tempest";
    public const string InnerRelease = "war.inner_release";
    public const string NascentChaos = "war.nascent_chaos";
    public const string Infuriate = "war.infuriate";

    // Defensive (8)
    public const string Holmgang = "war.holmgang";
    public const string Vengeance = "war.vengeance";
    public const string Bloodwhetting = "war.bloodwhetting";
    public const string ThrillOfBattle = "war.thrill_of_battle";
    public const string Equilibrium = "war.equilibrium";
    public const string ShakeItOff = "war.shake_it_off";
    public const string NascentFlash = "war.nascent_flash";
    public const string RawIntuition = "war.raw_intuition";

    // Damage (6)
    public const string FellCleave = "war.fell_cleave";
    public const string InnerChaos = "war.inner_chaos";
    public const string PrimalRend = "war.primal_rend";
    public const string Upheaval = "war.upheaval";
    public const string Onslaught = "war.onslaught";
    public const string Orogeny = "war.orogeny";

    // Advanced (6)
    public const string IRWindow = "war.ir_window";
    public const string GaugePooling = "war.gauge_pooling";
    public const string MitigationStacking = "war.mitigation_stacking";
    public const string PartyProtection = "war.party_protection";
    public const string TankSwap = "war.tank_swap";
    public const string InvulnTiming = "war.invuln_timing";

    /// <summary>
    /// All WAR concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        BeastGauge, SurgingTempest, InnerRelease, NascentChaos, Infuriate,
        Holmgang, Vengeance, Bloodwhetting, ThrillOfBattle, Equilibrium, ShakeItOff, NascentFlash, RawIntuition,
        FellCleave, InnerChaos, PrimalRend, Upheaval, Onslaught, Orogeny,
        IRWindow, GaugePooling, MitigationStacking, PartyProtection, TankSwap, InvulnTiming,
    };
}

/// <summary>
/// Well-known concept IDs for DRK tanking.
/// </summary>
public static class DrkConcepts
{
    // Core Mechanics (5)
    public const string BloodGauge = "drk.blood_gauge";
    public const string Darkside = "drk.darkside";
    public const string DarkArts = "drk.dark_arts";
    public const string BloodWeapon = "drk.blood_weapon";
    public const string Delirium = "drk.delirium";

    // Defensive (8)
    public const string LivingDead = "drk.living_dead";
    public const string TheBlackestNight = "drk.the_blackest_night";
    public const string ShadowWall = "drk.shadow_wall";
    public const string DarkMind = "drk.dark_mind";
    public const string Oblation = "drk.oblation";
    public const string DarkMissionary = "drk.dark_missionary";
    public const string LivingShadow = "drk.living_shadow";
    public const string WalkingDead = "drk.walking_dead";

    // Damage (6)
    public const string EdgeOfShadow = "drk.edge_of_shadow";
    public const string Bloodspiller = "drk.bloodspiller";
    public const string CarveAndSpit = "drk.carve_and_spit";
    public const string SaltedEarth = "drk.salted_earth";
    public const string Shadowbringer = "drk.shadowbringer";
    public const string Disesteem = "drk.disesteem";

    // Advanced (6)
    public const string TBNManagement = "drk.tbn_management";
    public const string DarksideMaintenance = "drk.darkside_maintenance";
    public const string MitigationStacking = "drk.mitigation_stacking";
    public const string PartyProtection = "drk.party_protection";
    public const string TankSwap = "drk.tank_swap";
    public const string InvulnTiming = "drk.invuln_timing";

    /// <summary>
    /// All DRK concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        BloodGauge, Darkside, DarkArts, BloodWeapon, Delirium,
        LivingDead, TheBlackestNight, ShadowWall, DarkMind, Oblation, DarkMissionary, LivingShadow, WalkingDead,
        EdgeOfShadow, Bloodspiller, CarveAndSpit, SaltedEarth, Shadowbringer, Disesteem,
        TBNManagement, DarksideMaintenance, MitigationStacking, PartyProtection, TankSwap, InvulnTiming,
    };
}

/// <summary>
/// Well-known concept IDs for GNB tanking.
/// </summary>
public static class GnbConcepts
{
    // Core Mechanics (5)
    public const string CartridgeGauge = "gnb.cartridge_gauge";
    public const string NoMercy = "gnb.no_mercy";
    public const string GnashingFang = "gnb.gnashing_fang";
    public const string Continuation = "gnb.continuation";
    public const string Bloodfest = "gnb.bloodfest";

    // Defensive (8)
    public const string Superbolide = "gnb.superbolide";
    public const string HeartOfCorundum = "gnb.heart_of_corundum";
    public const string Nebula = "gnb.nebula";
    public const string Camouflage = "gnb.camouflage";
    public const string Aurora = "gnb.aurora";
    public const string HeartOfLight = "gnb.heart_of_light";
    public const string GreatNebula = "gnb.great_nebula";
    public const string Trajectory = "gnb.trajectory";

    // Damage (6)
    public const string BurstStrike = "gnb.burst_strike";
    public const string DoubleDown = "gnb.double_down";
    public const string SonicBreak = "gnb.sonic_break";
    public const string BowShock = "gnb.bow_shock";
    public const string ReignOfBeasts = "gnb.reign_of_beasts";
    public const string BlastingZone = "gnb.blasting_zone";

    // Advanced (6)
    public const string NoMercyWindow = "gnb.no_mercy_window";
    public const string ContinuationChain = "gnb.continuation_chain";
    public const string MitigationStacking = "gnb.mitigation_stacking";
    public const string PartyProtection = "gnb.party_protection";
    public const string TankSwap = "gnb.tank_swap";
    public const string InvulnTiming = "gnb.invuln_timing";

    /// <summary>
    /// All GNB concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        CartridgeGauge, NoMercy, GnashingFang, Continuation, Bloodfest,
        Superbolide, HeartOfCorundum, Nebula, Camouflage, Aurora, HeartOfLight, GreatNebula, Trajectory,
        BurstStrike, DoubleDown, SonicBreak, BowShock, ReignOfBeasts, BlastingZone,
        NoMercyWindow, ContinuationChain, MitigationStacking, PartyProtection, TankSwap, InvulnTiming,
    };
}

/// <summary>
/// NIN (Hermes) Training Mode concepts covering Ninja mechanics.
/// 25 concepts across 7 categories.
/// </summary>
public static class NinConcepts
{
    // Core Mechanics (6)
    public const string NinkiGauge = "nin.ninki_gauge";
    public const string Kazematoi = "nin.kazematoi";
    public const string MudraSystem = "nin.mudra_system";
    public const string Huton = "nin.huton";
    public const string Suiton = "nin.suiton";
    public const string NinjutsuWeaving = "nin.ninjutsu_weaving";

    // Burst Window (5)
    public const string KunaisBane = "nin.kunais_bane";
    public const string TenChiJin = "nin.ten_chi_jin";
    public const string Kassatsu = "nin.kassatsu";
    public const string MugDokumori = "nin.mug_dokumori";
    public const string Bunshin = "nin.bunshin";

    // Combo & Positionals (4)
    public const string ComboBasics = "nin.combo_basics";
    public const string Positionals = "nin.positionals";
    public const string TrueNorthUsage = "nin.true_north_usage";
    public const string KazematoiManagement = "nin.kazematoi_management";

    // Procs & Raiju (3)
    public const string RaijuProcs = "nin.raiju_procs";
    public const string PhantomKamaitachi = "nin.phantom_kamaitachi";
    public const string TenriJindo = "nin.tenri_jindo";

    // Ninki Spenders (3)
    public const string Bhavacakra = "nin.bhavacakra";
    public const string Meisui = "nin.meisui";
    public const string NinkiPooling = "nin.ninki_pooling";

    // AoE Rotation (2)
    public const string AoeCombo = "nin.aoe_combo";
    public const string AoeNinjutsu = "nin.aoe_ninjutsu";

    // Advanced (2)
    public const string BurstAlignment = "nin.burst_alignment";
    public const string TcjOptimization = "nin.tcj_optimization";

    /// <summary>
    /// All NIN concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        NinkiGauge, Kazematoi, MudraSystem, Huton, Suiton, NinjutsuWeaving,
        // Burst Window
        KunaisBane, TenChiJin, Kassatsu, MugDokumori, Bunshin,
        // Combo & Positionals
        ComboBasics, Positionals, TrueNorthUsage, KazematoiManagement,
        // Procs & Raiju
        RaijuProcs, PhantomKamaitachi, TenriJindo,
        // Ninki Spenders
        Bhavacakra, Meisui, NinkiPooling,
        // AoE Rotation
        AoeCombo, AoeNinjutsu,
        // Advanced
        BurstAlignment, TcjOptimization,
    };
}

/// <summary>
/// SAM (Nike) Training Mode concepts covering Samurai mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class SamConcepts
{
    // Core Mechanics (6)
    public const string ComboBasics = "sam.combo_basics";
    public const string SenSystem = "sam.sen_system";
    public const string KenkiGauge = "sam.kenki_gauge";
    public const string Meditation = "sam.meditation";
    public const string FugetsuBuff = "sam.fugetsu_buff";
    public const string FukaBuff = "sam.fuka_buff";

    // Iaijutsu System (5)
    public const string IaijutsuSelection = "sam.iaijutsu_selection";
    public const string HiganbanaDoT = "sam.higanbana_dot";
    public const string MidareSetsugekka = "sam.midare_setsugekka";
    public const string TenkaGoken = "sam.tenka_goken";
    public const string TsubameGaeshi = "sam.tsubame_gaeshi";

    // Burst Window (5)
    public const string IkishotenBurst = "sam.ikishoten_burst";
    public const string OgiNamikiri = "sam.ogi_namikiri";
    public const string Zanshin = "sam.zanshin";
    public const string BurstAlignment = "sam.burst_alignment";
    public const string SeneiTiming = "sam.senei_timing";

    // Meikyo Shisui (3)
    public const string MeikyoShisui = "sam.meikyo_shisui";
    public const string MeikyoFinisherPriority = "sam.meikyo_finisher_priority";
    public const string MeikyoBuffRefresh = "sam.meikyo_buff_refresh";

    // Positionals (3)
    public const string Positionals = "sam.positionals";
    public const string TrueNorthUsage = "sam.true_north_usage";
    public const string PositionalRecovery = "sam.positional_recovery";

    // AoE & Advanced (3)
    public const string AoeRotation = "sam.aoe_rotation";
    public const string KenkiSpending = "sam.kenki_spending";
    public const string HagakureUsage = "sam.hagakure_usage";

    /// <summary>
    /// All SAM concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        ComboBasics, SenSystem, KenkiGauge, Meditation, FugetsuBuff, FukaBuff,
        // Iaijutsu System
        IaijutsuSelection, HiganbanaDoT, MidareSetsugekka, TenkaGoken, TsubameGaeshi,
        // Burst Window
        IkishotenBurst, OgiNamikiri, Zanshin, BurstAlignment, SeneiTiming,
        // Meikyo Shisui
        MeikyoShisui, MeikyoFinisherPriority, MeikyoBuffRefresh,
        // Positionals
        Positionals, TrueNorthUsage, PositionalRecovery,
        // AoE & Advanced
        AoeRotation, KenkiSpending, HagakureUsage,
    };
}

/// <summary>
/// MNK (Kratos) Training Mode concepts covering Monk mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class MnkConcepts
{
    // Core Mechanics (6)
    public const string ComboBasics = "mnk.combo_basics";
    public const string FormSystem = "mnk.form_system";
    public const string Positionals = "mnk.positionals";
    public const string DisciplinedFist = "mnk.disciplined_fist";
    public const string DemolishDot = "mnk.demolish_dot";
    public const string Meditation = "mnk.meditation";

    // Chakra System (4)
    public const string ChakraGauge = "mnk.chakra_gauge";
    public const string TheForbiddenChakra = "mnk.the_forbidden_chakra";
    public const string Enlightenment = "mnk.enlightenment";
    public const string SteelPeak = "mnk.steel_peak";

    // Beast Chakra & Blitz (5)
    public const string BeastChakra = "mnk.beast_chakra";
    public const string MasterfulBlitz = "mnk.masterful_blitz";
    public const string ElixirField = "mnk.elixir_field";
    public const string RisingPhoenix = "mnk.rising_phoenix";
    public const string PhantomRush = "mnk.phantom_rush";

    // Burst Window (4)
    public const string PerfectBalance = "mnk.perfect_balance";
    public const string RiddleOfFire = "mnk.riddle_of_fire";
    public const string Brotherhood = "mnk.brotherhood";
    public const string BurstAlignment = "mnk.burst_alignment";

    // Movement & Utility (3)
    public const string Thunderclap = "mnk.thunderclap";
    public const string TrueNorthUsage = "mnk.true_north_usage";
    public const string RiddleOfWind = "mnk.riddle_of_wind";

    // AoE Rotation (3)
    public const string AoeCombo = "mnk.aoe_combo";
    public const string HowlingFist = "mnk.howling_fist";
    public const string AoeThreshold = "mnk.aoe_threshold";

    /// <summary>
    /// All MNK concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        ComboBasics, FormSystem, Positionals, DisciplinedFist, DemolishDot, Meditation,
        // Chakra System
        ChakraGauge, TheForbiddenChakra, Enlightenment, SteelPeak,
        // Beast Chakra & Blitz
        BeastChakra, MasterfulBlitz, ElixirField, RisingPhoenix, PhantomRush,
        // Burst Window
        PerfectBalance, RiddleOfFire, Brotherhood, BurstAlignment,
        // Movement & Utility
        Thunderclap, TrueNorthUsage, RiddleOfWind,
        // AoE Rotation
        AoeCombo, HowlingFist, AoeThreshold,
    };
}

/// <summary>
/// RPR (Thanatos) Training Mode concepts covering Reaper mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class RprConcepts
{
    // Core Mechanics (6)
    public const string ComboBasics = "rpr.combo_basics";
    public const string SoulGauge = "rpr.soul_gauge";
    public const string SoulSlice = "rpr.soul_slice";
    public const string DeathsDesign = "rpr.deaths_design";
    public const string SoulReaver = "rpr.soul_reaver";
    public const string Positionals = "rpr.positionals";

    // Soul Reaver System (4)
    public const string Gibbet = "rpr.gibbet";
    public const string Gallows = "rpr.gallows";
    public const string Guillotine = "rpr.guillotine";
    public const string EnhancedProcs = "rpr.enhanced_procs";

    // Shroud & Enshroud (6)
    public const string ShroudGauge = "rpr.shroud_gauge";
    public const string Enshroud = "rpr.enshroud";
    public const string LemureShroud = "rpr.lemure_shroud";
    public const string VoidShroud = "rpr.void_shroud";
    public const string VoidReaping = "rpr.void_reaping";
    public const string GrimReaping = "rpr.grim_reaping";

    // Enshroud Finishers (4)
    public const string Communio = "rpr.communio";
    public const string Perfectio = "rpr.perfectio";
    public const string LemuresSlice = "rpr.lemures_slice";
    public const string Sacrificium = "rpr.sacrificium";

    // Party & Utility (5)
    public const string ArcaneCircle = "rpr.arcane_circle";
    public const string ImmortalSacrifice = "rpr.immortal_sacrifice";
    public const string PlentifulHarvest = "rpr.plentiful_harvest";
    public const string HarvestMoon = "rpr.harvest_moon";
    public const string AoeRotation = "rpr.aoe_rotation";

    /// <summary>
    /// All RPR concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        ComboBasics, SoulGauge, SoulSlice, DeathsDesign, SoulReaver, Positionals,
        // Soul Reaver System
        Gibbet, Gallows, Guillotine, EnhancedProcs,
        // Shroud & Enshroud
        ShroudGauge, Enshroud, LemureShroud, VoidShroud, VoidReaping, GrimReaping,
        // Enshroud Finishers
        Communio, Perfectio, LemuresSlice, Sacrificium,
        // Party & Utility
        ArcaneCircle, ImmortalSacrifice, PlentifulHarvest, HarvestMoon, AoeRotation,
    };
}

/// <summary>
/// DRG (Zeus) Training Mode concepts covering Dragoon mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class DrgConcepts
{
    // Core Mechanics (5)
    public const string EyeGauge = "drg.eye_gauge";
    public const string LifeOfDragon = "drg.life_of_dragon";
    public const string FirstmindsFocus = "drg.firstminds_focus";
    public const string PowerSurge = "drg.power_surge";
    public const string ComboBasics = "drg.combo_basics";

    // Burst Window (5)
    public const string LanceCharge = "drg.lance_charge";
    public const string BattleLitany = "drg.battle_litany";
    public const string LifeSurge = "drg.life_surge";
    public const string BurstWindow = "drg.burst_window";
    public const string BuffAlignment = "drg.buff_alignment";

    // Jump Management (5)
    public const string HighJump = "drg.high_jump";
    public const string MirageDive = "drg.mirage_dive";
    public const string SpineshatterDive = "drg.spineshatter_dive";
    public const string DragonfireDive = "drg.dragonfire_dive";
    public const string AnimationLock = "drg.animation_lock";

    // Life of the Dragon Phase (4)
    public const string Geirskogul = "drg.geirskogul";
    public const string Nastrond = "drg.nastrond";
    public const string Stardiver = "drg.stardiver";
    public const string LifeOptimization = "drg.life_optimization";

    // Positionals (3)
    public const string Positionals = "drg.positionals";
    public const string TrueNorthUsage = "drg.true_north_usage";
    public const string PositionalRecovery = "drg.positional_recovery";

    // Advanced (3)
    public const string WyrmwindThrust = "drg.wyrmwind_thrust";
    public const string DotMaintenance = "drg.dot_maintenance";
    public const string AoeRotation = "drg.aoe_rotation";

    /// <summary>
    /// All DRG concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        EyeGauge, LifeOfDragon, FirstmindsFocus, PowerSurge, ComboBasics,
        // Burst Window
        LanceCharge, BattleLitany, LifeSurge, BurstWindow, BuffAlignment,
        // Jump Management
        HighJump, MirageDive, SpineshatterDive, DragonfireDive, AnimationLock,
        // Life of the Dragon
        Geirskogul, Nastrond, Stardiver, LifeOptimization,
        // Positionals
        Positionals, TrueNorthUsage, PositionalRecovery,
        // Advanced
        WyrmwindThrust, DotMaintenance, AoeRotation,
    };
}

/// <summary>
/// Represents a lesson recommendation based on detected performance issues.
/// </summary>
public sealed class LessonRecommendation
{
    /// <summary>
    /// The lesson being recommended.
    /// </summary>
    public LessonDefinition Lesson { get; init; } = null!;

    /// <summary>
    /// Human-readable reason for the recommendation.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Priority score (0-100). Higher = more urgent to address.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// The performance issues that triggered this recommendation.
    /// </summary>
    public IssueType[] TriggeringIssues { get; init; } = Array.Empty<IssueType>();

    /// <summary>
    /// Priority level for display purposes.
    /// </summary>
    public string PriorityLevel => Priority switch
    {
        >= 80 => "HIGH",
        >= 60 => "MEDIUM",
        _ => "LOW"
    };
}

/// <summary>
/// Maps performance issues to relevant concept patterns for lesson recommendations.
/// </summary>
public static class IssueConceptMapping
{
    /// <summary>
    /// Maps IssueType to concept patterns that address that issue.
    /// Patterns use suffix matching (e.g., "emergency_healing" matches "whm.emergency_healing", "sch.emergency_healing").
    /// </summary>
    public static readonly IReadOnlyDictionary<IssueType, (string[] ConceptPatterns, int BasePriority, string ReasonTemplate)> Mappings =
        new Dictionary<IssueType, (string[], int, string)>
        {
            [IssueType.PartyDeath] = (
                new[] { "emergency_healing", "benediction", "lustrate", "essential_dignity", "haima" },
                90,
                "Party members died during the fight"),

            [IssueType.AbilityUnused] = (
                new[] { "ogcd_weaving", "lily_management", "aetherflow", "addersgall", "card_management" },
                80,
                "Key abilities went unused"),

            [IssueType.NearDeath] = (
                new[] { "proactive_healing", "tank_priority", "healing_priority", "shield_timing" },
                75,
                "Party members dropped to critical HP"),

            [IssueType.GcdDowntime] = (
                new[] { "dps_optimization", "glare_priority", "dot_maintenance", "kardia" },
                70,
                "GCD uptime was below optimal"),

            [IssueType.CooldownDrift] = (
                new[] { "ogcd_weaving", "assize", "divine_benison", "aetherflow_refresh", "earthly_star" },
                65,
                "Cooldowns drifted from optimal timing"),

            [IssueType.HighOverheal] = (
                new[] { "ogcd_weaving", "proactive_healing", "lily_management", "shield_timing" },
                60,
                "Overheal percentage was high"),

            [IssueType.ResourceCapped] = (
                new[] { "lily_management", "blood_lily", "aetherflow_management", "addersgall_management", "card_management" },
                55,
                "Resources were capped (lilies, aetherflow, etc.)"),
        };
}
