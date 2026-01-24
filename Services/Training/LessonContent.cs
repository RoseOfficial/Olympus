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
            "whm" => WhmLessons.AllLessons,
            "sch" => SchLessons.AllLessons,
            "ast" => AstLessons.AllLessons,
            "sge" => SgeLessons.AllLessons,
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
            .ToArray();
    }
}
