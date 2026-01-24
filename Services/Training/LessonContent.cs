namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a structured lesson for training mode.
/// </summary>
public sealed class LessonDefinition
{
    /// <summary>
    /// Unique lesson identifier (e.g., "whm.lesson_1").
    /// </summary>
    public string LessonId { get; init; } = string.Empty;

    /// <summary>
    /// Job prefix (whm, sch, ast, sge).
    /// </summary>
    public string JobPrefix { get; init; } = string.Empty;

    /// <summary>
    /// Lesson title (e.g., "Healer Fundamentals").
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Overview description of the lesson.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Lesson number (1-7).
    /// </summary>
    public int LessonNumber { get; init; }

    /// <summary>
    /// LessonIds required to be completed first.
    /// </summary>
    public string[] Prerequisites { get; init; } = Array.Empty<string>();

    /// <summary>
    /// ConceptIds from TrainingModels that this lesson covers.
    /// </summary>
    public string[] ConceptsCovered { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Key educational points for this lesson.
    /// </summary>
    public string[] KeyPoints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Related ability names.
    /// </summary>
    public string[] RelatedAbilities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Learning tips for practicing.
    /// </summary>
    public string[] Tips { get; init; } = Array.Empty<string>();
}

/// <summary>
/// WHM (Apollo) lesson content - 7 lessons covering 27 concepts.
/// </summary>
public static class WhmLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "whm.lesson_1",
        JobPrefix = "whm",
        Title = "Healer Fundamentals",
        LessonNumber = 1,
        Description = "Learn the core principles of healing in FFXIV: prioritizing targets, understanding party-wide damage, and weaving oGCDs effectively.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            WhmConcepts.HealingPriority,
            WhmConcepts.TankPriority,
            WhmConcepts.PartyWideDamage,
            WhmConcepts.OgcdWeaving,
        },
        KeyPoints = new[]
        {
            "Tank takes priority in most scenarios - they take the most consistent damage",
            "Party-wide damage events require different healing than tank damage",
            "oGCD heals should be weaved between GCDs to maintain DPS uptime",
            "Never let oGCDs sit unused when healing is needed",
        },
        RelatedAbilities = new[] { "Cure", "Cure II", "Medica", "Medica II", "Regen" },
        Tips = new[]
        {
            "Watch the party list - anticipate damage before it happens",
            "Practice weaving one oGCD between each GCD in a striking dummy",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "whm.lesson_2",
        JobPrefix = "whm",
        Title = "Emergency Response",
        LessonNumber = 2,
        Description = "Master WHM's emergency healing toolkit: when to use Benediction, Tetragrammaton, and how to react to critical situations.",
        Prerequisites = new[] { "whm.lesson_1" },
        ConceptsCovered = new[]
        {
            WhmConcepts.EmergencyHealing,
            WhmConcepts.BenedictionUsage,
            WhmConcepts.TetragrammatonUsage,
        },
        KeyPoints = new[]
        {
            "Benediction is your panic button - full HP heal on 180s cooldown",
            "Tetragrammaton is a strong oGCD heal (60s) - use it liberally",
            "Emergency healing means someone will die without immediate intervention",
            "Don't hold Benediction 'for emergencies' that never come",
        },
        RelatedAbilities = new[] { "Benediction", "Tetragrammaton" },
        Tips = new[]
        {
            "If the tank is below 30% and taking heavy damage, that's an emergency",
            "Tetragrammaton can handle most 'urgent' situations - save Benediction for true emergencies",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "whm.lesson_3",
        JobPrefix = "whm",
        Title = "The Lily System",
        LessonNumber = 3,
        Description = "Understand WHM's unique Lily gauge: generating lilies, spending them efficiently, and maximizing Blood Lily damage.",
        Prerequisites = new[] { "whm.lesson_2" },
        ConceptsCovered = new[]
        {
            WhmConcepts.LilyManagement,
            WhmConcepts.AfflatusSolaceUsage,
            WhmConcepts.AfflatusRaptureUsage,
            WhmConcepts.BloodLilyBuilding,
            WhmConcepts.AfflatusMiseryTiming,
        },
        KeyPoints = new[]
        {
            "Lilies generate every 20s in combat (30s at lower levels)",
            "Afflatus Solace: single-target lily heal - use for tank healing",
            "Afflatus Rapture: AoE lily heal - use for party-wide damage",
            "Each lily spent builds toward Blood Lily (Afflatus Misery)",
            "Afflatus Misery is your strongest single attack - don't waste it",
        },
        RelatedAbilities = new[] { "Afflatus Solace", "Afflatus Rapture", "Afflatus Misery" },
        Tips = new[]
        {
            "Lilies are 'free' heals - they don't cost MP and refund GCD time via Misery",
            "Always try to use 3 lilies before downtime to get Misery ready",
            "Don't overcap lilies (max 3) - use them or lose them",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "whm.lesson_4",
        JobPrefix = "whm",
        Title = "Proactive Healing",
        LessonNumber = 4,
        Description = "Learn to heal before damage happens: maintaining Regen, timing shields, and using Assize and Divine Benison effectively.",
        Prerequisites = new[] { "whm.lesson_3" },
        ConceptsCovered = new[]
        {
            WhmConcepts.ProactiveHealing,
            WhmConcepts.RegenMaintenance,
            WhmConcepts.ShieldTiming,
            WhmConcepts.DivineBenisonUsage,
            WhmConcepts.AssizeUsage,
        },
        KeyPoints = new[]
        {
            "Proactive healing prevents emergencies before they happen",
            "Keep Regen on the tank at all times during combat",
            "Divine Benison provides a shield - apply before damage, not after",
            "Assize heals AND damages - use it on cooldown for DPS when healing is adequate",
            "Shields applied after damage are wasted shields",
        },
        RelatedAbilities = new[] { "Regen", "Divine Benison", "Assize" },
        Tips = new[]
        {
            "Put Regen on the tank before the pull, not during",
            "Assize has a short cooldown (40s) - don't hold it for 'big heals'",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "whm.lesson_5",
        JobPrefix = "whm",
        Title = "Defensive Cooldowns",
        LessonNumber = 5,
        Description = "Master WHM's defensive toolkit: Temperance for party mitigation, Aquaveil for tank protection, and Liturgy of the Bell for heavy damage.",
        Prerequisites = new[] { "whm.lesson_4" },
        ConceptsCovered = new[]
        {
            WhmConcepts.TemperanceUsage,
            WhmConcepts.AquaveilUsage,
            WhmConcepts.LiturgyOfTheBellUsage,
        },
        KeyPoints = new[]
        {
            "Temperance: 10% party mitigation + healing boost for 20s (2min CD)",
            "Aquaveil: 15% mitigation on one target for 8s (1min CD) - great for tankbusters",
            "Liturgy of the Bell: place it, party takes damage, bell heals them",
            "Use Temperance proactively before heavy party damage phases",
        },
        RelatedAbilities = new[] { "Temperance", "Aquaveil", "Liturgy of the Bell" },
        Tips = new[]
        {
            "Liturgy requires 5 'rings' of damage to maximize healing - plan around mechanics",
            "Aquaveil on the tank before a tankbuster is often better than healing after",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "whm.lesson_6",
        JobPrefix = "whm",
        Title = "DPS Optimization",
        LessonNumber = 6,
        Description = "Maximize your damage contribution: prioritizing Glare, maintaining Dia (DoT), and balancing DPS with healing.",
        Prerequisites = new[] { "whm.lesson_5" },
        ConceptsCovered = new[]
        {
            WhmConcepts.DpsOptimization,
            WhmConcepts.GlarePriority,
            WhmConcepts.DotMaintenance,
        },
        KeyPoints = new[]
        {
            "Glare is your filler - cast it whenever you're not healing",
            "Dia (DoT) should be maintained on the boss at all times",
            "DoT refresh at 3s or less remaining to avoid clipping",
            "Good healers do damage while keeping the party alive",
            "Dead DPS do 0 damage - healing enables party DPS",
        },
        RelatedAbilities = new[] { "Glare III", "Dia", "Holy" },
        Tips = new[]
        {
            "In dungeons, Holy's stun is a mitigation tool - use it early in pulls",
            "Aim for 80%+ GCD uptime on Glare in boss fights",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "whm.lesson_7",
        JobPrefix = "whm",
        Title = "Utility & Coordination",
        LessonNumber = 7,
        Description = "Final lesson: Esuna usage, raise prioritization, and coordinating with your co-healer for optimal party performance.",
        Prerequisites = new[] { "whm.lesson_6" },
        ConceptsCovered = new[]
        {
            WhmConcepts.EsunaUsage,
            WhmConcepts.RaiseDecision,
            WhmConcepts.CoHealerAwareness,
            WhmConcepts.PartyCoordination,
        },
        KeyPoints = new[]
        {
            "Esuna removes debuffs with a white bar above them",
            "Raise has a long cast time - Swiftcast + Raise is standard",
            "Communicate with your co-healer to avoid duplicate raises",
            "Don't both healers use party mitigation at the same time",
            "Coordinate who handles tank healing vs party healing",
        },
        RelatedAbilities = new[] { "Esuna", "Raise", "Swiftcast" },
        Tips = new[]
        {
            "In progression, calling raises in voice chat prevents wasted MP",
            "Some debuffs are not cleansable even with a white bar - learn which ones",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// SCH (Athena) lesson content - 7 lessons covering 27 concepts.
/// </summary>
public static class SchLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "sch.lesson_1",
        JobPrefix = "sch",
        Title = "Scholar Fundamentals",
        LessonNumber = 1,
        Description = "Learn SCH's core healing identity: emergency healing with Lustrate, shield timing, and the importance of preventing damage.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            SchConcepts.EmergencyHealing,
            SchConcepts.LustrateUsage,
            SchConcepts.ShieldTiming,
        },
        KeyPoints = new[]
        {
            "SCH is a shield healer - prevent damage rather than heal after",
            "Lustrate is your emergency single-target heal (costs 1 Aetherflow)",
            "Shields must be applied BEFORE damage hits to be effective",
            "Emergency healing means someone will die without immediate intervention",
        },
        RelatedAbilities = new[] { "Lustrate", "Adloquium", "Succor", "Physick" },
        Tips = new[]
        {
            "Lustrate heals instantly - use it when the tank is in danger",
            "Watch cast bars and apply shields before boss attacks land",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "sch.lesson_2",
        JobPrefix = "sch",
        Title = "Aetherflow Mastery",
        LessonNumber = 2,
        Description = "Master Aetherflow resource management: refresh timing, balancing healing vs Energy Drain damage, and avoiding waste.",
        Prerequisites = new[] { "sch.lesson_1" },
        ConceptsCovered = new[]
        {
            SchConcepts.AetherflowManagement,
            SchConcepts.AetherflowRefresh,
            SchConcepts.EnergyDrainUsage,
        },
        KeyPoints = new[]
        {
            "Aetherflow grants 3 stacks every 60s - never let it sit off cooldown",
            "Spend all Aetherflow before refreshing to avoid waste",
            "Energy Drain is DPS - use excess Aetherflow stacks on it",
            "Healing Aetherflow abilities take priority over Energy Drain",
        },
        RelatedAbilities = new[] { "Aetherflow", "Energy Drain", "Lustrate", "Indomitability", "Sacred Soil", "Excogitation" },
        Tips = new[]
        {
            "Plan your Aetherflow usage around the 60s cooldown",
            "In low-damage phases, dump stacks into Energy Drain before refresh",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "sch.lesson_3",
        JobPrefix = "sch",
        Title = "Fairy Management",
        LessonNumber = 3,
        Description = "Understand your fairy companion: passive healing, Whispering Dawn, Fey Illumination, Fey Blessing, and Fey Union.",
        Prerequisites = new[] { "sch.lesson_2" },
        ConceptsCovered = new[]
        {
            SchConcepts.FairyManagement,
            SchConcepts.WhisperingDawnUsage,
            SchConcepts.FeyIlluminationUsage,
            SchConcepts.FeyBlessingUsage,
            SchConcepts.FeyUnionUsage,
        },
        KeyPoints = new[]
        {
            "Your fairy heals automatically - Embrace is free passive healing",
            "Whispering Dawn: party HoT (60s CD) - use for sustained party damage",
            "Fey Illumination: healing boost + magic mitigation (2min CD)",
            "Fey Blessing: instant party heal (60s CD) - use for burst party damage",
            "Fey Union: tethers fairy to tank for continuous single-target healing",
        },
        RelatedAbilities = new[] { "Summon Eos", "Whispering Dawn", "Fey Illumination", "Fey Blessing", "Fey Union" },
        Tips = new[]
        {
            "Fairy abilities are oGCDs - weave them freely",
            "Fey Union drains Fairy Gauge - monitor your gauge",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "sch.lesson_4",
        JobPrefix = "sch",
        Title = "Advanced Fairy & Seraph",
        LessonNumber = 4,
        Description = "Master Seraph transformation, Dissipation sacrifice, and Excogitation's delayed healing.",
        Prerequisites = new[] { "sch.lesson_3" },
        ConceptsCovered = new[]
        {
            SchConcepts.SeraphUsage,
            SchConcepts.DissipationUsage,
            SchConcepts.ExcogitationUsage,
        },
        KeyPoints = new[]
        {
            "Seraph replaces your fairy with stronger abilities for 22s",
            "Seraph's Consolation provides shields - use before damage",
            "Dissipation sacrifices fairy for 3 Aetherflow + 20% healing boost",
            "Excogitation: delayed heal that triggers when target drops below 50%",
        },
        RelatedAbilities = new[] { "Summon Seraph", "Consolation", "Dissipation", "Excogitation" },
        Tips = new[]
        {
            "Excog on the tank before tankbusters is excellent proactive healing",
            "Dissipation is best used when you need burst healing or before downtime",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "sch.lesson_5",
        JobPrefix = "sch",
        Title = "Shield Economy",
        LessonNumber = 5,
        Description = "Optimize your shields: Adloquium vs Succor decisions, Deployment Tactics spreading, Emergency Tactics converting, and Recitation crits.",
        Prerequisites = new[] { "sch.lesson_4" },
        ConceptsCovered = new[]
        {
            SchConcepts.AdloquiumUsage,
            SchConcepts.SuccorUsage,
            SchConcepts.DeploymentTactics,
            SchConcepts.EmergencyTacticsUsage,
            SchConcepts.RecitationUsage,
        },
        KeyPoints = new[]
        {
            "Adloquium: single-target shield + heal - use on tank",
            "Succor: party-wide shield + heal - use before party damage",
            "Deployment Tactics: spreads your Adlo shield to the whole party",
            "Emergency Tactics: converts next shield into pure healing",
            "Recitation: guarantees crit on next Adlo/Succor/Indom/Excog",
        },
        RelatedAbilities = new[] { "Adloquium", "Succor", "Deployment Tactics", "Emergency Tactics", "Recitation" },
        Tips = new[]
        {
            "Recitation + Adlo + Deploy = massive party-wide shields",
            "Emergency Tactics is useful when shields would be wasted (already shielded)",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "sch.lesson_6",
        JobPrefix = "sch",
        Title = "oGCD Healing & Mitigation",
        LessonNumber = 6,
        Description = "Master Indomitability for burst party healing, Sacred Soil for mitigation zones, and Expedient for movement and defense.",
        Prerequisites = new[] { "sch.lesson_5" },
        ConceptsCovered = new[]
        {
            SchConcepts.IndomitabilityUsage,
            SchConcepts.SacredSoilUsage,
            SchConcepts.ExpedientUsage,
        },
        KeyPoints = new[]
        {
            "Indomitability: instant party heal (costs 1 Aetherflow, 30s CD)",
            "Sacred Soil: ground AoE with 10% mitigation + HoT (costs 1 Aetherflow)",
            "Expedient: party sprint + 10% mitigation for 20s (2min CD)",
            "Sacred Soil's HoT is extremely powerful - place it before damage",
        },
        RelatedAbilities = new[] { "Indomitability", "Sacred Soil", "Expedient" },
        Tips = new[]
        {
            "Sacred Soil is often better than Indom due to the mitigation",
            "Expedient is great for mechanics requiring movement",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "sch.lesson_7",
        JobPrefix = "sch",
        Title = "DPS & Coordination",
        LessonNumber = 7,
        Description = "Final lesson: Chain Stratagem raid buff, DoT maintenance, raise coordination, and working with your co-healer.",
        Prerequisites = new[] { "sch.lesson_6" },
        ConceptsCovered = new[]
        {
            SchConcepts.DpsOptimization,
            SchConcepts.ChainStratagemTiming,
            SchConcepts.DotMaintenance,
            SchConcepts.RaiseDecision,
            SchConcepts.CoHealerAwareness,
            SchConcepts.EsunaUsage,
        },
        KeyPoints = new[]
        {
            "Chain Stratagem: 10% crit rate buff on target for 20s (2min CD)",
            "Align Chain Stratagem with party burst windows",
            "Maintain Biolysis (DoT) on the boss at all times",
            "Coordinate raises with co-healer to avoid double-raising",
            "SCH often handles shields while regen healer handles HoTs",
        },
        RelatedAbilities = new[] { "Chain Stratagem", "Broil IV", "Biolysis", "Resurrection", "Esuna" },
        Tips = new[]
        {
            "Chain Stratagem at the start of the fight and on cooldown (with burst)",
            "Communicate shield plans with your co-healer in prog",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// AST (Astraea) lesson content - 7 lessons covering 27 concepts.
/// </summary>
public static class AstLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "ast.lesson_1",
        JobPrefix = "ast",
        Title = "Astrologian Fundamentals",
        LessonNumber = 1,
        Description = "Learn AST's core healing: Essential Dignity for emergencies, HoT-focused healing, and the regen healer identity.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            AstConcepts.EmergencyHealing,
            AstConcepts.EssentialDignityUsage,
            AstConcepts.HotManagement,
        },
        KeyPoints = new[]
        {
            "AST is a regen healer - focus on HoTs for sustained healing",
            "Essential Dignity: emergency heal that's stronger on low HP targets",
            "Essential Dignity has 2 charges (40s recharge) - use them freely",
            "HoTs heal over time - apply them before damage, not after",
        },
        RelatedAbilities = new[] { "Essential Dignity", "Benefic", "Benefic II", "Aspected Benefic" },
        Tips = new[]
        {
            "Essential Dignity heals MORE when the target is low - don't panic early",
            "Always have at least one Essential Dignity charge ready for emergencies",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "ast.lesson_2",
        JobPrefix = "ast",
        Title = "Card System Basics",
        LessonNumber = 2,
        Description = "Understand AST's card system: drawing cards, playing them on party members, and Minor Arcana for self-use.",
        Prerequisites = new[] { "ast.lesson_1" },
        ConceptsCovered = new[]
        {
            AstConcepts.CardManagement,
            AstConcepts.DrawTiming,
            AstConcepts.MinorArcanaUsage,
        },
        KeyPoints = new[]
        {
            "Draw gives you a card every 30s - never let it sit unused",
            "Cards give damage buffs - play them on DPS for maximum value",
            "Minor Arcana converts your card into Lord/Lady of Crowns",
            "Lord of Crowns: damage, Lady of Crowns: party heal",
        },
        RelatedAbilities = new[] { "Draw", "Play", "Minor Arcana", "Lord of Crowns", "Lady of Crowns" },
        Tips = new[]
        {
            "Play cards during burst windows for maximum DPS gain",
            "Don't hold cards - they have limited duration once drawn",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "ast.lesson_3",
        JobPrefix = "ast",
        Title = "Card System Advanced",
        LessonNumber = 3,
        Description = "Master Astrodyne gauge building, Divination raid buff timing, and Oracle mechanics.",
        Prerequisites = new[] { "ast.lesson_2" },
        ConceptsCovered = new[]
        {
            AstConcepts.AstrodyneBuilding,
            AstConcepts.DivinationTiming,
            AstConcepts.OracleUsage,
        },
        KeyPoints = new[]
        {
            "Astrodyne: powered by playing 3 cards, grants buffs based on variety",
            "Divination: 6% party damage buff for 20s (2min CD)",
            "Align Divination with other party raid buffs",
            "Oracle: granted after Divination, strong attack - don't waste it",
        },
        RelatedAbilities = new[] { "Astrodyne", "Divination", "Oracle" },
        Tips = new[]
        {
            "Track your Astrodyne seals - variety gives better buffs",
            "Divination at opener and then on cooldown with burst",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "ast.lesson_4",
        JobPrefix = "ast",
        Title = "HoT Economy",
        LessonNumber = 4,
        Description = "Optimize your HoTs: Aspected Benefic for tank, Aspected Helios for party, Celestial Opposition, and Synastry.",
        Prerequisites = new[] { "ast.lesson_3" },
        ConceptsCovered = new[]
        {
            AstConcepts.AspectedBeneficUsage,
            AstConcepts.AspectedHeliosUsage,
            AstConcepts.CelestialOppositionUsage,
            AstConcepts.SynastryUsage,
        },
        KeyPoints = new[]
        {
            "Aspected Benefic: single-target HoT - keep on tank",
            "Aspected Helios: party HoT - use for sustained party damage",
            "Celestial Opposition: instant party heal + HoT (60s CD)",
            "Synastry: links you to a target, duplicating heals (2min CD)",
        },
        RelatedAbilities = new[] { "Aspected Benefic", "Aspected Helios", "Celestial Opposition", "Synastry" },
        Tips = new[]
        {
            "Celestial Opposition is free oGCD healing - use it on cooldown",
            "Synastry on the tank doubles your healing efficiency",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "ast.lesson_5",
        JobPrefix = "ast",
        Title = "Earthly Star Mastery",
        LessonNumber = 5,
        Description = "Master Earthly Star: placement timing, maturation mechanics, and maximizing its healing potential.",
        Prerequisites = new[] { "ast.lesson_4" },
        ConceptsCovered = new[]
        {
            AstConcepts.EarthlyStarPlacement,
            AstConcepts.EarthlyStarMaturation,
        },
        KeyPoints = new[]
        {
            "Earthly Star: ground AoE that matures over 10s for stronger effect",
            "Place it 10s before you need the heal for maximum potency",
            "Stellar Detonation triggers it manually, or it auto-detonates at 20s",
            "Mature (Giant Dominance): 720 potency heal vs immature 360 potency",
        },
        RelatedAbilities = new[] { "Earthly Star", "Stellar Detonation" },
        Tips = new[]
        {
            "Learn boss timelines to place Star 10s before raidwides",
            "A mature Star is one of the strongest heals in the game",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "ast.lesson_6",
        JobPrefix = "ast",
        Title = "Defensive Cooldowns",
        LessonNumber = 6,
        Description = "Master AST's defensive toolkit: Celestial Intersection, Exaltation, Horoscope, Neutral Sect, Collective Unconscious, Macrocosmos, and Lightspeed.",
        Prerequisites = new[] { "ast.lesson_5" },
        ConceptsCovered = new[]
        {
            AstConcepts.CelestialIntersectionUsage,
            AstConcepts.ExaltationUsage,
            AstConcepts.HoroscopeUsage,
            AstConcepts.NeutralSectUsage,
            AstConcepts.CollectiveUnconsciousUsage,
            AstConcepts.MacrocosmosUsage,
            AstConcepts.LightspeedUsage,
        },
        KeyPoints = new[]
        {
            "Celestial Intersection: oGCD shield + HoT (30s CD)",
            "Exaltation: single-target mitigation + delayed heal (60s CD)",
            "Horoscope: stores healing, detonates for party heal",
            "Neutral Sect: adds shields to your HoTs for 20s (2min CD)",
            "Collective Unconscious: channeled party mitigation + HoT",
            "Macrocosmos: stores damage taken, heals it back (2min CD)",
            "Lightspeed: instant casts for 15s - great for movement",
        },
        RelatedAbilities = new[] { "Celestial Intersection", "Exaltation", "Horoscope", "Neutral Sect", "Collective Unconscious", "Macrocosmos", "Lightspeed" },
        Tips = new[]
        {
            "Neutral Sect before a raidwide gives everyone shields",
            "Macrocosmos must be activated while damage is being taken",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "ast.lesson_7",
        JobPrefix = "ast",
        Title = "DPS & Coordination",
        LessonNumber = 7,
        Description = "Final lesson: DPS optimization, DoT maintenance, raise decisions, and co-healer coordination.",
        Prerequisites = new[] { "ast.lesson_6" },
        ConceptsCovered = new[]
        {
            AstConcepts.DpsOptimization,
            AstConcepts.DotMaintenance,
            AstConcepts.RaiseDecision,
            AstConcepts.CoHealerAwareness,
            AstConcepts.EsunaUsage,
        },
        KeyPoints = new[]
        {
            "Maintain Combust (DoT) on the boss at all times",
            "Fall Malefic is your filler - cast it when not healing",
            "Coordinate raises with your co-healer",
            "AST handles regens while shield healers handle shields",
            "Esuna removes debuffs with white bars",
        },
        RelatedAbilities = new[] { "Fall Malefic", "Combust III", "Ascend", "Esuna" },
        Tips = new[]
        {
            "Gravity II for AoE situations (3+ targets)",
            "Call raises in voice chat during progression",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// SGE (Asclepius) lesson content - 7 lessons covering 30 concepts.
/// </summary>
public static class SgeLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "sge.lesson_1",
        JobPrefix = "sge",
        Title = "Sage Fundamentals",
        LessonNumber = 1,
        Description = "Learn SGE's unique identity: Kardia passive healing, emergency response, and target selection for optimal Kardia use.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            SgeConcepts.EmergencyHealing,
            SgeConcepts.KardiaManagement,
            SgeConcepts.KardiaTargetSelection,
        },
        KeyPoints = new[]
        {
            "Kardia: passive heal on target when you deal damage - keep it on tank",
            "SGE heals by dealing damage (Kardia) - DPS = healing",
            "Emergency healing uses Addersgall abilities (Druochole, Taurochole)",
            "Kardia target should rarely change once set on the main tank",
        },
        RelatedAbilities = new[] { "Kardia", "Diagnosis", "Prognosis", "Druochole" },
        Tips = new[]
        {
            "Apply Kardia before combat starts - it's your primary tank healing",
            "Your GCD attacks heal the Kardia target automatically",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "sge.lesson_2",
        JobPrefix = "sge",
        Title = "Addersgall Economy",
        LessonNumber = 2,
        Description = "Master Addersgall resource: generation timing, spending on heals vs damage, and the four Addersgall abilities.",
        Prerequisites = new[] { "sge.lesson_1" },
        ConceptsCovered = new[]
        {
            SgeConcepts.AddersgallManagement,
            SgeConcepts.DruocholeUsage,
            SgeConcepts.TaurocholeUsage,
            SgeConcepts.KeracholeUsage,
            SgeConcepts.IxocholeUsage,
        },
        KeyPoints = new[]
        {
            "Addersgall: regenerates 1 stack every 20s (max 3)",
            "Druochole: single-target heal (1 stack) - basic tank heal",
            "Taurochole: single-target heal + 10% mitigation (1 stack)",
            "Kerachole: party mitigation + HoT (1 stack) - 30s CD",
            "Ixochole: party heal (1 stack) - 30s CD",
        },
        RelatedAbilities = new[] { "Druochole", "Taurochole", "Kerachole", "Ixochole" },
        Tips = new[]
        {
            "Don't overcap Addersgall - spend stacks before hitting 3",
            "Kerachole is extremely efficient - use it on cooldown for mitigation",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "sge.lesson_3",
        JobPrefix = "sge",
        Title = "Kardia Optimization",
        LessonNumber = 3,
        Description = "Enhance Kardia healing with Soteria's boost and Philosophia's party-wide Kardia effect.",
        Prerequisites = new[] { "sge.lesson_2" },
        ConceptsCovered = new[]
        {
            SgeConcepts.SoteriaUsage,
            SgeConcepts.PhilosophiaUsage,
        },
        KeyPoints = new[]
        {
            "Soteria: 50% Kardia healing boost for 15s (60s CD)",
            "Use Soteria during high tank damage phases",
            "Philosophia: party-wide Kardia effect for 20s (3min CD)",
            "Philosophia lets your damage heal the entire party",
        },
        RelatedAbilities = new[] { "Soteria", "Philosophia" },
        Tips = new[]
        {
            "Soteria is free extra healing - use it liberally",
            "Philosophia during AoE damage phases is extremely powerful",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "sge.lesson_4",
        JobPrefix = "sge",
        Title = "Eukrasia System",
        LessonNumber = 4,
        Description = "Understand Eukrasia: converting Diagnosis to shields, Prognosis to party shields, and Dosis to DoT.",
        Prerequisites = new[] { "sge.lesson_3" },
        ConceptsCovered = new[]
        {
            SgeConcepts.EukrasiaDecisions,
            SgeConcepts.EukrasianDiagnosisUsage,
            SgeConcepts.EukrasianPrognosisUsage,
            SgeConcepts.EukrasianDosisUsage,
        },
        KeyPoints = new[]
        {
            "Eukrasia modifies your next Diagnosis, Prognosis, or Dosis",
            "E.Diagnosis: single-target shield - apply before tankbusters",
            "E.Prognosis: party shield - apply before raidwides",
            "E.Dosis: your DoT - maintain 100% uptime on boss",
        },
        RelatedAbilities = new[] { "Eukrasia", "Eukrasian Diagnosis", "Eukrasian Prognosis", "Eukrasian Dosis" },
        Tips = new[]
        {
            "Eukrasia is instant - weave it before your shield spell",
            "E.Dosis refresh at 3s or less to avoid clipping",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "sge.lesson_5",
        JobPrefix = "sge",
        Title = "oGCD Healing Toolkit",
        LessonNumber = 5,
        Description = "Master SGE's powerful oGCD heals: Physis, Holos, Pneuma, and Krasis for enhanced healing.",
        Prerequisites = new[] { "sge.lesson_4" },
        ConceptsCovered = new[]
        {
            SgeConcepts.PhysisUsage,
            SgeConcepts.HolosUsage,
            SgeConcepts.PneumaUsage,
            SgeConcepts.KrasisUsage,
        },
        KeyPoints = new[]
        {
            "Physis II: party HoT + healing received boost (60s CD)",
            "Holos: party shield + heal (2min CD) - strong raidwide mitigation",
            "Pneuma: line AoE damage + party heal (2min CD)",
            "Krasis: 20% healing boost on target for 10s (60s CD)",
        },
        RelatedAbilities = new[] { "Physis II", "Holos", "Pneuma", "Krasis" },
        Tips = new[]
        {
            "Physis is free - use it on cooldown for the healing boost",
            "Pneuma deals damage AND heals - never let it sit unused",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "sge.lesson_6",
        JobPrefix = "sge",
        Title = "Defensive Cooldowns",
        LessonNumber = 6,
        Description = "Master SGE's defensive abilities: Haima, Panhaima, Pepsis shield conversion, Zoe healing boost, and Rhizomata resource generation.",
        Prerequisites = new[] { "sge.lesson_5" },
        ConceptsCovered = new[]
        {
            SgeConcepts.HaimaUsage,
            SgeConcepts.PanhaimaUsage,
            SgeConcepts.PepsisUsage,
            SgeConcepts.ZoeUsage,
            SgeConcepts.RhizomataUsage,
        },
        KeyPoints = new[]
        {
            "Haima: stacking shields on single target (2min CD) - great for tankbusters",
            "Panhaima: stacking shields on party (2min CD) - great for multi-hit raidwides",
            "Pepsis: converts your shields into healing instantly",
            "Zoe: 50% boost to next GCD heal (90s CD)",
            "Rhizomata: grants 1 Addersgall stack (90s CD) - free resources",
        },
        RelatedAbilities = new[] { "Haima", "Panhaima", "Pepsis", "Zoe", "Rhizomata" },
        Tips = new[]
        {
            "Haima's shields refresh when broken - put it on tank before autos",
            "Pepsis is useful when shields would be wasted",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "sge.lesson_7",
        JobPrefix = "sge",
        Title = "DPS & Coordination",
        LessonNumber = 7,
        Description = "Final lesson: DPS optimization, DoT maintenance, offensive abilities (Phlegma, Toxikon, Psyche), raise coordination, and co-healer synergy.",
        Prerequisites = new[] { "sge.lesson_6" },
        ConceptsCovered = new[]
        {
            SgeConcepts.DpsOptimization,
            SgeConcepts.DotMaintenance,
            SgeConcepts.PhlegmaUsage,
            SgeConcepts.ToxikonUsage,
            SgeConcepts.PsycheUsage,
            SgeConcepts.RaiseDecision,
            SgeConcepts.CoHealerAwareness,
            SgeConcepts.EsunaUsage,
        },
        KeyPoints = new[]
        {
            "Dosis is your filler - cast it when not healing (Kardia heals tank)",
            "Phlegma: 2 charges, strong instant damage - use in burst windows",
            "Toxikon: instant damage, costs Addersting (from broken E.Diagnosis shields)",
            "Psyche: strong oGCD damage (1min CD) - use on cooldown",
            "Coordinate raises with co-healer, shields with regen healer",
        },
        RelatedAbilities = new[] { "Dosis III", "Phlegma III", "Toxikon II", "Psyche", "Egeiro", "Esuna" },
        Tips = new[]
        {
            "SGE's high DPS comes from Kardia - casting Dosis heals AND damages",
            "Save Phlegma charges for burst windows when possible",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// PLD (Themis) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class PldLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "pld.lesson_1",
        JobPrefix = "pld",
        Title = "Oath Gauge & Stance",
        LessonNumber = 1,
        Description = "Learn Paladin's core mechanics: the Oath Gauge system, maintaining tank stance, and using Sheltron for self-mitigation.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            PldConcepts.OathGauge,
            PldConcepts.Sheltron,
        },
        KeyPoints = new[]
        {
            "Oath Gauge builds from auto-attacks while Iron Will (tank stance) is active",
            "Sheltron costs 50 Oath and provides a strong shield + mitigation",
            "Never let Oath cap at 100 - spend it on Sheltron regularly",
            "Iron Will must be on to generate enmity and Oath",
        },
        RelatedAbilities = new[] { "Iron Will", "Sheltron", "Holy Sheltron", "Intervention" },
        Tips = new[]
        {
            "Use Sheltron proactively before tankbusters, not reactively after",
            "Intervention lets you give your Sheltron to another party member",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "pld.lesson_2",
        JobPrefix = "pld",
        Title = "Defensive Fundamentals",
        LessonNumber = 2,
        Description = "Master PLD's defensive toolkit: Rampart, Sentinel, Bulwark, and Reprisal for consistent damage reduction.",
        Prerequisites = new[] { "pld.lesson_1" },
        ConceptsCovered = new[]
        {
            PldConcepts.Sentinel,
            PldConcepts.Bulwark,
            PldConcepts.MitigationStacking,
        },
        KeyPoints = new[]
        {
            "Rampart: 20% mitigation for 20s (90s CD) - your bread and butter",
            "Sentinel: 30% mitigation for 15s (120s CD) - use for heavy damage",
            "Bulwark: Increases block rate for 10s (90s CD) - synergizes with Sheltron",
            "Stack mitigations by combining different abilities, not by overlapping the same one",
        },
        RelatedAbilities = new[] { "Rampart", "Sentinel", "Bulwark", "Reprisal", "Arm's Length" },
        Tips = new[]
        {
            "Alternate Rampart and Sentinel to always have mitigation available",
            "Bulwark + Sheltron = high block rate for sustained damage",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "pld.lesson_3",
        JobPrefix = "pld",
        Title = "Hallowed Ground",
        LessonNumber = 3,
        Description = "Master Paladin's invulnerability: when to use Hallowed Ground, timing considerations, and emergency situations.",
        Prerequisites = new[] { "pld.lesson_2" },
        ConceptsCovered = new[]
        {
            PldConcepts.HallowedGround,
            PldConcepts.InvulnTiming,
        },
        KeyPoints = new[]
        {
            "Hallowed Ground: Complete invulnerability for 10s (420s CD)",
            "The longest cooldown of all tank invulns - use it wisely",
            "Best invuln in the game - you take 0 damage, no healer attention needed",
            "Can be used proactively for big pulls or reactively in emergencies",
        },
        RelatedAbilities = new[] { "Hallowed Ground" },
        Tips = new[]
        {
            "Don't save Hallowed 'just in case' - it's too long a CD to waste",
            "Communicate with healers when you plan to use it for planned damage",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "pld.lesson_4",
        JobPrefix = "pld",
        Title = "Fight or Flight",
        LessonNumber = 4,
        Description = "Optimize your damage: Fight or Flight burst windows, Atonement chains, and Goring Blade DoT management.",
        Prerequisites = new[] { "pld.lesson_3" },
        ConceptsCovered = new[]
        {
            PldConcepts.FightOrFlight,
            PldConcepts.BurstWindow,
            PldConcepts.GoringBlade,
            PldConcepts.AtonementChain,
        },
        KeyPoints = new[]
        {
            "Fight or Flight: 25% damage boost for 20s (60s CD)",
            "Goring Blade combo finisher applies a strong DoT - maintain it",
            "Atonement chain follows Royal Authority - 3 free Atonements",
            "Align your biggest attacks inside Fight or Flight windows",
        },
        RelatedAbilities = new[] { "Fight or Flight", "Goring Blade", "Atonement", "Supplication", "Sepulchre" },
        Tips = new[]
        {
            "Start Fight or Flight after your first GCD to maximize uptime",
            "Atonement > Supplication > Sepulchre is the correct order",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "pld.lesson_5",
        JobPrefix = "pld",
        Title = "Magic Phase",
        LessonNumber = 5,
        Description = "Master PLD's unique magic rotation: Requiescat, Holy Spirit, and the Confiteor blade combo.",
        Prerequisites = new[] { "pld.lesson_4" },
        ConceptsCovered = new[]
        {
            PldConcepts.Requiescat,
            PldConcepts.HolySpirit,
            PldConcepts.Confiteor,
            PldConcepts.BladeCombo,
            PldConcepts.MagicPhase,
        },
        KeyPoints = new[]
        {
            "Requiescat: Enables instant-cast Holy Spirit and Confiteor combo (60s CD)",
            "Holy Spirit: Ranged magic attack - instant during Requiescat",
            "Confiteor + Blade combo: 4-hit finisher that consumes Requiescat stacks",
            "Magic phase provides movement flexibility and high burst damage",
        },
        RelatedAbilities = new[] { "Requiescat", "Holy Spirit", "Holy Circle", "Confiteor", "Blade of Faith", "Blade of Truth", "Blade of Valor" },
        Tips = new[]
        {
            "Use magic phase when you need to move or during downtime",
            "Requiescat stacks are consumed by Confiteor combo - don't waste them",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "pld.lesson_6",
        JobPrefix = "pld",
        Title = "Party Protection",
        LessonNumber = 6,
        Description = "Master PLD's party utility: Divine Veil for party shields, Cover for protecting allies, and Passage of Arms.",
        Prerequisites = new[] { "pld.lesson_5" },
        ConceptsCovered = new[]
        {
            PldConcepts.DivineVeil,
            PldConcepts.Cover,
            PldConcepts.PassageOfArms,
            PldConcepts.PartyProtection,
            PldConcepts.Clemency,
        },
        KeyPoints = new[]
        {
            "Divine Veil: Party shield when you receive healing (90s CD)",
            "Cover: Take damage for a party member (120s CD) - use carefully",
            "Passage of Arms: Channeled 15% party mitigation behind you",
            "Clemency: Emergency heal for yourself or others (costs GCD + MP)",
        },
        RelatedAbilities = new[] { "Divine Veil", "Cover", "Passage of Arms", "Clemency" },
        Tips = new[]
        {
            "Divine Veil requires healing to activate - coordinate with healers",
            "Passage of Arms can be instantly canceled after the buff is applied",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "pld.lesson_7",
        JobPrefix = "pld",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Final lesson: tank swaps, mitigation planning, oGCD weaving, and optimizing your full rotation.",
        Prerequisites = new[] { "pld.lesson_6" },
        ConceptsCovered = new[]
        {
            PldConcepts.TankSwap,
            PldConcepts.Expiacion,
            PldConcepts.CircleOfScorn,
            PldConcepts.Intervene,
        },
        KeyPoints = new[]
        {
            "Provoke + Shirk for smooth tank swaps during mechanics",
            "Expiacion: Strong oGCD damage (30s CD) - weave it",
            "Circle of Scorn: DoT + damage oGCD (30s CD) - use on cooldown",
            "Intervene: Gap closer with 2 charges - save for movement or burst",
        },
        RelatedAbilities = new[] { "Provoke", "Shirk", "Expiacion", "Circle of Scorn", "Intervene" },
        Tips = new[]
        {
            "Plan your mitigation around boss timelines for maximum efficiency",
            "Communicate tank swaps with your co-tank before pull",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// WAR (Ares) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class WarLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "war.lesson_1",
        JobPrefix = "war",
        Title = "Beast Gauge & Tempest",
        LessonNumber = 1,
        Description = "Learn Warrior's core mechanics: Beast Gauge generation, maintaining Surging Tempest buff, and basic combo flow.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            WarConcepts.BeastGauge,
            WarConcepts.SurgingTempest,
        },
        KeyPoints = new[]
        {
            "Beast Gauge builds from your weapon skill combo (10 per combo action)",
            "Surging Tempest: 10% damage buff from Storm's Eye combo finisher",
            "Keep Surging Tempest active at all times - it's your main damage buff",
            "Don't overcap Beast Gauge at 100 - spend it on Fell Cleave",
        },
        RelatedAbilities = new[] { "Defiance", "Heavy Swing", "Maim", "Storm's Path", "Storm's Eye" },
        Tips = new[]
        {
            "Alternate between Storm's Path (gauge) and Storm's Eye (buff refresh)",
            "Surging Tempest can be extended up to 60s - don't over-refresh",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "war.lesson_2",
        JobPrefix = "war",
        Title = "Defensive Fundamentals",
        LessonNumber = 2,
        Description = "Master WAR's defensive toolkit: Rampart, Vengeance, and the powerful Bloodwhetting self-heal.",
        Prerequisites = new[] { "war.lesson_1" },
        ConceptsCovered = new[]
        {
            WarConcepts.Vengeance,
            WarConcepts.Bloodwhetting,
            WarConcepts.RawIntuition,
            WarConcepts.MitigationStacking,
        },
        KeyPoints = new[]
        {
            "Rampart: 20% mitigation for 20s (90s CD) - standard tank cooldown",
            "Vengeance: 30% mitigation + damage reflect for 15s (120s CD)",
            "Bloodwhetting: 10% mitigation + massive healing on attacks (25s CD)",
            "Bloodwhetting makes WAR nearly immortal during large pulls",
        },
        RelatedAbilities = new[] { "Rampart", "Vengeance", "Bloodwhetting", "Raw Intuition", "Reprisal" },
        Tips = new[]
        {
            "Bloodwhetting is your best dungeon cooldown - heals scale with targets hit",
            "Combine Vengeance with AoE attacks for reflect damage in dungeons",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "war.lesson_3",
        JobPrefix = "war",
        Title = "Holmgang",
        LessonNumber = 3,
        Description = "Master Warrior's invulnerability: Holmgang timing, target requirements, and coordinating with healers.",
        Prerequisites = new[] { "war.lesson_2" },
        ConceptsCovered = new[]
        {
            WarConcepts.Holmgang,
            WarConcepts.InvulnTiming,
        },
        KeyPoints = new[]
        {
            "Holmgang: Cannot drop below 1 HP for 10s (240s CD)",
            "Shortest cooldown of all tank invulns - can be used frequently",
            "You WILL drop to 1 HP - healers must heal you after",
            "Binds you to target for duration - can limit movement",
        },
        RelatedAbilities = new[] { "Holmgang" },
        Tips = new[]
        {
            "Use Holmgang aggressively due to its short cooldown",
            "Bloodwhetting after Holmgang can self-heal you back up",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "war.lesson_4",
        JobPrefix = "war",
        Title = "Inner Release",
        LessonNumber = 4,
        Description = "Master WAR's burst window: Inner Release setup, Fell Cleave spam, and maximizing damage during the window.",
        Prerequisites = new[] { "war.lesson_3" },
        ConceptsCovered = new[]
        {
            WarConcepts.InnerRelease,
            WarConcepts.IRWindow,
            WarConcepts.FellCleave,
            WarConcepts.Infuriate,
        },
        KeyPoints = new[]
        {
            "Inner Release: Free Fell Cleaves and guaranteed crits for 15s (60s CD)",
            "Spam Fell Cleave during Inner Release - no Beast Gauge cost",
            "Infuriate: Grants 50 Beast Gauge (60s CD) + Nascent Chaos buff",
            "Fit 5 Fell Cleaves minimum in each Inner Release window",
        },
        RelatedAbilities = new[] { "Inner Release", "Fell Cleave", "Infuriate", "Primal Rend" },
        Tips = new[]
        {
            "Pool Beast Gauge before Inner Release for even more damage",
            "Inner Release + Primal Rend is your biggest burst combo",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "war.lesson_5",
        JobPrefix = "war",
        Title = "Self-Sustain",
        LessonNumber = 5,
        Description = "Master WAR's incredible self-healing: Thrill of Battle, Equilibrium, and Bloodwhetting synergies.",
        Prerequisites = new[] { "war.lesson_4" },
        ConceptsCovered = new[]
        {
            WarConcepts.ThrillOfBattle,
            WarConcepts.Equilibrium,
            WarConcepts.NascentChaos,
        },
        KeyPoints = new[]
        {
            "Thrill of Battle: +20% max HP + healing for 10s (90s CD)",
            "Equilibrium: Strong self-heal + HoT (60s CD)",
            "Nascent Chaos: Inner Chaos instead of Fell Cleave (from Infuriate)",
            "WAR can self-sustain through most content with proper cooldown use",
        },
        RelatedAbilities = new[] { "Thrill of Battle", "Equilibrium", "Inner Chaos" },
        Tips = new[]
        {
            "Thrill before big damage maximizes healing received",
            "Inner Chaos is your strongest single hit - use in burst windows",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "war.lesson_6",
        JobPrefix = "war",
        Title = "Party Protection",
        LessonNumber = 6,
        Description = "Master WAR's party utility: Shake It Off for party shields and Nascent Flash for ally protection.",
        Prerequisites = new[] { "war.lesson_5" },
        ConceptsCovered = new[]
        {
            WarConcepts.ShakeItOff,
            WarConcepts.NascentFlash,
            WarConcepts.PartyProtection,
        },
        KeyPoints = new[]
        {
            "Shake It Off: Party shield that scales with active buffs (90s CD)",
            "Nascent Flash: Share your Bloodwhetting healing with a party member",
            "Shake It Off removes Rampart/Vengeance/etc. but makes bigger shield",
            "Coordinate party mitigation with healers and co-tank",
        },
        RelatedAbilities = new[] { "Shake It Off", "Nascent Flash" },
        Tips = new[]
        {
            "Use Shake It Off before letting buffs expire naturally",
            "Nascent Flash on a DPS during Bloodwhetting keeps them alive",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "war.lesson_7",
        JobPrefix = "war",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Final lesson: gauge pooling, tank swaps, oGCD weaving, and maximizing damage while maintaining survivability.",
        Prerequisites = new[] { "war.lesson_6" },
        ConceptsCovered = new[]
        {
            WarConcepts.GaugePooling,
            WarConcepts.TankSwap,
            WarConcepts.PrimalRend,
            WarConcepts.Upheaval,
            WarConcepts.Onslaught,
            WarConcepts.Orogeny,
        },
        KeyPoints = new[]
        {
            "Pool gauge to 50-80 before Inner Release for maximum burst",
            "Primal Rend: Granted by Inner Release, huge damage, can be delayed",
            "Upheaval: Strong oGCD (30s CD) - weave on cooldown",
            "Onslaught: Gap closer with 3 charges - use for movement or damage",
        },
        RelatedAbilities = new[] { "Provoke", "Shirk", "Primal Rend", "Upheaval", "Onslaught", "Orogeny" },
        Tips = new[]
        {
            "Primal Rend can be held up to 30s for raid buff alignment",
            "Use all 3 Onslaught charges during burst windows if possible",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// DRK (Nyx) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class DrkLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "drk.lesson_1",
        JobPrefix = "drk",
        Title = "Blood Gauge & Darkside",
        LessonNumber = 1,
        Description = "Learn Dark Knight's core mechanics: Blood Gauge, maintaining Darkside buff, and basic MP management.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            DrkConcepts.BloodGauge,
            DrkConcepts.Darkside,
            DrkConcepts.DarksideMaintenance,
        },
        KeyPoints = new[]
        {
            "Blood Gauge builds from combo actions (20 per combo finisher)",
            "Darkside: 10% damage buff - maintained by using Edge/Flood of Shadow",
            "Edge of Shadow costs 3000 MP and refreshes Darkside timer",
            "Never let Darkside fall off - it's your primary damage buff",
        },
        RelatedAbilities = new[] { "Grit", "Hard Slash", "Syphon Strike", "Souleater", "Edge of Shadow" },
        Tips = new[]
        {
            "Use Edge of Shadow when MP is high to prevent overcapping",
            "Darkside timer extends up to 60s - don't over-refresh",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "drk.lesson_2",
        JobPrefix = "drk",
        Title = "Defensive Fundamentals",
        LessonNumber = 2,
        Description = "Master DRK's defensive toolkit: Rampart, Shadow Wall, Dark Mind, and Oblation.",
        Prerequisites = new[] { "drk.lesson_1" },
        ConceptsCovered = new[]
        {
            DrkConcepts.ShadowWall,
            DrkConcepts.DarkMind,
            DrkConcepts.Oblation,
            DrkConcepts.MitigationStacking,
        },
        KeyPoints = new[]
        {
            "Rampart: 20% mitigation for 20s (90s CD) - standard tank cooldown",
            "Shadow Wall: 30% mitigation for 15s (120s CD) - big defensive CD",
            "Dark Mind: 20% magic mitigation (60s CD) - use for magic damage",
            "Oblation: 10% mitigation with 2 charges (60s CD) - use liberally",
        },
        RelatedAbilities = new[] { "Rampart", "Shadow Wall", "Dark Mind", "Oblation", "Reprisal" },
        Tips = new[]
        {
            "Dark Mind only works on magic damage - learn which attacks are magic",
            "Oblation can be used on yourself or party members",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "drk.lesson_3",
        JobPrefix = "drk",
        Title = "Living Dead",
        LessonNumber = 3,
        Description = "Master Dark Knight's unique invulnerability: Living Dead mechanics, Walking Dead state, and healer coordination.",
        Prerequisites = new[] { "drk.lesson_2" },
        ConceptsCovered = new[]
        {
            DrkConcepts.LivingDead,
            DrkConcepts.WalkingDead,
            DrkConcepts.InvulnTiming,
        },
        KeyPoints = new[]
        {
            "Living Dead: 10s buff, if you would die, become Walking Dead instead",
            "Walking Dead: Cannot die for 10s, but must be healed to full or die",
            "Your attacks heal you during Walking Dead - hit things!",
            "Most complex tank invuln - requires healer coordination",
        },
        RelatedAbilities = new[] { "Living Dead" },
        Tips = new[]
        {
            "Walking Dead heals you for damage dealt - AoE attacks help",
            "Communicate with healers before using Living Dead in prog",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "drk.lesson_4",
        JobPrefix = "drk",
        Title = "The Blackest Night",
        LessonNumber = 4,
        Description = "Master DRK's signature ability: The Blackest Night shield timing, Dark Arts proc, and optimal usage.",
        Prerequisites = new[] { "drk.lesson_3" },
        ConceptsCovered = new[]
        {
            DrkConcepts.TheBlackestNight,
            DrkConcepts.TBNManagement,
            DrkConcepts.DarkArts,
        },
        KeyPoints = new[]
        {
            "TBN: 25% max HP shield on self or ally, costs 3000 MP (15s CD)",
            "If shield breaks, you get Dark Arts (free Edge/Flood of Shadow)",
            "TBN is DRK's most powerful defensive and offensive tool",
            "Shield must break fully to get Dark Arts - don't waste on chip damage",
        },
        RelatedAbilities = new[] { "The Blackest Night", "Edge of Shadow", "Flood of Shadow" },
        Tips = new[]
        {
            "TBN before tankbusters guarantees the shield breaks",
            "Don't TBN during low damage - wasted MP if shield doesn't break",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "drk.lesson_5",
        JobPrefix = "drk",
        Title = "Blood Weapon & Delirium",
        LessonNumber = 5,
        Description = "Master DRK's burst windows: Blood Weapon for MP and gauge, Delirium for Bloodspiller spam.",
        Prerequisites = new[] { "drk.lesson_4" },
        ConceptsCovered = new[]
        {
            DrkConcepts.BloodWeapon,
            DrkConcepts.Delirium,
            DrkConcepts.Bloodspiller,
            DrkConcepts.EdgeOfShadow,
        },
        KeyPoints = new[]
        {
            "Blood Weapon: Your attacks restore MP and Blood Gauge for 15s (60s CD)",
            "Delirium: Free Bloodspillers and guaranteed crits for 15s (60s CD)",
            "Bloodspiller: Spends 50 Blood Gauge for high damage",
            "Stack Blood Weapon and Delirium together for massive burst",
        },
        RelatedAbilities = new[] { "Blood Weapon", "Delirium", "Bloodspiller", "Quietus" },
        Tips = new[]
        {
            "Use 5 GCDs during Blood Weapon to maximize MP and gauge gain",
            "Delirium Bloodspillers are free - spam them during the window",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "drk.lesson_6",
        JobPrefix = "drk",
        Title = "Party Protection",
        LessonNumber = 6,
        Description = "Master DRK's party utility: Dark Missionary for party mitigation and Living Shadow for damage.",
        Prerequisites = new[] { "drk.lesson_5" },
        ConceptsCovered = new[]
        {
            DrkConcepts.DarkMissionary,
            DrkConcepts.LivingShadow,
            DrkConcepts.PartyProtection,
        },
        KeyPoints = new[]
        {
            "Dark Missionary: 10% magic mitigation for party for 15s (90s CD)",
            "Living Shadow: Summons shadow clone that attacks for 24s (120s CD)",
            "Living Shadow is a huge DPS gain - never let it sit unused",
            "Coordinate party mitigation with healers and co-tank",
        },
        RelatedAbilities = new[] { "Dark Missionary", "Living Shadow" },
        Tips = new[]
        {
            "Dark Missionary only mitigates magic - learn boss damage types",
            "Living Shadow takes a moment to start attacking after summon",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "drk.lesson_7",
        JobPrefix = "drk",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Final lesson: MP management, tank swaps, oGCD weaving, and maximizing damage output.",
        Prerequisites = new[] { "drk.lesson_6" },
        ConceptsCovered = new[]
        {
            DrkConcepts.TankSwap,
            DrkConcepts.CarveAndSpit,
            DrkConcepts.SaltedEarth,
            DrkConcepts.Shadowbringer,
            DrkConcepts.Disesteem,
        },
        KeyPoints = new[]
        {
            "Manage MP carefully - you need 3000 for TBN and Edge of Shadow",
            "Carve and Spit: Strong oGCD + MP restore (60s CD)",
            "Salted Earth: Ground DoT zone (90s CD) - place under boss",
            "Shadowbringer: Big damage oGCD, 2 charges (60s CD)",
        },
        RelatedAbilities = new[] { "Provoke", "Shirk", "Carve and Spit", "Salted Earth", "Shadowbringer", "Disesteem" },
        Tips = new[]
        {
            "Never overcap MP - dump into Edge of Shadow before Blood Weapon",
            "Salted Earth + Salt and Darkness combo for burst damage",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// GNB (Hephaestus) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class GnbLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "gnb.lesson_1",
        JobPrefix = "gnb",
        Title = "Cartridge Gauge",
        LessonNumber = 1,
        Description = "Learn Gunbreaker's core mechanics: Cartridge Gauge generation, Burst Strike usage, and basic combo flow.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            GnbConcepts.CartridgeGauge,
            GnbConcepts.BurstStrike,
        },
        KeyPoints = new[]
        {
            "Cartridge Gauge builds from Solid Barrel combo finisher (1 cartridge)",
            "Burst Strike: Spends 1 cartridge for high damage",
            "Maximum 3 cartridges - don't overcap",
            "Royal Guard (tank stance) must be active for enmity",
        },
        RelatedAbilities = new[] { "Royal Guard", "Keen Edge", "Brutal Shell", "Solid Barrel", "Burst Strike" },
        Tips = new[]
        {
            "Brutal Shell combo gives a small shield and heal - nice sustain",
            "Save cartridges for No Mercy windows when possible",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "gnb.lesson_2",
        JobPrefix = "gnb",
        Title = "Defensive Fundamentals",
        LessonNumber = 2,
        Description = "Master GNB's defensive toolkit: Rampart, Nebula, Camouflage, and the powerful Heart of Corundum.",
        Prerequisites = new[] { "gnb.lesson_1" },
        ConceptsCovered = new[]
        {
            GnbConcepts.Nebula,
            GnbConcepts.Camouflage,
            GnbConcepts.HeartOfCorundum,
            GnbConcepts.Aurora,
            GnbConcepts.MitigationStacking,
        },
        KeyPoints = new[]
        {
            "Rampart: 20% mitigation for 20s (90s CD) - standard tank cooldown",
            "Nebula: 30% mitigation for 15s (120s CD) - big defensive CD",
            "Camouflage: 10% mitigation + parry rate for 20s (90s CD)",
            "Heart of Corundum: 15% mitigation + heal + shield (25s CD)",
        },
        RelatedAbilities = new[] { "Rampart", "Nebula", "Camouflage", "Heart of Corundum", "Aurora" },
        Tips = new[]
        {
            "Heart of Corundum is extremely strong - use it frequently",
            "Aurora is a HoT that can be used on yourself or allies",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "gnb.lesson_3",
        JobPrefix = "gnb",
        Title = "Superbolide",
        LessonNumber = 3,
        Description = "Master Gunbreaker's invulnerability: Superbolide mechanics, HP drop, and healer coordination.",
        Prerequisites = new[] { "gnb.lesson_2" },
        ConceptsCovered = new[]
        {
            GnbConcepts.Superbolide,
            GnbConcepts.InvulnTiming,
        },
        KeyPoints = new[]
        {
            "Superbolide: Drops HP to 1, then invulnerable for 10s (360s CD)",
            "You WILL drop to 1 HP - healers must heal you after",
            "Can be used reactively when about to die or proactively for big hits",
            "Second shortest tank invuln CD after Holmgang",
        },
        RelatedAbilities = new[] { "Superbolide" },
        Tips = new[]
        {
            "Don't panic healers - communicate before using Superbolide",
            "Heart of Corundum after Superbolide helps with recovery",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "gnb.lesson_4",
        JobPrefix = "gnb",
        Title = "No Mercy Window",
        LessonNumber = 4,
        Description = "Master GNB's burst window: No Mercy setup, Double Down, and maximizing damage during the window.",
        Prerequisites = new[] { "gnb.lesson_3" },
        ConceptsCovered = new[]
        {
            GnbConcepts.NoMercy,
            GnbConcepts.NoMercyWindow,
            GnbConcepts.DoubleDown,
            GnbConcepts.Bloodfest,
        },
        KeyPoints = new[]
        {
            "No Mercy: 20% damage boost for 20s (60s CD)",
            "Double Down: Huge AoE damage, costs 2 cartridges (60s CD)",
            "Bloodfest: Instantly grants 3 cartridges (120s CD)",
            "Pack your biggest hits into No Mercy windows",
        },
        RelatedAbilities = new[] { "No Mercy", "Double Down", "Bloodfest" },
        Tips = new[]
        {
            "Enter No Mercy with 2+ cartridges for Double Down",
            "Bloodfest every other No Mercy to refill cartridges",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "gnb.lesson_5",
        JobPrefix = "gnb",
        Title = "Gnashing Fang Combo",
        LessonNumber = 5,
        Description = "Master GNB's signature combo: Gnashing Fang, Continuation weaving, and the full combo chain.",
        Prerequisites = new[] { "gnb.lesson_4" },
        ConceptsCovered = new[]
        {
            GnbConcepts.GnashingFang,
            GnbConcepts.Continuation,
            GnbConcepts.ContinuationChain,
        },
        KeyPoints = new[]
        {
            "Gnashing Fang: Starts the 3-hit combo, costs 1 cartridge (30s CD)",
            "Each hit enables a Continuation oGCD - weave them!",
            "Gnashing Fang > Jugular Rip > Savage Claw > Abdomen Tear > Wicked Talon > Eye Gouge",
            "This is GNB's most complex and rewarding combo",
        },
        RelatedAbilities = new[] { "Gnashing Fang", "Savage Claw", "Wicked Talon", "Jugular Rip", "Abdomen Tear", "Eye Gouge" },
        Tips = new[]
        {
            "Never skip Continuation - it's free oGCD damage",
            "Practice the rhythm: GCD > oGCD > GCD > oGCD > GCD > oGCD",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "gnb.lesson_6",
        JobPrefix = "gnb",
        Title = "Party Protection",
        LessonNumber = 6,
        Description = "Master GNB's party utility: Heart of Light for party mitigation and Heart of Corundum for ally protection.",
        Prerequisites = new[] { "gnb.lesson_5" },
        ConceptsCovered = new[]
        {
            GnbConcepts.HeartOfLight,
            GnbConcepts.GreatNebula,
            GnbConcepts.PartyProtection,
        },
        KeyPoints = new[]
        {
            "Heart of Light: 10% magic mitigation for party for 15s (90s CD)",
            "Heart of Corundum can be used on allies, not just yourself",
            "Aurora can also be used on party members for healing",
            "Coordinate party mitigation with healers and co-tank",
        },
        RelatedAbilities = new[] { "Heart of Light", "Heart of Corundum", "Aurora" },
        Tips = new[]
        {
            "Heart of Light only mitigates magic - learn boss damage types",
            "Heart of Corundum on healers during heavy damage can save lives",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "gnb.lesson_7",
        JobPrefix = "gnb",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Final lesson: tank swaps, oGCD weaving, Reign of Beasts combo, and optimizing your full rotation.",
        Prerequisites = new[] { "gnb.lesson_6" },
        ConceptsCovered = new[]
        {
            GnbConcepts.TankSwap,
            GnbConcepts.SonicBreak,
            GnbConcepts.BowShock,
            GnbConcepts.ReignOfBeasts,
            GnbConcepts.BlastingZone,
            GnbConcepts.Trajectory,
        },
        KeyPoints = new[]
        {
            "Sonic Break: DoT attack (60s CD) - use in No Mercy",
            "Bow Shock: AoE DoT (60s CD) - use in No Mercy",
            "Reign of Beasts: New combo from No Mercy, 3-hit chain",
            "Blasting Zone: Strong oGCD (30s CD) - weave on cooldown",
        },
        RelatedAbilities = new[] { "Provoke", "Shirk", "Sonic Break", "Bow Shock", "Reign of Beasts", "Blasting Zone", "Trajectory" },
        Tips = new[]
        {
            "Trajectory has 2 charges - save for movement or gap closing",
            "GNB has the busiest rotation - practice oGCD weaving",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// DRG (Zeus) lesson content - 7 progressive lessons covering Dragoon mechanics.
/// </summary>
public static class DrgLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "drg.lesson_1",
        JobPrefix = "drg",
        Title = "Dragoon Fundamentals",
        LessonNumber = 1,
        Description = "Master Dragoon basics: the combo flow, Power Surge buff, and positional requirements.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            DrgConcepts.ComboBasics,
            DrgConcepts.PowerSurge,
            DrgConcepts.Positionals,
        },
        KeyPoints = new[]
        {
            "Two combo paths: True Thrust → Vorpal Thrust → Full Thrust (damage) or True Thrust → Disembowel → Chaos Thrust (DoT + buff)",
            "Disembowel grants Power Surge: +10% damage for 30s - keep this buff up at all times",
            "Chaos Thrust must be used from rear, Full Thrust from flank for maximum damage",
            "Alternate between combos: Chaos Thrust combo → Full Thrust combo → repeat",
        },
        RelatedAbilities = new[] { "True Thrust", "Vorpal Thrust", "Full Thrust", "Disembowel", "Chaos Thrust" },
        Tips = new[]
        {
            "Always refresh Power Surge before it falls off",
            "Missing positionals is a 100 potency loss - use True North when needed",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "drg.lesson_2",
        JobPrefix = "drg",
        Title = "Jump Management",
        LessonNumber = 2,
        Description = "Learn to use Dragoon's signature jump abilities safely and effectively.",
        Prerequisites = new[] { "drg.lesson_1" },
        ConceptsCovered = new[]
        {
            DrgConcepts.HighJump,
            DrgConcepts.MirageDive,
            DrgConcepts.AnimationLock,
        },
        KeyPoints = new[]
        {
            "High Jump: Your main jump oGCD (30s CD) - deals damage and grants Dive Ready",
            "Dive Ready enables Mirage Dive - use it to build Eye gauge (explained in Lesson 3)",
            "Jumps have ~0.8s animation lock - never use during mechanics or AoE indicators",
            "Always weave jumps AFTER your GCD completes to avoid clipping",
        },
        RelatedAbilities = new[] { "High Jump", "Mirage Dive" },
        Tips = new[]
        {
            "Jumps return you to your original position - you won't fly away",
            "Practice jump timing in low-stakes content first",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "drg.lesson_3",
        JobPrefix = "drg",
        Title = "Eye Gauge & Geirskogul",
        LessonNumber = 3,
        Description = "Understand the Eye gauge system and how to activate Life of the Dragon.",
        Prerequisites = new[] { "drg.lesson_2" },
        ConceptsCovered = new[]
        {
            DrgConcepts.EyeGauge,
            DrgConcepts.Geirskogul,
            DrgConcepts.LifeOfDragon,
        },
        KeyPoints = new[]
        {
            "Mirage Dive builds Eye gauge (1 eye per use, max 2)",
            "Geirskogul (60s CD): Line AoE that consumes 1 eye when at 2 eyes to enter Life of the Dragon",
            "At 2 eyes, Geirskogul transforms you into Life of the Dragon state for 20s",
            "Life of the Dragon unlocks Nastrond and Stardiver (covered in Lesson 4)",
        },
        RelatedAbilities = new[] { "Mirage Dive", "Geirskogul" },
        Tips = new[]
        {
            "Build 2 eyes before using Geirskogul to maximize Life of the Dragon uptime",
            "Track your eye count - don't waste Mirage Dive at 2 eyes",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "drg.lesson_4",
        JobPrefix = "drg",
        Title = "Life of the Dragon",
        LessonNumber = 4,
        Description = "Maximize your damage during the powerful Life of the Dragon phase.",
        Prerequisites = new[] { "drg.lesson_3" },
        ConceptsCovered = new[]
        {
            DrgConcepts.Nastrond,
            DrgConcepts.Stardiver,
            DrgConcepts.LifeOptimization,
        },
        KeyPoints = new[]
        {
            "During Life of the Dragon: Geirskogul becomes Nastrond (same CD, no eye cost)",
            "Nastrond: Powerful line AoE - use it 3-4 times during the 20s window",
            "Stardiver: Big dive attack (30s CD) - use once per Life phase for massive damage",
            "Starcross follows Stardiver automatically - free extra damage",
        },
        RelatedAbilities = new[] { "Nastrond", "Stardiver", "Starcross" },
        Tips = new[]
        {
            "Enter Life of the Dragon during buff windows for maximum burst",
            "Don't let Life expire without using Nastrond and Stardiver",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "drg.lesson_5",
        JobPrefix = "drg",
        Title = "Burst Window Setup",
        LessonNumber = 5,
        Description = "Align your buffs and burst abilities for maximum damage output.",
        Prerequisites = new[] { "drg.lesson_4" },
        ConceptsCovered = new[]
        {
            DrgConcepts.LanceCharge,
            DrgConcepts.BattleLitany,
            DrgConcepts.BurstWindow,
        },
        KeyPoints = new[]
        {
            "Lance Charge (60s CD): Personal +10% damage for 20s - your main damage buff",
            "Battle Litany (120s CD): Party-wide +10% crit rate - coordinate with raid buffs",
            "Use Lance Charge on cooldown, align Battle Litany with 2-minute burst windows",
            "Enter Life of the Dragon during Lance Charge for buffed Nastronds and Stardiver",
        },
        RelatedAbilities = new[] { "Lance Charge", "Battle Litany" },
        Tips = new[]
        {
            "Battle Litany benefits the whole party - use it with other raid buffs",
            "Don't hold Lance Charge waiting for Battle Litany - it's on a 60s cycle",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "drg.lesson_6",
        JobPrefix = "drg",
        Title = "Life Surge & Critical Hits",
        LessonNumber = 6,
        Description = "Optimize Life Surge usage and positional recovery with True North.",
        Prerequisites = new[] { "drg.lesson_5" },
        ConceptsCovered = new[]
        {
            DrgConcepts.LifeSurge,
            DrgConcepts.TrueNorthUsage,
            DrgConcepts.PositionalRecovery,
            DrgConcepts.BuffAlignment,
        },
        KeyPoints = new[]
        {
            "Life Surge (40s CD, 2 charges): Guarantees next weaponskill crits + heals you",
            "Always use Life Surge on Heavens' Thrust (your highest potency GCD)",
            "True North (45s CD, 2 charges): Ignore positional requirements for 10s",
            "Save True North for when you can't reach rear/flank due to mechanics",
        },
        RelatedAbilities = new[] { "Life Surge", "True North", "Heavens' Thrust" },
        Tips = new[]
        {
            "Life Surge has 2 charges - don't let them cap",
            "True North also has 2 charges - use one for Chaos Thrust, one for Full Thrust if needed",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "drg.lesson_7",
        JobPrefix = "drg",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Master Wyrmwind Thrust, DoT uptime, AoE rotation, and other advanced techniques.",
        Prerequisites = new[] { "drg.lesson_6" },
        ConceptsCovered = new[]
        {
            DrgConcepts.WyrmwindThrust,
            DrgConcepts.DotMaintenance,
            DrgConcepts.AoeRotation,
            DrgConcepts.FirstmindsFocus,
            DrgConcepts.SpineshatterDive,
            DrgConcepts.DragonfireDive,
        },
        KeyPoints = new[]
        {
            "Wyrmwind Thrust: Unlocked at 2 Firstminds Focus (built from Raiden/Heavens' Thrust)",
            "Chaotic Spring (upgraded Chaos Thrust): Keep the DoT up at all times (~30s)",
            "AoE: Doom Spike → Sonic Thrust → Coerthan Torment at 3+ targets",
            "Spineshatter Dive / Dragonfire Dive: Gap closers with damage - use in burst",
        },
        RelatedAbilities = new[] { "Wyrmwind Thrust", "Chaotic Spring", "Doom Spike", "Sonic Thrust", "Coerthan Torment", "Spineshatter Dive", "Dragonfire Dive" },
        Tips = new[]
        {
            "Wyrmwind Thrust is a ranged attack - useful during forced disengages",
            "Dragonfire Dive is AoE - save for multiple targets when possible",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// MNK (Kratos) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class MnkLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "mnk.lesson_1",
        JobPrefix = "mnk",
        Title = "Monk Fundamentals",
        LessonNumber = 1,
        Description = "Learn the core principles of Monk: the form system, basic combos, and positional requirements.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            MnkConcepts.ComboBasics,
            MnkConcepts.FormSystem,
            MnkConcepts.Positionals,
        },
        KeyPoints = new[]
        {
            "Forms cycle: Opo-opo → Raptor → Coeurl → Opo-opo",
            "Each form has specific weaponskills: Bootshine/Dragon Kick (Opo-opo), True Strike/Twin Snakes (Raptor), Snap Punch/Demolish (Coeurl)",
            "Positionals: Bootshine/Demolish require rear, True Strike/Snap Punch require flank",
            "Hitting positionals correctly grants bonus potency - essential for optimal damage",
        },
        RelatedAbilities = new[] { "Bootshine", "Dragon Kick", "True Strike", "Twin Snakes", "Snap Punch", "Demolish" },
        Tips = new[]
        {
            "The form cycle is automatic - just use the correct GCD for your current form",
            "Learn boss patterns to position yourself for positionals before your GCD lands",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "mnk.lesson_2",
        JobPrefix = "mnk",
        Title = "Maintaining Your Buffs",
        LessonNumber = 2,
        Description = "Master buff maintenance with Disciplined Fist, Demolish DoT, and out-of-combat Meditation.",
        Prerequisites = new[] { "mnk.lesson_1" },
        ConceptsCovered = new[]
        {
            MnkConcepts.DisciplinedFist,
            MnkConcepts.DemolishDot,
            MnkConcepts.Meditation,
        },
        KeyPoints = new[]
        {
            "Disciplined Fist (+15% damage) from Twin Snakes/Demolish - never let this drop",
            "Demolish is a powerful DoT (18s duration) - maintain near 100% uptime",
            "Refresh Demolish when it has ~3 seconds remaining",
            "Meditation: Build Chakra out of combat to start fights with resources ready",
        },
        RelatedAbilities = new[] { "Twin Snakes", "Demolish", "Meditation" },
        Tips = new[]
        {
            "Twin Snakes (Raptor form) and Demolish (Coeurl form) both grant Disciplined Fist",
            "Use Meditation during downtime or before pulls to build Chakra for burst",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "mnk.lesson_3",
        JobPrefix = "mnk",
        Title = "The Chakra System",
        LessonNumber = 3,
        Description = "Understand Chakra generation and spending for optimal damage output.",
        Prerequisites = new[] { "mnk.lesson_2" },
        ConceptsCovered = new[]
        {
            MnkConcepts.ChakraGauge,
            MnkConcepts.TheForbiddenChakra,
            MnkConcepts.Enlightenment,
            MnkConcepts.SteelPeak,
        },
        KeyPoints = new[]
        {
            "Chakra gauge: 0-5 stacks, builds from weaponskills and critical hits",
            "The Forbidden Chakra (5 Chakra): High-potency single-target oGCD",
            "Enlightenment (5 Chakra): AoE alternative for 3+ targets",
            "Never overcap Chakra - spend at 5 stacks before generating more",
        },
        RelatedAbilities = new[] { "The Forbidden Chakra", "Enlightenment", "Steel Peak", "Howling Fist" },
        Tips = new[]
        {
            "Chakra generates faster during Brotherhood buff - more critical hits",
            "Weave Chakra spenders between GCDs to maximize damage",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "mnk.lesson_4",
        JobPrefix = "mnk",
        Title = "Beast Chakra & Masterful Blitz",
        LessonNumber = 4,
        Description = "Master the Beast Chakra system and learn which Blitz to execute based on your combination.",
        Prerequisites = new[] { "mnk.lesson_3" },
        ConceptsCovered = new[]
        {
            MnkConcepts.BeastChakra,
            MnkConcepts.MasterfulBlitz,
            MnkConcepts.ElixirField,
            MnkConcepts.RisingPhoenix,
            MnkConcepts.PhantomRush,
        },
        KeyPoints = new[]
        {
            "Beast Chakra types: Opo-opo (Lunar), Raptor (Solar), Coeurl (Celestial)",
            "3 matching Beast Chakra = Elixir Field (large AoE)",
            "2 Solar + 1 different = Rising Phoenix (cone AoE)",
            "All 3 different types = Phantom Rush (highest single-target, requires Fury buff)",
            "Masterful Blitz becomes available when you have 3 Beast Chakra",
        },
        RelatedAbilities = new[] { "Masterful Blitz", "Elixir Field", "Rising Phoenix", "Phantom Rush", "Elixir Burst", "Flint Strike" },
        Tips = new[]
        {
            "In single-target, aim for Phantom Rush (all 3 different) for max damage",
            "In AoE, Elixir Field (3 matching) hits all nearby enemies",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "mnk.lesson_5",
        JobPrefix = "mnk",
        Title = "Burst Windows",
        LessonNumber = 5,
        Description = "Execute powerful burst windows with Perfect Balance, Riddle of Fire, and Brotherhood.",
        Prerequisites = new[] { "mnk.lesson_4" },
        ConceptsCovered = new[]
        {
            MnkConcepts.PerfectBalance,
            MnkConcepts.RiddleOfFire,
            MnkConcepts.Brotherhood,
            MnkConcepts.BurstAlignment,
        },
        KeyPoints = new[]
        {
            "Perfect Balance (2 charges, 40s recharge): Use any weaponskill regardless of form",
            "Riddle of Fire (60s CD, 20s duration): +15% damage buff - core burst window",
            "Brotherhood (120s CD): Party-wide 5% damage buff + Chakra generation boost",
            "Align Riddle of Fire and Brotherhood with party raid buffs for maximum impact",
        },
        RelatedAbilities = new[] { "Perfect Balance", "Riddle of Fire", "Brotherhood" },
        Tips = new[]
        {
            "Use Perfect Balance to quickly build Beast Chakra during Riddle of Fire",
            "Brotherhood aligns with 2-minute party buffs - coordinate with your group",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "mnk.lesson_6",
        JobPrefix = "mnk",
        Title = "Movement & Utility",
        LessonNumber = 6,
        Description = "Master gap closers, True North usage, and auto-attack optimization.",
        Prerequisites = new[] { "mnk.lesson_5" },
        ConceptsCovered = new[]
        {
            MnkConcepts.Thunderclap,
            MnkConcepts.TrueNorthUsage,
            MnkConcepts.RiddleOfWind,
        },
        KeyPoints = new[]
        {
            "Thunderclap (3 charges): Dash to target - use for gap closing and dodging",
            "True North (2 charges, 45s recharge): Ignore positional requirements for 10s",
            "Riddle of Wind: Increases auto-attack speed - free damage during burst",
            "Save True North for when mechanics prevent proper positioning",
        },
        RelatedAbilities = new[] { "Thunderclap", "True North", "Riddle of Wind" },
        Tips = new[]
        {
            "Don't waste Thunderclap charges - keep at least one for emergency movement",
            "Plan True North usage around fight mechanics you know will disrupt positioning",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "mnk.lesson_7",
        JobPrefix = "mnk",
        Title = "AoE & Optimization",
        LessonNumber = 7,
        Description = "Learn the AoE rotation and advanced optimization techniques.",
        Prerequisites = new[] { "mnk.lesson_6" },
        ConceptsCovered = new[]
        {
            MnkConcepts.AoeCombo,
            MnkConcepts.HowlingFist,
            MnkConcepts.AoeThreshold,
        },
        KeyPoints = new[]
        {
            "AoE combo: Arm of the Destroyer → Four-point Fury → Rockbreaker",
            "Switch to AoE rotation at 3+ targets for efficiency",
            "Howling Fist: AoE oGCD - use with Enlightenment in multi-target",
            "Perfect Balance works in AoE too - quickly build Beast Chakra for Elixir Field",
        },
        RelatedAbilities = new[] { "Arm of the Destroyer", "Four-point Fury", "Rockbreaker", "Howling Fist" },
        Tips = new[]
        {
            "In dungeons, use AoE combo on trash packs, single-target on bosses",
            "Even in AoE, maintain Disciplined Fist buff for the damage bonus",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// SAM (Nike) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class SamLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "sam.lesson_1",
        JobPrefix = "sam",
        Title = "Samurai Fundamentals",
        LessonNumber = 1,
        Description = "Learn the core principles of Samurai: combo routes, Sen collection, and maintaining your damage and haste buffs.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            SamConcepts.ComboBasics,
            SamConcepts.SenSystem,
            SamConcepts.FugetsuBuff,
            SamConcepts.FukaBuff,
        },
        KeyPoints = new[]
        {
            "Three combo routes: Jinpu path (Getsu Sen), Shifu path (Ka Sen), Yukikaze path (Setsu Sen)",
            "Fugetsu buff (+13% damage) comes from Jinpu path - never let this drop",
            "Fuka buff (+13% haste) comes from Shifu path - maintain for faster GCDs",
            "Each combo finisher (Gekko, Kasha, Yukikaze) grants one Sen",
        },
        RelatedAbilities = new[] { "Hakaze", "Gyofu", "Jinpu", "Shifu", "Gekko", "Kasha", "Yukikaze" },
        Tips = new[]
        {
            "Refresh Fugetsu and Fuka when they have ~5 seconds left",
            "If both buffs are about to fall off, prioritize Fugetsu (damage) first",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "sam.lesson_2",
        JobPrefix = "sam",
        Title = "Kenki & Meditation",
        LessonNumber = 2,
        Description = "Master Kenki gauge management and Meditation stacks to maximize your damage output.",
        Prerequisites = new[] { "sam.lesson_1" },
        ConceptsCovered = new[]
        {
            SamConcepts.KenkiGauge,
            SamConcepts.KenkiSpending,
            SamConcepts.Meditation,
        },
        KeyPoints = new[]
        {
            "Kenki builds from combo actions, caps at 100 - never let it overcap",
            "Shinten (25 Kenki): Single-target oGCD - your primary Kenki spender",
            "Kyuten (25 Kenki): AoE alternative for 3+ targets",
            "Meditation stacks (max 3) from Iaijutsu - spend on Shoha for big damage",
        },
        RelatedAbilities = new[] { "Shinten", "Kyuten", "Shoha", "Shoha II" },
        Tips = new[]
        {
            "Spend Kenki freely to avoid overcapping - it's free damage",
            "Save some Kenki for movement if you know mechanics are coming",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "sam.lesson_3",
        JobPrefix = "sam",
        Title = "Iaijutsu System",
        LessonNumber = 3,
        Description = "Learn when to use each Iaijutsu based on your Sen count and combat situation.",
        Prerequisites = new[] { "sam.lesson_2" },
        ConceptsCovered = new[]
        {
            SamConcepts.IaijutsuSelection,
            SamConcepts.HiganbanaDoT,
            SamConcepts.MidareSetsugekka,
            SamConcepts.TenkaGoken,
        },
        KeyPoints = new[]
        {
            "1 Sen = Higanbana: 60s DoT - apply once, then maintain with refreshes",
            "2 Sen = Tenka Goken: AoE damage - only use in multi-target situations",
            "3 Sen = Midare Setsugekka: Your hardest-hitting single GCD - primary burst tool",
            "In single target, always build to 3 Sen for Midare (don't use 1-2 Sen on Higanbana/Tenka)",
        },
        RelatedAbilities = new[] { "Iaijutsu", "Higanbana", "Midare Setsugekka", "Tenka Goken" },
        Tips = new[]
        {
            "Refresh Higanbana when it has 3 seconds or less remaining",
            "In opener, apply Higanbana with your first Sen, then focus on Midare",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "sam.lesson_4",
        JobPrefix = "sam",
        Title = "Tsubame-gaeshi & Meikyo",
        LessonNumber = 4,
        Description = "Master Tsubame-gaeshi follow-ups and Meikyo Shisui's combo-skipping power.",
        Prerequisites = new[] { "sam.lesson_3" },
        ConceptsCovered = new[]
        {
            SamConcepts.TsubameGaeshi,
            SamConcepts.MeikyoShisui,
            SamConcepts.MeikyoFinisherPriority,
        },
        KeyPoints = new[]
        {
            "After any Iaijutsu, Tsubame-gaeshi grants a Kaeshi follow-up (same Sen requirement)",
            "Kaeshi: Setsugekka after Midare is massive damage - never skip it",
            "Meikyo Shisui (3 stacks): Use combo finishers without the combo (skip Hakaze/Jinpu/Shifu)",
            "During Meikyo, prioritize missing Sen types, then Gekko for highest potency",
        },
        RelatedAbilities = new[] { "Tsubame-gaeshi", "Kaeshi: Setsugekka", "Kaeshi: Goken", "Kaeshi: Higanbana", "Meikyo Shisui" },
        Tips = new[]
        {
            "Don't use Meikyo just to skip combos - use it to quickly build Sen or refresh buffs",
            "Meikyo + 3 finishers = instant 3 Sen for Midare",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "sam.lesson_5",
        JobPrefix = "sam",
        Title = "Ikishoten Burst Window",
        LessonNumber = 5,
        Description = "Execute the powerful Ikishoten burst sequence with Ogi Namikiri and Zanshin.",
        Prerequisites = new[] { "sam.lesson_4" },
        ConceptsCovered = new[]
        {
            SamConcepts.IkishotenBurst,
            SamConcepts.OgiNamikiri,
            SamConcepts.Zanshin,
            SamConcepts.SeneiTiming,
        },
        KeyPoints = new[]
        {
            "Ikishoten (120s CD): Grants 50 Kenki + Ogi Namikiri Ready",
            "Ogi Namikiri: Your highest potency GCD - use immediately after Ikishoten",
            "Kaeshi: Namikiri follows Ogi Namikiri - another massive hit",
            "Zanshin: Enhanced Kenki spender available after Ogi Namikiri sequence",
            "Senei (120s CD): High-potency oGCD - align with burst window",
        },
        RelatedAbilities = new[] { "Ikishoten", "Ogi Namikiri", "Kaeshi: Namikiri", "Zanshin", "Senei" },
        Tips = new[]
        {
            "Don't use Ikishoten above 50 Kenki - you'll overcap and waste gauge",
            "Full burst: Ikishoten → Ogi Namikiri → Kaeshi: Namikiri → Zanshin → Senei",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "sam.lesson_6",
        JobPrefix = "sam",
        Title = "Positionals & True North",
        LessonNumber = 6,
        Description = "Maximize damage by hitting positionals correctly and using True North when needed.",
        Prerequisites = new[] { "sam.lesson_5" },
        ConceptsCovered = new[]
        {
            SamConcepts.Positionals,
            SamConcepts.TrueNorthUsage,
            SamConcepts.PositionalRecovery,
        },
        KeyPoints = new[]
        {
            "Gekko: Rear positional - stand behind the boss",
            "Kasha: Flank positional - stand at the boss's side",
            "Missing a positional loses significant potency - the attack still works but hits weaker",
            "True North (45s CD, 2 charges): Ignores positional requirements for 10s",
        },
        RelatedAbilities = new[] { "Gekko", "Kasha", "True North" },
        Tips = new[]
        {
            "Plan Meikyo Shisui finishers around your position - don't waste True North",
            "Save True North for when you physically can't reach the correct position",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "sam.lesson_7",
        JobPrefix = "sam",
        Title = "Advanced Optimization",
        LessonNumber = 7,
        Description = "Advanced techniques including burst alignment, Meikyo buff management, AoE rotation, and Hagakure usage.",
        Prerequisites = new[] { "sam.lesson_6" },
        ConceptsCovered = new[]
        {
            SamConcepts.BurstAlignment,
            SamConcepts.MeikyoBuffRefresh,
            SamConcepts.AoeRotation,
            SamConcepts.HagakureUsage,
        },
        KeyPoints = new[]
        {
            "Ikishoten aligns with 2-minute party buffs - coordinate burst windows",
            "Use Meikyo proactively to quickly refresh expiring buffs (Fugetsu/Fuka)",
            "AoE rotation: Fuko → Mangetsu (Getsu) or Oka (Ka) - only builds 2 Sen types",
            "Hagakure: Converts all Sen to Kenki (10 per Sen) - use before phase transitions",
        },
        RelatedAbilities = new[] { "Fuko", "Mangetsu", "Oka", "Hagakure" },
        Tips = new[]
        {
            "In AoE, you can't build Setsu Sen - adapt your rotation accordingly",
            "Hagakure is great for downtime when you'd waste Sen anyway",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

public static class NinLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "nin.lesson_1",
        JobPrefix = "nin",
        Title = "Ninja Fundamentals",
        LessonNumber = 1,
        Description = "Master Ninja basics: the combo flow, positional requirements, and Kazematoi management.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            NinConcepts.ComboBasics,
            NinConcepts.Positionals,
            NinConcepts.Kazematoi,
        },
        KeyPoints = new[]
        {
            "Basic combo: Spinning Edge → Gust Slash → Aeolian Edge (rear) or Armor Crush (flank)",
            "Aeolian Edge (rear positional) is your highest potency finisher",
            "Armor Crush (flank positional) grants Kazematoi stacks",
            "Kazematoi stacks enhance Aeolian Edge - build them with Armor Crush, spend with Aeolian Edge",
        },
        RelatedAbilities = new[] { "Spinning Edge", "Gust Slash", "Aeolian Edge", "Armor Crush" },
        Tips = new[]
        {
            "Alternate finishers: Armor Crush to build Kazematoi, Aeolian Edge to spend them",
            "Missing positionals loses significant potency - use True North when needed",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "nin.lesson_2",
        JobPrefix = "nin",
        Title = "Mudra Mastery",
        LessonNumber = 2,
        Description = "Learn the mudra system and execute Ninjutsu combinations effectively.",
        Prerequisites = new[] { "nin.lesson_1" },
        ConceptsCovered = new[]
        {
            NinConcepts.MudraSystem,
            NinConcepts.NinjutsuWeaving,
            NinConcepts.Huton,
        },
        KeyPoints = new[]
        {
            "Mudras (Ten, Chi, Jin) combine to create Ninjutsu - memorize the sequences",
            "Raiton (Ten → Chi or Chi → Ten): Single-target damage - your bread and butter",
            "Huton (Jin → Chi → Ten): Grants Huton buff (+15% attack speed) at lower levels",
            "Suiton (Ten → Chi → Jin): Enables Kunai's Bane from range - crucial for burst",
            "Mudras are oGCDs but Ninjutsu is a GCD - weave mudras between GCDs",
        },
        RelatedAbilities = new[] { "Ten", "Chi", "Jin", "Ninjutsu", "Raiton", "Huton", "Suiton" },
        Tips = new[]
        {
            "Practice mudra sequences until they're muscle memory",
            "Bunny (Rabbit Medium) appears if you mess up - no damage, just try again",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "nin.lesson_3",
        JobPrefix = "nin",
        Title = "Ninki & Spenders",
        LessonNumber = 3,
        Description = "Understand Ninki gauge management and when to spend it.",
        Prerequisites = new[] { "nin.lesson_2" },
        ConceptsCovered = new[]
        {
            NinConcepts.NinkiGauge,
            NinConcepts.Bhavacakra,
            NinConcepts.NinkiPooling,
        },
        KeyPoints = new[]
        {
            "Ninki builds from weaponskills and Mug/Dokumori (0-100 gauge)",
            "Bhavacakra (50 Ninki): High-potency single-target oGCD damage",
            "Hellfrog Medium (50 Ninki): AoE alternative for 3+ targets",
            "Pool Ninki before burst windows - spend during Kunai's Bane for buffed damage",
        },
        RelatedAbilities = new[] { "Bhavacakra", "Hellfrog Medium" },
        Tips = new[]
        {
            "Don't cap at 100 Ninki - spend before overcapping",
            "During burst, weave Bhavacakra between GCDs for maximum damage",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "nin.lesson_4",
        JobPrefix = "nin",
        Title = "Burst Window Basics",
        LessonNumber = 4,
        Description = "Set up and execute your burst window with Suiton and Kunai's Bane.",
        Prerequisites = new[] { "nin.lesson_3" },
        ConceptsCovered = new[]
        {
            NinConcepts.Suiton,
            NinConcepts.KunaisBane,
            NinConcepts.MugDokumori,
        },
        KeyPoints = new[]
        {
            "Suiton (Ten → Chi → Jin) grants a buff that enables Kunai's Bane",
            "Kunai's Bane (60s CD): Your primary burst - +10% damage debuff on target",
            "Mug (120s CD): Damage + 40 Ninki generation - use in burst window",
            "Dokumori (120s CD): Replaces Mug at higher levels, grants party buff",
            "Burst order: Suiton → Mug/Dokumori → Kunai's Bane → dump Ninki and oGCDs",
        },
        RelatedAbilities = new[] { "Suiton", "Kunai's Bane", "Mug", "Dokumori" },
        Tips = new[]
        {
            "Always have Suiton up before Kunai's Bane - it's required",
            "Kunai's Bane aligns with 60s party buffs - coordinate with team",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "nin.lesson_5",
        JobPrefix = "nin",
        Title = "Advanced Burst: TCJ & Kassatsu",
        LessonNumber = 5,
        Description = "Master Ten Chi Jin and Kassatsu for maximum burst damage.",
        Prerequisites = new[] { "nin.lesson_4" },
        ConceptsCovered = new[]
        {
            NinConcepts.Kassatsu,
            NinConcepts.TenChiJin,
            NinConcepts.TcjOptimization,
        },
        KeyPoints = new[]
        {
            "Kassatsu (60s CD): Makes next Ninjutsu stronger and instant - use for Hyosho Ranryu",
            "Hyosho Ranryu: Kassatsu-only ice Ninjutsu with massive potency",
            "Ten Chi Jin (120s CD): Execute 3 Ninjutsu in sequence without GCDs between",
            "TCJ sequence: Fuma Shuriken → Raiton → Suiton (standard) for 2-minute burst",
            "During TCJ you cannot move - position safely before activating",
        },
        RelatedAbilities = new[] { "Kassatsu", "Ten Chi Jin", "Hyosho Ranryu" },
        Tips = new[]
        {
            "TCJ ends if you move or take damage - use in safe windows",
            "Align Kassatsu with every Kunai's Bane window for Hyosho Ranryu",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "nin.lesson_6",
        JobPrefix = "nin",
        Title = "Procs & Movement",
        LessonNumber = 6,
        Description = "Handle Raiju procs, Bunshin, and movement abilities effectively.",
        Prerequisites = new[] { "nin.lesson_5" },
        ConceptsCovered = new[]
        {
            NinConcepts.RaijuProcs,
            NinConcepts.Bunshin,
            NinConcepts.PhantomKamaitachi,
            NinConcepts.TenriJindo,
        },
        KeyPoints = new[]
        {
            "Raiton grants Raiju Ready: Choose Forked Raiju (gap closer) or Fleeting Raiju (stationary)",
            "Forked Raiju: Use when you need to close distance to the boss",
            "Fleeting Raiju: Use when already in melee range - same damage, no movement",
            "Bunshin (90s CD, 50 Ninki): Shadow clone that attacks with you - use in burst",
            "Phantom Kamaitachi: Bunshin grants this AoE GCD proc - don't waste it",
            "Tenri Jindo (Lv.100): Proc after Kunai's Bane - big damage finisher",
        },
        RelatedAbilities = new[] { "Forked Raiju", "Fleeting Raiju", "Bunshin", "Phantom Kamaitachi", "Tenri Jindo" },
        Tips = new[]
        {
            "Don't sit on Raiju procs - they expire and you lose damage",
            "Bunshin before burst means more shadow attacks during buffs",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "nin.lesson_7",
        JobPrefix = "nin",
        Title = "Optimization & AoE",
        LessonNumber = 7,
        Description = "Advanced optimization including Kazematoi management, Meisui, True North, and AoE rotation.",
        Prerequisites = new[] { "nin.lesson_6" },
        ConceptsCovered = new[]
        {
            NinConcepts.KazematoiManagement,
            NinConcepts.TrueNorthUsage,
            NinConcepts.BurstAlignment,
            NinConcepts.Meisui,
            NinConcepts.AoeCombo,
            NinConcepts.AoeNinjutsu,
        },
        KeyPoints = new[]
        {
            "Kazematoi: Build with Armor Crush before burst, spend enhanced Aeolian Edges during burst",
            "True North (45s CD, 2 charges): Use when positionals impossible due to mechanics",
            "Meisui: Converts Suiton buff into 50 Ninki - use if you won't need Kunai's Bane",
            "Burst alignment: Coordinate Kunai's Bane with party 60s/120s windows",
            "AoE combo: Death Blossom → Hakke Mujinsatsu at 3+ targets",
            "AoE Ninjutsu: Katon (2+ targets), Doton (3+ stationary), Goka Mekkyaku (Kassatsu AoE)",
        },
        RelatedAbilities = new[] { "True North", "Meisui", "Death Blossom", "Hakke Mujinsatsu", "Katon", "Doton", "Goka Mekkyaku" },
        Tips = new[]
        {
            "Don't waste True North - only use when you truly can't reach the position",
            "Doton is a DoT ground effect - only use if enemies will stand in it",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// RPR (Thanatos) lesson content - 7 lessons covering 25 concepts.
/// </summary>
public static class RprLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "rpr.lesson_1",
        JobPrefix = "rpr",
        Title = "Reaper Fundamentals",
        LessonNumber = 1,
        Description = "Learn the core principles of Reaper: basic combos, Soul gauge building, and Death's Design maintenance.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            RprConcepts.ComboBasics,
            RprConcepts.SoulGauge,
            RprConcepts.SoulSlice,
            RprConcepts.DeathsDesign,
        },
        KeyPoints = new[]
        {
            "Basic combo: Slice → Waxing Slice → Infernal Slice grants 10 Soul Gauge",
            "Soul Slice is an oGCD that grants 50 Soul Gauge (30s recharge, 2 charges)",
            "Death's Design (+10% damage) must be maintained at all times via Shadow of Death",
            "Shadow of Death extends Death's Design by 30s (max 60s) - refresh before it falls off",
        },
        RelatedAbilities = new[] { "Slice", "Waxing Slice", "Infernal Slice", "Soul Slice", "Shadow of Death" },
        Tips = new[]
        {
            "Use Soul Slice on cooldown to prevent overcapping charges",
            "Refresh Death's Design when it has ~10-15 seconds remaining for smooth uptime",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "rpr.lesson_2",
        JobPrefix = "rpr",
        Title = "Soul Reaver & Positionals",
        LessonNumber = 2,
        Description = "Master Soul Reaver state: entering with Blood Stalk/Gluttony, Gibbet/Gallows positionals, and Enhanced procs.",
        Prerequisites = new[] { "rpr.lesson_1" },
        ConceptsCovered = new[]
        {
            RprConcepts.SoulReaver,
            RprConcepts.Gibbet,
            RprConcepts.Gallows,
            RprConcepts.Positionals,
            RprConcepts.EnhancedProcs,
        },
        KeyPoints = new[]
        {
            "Blood Stalk (50 Soul) grants 1 Soul Reaver stack; Gluttony (50 Soul) grants 2 stacks",
            "Gibbet (flank positional): Use from the side, grants Enhanced Gallows buff",
            "Gallows (rear positional): Use from behind, grants Enhanced Gibbet buff",
            "Enhanced procs last 60s - alternate Gibbet/Gallows to always use the Enhanced version",
            "Each Soul Reaver ability grants 10 Shroud Gauge",
        },
        RelatedAbilities = new[] { "Blood Stalk", "Gluttony", "Gibbet", "Gallows", "True North" },
        Tips = new[]
        {
            "Gluttony is preferred over Blood Stalk (2 stacks vs 1) when available",
            "Use True North when positionals are impossible during mechanics",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "rpr.lesson_3",
        JobPrefix = "rpr",
        Title = "Shroud Gauge Management",
        LessonNumber = 3,
        Description = "Understand Shroud gauge building, AoE Soul Reaver with Guillotine, and entering Enshroud.",
        Prerequisites = new[] { "rpr.lesson_2" },
        ConceptsCovered = new[]
        {
            RprConcepts.ShroudGauge,
            RprConcepts.Guillotine,
            RprConcepts.Enshroud,
        },
        KeyPoints = new[]
        {
            "Shroud Gauge builds from Soul Reaver abilities (Gibbet/Gallows/Guillotine = +10 each)",
            "Enshroud requires 50 Shroud Gauge to enter (powerful burst state)",
            "Guillotine is AoE Soul Reaver (no positional) - use at 3+ targets",
            "Don't overcap Shroud at 100 - enter Enshroud before capping",
        },
        RelatedAbilities = new[] { "Gibbet", "Gallows", "Guillotine", "Enshroud" },
        Tips = new[]
        {
            "Plan your Soul Reaver usage to enter Enshroud during raid buff windows",
            "Guillotine still grants Enhanced procs for AoE, but no positional bonus",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "rpr.lesson_4",
        JobPrefix = "rpr",
        Title = "Enshroud Burst Window",
        LessonNumber = 4,
        Description = "Execute the Enshroud burst phase: Lemure Shroud stacks, Void Shroud generation, and Void/Cross Reaping.",
        Prerequisites = new[] { "rpr.lesson_3" },
        ConceptsCovered = new[]
        {
            RprConcepts.LemureShroud,
            RprConcepts.VoidShroud,
            RprConcepts.VoidReaping,
            RprConcepts.GrimReaping,
        },
        KeyPoints = new[]
        {
            "Enshroud grants 5 Lemure Shroud stacks (consume with Void/Cross Reaping)",
            "Void Reaping and Cross Reaping are your main GCDs during Enshroud",
            "Each Void/Cross Reaping grants 1 Void Shroud stack (for oGCDs)",
            "Grim Reaping is the AoE version of Void/Cross Reaping (no positional)",
            "Enshroud lasts 30s - you have time to use all 5 stacks plus finisher",
        },
        RelatedAbilities = new[] { "Enshroud", "Void Reaping", "Cross Reaping", "Grim Reaping" },
        Tips = new[]
        {
            "Alternate Void Reaping and Cross Reaping for the Enhanced damage bonus",
            "Save Enshroud for raid buff windows whenever possible",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "rpr.lesson_5",
        JobPrefix = "rpr",
        Title = "Enshroud Finishers",
        LessonNumber = 5,
        Description = "Master Enshroud finishers: Communio timing, Perfectio proc, Lemure's Slice oGCDs, and Sacrificium.",
        Prerequisites = new[] { "rpr.lesson_4" },
        ConceptsCovered = new[]
        {
            RprConcepts.Communio,
            RprConcepts.Perfectio,
            RprConcepts.LemuresSlice,
            RprConcepts.Sacrificium,
        },
        KeyPoints = new[]
        {
            "Communio: Use when you have 1 Lemure Shroud remaining (high potency finisher)",
            "Communio grants Perfectio Ready buff - use Perfectio immediately after",
            "Lemure's Slice/Scythe cost 2 Void Shroud each - weave them during Enshroud",
            "Sacrificium (Lv.92): Additional finisher oGCD after Void Shroud spending",
        },
        RelatedAbilities = new[] { "Communio", "Perfectio", "Lemure's Slice", "Lemure's Scythe", "Sacrificium" },
        Tips = new[]
        {
            "Standard Enshroud: 4x Void/Cross Reaping → Communio → Perfectio",
            "Weave Lemure's Slice between GCDs when you have 2+ Void Shroud",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "rpr.lesson_6",
        JobPrefix = "rpr",
        Title = "Party Buff Coordination",
        LessonNumber = 6,
        Description = "Maximize party damage with Arcane Circle, Immortal Sacrifice stacks, and Plentiful Harvest.",
        Prerequisites = new[] { "rpr.lesson_5" },
        ConceptsCovered = new[]
        {
            RprConcepts.ArcaneCircle,
            RprConcepts.ImmortalSacrifice,
            RprConcepts.PlentifulHarvest,
        },
        KeyPoints = new[]
        {
            "Arcane Circle: 3% party-wide damage buff for 20s (120s cooldown)",
            "Circle of Sacrifice: Party members under Arcane Circle grant Immortal Sacrifice stacks (max 8)",
            "Plentiful Harvest consumes Immortal Sacrifice stacks for damage + 50 Shroud Gauge",
            "Align Arcane Circle with party 2-minute burst windows for maximum value",
        },
        RelatedAbilities = new[] { "Arcane Circle", "Plentiful Harvest" },
        Tips = new[]
        {
            "Use Arcane Circle at the start of fight and every 2 minutes with party buffs",
            "Plentiful Harvest enables quick Shroud building after Arcane Circle",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "rpr.lesson_7",
        JobPrefix = "rpr",
        Title = "AoE & Movement",
        LessonNumber = 7,
        Description = "Learn the AoE rotation and use Harvest Moon for movement and ranged situations.",
        Prerequisites = new[] { "rpr.lesson_6" },
        ConceptsCovered = new[]
        {
            RprConcepts.AoeRotation,
            RprConcepts.HarvestMoon,
        },
        KeyPoints = new[]
        {
            "AoE threshold: Use AoE rotation at 3+ targets",
            "AoE combo: Spinning Scythe → Nightmare Scythe (grants 10 Soul Gauge)",
            "Whorl of Death applies Death's Design to all targets (AoE version of Shadow of Death)",
            "Harvest Moon: Ranged GCD for movement or disengagement situations",
            "Use Soulsow before combat to have Harvest Moon ready",
        },
        RelatedAbilities = new[] { "Spinning Scythe", "Nightmare Scythe", "Whorl of Death", "Soulsow", "Harvest Moon" },
        Tips = new[]
        {
            "In dungeons, apply Whorl of Death to all enemies before combo",
            "Harvest Moon is perfect for boss disengagement phases",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// VPR (Echidna) lesson content - 7 progressive lessons covering Viper mechanics.
/// </summary>
public static class VprLessons
{
    public static readonly LessonDefinition Lesson1 = new()
    {
        LessonId = "vpr.lesson_1",
        JobPrefix = "vpr",
        Title = "Viper Fundamentals",
        LessonNumber = 1,
        Description = "Learn the core principles of Viper: two-path combo system, buff maintenance with Hunter's Instinct and Swiftscaled, and Honed buff usage.",
        Prerequisites = Array.Empty<string>(),
        ConceptsCovered = new[]
        {
            VprConcepts.ComboBasics,
            VprConcepts.BuffCycling,
            VprConcepts.HonedBuffs,
        },
        KeyPoints = new[]
        {
            "Two combo paths: Steel Fangs path (grants Hunter's Instinct +10% damage) and Reaving Fangs path (grants Swiftscaled +15% auto-attack)",
            "Alternate between paths to maintain both buffs at all times (both last 40s)",
            "Honed Steel/Honed Reavers procs upgrade your combo starters for more damage",
            "Each path: Starter → Flanksting/Hindsting finisher → alternate path next",
        },
        RelatedAbilities = new[] { "Steel Fangs", "Reaving Fangs", "Hunter's Sting", "Swiftskin's Sting", "Flanksting Strike", "Hindsting Strike", "Flanksbane Fang", "Hindsbane Fang" },
        Tips = new[]
        {
            "Never let Hunter's Instinct or Swiftscaled fall off - both are crucial for damage",
            "Use Honed procs immediately - they only last 60s and upgrade your next combo starter",
        },
    };

    public static readonly LessonDefinition Lesson2 = new()
    {
        LessonId = "vpr.lesson_2",
        JobPrefix = "vpr",
        Title = "Resource Management",
        LessonNumber = 2,
        Description = "Master Serpent Offering gauge building, Rattling Coil stacks, and Uncoiled Fury for movement.",
        Prerequisites = new[] { "vpr.lesson_1" },
        ConceptsCovered = new[]
        {
            VprConcepts.SerpentOffering,
            VprConcepts.RattlingCoil,
            VprConcepts.OfferingGeneration,
        },
        KeyPoints = new[]
        {
            "Serpent Offering (0-100): Built by combo finishers (10 each), consumed by Reawaken (50)",
            "Rattling Coil (0-3 stacks): Built by Twinblade combos and Serpent's Ire, consumed by Uncoiled Fury",
            "Uncoiled Fury: Ranged GCD that spends 1 Rattling Coil - use for movement or to avoid overcapping",
            "Don't overcap Serpent Offering at 100 - enter Reawaken before capping",
        },
        RelatedAbilities = new[] { "Uncoiled Fury", "Writhing Snap", "Serpent's Ire" },
        Tips = new[]
        {
            "Use Uncoiled Fury during forced movement phases to maintain DPS",
            "Track Serpent Offering carefully - wasting gauge is a significant DPS loss",
        },
    };

    public static readonly LessonDefinition Lesson3 = new()
    {
        LessonId = "vpr.lesson_3",
        JobPrefix = "vpr",
        Title = "Venom & Positionals",
        LessonNumber = 3,
        Description = "Understand the venom buff system and how it determines which positional finisher to use.",
        Prerequisites = new[] { "vpr.lesson_2" },
        ConceptsCovered = new[]
        {
            VprConcepts.VenomSystem,
            VprConcepts.PositionalFinishers,
            VprConcepts.Positionals,
            VprConcepts.TrueNorthUsage,
        },
        KeyPoints = new[]
        {
            "Venom buffs (Flankstung, Hindstung, Flanksbane, Hindsbane) indicate your NEXT positional",
            "Flankstung/Flanksbane → Use flank (side) positional finisher for bonus potency",
            "Hindstung/Hindsbane → Use rear (behind) positional finisher for bonus potency",
            "True North allows you to ignore positional requirements for 10s (2 charges, 45s recharge)",
        },
        RelatedAbilities = new[] { "Flanksting Strike", "Hindsting Strike", "Flanksbane Fang", "Hindsbane Fang", "True North" },
        Tips = new[]
        {
            "The venom name tells you where to stand: Flank = side, Hind = rear",
            "Save True North charges for mechanics that force bad positioning",
        },
    };

    public static readonly LessonDefinition Lesson4 = new()
    {
        LessonId = "vpr.lesson_4",
        JobPrefix = "vpr",
        Title = "Twinblade Combos",
        LessonNumber = 4,
        Description = "Master the Twinblade system: Vicewinder initiation, Coil follow-ups, and Noxious Gnash maintenance.",
        Prerequisites = new[] { "vpr.lesson_3" },
        ConceptsCovered = new[]
        {
            VprConcepts.DreadCombo,
            VprConcepts.Vicewinder,
            VprConcepts.TwinfangTwinblood,
            VprConcepts.NoxiousGnash,
        },
        KeyPoints = new[]
        {
            "Vicewinder: Starts Twinblade combo, applies Noxious Gnash debuff (+10% damage, 20s)",
            "After Vicewinder, use Hunter's Coil (flank) OR Swiftskin's Coil (rear) based on position",
            "Each Coil grants a Twinfang or Twinblood proc - weave them as oGCDs immediately",
            "Twinblade GCDs are 3.0s recast (faster than normal) - grants 1 Rattling Coil stack",
        },
        RelatedAbilities = new[] { "Vicewinder", "Hunter's Coil", "Swiftskin's Coil", "Twinfang", "Twinblood", "Uncoiled Fury" },
        Tips = new[]
        {
            "Noxious Gnash must be active to enter Reawaken - refresh it with Vicewinder/Vicepit",
            "Use Twinfang/Twinblood immediately after Coils - they're free oGCD damage",
        },
    };

    public static readonly LessonDefinition Lesson5 = new()
    {
        LessonId = "vpr.lesson_5",
        JobPrefix = "vpr",
        Title = "Reawaken Burst",
        LessonNumber = 5,
        Description = "Execute the Reawaken burst phase: entry requirements, Generation GCD sequence, and Legacy oGCD weaving.",
        Prerequisites = new[] { "vpr.lesson_4" },
        ConceptsCovered = new[]
        {
            VprConcepts.ReawakenEntry,
            VprConcepts.GenerationSequence,
            VprConcepts.LegacyWeaving,
            VprConcepts.AnguineTribute,
        },
        KeyPoints = new[]
        {
            "Reawaken entry requires: 50+ Serpent Offering AND Noxious Gnash on target AND 10s+ on buffs",
            "Reawaken grants 5 Anguine Tribute stacks - use Generation GCDs to consume them",
            "Generation sequence: First → Second → Third → Fourth Generation → Ouroboros finisher",
            "Each Generation GCD grants a Legacy oGCD - weave First/Second/Third/Fourth Legacy between GCDs",
        },
        RelatedAbilities = new[] { "Reawaken", "First Generation", "Second Generation", "Third Generation", "Fourth Generation", "First Legacy", "Second Legacy", "Third Legacy", "Fourth Legacy", "Ouroboros" },
        Tips = new[]
        {
            "Never enter Reawaken if buffs will fall off mid-burst - refresh them first",
            "Generation GCDs are 2.1s recast - weave one Legacy oGCD between each",
        },
    };

    public static readonly LessonDefinition Lesson6 = new()
    {
        LessonId = "vpr.lesson_6",
        JobPrefix = "vpr",
        Title = "Burst Optimization",
        LessonNumber = 6,
        Description = "Maximize burst damage with Serpent's Ire timing, Ready to Reawaken proc, and raid buff alignment.",
        Prerequisites = new[] { "vpr.lesson_5" },
        ConceptsCovered = new[]
        {
            VprConcepts.BurstWindow,
            VprConcepts.ReadyToReawaken,
            VprConcepts.SerpentsIre,
            VprConcepts.TimelineAwareness,
        },
        KeyPoints = new[]
        {
            "Serpent's Ire: 2-minute cooldown that grants 1 Rattling Coil and Ready to Reawaken",
            "Ready to Reawaken: Allows free Reawaken without spending Serpent Offering (use immediately!)",
            "Align Reawaken with party raid buff windows (2-minute intervals) for maximum damage",
            "Hold Reawaken briefly if raid buffs are imminent (within 5-10 seconds)",
        },
        RelatedAbilities = new[] { "Serpent's Ire", "Reawaken" },
        Tips = new[]
        {
            "Serpent's Ire is your burst enabler - always use it with party buffs",
            "Don't hold Reawaken too long - losing casts over the fight is worse than missing some buffs",
        },
    };

    public static readonly LessonDefinition Lesson7 = new()
    {
        LessonId = "vpr.lesson_7",
        JobPrefix = "vpr",
        Title = "Complete Rotation",
        LessonNumber = 7,
        Description = "Synthesize all Viper mechanics: full rotation flow, AoE decisions, and movement optimization.",
        Prerequisites = new[] { "vpr.lesson_6" },
        ConceptsCovered = new[]
        {
            VprConcepts.UncoiledFury,
            VprConcepts.AoeRotation,
            VprConcepts.DualWieldAoe,
        },
        KeyPoints = new[]
        {
            "AoE threshold: Use AoE rotation at 3+ targets (Steel Maw → Reaving Maw path)",
            "Vicepit is the AoE version of Vicewinder - applies Noxious Gnash to all targets",
            "AoE Twinblades: Pit combos + Blood/Fang follow-ups for AoE Rattling Coils",
            "Use Uncoiled Fury for movement phases when you can't reach the boss",
        },
        RelatedAbilities = new[] { "Steel Maw", "Reaving Maw", "Vicepit", "Hunter's Den", "Swiftskin's Den", "Uncoiled Twinfang", "Uncoiled Twinblood" },
        Tips = new[]
        {
            "In dungeons, keep Noxious Gnash up with Vicepit for the +10% damage on AoE",
            "Plan Rattling Coil usage around movement - Uncoiled Fury is your mobility tool",
        },
    };

    public static readonly LessonDefinition[] AllLessons = new[]
    {
        Lesson1, Lesson2, Lesson3, Lesson4, Lesson5, Lesson6, Lesson7,
    };
}

/// <summary>
/// Helper class for accessing all lessons.
/// </summary>
public static class LessonRegistry
{
    /// <summary>
    /// Gets all lessons for a specific job.
    /// </summary>
    public static IReadOnlyList<LessonDefinition> GetLessonsForJob(string jobPrefix)
    {
        return jobPrefix.ToLowerInvariant() switch
        {
            // Healers
            "whm" => WhmLessons.AllLessons,
            "sch" => SchLessons.AllLessons,
            "ast" => AstLessons.AllLessons,
            "sge" => SgeLessons.AllLessons,
            // Tanks
            "pld" => PldLessons.AllLessons,
            "war" => WarLessons.AllLessons,
            "drk" => DrkLessons.AllLessons,
            "gnb" => GnbLessons.AllLessons,
            // Melee DPS
            "drg" => DrgLessons.AllLessons,
            "nin" => NinLessons.AllLessons,
            "sam" => SamLessons.AllLessons,
            "mnk" => MnkLessons.AllLessons,
            "rpr" => RprLessons.AllLessons,
            "vpr" => VprLessons.AllLessons,
            _ => Array.Empty<LessonDefinition>(),
        };
    }

    /// <summary>
    /// Gets a specific lesson by ID.
    /// </summary>
    public static LessonDefinition? GetLesson(string lessonId)
    {
        if (string.IsNullOrEmpty(lessonId))
            return null;

        var parts = lessonId.Split('.');
        if (parts.Length < 2)
            return null;

        var lessons = GetLessonsForJob(parts[0]);
        return lessons.FirstOrDefault(l => l.LessonId == lessonId);
    }

    /// <summary>
    /// Gets all lessons across all jobs.
    /// </summary>
    public static IReadOnlyList<LessonDefinition> GetAllLessons()
    {
        return WhmLessons.AllLessons
            .Concat(SchLessons.AllLessons)
            .Concat(AstLessons.AllLessons)
            .Concat(SgeLessons.AllLessons)
            .Concat(PldLessons.AllLessons)
            .Concat(WarLessons.AllLessons)
            .Concat(DrkLessons.AllLessons)
            .Concat(GnbLessons.AllLessons)
            .Concat(DrgLessons.AllLessons)
            .Concat(NinLessons.AllLessons)
            .Concat(SamLessons.AllLessons)
            .Concat(MnkLessons.AllLessons)
            .Concat(RprLessons.AllLessons)
            .Concat(VprLessons.AllLessons)
            .ToArray();
    }
}
