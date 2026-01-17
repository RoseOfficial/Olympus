using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Monk (MNK) and Pugilist (PGL) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Kratos, the Greek god of strength and power.
/// </summary>
public static class MNKActions
{
    #region Opo-opo Form GCDs

    /// <summary>
    /// Bootshine - Opo-opo form GCD (Lv.1)
    /// Rear positional: Guaranteed critical hit
    /// Grants Raptor's Fury when in Opo-opo form
    /// </summary>
    public static readonly ActionDefinition Bootshine = new()
    {
        ActionId = 53,
        Name = "Bootshine",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 210
        // Rear positional: Guaranteed crit
    };

    /// <summary>
    /// Dragon Kick - Opo-opo form GCD (Lv.50)
    /// Flank positional: Grants Leaden Fist buff
    /// Grants Raptor's Fury when in Opo-opo form
    /// </summary>
    public static readonly ActionDefinition DragonKick = new()
    {
        ActionId = 74,
        Name = "Dragon Kick",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 320,
        AppliedStatusId = StatusIds.LeadenFist,
        AppliedStatusDuration = 30f
        // Flank positional: Grants Leaden Fist
    };

    /// <summary>
    /// Leaping Opo - Enhanced Bootshine (Lv.96)
    /// Upgraded version with higher potency
    /// </summary>
    public static readonly ActionDefinition LeapingOpo = new()
    {
        ActionId = 36945,
        Name = "Leaping Opo",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 260
        // No positional requirement
    };

    #endregion

    #region Raptor Form GCDs

    /// <summary>
    /// True Strike - Raptor form GCD (Lv.4)
    /// Rear positional: Increased potency
    /// </summary>
    public static readonly ActionDefinition TrueStrike = new()
    {
        ActionId = 54,
        Name = "True Strike",
        MinLevel = 4,
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
    /// Twin Snakes - Raptor form GCD (Lv.18)
    /// Flank positional: Increased potency
    /// Grants Disciplined Fist buff (+15% damage)
    /// </summary>
    public static readonly ActionDefinition TwinSnakes = new()
    {
        ActionId = 61,
        Name = "Twin Snakes",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 280,
        AppliedStatusId = StatusIds.DisciplinedFist,
        AppliedStatusDuration = 15f
        // Flank positional: Increased potency
    };

    /// <summary>
    /// Rising Raptor - Enhanced True Strike (Lv.96)
    /// Upgraded version with higher potency
    /// </summary>
    public static readonly ActionDefinition RisingRaptor = new()
    {
        ActionId = 36946,
        Name = "Rising Raptor",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 340
        // No positional requirement
    };

    #endregion

    #region Coeurl Form GCDs

    /// <summary>
    /// Snap Punch - Coeurl form GCD (Lv.6)
    /// Flank positional: Increased potency
    /// </summary>
    public static readonly ActionDefinition SnapPunch = new()
    {
        ActionId = 56,
        Name = "Snap Punch",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 310
        // Flank positional: Increased potency
    };

    /// <summary>
    /// Demolish - Coeurl form GCD (Lv.30)
    /// Rear positional: Increased potency
    /// Applies Demolish DoT
    /// </summary>
    public static readonly ActionDefinition Demolish = new()
    {
        ActionId = 66,
        Name = "Demolish",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 130,
        AppliedStatusId = StatusIds.Demolish,
        AppliedStatusDuration = 18f
        // Rear positional: Increased potency
        // DoT: 70 potency every 3s for 18s
    };

    /// <summary>
    /// Pouncing Coeurl - Enhanced Snap Punch (Lv.96)
    /// Upgraded version with higher potency
    /// </summary>
    public static readonly ActionDefinition PouncingCoeurl = new()
    {
        ActionId = 36947,
        Name = "Pouncing Coeurl",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 370
        // No positional requirement
    };

    #endregion

    #region AoE Form GCDs

    /// <summary>
    /// Arm of the Destroyer - Opo-opo form AoE (Lv.26)
    /// Circle AoE around self
    /// </summary>
    public static readonly ActionDefinition ArmOfTheDestroyer = new()
    {
        ActionId = 62,
        Name = "Arm of the Destroyer",
        MinLevel = 26,
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
    /// Shadow of the Destroyer - Enhanced Arm of the Destroyer (Lv.82)
    /// </summary>
    public static readonly ActionDefinition ShadowOfTheDestroyer = new()
    {
        ActionId = 25767,
        Name = "Shadow of the Destroyer",
        MinLevel = 82,
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
    /// Four-point Fury - Raptor form AoE (Lv.45)
    /// Grants Disciplined Fist
    /// </summary>
    public static readonly ActionDefinition FourPointFury = new()
    {
        ActionId = 16473,
        Name = "Four-point Fury",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120,
        AppliedStatusId = StatusIds.DisciplinedFist,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Rockbreaker - Coeurl form AoE (Lv.30)
    /// </summary>
    public static readonly ActionDefinition Rockbreaker = new()
    {
        ActionId = 70,
        Name = "Rockbreaker",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 130
    };

    #endregion

    #region Masterful Blitz Actions (GCD)

    /// <summary>
    /// Elixir Field - Blitz when all 3 Beast Chakra are same type (Lv.60)
    /// </summary>
    public static readonly ActionDefinition ElixirField = new()
    {
        ActionId = 3545,
        Name = "Elixir Field",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
        // Requires 3 identical Beast Chakra
    };

    /// <summary>
    /// Flint Strike - Blitz when 2 same + 1 different Beast Chakra (Lv.60)
    /// </summary>
    public static readonly ActionDefinition FlintStrike = new()
    {
        ActionId = 25882,
        Name = "Flint Strike",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
        // Requires 2 same + 1 different Beast Chakra
    };

    /// <summary>
    /// Celestial Revolution - Blitz alternative at lower levels (Lv.60)
    /// </summary>
    public static readonly ActionDefinition CelestialRevolution = new()
    {
        ActionId = 25765,
        Name = "Celestial Revolution",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 450
    };

    /// <summary>
    /// Rising Phoenix - Enhanced Flint Strike (Lv.86)
    /// </summary>
    public static readonly ActionDefinition RisingPhoenix = new()
    {
        ActionId = 25768,
        Name = "Rising Phoenix",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Phantom Rush - Ultimate Blitz with both Nadi (Lv.90)
    /// </summary>
    public static readonly ActionDefinition PhantomRush = new()
    {
        ActionId = 25769,
        Name = "Phantom Rush",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1150
        // Requires both Lunar and Solar Nadi
    };

    /// <summary>
    /// Elixir Burst - Enhanced Elixir Field (Lv.92)
    /// </summary>
    public static readonly ActionDefinition ElixirBurst = new()
    {
        ActionId = 36948,
        Name = "Elixir Burst",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Wind's Reply - Follow-up after Phantom Rush (Lv.100)
    /// </summary>
    public static readonly ActionDefinition WindsReply = new()
    {
        ActionId = 36949,
        Name = "Wind's Reply",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f, // Ranged GCD
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Fire's Reply - Damage oGCD after Riddle of Fire ends (Lv.100)
    /// </summary>
    public static readonly ActionDefinition FiresReply = new()
    {
        ActionId = 36950,
        Name = "Fire's Reply",
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
    };

    #endregion

    #region Chakra Spenders (oGCD)

    /// <summary>
    /// The Forbidden Chakra - Single target Chakra spender (Lv.54)
    /// Requires 5 Chakra
    /// </summary>
    public static readonly ActionDefinition TheForbiddenChakra = new()
    {
        ActionId = 3547,
        Name = "The Forbidden Chakra",
        MinLevel = 54,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 340
        // Requires 5 Chakra
    };

    /// <summary>
    /// Enlightenment - AoE Chakra spender (Lv.74)
    /// Requires 5 Chakra
    /// </summary>
    public static readonly ActionDefinition Enlightenment = new()
    {
        ActionId = 16474,
        Name = "Enlightenment",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 10f,
        Radius = 10f, // Line AoE
        MpCost = 0,
        DamagePotency = 170
        // Requires 5 Chakra
    };

    /// <summary>
    /// Howling Fist - Pre-Enlightenment AoE (Lv.40)
    /// Requires 5 Chakra
    /// </summary>
    public static readonly ActionDefinition HowlingFist = new()
    {
        ActionId = 25763,
        Name = "Howling Fist",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 10f,
        Radius = 10f, // Line AoE
        MpCost = 0,
        DamagePotency = 100
        // Requires 5 Chakra
    };

    /// <summary>
    /// Steel Peak - Pre-Forbidden Chakra single target (Lv.15)
    /// </summary>
    public static readonly ActionDefinition SteelPeak = new()
    {
        ActionId = 25761,
        Name = "Steel Peak",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 180
        // Requires 5 Chakra
    };

    #endregion

    #region Buff Actions (oGCD)

    /// <summary>
    /// Riddle of Fire - Primary damage buff (Lv.68)
    /// +15% damage for 20s
    /// </summary>
    public static readonly ActionDefinition RiddleOfFire = new()
    {
        ActionId = 7395,
        Name = "Riddle of Fire",
        MinLevel = 68,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RiddleOfFire,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Brotherhood - Party damage buff (Lv.70)
    /// +5% damage to party, generates Chakra
    /// </summary>
    public static readonly ActionDefinition Brotherhood = new()
    {
        ActionId = 7396,
        Name = "Brotherhood",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Brotherhood,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Perfect Balance - Form-free GCD usage (Lv.50)
    /// 3 stacks, allows any form GCD
    /// </summary>
    public static readonly ActionDefinition PerfectBalance = new()
    {
        ActionId = 69,
        Name = "Perfect Balance",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 40f, // 2 charges at 40s each
        MpCost = 0,
        AppliedStatusId = StatusIds.PerfectBalance,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Riddle of Wind - Speed buff and AoE damage (Lv.72)
    /// </summary>
    public static readonly ActionDefinition RiddleOfWind = new()
    {
        ActionId = 25766,
        Name = "Riddle of Wind",
        MinLevel = 72,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 400,
        AppliedStatusId = StatusIds.RiddleOfWind,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Riddle of Earth - Defensive cooldown (Lv.64)
    /// </summary>
    public static readonly ActionDefinition RiddleOfEarth = new()
    {
        ActionId = 7394,
        Name = "Riddle of Earth",
        MinLevel = 64,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RiddleOfEarth,
        AppliedStatusDuration = 10f
    };

    #endregion

    #region Utility Actions (oGCD)

    /// <summary>
    /// Thunderclap - Gap closer (Lv.35)
    /// 3 charges
    /// </summary>
    public static readonly ActionDefinition Thunderclap = new()
    {
        ActionId = 25762,
        Name = "Thunderclap",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 30f, // Per charge
        Range = 20f,
        MpCost = 0
    };

    /// <summary>
    /// Mantra - Party healing increase (Lv.42)
    /// </summary>
    public static readonly ActionDefinition Mantra = new()
    {
        ActionId = 65,
        Name = "Mantra",
        MinLevel = 42,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Mantra,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Meditation - Chakra generation out of combat (Lv.54)
    /// </summary>
    public static readonly ActionDefinition Meditation = new()
    {
        ActionId = 3546,
        Name = "Meditation",
        MinLevel = 54,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Anatman - Extends Disciplined Fist and form timer (Lv.78)
    /// </summary>
    public static readonly ActionDefinition Anatman = new()
    {
        ActionId = 16475,
        Name = "Anatman",
        MinLevel = 78,
        Category = ActionCategory.GCD, // Actually a channeled GCD
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Anatman,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Six-sided Star - Slow but powerful GCD (Lv.80)
    /// Increases GCD recast time
    /// </summary>
    public static readonly ActionDefinition SixSidedStar = new()
    {
        ActionId = 16476,
        Name = "Six-sided Star",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 5f, // Longer GCD
        Range = 3f,
        MpCost = 0,
        DamagePotency = 710
    };

    /// <summary>
    /// Form Shift - Changes form outside combat (Lv.52)
    /// </summary>
    public static readonly ActionDefinition FormShift = new()
    {
        ActionId = 4262,
        Name = "Form Shift",
        MinLevel = 52,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0,
        AppliedStatusId = StatusIds.FormlessFist,
        AppliedStatusDuration = 30f
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

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Form statuses
        public const uint OpoOpoForm = 107;
        public const uint RaptorForm = 108;
        public const uint CoeurlForm = 109;
        public const uint FormlessFist = 2513;

        // Damage buffs
        public const uint LeadenFist = 1861;
        public const uint DisciplinedFist = 3001;
        public const uint RiddleOfFire = 1181;
        public const uint Brotherhood = 1182;
        public const uint BrotherhoodMeditative = 2173; // Chakra generation buff
        public const uint PerfectBalance = 110;
        public const uint RiddleOfWind = 2687;

        // Proc buffs
        public const uint RaptorsFury = 3848; // From Opo-opo form
        public const uint CoeurlsFury = 3849; // From Raptor form
        public const uint OpooposFury = 3850; // From Coeurl form

        // Blitz-related
        public const uint FiresRumination = 3847; // After Riddle of Fire
        public const uint WindsRumination = 3846; // After Riddle of Wind

        // DoT
        public const uint Demolish = 246;

        // Defensive/Utility
        public const uint RiddleOfEarth = 1179;
        public const uint EarthsRumination = 3841; // After Riddle of Earth
        public const uint Mantra = 102;
        public const uint Anatman = 1862;

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;
    }

    #endregion

    #region Beast Chakra Types

    /// <summary>
    /// Beast Chakra types for Masterful Blitz.
    /// </summary>
    public enum BeastChakraType : byte
    {
        None = 0,
        OpoOpo = 1,
        Raptor = 2,
        Coeurl = 3
    }

    /// <summary>
    /// Nadi types for Phantom Rush tracking.
    /// </summary>
    [System.Flags]
    public enum NadiFlags : byte
    {
        None = 0,
        Lunar = 1,
        Solar = 2
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Chakra spender for single target at the player's level.
    /// </summary>
    public static ActionDefinition GetChakraSpender(byte level)
    {
        if (level >= TheForbiddenChakra.MinLevel)
            return TheForbiddenChakra;
        return SteelPeak;
    }

    /// <summary>
    /// Gets the best AoE Chakra spender at the player's level.
    /// </summary>
    public static ActionDefinition GetAoeChakraSpender(byte level)
    {
        if (level >= Enlightenment.MinLevel)
            return Enlightenment;
        if (level >= HowlingFist.MinLevel)
            return HowlingFist;
        return SteelPeak;
    }

    /// <summary>
    /// Gets the Opo-opo form AoE action for the player's level.
    /// </summary>
    public static ActionDefinition GetOpoOpoAoe(byte level)
    {
        if (level >= ShadowOfTheDestroyer.MinLevel)
            return ShadowOfTheDestroyer;
        return ArmOfTheDestroyer;
    }

    /// <summary>
    /// Determines the Blitz action based on Beast Chakra configuration.
    /// </summary>
    /// <param name="level">Player level.</param>
    /// <param name="hasLunarNadi">Whether Lunar Nadi is active.</param>
    /// <param name="hasSolarNadi">Whether Solar Nadi is active.</param>
    /// <param name="chakra1">First Beast Chakra type.</param>
    /// <param name="chakra2">Second Beast Chakra type.</param>
    /// <param name="chakra3">Third Beast Chakra type.</param>
    public static ActionDefinition? GetBlitzAction(
        byte level,
        bool hasLunarNadi,
        bool hasSolarNadi,
        BeastChakraType chakra1,
        BeastChakraType chakra2,
        BeastChakraType chakra3)
    {
        // Need all 3 chakra for a Blitz
        if (chakra1 == BeastChakraType.None || chakra2 == BeastChakraType.None || chakra3 == BeastChakraType.None)
            return null;

        // Phantom Rush requires both Nadi
        if (hasLunarNadi && hasSolarNadi && level >= PhantomRush.MinLevel)
            return PhantomRush;

        // Check if all 3 chakra are the same (Elixir Field/Burst path)
        if (chakra1 == chakra2 && chakra2 == chakra3)
        {
            if (level >= ElixirBurst.MinLevel)
                return ElixirBurst;
            if (level >= ElixirField.MinLevel)
                return ElixirField;
        }

        // Check if all 3 chakra are different (Flint Strike/Rising Phoenix path)
        if (chakra1 != chakra2 && chakra2 != chakra3 && chakra1 != chakra3)
        {
            if (level >= RisingPhoenix.MinLevel)
                return RisingPhoenix;
            if (level >= FlintStrike.MinLevel)
                return FlintStrike;
        }

        // Mixed chakra (2 same, 1 different) - Celestial Revolution
        if (level >= CelestialRevolution.MinLevel)
            return CelestialRevolution;

        return null;
    }

    #endregion
}
