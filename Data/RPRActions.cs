using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Reaper (RPR) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Thanatos, the Greek god of death.
/// </summary>
public static class RPRActions
{
    #region Basic Combo GCDs

    /// <summary>
    /// Slice - Basic combo starter (Lv.1)
    /// First hit of the standard combo.
    /// </summary>
    public static readonly ActionDefinition Slice = new()
    {
        ActionId = 24373,
        Name = "Slice",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 320
    };

    /// <summary>
    /// Waxing Slice - Second combo hit (Lv.5)
    /// Combo from Slice.
    /// </summary>
    public static readonly ActionDefinition WaxingSlice = new()
    {
        ActionId = 24374,
        Name = "Waxing Slice",
        MinLevel = 5,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 400
    };

    /// <summary>
    /// Infernal Slice - Third combo hit (Lv.30)
    /// Combo from Waxing Slice. Grants 10 Soul Gauge.
    /// </summary>
    public static readonly ActionDefinition InfernalSlice = new()
    {
        ActionId = 24375,
        Name = "Infernal Slice",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
        // Grants 10 Soul Gauge on combo
    };

    #endregion

    #region AoE Combo GCDs

    /// <summary>
    /// Spinning Scythe - AoE combo starter (Lv.25)
    /// Circle AoE around self.
    /// </summary>
    public static readonly ActionDefinition SpinningScythe = new()
    {
        ActionId = 24376,
        Name = "Spinning Scythe",
        MinLevel = 25,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Nightmare Scythe - AoE combo finisher (Lv.45)
    /// Combo from Spinning Scythe. Grants 10 Soul Gauge.
    /// </summary>
    public static readonly ActionDefinition NightmareScythe = new()
    {
        ActionId = 24377,
        Name = "Nightmare Scythe",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 180
        // Grants 10 Soul Gauge on combo
    };

    #endregion

    #region Death's Design (DoT/Debuff)

    /// <summary>
    /// Shadow of Death - Single target debuff (Lv.10)
    /// Applies Death's Design (+10% damage taken) for 30s.
    /// Extends duration by 30s, max 60s.
    /// </summary>
    public static readonly ActionDefinition ShadowOfDeath = new()
    {
        ActionId = 24378,
        Name = "Shadow of Death",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300,
        AppliedStatusId = StatusIds.DeathsDesign,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Whorl of Death - AoE debuff (Lv.35)
    /// Applies Death's Design (+10% damage taken) for 30s to all enemies.
    /// </summary>
    public static readonly ActionDefinition WhorlOfDeath = new()
    {
        ActionId = 24379,
        Name = "Whorl of Death",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.DeathsDesign,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region Soul Reaver GCDs (Gibbet/Gallows/Guillotine)

    /// <summary>
    /// Gibbet - Flank positional Soul Reaver GCD (Lv.70)
    /// Consumes Soul Reaver. Grants Enhanced Gallows.
    /// </summary>
    public static readonly ActionDefinition Gibbet = new()
    {
        ActionId = 24382,
        Name = "Gibbet",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.EnhancedGallows,
        AppliedStatusDuration = 60f
        // Flank positional: +60 potency
        // Grants 10 Shroud Gauge
    };

    /// <summary>
    /// Gallows - Rear positional Soul Reaver GCD (Lv.70)
    /// Consumes Soul Reaver. Grants Enhanced Gibbet.
    /// </summary>
    public static readonly ActionDefinition Gallows = new()
    {
        ActionId = 24383,
        Name = "Gallows",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.EnhancedGibbet,
        AppliedStatusDuration = 60f
        // Rear positional: +60 potency
        // Grants 10 Shroud Gauge
    };

    /// <summary>
    /// Guillotine - AoE Soul Reaver GCD (Lv.70)
    /// Consumes Soul Reaver.
    /// </summary>
    public static readonly ActionDefinition Guillotine = new()
    {
        ActionId = 24384,
        Name = "Guillotine",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 200
        // Grants 10 Shroud Gauge
    };

    /// <summary>
    /// Void Reaping - Enhanced Gibbet (Lv.80)
    /// Upgraded version during Enshroud.
    /// </summary>
    public static readonly ActionDefinition VoidReaping = new()
    {
        ActionId = 24395,
        Name = "Void Reaping",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f, // Reduced GCD during Enshroud
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.EnhancedCrossReaping,
        AppliedStatusDuration = 30f
        // Consumes 1 Lemure Shroud
        // Grants 1 Void Shroud
    };

    /// <summary>
    /// Cross Reaping - Enhanced Gallows (Lv.80)
    /// Upgraded version during Enshroud.
    /// </summary>
    public static readonly ActionDefinition CrossReaping = new()
    {
        ActionId = 24396,
        Name = "Cross Reaping",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1.5f, // Reduced GCD during Enshroud
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460,
        AppliedStatusId = StatusIds.EnhancedVoidReaping,
        AppliedStatusDuration = 30f
        // Consumes 1 Lemure Shroud
        // Grants 1 Void Shroud
    };

    /// <summary>
    /// Grim Reaping - AoE Enshroud GCD (Lv.80)
    /// </summary>
    public static readonly ActionDefinition GrimReaping = new()
    {
        ActionId = 24397,
        Name = "Grim Reaping",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f, // Reduced GCD during Enshroud
        Range = 0f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 200
        // Consumes 1 Lemure Shroud
        // Grants 1 Void Shroud
    };

    #endregion

    #region Enshroud Finishers

    /// <summary>
    /// Communio - Enshroud finisher (Lv.90)
    /// Consumes remaining Lemure Shroud. High potency finisher.
    /// </summary>
    public static readonly ActionDefinition Communio = new()
    {
        ActionId = 24398,
        Name = "Communio",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.3f, // Cast time
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // Has splash damage
        MpCost = 0,
        DamagePotency = 1100
        // Consumes remaining Lemure Shroud
        // Grants Perfectio Parata at level 100
    };

    /// <summary>
    /// Perfectio - Post-Communio finisher (Lv.100)
    /// Follow-up after Communio. Dawntrail capstone.
    /// </summary>
    public static readonly ActionDefinition Perfectio = new()
    {
        ActionId = 36973,
        Name = "Perfectio",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1300
        // Requires Perfectio Parata buff
    };

    /// <summary>
    /// Executioner's Gibbet - Enhanced Gibbet during Executioner (Lv.96)
    /// Game auto-replaces Gibbet when Executioner buff is active. Flank positional.
    /// </summary>
    public static readonly ActionDefinition ExecutionersGibbet = new()
    {
        ActionId = 36970,
        Name = "Executioner's Gibbet",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
        // Flank positional, consumes Executioner stack
    };

    /// <summary>
    /// Executioner's Gallows - Enhanced Gallows during Executioner (Lv.96)
    /// Game auto-replaces Gallows when Executioner buff is active. Rear positional.
    /// </summary>
    public static readonly ActionDefinition ExecutionersGallows = new()
    {
        ActionId = 36971,
        Name = "Executioner's Gallows",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 700
        // Rear positional, consumes Executioner stack
    };

    /// <summary>
    /// Executioner's Guillotine - Enhanced Guillotine during Executioner (Lv.96)
    /// Game auto-replaces Guillotine when Executioner buff is active. AoE.
    /// </summary>
    public static readonly ActionDefinition ExecutionersGuillotine = new()
    {
        ActionId = 36972,
        Name = "Executioner's Guillotine",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 8f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 340
    };

    #endregion

    #region Soul Spenders (oGCD)

    /// <summary>
    /// Blood Stalk - Single target Soul spender (Lv.50)
    /// Consumes 50 Soul. Grants 1 stack of Soul Reaver.
    /// </summary>
    public static readonly ActionDefinition BloodStalk = new()
    {
        ActionId = 24389,
        Name = "Blood Stalk",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 340
        // Costs 50 Soul Gauge
        // Grants Soul Reaver (1 stack)
    };

    /// <summary>
    /// Grim Swathe - AoE Soul spender (Lv.55)
    /// Consumes 50 Soul. Grants 1 stack of Soul Reaver.
    /// </summary>
    public static readonly ActionDefinition GrimSwathe = new()
    {
        ActionId = 24392,
        Name = "Grim Swathe",
        MinLevel = 55,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 140
        // Costs 50 Soul Gauge
        // Grants Soul Reaver (1 stack)
    };

    /// <summary>
    /// Gluttony - Enhanced Soul spender (Lv.76)
    /// Consumes 50 Soul. Grants 2 stacks of Soul Reaver.
    /// </summary>
    public static readonly ActionDefinition Gluttony = new()
    {
        ActionId = 24393,
        Name = "Gluttony",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // 60s cooldown
        Range = 3f,
        Radius = 5f, // Splash damage
        MpCost = 0,
        DamagePotency = 520
        // Costs 50 Soul Gauge
        // Grants Soul Reaver (2 stacks)
    };

    /// <summary>
    /// Unveiled Gibbet - Enhanced Blood Stalk (Lv.70)
    /// Only available with Enhanced Gibbet buff.
    /// </summary>
    public static readonly ActionDefinition UnveiledGibbet = new()
    {
        ActionId = 24390,
        Name = "Unveiled Gibbet",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440
        // Costs 50 Soul Gauge
        // Requires Enhanced Gibbet
    };

    /// <summary>
    /// Unveiled Gallows - Enhanced Blood Stalk (Lv.70)
    /// Only available with Enhanced Gallows buff.
    /// </summary>
    public static readonly ActionDefinition UnveiledGallows = new()
    {
        ActionId = 24391,
        Name = "Unveiled Gallows",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 440
        // Costs 50 Soul Gauge
        // Requires Enhanced Gallows
    };

    #endregion

    #region Enshroud Actions

    /// <summary>
    /// Enshroud - Enter Enshroud state (Lv.80)
    /// Consumes 50 Shroud. Grants 5 Lemure Shroud stacks.
    /// </summary>
    public static readonly ActionDefinition Enshroud = new()
    {
        ActionId = 24394,
        Name = "Enshroud",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Enshrouded,
        AppliedStatusDuration = 30f
        // Costs 50 Shroud Gauge
        // Grants 5 Lemure Shroud
    };

    /// <summary>
    /// Lemure's Slice - Enshroud oGCD (Lv.86)
    /// Consumes 2 Void Shroud. High damage oGCD.
    /// </summary>
    public static readonly ActionDefinition LemuresSlice = new()
    {
        ActionId = 24399,
        Name = "Lemure's Slice",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 280
        // Costs 2 Void Shroud
    };

    /// <summary>
    /// Lemure's Scythe - AoE Enshroud oGCD (Lv.86)
    /// Consumes 2 Void Shroud.
    /// </summary>
    public static readonly ActionDefinition LemuresScythe = new()
    {
        ActionId = 24400,
        Name = "Lemure's Scythe",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
        // Costs 2 Void Shroud
    };

    /// <summary>
    /// Sacrificium - Enhanced Enshroud finisher (Lv.92)
    /// Dawntrail addition.
    /// </summary>
    public static readonly ActionDefinition Sacrificium = new()
    {
        ActionId = 36969,
        Name = "Sacrificium",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 530
        // Requires Oblatio buff
    };

    #endregion

    #region Party Buff

    /// <summary>
    /// Arcane Circle - Party damage buff (Lv.72)
    /// +3% damage to party for 20s. Grants Immortal Sacrifice stacks.
    /// </summary>
    public static readonly ActionDefinition ArcaneCircle = new()
    {
        ActionId = 24405,
        Name = "Arcane Circle",
        MinLevel = 72,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f, // 2-minute cooldown
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ArcaneCircle,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Plentiful Harvest - Immortal Sacrifice consumer (Lv.88)
    /// Consumes Immortal Sacrifice stacks for damage.
    /// </summary>
    public static readonly ActionDefinition PlentifulHarvest = new()
    {
        ActionId = 24385,
        Name = "Plentiful Harvest",
        MinLevel = 88,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 15f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1000 // Base potency, scales with stacks
        // Requires Immortal Sacrifice stacks
        // Grants 50 Shroud Gauge
    };

    #endregion

    #region Utility Actions

    /// <summary>
    /// Soul Slice - Single target Soul builder (Lv.60)
    /// Grants 50 Soul Gauge. 2 charges.
    /// </summary>
    public static readonly ActionDefinition SoulSlice = new()
    {
        ActionId = 24380,
        Name = "Soul Slice",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f, // 30s per charge
        Range = 3f,
        MpCost = 0,
        DamagePotency = 460
        // Grants 50 Soul Gauge
    };

    /// <summary>
    /// Soul Scythe - AoE Soul builder (Lv.65)
    /// Grants 50 Soul Gauge. 2 charges.
    /// </summary>
    public static readonly ActionDefinition SoulScythe = new()
    {
        ActionId = 24381,
        Name = "Soul Scythe",
        MinLevel = 65,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f, // 30s per charge
        Range = 0f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 180
        // Grants 50 Soul Gauge
    };

    /// <summary>
    /// Harvest Moon - Ranged GCD after Soulsow (Lv.82)
    /// </summary>
    public static readonly ActionDefinition HarvestMoon = new()
    {
        ActionId = 24388,
        Name = "Harvest Moon",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 800
        // Requires Soulsow buff
    };

    /// <summary>
    /// Soulsow - Pre-combat preparation (Lv.82)
    /// Cast outside combat to prepare Harvest Moon.
    /// </summary>
    public static readonly ActionDefinition Soulsow = new()
    {
        ActionId = 24387,
        Name = "Soulsow",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 5f,
        RecastTime = 2.5f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Soulsow,
        AppliedStatusDuration = 3600f // Very long duration
    };

    /// <summary>
    /// Hell's Ingress - Forward dash (Lv.20)
    /// Grants Enhanced Harpe.
    /// </summary>
    public static readonly ActionDefinition HellsIngress = new()
    {
        ActionId = 24401,
        Name = "Hell's Ingress",
        MinLevel = 20,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 15f,
        MpCost = 0
    };

    /// <summary>
    /// Hell's Egress - Backward dash (Lv.20)
    /// Grants Enhanced Harpe.
    /// </summary>
    public static readonly ActionDefinition HellsEgress = new()
    {
        ActionId = 24402,
        Name = "Hell's Egress",
        MinLevel = 20,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 15f,
        MpCost = 0
    };

    /// <summary>
    /// Regress - Return to Hellsgate (Lv.74)
    /// </summary>
    public static readonly ActionDefinition Regress = new()
    {
        ActionId = 24403,
        Name = "Regress",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    /// <summary>
    /// Harpe - Ranged filler GCD (Lv.15)
    /// </summary>
    public static readonly ActionDefinition Harpe = new()
    {
        ActionId = 24386,
        Name = "Harpe",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.3f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Arcane Crest - Defensive barrier (Lv.40)
    /// </summary>
    public static readonly ActionDefinition ArcaneCrest = new()
    {
        ActionId = 24404,
        Name = "Arcane Crest",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
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
        // Target debuff
        public const uint DeathsDesign = 2586;

        // Soul Reaver state
        public const uint SoulReaver = 2587;

        // Enhanced positionals (proc from opposite positional)
        public const uint EnhancedGibbet = 2588;
        public const uint EnhancedGallows = 2589;

        // Enhanced Enshroud positionals
        public const uint EnhancedVoidReaping = 2590;
        public const uint EnhancedCrossReaping = 2591;

        // Enshroud state
        public const uint Enshrouded = 2593;

        // Party buff
        public const uint ArcaneCircle = 2599;
        public const uint CircleOfSacrifice = 2600; // Party member buff (granted to allies near Arcane Circle)
        public const uint ImmortalSacrifice = 2592; // Reaper's stacking buff
        public const uint BloodsownCircle = 2972; // Personal self-buff from Arcane Circle (grants Immortal Sacrifice stacks on expiry)

        // Soulsow / Harvest Moon
        public const uint Soulsow = 2594;

        // Perfectio proc (Dawntrail)
        public const uint PerfectioParata = 3860;

        // Executioner proc (Lv.96+ Gibbet/Gallows/Guillotine enhancement)
        public const uint Executioner = 3858; // Granted post-Plentiful Harvest at Lv.96+
        public const uint PerfectioOcculta = 3859; // Precursor to PerfectioParata

        // Oblatio proc for Sacrificium (Dawntrail)
        public const uint Oblatio = 3857;

        // Ideal Host (Dawntrail)
        public const uint IdealHost = 3905;

        // Enhanced Harpe
        public const uint EnhancedHarpe = 2845;

        // Threshold (Arcane Crest barrier)
        public const uint Threshold = 2595;
        public const uint CrestOfTimeBorrowed = 2597;
        public const uint CrestOfTimeReturned = 2598;

        // Role buffs
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
        public const uint Feint = 1195;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the appropriate Soul Reaver action based on enhanced buff.
    /// </summary>
    public static ActionDefinition GetSoulReaverAction(bool hasEnhancedGibbet, bool hasEnhancedGallows, bool isAoe)
    {
        if (isAoe)
            return Guillotine;

        // Use the enhanced version if we have the buff
        if (hasEnhancedGibbet)
            return Gibbet;
        if (hasEnhancedGallows)
            return Gallows;

        // Default to Gibbet (flank) if no buff
        return Gibbet;
    }

    /// <summary>
    /// Gets the appropriate Enshroud GCD based on enhanced buff.
    /// </summary>
    public static ActionDefinition GetEnshroudGcd(bool hasEnhancedVoidReaping, bool hasEnhancedCrossReaping, bool isAoe)
    {
        if (isAoe)
            return GrimReaping;

        // Use the enhanced version if we have the buff
        if (hasEnhancedVoidReaping)
            return VoidReaping;
        if (hasEnhancedCrossReaping)
            return CrossReaping;

        // Default to Void Reaping if no buff
        return VoidReaping;
    }

    /// <summary>
    /// Gets the appropriate Soul spender based on state.
    /// </summary>
    public static ActionDefinition GetSoulSpender(bool hasEnhancedGibbet, bool hasEnhancedGallows, bool gluttonyReady, bool isAoe)
    {
        if (isAoe)
            return GrimSwathe;

        if (gluttonyReady)
            return Gluttony;

        // Use Unveiled versions if we have the buff
        if (hasEnhancedGibbet)
            return UnveiledGibbet;
        if (hasEnhancedGallows)
            return UnveiledGallows;

        return BloodStalk;
    }

    #endregion
}
