using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Pictomancer (PCT) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Iris, the Greek goddess of the rainbow.
/// </summary>
public static class PCTActions
{
    #region Base Combo GCDs (Subtractive Colors)

    /// <summary>
    /// Fire in Red - Base combo starter (Lv.1)
    /// Short cast, generates palette gauge
    /// </summary>
    public static readonly ActionDefinition FireInRed = new()
    {
        ActionId = 34650,
        Name = "Fire in Red",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 440
    };

    /// <summary>
    /// Aero in Green - Base combo second hit (Lv.5)
    /// </summary>
    public static readonly ActionDefinition AeroInGreen = new()
    {
        ActionId = 34651,
        Name = "Aero in Green",
        MinLevel = 5,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 480
    };

    /// <summary>
    /// Water in Blue - Base combo finisher (Lv.15)
    /// Grants White Paint stack
    /// </summary>
    public static readonly ActionDefinition WaterInBlue = new()
    {
        ActionId = 34652,
        Name = "Water in Blue",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 520
    };

    #endregion

    #region Subtractive Combo GCDs (Additive Colors)

    /// <summary>
    /// Blizzard in Cyan - Subtractive combo starter (Lv.60)
    /// Higher potency, requires Subtractive Palette
    /// </summary>
    public static readonly ActionDefinition BlizzardInCyan = new()
    {
        ActionId = 34653,
        Name = "Blizzard in Cyan",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 840
    };

    /// <summary>
    /// Stone in Yellow - Subtractive combo second hit (Lv.60)
    /// </summary>
    public static readonly ActionDefinition StoneInYellow = new()
    {
        ActionId = 34654,
        Name = "Stone in Yellow",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 880
    };

    /// <summary>
    /// Thunder in Magenta - Subtractive combo finisher (Lv.60)
    /// Grants White Paint stack
    /// </summary>
    public static readonly ActionDefinition ThunderInMagenta = new()
    {
        ActionId = 34655,
        Name = "Thunder in Magenta",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 920
    };

    #endregion

    #region AoE Base Combo

    /// <summary>
    /// Fire II in Red - AoE base combo starter (Lv.25)
    /// </summary>
    public static readonly ActionDefinition Fire2InRed = new()
    {
        ActionId = 34656,
        Name = "Fire II in Red",
        MinLevel = 25,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 140
    };

    /// <summary>
    /// Aero II in Green - AoE base combo second hit (Lv.35)
    /// </summary>
    public static readonly ActionDefinition Aero2InGreen = new()
    {
        ActionId = 34657,
        Name = "Aero II in Green",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 160
    };

    /// <summary>
    /// Water II in Blue - AoE base combo finisher (Lv.45)
    /// Grants White Paint stack
    /// </summary>
    public static readonly ActionDefinition Water2InBlue = new()
    {
        ActionId = 34658,
        Name = "Water II in Blue",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 180
    };

    #endregion

    #region AoE Subtractive Combo

    /// <summary>
    /// Blizzard II in Cyan - AoE subtractive combo starter (Lv.60)
    /// </summary>
    public static readonly ActionDefinition Blizzard2InCyan = new()
    {
        ActionId = 34659,
        Name = "Blizzard II in Cyan",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 280
    };

    /// <summary>
    /// Stone II in Yellow - AoE subtractive combo second hit (Lv.60)
    /// </summary>
    public static readonly ActionDefinition Stone2InYellow = new()
    {
        ActionId = 34660,
        Name = "Stone II in Yellow",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 300
    };

    /// <summary>
    /// Thunder II in Magenta - AoE subtractive combo finisher (Lv.60)
    /// </summary>
    public static readonly ActionDefinition Thunder2InMagenta = new()
    {
        ActionId = 34661,
        Name = "Thunder II in Magenta",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.3f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 320
    };

    #endregion

    #region Paint Spenders

    /// <summary>
    /// Holy in White - Instant White Paint spender (Lv.80)
    /// Good for movement, instant cast
    /// </summary>
    public static readonly ActionDefinition HolyInWhite = new()
    {
        ActionId = 34662,
        Name = "Holy in White",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 560
    };

    /// <summary>
    /// Comet in Black - Instant Black Paint spender (Lv.90)
    /// High damage, instant cast
    /// </summary>
    public static readonly ActionDefinition CometInBlack = new()
    {
        ActionId = 34663,
        Name = "Comet in Black",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 880
    };

    #endregion

    #region Motif GCDs

    /// <summary>
    /// Creature Motif - Paint a creature on canvas (Lv.30)
    /// Long cast, used pre-pull or in downtime
    /// </summary>
    public static readonly ActionDefinition CreatureMotif = new()
    {
        ActionId = 34689,
        Name = "Creature Motif",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Pom Motif - Paint a Pom creature (base form at Lv.30)
    /// </summary>
    public static readonly ActionDefinition PomMotif = new()
    {
        ActionId = 34664,
        Name = "Pom Motif",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Wing Motif - Paint a Winged creature (Lv.30)
    /// </summary>
    public static readonly ActionDefinition WingMotif = new()
    {
        ActionId = 34665,
        Name = "Wing Motif",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Claw Motif - Paint a Clawed creature (Lv.96)
    /// </summary>
    public static readonly ActionDefinition ClawMotif = new()
    {
        ActionId = 34666,
        Name = "Claw Motif",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Maw Motif - Paint a Fanged creature (Lv.96)
    /// </summary>
    public static readonly ActionDefinition MawMotif = new()
    {
        ActionId = 34667,
        Name = "Maw Motif",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Weapon Motif - Paint a Hammer on canvas (Lv.50)
    /// </summary>
    public static readonly ActionDefinition WeaponMotif = new()
    {
        ActionId = 34690,
        Name = "Weapon Motif",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Hammer Motif - Upgraded weapon motif (Lv.50)
    /// </summary>
    public static readonly ActionDefinition HammerMotif = new()
    {
        ActionId = 34668,
        Name = "Hammer Motif",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Landscape Motif - Paint Starry Sky on canvas (Lv.70)
    /// </summary>
    public static readonly ActionDefinition LandscapeMotif = new()
    {
        ActionId = 34691,
        Name = "Landscape Motif",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Starry Sky Motif - Upgraded landscape motif (Lv.70)
    /// </summary>
    public static readonly ActionDefinition StarrySkyMotif = new()
    {
        ActionId = 34669,
        Name = "Starry Sky Motif",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    #endregion

    #region Muse oGCDs

    /// <summary>
    /// Living Muse - Summon creature from canvas (Lv.30)
    /// Base form, upgrades based on painted creature
    /// </summary>
    public static readonly ActionDefinition LivingMuse = new()
    {
        ActionId = 35347,
        Name = "Living Muse",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Pom Muse - Summon Pom creature (Lv.30)
    /// </summary>
    public static readonly ActionDefinition PomMuse = new()
    {
        ActionId = 34670,
        Name = "Pom Muse",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Winged Muse - Summon Winged creature (Lv.30)
    /// </summary>
    public static readonly ActionDefinition WingedMuse = new()
    {
        ActionId = 34671,
        Name = "Winged Muse",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Clawed Muse - Summon Clawed creature (Lv.96)
    /// </summary>
    public static readonly ActionDefinition ClawedMuse = new()
    {
        ActionId = 34672,
        Name = "Clawed Muse",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Fanged Muse - Summon Fanged creature (Lv.96)
    /// </summary>
    public static readonly ActionDefinition FangedMuse = new()
    {
        ActionId = 34673,
        Name = "Fanged Muse",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1100
    };

    /// <summary>
    /// Steel Muse - Summon weapon from canvas (Lv.50)
    /// Unlocks Hammer combo
    /// </summary>
    public static readonly ActionDefinition SteelMuse = new()
    {
        ActionId = 35348,
        Name = "Steel Muse",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    /// <summary>
    /// Striking Muse - Activate hammer from canvas (Lv.50)
    /// Unlocks Hammer Stamp combo
    /// </summary>
    public static readonly ActionDefinition StrikingMuse = new()
    {
        ActionId = 34674,
        Name = "Striking Muse",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    /// <summary>
    /// Scenic Muse - Activate landscape from canvas (Lv.70)
    /// </summary>
    public static readonly ActionDefinition ScenicMuse = new()
    {
        ActionId = 35349,
        Name = "Scenic Muse",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
    };

    /// <summary>
    /// Starry Muse - Party damage buff (Lv.70)
    /// 20s window, +5% damage for party
    /// </summary>
    public static readonly ActionDefinition StarryMuse = new()
    {
        ActionId = 34675,
        Name = "Starry Muse",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.StarryMuse,
        AppliedStatusDuration = 20f
    };

    #endregion

    #region Hammer Combo GCDs

    /// <summary>
    /// Hammer Stamp - Hammer combo starter (Lv.50)
    /// Instant cast, high damage
    /// </summary>
    public static readonly ActionDefinition HammerStamp = new()
    {
        ActionId = 34678,
        Name = "Hammer Stamp",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 560
    };

    /// <summary>
    /// Hammer Brush - Hammer combo second hit (Lv.86)
    /// </summary>
    public static readonly ActionDefinition HammerBrush = new()
    {
        ActionId = 34679,
        Name = "Hammer Brush",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 620
    };

    /// <summary>
    /// Polishing Hammer - Hammer combo finisher (Lv.86)
    /// </summary>
    public static readonly ActionDefinition PolishingHammer = new()
    {
        ActionId = 34680,
        Name = "Polishing Hammer",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 680
    };

    #endregion

    #region Portraits

    /// <summary>
    /// Mog of the Ages - Portrait after 2 Living Muses (Lv.30)
    /// </summary>
    public static readonly ActionDefinition MogOfTheAges = new()
    {
        ActionId = 34676,
        Name = "Mog of the Ages",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1300
    };

    /// <summary>
    /// Retribution of the Madeen - Portrait after 4 Living Muses (Lv.96)
    /// </summary>
    public static readonly ActionDefinition RetributionOfTheMadeen = new()
    {
        ActionId = 34677,
        Name = "Retribution of the Madeen",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1400
    };

    #endregion

    #region Finishers

    /// <summary>
    /// Rainbow Drip - Powerful finisher GCD (Lv.92)
    /// Available with Rainbow Bright or during burst
    /// </summary>
    public static readonly ActionDefinition RainbowDrip = new()
    {
        ActionId = 34688,
        Name = "Rainbow Drip",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 4.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 1000
    };

    /// <summary>
    /// Star Prism - Starry Muse finisher (Lv.100)
    /// Available during Starstruck
    /// </summary>
    public static readonly ActionDefinition StarPrism = new()
    {
        ActionId = 34681,
        Name = "Star Prism",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 25f, // Party-wide damage + heal
        MpCost = 0,
        DamagePotency = 1400,
        HealPotency = 500
    };

    #endregion

    #region Utility oGCDs

    /// <summary>
    /// Subtractive Palette - Enable subtractive combo (Lv.60)
    /// Requires 50 palette gauge
    /// </summary>
    public static readonly ActionDefinition SubtractivePalette = new()
    {
        ActionId = 34683,
        Name = "Subtractive Palette",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0,
        AppliedStatusId = StatusIds.SubtractivePalette,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Tempera Coat - Personal mitigation (Lv.10)
    /// 20% damage reduction
    /// </summary>
    public static readonly ActionDefinition TemperaCoat = new()
    {
        ActionId = 34685,
        Name = "Tempera Coat",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.TemperaCoat,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Tempera Grassa - Party mitigation (Lv.88)
    /// 10% damage reduction for party
    /// </summary>
    public static readonly ActionDefinition TemperaGrassa = new()
    {
        ActionId = 34686,
        Name = "Tempera Grassa",
        MinLevel = 88,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.TemperaGrassa,
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Smudge - Sprint/movement ability (Lv.20)
    /// </summary>
    public static readonly ActionDefinition Smudge = new()
    {
        ActionId = 34684,
        Name = "Smudge",
        MinLevel = 20,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 20f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Smudge,
        AppliedStatusDuration = 5f
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Role buffs
        public const uint Swiftcast = 167;
        public const uint LucidDreaming = 1204;
        public const uint Surecast = 160;

        // Pictomancer core
        public const uint SubtractivePalette = 3674;     // Enables subtractive combo
        public const uint MonochromeTones = 3691;        // Monochrome mode active
        public const uint StarryMuse = 3685;             // Party damage buff (20s)
        public const uint Starstruck = 3681;             // Star Prism ready
        public const uint Hyperphantasia = 3688;         // Enhanced state during Starry Muse
        public const uint Inspiration = 3689;            // Motif cast time reduction
        public const uint SubtractiveSpectrum = 3690;    // Enhanced subtractive spells
        public const uint RainbowBright = 3679;          // Rainbow Drip ready
        public const uint HammerTime = 3680;             // Hammer combo active
        public const uint Aetherhues = 3675;             // Aetherhue stacks

        // Mitigation
        public const uint TemperaCoat = 3686;            // Personal mitigation
        public const uint TemperaGrassa = 3687;          // Party mitigation
        public const uint Smudge = 3684;                 // Sprint buff

        // Debuffs
        public const uint Addle = 1203;
    }

    #endregion

    #region Canvas State Enums

    /// <summary>
    /// Creature motif types that can be painted on the canvas.
    /// </summary>
    public enum CreatureMotifType : byte
    {
        None = 0,
        Pom = 1,
        Wing = 2,
        Claw = 3,
        Maw = 4
    }

    /// <summary>
    /// Canvas flags for tracking what's been painted.
    /// </summary>
    [System.Flags]
    public enum CanvasFlags : byte
    {
        None = 0,
        Pom = 1,
        Wing = 2,
        Claw = 4,
        Maw = 8,
        Weapon = 16,
        Landscape = 32
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the appropriate creature motif to paint based on level and current creature count.
    /// </summary>
    public static ActionDefinition GetCreatureMotif(byte level, int creatureCount)
    {
        // At level 96+, alternate between Claw and Maw for Madeen
        if (level >= ClawMotif.MinLevel)
        {
            return creatureCount % 2 == 0 ? ClawMotif : MawMotif;
        }

        // Before 96, alternate between Pom and Wing
        return creatureCount % 2 == 0 ? PomMotif : WingMotif;
    }

    /// <summary>
    /// Gets the Living Muse action based on the creature painted on canvas.
    /// </summary>
    public static ActionDefinition GetLivingMuse(CreatureMotifType creatureType)
    {
        return creatureType switch
        {
            CreatureMotifType.Pom => PomMuse,
            CreatureMotifType.Wing => WingedMuse,
            CreatureMotifType.Claw => ClawedMuse,
            CreatureMotifType.Maw => FangedMuse,
            _ => LivingMuse
        };
    }

    /// <summary>
    /// Gets the next hammer combo action based on combo step.
    /// </summary>
    public static ActionDefinition? GetHammerComboAction(int comboStep, byte level)
    {
        return comboStep switch
        {
            0 => level >= HammerStamp.MinLevel ? HammerStamp : null,
            1 => level >= HammerBrush.MinLevel ? HammerBrush : HammerStamp,
            2 => level >= PolishingHammer.MinLevel ? PolishingHammer : null,
            _ => null
        };
    }

    /// <summary>
    /// Gets the portrait action based on muse count.
    /// After 2 muses: Mog of the Ages
    /// After 4 muses: Retribution of the Madeen
    /// </summary>
    public static ActionDefinition? GetPortrait(int museCount, byte level)
    {
        if (museCount >= 4 && level >= RetributionOfTheMadeen.MinLevel)
            return RetributionOfTheMadeen;
        if (museCount >= 2)
            return MogOfTheAges;
        return null;
    }

    /// <summary>
    /// Determines if we should use AoE rotation based on enemy count.
    /// </summary>
    public static bool ShouldUseAoe(int enemyCount, byte level)
    {
        // Need at least 3 enemies for AoE to be worth it
        // Also need to be high enough level for AoE skills
        return enemyCount >= 3 && level >= Fire2InRed.MinLevel;
    }

    /// <summary>
    /// Gets the base combo action based on combo step and AoE mode.
    /// </summary>
    public static ActionDefinition GetBaseComboAction(int comboStep, bool isAoe, byte level)
    {
        if (isAoe && level >= Fire2InRed.MinLevel)
        {
            return comboStep switch
            {
                0 => Fire2InRed,
                1 => level >= Aero2InGreen.MinLevel ? Aero2InGreen : Fire2InRed,
                2 => level >= Water2InBlue.MinLevel ? Water2InBlue : (level >= Aero2InGreen.MinLevel ? Aero2InGreen : Fire2InRed),
                _ => Fire2InRed
            };
        }

        return comboStep switch
        {
            0 => FireInRed,
            1 => level >= AeroInGreen.MinLevel ? AeroInGreen : FireInRed,
            2 => level >= WaterInBlue.MinLevel ? WaterInBlue : (level >= AeroInGreen.MinLevel ? AeroInGreen : FireInRed),
            _ => FireInRed
        };
    }

    /// <summary>
    /// Gets the subtractive combo action based on combo step and AoE mode.
    /// </summary>
    public static ActionDefinition GetSubtractiveComboAction(int comboStep, bool isAoe, byte level)
    {
        if (level < BlizzardInCyan.MinLevel)
            return GetBaseComboAction(comboStep, isAoe, level);

        if (isAoe)
        {
            return comboStep switch
            {
                0 => Blizzard2InCyan,
                1 => Stone2InYellow,
                2 => Thunder2InMagenta,
                _ => Blizzard2InCyan
            };
        }

        return comboStep switch
        {
            0 => BlizzardInCyan,
            1 => StoneInYellow,
            2 => ThunderInMagenta,
            _ => BlizzardInCyan
        };
    }

    #endregion
}
