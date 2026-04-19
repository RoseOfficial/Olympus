using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Red Mage (RDM) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Circe, the Greek goddess of sorcery who transformed her enemies.
/// </summary>
public static class RDMActions
{
    #region Filler GCDs (Hardcast)

    /// <summary>
    /// Jolt - Basic hardcast filler (Lv.2)
    /// Triggers Dualcast proc
    /// </summary>
    public static readonly ActionDefinition Jolt = new()
    {
        ActionId = 7503,
        Name = "Jolt",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 170
    };

    /// <summary>
    /// Jolt II - Enhanced hardcast filler (Lv.62)
    /// Upgrades Jolt
    /// </summary>
    public static readonly ActionDefinition Jolt2 = new()
    {
        ActionId = 7524,
        Name = "Jolt II",
        MinLevel = 62,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 280
    };

    /// <summary>
    /// Jolt III - Maximum potency hardcast filler (Lv.84)
    /// Upgrades Jolt II
    /// </summary>
    public static readonly ActionDefinition Jolt3 = new()
    {
        ActionId = 37004,
        Name = "Jolt III",
        MinLevel = 84,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 360
    };

    #endregion

    #region Long Spells (Dualcast Consumers)

    /// <summary>
    /// Verthunder - Black mana generator (Lv.4)
    /// +6 Black Mana, 5s cast (instant with Dualcast)
    /// </summary>
    public static readonly ActionDefinition Verthunder = new()
    {
        ActionId = 7505,
        Name = "Verthunder",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 5.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 360
    };

    /// <summary>
    /// Veraero - White mana generator (Lv.10)
    /// +6 White Mana, 5s cast (instant with Dualcast)
    /// </summary>
    public static readonly ActionDefinition Veraero = new()
    {
        ActionId = 7507,
        Name = "Veraero",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 5.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 360
    };

    /// <summary>
    /// Verthunder III - Enhanced black mana generator (Lv.82)
    /// Upgrades Verthunder
    /// </summary>
    public static readonly ActionDefinition Verthunder3 = new()
    {
        ActionId = 25855,
        Name = "Verthunder III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 5.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 440
    };

    /// <summary>
    /// Veraero III - Enhanced white mana generator (Lv.82)
    /// Upgrades Veraero
    /// </summary>
    public static readonly ActionDefinition Veraero3 = new()
    {
        ActionId = 25856,
        Name = "Veraero III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 5.0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 440
    };

    #endregion

    #region Proc Spells (Instant)

    /// <summary>
    /// Verfire - Black mana proc spell (Lv.26)
    /// +5 Black Mana, instant, requires Verfire Ready
    /// </summary>
    public static readonly ActionDefinition Verfire = new()
    {
        ActionId = 7510,
        Name = "Verfire",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 380
    };

    /// <summary>
    /// Verstone - White mana proc spell (Lv.30)
    /// +5 White Mana, instant, requires Verstone Ready
    /// </summary>
    public static readonly ActionDefinition Verstone = new()
    {
        ActionId = 7511,
        Name = "Verstone",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 380
    };

    #endregion

    #region Melee Combo GCDs

    /// <summary>
    /// Riposte - Melee combo starter (Lv.1)
    /// Enchanted version requires mana
    /// </summary>
    public static readonly ActionDefinition Riposte = new()
    {
        ActionId = 7504,
        Name = "Riposte",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Enchanted Riposte - Melee combo starter with mana (Lv.1)
    /// Requires 20|20 mana, consumes 20 of each
    /// </summary>
    public static readonly ActionDefinition EnchantedRiposte = new()
    {
        ActionId = 7527,
        Name = "Enchanted Riposte",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f, // Weaponskill recast
        Range = 3f,
        MpCost = 0,
        DamagePotency = 300
    };

    /// <summary>
    /// Zwerchhau - Melee combo second hit (Lv.35)
    /// </summary>
    public static readonly ActionDefinition Zwerchhau = new()
    {
        ActionId = 7512,
        Name = "Zwerchhau",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Enchanted Zwerchhau - Melee combo second hit with mana (Lv.35)
    /// Requires 15|15 mana, consumes 15 of each
    /// </summary>
    public static readonly ActionDefinition EnchantedZwerchhau = new()
    {
        ActionId = 7528,
        Name = "Enchanted Zwerchhau",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 360
    };

    /// <summary>
    /// Redoublement - Melee combo finisher (Lv.50)
    /// </summary>
    public static readonly ActionDefinition Redoublement = new()
    {
        ActionId = 7516,
        Name = "Redoublement",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Enchanted Redoublement - Melee combo finisher with mana (Lv.50)
    /// Requires 15|15 mana, consumes 15 of each
    /// </summary>
    public static readonly ActionDefinition EnchantedRedoublement = new()
    {
        ActionId = 7529,
        Name = "Enchanted Redoublement",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.2f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 500
    };

    #endregion

    #region Finisher GCDs

    /// <summary>
    /// Verflare - Black mana finisher (Lv.68)
    /// +21 Black Mana, guaranteed Verfire proc
    /// Use when Black < White
    /// </summary>
    public static readonly ActionDefinition Verflare = new()
    {
        ActionId = 7525,
        Name = "Verflare",
        MinLevel = 68,
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
    /// Verholy - White mana finisher (Lv.70)
    /// +21 White Mana, guaranteed Verstone proc
    /// Use when White < Black
    /// </summary>
    public static readonly ActionDefinition Verholy = new()
    {
        ActionId = 7526,
        Name = "Verholy",
        MinLevel = 70,
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
    /// Scorch - Follow-up after Verflare/Verholy (Lv.80)
    /// +4 Black and White Mana
    /// </summary>
    public static readonly ActionDefinition Scorch = new()
    {
        ActionId = 16530,
        Name = "Scorch",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Resolution - Final finisher after Scorch (Lv.90)
    /// +4 Black and White Mana
    /// </summary>
    public static readonly ActionDefinition Resolution = new()
    {
        ActionId = 25858,
        Name = "Resolution",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 800
    };

    /// <summary>
    /// Grand Impact - Powerful instant GCD (Lv.96)
    /// Available after Resolution or via Acceleration
    /// </summary>
    public static readonly ActionDefinition GrandImpact = new()
    {
        ActionId = 37006,
        Name = "Grand Impact",
        MinLevel = 96,
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

    #endregion

    #region AoE GCDs

    /// <summary>
    /// Verthunder II - AoE black mana generator (Lv.18)
    /// +7 Black Mana
    /// </summary>
    public static readonly ActionDefinition Verthunder2 = new()
    {
        ActionId = 16524,
        Name = "Verthunder II",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 200,
        DamagePotency = 140
    };

    /// <summary>
    /// Veraero II - AoE white mana generator (Lv.22)
    /// +7 White Mana
    /// </summary>
    public static readonly ActionDefinition Veraero2 = new()
    {
        ActionId = 16525,
        Name = "Veraero II",
        MinLevel = 22,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 200,
        DamagePotency = 140
    };

    /// <summary>
    /// Impact - AoE instant damage (Lv.66)
    /// Consumes Dualcast
    /// </summary>
    public static readonly ActionDefinition Impact = new()
    {
        ActionId = 16526,
        Name = "Impact",
        MinLevel = 66,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 5.0f, // Instant with Dualcast
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 210
    };

    /// <summary>
    /// Enchanted Moulinet - AoE melee (Lv.52)
    /// Requires 20|20 mana
    /// </summary>
    public static readonly ActionDefinition EnchantedMoulinet = new()
    {
        ActionId = 7530,
        Name = "Enchanted Moulinet",
        MinLevel = 52,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Enchanted Moulinet Deux - AoE melee second hit (Lv.96)
    /// </summary>
    public static readonly ActionDefinition EnchantedMoulinetDeux = new()
    {
        ActionId = 37002,
        Name = "Enchanted Moulinet Deux",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 140
    };

    /// <summary>
    /// Enchanted Moulinet Trois - AoE melee third hit (Lv.96)
    /// </summary>
    public static readonly ActionDefinition EnchantedMoulinetTrois = new()
    {
        ActionId = 37003,
        Name = "Enchanted Moulinet Trois",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1.5f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 150
    };

    #endregion

    #region oGCDs - Damage

    /// <summary>
    /// Fleche - Single target oGCD (Lv.45)
    /// 25s recast
    /// </summary>
    public static readonly ActionDefinition Fleche = new()
    {
        ActionId = 7517,
        Name = "Fleche",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 480
    };

    /// <summary>
    /// Contre Sixte - AoE oGCD (Lv.56)
    /// 45s recast
    /// </summary>
    public static readonly ActionDefinition ContreSixte = new()
    {
        ActionId = 7519,
        Name = "Contre Sixte",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 45f,
        Range = 25f,
        Radius = 6f,
        MpCost = 0,
        DamagePotency = 420
    };

    /// <summary>
    /// Corps-a-corps - Gap closer (Lv.6)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition CorpsACorps = new()
    {
        ActionId = 7506,
        Name = "Corps-a-corps",
        MinLevel = 6,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 35f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Engagement - Melee oGCD (Lv.40)
    /// 2 charges, shares cooldown with Displacement
    /// </summary>
    public static readonly ActionDefinition Engagement = new()
    {
        ActionId = 16527,
        Name = "Engagement",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 35f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 180
    };

    /// <summary>
    /// Displacement - Backstep oGCD (Lv.40)
    /// Same damage as Engagement, but moves backward
    /// </summary>
    public static readonly ActionDefinition Displacement = new()
    {
        ActionId = 7515,
        Name = "Displacement",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 35f,
        Range = 3f,
        MpCost = 0,
        DamagePotency = 180
    };

    /// <summary>
    /// Vice of Thorns - Thorns oGCD (Lv.92)
    /// Available during Thorned Flourish
    /// </summary>
    public static readonly ActionDefinition ViceOfThorns = new()
    {
        ActionId = 37005,
        Name = "Vice of Thorns",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Prefulgence - Powerful oGCD during burst (Lv.100)
    /// Available during Prefulgence Ready
    /// </summary>
    public static readonly ActionDefinition Prefulgence = new()
    {
        ActionId = 37007,
        Name = "Prefulgence",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 900
    };

    #endregion

    #region oGCDs - Buffs

    /// <summary>
    /// Embolden - Party damage buff (Lv.58)
    /// 5% magic damage for 20s
    /// </summary>
    public static readonly ActionDefinition Embolden = new()
    {
        ActionId = 7520,
        Name = "Embolden",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Embolden,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Manafication - Mana doubler and reset (Lv.60)
    /// Doubles current mana (up to 50|50), grants Manafication stacks
    /// </summary>
    public static readonly ActionDefinition Manafication = new()
    {
        ActionId = 7521,
        Name = "Manafication",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 110f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Manafication,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Acceleration - Instant cast proc (Lv.50)
    /// Guarantees Verfire/Verstone procs, enables Grand Impact at Lv.96
    /// </summary>
    public static readonly ActionDefinition Acceleration = new()
    {
        ActionId = 7518,
        Name = "Acceleration",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 55f, // 2 charges at Lv.88
        MpCost = 0,
        AppliedStatusId = StatusIds.Acceleration,
        AppliedStatusDuration = 20f
    };

    #endregion

    #region Utility

    /// <summary>
    /// Vercure - Single target heal (Lv.54)
    /// </summary>
    public static readonly ActionDefinition Vercure = new()
    {
        ActionId = 7514,
        Name = "Vercure",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 500,
        HealPotency = 350
    };

    /// <summary>
    /// Verraise - Resurrection (Lv.64)
    /// </summary>
    public static readonly ActionDefinition Verraise = new()
    {
        ActionId = 7523,
        Name = "Verraise",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Raise,
        CastTime = 10f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 2400
    };

    /// <summary>
    /// Magick Barrier - Party mitigation (Lv.86)
    /// 10% magic damage reduction + 5% healing increase
    /// </summary>
    public static readonly ActionDefinition MagickBarrier = new()
    {
        ActionId = 25857,
        Name = "Magick Barrier",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.MagickBarrier,
        AppliedStatusDuration = 10f
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

        // Red Mage core
        public const uint Dualcast = 1249;          // Enables instant cast
        public const uint VerfireReady = 1234;      // Verfire proc
        public const uint VerstoneReady = 1235;     // Verstone proc
        public const uint Acceleration = 1238;      // Guaranteed procs + Grand Impact
        public const uint Embolden = 1239;          // Party damage buff (self)
        public const uint EmboldenParty = 1297;     // Party damage buff (others)
        public const uint Manafication = 1971;      // Mana boost + damage buff
        public const uint MagickBarrier = 2707;     // Party mitigation

        // Finisher state tracking
        public const uint ThornedFlourish = 3876;   // Vice of Thorns ready
        public const uint GrandImpactReady = 3877;  // Grand Impact ready
        public const uint PrefulgenceReady = 3878;  // Prefulgence ready

        // Debuffs
        public const uint Addle = 1203;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Jolt spell for the player's level.
    /// </summary>
    public static ActionDefinition GetJoltSpell(byte level)
    {
        if (level >= Jolt3.MinLevel)
            return Jolt3;
        if (level >= Jolt2.MinLevel)
            return Jolt2;
        return Jolt;
    }

    /// <summary>
    /// Gets the Verthunder spell for the player's level.
    /// </summary>
    public static ActionDefinition GetVerthunderSpell(byte level)
    {
        if (level >= Verthunder3.MinLevel)
            return Verthunder3;
        return Verthunder;
    }

    /// <summary>
    /// Gets the Veraero spell for the player's level.
    /// </summary>
    public static ActionDefinition GetVeraeroSpell(byte level)
    {
        if (level >= Veraero3.MinLevel)
            return Veraero3;
        return Veraero;
    }

    /// <summary>
    /// Gets the appropriate long spell to balance mana.
    /// Returns Verthunder if need more Black, Veraero if need more White.
    /// </summary>
    public static ActionDefinition GetBalancedLongSpell(byte level, int blackMana, int whiteMana)
    {
        // If imbalanced by 30+, use the one that generates LOWER mana type
        var imbalance = blackMana - whiteMana;
        if (imbalance >= 25)
            return GetVeraeroSpell(level); // Need White
        if (imbalance <= -25)
            return GetVerthunderSpell(level); // Need Black

        // Otherwise, alternate or use the lower one
        if (blackMana <= whiteMana)
            return GetVerthunderSpell(level);
        return GetVeraeroSpell(level);
    }

    /// <summary>
    /// Gets the appropriate proc spell to use.
    /// Prioritizes expiring procs and mana balance.
    /// </summary>
    public static ActionDefinition? GetProcSpell(byte level, bool hasVerfire, bool hasVerstone,
        float verfireRemaining, float verstoneRemaining, int blackMana, int whiteMana)
    {
        if (!hasVerfire && !hasVerstone)
            return null;

        // Only have one proc
        if (hasVerfire && !hasVerstone && level >= Verfire.MinLevel)
            return Verfire;
        if (hasVerstone && !hasVerfire && level >= Verstone.MinLevel)
            return Verstone;

        // Both procs - use expiring one first
        if (verfireRemaining < verstoneRemaining && verfireRemaining < 5f && level >= Verfire.MinLevel)
            return Verfire;
        if (verstoneRemaining < verfireRemaining && verstoneRemaining < 5f && level >= Verstone.MinLevel)
            return Verstone;

        // Both procs available - use to balance mana
        var imbalance = blackMana - whiteMana;
        if (imbalance >= 0 && level >= Verstone.MinLevel)
            return Verstone; // Need White
        if (level >= Verfire.MinLevel)
            return Verfire; // Need Black

        return null;
    }

    /// <summary>
    /// Gets the finisher to use after Redoublement.
    /// Returns Verflare if Black <= White, Verholy otherwise.
    /// </summary>
    public static ActionDefinition GetFinisher(byte level, int blackMana, int whiteMana)
    {
        // Below Lv.70, only Verflare is available
        if (level < Verholy.MinLevel)
            return Verflare;

        // Use the finisher that generates the LOWER mana type
        if (blackMana <= whiteMana)
            return Verflare; // +21 Black
        return Verholy; // +21 White
    }

    /// <summary>
    /// Gets the AoE hardcast spell to balance mana.
    /// </summary>
    public static ActionDefinition GetAoeHardcast(byte level, int blackMana, int whiteMana)
    {
        if (level < Verthunder2.MinLevel)
            return GetJoltSpell(level); // Fallback

        var imbalance = blackMana - whiteMana;
        if (imbalance >= 0 && level >= Veraero2.MinLevel)
            return Veraero2; // Need White
        return Verthunder2; // Need Black
    }

    /// <summary>
    /// Checks if mana is within safe balance (within 30 difference).
    /// </summary>
    public static bool IsManaBalanced(int blackMana, int whiteMana)
    {
        var imbalance = System.Math.Abs(blackMana - whiteMana);
        return imbalance < 30;
    }

    #endregion
}
