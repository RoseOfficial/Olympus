using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Dragoon (DRG) and Lancer (LNC) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Zeus, the Greek god of sky and thunder.
/// </summary>
public static class DRGActions
{
    #region Single-Target Combo GCDs

    /// <summary>
    /// True Thrust - Combo starter (Lv.1)
    /// Base combo action for both combo paths
    /// </summary>
    public static readonly ActionDefinition TrueThrust = new()
    {
        ActionId = 75,
        Name = "True Thrust",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 230
    };

    /// <summary>
    /// Vorpal Thrust - Second combo action (Lv.4)
    /// Leads to Heavens' Thrust / Full Thrust
    /// </summary>
    public static readonly ActionDefinition VorpalThrust = new()
    {
        ActionId = 78,
        Name = "Vorpal Thrust",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 280
        // Combo from True Thrust
    };

    /// <summary>
    /// Full Thrust - Third combo action (Lv.26)
    /// High damage finisher
    /// Replaced by Heavens' Thrust at Lv.86
    /// </summary>
    public static readonly ActionDefinition FullThrust = new()
    {
        ActionId = 84,
        Name = "Full Thrust",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // Combo from Vorpal Thrust
    };

    /// <summary>
    /// Heavens' Thrust - Enhanced Full Thrust (Lv.86)
    /// </summary>
    public static readonly ActionDefinition HeavensThrust = new()
    {
        ActionId = 25771,
        Name = "Heavens' Thrust",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440
        // Combo from Vorpal Thrust
    };

    /// <summary>
    /// Disembowel - Second combo action (Lv.18)
    /// Grants Power Surge (+15% damage)
    /// Leads to Chaotic Spring / Chaos Thrust
    /// </summary>
    public static readonly ActionDefinition Disembowel = new()
    {
        ActionId = 87,
        Name = "Disembowel",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250,
        AppliedStatusId = StatusIds.PowerSurge,
        AppliedStatusDuration = 30f
        // Combo from True Thrust
    };

    /// <summary>
    /// Chaos Thrust - Third combo action (Lv.50)
    /// Rear positional: Increased potency
    /// Applies DoT
    /// Replaced by Chaotic Spring at Lv.86
    /// </summary>
    public static readonly ActionDefinition ChaosThrust = new()
    {
        ActionId = 88,
        Name = "Chaos Thrust",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 260,
        AppliedStatusId = StatusIds.ChaosThrust,
        AppliedStatusDuration = 24f
        // Rear positional: Increased potency
        // DoT: 45 potency every 3s for 24s
    };

    /// <summary>
    /// Chaotic Spring - Enhanced Chaos Thrust (Lv.86)
    /// Rear positional: Increased potency
    /// Applies DoT
    /// </summary>
    public static readonly ActionDefinition ChaoticSpring = new()
    {
        ActionId = 25772,
        Name = "Chaotic Spring",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300,
        AppliedStatusId = StatusIds.ChaoticSpring,
        AppliedStatusDuration = 24f
        // Rear positional: Increased potency
        // DoT: 45 potency every 3s for 24s
    };

    #endregion

    #region Positional Finisher Procs

    /// <summary>
    /// Fang and Claw - Positional proc (Lv.56)
    /// Flank positional: Increased potency
    /// Combo from Heavens' Thrust / Full Thrust or Chaotic Spring / Chaos Thrust
    /// </summary>
    public static readonly ActionDefinition FangAndClaw = new()
    {
        ActionId = 3554,
        Name = "Fang and Claw",
        MinLevel = 56,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
        // Flank positional: Increased potency
    };

    /// <summary>
    /// Wheeling Thrust - Positional proc (Lv.58)
    /// Rear positional: Increased potency
    /// Combo from Fang and Claw
    /// </summary>
    public static readonly ActionDefinition WheelingThrust = new()
    {
        ActionId = 3556,
        Name = "Wheeling Thrust",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
        // Rear positional: Increased potency
    };

    /// <summary>
    /// Drakesbane - Enhanced positional finisher (Lv.92)
    /// Replaces both Fang and Claw and Wheeling Thrust
    /// No positional requirement
    /// </summary>
    public static readonly ActionDefinition Drakesbane = new()
    {
        ActionId = 36952,
        Name = "Drakesbane",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // No positional requirement
    };

    #endregion

    #region AoE Combo GCDs

    /// <summary>
    /// Doom Spike - AoE combo starter (Lv.40)
    /// Line AoE
    /// </summary>
    public static readonly ActionDefinition DoomSpike = new()
    {
        ActionId = 86,
        Name = "Doom Spike",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 10f,
        MpCost = 0,
        DamagePotency = 110
        // Line AoE
    };

    /// <summary>
    /// Sonic Thrust - AoE combo second (Lv.62)
    /// Grants Power Surge
    /// </summary>
    public static readonly ActionDefinition SonicThrust = new()
    {
        ActionId = 7397,
        Name = "Sonic Thrust",
        MinLevel = 62,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 10f,
        MpCost = 0,
        DamagePotency = 120,
        AppliedStatusId = StatusIds.PowerSurge,
        AppliedStatusDuration = 30f
        // Combo from Doom Spike
    };

    /// <summary>
    /// Coerthan Torment - AoE combo finisher (Lv.72)
    /// </summary>
    public static readonly ActionDefinition CoerthanTorment = new()
    {
        ActionId = 16477,
        Name = "Coerthan Torment",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 10f,
        MpCost = 0,
        DamagePotency = 150
        // Combo from Sonic Thrust
    };

    #endregion

    #region Jump Actions (oGCD)

    /// <summary>
    /// Jump - Basic jump attack (Lv.30)
    /// Replaced by High Jump at Lv.74
    /// </summary>
    public static readonly ActionDefinition Jump = new()
    {
        ActionId = 92,
        Name = "Jump",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 320
    };

    /// <summary>
    /// High Jump - Enhanced Jump (Lv.74)
    /// Grants Dive Ready for Mirage Dive
    /// </summary>
    public static readonly ActionDefinition HighJump = new()
    {
        ActionId = 16478,
        Name = "High Jump",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 400,
        AppliedStatusId = StatusIds.DiveReady,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Mirage Dive - Follow-up dive (Lv.68)
    /// Requires Dive Ready status
    /// Grants Dragon Eye
    /// </summary>
    public static readonly ActionDefinition MirageDive = new()
    {
        ActionId = 7399,
        Name = "Mirage Dive",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 200
        // Requires Dive Ready
        // Grants Dragon Eye
    };

    /// <summary>
    /// Spineshatter Dive - Gap closer (Lv.45)
    /// 2 charges at Lv.84
    /// </summary>
    public static readonly ActionDefinition SpineshatterDive = new()
    {
        ActionId = 95,
        Name = "Spineshatter Dive",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 60f, // 2 charges at Lv.84
        Range = 20f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Dragonfire Dive - AoE damage dive (Lv.50)
    /// Circle AoE on target
    /// </summary>
    public static readonly ActionDefinition DragonfireDive = new()
    {
        ActionId = 96,
        Name = "Dragonfire Dive",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 300
    };

    #endregion

    #region Life of the Dragon Actions (oGCD)

    /// <summary>
    /// Geirskogul - Life activation (Lv.60)
    /// Enters Life of the Dragon at 2 Dragon Eyes
    /// </summary>
    public static readonly ActionDefinition Geirskogul = new()
    {
        ActionId = 3555,
        Name = "Geirskogul",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 15f,
        MpCost = 0,
        DamagePotency = 280
        // Line AoE
        // At 2 Dragon Eyes: Enters Life of the Dragon
    };

    /// <summary>
    /// Nastrond - Life spender (Lv.70)
    /// Can use 3 times during Life of the Dragon
    /// </summary>
    public static readonly ActionDefinition Nastrond = new()
    {
        ActionId = 7400,
        Name = "Nastrond",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2f,
        Range = 15f,
        MpCost = 0,
        DamagePotency = 360
        // Line AoE
        // Requires Life of the Dragon
    };

    /// <summary>
    /// Stardiver - Life finisher (Lv.80)
    /// High-damage dive during Life of the Dragon
    /// </summary>
    public static readonly ActionDefinition Stardiver = new()
    {
        ActionId = 16480,
        Name = "Stardiver",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 620
        // Requires Life of the Dragon
    };

    /// <summary>
    /// Wyrmwind Thrust - Firstmind Focus spender (Lv.90)
    /// Requires 2 Firstmind's Focus stacks
    /// </summary>
    public static readonly ActionDefinition WyrmwindThrust = new()
    {
        ActionId = 25773,
        Name = "Wyrmwind Thrust",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 10f,
        Range = 15f,
        MpCost = 0,
        DamagePotency = 440
        // Line AoE
        // Requires 2 Firstmind's Focus
    };

    /// <summary>
    /// Rise of the Dragon - Follow-up after Dragonfire Dive (Lv.92)
    /// </summary>
    public static readonly ActionDefinition RiseOfTheDragon = new()
    {
        ActionId = 36953,
        Name = "Rise of the Dragon",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 550
        // Requires Draconian Fire
    };

    /// <summary>
    /// Starcross - Follow-up after Stardiver (Lv.100)
    /// </summary>
    public static readonly ActionDefinition Starcross = new()
    {
        ActionId = 36956,
        Name = "Starcross",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
        // Requires Starcross Ready
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Lance Charge - Personal damage buff (Lv.30)
    /// +10% damage for 20s
    /// </summary>
    public static readonly ActionDefinition LanceCharge = new()
    {
        ActionId = 85,
        Name = "Lance Charge",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.LanceCharge,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Battle Litany - Party critical hit buff (Lv.52)
    /// +10% crit rate for party for 20s
    /// </summary>
    public static readonly ActionDefinition BattleLitany = new()
    {
        ActionId = 3557,
        Name = "Battle Litany",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.BattleLitany,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Life Surge - Next GCD guaranteed crit (Lv.6)
    /// 2 charges at Lv.88
    /// </summary>
    public static readonly ActionDefinition LifeSurge = new()
    {
        ActionId = 83,
        Name = "Life Surge",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 40f, // 2 charges at Lv.88
        MpCost = 0,
        AppliedStatusId = StatusIds.LifeSurge,
        AppliedStatusDuration = 5f
    };

    /// <summary>
    /// Dragon Sight - Tether damage buff (Lv.66)
    /// +10% damage to self and target
    /// </summary>
    public static readonly ActionDefinition DragonSight = new()
    {
        ActionId = 7398,
        Name = "Dragon Sight",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 12f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RightEye,
        AppliedStatusDuration = 20f
    };

    #endregion

    #region Role Actions (oGCD)

    /// <summary>
    /// Second Wind - Self heal (Lv.8)
    /// </summary>
    public static readonly ActionDefinition SecondWind = new()
    {
        ActionId = 7541,
        Name = "Second Wind",
        MinLevel = 8,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        HealPotency = 800
    };

    /// <summary>
    /// Bloodbath - Lifesteal buff (Lv.12)
    /// </summary>
    public static readonly ActionDefinition Bloodbath = new()
    {
        ActionId = 7542,
        Name = "Bloodbath",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Bloodbath,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Feint - Enemy damage reduction (Lv.22)
    /// </summary>
    public static readonly ActionDefinition Feint = new()
    {
        ActionId = 7549,
        Name = "Feint",
        MinLevel = 22,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 10f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Feint,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Arm's Length - Knockback immunity (Lv.32)
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
    /// True North - Ignore positional requirements (Lv.50)
    /// </summary>
    public static readonly ActionDefinition TrueNorth = new()
    {
        ActionId = 7546,
        Name = "True North",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 45f, // 2 charges
        MpCost = 0,
        AppliedStatusId = StatusIds.TrueNorth,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Leg Sweep - Stun (Lv.10)
    /// </summary>
    public static readonly ActionDefinition LegSweep = new()
    {
        ActionId = 7863,
        Name = "Leg Sweep",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 3f,
        MpCost = 0
    };

    #endregion

    #region Utility Actions

    /// <summary>
    /// Piercing Talon - Ranged attack (Lv.15)
    /// </summary>
    public static readonly ActionDefinition PiercingTalon = new()
    {
        ActionId = 90,
        Name = "Piercing Talon",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 150
        // Breaks combo
    };

    /// <summary>
    /// Elusive Jump - Backward movement (Lv.35)
    /// </summary>
    public static readonly ActionDefinition ElusiveJump = new()
    {
        ActionId = 94,
        Name = "Elusive Jump",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 30f,
        MpCost = 0
    };

    /// <summary>
    /// Winged Glide - Forward movement (Lv.45)
    /// </summary>
    public static readonly ActionDefinition WingedGlide = new()
    {
        ActionId = 36951,
        Name = "Winged Glide",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 60f, // 2 charges
        MpCost = 0
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Damage buffs
        public const uint PowerSurge = 2720;      // From Disembowel/Sonic Thrust (+15% damage)
        public const uint LanceCharge = 1864;     // +10% damage
        public const uint LifeSurge = 116;        // Next GCD guaranteed crit
        public const uint BattleLitany = 786;     // Party crit buff
        public const uint RightEye = 1183;        // Dragon Sight self buff
        public const uint LeftEye = 1454;         // Dragon Sight tether buff

        // Combo/Proc statuses
        public const uint FangAndClawBared = 802;   // Flank proc ready
        public const uint WheelInMotion = 803;      // Rear proc ready
        public const uint DraconianFire = 1863;     // Enhanced Coerthan / Rise of the Dragon ready

        // Life of the Dragon
        public const uint DiveReady = 1243;         // Can use Mirage Dive
        public const uint LifeOfTheDragon = 2546;   // Life state active
        public const uint NastrondReady = 3844;     // Can use Nastrond
        public const uint StardiverReady = 3845;    // Can use Stardiver
        public const uint StarcrossReady = 3846;    // Can use Starcross

        // DoT effects
        public const uint ChaosThrust = 118;        // DoT on target (old)
        public const uint ChaoticSpring = 2719;     // DoT on target (new)

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best third combo action for the True Thrust > Vorpal line.
    /// </summary>
    public static ActionDefinition GetVorpalFinisher(byte level)
    {
        if (level >= HeavensThrust.MinLevel)
            return HeavensThrust;
        if (level >= FullThrust.MinLevel)
            return FullThrust;
        return VorpalThrust;
    }

    /// <summary>
    /// Gets the best third combo action for the True Thrust > Disembowel line.
    /// </summary>
    public static ActionDefinition GetDisembowelFinisher(byte level)
    {
        if (level >= ChaoticSpring.MinLevel)
            return ChaoticSpring;
        if (level >= ChaosThrust.MinLevel)
            return ChaosThrust;
        return Disembowel;
    }

    /// <summary>
    /// Gets the positional finisher action based on level.
    /// </summary>
    public static ActionDefinition GetPositionalFinisher(byte level)
    {
        if (level >= Drakesbane.MinLevel)
            return Drakesbane;
        if (level >= WheelingThrust.MinLevel)
            return WheelingThrust;
        return FangAndClaw;
    }

    /// <summary>
    /// Gets the best jump action for the player's level.
    /// </summary>
    public static ActionDefinition GetJumpAction(byte level)
    {
        if (level >= HighJump.MinLevel)
            return HighJump;
        return Jump;
    }

    /// <summary>
    /// Gets the AoE combo finisher for the player's level.
    /// </summary>
    public static ActionDefinition GetAoeFinisher(byte level)
    {
        if (level >= CoerthanTorment.MinLevel)
            return CoerthanTorment;
        if (level >= SonicThrust.MinLevel)
            return SonicThrust;
        return DoomSpike;
    }

    /// <summary>
    /// Gets the DoT status ID based on level.
    /// </summary>
    public static uint GetDotStatusId(byte level)
    {
        if (level >= ChaoticSpring.MinLevel)
            return StatusIds.ChaoticSpring;
        return StatusIds.ChaosThrust;
    }

    #endregion
}
