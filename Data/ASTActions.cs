using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Astrologian (AST) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Astraea, the goddess of stars and justice.
/// </summary>
public static class ASTActions
{
    #region GCD Heals

    public static readonly ActionDefinition Benefic = new()
    {
        ActionId = 3594,
        Name = "Benefic",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 500
    };

    public static readonly ActionDefinition BeneficII = new()
    {
        ActionId = 3610,
        Name = "Benefic II",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 700,
        HealPotency = 800
    };

    public static readonly ActionDefinition AspectedBenefic = new()
    {
        ActionId = 3595,
        Name = "Aspected Benefic",
        MinLevel = 34,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 250,
        AppliedStatusId = 835, // Aspected Benefic regen
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition Helios = new()
    {
        ActionId = 3600,
        Name = "Helios",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 700,
        HealPotency = 400
    };

    public static readonly ActionDefinition AspectedHelios = new()
    {
        ActionId = 3601,
        Name = "Aspected Helios",
        MinLevel = 42,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 800,
        HealPotency = 250,
        AppliedStatusId = 836, // Aspected Helios regen
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Helios Conjunction - Upgraded Aspected Helios at level 96.
    /// Higher potency regen.
    /// </summary>
    public static readonly ActionDefinition HeliosConjunction = new()
    {
        ActionId = 37030,
        Name = "Helios Conjunction",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 800,
        HealPotency = 250,
        AppliedStatusId = 3988, // Helios Conjunction regen
        AppliedStatusDuration = 15f
    };

    #endregion

    #region GCD Damage

    public static readonly ActionDefinition Malefic = new()
    {
        ActionId = 3596,
        Name = "Malefic",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 150
    };

    public static readonly ActionDefinition MaleficII = new()
    {
        ActionId = 3598,
        Name = "Malefic II",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 170
    };

    public static readonly ActionDefinition MaleficIII = new()
    {
        ActionId = 7442,
        Name = "Malefic III",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 220
    };

    public static readonly ActionDefinition MaleficIV = new()
    {
        ActionId = 16555,
        Name = "Malefic IV",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 270
    };

    public static readonly ActionDefinition FallMalefic = new()
    {
        ActionId = 25871,
        Name = "Fall Malefic",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 310
    };

    public static readonly ActionDefinition Gravity = new()
    {
        ActionId = 3615,
        Name = "Gravity",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 120
    };

    public static readonly ActionDefinition GravityII = new()
    {
        ActionId = 25872,
        Name = "Gravity II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 130
    };

    #endregion

    #region GCD DoTs

    public static readonly ActionDefinition Combust = new()
    {
        ActionId = 3599,
        Name = "Combust",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 0,
        AppliedStatusId = 838, // Combust DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition CombustII = new()
    {
        ActionId = 3608,
        Name = "Combust II",
        MinLevel = 46,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 0,
        AppliedStatusId = 843, // Combust II DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition CombustIII = new()
    {
        ActionId = 16554,
        Name = "Combust III",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 0,
        AppliedStatusId = 1881, // Combust III DoT
        AppliedStatusDuration = 30f
    };

    #endregion

    #region oGCD Heals

    public static readonly ActionDefinition EssentialDignity = new()
    {
        ActionId = 3614,
        Name = "Essential Dignity",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 40f, // Has 2 charges at level 78
        Range = 30f,
        MpCost = 0,
        HealPotency = 400 // Scales up to 1100 at low HP
    };

    public static readonly ActionDefinition CelestialIntersection = new()
    {
        ActionId = 16556,
        Name = "Celestial Intersection",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 30f, // Has 2 charges
        Range = 30f,
        MpCost = 0,
        HealPotency = 200,
        ShieldPotency = 200
    };

    public static readonly ActionDefinition CelestialOpposition = new()
    {
        ActionId = 16553,
        Name = "Celestial Opposition",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 200,
        AppliedStatusId = 1879, // Opposition regen
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition Exaltation = new()
    {
        ActionId = 25873,
        Name = "Exaltation",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 500, // Delayed heal after 8s
        AppliedStatusId = 2717, // Exaltation buff (10% damage reduction)
        AppliedStatusDuration = 8f
    };

    public static readonly ActionDefinition Horoscope = new()
    {
        ActionId = 16557,
        Name = "Horoscope",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        AppliedStatusId = 1890, // Horoscope buff
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Horoscope activation - Detonates Horoscope for AoE heal.
    /// </summary>
    public static readonly ActionDefinition HoroscopeEnd = new()
    {
        ActionId = 16558,
        Name = "Horoscope",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 400 // 200 base, 400 if enhanced by Helios
    };

    public static readonly ActionDefinition Macrocosmos = new()
    {
        ActionId = 25874,
        Name = "Macrocosmos",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE damage
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f, // Instant
        RecastTime = 180f,
        Range = 25f,
        Radius = 20f,
        MpCost = 0,
        DamagePotency = 250,
        AppliedStatusId = 2718, // Macrocosmos buff on party
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Microcosmos - Detonates Macrocosmos for heal.
    /// </summary>
    public static readonly ActionDefinition Microcosmos = new()
    {
        ActionId = 25875,
        Name = "Microcosmos",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 200 // + 50% of damage absorbed
    };

    #endregion

    #region Earthly Star

    public static readonly ActionDefinition EarthlyStar = new()
    {
        ActionId = 7439,
        Name = "Earthly Star",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.None, // Places star, no immediate effect
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        Radius = 8f,
        MpCost = 0,
        AppliedStatusDuration = 20f // Star lasts 20s if not detonated
    };

    /// <summary>
    /// Stellar Detonation - Detonates Earthly Star.
    /// Giant Dominance (mature) at 10s: 720 potency
    /// Earthly Dominance (immature): 540 potency
    /// </summary>
    public static readonly ActionDefinition StellarDetonation = new()
    {
        ActionId = 8324,
        Name = "Stellar Detonation",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 8f, // Same as star
        MpCost = 0,
        HealPotency = 720, // Giant Dominance
        DamagePotency = 310 // Giant Dominance
    };

    #endregion

    #region Card Actions

    /// <summary>
    /// Astral Draw - Draws cards from the astral deck (Dawntrail rework).
    /// In Dawntrail, AST alternates between Astral and Umbral draws.
    /// Astral Draw provides: Spear (ranged), Arrow (defensive), Spire (curative).
    /// </summary>
    public static readonly ActionDefinition AstralDraw = new()
    {
        ActionId = 37017,
        Name = "Astral Draw",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 55f,
        MpCost = 0
    };

    /// <summary>
    /// Umbral Draw - Draws cards from the umbral deck (Dawntrail rework).
    /// In Dawntrail, AST alternates between Astral and Umbral draws.
    /// Umbral Draw provides: Balance (melee), Bole (defensive), Ewer (curative).
    /// </summary>
    public static readonly ActionDefinition UmbralDraw = new()
    {
        ActionId = 37018,
        Name = "Umbral Draw",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 55f,
        MpCost = 0
    };

    /// <summary>
    /// Play I - Base action slot (transforms to The Balance or The Spear).
    /// Use TheBalance or TheSpear actions instead.
    /// </summary>
    public static readonly ActionDefinition PlayI = new()
    {
        ActionId = 37019,
        Name = "Play I",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3887,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Play II - Base action slot (transforms to The Arrow or The Bole).
    /// </summary>
    public static readonly ActionDefinition PlayII = new()
    {
        ActionId = 37020,
        Name = "Play II",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3888,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Balance - 6% damage buff for melee DPS/tanks, 3% for others.
    /// Available when Balance is drawn from Astral Draw.
    /// </summary>
    public static readonly ActionDefinition TheBalance = new()
    {
        ActionId = 37023,
        Name = "The Balance",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3887, // The Balance buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Spear - 6% damage buff for ranged DPS/healers, 3% for others.
    /// Available when Spear is drawn from Umbral Draw.
    /// </summary>
    public static readonly ActionDefinition TheSpear = new()
    {
        ActionId = 37026,
        Name = "The Spear",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3889, // The Spear buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Bole - Defense buff card from Astral Draw.
    /// </summary>
    public static readonly ActionDefinition TheBole = new()
    {
        ActionId = 37027,
        Name = "The Bole",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3890, // The Bole buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Arrow - Utility card from Astral Draw.
    /// </summary>
    public static readonly ActionDefinition TheArrow = new()
    {
        ActionId = 37024,
        Name = "The Arrow",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3888, // The Arrow buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Ewer - Utility card from Umbral Draw.
    /// </summary>
    public static readonly ActionDefinition TheEwer = new()
    {
        ActionId = 37028,
        Name = "The Ewer",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3891, // The Ewer buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// The Spire - Utility card from Umbral Draw.
    /// </summary>
    public static readonly ActionDefinition TheSpire = new()
    {
        ActionId = 37025,
        Name = "The Spire",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3892, // The Spire buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Play III - Plays Lord of Crowns (AoE damage buff).
    /// </summary>
    public static readonly ActionDefinition PlayIII = new()
    {
        ActionId = 37021,
        Name = "Play III",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 3889, // Lord of Crowns buff
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Minor Arcana - Draws a Minor Arcana card.
    /// </summary>
    public static readonly ActionDefinition MinorArcana = new()
    {
        ActionId = 37022,
        Name = "Minor Arcana",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Lady of Crowns - Party AoE heal from Minor Arcana.
    /// </summary>
    public static readonly ActionDefinition LadyOfCrowns = new()
    {
        ActionId = 7445,
        Name = "Lady of Crowns",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 400
    };

    /// <summary>
    /// Lord of Crowns - Party AoE damage from Minor Arcana.
    /// </summary>
    public static readonly ActionDefinition LordOfCrowns = new()
    {
        ActionId = 7444,
        Name = "Lord of Crowns",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 20f,
        MpCost = 0,
        DamagePotency = 400
    };

    /// <summary>
    /// Astrodyne - Consumes 3 seals for party buff.
    /// Effect varies based on seal diversity.
    /// </summary>
    public static readonly ActionDefinition Astrodyne = new()
    {
        ActionId = 25870,
        Name = "Astrodyne",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Divination - Party damage buff.
    /// </summary>
    public static readonly ActionDefinition Divination = new()
    {
        ActionId = 16552,
        Name = "Divination",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = 1878, // Divination buff
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Oracle - Follow-up damage after Divination.
    /// Requires Divining status from using Divination.
    /// </summary>
    public static readonly ActionDefinition Oracle = new()
    {
        ActionId = 37029,
        Name = "Oracle",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 25f,
        MpCost = 0,
        DamagePotency = 860
    };

    #endregion

    #region Buffs

    public static readonly ActionDefinition Lightspeed = new()
    {
        ActionId = 3606,
        Name = "Lightspeed",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // 2 charges at 90
        MpCost = 0,
        AppliedStatusId = 841, // Lightspeed buff
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition Synastry = new()
    {
        ActionId = 3612,
        Name = "Synastry",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 845, // Synastry buff
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition NeutralSect = new()
    {
        ActionId = 16559,
        Name = "Neutral Sect",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 1892, // Neutral Sect buff
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition CollectiveUnconscious = new()
    {
        ActionId = 3613,
        Name = "Collective Unconscious",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.HoT,
        CastTime = 0f, // Channeled
        RecastTime = 60f,
        Range = 0f,
        Radius = 8f,
        MpCost = 0,
        HealPotency = 100, // Per tick
        AppliedStatusId = 848, // Wheel of Fortune (regen after channel)
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Sun Sign - Follow-up from Neutral Sect.
    /// Party shield and heal.
    /// </summary>
    public static readonly ActionDefinition SunSign = new()
    {
        ActionId = 37031,
        Name = "Sun Sign",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        HealPotency = 500,
        ShieldPotency = 500
    };

    #endregion

    #region Role Actions

    public static readonly ActionDefinition Swiftcast = new()
    {
        ActionId = 7561,
        Name = "Swiftcast",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = 167,
        AppliedStatusDuration = 10f
    };

    public static readonly ActionDefinition LucidDreaming = new()
    {
        ActionId = 7562,
        Name = "Lucid Dreaming",
        MinLevel = 14,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = 1204,
        AppliedStatusDuration = 21f
    };

    public static readonly ActionDefinition Surecast = new()
    {
        ActionId = 7559,
        Name = "Surecast",
        MinLevel = 44,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 160,
        AppliedStatusDuration = 6f
    };

    public static readonly ActionDefinition Rescue = new()
    {
        ActionId = 7571,
        Name = "Rescue",
        MinLevel = 48,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 30f,
        MpCost = 0
    };

    public static readonly ActionDefinition Esuna = new()
    {
        ActionId = 7568,
        Name = "Esuna",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Cleanse,
        CastTime = 1.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400
    };

    public static readonly ActionDefinition Ascend = new()
    {
        ActionId = 3603,
        Name = "Ascend",
        MinLevel = 12,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Raise,
        CastTime = 8.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 2400
    };

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// All AST GCD damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DamageGcds =
    {
        FallMalefic, MaleficIV, MaleficIII, MaleficII, Malefic
    };

    /// <summary>
    /// All AST GCD DoT spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DotGcds =
    {
        CombustIII, CombustII, Combust
    };

    /// <summary>
    /// All AST GCD single-target heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] SingleHealGcds =
    {
        BeneficII, Benefic
    };

    /// <summary>
    /// All AST GCD AoE heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEHealGcds =
    {
        HeliosConjunction, AspectedHelios, Helios
    };

    /// <summary>
    /// All AST GCD AoE damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEDamageGcds =
    {
        GravityII, Gravity
    };

    /// <summary>
    /// Gets the appropriate damage GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetDamageGcdForLevel(byte level)
    {
        foreach (var action in DamageGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Malefic;
    }

    /// <summary>
    /// Gets the appropriate DoT for the player's level.
    /// </summary>
    public static ActionDefinition GetDotForLevel(byte level)
    {
        foreach (var action in DotGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Combust;
    }

    /// <summary>
    /// Gets the appropriate single-target heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetSingleHealGcdForLevel(byte level)
    {
        foreach (var action in SingleHealGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Benefic;
    }

    /// <summary>
    /// Gets the appropriate AoE heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetAoEHealGcdForLevel(byte level)
    {
        foreach (var action in AoEHealGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Helios;
    }

    /// <summary>
    /// Gets the appropriate AoE damage GCD for the player's level.
    /// Returns null if player level is below Gravity (level 45).
    /// </summary>
    public static ActionDefinition? GetAoEDamageGcdForLevel(byte level)
    {
        foreach (var action in AoEDamageGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return null;
    }

    /// <summary>
    /// Alias for GetAoEDamageGcdForLevel.
    /// </summary>
    public static ActionDefinition? GetAoEDamageForLevel(byte level) => GetAoEDamageGcdForLevel(level);

    /// <summary>
    /// Gets the DoT status ID for the player's level.
    /// </summary>
    public static uint GetDotStatusId(byte level)
    {
        if (level >= CombustIII.MinLevel)
            return CombustIII.AppliedStatusId ?? 1881;
        if (level >= CombustII.MinLevel)
            return CombustII.AppliedStatusId ?? 843;
        return Combust.AppliedStatusId ?? 838;
    }

    #endregion

    #region Status IDs

    /// <summary>
    /// Aspected Benefic regen status ID.
    /// </summary>
    public const ushort AspectedBeneficStatusId = 835;

    /// <summary>
    /// Aspected Helios regen status ID.
    /// </summary>
    public const ushort AspectedHeliosStatusId = 836;

    /// <summary>
    /// Helios Conjunction regen status ID.
    /// </summary>
    public const ushort HeliosConjunctionStatusId = 3988;

    /// <summary>
    /// Combust DoT status ID.
    /// </summary>
    public const ushort CombustStatusId = 838;

    /// <summary>
    /// Combust II DoT status ID.
    /// </summary>
    public const ushort CombustIIStatusId = 843;

    /// <summary>
    /// Combust III DoT status ID.
    /// </summary>
    public const ushort CombustIIIStatusId = 1881;

    /// <summary>
    /// Lightspeed buff status ID.
    /// </summary>
    public const ushort LightspeedStatusId = 841;

    /// <summary>
    /// Synastry buff status ID.
    /// </summary>
    public const ushort SynastryStatusId = 845;

    /// <summary>
    /// Divination buff status ID.
    /// </summary>
    public const ushort DivinationStatusId = 1878;

    /// <summary>
    /// Divining status ID (Oracle proc).
    /// </summary>
    public const ushort DiviningStatusId = 3893;

    /// <summary>
    /// Neutral Sect buff status ID.
    /// </summary>
    public const ushort NeutralSectStatusId = 1892;

    /// <summary>
    /// Horoscope buff status ID.
    /// </summary>
    public const ushort HoroscopeStatusId = 1890;

    /// <summary>
    /// Horoscope Helios (enhanced) buff status ID.
    /// </summary>
    public const ushort HoroscopeHeliosStatusId = 1891;

    /// <summary>
    /// Macrocosmos buff status ID.
    /// </summary>
    public const ushort MacrocosmosStatusId = 2718;

    /// <summary>
    /// Celestial Opposition regen status ID.
    /// </summary>
    public const ushort CelestialOppositionStatusId = 1879;

    /// <summary>
    /// Exaltation buff status ID.
    /// </summary>
    public const ushort ExaltationStatusId = 2717;

    /// <summary>
    /// Collective Unconscious (mitigation while channeling) status ID.
    /// </summary>
    public const ushort CollectiveUnconsciousStatusId = 849;

    /// <summary>
    /// Wheel of Fortune (regen after Collective Unconscious) status ID.
    /// </summary>
    public const ushort WheelOfFortuneStatusId = 848;

    /// <summary>
    /// Earthly Dominance (immature star) status ID.
    /// </summary>
    public const ushort EarthlyDominanceStatusId = 1224;

    /// <summary>
    /// Giant Dominance (mature star) status ID.
    /// </summary>
    public const ushort GiantDominanceStatusId = 1248;

    /// <summary>
    /// The Balance card buff status ID.
    /// </summary>
    public const ushort TheBalanceStatusId = 3887;

    /// <summary>
    /// The Spear card buff status ID.
    /// </summary>
    public const ushort TheSpearStatusId = 3888;

    /// <summary>
    /// Lord of Crowns buff status ID.
    /// </summary>
    public const ushort LordOfCrownsStatusId = 3889;

    /// <summary>
    /// Lady of Crowns buff status ID.
    /// </summary>
    public const ushort LadyOfCrownsStatusId = 3890;

    /// <summary>
    /// Sun Sign buff status ID.
    /// </summary>
    public const ushort SunSignStatusId = 3896;

    #endregion

    #region Card Types

    /// <summary>
    /// Card types in the Astrologian card system.
    /// </summary>
    public enum CardType : byte
    {
        None = 0,
        TheBalance = 1,  // Melee DPS buff
        TheSpear = 2,    // Ranged DPS buff
        Lord = 3,        // AoE damage buff
        Lady = 4         // AoE heal
    }

    /// <summary>
    /// Seal types for Astrodyne.
    /// </summary>
    public enum SealType : byte
    {
        None = 0,
        Lunar = 1,
        Solar = 2,
        Celestial = 3
    }

    #endregion
}
