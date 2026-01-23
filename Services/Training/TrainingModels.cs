namespace Olympus.Services.Training;

using System;
using Olympus.Config;

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
