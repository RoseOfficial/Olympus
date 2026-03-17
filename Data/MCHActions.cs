using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Machinist (MCH) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Prometheus, the Greek titan of fire and technology.
/// </summary>
public static class MCHActions
{
    #region Single-Target Combo GCDs

    /// <summary>
    /// Heated Split Shot - Combo starter (Lv.54 upgrade of Split Shot)
    /// Base combo action, +5 Heat
    /// </summary>
    public static readonly ActionDefinition HeatedSplitShot = new()
    {
        ActionId = 7411,
        Name = "Heated Split Shot",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Split Shot - Combo starter (Lv.1)
    /// Base combo action
    /// Replaced by Heated Split Shot at Lv.54
    /// </summary>
    public static readonly ActionDefinition SplitShot = new()
    {
        ActionId = 2866,
        Name = "Split Shot",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Heated Slug Shot - Second combo action (Lv.60 upgrade of Slug Shot)
    /// Combo from Heated Split Shot, +5 Heat
    /// </summary>
    public static readonly ActionDefinition HeatedSlugShot = new()
    {
        ActionId = 7412,
        Name = "Heated Slug Shot",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 280
    };

    /// <summary>
    /// Slug Shot - Second combo action (Lv.2)
    /// Combo from Split Shot
    /// Replaced by Heated Slug Shot at Lv.60
    /// </summary>
    public static readonly ActionDefinition SlugShot = new()
    {
        ActionId = 2868,
        Name = "Slug Shot",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 180
    };

    /// <summary>
    /// Heated Clean Shot - Third combo action (Lv.64 upgrade of Clean Shot)
    /// Combo finisher, +5 Heat, +10 Battery
    /// </summary>
    public static readonly ActionDefinition HeatedCleanShot = new()
    {
        ActionId = 7413,
        Name = "Heated Clean Shot",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 360
    };

    /// <summary>
    /// Clean Shot - Third combo action (Lv.26)
    /// Combo finisher
    /// Replaced by Heated Clean Shot at Lv.64
    /// </summary>
    public static readonly ActionDefinition CleanShot = new()
    {
        ActionId = 2873,
        Name = "Clean Shot",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 240
    };

    #endregion

    #region Tool Actions (GCD)

    /// <summary>
    /// Drill - High damage single target (Lv.58)
    /// 20s cooldown, 2 charges at Lv.98
    /// </summary>
    public static readonly ActionDefinition Drill = new()
    {
        ActionId = 16498,
        Name = "Drill",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 580
    };

    /// <summary>
    /// Air Anchor - High damage single target (Lv.76)
    /// 40s cooldown, +20 Battery
    /// Upgraded from Hot Shot
    /// </summary>
    public static readonly ActionDefinition AirAnchor = new()
    {
        ActionId = 16500,
        Name = "Air Anchor",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 580
    };

    /// <summary>
    /// Hot Shot - High damage single target (Lv.4)
    /// 40s cooldown
    /// Replaced by Air Anchor at Lv.76
    /// </summary>
    public static readonly ActionDefinition HotShot = new()
    {
        ActionId = 2872,
        Name = "Hot Shot",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 240
    };

    /// <summary>
    /// Chain Saw - High damage single target (Lv.90)
    /// 60s cooldown, +20 Battery
    /// </summary>
    public static readonly ActionDefinition ChainSaw = new()
    {
        ActionId = 25788,
        Name = "Chain Saw",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 580
    };

    /// <summary>
    /// Excavator - High damage single target (Lv.96)
    /// Replaces Chain Saw in rotation, follows Excavator Ready proc
    /// +20 Battery
    /// </summary>
    public static readonly ActionDefinition Excavator = new()
    {
        ActionId = 36981,
        Name = "Excavator",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 580
    };

    /// <summary>
    /// Full Metal Field - Powerful finisher (Lv.100)
    /// Follows Full Metal Machinist proc from Barrel Stabilizer
    /// </summary>
    public static readonly ActionDefinition FullMetalField = new()
    {
        ActionId = 36982,
        Name = "Full Metal Field",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
    };

    #endregion

    #region Hypercharge Actions (GCD)

    /// <summary>
    /// Heat Blast - Overheated single target (Lv.35)
    /// Only usable during Overheated, 1.5s GCD
    /// Reduces Gauss Round and Ricochet CD by 15s
    /// </summary>
    public static readonly ActionDefinition HeatBlast = new()
    {
        ActionId = 7410,
        Name = "Heat Blast",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Blazing Shot - Enhanced Heat Blast (Lv.68)
    /// Higher potency Heat Blast during Overheated
    /// </summary>
    public static readonly ActionDefinition BlazingShot = new()
    {
        ActionId = 36978,
        Name = "Blazing Shot",
        MinLevel = 68,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 220
    };

    /// <summary>
    /// Auto Crossbow - Overheated AoE (Lv.52)
    /// Only usable during Overheated, 1.5s GCD
    /// </summary>
    public static readonly ActionDefinition AutoCrossbow = new()
    {
        ActionId = 16497,
        Name = "Auto Crossbow",
        MinLevel = 52,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 12f,
        Radius = 12f,
        MpCost = 0,
        DamagePotency = 140
    };

    #endregion

    #region AoE GCDs

    /// <summary>
    /// Spread Shot - AoE combo starter (Lv.18)
    /// Cone AoE, +5 Heat
    /// </summary>
    public static readonly ActionDefinition SpreadShot = new()
    {
        ActionId = 2870,
        Name = "Spread Shot",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 12f,
        Radius = 12f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Scattergun - Enhanced AoE (Lv.82)
    /// Upgraded Spread Shot, +10 Heat
    /// </summary>
    public static readonly ActionDefinition Scattergun = new()
    {
        ActionId = 25786,
        Name = "Scattergun",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 12f,
        Radius = 12f,
        MpCost = 0,
        DamagePotency = 160
    };

    /// <summary>
    /// Bioblaster - DoT AoE (Lv.72)
    /// Line AoE, applies DoT
    /// Shares recast with Drill
    /// </summary>
    public static readonly ActionDefinition Bioblaster = new()
    {
        ActionId = 16499,
        Name = "Bioblaster",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 12f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 50,
        AppliedStatusId = StatusIds.Bioblaster,
        AppliedStatusDuration = 15f
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Gauss Round - Charge-based oGCD (Lv.15)
    /// 3 charges, 30s recast per charge
    /// CD reduced by 15s per Heat Blast
    /// </summary>
    public static readonly ActionDefinition GaussRound = new()
    {
        ActionId = 2874,
        Name = "Gauss Round",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Ricochet - Charge-based AoE oGCD (Lv.50)
    /// 3 charges, 30s recast per charge
    /// CD reduced by 15s per Heat Blast
    /// </summary>
    public static readonly ActionDefinition Ricochet = new()
    {
        ActionId = 2890,
        Name = "Ricochet",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Double Check - Enhanced Gauss Round (Lv.92)
    /// Higher potency, replaces Gauss Round usage
    /// </summary>
    public static readonly ActionDefinition DoubleCheck = new()
    {
        ActionId = 36979,
        Name = "Double Check",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 160
    };

    /// <summary>
    /// Checkmate - Enhanced Ricochet (Lv.92)
    /// Higher potency, replaces Ricochet usage
    /// </summary>
    public static readonly ActionDefinition Checkmate = new()
    {
        ActionId = 36980,
        Name = "Checkmate",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 160
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Reassemble - Guaranteed crit/DH (Lv.10)
    /// 2 charges at Lv.84, 55s recast per charge
    /// </summary>
    public static readonly ActionDefinition Reassemble = new()
    {
        ActionId = 2876,
        Name = "Reassemble",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 55f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Reassembled,
        AppliedStatusDuration = 5f
    };

    /// <summary>
    /// Barrel Stabilizer - Heat generator (Lv.66)
    /// +50 Heat, grants Full Metal Machinist at Lv.100
    /// 120s cooldown
    /// </summary>
    public static readonly ActionDefinition BarrelStabilizer = new()
    {
        ActionId = 7414,
        Name = "Barrel Stabilizer",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Hypercharged,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Wildfire - Detonation damage (Lv.45)
    /// Detonates after 10s dealing damage based on weaponskills used
    /// 120s cooldown
    /// </summary>
    public static readonly ActionDefinition Wildfire = new()
    {
        ActionId = 2878,
        Name = "Wildfire",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 25f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Wildfire,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Hypercharge - Overheated state (Lv.30)
    /// Costs 50 Heat, enables Heat Blast for 10s
    /// </summary>
    public static readonly ActionDefinition Hypercharge = new()
    {
        ActionId = 17209,
        Name = "Hypercharge",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 10f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Overheated,
        AppliedStatusDuration = 10f
    };

    #endregion

    #region Automaton Queen

    /// <summary>
    /// Automaton Queen - Summon pet (Lv.80)
    /// Costs 50-100 Battery, damage scales with Battery spent
    /// </summary>
    public static readonly ActionDefinition AutomatonQueen = new()
    {
        ActionId = 16501,
        Name = "Automaton Queen",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 6f,
        MpCost = 0
    };

    /// <summary>
    /// Rook Autoturret - Summon turret (Lv.40)
    /// Replaced by Automaton Queen at Lv.80
    /// Costs 50-100 Battery
    /// </summary>
    public static readonly ActionDefinition RookAutoturret = new()
    {
        ActionId = 2864,
        Name = "Rook Autoturret",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 6f,
        MpCost = 0
    };

    /// <summary>
    /// Queen Overdrive - Force Queen finisher (Lv.80)
    /// Commands Automaton Queen to use Pile Bunker immediately
    /// </summary>
    public static readonly ActionDefinition QueenOverdrive = new()
    {
        ActionId = 16502,
        Name = "Queen Overdrive",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0
    };

    /// <summary>
    /// Rook Overdrive - Force turret finisher (Lv.40)
    /// Replaced by Queen Overdrive at Lv.80
    /// </summary>
    public static readonly ActionDefinition RookOverdrive = new()
    {
        ActionId = 7415,
        Name = "Rook Overdrive",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0
    };

    #endregion

    #region Role Actions (oGCD)

    /// <summary>
    /// Tactician - Party damage mitigation (Lv.56)
    /// </summary>
    public static readonly ActionDefinition Tactician = new()
    {
        ActionId = 16889,
        Name = "Tactician",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Tactician,
        AppliedStatusDuration = 15f
    };

    #endregion

    #region Utility Actions

    /// <summary>
    /// Dismantle - Enemy damage reduction (Lv.62)
    /// </summary>
    public static readonly ActionDefinition Dismantle = new()
    {
        ActionId = 2887,
        Name = "Dismantle",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 25f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Dismantled,
        AppliedStatusDuration = 10f
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Buff statuses
        public const uint Reassembled = 851;     // Guaranteed crit/DH on next weaponskill
        public const uint Overheated = 2688;     // Can use Heat Blast/Auto Crossbow
        public const uint Hypercharged = 3864;   // From Barrel Stabilizer (not to be confused with Overheated)
        public const uint FullMetalMachinist = 3866; // Can use Full Metal Field

        // Queen-related
        public const uint AutomatonQueenActive = 2685; // Queen is summoned

        // Target debuffs
        public const uint Wildfire = 861;        // Will detonate after duration
        public const uint Bioblaster = 1866;     // DoT from Bioblaster

        // Role buffs
        public const uint ArmsLength = 1209;
        public const uint Peloton = 1199;
        public const uint Tactician = 1951;
        public const uint Dismantled = 860;

        // Proc statuses
        public const uint ExcavatorReady = 3865;  // Can use Excavator
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best combo starter for the player's level.
    /// </summary>
    public static ActionDefinition GetComboStarter(byte level)
    {
        if (level >= HeatedSplitShot.MinLevel)
            return HeatedSplitShot;
        return SplitShot;
    }

    /// <summary>
    /// Gets the best second combo action for the player's level.
    /// </summary>
    public static ActionDefinition GetComboSecond(byte level)
    {
        if (level >= HeatedSlugShot.MinLevel)
            return HeatedSlugShot;
        return SlugShot;
    }

    /// <summary>
    /// Gets the best third combo action for the player's level.
    /// </summary>
    public static ActionDefinition GetComboFinisher(byte level)
    {
        if (level >= HeatedCleanShot.MinLevel)
            return HeatedCleanShot;
        return CleanShot;
    }

    /// <summary>
    /// Gets the best AoE action for the player's level.
    /// </summary>
    public static ActionDefinition GetAoeAction(byte level)
    {
        if (level >= Scattergun.MinLevel)
            return Scattergun;
        return SpreadShot;
    }

    /// <summary>
    /// Gets the best overheated GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetOverheatedGcd(byte level, bool aoe)
    {
        if (aoe && level >= AutoCrossbow.MinLevel)
            return AutoCrossbow;

        if (level >= BlazingShot.MinLevel)
            return BlazingShot;

        return HeatBlast;
    }

    /// <summary>
    /// Gets the best pet summon action for the player's level.
    /// </summary>
    public static ActionDefinition GetPetSummon(byte level)
    {
        if (level >= AutomatonQueen.MinLevel)
            return AutomatonQueen;
        return RookAutoturret;
    }

    /// <summary>
    /// Gets the best pet overdrive action for the player's level.
    /// </summary>
    public static ActionDefinition GetPetOverdrive(byte level)
    {
        if (level >= QueenOverdrive.MinLevel)
            return QueenOverdrive;
        return RookOverdrive;
    }

    /// <summary>
    /// Gets the best Gauss Round replacement for the player's level.
    /// </summary>
    public static ActionDefinition GetGaussRound(byte level)
    {
        if (level >= DoubleCheck.MinLevel)
            return DoubleCheck;
        return GaussRound;
    }

    /// <summary>
    /// Gets the best Ricochet replacement for the player's level.
    /// </summary>
    public static ActionDefinition GetRicochet(byte level)
    {
        if (level >= Checkmate.MinLevel)
            return Checkmate;
        return Ricochet;
    }

    /// <summary>
    /// Gets the Air Anchor or Hot Shot for the player's level.
    /// </summary>
    public static ActionDefinition GetAirAnchor(byte level)
    {
        if (level >= AirAnchor.MinLevel)
            return AirAnchor;
        return HotShot;
    }

    #endregion
}
