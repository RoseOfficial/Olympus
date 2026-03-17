using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Dancer (DNC) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Terpsichore, the Greek muse of dance.
/// </summary>
public static class DNCActions
{
    #region Single-Target GCDs

    /// <summary>
    /// Cascade - Basic single target combo starter (Lv.1)
    /// Grants Silken Symmetry on hit.
    /// </summary>
    public static readonly ActionDefinition Cascade = new()
    {
        ActionId = 15989,
        Name = "Cascade",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 220
    };

    /// <summary>
    /// Fountain - Single target combo finisher (Lv.2)
    /// Requires combo from Cascade. Grants Silken Flow.
    /// </summary>
    public static readonly ActionDefinition Fountain = new()
    {
        ActionId = 15990,
        Name = "Fountain",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 340 // Combo potency
    };

    /// <summary>
    /// Reverse Cascade - Silken Symmetry proc consumer (Lv.20)
    /// Requires Silken Symmetry buff.
    /// </summary>
    public static readonly ActionDefinition ReverseCascade = new()
    {
        ActionId = 15991,
        Name = "Reverse Cascade",
        MinLevel = 20,
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
    /// Fountainfall - Silken Flow proc consumer (Lv.40)
    /// Requires Silken Flow buff.
    /// </summary>
    public static readonly ActionDefinition Fountainfall = new()
    {
        ActionId = 15992,
        Name = "Fountainfall",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 340
    };

    #endregion

    #region AoE GCDs

    /// <summary>
    /// Windmill - AoE combo starter (Lv.15)
    /// Grants Silken Symmetry on hit.
    /// </summary>
    public static readonly ActionDefinition Windmill = new()
    {
        ActionId = 15993,
        Name = "Windmill",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Bladeshower - AoE combo finisher (Lv.25)
    /// Requires combo from Windmill. Grants Silken Flow.
    /// </summary>
    public static readonly ActionDefinition Bladeshower = new()
    {
        ActionId = 15994,
        Name = "Bladeshower",
        MinLevel = 25,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140 // Combo potency
    };

    /// <summary>
    /// Rising Windmill - AoE Silken Symmetry proc consumer (Lv.35)
    /// Requires Silken Symmetry buff.
    /// </summary>
    public static readonly ActionDefinition RisingWindmill = new()
    {
        ActionId = 15995,
        Name = "Rising Windmill",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Bloodshower - AoE Silken Flow proc consumer (Lv.45)
    /// Requires Silken Flow buff.
    /// </summary>
    public static readonly ActionDefinition Bloodshower = new()
    {
        ActionId = 15996,
        Name = "Bloodshower",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 180
    };

    #endregion

    #region Dance Steps

    /// <summary>
    /// Standard Step - Initiates Standard Step dance (Lv.15)
    /// 30s recast, 30s duration buff on party.
    /// </summary>
    public static readonly ActionDefinition StandardStep = new()
    {
        ActionId = 15997,
        Name = "Standard Step",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 30f,
        MpCost = 0
    };

    /// <summary>
    /// Technical Step - Initiates Technical Step dance (Lv.70)
    /// 120s recast, party damage buff.
    /// </summary>
    public static readonly ActionDefinition TechnicalStep = new()
    {
        ActionId = 15998,
        Name = "Technical Step",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
    };

    /// <summary>
    /// Emboite (Red) - First dance step (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Emboite = new()
    {
        ActionId = 15999,
        Name = "Emboite",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Entrechat (Blue) - Second dance step (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Entrechat = new()
    {
        ActionId = 16000,
        Name = "Entrechat",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Jete (Green) - Third dance step (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Jete = new()
    {
        ActionId = 16001,
        Name = "Jete",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Pirouette (Yellow) - Fourth dance step (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Pirouette = new()
    {
        ActionId = 16002,
        Name = "Pirouette",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    #endregion

    #region Dance Finishers

    /// <summary>
    /// Standard Finish - Completes Standard Step (Lv.15)
    /// Damage based on successful steps. Applies party buff.
    /// </summary>
    public static readonly ActionDefinition StandardFinish = new()
    {
        ActionId = 16003,
        Name = "Standard Finish",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 15f,
        MpCost = 0,
        DamagePotency = 850 // At 2 steps
    };

    /// <summary>
    /// Technical Finish - Completes Technical Step (Lv.70)
    /// Damage based on successful steps. Applies party buff.
    /// Grants Flourishing Finish at Lv.82.
    /// </summary>
    public static readonly ActionDefinition TechnicalFinish = new()
    {
        ActionId = 16004,
        Name = "Technical Finish",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 15f,
        MpCost = 0,
        DamagePotency = 1300 // At 4 steps
    };

    /// <summary>
    /// Tillana - Follow-up to Technical Finish (Lv.82)
    /// Requires Flourishing Finish buff. Grants 50 Esprit.
    /// </summary>
    public static readonly ActionDefinition Tillana = new()
    {
        ActionId = 25790,
        Name = "Tillana",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 15f,
        MpCost = 0,
        DamagePotency = 600
    };

    #endregion

    #region Esprit Spenders

    /// <summary>
    /// Saber Dance - Esprit spender (Lv.76)
    /// Costs 50 Esprit. High potency damage.
    /// </summary>
    public static readonly ActionDefinition SaberDance = new()
    {
        ActionId = 16005,
        Name = "Saber Dance",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 520
    };

    /// <summary>
    /// Dance of the Dawn - Enhanced Esprit spender during burst (Lv.100)
    /// Requires Dance of the Dawn Ready buff. Replaces Saber Dance during burst.
    /// </summary>
    public static readonly ActionDefinition DanceOfTheDawn = new()
    {
        ActionId = 36985,
        Name = "Dance of the Dawn",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 900
    };

    #endregion

    #region Proc GCDs (Lv.90+)

    /// <summary>
    /// Starfall Dance - High potency proc during Devilment (Lv.90)
    /// Requires Flourishing Starfall buff (from Devilment).
    /// </summary>
    public static readonly ActionDefinition StarfallDance = new()
    {
        ActionId = 25792,
        Name = "Starfall Dance",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Last Dance - Follow-up proc (Lv.92)
    /// Requires Last Dance Ready buff.
    /// </summary>
    public static readonly ActionDefinition LastDance = new()
    {
        ActionId = 36983,
        Name = "Last Dance",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 520
    };

    /// <summary>
    /// Finishing Move - Enhanced Standard Finish follow-up (Lv.96)
    /// Requires Finishing Move Ready buff (from Standard Finish).
    /// </summary>
    public static readonly ActionDefinition FinishingMove = new()
    {
        ActionId = 36984,
        Name = "Finishing Move",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Radius = 15f,
        MpCost = 0,
        DamagePotency = 850
    };

    #endregion

    #region Fan Dance oGCDs

    /// <summary>
    /// Fan Dance - Single target feather spender (Lv.30)
    /// Costs 1 Feather. 50% chance to grant Threefold Fan Dance.
    /// </summary>
    public static readonly ActionDefinition FanDance = new()
    {
        ActionId = 16007,
        Name = "Fan Dance",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Fan Dance II - AoE feather spender (Lv.30)
    /// Costs 1 Feather. 50% chance to grant Threefold Fan Dance.
    /// </summary>
    public static readonly ActionDefinition FanDanceII = new()
    {
        ActionId = 16008,
        Name = "Fan Dance II",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Fan Dance III - Threefold proc consumer (Lv.66)
    /// Requires Threefold Fan Dance buff.
    /// </summary>
    public static readonly ActionDefinition FanDanceIII = new()
    {
        ActionId = 16009,
        Name = "Fan Dance III",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Fan Dance IV - Fourfold proc consumer (Lv.86)
    /// Requires Fourfold Fan Dance buff (from Flourish).
    /// </summary>
    public static readonly ActionDefinition FanDanceIV = new()
    {
        ActionId = 25791,
        Name = "Fan Dance IV",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 15f,
        MpCost = 0,
        DamagePotency = 300
    };

    #endregion

    #region Buff oGCDs

    /// <summary>
    /// Devilment - Burst buff (Lv.62)
    /// +20% crit/DH for 20s. Grants Flourishing Starfall at Lv.90.
    /// </summary>
    public static readonly ActionDefinition Devilment = new()
    {
        ActionId = 16011,
        Name = "Devilment",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Devilment,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Flourish - Grants all procs (Lv.72)
    /// Grants Symmetry, Flow, Threefold, Fourfold. 60s CD.
    /// </summary>
    public static readonly ActionDefinition Flourish = new()
    {
        ActionId = 16013,
        Name = "Flourish",
        MinLevel = 72,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    #endregion

    #region Partner Skills

    /// <summary>
    /// Closed Position - Sets dance partner (Lv.60)
    /// Partner receives Standard Finish buff and generates Esprit.
    /// </summary>
    public static readonly ActionDefinition ClosedPosition = new()
    {
        ActionId = 16006,
        Name = "Closed Position",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ClosedPosition,
        AppliedStatusDuration = float.MaxValue
    };

    /// <summary>
    /// Ending - Removes dance partner status (Lv.60)
    /// </summary>
    public static readonly ActionDefinition Ending = new()
    {
        ActionId = 18073,
        Name = "Ending",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    #endregion

    #region Utility oGCDs

    /// <summary>
    /// Shield Samba - Party mitigation (Lv.56)
    /// -15% damage taken for 15s, 90s cooldown.
    /// </summary>
    public static readonly ActionDefinition ShieldSamba = new()
    {
        ActionId = 16012,
        Name = "Shield Samba",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ShieldSamba,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Curing Waltz - Party heal (Lv.52)
    /// Heals self and nearby party members.
    /// </summary>
    public static readonly ActionDefinition CuringWaltz = new()
    {
        ActionId = 16015,
        Name = "Curing Waltz",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Radius = 5f,
        MpCost = 0,
        HealPotency = 300
    };

    /// <summary>
    /// Improvisation - Party heal/mitigation (Lv.80)
    /// Creates healing zone, grants Improvised Finish on end.
    /// </summary>
    public static readonly ActionDefinition Improvisation = new()
    {
        ActionId = 16014,
        Name = "Improvisation",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 8f,
        MpCost = 0
    };

    /// <summary>
    /// Improvised Finish - Completes Improvisation (Lv.80)
    /// Ends Improvisation and grants barrier.
    /// </summary>
    public static readonly ActionDefinition ImprovisedFinish = new()
    {
        ActionId = 25789,
        Name = "Improvised Finish",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Radius = 8f,
        MpCost = 0
    };

    /// <summary>
    /// En Avant - Dash forward (Lv.50)
    /// 3 charges, 30s recharge.
    /// </summary>
    public static readonly ActionDefinition EnAvant = new()
    {
        ActionId = 16010,
        Name = "En Avant",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        MpCost = 0
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Proc buffs (on self)
        public const uint SilkenSymmetry = 2693;
        public const uint SilkenFlow = 2694;
        public const uint FlourishingSymmetry = 3017;  // Enhanced version
        public const uint FlourishingFlow = 3018;      // Enhanced version
        public const uint ThreefoldFanDance = 1820;
        public const uint FourfoldFanDance = 2699;

        // Dance finisher procs
        public const uint FlourishingFinish = 2698;    // Tillana ready
        public const uint FlourishingStarfall = 2700;  // Starfall Dance ready

        // High-level procs (Lv.90+)
        public const uint LastDanceReady = 3867;
        public const uint FinishingMoveReady = 3868;
        public const uint DanceOfTheDawnReady = 3869;

        // Burst buffs
        public const uint Devilment = 1825;
        public const uint TechnicalFinish = 1822;      // Party damage buff
        public const uint StandardFinish = 1821;       // Party damage buff

        // Partner system
        public const uint ClosedPosition = 1823;       // On self
        public const uint DancePartner = 1824;         // On partner

        // Utility buffs
        public const uint ShieldSamba = 1826;
        public const uint Improvisation = 1827;

        // Role buffs
        public const uint ArmsLength = 1209;
        public const uint Peloton = 1199;
    }

    #endregion

    #region Dance Step Enum

    /// <summary>
    /// Dance step types from the game gauge.
    /// Values match the game's internal representation.
    /// </summary>
    public enum DanceStep : byte
    {
        None = 0,
        Emboite = 1,     // Red
        Entrechat = 2,   // Blue
        Jete = 3,        // Green
        Pirouette = 4    // Yellow
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the action for a dance step value.
    /// </summary>
    public static ActionDefinition? GetStepAction(byte step) => step switch
    {
        1 => Emboite,
        2 => Entrechat,
        3 => Jete,
        4 => Pirouette,
        _ => null
    };

    /// <summary>
    /// Gets the proc action for Silken Symmetry based on target count.
    /// </summary>
    public static ActionDefinition GetSymmetryAction(byte level, int enemyCount)
    {
        if (enemyCount >= 3 && level >= RisingWindmill.MinLevel)
            return RisingWindmill;
        return ReverseCascade;
    }

    /// <summary>
    /// Gets the proc action for Silken Flow based on target count.
    /// </summary>
    public static ActionDefinition GetFlowAction(byte level, int enemyCount)
    {
        if (enemyCount >= 3 && level >= Bloodshower.MinLevel)
            return Bloodshower;
        return Fountainfall;
    }

    /// <summary>
    /// Gets the feather spender based on target count.
    /// </summary>
    public static ActionDefinition GetFanDance(int enemyCount)
    {
        if (enemyCount >= 3)
            return FanDanceII;
        return FanDance;
    }

    /// <summary>
    /// Gets the combo starter based on target count.
    /// </summary>
    public static ActionDefinition GetComboStarter(byte level, int enemyCount)
    {
        if (enemyCount >= 3 && level >= Windmill.MinLevel)
            return Windmill;
        return Cascade;
    }

    /// <summary>
    /// Gets the combo finisher based on target count.
    /// </summary>
    public static ActionDefinition GetComboFinisher(byte level, int enemyCount)
    {
        if (enemyCount >= 3 && level >= Bladeshower.MinLevel)
            return Bladeshower;
        return Fountain;
    }

    #endregion
}
