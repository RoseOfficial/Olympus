using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Warrior (WAR) and Marauder (MRD) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Ares, the Greek god of war and battle fury.
/// </summary>
public static class WARActions
{
    #region Combo Actions (GCD)

    /// <summary>
    /// Heavy Swing - Basic combo starter (Lv.1)
    /// </summary>
    public static readonly ActionDefinition HeavySwing = new()
    {
        ActionId = 31,
        Name = "Heavy Swing",
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
    /// Maim - Combo from Heavy Swing (Lv.4)
    /// Grants +10 Beast Gauge
    /// </summary>
    public static readonly ActionDefinition Maim = new()
    {
        ActionId = 37,
        Name = "Maim",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 150 // 340 when combo
        // Combos from Heavy Swing, grants +10 Beast Gauge
    };

    /// <summary>
    /// Storm's Path - Combo finisher from Maim (Lv.26)
    /// Heals for a portion of damage dealt, grants +20 Beast Gauge
    /// </summary>
    public static readonly ActionDefinition StormsPath = new()
    {
        ActionId = 42,
        Name = "Storm's Path",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 160, // 480 when combo
        HealPotency = 250
        // Combos from Maim, grants +20 Beast Gauge
    };

    /// <summary>
    /// Storm's Eye - Combo finisher from Maim (Lv.50)
    /// Grants Surging Tempest buff (+10% damage), grants +10 Beast Gauge
    /// </summary>
    public static readonly ActionDefinition StormsEye = new()
    {
        ActionId = 45,
        Name = "Storm's Eye",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 160, // 480 when combo
        AppliedStatusId = StatusIds.SurgingTempest,
        AppliedStatusDuration = 30f
        // Combos from Maim, grants +10 Beast Gauge
    };

    #endregion

    #region AoE Combo Actions (GCD)

    /// <summary>
    /// Overpower - AoE combo starter (Lv.10)
    /// Cone attack in front
    /// </summary>
    public static readonly ActionDefinition Overpower = new()
    {
        ActionId = 41,
        Name = "Overpower",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Cone,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 110
    };

    /// <summary>
    /// Mythril Tempest - AoE combo from Overpower (Lv.40)
    /// Grants Surging Tempest buff, grants +20 Beast Gauge
    /// </summary>
    public static readonly ActionDefinition MythrilTempest = new()
    {
        ActionId = 16462,
        Name = "Mythril Tempest",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100, // 140 when combo
        AppliedStatusId = StatusIds.SurgingTempest,
        AppliedStatusDuration = 30f
        // Combos from Overpower, grants +20 Beast Gauge
    };

    #endregion

    #region Beast Gauge Spenders (GCD)

    /// <summary>
    /// Inner Beast - Pre-Fell Cleave spender (Lv.35)
    /// Costs 50 Beast Gauge, grants healing on hit
    /// </summary>
    public static readonly ActionDefinition InnerBeast = new()
    {
        ActionId = 49,
        Name = "Inner Beast",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 330,
        HealPotency = 100
        // Costs 50 Beast Gauge
    };

    /// <summary>
    /// Fell Cleave - Primary Beast Gauge spender (Lv.54)
    /// Costs 50 Beast Gauge (free during Inner Release)
    /// </summary>
    public static readonly ActionDefinition FellCleave = new()
    {
        ActionId = 3549,
        Name = "Fell Cleave",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 580
        // Costs 50 Beast Gauge (free during Inner Release)
    };

    /// <summary>
    /// Inner Chaos - Enhanced Fell Cleave when Nascent Chaos is active (Lv.80)
    /// Consumes Nascent Chaos buff, guaranteed crit/direct hit
    /// </summary>
    public static readonly ActionDefinition InnerChaos = new()
    {
        ActionId = 16465,
        Name = "Inner Chaos",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 660
        // Requires Nascent Chaos buff, guaranteed crit/direct hit
    };

    /// <summary>
    /// Steel Cyclone - AoE Beast Gauge spender (Lv.45)
    /// Costs 50 Beast Gauge, grants healing on hit
    /// </summary>
    public static readonly ActionDefinition SteelCyclone = new()
    {
        ActionId = 51,
        Name = "Steel Cyclone",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 160,
        HealPotency = 50
        // Costs 50 Beast Gauge
    };

    /// <summary>
    /// Decimate - Upgraded Steel Cyclone (Lv.60)
    /// Costs 50 Beast Gauge (free during Inner Release)
    /// </summary>
    public static readonly ActionDefinition Decimate = new()
    {
        ActionId = 3550,
        Name = "Decimate",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 180
        // Costs 50 Beast Gauge (free during Inner Release)
    };

    /// <summary>
    /// Chaotic Cyclone - Enhanced Decimate when Nascent Chaos is active (Lv.72)
    /// Consumes Nascent Chaos buff, guaranteed crit/direct hit
    /// </summary>
    public static readonly ActionDefinition ChaoticCyclone = new()
    {
        ActionId = 16463,
        Name = "Chaotic Cyclone",
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
        // Requires Nascent Chaos buff, guaranteed crit/direct hit
    };

    /// <summary>
    /// Primal Rend - Post-Inner Release burst (Lv.90)
    /// Available after Inner Release ends, gap closer with AoE
    /// </summary>
    public static readonly ActionDefinition PrimalRend = new()
    {
        ActionId = 25753,
        Name = "Primal Rend",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
        // Requires Primal Rend Ready buff
    };

    /// <summary>
    /// Primal Ruination - Follow-up to Primal Rend (Lv.100)
    /// Available after using Primal Rend
    /// </summary>
    public static readonly ActionDefinition PrimalRuination = new()
    {
        ActionId = 36925,
        Name = "Primal Ruination",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 780
        // Requires Primal Ruination Ready buff
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Upheaval - Single-target oGCD damage (Lv.64)
    /// </summary>
    public static readonly ActionDefinition Upheaval = new()
    {
        ActionId = 7387,
        Name = "Upheaval",
        MinLevel = 64,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
    };

    /// <summary>
    /// Orogeny - AoE oGCD damage (Lv.86)
    /// </summary>
    public static readonly ActionDefinition Orogeny = new()
    {
        ActionId = 25752,
        Name = "Orogeny",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Onslaught - Gap closer (Lv.62)
    /// 3 charges
    /// </summary>
    public static readonly ActionDefinition Onslaught = new()
    {
        ActionId = 7386,
        Name = "Onslaught",
        MinLevel = 62,
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
    /// Berserk - Pre-Inner Release damage buff (Lv.6)
    /// +10% damage for 15s
    /// </summary>
    public static readonly ActionDefinition Berserk = new()
    {
        ActionId = 38,
        Name = "Berserk",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Berserk,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Inner Release - Primary damage buff (Lv.70)
    /// Beast Gauge abilities cost 0 and are guaranteed crit/direct hit for 15s
    /// Grants 3 stacks
    /// </summary>
    public static readonly ActionDefinition InnerRelease = new()
    {
        ActionId = 7389,
        Name = "Inner Release",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.InnerRelease,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Infuriate - Beast Gauge generator (Lv.50)
    /// Grants +50 Beast Gauge and Nascent Chaos buff
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition Infuriate = new()
    {
        ActionId = 52,
        Name = "Infuriate",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // Per charge
        MpCost = 0,
        AppliedStatusId = StatusIds.NascentChaos,
        AppliedStatusDuration = 30f
        // Grants +50 Beast Gauge, 2 charges
    };

    /// <summary>
    /// Defiance - Tank stance (Lv.10)
    /// Toggle - increases enmity
    /// </summary>
    public static readonly ActionDefinition Defiance = new()
    {
        ActionId = 48,
        Name = "Defiance",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Defiance
    };

    #endregion

    #region Defensive Actions (oGCD)

    /// <summary>
    /// Holmgang - Invulnerability (Lv.42)
    /// Cannot drop below 1 HP for 10s (shorter CD than other tank invulns)
    /// </summary>
    public static readonly ActionDefinition Holmgang = new()
    {
        ActionId = 43,
        Name = "Holmgang",
        MinLevel = 42,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 240f, // 4 minutes
        Range = 6f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Holmgang,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Vengeance - Major defensive cooldown (Lv.38)
    /// 30% damage reduction + thorns for 15s
    /// </summary>
    public static readonly ActionDefinition Vengeance = new()
    {
        ActionId = 44,
        Name = "Vengeance",
        MinLevel = 38,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Vengeance,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Damnation - Enhanced Vengeance (Lv.92)
    /// 40% damage reduction + enhanced thorns for 15s
    /// </summary>
    public static readonly ActionDefinition Damnation = new()
    {
        ActionId = 36923,
        Name = "Damnation",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Damnation,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Raw Intuition - Short defensive cooldown (Lv.56)
    /// 10% damage reduction + heal on weaponskill hit for 6s
    /// </summary>
    public static readonly ActionDefinition RawIntuition = new()
    {
        ActionId = 3551,
        Name = "Raw Intuition",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 25f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RawIntuition,
        AppliedStatusDuration = 6f
    };

    /// <summary>
    /// Bloodwhetting - Enhanced Raw Intuition (Lv.82)
    /// 10% damage reduction + stronger heal on weaponskill hit for 8s
    /// Also grants Stem the Flow (10% DR) and Stem the Tide (shield)
    /// </summary>
    public static readonly ActionDefinition Bloodwhetting = new()
    {
        ActionId = 25751,
        Name = "Bloodwhetting",
        MinLevel = 82,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Shield | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 25f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Bloodwhetting,
        AppliedStatusDuration = 8f
    };

    /// <summary>
    /// Thrill of Battle - Max HP increase (Lv.30)
    /// +20% max HP for 10s, heals for the amount gained
    /// </summary>
    public static readonly ActionDefinition ThrillOfBattle = new()
    {
        ActionId = 40,
        Name = "Thrill of Battle",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ThrillOfBattle,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Equilibrium - Self-heal oGCD (Lv.58)
    /// Potent self-heal with HoT component
    /// </summary>
    public static readonly ActionDefinition Equilibrium = new()
    {
        ActionId = 3552,
        Name = "Equilibrium",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        HealPotency = 1200,
        AppliedStatusId = StatusIds.EquilibriumHoT,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Shake It Off - Party shield (Lv.68)
    /// Grants shield to party, cleanses own debuffs for increased potency
    /// </summary>
    public static readonly ActionDefinition ShakeItOff = new()
    {
        ActionId = 7388,
        Name = "Shake It Off",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Shield | ActionEffectType.Cleanse,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        Radius = 30f,
        AppliedStatusId = StatusIds.ShakeItOff,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Nascent Flash - Ally mitigation (Lv.76)
    /// Grants mitigation to target ally, heals them when you land weaponskills
    /// </summary>
    public static readonly ActionDefinition NascentFlash = new()
    {
        ActionId = 16464,
        Name = "Nascent Flash",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.NascentFlash,
        AppliedStatusDuration = 8f
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
        public const uint Defiance = 91;

        // Damage buffs
        public const uint SurgingTempest = 2677;
        public const uint Berserk = 86;
        public const uint InnerRelease = 1177;
        public const uint NascentChaos = 1897;
        public const uint PrimalRendReady = 2624;
        public const uint PrimalRuinationReady = 3834;
        public const uint InnerStrength = 2663; // Inner Release enhanced state

        // Defensive buffs
        public const uint Holmgang = 409;
        public const uint Vengeance = 89;
        public const uint Damnation = 3832;
        public const uint RawIntuition = 735;
        public const uint Bloodwhetting = 2678;
        public const uint StemTheFlow = 2679; // From Bloodwhetting
        public const uint StemTheTide = 2680; // From Bloodwhetting
        public const uint ThrillOfBattle = 87;
        public const uint EquilibriumHoT = 2681;
        public const uint ShakeItOff = 1457;
        public const uint NascentFlash = 1857;
        public const uint NascentGlint = 1858; // Self buff from Nascent Flash

        // Role action buffs
        public const uint Rampart = 1191;
        public const uint Reprisal = 1193;
        public const uint ArmsLength = 1209;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Beast Gauge spender for single target at the player's level.
    /// </summary>
    public static ActionDefinition GetFellCleaveAction(byte level)
    {
        if (level >= FellCleave.MinLevel)
            return FellCleave;
        return InnerBeast;
    }

    /// <summary>
    /// Gets the best Beast Gauge spender for AoE at the player's level.
    /// </summary>
    public static ActionDefinition GetDecimateAction(byte level)
    {
        if (level >= Decimate.MinLevel)
            return Decimate;
        return SteelCyclone;
    }

    /// <summary>
    /// Gets the best damage buff for the player's level.
    /// </summary>
    public static ActionDefinition GetDamageBuffAction(byte level)
    {
        if (level >= InnerRelease.MinLevel)
            return InnerRelease;
        return Berserk;
    }

    /// <summary>
    /// Gets the best major defensive cooldown for the player's level.
    /// </summary>
    public static ActionDefinition GetVengeanceAction(byte level)
    {
        if (level >= Damnation.MinLevel)
            return Damnation;
        return Vengeance;
    }

    /// <summary>
    /// Gets the best short defensive cooldown for the player's level.
    /// </summary>
    public static ActionDefinition GetBloodwhettingAction(byte level)
    {
        if (level >= Bloodwhetting.MinLevel)
            return Bloodwhetting;
        return RawIntuition;
    }

    /// <summary>
    /// Gets the best single-target combo finisher for the player's level and buff status.
    /// </summary>
    /// <param name="level">Player's current level.</param>
    /// <param name="needsSurgingTempest">True if Surging Tempest buff needs refreshing.</param>
    public static ActionDefinition GetComboFinisher(byte level, bool needsSurgingTempest)
    {
        // If we need the buff and have Storm's Eye, use it
        if (needsSurgingTempest && level >= StormsEye.MinLevel)
            return StormsEye;

        // Otherwise use Storm's Path for healing
        if (level >= StormsPath.MinLevel)
            return StormsPath;

        // Fallback to Maim at very low levels
        return Maim;
    }

    #endregion
}
