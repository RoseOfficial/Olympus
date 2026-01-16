using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Dark Knight (DRK) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Nyx, the Greek goddess of night and darkness.
/// </summary>
public static class DRKActions
{
    #region Combo Actions (GCD)

    /// <summary>
    /// Hard Slash - Basic combo starter (Lv.1)
    /// </summary>
    public static readonly ActionDefinition HardSlash = new()
    {
        ActionId = 3617,
        Name = "Hard Slash",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Syphon Strike - Combo from Hard Slash (Lv.2)
    /// Restores MP on combo hit
    /// </summary>
    public static readonly ActionDefinition SyphonStrike = new()
    {
        ActionId = 3623,
        Name = "Syphon Strike",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 140 // 340 when combo
        // Combos from Hard Slash, restores 600 MP
    };

    /// <summary>
    /// Souleater - Combo finisher from Syphon Strike (Lv.26)
    /// Heals for portion of damage dealt, grants +20 Blood Gauge
    /// </summary>
    public static readonly ActionDefinition Souleater = new()
    {
        ActionId = 3632,
        Name = "Souleater",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 180, // 480 when combo
        HealPotency = 300
        // Combos from Syphon Strike, grants +20 Blood Gauge
    };

    #endregion

    #region AoE Combo Actions (GCD)

    /// <summary>
    /// Unleash - AoE combo starter (Lv.6)
    /// Circle AoE around self
    /// </summary>
    public static readonly ActionDefinition Unleash = new()
    {
        ActionId = 3621,
        Name = "Unleash",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120
    };

    /// <summary>
    /// Stalwart Soul - AoE combo from Unleash (Lv.40)
    /// Restores MP, grants +20 Blood Gauge
    /// </summary>
    public static readonly ActionDefinition StalwartSoul = new()
    {
        ActionId = 16468,
        Name = "Stalwart Soul",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120 // 160 when combo
        // Combos from Unleash, restores 600 MP, grants +20 Blood Gauge
    };

    #endregion

    #region Blood Gauge Spenders (GCD)

    /// <summary>
    /// Bloodspiller - Primary Blood Gauge spender (Lv.62)
    /// Costs 50 Blood Gauge (free during Delirium)
    /// </summary>
    public static readonly ActionDefinition Bloodspiller = new()
    {
        ActionId = 7392,
        Name = "Bloodspiller",
        MinLevel = 62,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 580
        // Costs 50 Blood Gauge (free during Delirium)
    };

    /// <summary>
    /// Quietus - AoE Blood Gauge spender (Lv.64)
    /// Costs 50 Blood Gauge (free during Delirium)
    /// </summary>
    public static readonly ActionDefinition Quietus = new()
    {
        ActionId = 7391,
        Name = "Quietus",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 240
        // Costs 50 Blood Gauge (free during Delirium)
    };

    #endregion

    #region Delirium Combo Actions (GCD) - Lv.96+

    /// <summary>
    /// Scarlet Delirium - First hit of Delirium combo (Lv.96)
    /// Replaces Bloodspiller during Delirium at Lv.96+
    /// </summary>
    public static readonly ActionDefinition ScarletDelirium = new()
    {
        ActionId = 36928,
        Name = "Scarlet Delirium",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 600
        // Requires Delirium, grants Scorn buff
    };

    /// <summary>
    /// Comeuppance - Second hit of Delirium combo (Lv.96)
    /// Follows Scarlet Delirium
    /// </summary>
    public static readonly ActionDefinition Comeuppance = new()
    {
        ActionId = 36929,
        Name = "Comeuppance",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
        // Follows Scarlet Delirium
    };

    /// <summary>
    /// Torcleaver - Third hit of Delirium combo (Lv.96)
    /// Follows Comeuppance
    /// </summary>
    public static readonly ActionDefinition Torcleaver = new()
    {
        ActionId = 36930,
        Name = "Torcleaver",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 800
        // Follows Comeuppance, grants Scornful Edge buff
    };

    /// <summary>
    /// Disesteem - Follow-up to Torcleaver (Lv.100)
    /// High potency finisher available after Torcleaver
    /// </summary>
    public static readonly ActionDefinition Disesteem = new()
    {
        ActionId = 36931,
        Name = "Disesteem",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1000
        // Requires Scornful Edge buff from Torcleaver
    };

    #endregion

    #region Edge Actions (oGCD) - Darkside Maintenance

    /// <summary>
    /// Edge of Darkness - Pre-74 single target (Lv.40)
    /// Grants Darkside buff, costs 3000 MP
    /// </summary>
    public static readonly ActionDefinition EdgeOfDarkness = new()
    {
        ActionId = 3634,
        Name = "Edge of Darkness",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f, // Short recast, limited by MP
        Range = 3f,
        MpCost = 3000,
        DamagePotency = 300,
        AppliedStatusId = StatusIds.Darkside,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Edge of Shadow - Upgraded Edge of Darkness (Lv.74)
    /// Grants Darkside buff, costs 3000 MP
    /// </summary>
    public static readonly ActionDefinition EdgeOfShadow = new()
    {
        ActionId = 16470,
        Name = "Edge of Shadow",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f, // Short recast, limited by MP
        Range = 3f,
        MpCost = 3000,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.Darkside,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Flood of Darkness - Pre-74 AoE (Lv.30)
    /// Grants Darkside buff, costs 3000 MP
    /// </summary>
    public static readonly ActionDefinition FloodOfDarkness = new()
    {
        ActionId = 16466,
        Name = "Flood of Darkness",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 10f,
        Radius = 5f,
        MpCost = 3000,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.Darkside,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Flood of Shadow - Upgraded Flood of Darkness (Lv.74)
    /// Grants Darkside buff, costs 3000 MP
    /// </summary>
    public static readonly ActionDefinition FloodOfShadow = new()
    {
        ActionId = 16469,
        Name = "Flood of Shadow",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 10f,
        Radius = 5f,
        MpCost = 3000,
        DamagePotency = 160,
        AppliedStatusId = StatusIds.Darkside,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Shadowbringer - High potency oGCD (Lv.90)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition Shadowbringer = new()
    {
        ActionId = 25757,
        Name = "Shadowbringer",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f, // Per charge
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
        // 2 charges
    };

    /// <summary>
    /// Carve and Spit - Single target oGCD (Lv.60)
    /// Restores MP
    /// </summary>
    public static readonly ActionDefinition CarveAndSpit = new()
    {
        ActionId = 3643,
        Name = "Carve and Spit",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 540
        // Restores 600 MP
    };

    /// <summary>
    /// Abyssal Drain - AoE oGCD with heal (Lv.56)
    /// </summary>
    public static readonly ActionDefinition AbyssalDrain = new()
    {
        ActionId = 3641,
        Name = "Abyssal Drain",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 240,
        HealPotency = 200
    };

    /// <summary>
    /// Salted Earth - Ground-targeted DoT (Lv.52)
    /// </summary>
    public static readonly ActionDefinition SaltedEarth = new()
    {
        ActionId = 7394,
        Name = "Salted Earth",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 50 // Per tick
        // Ground DoT, 15s duration
    };

    /// <summary>
    /// Salt and Darkness - Enhances Salted Earth (Lv.86)
    /// Only usable while Salted Earth is active
    /// </summary>
    public static readonly ActionDefinition SaltAndDarkness = new()
    {
        ActionId = 25755,
        Name = "Salt and Darkness",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 500
        // Requires Salted Earth active
    };

    /// <summary>
    /// Plunge - Gap closer (Lv.54)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition Plunge = new()
    {
        ActionId = 3640,
        Name = "Plunge",
        MinLevel = 54,
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
    /// Blood Weapon - MP/Blood regen buff (Lv.35)
    /// Grants MP and Blood on weaponskill hit for 15s
    /// </summary>
    public static readonly ActionDefinition BloodWeapon = new()
    {
        ActionId = 3625,
        Name = "Blood Weapon",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.BloodWeapon,
        AppliedStatusDuration = 15f
        // Grants 600 MP and 10 Blood per weaponskill (5 stacks)
    };

    /// <summary>
    /// Delirium - Burst window buff (Lv.68)
    /// At 68-95: Grants 3 free Bloodspillers
    /// At 96+: Enables Scarlet Delirium combo
    /// </summary>
    public static readonly ActionDefinition Delirium = new()
    {
        ActionId = 7390,
        Name = "Delirium",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Delirium,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Living Shadow - Summons shadow clone (Lv.80)
    /// Clone performs attacks for 24s
    /// </summary>
    public static readonly ActionDefinition LivingShadow = new()
    {
        ActionId = 16472,
        Name = "Living Shadow",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
        // Costs 50 Blood Gauge, summons shadow for 24s
    };

    /// <summary>
    /// Grit - Tank stance (Lv.10)
    /// Toggle - increases enmity
    /// </summary>
    public static readonly ActionDefinition Grit = new()
    {
        ActionId = 3629,
        Name = "Grit",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Grit
    };

    #endregion

    #region Defensive Actions (oGCD)

    /// <summary>
    /// The Blackest Night - Signature shield (Lv.70)
    /// 25% max HP shield, grants Dark Arts if broken
    /// </summary>
    public static readonly ActionDefinition TheBlackestNight = new()
    {
        ActionId = 7393,
        Name = "The Blackest Night",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly, // Can target self or ally
        EffectTypes = ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 30f,
        MpCost = 3000,
        AppliedStatusId = StatusIds.TheBlackestNight,
        AppliedStatusDuration = 7f
        // Shield = 25% of target max HP
        // If broken, grants Dark Arts (free Edge/Flood)
    };

    /// <summary>
    /// Living Dead - Invulnerability (Lv.50)
    /// Cannot die for 10s, then Walking Dead for 10s
    /// </summary>
    public static readonly ActionDefinition LivingDead = new()
    {
        ActionId = 3638,
        Name = "Living Dead",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 300f, // 5 minutes
        MpCost = 0,
        AppliedStatusId = StatusIds.LivingDead,
        AppliedStatusDuration = 10f
        // After Living Dead expires, Walking Dead for 10s
        // During Walking Dead, must be healed to full or die
    };

    /// <summary>
    /// Shadow Wall - Major defensive cooldown (Lv.38)
    /// 30% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition ShadowWall = new()
    {
        ActionId = 3636,
        Name = "Shadow Wall",
        MinLevel = 38,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ShadowWall,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Shadowed Vigil - Enhanced Shadow Wall (Lv.92)
    /// 40% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition ShadowedVigil = new()
    {
        ActionId = 36927,
        Name = "Shadowed Vigil",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ShadowedVigil,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Dark Mind - Magic damage reduction (Lv.45)
    /// 20% magic damage reduction for 10s
    /// </summary>
    public static readonly ActionDefinition DarkMind = new()
    {
        ActionId = 3634,
        Name = "Dark Mind",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.DarkMind,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Dark Missionary - Party magic mitigation (Lv.76)
    /// 10% magic damage reduction for party for 15s
    /// </summary>
    public static readonly ActionDefinition DarkMissionary = new()
    {
        ActionId = 16471,
        Name = "Dark Missionary",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        Radius = 30f,
        AppliedStatusId = StatusIds.DarkMissionary,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Oblation - Short mitigation (Lv.82)
    /// 10% damage reduction for self or ally, 2 charges
    /// </summary>
    public static readonly ActionDefinition Oblation = new()
    {
        ActionId = 25754,
        Name = "Oblation",
        MinLevel = 82,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // Per charge
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Oblation,
        AppliedStatusDuration = 10f
        // 2 charges
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
        AppliedStatusId = StatusIds.Rampart,
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
        AppliedStatusId = StatusIds.Reprisal,
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
        AppliedStatusId = StatusIds.ArmsLength,
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
        // Tank stance
        public const uint Grit = 743;

        // Darkside and Dark Arts
        public const uint Darkside = 751;
        public const uint DarkArts = 1894; // Free Edge/Flood when TBN breaks

        // Buff statuses
        public const uint BloodWeapon = 742;
        public const uint Delirium = 1972;
        public const uint ScornfulEdge = 3837; // From Torcleaver at Lv.96+

        // Defensive buffs
        public const uint LivingDead = 810;
        public const uint WalkingDead = 811; // Critical state after Living Dead
        public const uint ShadowWall = 747;
        public const uint ShadowedVigil = 3836;
        public const uint DarkMind = 746;
        public const uint DarkMissionary = 1894;
        public const uint TheBlackestNight = 1178;
        public const uint Oblation = 2682;
        public const uint SaltedEarth = 749;

        // Role action buffs
        public const uint Rampart = 1191;
        public const uint Reprisal = 1193;
        public const uint ArmsLength = 1209;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Edge action for single target at the player's level.
    /// </summary>
    public static ActionDefinition GetEdgeAction(byte level)
    {
        if (level >= EdgeOfShadow.MinLevel)
            return EdgeOfShadow;
        return EdgeOfDarkness;
    }

    /// <summary>
    /// Gets the best Flood action for AoE at the player's level.
    /// </summary>
    public static ActionDefinition GetFloodAction(byte level)
    {
        if (level >= FloodOfShadow.MinLevel)
            return FloodOfShadow;
        return FloodOfDarkness;
    }

    /// <summary>
    /// Gets the best major defensive cooldown for the player's level.
    /// </summary>
    public static ActionDefinition GetShadowWallAction(byte level)
    {
        if (level >= ShadowedVigil.MinLevel)
            return ShadowedVigil;
        return ShadowWall;
    }

    /// <summary>
    /// Gets the MP cost for Edge/Flood abilities.
    /// </summary>
    public const int EdgeFloodMpCost = 3000;

    /// <summary>
    /// Gets the MP cost for The Blackest Night.
    /// </summary>
    public const int TbnMpCost = 3000;

    /// <summary>
    /// Gets the Blood Gauge cost for Bloodspiller/Quietus.
    /// </summary>
    public const int BloodspillerCost = 50;

    /// <summary>
    /// Gets the Blood Gauge cost for Living Shadow.
    /// </summary>
    public const int LivingShadowCost = 50;

    #endregion
}
