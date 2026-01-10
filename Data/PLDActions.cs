using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Paladin (PLD) and Gladiator (GLA) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Themis, the Greek goddess of divine law and order.
/// </summary>
public static class PLDActions
{
    #region Combo Actions (GCD)

    /// <summary>
    /// Fast Blade - Basic combo starter (Lv.1)
    /// </summary>
    public static readonly ActionDefinition FastBlade = new()
    {
        ActionId = 9,
        Name = "Fast Blade",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Riot Blade - Combo from Fast Blade (Lv.4), restores MP
    /// </summary>
    public static readonly ActionDefinition RiotBlade = new()
    {
        ActionId = 15,
        Name = "Riot Blade",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 140, // 300 when combo
        // Combos from Fast Blade
    };

    /// <summary>
    /// Royal Authority - Combo finisher from Riot Blade (Lv.60)
    /// Grants 3 stacks of Sword Oath (enables Atonement)
    /// </summary>
    public static readonly ActionDefinition RoyalAuthority = new()
    {
        ActionId = 3539,
        Name = "Royal Authority",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 140, // 440 when combo
        // Combos from Riot Blade
        AppliedStatusId = 1902, // Sword Oath (3 stacks)
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Rage of Halone - Combo finisher from Riot Blade (Lv.26)
    /// Pre-level 60 finisher
    /// </summary>
    public static readonly ActionDefinition RageOfHalone = new()
    {
        ActionId = 21,
        Name = "Rage of Halone",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 100, // 330 when combo
        // Combos from Riot Blade
    };

    #endregion

    #region DoT Actions (GCD)

    /// <summary>
    /// Goring Blade - DoT combo action (Lv.54)
    /// Now a standalone action, not part of main combo
    /// </summary>
    public static readonly ActionDefinition GoringBlade = new()
    {
        ActionId = 3538,
        Name = "Goring Blade",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700,
        AppliedStatusId = 725, // Goring Blade DoT
        AppliedStatusDuration = 21f
    };

    /// <summary>
    /// Blade of Honor - Upgraded Goring Blade (Lv.100)
    /// Enhanced version with higher potency
    /// </summary>
    public static readonly ActionDefinition BladeOfHonor = new()
    {
        ActionId = 36922,
        Name = "Blade of Honor",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 1000,
        Radius = 5f // Small AoE around target
    };

    #endregion

    #region Atonement Actions (GCD)

    /// <summary>
    /// Atonement - Spender for Sword Oath stacks (Lv.76)
    /// </summary>
    public static readonly ActionDefinition Atonement = new()
    {
        ActionId = 16460,
        Name = "Atonement",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440
    };

    /// <summary>
    /// Supplication - Follow-up to Atonement (Lv.76)
    /// </summary>
    public static readonly ActionDefinition Supplication = new()
    {
        ActionId = 36918,
        Name = "Supplication",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460
    };

    /// <summary>
    /// Sepulchre - Follow-up to Supplication (Lv.76)
    /// </summary>
    public static readonly ActionDefinition Sepulchre = new()
    {
        ActionId = 36919,
        Name = "Sepulchre",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 480
    };

    #endregion

    #region AoE Combo Actions (GCD)

    /// <summary>
    /// Total Eclipse - AoE combo starter (Lv.6)
    /// </summary>
    public static readonly ActionDefinition TotalEclipse = new()
    {
        ActionId = 7381,
        Name = "Total Eclipse",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Prominence - AoE combo from Total Eclipse (Lv.40)
    /// </summary>
    public static readonly ActionDefinition Prominence = new()
    {
        ActionId = 16457,
        Name = "Prominence",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100, // 170 when combo
        // Combos from Total Eclipse
    };

    #endregion

    #region Magic Phase Actions (GCD)

    /// <summary>
    /// Holy Spirit - Magic attack during Requiescat (Lv.64)
    /// </summary>
    public static readonly ActionDefinition HolySpirit = new()
    {
        ActionId = 7384,
        Name = "Holy Spirit",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f, // Instant during Requiescat
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 1000, // 0 during Requiescat
        DamagePotency = 400
    };

    /// <summary>
    /// Holy Circle - Magic AoE during Requiescat (Lv.72)
    /// </summary>
    public static readonly ActionDefinition HolyCircle = new()
    {
        ActionId = 16458,
        Name = "Holy Circle",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f, // Instant during Requiescat
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 1000, // 0 during Requiescat
        DamagePotency = 200
    };

    /// <summary>
    /// Confiteor - Magic burst finisher during Requiescat (Lv.80)
    /// </summary>
    public static readonly ActionDefinition Confiteor = new()
    {
        ActionId = 16459,
        Name = "Confiteor",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // AoE around target
        MpCost = 0, // Requiescat only
        DamagePotency = 940
    };

    /// <summary>
    /// Blade of Faith - Confiteor combo 1 (Lv.90)
    /// </summary>
    public static readonly ActionDefinition BladeOfFaith = new()
    {
        ActionId = 25748,
        Name = "Blade of Faith",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 740,
        // Combos from Confiteor
    };

    /// <summary>
    /// Blade of Truth - Confiteor combo 2 (Lv.90)
    /// </summary>
    public static readonly ActionDefinition BladeOfTruth = new()
    {
        ActionId = 25749,
        Name = "Blade of Truth",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 840,
        // Combos from Blade of Faith
    };

    /// <summary>
    /// Blade of Valor - Confiteor combo 3 (Lv.90)
    /// </summary>
    public static readonly ActionDefinition BladeOfValor = new()
    {
        ActionId = 25750,
        Name = "Blade of Valor",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 940,
        // Combos from Blade of Truth
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Circle of Scorn - DoT oGCD (Lv.50)
    /// </summary>
    public static readonly ActionDefinition CircleOfScorn = new()
    {
        ActionId = 23,
        Name = "Circle of Scorn",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140,
        AppliedStatusId = 248, // Circle of Scorn DoT
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Expiacion - Single target oGCD (Lv.86)
    /// </summary>
    public static readonly ActionDefinition Expiacion = new()
    {
        ActionId = 25747,
        Name = "Expiacion",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 3f,
        Radius = 5f, // AoE around target
        MpCost = 0,
        DamagePotency = 450
    };

    /// <summary>
    /// Spirits Within - Single target oGCD (Lv.30)
    /// </summary>
    public static readonly ActionDefinition SpiritsWithin = new()
    {
        ActionId = 29,
        Name = "Spirits Within",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 270
    };

    /// <summary>
    /// Intervene - Gap closer (Lv.74)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition Intervene = new()
    {
        ActionId = 16461,
        Name = "Intervene",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 30f, // Per charge
        Range = 20f,
        MpCost = 0,
        DamagePotency = 150
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Fight or Flight - Damage buff (Lv.2)
    /// +25% damage for 20s
    /// </summary>
    public static readonly ActionDefinition FightOrFlight = new()
    {
        ActionId = 20,
        Name = "Fight or Flight",
        MinLevel = 2,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = 76, // Fight or Flight
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Requiescat - Magic phase enabler (Lv.68)
    /// Enables magic attacks and makes them instant/free
    /// </summary>
    public static readonly ActionDefinition Requiescat = new()
    {
        ActionId = 7383,
        Name = "Requiescat",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400,
        AppliedStatusId = 1368, // Requiescat
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Iron Will - Tank stance (Lv.10)
    /// Toggle - increases enmity
    /// </summary>
    public static readonly ActionDefinition IronWill = new()
    {
        ActionId = 28,
        Name = "Iron Will",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2f,
        MpCost = 0,
        AppliedStatusId = 79 // Iron Will
    };

    #endregion

    #region Defensive Actions (oGCD)

    /// <summary>
    /// Sheltron - Oath Gauge shield (Lv.35)
    /// Costs 50 Oath Gauge
    /// </summary>
    public static readonly ActionDefinition Sheltron = new()
    {
        ActionId = 3542,
        Name = "Sheltron",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 5f,
        MpCost = 0,
        AppliedStatusId = 1856, // Sheltron
        AppliedStatusDuration = 6f
    };

    /// <summary>
    /// Holy Sheltron - Enhanced Sheltron (Lv.82)
    /// </summary>
    public static readonly ActionDefinition HolySheltron = new()
    {
        ActionId = 25746,
        Name = "Holy Sheltron",
        MinLevel = 82,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Shield | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 5f,
        MpCost = 0,
        AppliedStatusId = 2674, // Holy Sheltron
        AppliedStatusDuration = 8f
    };

    /// <summary>
    /// Sentinel - Major defensive cooldown (Lv.38)
    /// 30% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition Sentinel = new()
    {
        ActionId = 17,
        Name = "Sentinel",
        MinLevel = 38,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 74, // Sentinel
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Guardian - Enhanced Sentinel (Lv.92)
    /// 40% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition Guardian = new()
    {
        ActionId = 36920,
        Name = "Guardian",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 3835, // Guardian
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Bulwark - AoE block (Lv.52)
    /// 100% block rate for 10s
    /// </summary>
    public static readonly ActionDefinition Bulwark = new()
    {
        ActionId = 22,
        Name = "Bulwark",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = 77, // Bulwark
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Hallowed Ground - Invulnerability (Lv.50)
    /// 10s of immunity
    /// </summary>
    public static readonly ActionDefinition HallowedGround = new()
    {
        ActionId = 30,
        Name = "Hallowed Ground",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 420f, // 7 minutes
        MpCost = 0,
        AppliedStatusId = 82, // Hallowed Ground
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Divine Veil - Party shield (Lv.56)
    /// Shield barrier for party when triggered by healing
    /// </summary>
    public static readonly ActionDefinition DivineVeil = new()
    {
        ActionId = 3540,
        Name = "Divine Veil",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        Radius = 15f,
        AppliedStatusId = 727, // Divine Veil
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Passage of Arms - Directional defense (Lv.70)
    /// Party members behind you take 15% less damage
    /// </summary>
    public static readonly ActionDefinition PassageOfArms = new()
    {
        ActionId = 7385,
        Name = "Passage of Arms",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 1175, // Passage of Arms
        AppliedStatusDuration = 18f
    };

    /// <summary>
    /// Cover - Protect party member (Lv.45)
    /// Take damage for a party member
    /// </summary>
    public static readonly ActionDefinition Cover = new()
    {
        ActionId = 27,
        Name = "Cover",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 10f,
        MpCost = 0,
        AppliedStatusId = 80, // Covered
        AppliedStatusDuration = 12f
    };

    /// <summary>
    /// Clemency - Self/ally heal (Lv.58)
    /// GCD heal with cast time
    /// </summary>
    public static readonly ActionDefinition Clemency = new()
    {
        ActionId = 3541,
        Name = "Clemency",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 2000,
        HealPotency = 1200
    };

    #endregion

    #region Role Actions (oGCD)

    /// <summary>
    /// Rampart - Role action mitigation (Lv.8)
    /// 20% damage reduction for 20s
    /// </summary>
    public static readonly ActionDefinition Rampart = new()
    {
        ActionId = 7531,
        Name = "Rampart",
        MinLevel = 8,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = 1191, // Rampart
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Reprisal - Role action party mitigation (Lv.22)
    /// Enemies deal 10% less damage for 10s
    /// </summary>
    public static readonly ActionDefinition Reprisal = new()
    {
        ActionId = 7535,
        Name = "Reprisal",
        MinLevel = 22,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 60f,
        Radius = 5f,
        MpCost = 0,
        AppliedStatusId = 1193, // Reprisal
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Provoke - Role action enmity (Lv.15)
    /// Puts you at top of enmity list
    /// </summary>
    public static readonly ActionDefinition Provoke = new()
    {
        ActionId = 7533,
        Name = "Provoke",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0
    };

    /// <summary>
    /// Shirk - Role action enmity transfer (Lv.48)
    /// Transfer 25% enmity to target party member
    /// </summary>
    public static readonly ActionDefinition Shirk = new()
    {
        ActionId = 7537,
        Name = "Shirk",
        MinLevel = 48,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 25f,
        MpCost = 0
    };

    /// <summary>
    /// Arm's Length - Role action knockback immunity (Lv.32)
    /// </summary>
    public static readonly ActionDefinition ArmsLength = new()
    {
        ActionId = 7548,
        Name = "Arm's Length",
        MinLevel = 32,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 1209, // Arm's Length
        AppliedStatusDuration = 6f
    };

    /// <summary>
    /// Low Blow - Role action stun (Lv.12)
    /// </summary>
    public static readonly ActionDefinition LowBlow = new()
    {
        ActionId = 7540,
        Name = "Low Blow",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 3f,
        MpCost = 0
    };

    /// <summary>
    /// Interject - Role action silence (Lv.18)
    /// </summary>
    public static readonly ActionDefinition Interject = new()
    {
        ActionId = 7538,
        Name = "Interject",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        public const uint IronWill = 79;
        public const uint FightOrFlight = 76;
        public const uint Requiescat = 1368;
        public const uint SwordOath = 1902;
        public const uint GoringBladeDot = 725;
        public const uint CircleOfScornDot = 248;
        public const uint Sheltron = 1856;
        public const uint HolySheltron = 2674;
        public const uint Sentinel = 74;
        public const uint Guardian = 3835;
        public const uint Bulwark = 77;
        public const uint HallowedGround = 82;
        public const uint DivineVeil = 727;
        public const uint PassageOfArms = 1175;
        public const uint Rampart = 1191;
        public const uint Reprisal = 1193;
        public const uint ArmsLength = 1209;
        public const uint Covered = 80;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best single-target combo finisher for the player's level.
    /// </summary>
    public static ActionDefinition GetComboFinisher(byte level)
    {
        if (level >= RoyalAuthority.MinLevel)
            return RoyalAuthority;
        if (level >= RageOfHalone.MinLevel)
            return RageOfHalone;
        return RiotBlade; // Fallback to Riot Blade
    }

    /// <summary>
    /// Gets the best single-target damage GCD for the player's level (non-combo).
    /// </summary>
    public static ActionDefinition GetSheltronAction(byte level)
    {
        if (level >= HolySheltron.MinLevel)
            return HolySheltron;
        return Sheltron;
    }

    /// <summary>
    /// Gets the best defensive oGCD for the player's level.
    /// </summary>
    public static ActionDefinition GetSentinelAction(byte level)
    {
        if (level >= Guardian.MinLevel)
            return Guardian;
        return Sentinel;
    }

    #endregion
}
