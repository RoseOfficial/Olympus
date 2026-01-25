namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Services.Analytics;

/// <summary>
/// Player skill level for adaptive explanations.
/// </summary>
public enum SkillLevel
{
    /// <summary>
    /// New to the job - detailed explanations for everything.
    /// </summary>
    Beginner,

    /// <summary>
    /// Familiar with basics - normal explanations, detailed for unfamiliar concepts.
    /// </summary>
    Intermediate,

    /// <summary>
    /// Proficient - minimal explanations, more detail only for new concepts.
    /// </summary>
    Advanced,
}

/// <summary>
/// Result of skill level detection with score breakdown.
/// </summary>
public sealed class SkillLevelResult
{
    /// <summary>
    /// The detected skill level.
    /// </summary>
    public SkillLevel Level { get; init; }

    /// <summary>
    /// Composite score (0-100) used to determine skill level.
    /// </summary>
    public float CompositeScore { get; init; }

    /// <summary>
    /// Quiz pass rate component (0-100). Weight: 40%.
    /// </summary>
    public float QuizPassRate { get; init; }

    /// <summary>
    /// Quiz quality component (average score on passed quizzes, 0-100). Weight: 25%.
    /// </summary>
    public float QuizQuality { get; init; }

    /// <summary>
    /// Lessons completed component (0-100). Weight: 25%.
    /// </summary>
    public float LessonsCompleted { get; init; }

    /// <summary>
    /// Concepts learned component (0-100). Weight: 10%.
    /// </summary>
    public float ConceptsLearned { get; init; }

    /// <summary>
    /// Whether an engagement penalty was applied (lessons without quizzes).
    /// </summary>
    public bool EngagementPenaltyApplied { get; init; }

    /// <summary>
    /// Total quizzes available for this job.
    /// </summary>
    public int TotalQuizzes { get; init; }

    /// <summary>
    /// Quizzes passed for this job.
    /// </summary>
    public int PassedQuizzes { get; init; }

    /// <summary>
    /// Total lessons available for this job.
    /// </summary>
    public int TotalLessons { get; init; }

    /// <summary>
    /// Lessons completed for this job.
    /// </summary>
    public int CompletedLessonsCount { get; init; }

    /// <summary>
    /// Description of the skill level for display.
    /// </summary>
    public string LevelDescription => Level switch
    {
        SkillLevel.Beginner => "Beginner - Learning the basics",
        SkillLevel.Intermediate => "Intermediate - Building proficiency",
        SkillLevel.Advanced => "Advanced - Mastering the job",
        _ => "Unknown",
    };

    /// <summary>
    /// Concept mastery score component (0-100). Weight: 25%.
    /// Added in v3.28.0.
    /// </summary>
    public float ConceptMastery { get; init; }
}

/// <summary>
/// Analysis of concept mastery for a job, categorizing concepts by proficiency.
/// </summary>
public sealed class ConceptMasteryResult
{
    /// <summary>
    /// Concepts with 10+ opportunities and >85% success rate.
    /// </summary>
    public string[] MasteredConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Concepts with 10+ opportunities and less than 60% success rate.
    /// </summary>
    public string[] StrugglingConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Concepts with less than 10 opportunities (not enough data).
    /// </summary>
    public string[] DevelopingConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Overall mastery score (0-100) based on success rates across all concepts.
    /// </summary>
    public float MasteryScore { get; init; }

    /// <summary>
    /// Total concepts tracked for this job.
    /// </summary>
    public int TotalConcepts { get; init; }

    /// <summary>
    /// Summary description for UI display.
    /// </summary>
    public string Summary => MasteredConcepts.Length switch
    {
        0 when TotalConcepts == 0 => "No concepts tracked yet",
        0 => $"Working on {DevelopingConcepts.Length} concepts",
        _ => $"{MasteredConcepts.Length} mastered, {StrugglingConcepts.Length} need practice",
    };
}

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
/// VPR (Echidna) Training Mode concepts covering Viper mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class VprConcepts
{
    // Core Resources (3)
    public const string SerpentOffering = "vpr.serpent_offering";
    public const string AnguineTribute = "vpr.anguine_tribute";
    public const string RattlingCoil = "vpr.rattling_coil";

    // Dual Wield System (5)
    public const string ComboBasics = "vpr.combo_basics";
    public const string BuffCycling = "vpr.buff_cycling";
    public const string HonedBuffs = "vpr.honed_buffs";
    public const string OfferingGeneration = "vpr.offering_generation";
    public const string DualWieldAoe = "vpr.dual_wield_aoe";

    // Venom & Positionals (4)
    public const string VenomSystem = "vpr.venom_system";
    public const string PositionalFinishers = "vpr.positional_finishers";
    public const string Positionals = "vpr.positionals";
    public const string TrueNorthUsage = "vpr.true_north_usage";

    // Twinblade System (4)
    public const string DreadCombo = "vpr.dread_combo";
    public const string Vicewinder = "vpr.vicewinder";
    public const string TwinfangTwinblood = "vpr.twinfang_twinblood";
    public const string NoxiousGnash = "vpr.noxious_gnash";

    // Reawaken Burst (5)
    public const string ReawakenEntry = "vpr.reawaken_entry";
    public const string GenerationSequence = "vpr.generation_sequence";
    public const string LegacyWeaving = "vpr.legacy_weaving";
    public const string BurstWindow = "vpr.burst_window";
    public const string ReadyToReawaken = "vpr.ready_to_reawaken";

    // Utility & Coordination (4)
    public const string SerpentsIre = "vpr.serpents_ire";
    public const string UncoiledFury = "vpr.uncoiled_fury";
    public const string TimelineAwareness = "vpr.timeline_awareness";
    public const string AoeRotation = "vpr.aoe_rotation";

    /// <summary>
    /// All VPR concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Resources
        SerpentOffering, AnguineTribute, RattlingCoil,
        // Dual Wield System
        ComboBasics, BuffCycling, HonedBuffs, OfferingGeneration, DualWieldAoe,
        // Venom & Positionals
        VenomSystem, PositionalFinishers, Positionals, TrueNorthUsage,
        // Twinblade System
        DreadCombo, Vicewinder, TwinfangTwinblood, NoxiousGnash,
        // Reawaken Burst
        ReawakenEntry, GenerationSequence, LegacyWeaving, BurstWindow, ReadyToReawaken,
        // Utility & Coordination
        SerpentsIre, UncoiledFury, TimelineAwareness, AoeRotation,
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
/// MCH (Prometheus) Training Mode concepts covering Machinist mechanics.
/// 25 concepts across 7 categories.
/// </summary>
public static class MchConcepts
{
    // Gauge Fundamentals (4)
    public const string HeatGauge = "mch.heat_gauge";
    public const string BatteryGauge = "mch.battery_gauge";
    public const string GaugeOvercapping = "mch.gauge_overcapping";
    public const string GaugeInteractions = "mch.gauge_interactions";

    // Hypercharge System (5)
    public const string HyperchargeActivation = "mch.hypercharge_activation";
    public const string OverheatedState = "mch.overheated_state";
    public const string HeatBlastRotation = "mch.heat_blast_rotation";
    public const string OgcdWeaving = "mch.ogcd_weaving";
    public const string HyperchargeTiming = "mch.hypercharge_timing";

    // Wildfire Burst (4)
    public const string WildfirePlacement = "mch.wildfire_placement";
    public const string WildfireAlignment = "mch.wildfire_alignment";
    public const string BurstPartySync = "mch.burst_party_sync";
    public const string PhaseAwareness = "mch.phase_awareness";

    // Tool Priority (4)
    public const string DrillPriority = "mch.drill_priority";
    public const string AirAnchorUsage = "mch.air_anchor_usage";
    public const string ChainSawUsage = "mch.chain_saw_usage";
    public const string ProcTracking = "mch.proc_tracking";

    // Pet Management (3)
    public const string QueenSummoning = "mch.queen_summoning";
    public const string BatteryAccumulation = "mch.battery_accumulation";
    public const string QueenDamageScaling = "mch.queen_damage_scaling";

    // Reassemble & Utility (3)
    public const string ReassemblePriority = "mch.reassemble_priority";
    public const string ReassembleCharges = "mch.reassemble_charges";
    public const string InterruptUsage = "mch.interrupt_usage";

    // AoE Rotation (2)
    public const string AoeRotation = "mch.aoe_rotation";
    public const string TargetCountThreshold = "mch.target_count_threshold";

    /// <summary>
    /// All MCH concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Gauge Fundamentals
        HeatGauge, BatteryGauge, GaugeOvercapping, GaugeInteractions,
        // Hypercharge System
        HyperchargeActivation, OverheatedState, HeatBlastRotation, OgcdWeaving, HyperchargeTiming,
        // Wildfire Burst
        WildfirePlacement, WildfireAlignment, BurstPartySync, PhaseAwareness,
        // Tool Priority
        DrillPriority, AirAnchorUsage, ChainSawUsage, ProcTracking,
        // Pet Management
        QueenSummoning, BatteryAccumulation, QueenDamageScaling,
        // Reassemble & Utility
        ReassemblePriority, ReassembleCharges, InterruptUsage,
        // AoE Rotation
        AoeRotation, TargetCountThreshold,
    };
}

/// <summary>
/// BRD (Calliope) Training Mode concepts covering Bard mechanics.
/// 25 concepts across 7 categories.
/// </summary>
public static class BrdConcepts
{
    // Song System (4)
    public const string SongRotation = "brd.song_rotation";
    public const string WanderersMinuet = "brd.wanderers_minuet";
    public const string MagesBallad = "brd.mages_ballad";
    public const string ArmysPaeon = "brd.armys_paeon";

    // Repertoire & Pitch Perfect (3)
    public const string RepertoireStacks = "brd.repertoire_stacks";
    public const string PitchPerfect = "brd.pitch_perfect";
    public const string SongSwitching = "brd.song_switching";

    // Soul Voice & Apex (4)
    public const string SoulVoiceGauge = "brd.soul_voice_gauge";
    public const string ApexArrow = "brd.apex_arrow";
    public const string BlastArrow = "brd.blast_arrow";
    public const string SoulVoiceOvercapping = "brd.soul_voice_overcapping";

    // Proc System (4)
    public const string StraightShotReady = "brd.straight_shot_ready";
    public const string RefulgentArrow = "brd.refulgent_arrow";
    public const string Barrage = "brd.barrage";
    public const string ResonantArrow = "brd.resonant_arrow";

    // DoT Management (3)
    public const string CausticBite = "brd.caustic_bite";
    public const string Stormbite = "brd.stormbite";
    public const string IronJaws = "brd.iron_jaws";

    // Burst Window (4)
    public const string RagingStrikes = "brd.raging_strikes";
    public const string BattleVoice = "brd.battle_voice";
    public const string RadiantFinale = "brd.radiant_finale";
    public const string RadiantEncore = "brd.radiant_encore";

    // oGCD & Utility (3)
    public const string EmpyrealArrow = "brd.empyreal_arrow";
    public const string BloodletterManagement = "brd.bloodletter_management";
    public const string PartyUtility = "brd.party_utility";

    /// <summary>
    /// All BRD concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Song System
        SongRotation, WanderersMinuet, MagesBallad, ArmysPaeon,
        // Repertoire & Pitch Perfect
        RepertoireStacks, PitchPerfect, SongSwitching,
        // Soul Voice & Apex
        SoulVoiceGauge, ApexArrow, BlastArrow, SoulVoiceOvercapping,
        // Proc System
        StraightShotReady, RefulgentArrow, Barrage, ResonantArrow,
        // DoT Management
        CausticBite, Stormbite, IronJaws,
        // Burst Window
        RagingStrikes, BattleVoice, RadiantFinale, RadiantEncore,
        // oGCD & Utility
        EmpyrealArrow, BloodletterManagement, PartyUtility,
    };
}

/// <summary>
/// DNC (Terpsichore) Training Mode concepts covering Dancer mechanics.
/// 25 concepts across 7 categories.
/// </summary>
public static class DncConcepts
{
    // Dance System (4)
    public const string StandardStep = "dnc.standard_step";
    public const string TechnicalStep = "dnc.technical_step";
    public const string DanceExecution = "dnc.dance_execution";
    public const string DanceTimers = "dnc.dance_timers";

    // Proc System (4)
    public const string SilkenSymmetry = "dnc.silken_symmetry";
    public const string SilkenFlow = "dnc.silken_flow";
    public const string ThreefoldFan = "dnc.threefold_fan";
    public const string FourfoldFan = "dnc.fourfold_fan";

    // Esprit Gauge (3)
    public const string EspritGauge = "dnc.esprit_gauge";
    public const string SaberDance = "dnc.saber_dance";
    public const string EspritOvercapping = "dnc.esprit_overcapping";

    // Feather Gauge (3)
    public const string FeatherGauge = "dnc.feather_gauge";
    public const string FanDanceUsage = "dnc.fan_dance_usage";
    public const string FeatherOvercapping = "dnc.feather_overcapping";

    // Burst Window (4)
    public const string Devilment = "dnc.devilment";
    public const string Flourish = "dnc.flourish";
    public const string BurstAlignment = "dnc.burst_alignment";
    public const string PartyBurstSync = "dnc.party_burst_sync";

    // High-Level Abilities (4)
    public const string StarfallDance = "dnc.starfall_dance";
    public const string FinishingMove = "dnc.finishing_move";
    public const string LastDance = "dnc.last_dance";
    public const string Tillana = "dnc.tillana";

    // Partner & Utility (3)
    public const string ClosedPosition = "dnc.closed_position";
    public const string ShieldSamba = "dnc.shield_samba";
    public const string PartyUtility = "dnc.party_utility";

    /// <summary>
    /// All DNC concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Dance System
        StandardStep, TechnicalStep, DanceExecution, DanceTimers,
        // Proc System
        SilkenSymmetry, SilkenFlow, ThreefoldFan, FourfoldFan,
        // Esprit Gauge
        EspritGauge, SaberDance, EspritOvercapping,
        // Feather Gauge
        FeatherGauge, FanDanceUsage, FeatherOvercapping,
        // Burst Window
        Devilment, Flourish, BurstAlignment, PartyBurstSync,
        // High-Level Abilities
        StarfallDance, FinishingMove, LastDance, Tillana,
        // Partner & Utility
        ClosedPosition, ShieldSamba, PartyUtility,
    };
}

/// <summary>
/// SMN (Persephone) Training Mode concepts covering Summoner mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class SmnConcepts
{
    // Aetherflow System (4)
    public const string AetherflowStacks = "smn.aetherflow_stacks";
    public const string EnergyDrainUsage = "smn.energy_drain_usage";
    public const string AetherflowTiming = "smn.aetherflow_timing";
    public const string FesterNecrotize = "smn.fester_necrotize";

    // Primal Attunement (5)
    public const string AttunementSystem = "smn.attunement_system";
    public const string IfritPhase = "smn.ifrit_phase";
    public const string TitanPhase = "smn.titan_phase";
    public const string GarudaPhase = "smn.garuda_phase";
    public const string PrimalOrder = "smn.primal_order";

    // Primal Favor (4)
    public const string CrimsonCyclone = "smn.crimson_cyclone";
    public const string MountainBuster = "smn.mountain_buster";
    public const string Slipstream = "smn.slipstream";
    public const string FavorTiming = "smn.favor_timing";

    // Demi-Summon System (4)
    public const string DemiPhases = "smn.demi_phases";
    public const string BahamutPhase = "smn.bahamut_phase";
    public const string PhoenixPhase = "smn.phoenix_phase";
    public const string SolarBahamutPhase = "smn.solar_bahamut_phase";

    // Burst Abilities (4)
    public const string Enkindle = "smn.enkindle";
    public const string AstralFlow = "smn.astral_flow";
    public const string SearingLight = "smn.searing_light";
    public const string SearingFlash = "smn.searing_flash";

    // Filler & Utility (4)
    public const string RuinSpells = "smn.ruin_spells";
    public const string RuinIvProcs = "smn.ruin_iv_procs";
    public const string AoeRotation = "smn.aoe_rotation";
    public const string PartyCoordination = "smn.party_coordination";

    /// <summary>
    /// All SMN concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Aetherflow System
        AetherflowStacks, EnergyDrainUsage, AetherflowTiming, FesterNecrotize,
        // Primal Attunement
        AttunementSystem, IfritPhase, TitanPhase, GarudaPhase, PrimalOrder,
        // Primal Favor
        CrimsonCyclone, MountainBuster, Slipstream, FavorTiming,
        // Demi-Summon System
        DemiPhases, BahamutPhase, PhoenixPhase, SolarBahamutPhase,
        // Burst Abilities
        Enkindle, AstralFlow, SearingLight, SearingFlash,
        // Filler & Utility
        RuinSpells, RuinIvProcs, AoeRotation, PartyCoordination,
    };
}

/// <summary>
/// BLM (Hecate) Training Mode concepts covering Black Mage mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class BlmConcepts
{
    // Element System (5)
    public const string AstralFire = "blm.astral_fire";
    public const string UmbralIce = "blm.umbral_ice";
    public const string ElementTimer = "blm.element_timer";
    public const string Enochian = "blm.enochian";
    public const string ElementTransitions = "blm.element_transitions";

    // Gauge Resources (5)
    public const string UmbralHearts = "blm.umbral_hearts";
    public const string PolyglotStacks = "blm.polyglot_stacks";
    public const string AstralSoul = "blm.astral_soul";
    public const string GaugeOvercapping = "blm.gauge_overcapping";
    public const string MpManagement = "blm.mp_management";

    // Proc System (4)
    public const string FirestarterProc = "blm.firestarter_proc";
    public const string ThunderheadProc = "blm.thunderhead_proc";
    public const string ProcPriority = "blm.proc_priority";
    public const string ParadoxMechanic = "blm.paradox_mechanic";

    // Core Rotation (5)
    public const string FirePhase = "blm.fire_phase";
    public const string IcePhase = "blm.ice_phase";
    public const string FireIvSpam = "blm.fire_iv_spam";
    public const string DespairTiming = "blm.despair_timing";
    public const string ThunderDot = "blm.thunder_dot";

    // Cooldown Management (3)
    public const string LeyLines = "blm.ley_lines";
    public const string Triplecast = "blm.triplecast";
    public const string Manafont = "blm.manafont";

    // Advanced Execution (3)
    public const string MovementOptimization = "blm.movement_optimization";
    public const string XenoglossyUsage = "blm.xenoglossy_usage";
    public const string AoeRotation = "blm.aoe_rotation";

    /// <summary>
    /// All BLM concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Element System
        AstralFire, UmbralIce, ElementTimer, Enochian, ElementTransitions,
        // Gauge Resources
        UmbralHearts, PolyglotStacks, AstralSoul, GaugeOvercapping, MpManagement,
        // Proc System
        FirestarterProc, ThunderheadProc, ProcPriority, ParadoxMechanic,
        // Core Rotation
        FirePhase, IcePhase, FireIvSpam, DespairTiming, ThunderDot,
        // Cooldown Management
        LeyLines, Triplecast, Manafont,
        // Advanced Execution
        MovementOptimization, XenoglossyUsage, AoeRotation,
    };
}

/// <summary>
/// RDM (Circe) Training Mode concepts covering Red Mage mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class RdmConcepts
{
    // Mana System (5)
    public const string BlackMana = "rdm.black_mana";
    public const string WhiteMana = "rdm.white_mana";
    public const string ManaBalance = "rdm.mana_balance";
    public const string ManaImbalance = "rdm.mana_imbalance";
    public const string ManaOvercap = "rdm.mana_overcap";

    // Dualcast System (4)
    public const string DualcastMechanic = "rdm.dualcast_mechanic";
    public const string DualcastConsumption = "rdm.dualcast_consumption";
    public const string Acceleration = "rdm.acceleration";
    public const string SwiftcastUsage = "rdm.swiftcast_usage";

    // Proc Management (4)
    public const string VerfireProc = "rdm.verfire_proc";
    public const string VerstoneProc = "rdm.verstone_proc";
    public const string ProcPriority = "rdm.proc_priority";
    public const string ProcFishing = "rdm.proc_fishing";

    // Melee Combo (4)
    public const string MeleeEntry = "rdm.melee_entry";
    public const string ComboProgression = "rdm.combo_progression";
    public const string ComboTimer = "rdm.combo_timer";
    public const string MeleePositioning = "rdm.melee_positioning";

    // Finisher System (4)
    public const string FinisherSelection = "rdm.finisher_selection";
    public const string ScorchResolution = "rdm.scorch_resolution";
    public const string GrandImpact = "rdm.grand_impact";
    public const string FinisherProcs = "rdm.finisher_procs";

    // Burst & Utility (4)
    public const string Embolden = "rdm.embolden";
    public const string Manafication = "rdm.manafication";
    public const string OgcdWeaving = "rdm.ogcd_weaving";
    public const string AoeRotation = "rdm.aoe_rotation";

    /// <summary>
    /// All RDM concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Mana System
        BlackMana, WhiteMana, ManaBalance, ManaImbalance, ManaOvercap,
        // Dualcast System
        DualcastMechanic, DualcastConsumption, Acceleration, SwiftcastUsage,
        // Proc Management
        VerfireProc, VerstoneProc, ProcPriority, ProcFishing,
        // Melee Combo
        MeleeEntry, ComboProgression, ComboTimer, MeleePositioning,
        // Finisher System
        FinisherSelection, ScorchResolution, GrandImpact, FinisherProcs,
        // Burst & Utility
        Embolden, Manafication, OgcdWeaving, AoeRotation,
    };
}

/// <summary>
/// PCT (Iris) Training Mode concepts covering Pictomancer mechanics.
/// 25 concepts across 6 categories.
/// </summary>
public static class PctConcepts
{
    // Core Mechanics (5)
    public const string PaletteGauge = "pct.palette_gauge";
    public const string WhitePaint = "pct.white_paint";
    public const string BlackPaint = "pct.black_paint";
    public const string ComboBasics = "pct.combo_basics";
    public const string SubtractiveCombo = "pct.subtractive_combo";

    // Canvas System (4)
    public const string CreatureMotifs = "pct.creature_motifs";
    public const string WeaponCanvas = "pct.weapon_canvas";
    public const string LandscapeCanvas = "pct.landscape_canvas";
    public const string CanvasPrepull = "pct.canvas_prepull";

    // Muse Abilities (4)
    public const string LivingMuse = "pct.living_muse";
    public const string StrikingMuse = "pct.striking_muse";
    public const string StarryMuse = "pct.starry_muse";
    public const string MuseTiming = "pct.muse_timing";

    // Subtractive Palette (4)
    public const string SubtractivePalette = "pct.subtractive_palette";
    public const string MonochromaticTones = "pct.monochromatic_tones";
    public const string PaletteSpending = "pct.palette_spending";
    public const string FinisherPriority = "pct.finisher_priority";

    // Paint Spenders (3)
    public const string HolyInWhite = "pct.holy_in_white";
    public const string CometInBlack = "pct.comet_in_black";
    public const string RainbowDrip = "pct.rainbow_drip";

    // Advanced (5)
    public const string StarryMuseBurst = "pct.starry_muse_burst";
    public const string HammerCombo = "pct.hammer_combo";
    public const string AoeRotation = "pct.aoe_rotation";
    public const string MovementOptimization = "pct.movement_optimization";
    public const string PartyCoordination = "pct.party_coordination";

    /// <summary>
    /// All PCT concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        // Core Mechanics
        PaletteGauge, WhitePaint, BlackPaint, ComboBasics, SubtractiveCombo,
        // Canvas System
        CreatureMotifs, WeaponCanvas, LandscapeCanvas, CanvasPrepull,
        // Muse Abilities
        LivingMuse, StrikingMuse, StarryMuse, MuseTiming,
        // Subtractive Palette
        SubtractivePalette, MonochromaticTones, PaletteSpending, FinisherPriority,
        // Paint Spenders
        HolyInWhite, CometInBlack, RainbowDrip,
        // Advanced
        StarryMuseBurst, HammerCombo, AoeRotation, MovementOptimization, PartyCoordination,
    };
}

/// <summary>
/// Represents a lesson recommendation based on detected performance issues or concept mastery.
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
    /// Struggling concepts this lesson addresses (empty if issue-based only).
    /// </summary>
    public string[] StrugglingConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this recommendation is driven by mastery data.
    /// </summary>
    public bool IsMasteryDriven { get; init; }

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
