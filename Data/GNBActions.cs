using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Gunbreaker (GNB) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Hephaestus, the Greek god of forge and weapons.
/// </summary>
public static class GNBActions
{
    #region Combo Actions (GCD)

    /// <summary>
    /// Keen Edge - Basic combo starter (Lv.1)
    /// </summary>
    public static readonly ActionDefinition KeenEdge = new()
    {
        ActionId = 16137,
        Name = "Keen Edge",
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
    /// Brutal Shell - Combo from Keen Edge (Lv.4)
    /// Heals self and grants shield on combo hit
    /// </summary>
    public static readonly ActionDefinition BrutalShell = new()
    {
        ActionId = 16139,
        Name = "Brutal Shell",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 160, // 300 when combo
        HealPotency = 200, // Heal on combo
        ShieldPotency = 200, // Shield on combo
        AppliedStatusId = StatusIds.BrutalShell,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Solid Barrel - Combo finisher from Brutal Shell (Lv.26)
    /// Grants +1 Cartridge
    /// </summary>
    public static readonly ActionDefinition SolidBarrel = new()
    {
        ActionId = 16145,
        Name = "Solid Barrel",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 180 // 360 when combo, grants +1 Cartridge
    };

    #endregion

    #region AoE Combo Actions (GCD)

    /// <summary>
    /// Demon Slice - AoE combo starter (Lv.10)
    /// Circle AoE around self
    /// </summary>
    public static readonly ActionDefinition DemonSlice = new()
    {
        ActionId = 16141,
        Name = "Demon Slice",
        MinLevel = 10,
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
    /// Demon Slaughter - AoE combo from Demon Slice (Lv.40)
    /// Grants +1 Cartridge on combo
    /// </summary>
    public static readonly ActionDefinition DemonSlaughter = new()
    {
        ActionId = 16149,
        Name = "Demon Slaughter",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100 // 160 when combo, grants +1 Cartridge
    };

    #endregion

    #region Gnashing Fang Combo (GCD)

    /// <summary>
    /// Gnashing Fang - First hit of signature combo (Lv.60)
    /// Costs 1 Cartridge, grants Ready to Rip
    /// </summary>
    public static readonly ActionDefinition GnashingFang = new()
    {
        ActionId = 16146,
        Name = "Gnashing Fang",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f, // Has its own recast
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500,
        AppliedStatusId = StatusIds.ReadyToRip,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Savage Claw - Second hit of Gnashing Fang combo (Lv.60)
    /// Follows Gnashing Fang, grants Ready to Tear
    /// </summary>
    public static readonly ActionDefinition SavageClaw = new()
    {
        ActionId = 16147,
        Name = "Savage Claw",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 560,
        AppliedStatusId = StatusIds.ReadyToTear,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Wicked Talon - Third hit of Gnashing Fang combo (Lv.60)
    /// Follows Savage Claw, grants Ready to Gouge
    /// </summary>
    public static readonly ActionDefinition WickedTalon = new()
    {
        ActionId = 16150,
        Name = "Wicked Talon",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 620,
        AppliedStatusId = StatusIds.ReadyToGouge,
        AppliedStatusDuration = 10f
    };

    #endregion

    #region Continuation Actions (oGCD)

    /// <summary>
    /// Continuation - Base action that transforms (Lv.70)
    /// Becomes Jugular Rip, Abdomen Tear, Eye Gouge, or Hypervelocity
    /// </summary>
    public static readonly ActionDefinition Continuation = new()
    {
        ActionId = 16155,
        Name = "Continuation",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 0 // Transforms into actual action
    };

    /// <summary>
    /// Jugular Rip - Follow-up to Gnashing Fang (Lv.70)
    /// Must be used before next GCD or buff expires
    /// </summary>
    public static readonly ActionDefinition JugularRip = new()
    {
        ActionId = 16156,
        Name = "Jugular Rip",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 240
    };

    /// <summary>
    /// Abdomen Tear - Follow-up to Savage Claw (Lv.70)
    /// Must be used before next GCD or buff expires
    /// </summary>
    public static readonly ActionDefinition AbdomenTear = new()
    {
        ActionId = 16157,
        Name = "Abdomen Tear",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 280
    };

    /// <summary>
    /// Eye Gouge - Follow-up to Wicked Talon (Lv.70)
    /// Must be used before next GCD or buff expires
    /// </summary>
    public static readonly ActionDefinition EyeGouge = new()
    {
        ActionId = 16158,
        Name = "Eye Gouge",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 320
    };

    /// <summary>
    /// Hypervelocity - Follow-up to Burst Strike (Lv.86)
    /// </summary>
    public static readonly ActionDefinition Hypervelocity = new()
    {
        ActionId = 25759,
        Name = "Hypervelocity",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 200
    };

    #endregion

    #region Cartridge Spenders (GCD)

    /// <summary>
    /// Burst Strike - Primary cartridge spender (Lv.30)
    /// Costs 1 Cartridge, grants Ready to Blast at Lv.86+
    /// </summary>
    public static readonly ActionDefinition BurstStrike = new()
    {
        ActionId = 16162,
        Name = "Burst Strike",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.ReadyToBlast,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Fated Circle - AoE cartridge spender (Lv.72)
    /// Costs 1 Cartridge
    /// </summary>
    public static readonly ActionDefinition FatedCircle = new()
    {
        ActionId = 16163,
        Name = "Fated Circle",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Double Down - High potency cartridge spender (Lv.90)
    /// Costs 2 Cartridges, AoE
    /// </summary>
    public static readonly ActionDefinition DoubleDown = new()
    {
        ActionId = 25760,
        Name = "Double Down",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f, // Has its own recast
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1200 // First target, 15% falloff on others
    };

    #endregion

    #region Reign of Beasts Combo (GCD) - Lv.96+

    /// <summary>
    /// Reign of Beasts - Burst finisher (Lv.100)
    /// Available after No Mercy, grants Ready to Reign
    /// </summary>
    public static readonly ActionDefinition ReignOfBeasts = new()
    {
        ActionId = 36937,
        Name = "Reign of Beasts",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Noble Blood - Follow-up to Reign of Beasts (Lv.100)
    /// </summary>
    public static readonly ActionDefinition NobleBlood = new()
    {
        ActionId = 36938,
        Name = "Noble Blood",
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
    };

    /// <summary>
    /// Lion Heart - Final hit of Reign combo (Lv.100)
    /// </summary>
    public static readonly ActionDefinition LionHeart = new()
    {
        ActionId = 36939,
        Name = "Lion Heart",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1200
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Rough Divide - Gap closer (Lv.56)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition RoughDivide = new()
    {
        ActionId = 16154,
        Name = "Rough Divide",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 30f, // Per charge
        Range = 20f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Danger Zone - Single target oGCD (Lv.18)
    /// Upgraded to Blasting Zone at Lv.80
    /// </summary>
    public static readonly ActionDefinition DangerZone = new()
    {
        ActionId = 16144,
        Name = "Danger Zone",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Blasting Zone - Upgraded Danger Zone (Lv.80)
    /// </summary>
    public static readonly ActionDefinition BlastingZone = new()
    {
        ActionId = 16165,
        Name = "Blasting Zone",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Bow Shock - AoE oGCD with DoT (Lv.62)
    /// </summary>
    public static readonly ActionDefinition BowShock = new()
    {
        ActionId = 16159,
        Name = "Bow Shock",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 150, // Plus 60 potency DoT for 15s
        AppliedStatusId = StatusIds.BowShock,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Sonic Break - Single target DoT (Lv.54)
    /// </summary>
    public static readonly ActionDefinition SonicBreak = new()
    {
        ActionId = 16153,
        Name = "Sonic Break",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f, // Has its own recast
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300, // Plus 60 potency DoT for 30s
        AppliedStatusId = StatusIds.SonicBreak,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// No Mercy - Damage buff (Lv.2)
    /// 20% damage increase for 20s
    /// </summary>
    public static readonly ActionDefinition NoMercy = new()
    {
        ActionId = 16138,
        Name = "No Mercy",
        MinLevel = 2,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.NoMercy,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Bloodfest - Cartridge generator (Lv.76)
    /// Grants 3 Cartridges (max 3, can temporarily exceed with Bloodfest Ready)
    /// </summary>
    public static readonly ActionDefinition Bloodfest = new()
    {
        ActionId = 16164,
        Name = "Bloodfest",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
        // Grants 3 Cartridges and Ready to Reign at Lv.100
    };

    /// <summary>
    /// Royal Guard - Tank stance (Lv.10)
    /// Toggle - increases enmity generation
    /// </summary>
    public static readonly ActionDefinition RoyalGuard = new()
    {
        ActionId = 16142,
        Name = "Royal Guard",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RoyalGuard
    };

    #endregion

    #region Defensive Actions (oGCD)

    /// <summary>
    /// Camouflage - Parry buff (Lv.6)
    /// 50% parry rate for 20s, 10% damage reduction
    /// </summary>
    public static readonly ActionDefinition Camouflage = new()
    {
        ActionId = 16140,
        Name = "Camouflage",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Camouflage,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Nebula - Major defensive cooldown (Lv.38)
    /// 30% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition Nebula = new()
    {
        ActionId = 16148,
        Name = "Nebula",
        MinLevel = 38,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Nebula,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Great Nebula - Enhanced Nebula (Lv.92)
    /// 40% damage reduction for 15s
    /// </summary>
    public static readonly ActionDefinition GreatNebula = new()
    {
        ActionId = 36935,
        Name = "Great Nebula",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.GreatNebula,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Heart of Stone - Short defensive (Lv.68)
    /// 15% damage reduction for 7s, can target self or ally
    /// </summary>
    public static readonly ActionDefinition HeartOfStone = new()
    {
        ActionId = 16161,
        Name = "Heart of Stone",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.HeartOfStone,
        AppliedStatusDuration = 7f
    };

    /// <summary>
    /// Heart of Corundum - Enhanced Heart of Stone (Lv.82)
    /// 15% damage reduction for 4s, additional effects
    /// - Grants Catharsis: Heal when HP falls below 50%
    /// - Grants Clarity of Corundum: Extends duration by 4s
    /// </summary>
    public static readonly ActionDefinition HeartOfCorundum = new()
    {
        ActionId = 25758,
        Name = "Heart of Corundum",
        MinLevel = 82,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.HeartOfCorundum,
        AppliedStatusDuration = 4f // Base duration, can be extended
    };

    /// <summary>
    /// Superbolide - Invulnerability (Lv.50)
    /// Cannot take damage for 10s, sets HP to 1
    /// </summary>
    public static readonly ActionDefinition Superbolide = new()
    {
        ActionId = 16152,
        Name = "Superbolide",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 360f, // 6 minutes
        MpCost = 0,
        AppliedStatusId = StatusIds.Superbolide,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Aurora - Self or ally HoT (Lv.45)
    /// 200 potency HoT for 18s, 2 charges
    /// </summary>
    public static readonly ActionDefinition Aurora = new()
    {
        ActionId = 16151,
        Name = "Aurora",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f, // Per charge
        Range = 30f,
        MpCost = 0,
        HealPotency = 200, // Per tick
        AppliedStatusId = StatusIds.Aurora,
        AppliedStatusDuration = 18f
    };

    /// <summary>
    /// Heart of Light - Party magic mitigation (Lv.64)
    /// 10% magic damage reduction for party for 15s
    /// </summary>
    public static readonly ActionDefinition HeartOfLight = new()
    {
        ActionId = 16160,
        Name = "Heart of Light",
        MinLevel = 64,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.HeartOfLight,
        AppliedStatusDuration = 15f
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
    /// Enemies deal 10% less damage for 15s
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
        AppliedStatusDuration = 15f
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
        public const uint RoyalGuard = 1833;

        // Damage buffs
        public const uint NoMercy = 1831;

        // Continuation ready states
        public const uint ReadyToRip = 1842;
        public const uint ReadyToTear = 1843;
        public const uint ReadyToGouge = 1844;
        public const uint ReadyToBlast = 2686;
        public const uint ReadyToReign = 3840; // From Bloodfest at Lv.100

        // Combo/skill-applied
        public const uint BrutalShell = 1898;
        public const uint SonicBreak = 1837; // DoT debuff
        public const uint BowShock = 1838; // DoT debuff

        // Defensive buffs (self)
        public const uint Camouflage = 1832;
        public const uint Nebula = 1834;
        public const uint GreatNebula = 3838;
        public const uint Superbolide = 1836;
        public const uint HeartOfStone = 1840;
        public const uint HeartOfCorundum = 2683;
        public const uint Catharsis = 2685; // Heal when HP < 50%
        public const uint ClarityOfCorundum = 2684; // Extended duration

        // HoT/Heal
        public const uint Aurora = 1835;

        // Party buffs
        public const uint HeartOfLight = 1839;

        // Role action buffs
        public const uint Rampart = 1191;
        public const uint Reprisal = 1193;
        public const uint ArmsLength = 1209;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Danger Zone/Blasting Zone action for the player's level.
    /// </summary>
    public static ActionDefinition GetBlastingZoneAction(byte level)
    {
        if (level >= BlastingZone.MinLevel)
            return BlastingZone;
        return DangerZone;
    }

    /// <summary>
    /// Gets the best Nebula action for the player's level.
    /// </summary>
    public static ActionDefinition GetNebulaAction(byte level)
    {
        if (level >= GreatNebula.MinLevel)
            return GreatNebula;
        return Nebula;
    }

    /// <summary>
    /// Gets the best Heart defensive for the player's level.
    /// </summary>
    public static ActionDefinition GetHeartAction(byte level)
    {
        if (level >= HeartOfCorundum.MinLevel)
            return HeartOfCorundum;
        return HeartOfStone;
    }

    /// <summary>
    /// Returns true if the given action ID is part of the Gnashing Fang combo.
    /// </summary>
    public static bool IsGnashingFangCombo(uint actionId)
    {
        return actionId == GnashingFang.ActionId ||
               actionId == SavageClaw.ActionId ||
               actionId == WickedTalon.ActionId;
    }

    /// <summary>
    /// Returns true if the given action ID is a Continuation action.
    /// </summary>
    public static bool IsContinuationAction(uint actionId)
    {
        return actionId == JugularRip.ActionId ||
               actionId == AbdomenTear.ActionId ||
               actionId == EyeGouge.ActionId ||
               actionId == Hypervelocity.ActionId;
    }

    /// <summary>
    /// Maximum cartridge capacity.
    /// </summary>
    public const int MaxCartridges = 3;

    /// <summary>
    /// Cartridge cost for Gnashing Fang.
    /// </summary>
    public const int GnashingFangCost = 1;

    /// <summary>
    /// Cartridge cost for Burst Strike / Fated Circle.
    /// </summary>
    public const int BurstStrikeCost = 1;

    /// <summary>
    /// Cartridge cost for Double Down.
    /// </summary>
    public const int DoubleDownCost = 2;

    /// <summary>
    /// Cartridges granted by Bloodfest.
    /// </summary>
    public const int BloodfestCartridges = 3;

    #endregion
}
