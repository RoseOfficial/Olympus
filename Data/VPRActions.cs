using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Viper (VPR) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Echidna, the Greek mother of serpents.
/// </summary>
public static class VPRActions
{
    #region Dual Wield Combo GCDs

    /// <summary>
    /// Steel Fangs - Dual wield combo starter (Lv.1)
    /// First hit of the standard combo.
    /// Grants Honed Reavers (enables enhanced Reaving Fangs).
    /// </summary>
    public static readonly ActionDefinition SteelFangs = new()
    {
        ActionId = 34606,
        Name = "Steel Fangs",
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
    /// Reaving Fangs - Alternate combo starter (Lv.1)
    /// First hit, alternative path.
    /// Grants Honed Steel (enables enhanced Steel Fangs).
    /// </summary>
    public static readonly ActionDefinition ReavingFangs = new()
    {
        ActionId = 34607,
        Name = "Reaving Fangs",
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
    /// Hunter's Sting - Second combo from Steel Fangs (Lv.5)
    /// Combo from Steel Fangs. Grants Hunter's Instinct.
    /// </summary>
    public static readonly ActionDefinition HuntersSting = new()
    {
        ActionId = 34608,
        Name = "Hunter's Sting",
        MinLevel = 5,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Swiftskin's Sting - Second combo from Reaving Fangs (Lv.5)
    /// Combo from Reaving Fangs. Grants Swiftscaled.
    /// </summary>
    public static readonly ActionDefinition SwiftskinsString = new()
    {
        ActionId = 34609,
        Name = "Swiftskin's Sting",
        MinLevel = 5,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Flanksting Strike - Finisher from Hunter's Sting (Lv.10)
    /// Flank positional. Combo from Hunter's Sting.
    /// Grants +10 Serpent Offerings. Applies Hindstung Venom.
    /// </summary>
    public static readonly ActionDefinition FlankstingStrike = new()
    {
        ActionId = 34610,
        Name = "Flanksting Strike",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // Flank positional bonus
    };

    /// <summary>
    /// Flanksbane Fang - Finisher from Swiftskin's Sting (Lv.10)
    /// Flank positional. Combo from Swiftskin's Sting.
    /// Grants +10 Serpent Offerings. Applies Hindsbane Venom.
    /// </summary>
    public static readonly ActionDefinition FlanksbaneFang = new()
    {
        ActionId = 34611,
        Name = "Flanksbane Fang",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // Flank positional bonus
    };

    /// <summary>
    /// Hindsting Strike - Finisher from Hunter's Sting (Lv.10)
    /// Rear positional. Combo from Hunter's Sting.
    /// Grants +10 Serpent Offerings. Applies Flankstung Venom.
    /// </summary>
    public static readonly ActionDefinition HindstingStrike = new()
    {
        ActionId = 34612,
        Name = "Hindsting Strike",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // Rear positional bonus
    };

    /// <summary>
    /// Hindsbane Fang - Finisher from Swiftskin's Sting (Lv.10)
    /// Rear positional. Combo from Swiftskin's Sting.
    /// Grants +10 Serpent Offerings. Applies Flanksbane Venom.
    /// </summary>
    public static readonly ActionDefinition HindsbaneFang = new()
    {
        ActionId = 34613,
        Name = "Hindsbane Fang",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
        // Rear positional bonus
    };

    #endregion

    #region AoE Dual Wield Combo

    /// <summary>
    /// Steel Maw - AoE combo starter (Lv.12)
    /// Grants Honed Reavers.
    /// </summary>
    public static readonly ActionDefinition SteelMaw = new()
    {
        ActionId = 34614,
        Name = "Steel Maw",
        MinLevel = 12,
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
    /// Reaving Maw - AoE alternate combo starter (Lv.12)
    /// Grants Honed Steel.
    /// </summary>
    public static readonly ActionDefinition ReavingMaw = new()
    {
        ActionId = 34615,
        Name = "Reaving Maw",
        MinLevel = 12,
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
    /// Hunter's Bite - AoE second combo (Lv.20)
    /// Combo from Steel Maw. Grants Hunter's Instinct.
    /// </summary>
    public static readonly ActionDefinition HuntersBite = new()
    {
        ActionId = 34616,
        Name = "Hunter's Bite",
        MinLevel = 20,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120
    };

    /// <summary>
    /// Swiftskin's Bite - AoE second combo (Lv.20)
    /// Combo from Reaving Maw. Grants Swiftscaled.
    /// </summary>
    public static readonly ActionDefinition SwiftskinsBite = new()
    {
        ActionId = 34617,
        Name = "Swiftskin's Bite",
        MinLevel = 20,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 120
    };

    /// <summary>
    /// Jagged Maw - AoE finisher (Lv.20)
    /// Combo from Hunter's Bite.
    /// Grants +10 Serpent Offerings. Applies Grimskin's Venom.
    /// </summary>
    public static readonly ActionDefinition JaggedMaw = new()
    {
        ActionId = 34618,
        Name = "Jagged Maw",
        MinLevel = 20,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Bloodied Maw - AoE finisher (Lv.20)
    /// Combo from Swiftskin's Bite.
    /// Grants +10 Serpent Offerings. Applies Grimhunter's Venom.
    /// </summary>
    public static readonly ActionDefinition BloodiedMaw = new()
    {
        ActionId = 34619,
        Name = "Bloodied Maw",
        MinLevel = 20,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140
    };

    #endregion

    #region Twinblade Combo GCDs (3s GCD)

    /// <summary>
    /// Vicewinder - Twinblade combo starter (Lv.15)
    /// Applies Noxious Gnash debuff. Grants +1 Rattling Coil.
    /// 2 charges.
    /// </summary>
    public static readonly ActionDefinition Vicewinder = new()
    {
        ActionId = 34620,
        Name = "Vicewinder",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 40f, // Charge-based
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Applies Noxious Gnash for 20s
        // Grants 1 Rattling Coil
    };

    /// <summary>
    /// Hunter's Coil - Twinblade from Vicewinder (Lv.30)
    /// Combo from Vicewinder. Grants Swiftscaled and Hunter's Instinct.
    /// Grants +5 Serpent Offerings.
    /// </summary>
    public static readonly ActionDefinition HuntersCoil = new()
    {
        ActionId = 34621,
        Name = "Hunter's Coil",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 3f, // Twinblade GCD
        Range = 3f,
        MpCost = 0,
        DamagePotency = 550
        // Grants Poised for Twinfang
    };

    /// <summary>
    /// Swiftskin's Coil - Twinblade from Vicewinder (Lv.30)
    /// Combo from Vicewinder. Grants Swiftscaled and Hunter's Instinct.
    /// Grants +5 Serpent Offerings.
    /// </summary>
    public static readonly ActionDefinition SwiftskinsCoil = new()
    {
        ActionId = 34622,
        Name = "Swiftskin's Coil",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 3f, // Twinblade GCD
        Range = 3f,
        MpCost = 0,
        DamagePotency = 550
        // Grants Poised for Twinblood
    };

    /// <summary>
    /// Vicepit - AoE twinblade starter (Lv.35)
    /// Applies Noxious Gnash debuff. Grants +1 Rattling Coil.
    /// 2 charges.
    /// </summary>
    public static readonly ActionDefinition Vicepit = new()
    {
        ActionId = 34623,
        Name = "Vicepit",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 200
    };

    /// <summary>
    /// Hunter's Den - AoE twinblade combo (Lv.35)
    /// Combo from Vicepit. Grants Hunter's Instinct and Swiftscaled.
    /// </summary>
    public static readonly ActionDefinition HuntersDen = new()
    {
        ActionId = 34624,
        Name = "Hunter's Den",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 3f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 220
    };

    /// <summary>
    /// Swiftskin's Den - AoE twinblade combo (Lv.35)
    /// Combo from Vicepit. Grants Hunter's Instinct and Swiftscaled.
    /// </summary>
    public static readonly ActionDefinition SwiftskinsDen = new()
    {
        ActionId = 34625,
        Name = "Swiftskin's Den",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 3f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 220
    };

    #endregion

    #region Twinblade Follow-up oGCDs

    /// <summary>
    /// Twinfang - oGCD after Hunter's Coil (Lv.25)
    /// Use FIRST after Hunter's Coil.
    /// </summary>
    public static readonly ActionDefinition Twinfang = new()
    {
        ActionId = 34626,
        Name = "Twinfang",
        MinLevel = 25,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Twinblood - oGCD after Swiftskin's Coil (Lv.25)
    /// Use FIRST after Swiftskin's Coil.
    /// </summary>
    public static readonly ActionDefinition Twinblood = new()
    {
        ActionId = 34627,
        Name = "Twinblood",
        MinLevel = 25,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Twinfang Bite - AoE oGCD after Hunter's Den (Lv.40)
    /// </summary>
    public static readonly ActionDefinition TwinfangBite = new()
    {
        ActionId = 34628,
        Name = "Twinfang Bite",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 80
    };

    /// <summary>
    /// Twinblood Bite - AoE oGCD after Swiftskin's Den (Lv.40)
    /// </summary>
    public static readonly ActionDefinition TwinbloodBite = new()
    {
        ActionId = 34629,
        Name = "Twinblood Bite",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 80
    };

    /// <summary>
    /// Twinfang Thresh - AoE oGCD alternate (Lv.40)
    /// </summary>
    public static readonly ActionDefinition TwinfangThresh = new()
    {
        ActionId = 34630,
        Name = "Twinfang Thresh",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 80
    };

    /// <summary>
    /// Twinblood Thresh - AoE oGCD alternate (Lv.40)
    /// </summary>
    public static readonly ActionDefinition TwinbloodThresh = new()
    {
        ActionId = 34631,
        Name = "Twinblood Thresh",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 80
    };

    #endregion

    #region Uncoiled Fury (Rattling Coil Consumer)

    /// <summary>
    /// Uncoiled Fury - Ranged GCD that consumes Rattling Coil (Lv.45)
    /// Consumes 1 Rattling Coil. Ranged attack.
    /// </summary>
    public static readonly ActionDefinition UncoiledFury = new()
    {
        ActionId = 34633,
        Name = "Uncoiled Fury",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 3.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 680
        // Requires 1 Rattling Coil
        // Grants Poised for Twinfang
    };

    /// <summary>
    /// Uncoiled Twinfang - oGCD after Uncoiled Fury (Lv.45)
    /// Use FIRST after Uncoiled Fury.
    /// </summary>
    public static readonly ActionDefinition UncoiledTwinfang = new()
    {
        ActionId = 34634,
        Name = "Uncoiled Twinfang",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Uncoiled Twinblood - oGCD after Uncoiled Twinfang (Lv.45)
    /// Use SECOND after Uncoiled Fury.
    /// </summary>
    public static readonly ActionDefinition UncoiledTwinblood = new()
    {
        ActionId = 34635,
        Name = "Uncoiled Twinblood",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 100
    };

    #endregion

    #region Writhing Snap (Ranged)

    /// <summary>
    /// Writhing Snap - Basic ranged GCD (Lv.50)
    /// Filler for when out of melee range.
    /// </summary>
    public static readonly ActionDefinition WrithingSnap = new()
    {
        ActionId = 34632,
        Name = "Writhing Snap",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 20f,
        MpCost = 0,
        DamagePotency = 200
    };

    #endregion

    #region Reawaken Burst Sequence

    /// <summary>
    /// Reawaken - Enter burst mode (Lv.70)
    /// Consumes 50 Serpent Offerings. Grants 5 Anguine Tribute stacks.
    /// </summary>
    public static readonly ActionDefinition Reawaken = new()
    {
        ActionId = 34636,
        Name = "Reawaken",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
        // Costs 50 Serpent Offerings
        // Grants 5 Anguine Tribute
    };

    /// <summary>
    /// First Generation - Reawaken GCD 1 (Lv.70)
    /// Consumes 1 Anguine Tribute.
    /// </summary>
    public static readonly ActionDefinition FirstGeneration = new()
    {
        ActionId = 34637,
        Name = "First Generation",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Grants First Legacy Ready
    };

    /// <summary>
    /// Second Generation - Reawaken GCD 2 (Lv.70)
    /// Consumes 1 Anguine Tribute.
    /// </summary>
    public static readonly ActionDefinition SecondGeneration = new()
    {
        ActionId = 34638,
        Name = "Second Generation",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Grants Second Legacy Ready
    };

    /// <summary>
    /// Third Generation - Reawaken GCD 3 (Lv.70)
    /// Consumes 1 Anguine Tribute.
    /// </summary>
    public static readonly ActionDefinition ThirdGeneration = new()
    {
        ActionId = 34639,
        Name = "Third Generation",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Grants Third Legacy Ready
    };

    /// <summary>
    /// Fourth Generation - Reawaken GCD 4 (Lv.70)
    /// Consumes 1 Anguine Tribute.
    /// </summary>
    public static readonly ActionDefinition FourthGeneration = new()
    {
        ActionId = 34640,
        Name = "Fourth Generation",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Grants Fourth Legacy Ready
    };

    /// <summary>
    /// First Legacy - oGCD after First Generation (Lv.70)
    /// </summary>
    public static readonly ActionDefinition FirstLegacy = new()
    {
        ActionId = 34641,
        Name = "First Legacy",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Second Legacy - oGCD after Second Generation (Lv.70)
    /// </summary>
    public static readonly ActionDefinition SecondLegacy = new()
    {
        ActionId = 34642,
        Name = "Second Legacy",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Third Legacy - oGCD after Third Generation (Lv.70)
    /// </summary>
    public static readonly ActionDefinition ThirdLegacy = new()
    {
        ActionId = 34643,
        Name = "Third Legacy",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Fourth Legacy - oGCD after Fourth Generation (Lv.70)
    /// </summary>
    public static readonly ActionDefinition FourthLegacy = new()
    {
        ActionId = 34644,
        Name = "Fourth Legacy",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 250
    };

    /// <summary>
    /// Ouroboros - Reawaken finisher (Lv.80)
    /// High potency finisher. Consumes Reawakened state.
    /// </summary>
    public static readonly ActionDefinition Ouroboros = new()
    {
        ActionId = 34645,
        Name = "Ouroboros",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 1050
    };

    #endregion

    #region Serpent's Ire (Party Buff)

    /// <summary>
    /// Serpent's Ire - Party damage buff (Lv.86)
    /// +% damage to party. Grants Ready to Reawaken.
    /// Also grants +1 Rattling Coil.
    /// </summary>
    public static readonly ActionDefinition SerpentsIre = new()
    {
        ActionId = 34647,
        Name = "Serpent's Ire",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f, // 2-minute cooldown
        Radius = 30f,
        MpCost = 0
        // Grants Ready to Reawaken
        // Grants 1 Rattling Coil
    };

    #endregion

    #region Dawntrail Actions

    /// <summary>
    /// Death Rattle - Finisher oGCD (Lv.92)
    /// Uses after positional finishers.
    /// </summary>
    public static readonly ActionDefinition DeathRattle = new()
    {
        ActionId = 34648,
        Name = "Death Rattle",
        MinLevel = 92,
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
    /// Last Lash - AoE finisher oGCD (Lv.92)
    /// </summary>
    public static readonly ActionDefinition LastLash = new()
    {
        ActionId = 34649,
        Name = "Last Lash",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Serpent's Tail - Combo finisher marker (Lv.92)
    /// Transforms into Death Rattle or Last Lash.
    /// </summary>
    public static readonly ActionDefinition SerpentsTail = new()
    {
        ActionId = 35920,
        Name = "Serpent's Tail",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 0 // Placeholder - transforms
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Target debuff
        public const uint NoxiousGnash = 3667;

        // Self buffs
        public const uint HuntersInstinct = 3668;
        public const uint Swiftscaled = 3669;
        public const uint Reawakened = 3670;

        // Combo enhancement buffs
        public const uint HonedSteel = 3672;
        public const uint HonedReavers = 3773;

        // Venom buffs (dictate next positional)
        public const uint FlankstungVenom = 3645;
        public const uint HindstungVenom = 3646;
        public const uint FlanksbaneVenom = 3647;
        public const uint HindsbaneVenom = 3648;

        // AoE venom buffs
        public const uint GrimskinsVenom = 3649;
        public const uint GrimhuntersVenom = 3650;

        // oGCD procs
        public const uint PoisedForTwinfang = 3665;
        public const uint PoisedForTwinblood = 3666;

        // Serpent's Ire
        public const uint ReadyToReawaken = 4094;

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;
    }

    #endregion

    #region Combo Enums

    /// <summary>
    /// Viper combo state tracking from the job gauge.
    /// </summary>
    public enum DreadCombo : byte
    {
        None = 0,
        DreadwindyReady = 1,
        PitReady = 2,
        HunterCoilReady = 3,
        SwiftskinCoilReady = 4,
        HunterDenReady = 5,
        SwiftskinDenReady = 6,
    }

    /// <summary>
    /// Serpent combo state for oGCD follow-ups.
    /// </summary>
    public enum SerpentCombo : byte
    {
        None = 0,
        DeathRattle = 1,
        LastLash = 2,
        FirstLegacy = 3,
        SecondLegacy = 4,
        ThirdLegacy = 5,
        FourthLegacy = 6,
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the appropriate positional finisher based on venom buff and combo path.
    /// </summary>
    public static ActionDefinition GetPositionalFinisher(bool hasFlankstungVenom, bool hasHindstungVenom,
        bool hasFlanksbaneVenom, bool hasHindsbaneVenom, bool isFromHunters)
    {
        if (isFromHunters)
        {
            // From Hunter's Sting path
            if (hasHindstungVenom)
                return HindstingStrike; // Use rear to consume Hindstung
            if (hasFlankstungVenom)
                return FlankstingStrike; // Use flank to consume Flankstung
            // Default: Flanksting (flank)
            return FlankstingStrike;
        }
        else
        {
            // From Swiftskin's Sting path
            if (hasHindsbaneVenom)
                return HindsbaneFang; // Use rear to consume Hindsbane
            if (hasFlanksbaneVenom)
                return FlanksbaneFang; // Use flank to consume Flanksbane
            // Default: Flanksbane (flank)
            return FlanksbaneFang;
        }
    }

    /// <summary>
    /// Gets the correct oGCD to use based on DreadCombo state.
    /// </summary>
    public static ActionDefinition? GetDreadComboOgcd(DreadCombo state, bool isAoe)
    {
        return state switch
        {
            DreadCombo.HunterCoilReady => Twinfang,
            DreadCombo.SwiftskinCoilReady => Twinblood,
            DreadCombo.HunterDenReady => isAoe ? TwinfangBite : Twinfang,
            DreadCombo.SwiftskinDenReady => isAoe ? TwinbloodBite : Twinblood,
            _ => null
        };
    }

    /// <summary>
    /// Gets the correct Legacy oGCD based on SerpentCombo state.
    /// </summary>
    public static ActionDefinition? GetLegacyOgcd(SerpentCombo state)
    {
        return state switch
        {
            SerpentCombo.FirstLegacy => FirstLegacy,
            SerpentCombo.SecondLegacy => SecondLegacy,
            SerpentCombo.ThirdLegacy => ThirdLegacy,
            SerpentCombo.FourthLegacy => FourthLegacy,
            SerpentCombo.DeathRattle => DeathRattle,
            SerpentCombo.LastLash => LastLash,
            _ => null
        };
    }

    /// <summary>
    /// Gets the correct Generation GCD based on Anguine Tribute count.
    /// </summary>
    public static ActionDefinition GetGenerationGcd(int anguineTribute)
    {
        return anguineTribute switch
        {
            5 => FirstGeneration,
            4 => SecondGeneration,
            3 => ThirdGeneration,
            2 => FourthGeneration,
            1 => Ouroboros, // Last action consumes final stack
            _ => Reawaken // Fallback
        };
    }

    #endregion
}
