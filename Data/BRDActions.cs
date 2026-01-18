using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Bard (BRD) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Calliope, the Greek muse of epic poetry.
/// </summary>
public static class BRDActions
{
    #region Single-Target GCDs

    /// <summary>
    /// Heavy Shot - Basic single target (Lv.1)
    /// 35% chance to grant Straight Shot Ready
    /// Replaced by Burst Shot at Lv.76
    /// </summary>
    public static readonly ActionDefinition HeavyShot = new()
    {
        ActionId = 97,
        Name = "Heavy Shot",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 160
    };

    /// <summary>
    /// Burst Shot - Enhanced basic single target (Lv.76)
    /// 35% chance to grant Straight Shot Ready
    /// Replaces Heavy Shot
    /// </summary>
    public static readonly ActionDefinition BurstShot = new()
    {
        ActionId = 16495,
        Name = "Burst Shot",
        MinLevel = 76,
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
    /// Straight Shot - Proc consumer (Lv.2)
    /// Requires Straight Shot Ready (Hawk's Eye) buff
    /// Replaced by Refulgent Arrow at Lv.70
    /// </summary>
    public static readonly ActionDefinition StraightShot = new()
    {
        ActionId = 98,
        Name = "Straight Shot",
        MinLevel = 2,
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
    /// Refulgent Arrow - Enhanced proc consumer (Lv.70)
    /// Requires Straight Shot Ready (Hawk's Eye) buff
    /// Replaces Straight Shot
    /// </summary>
    public static readonly ActionDefinition RefulgentArrow = new()
    {
        ActionId = 7409,
        Name = "Refulgent Arrow",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 280
    };

    #endregion

    #region DoT Actions

    /// <summary>
    /// Venomous Bite - Poison DoT (Lv.6)
    /// 45s duration, chance to grant Straight Shot Ready
    /// Replaced by Caustic Bite at Lv.64
    /// </summary>
    public static readonly ActionDefinition VenomousBite = new()
    {
        ActionId = 100,
        Name = "Venomous Bite",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.VenomousBite,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Caustic Bite - Enhanced poison DoT (Lv.64)
    /// 45s duration, chance to grant Straight Shot Ready
    /// Replaces Venomous Bite
    /// </summary>
    public static readonly ActionDefinition CausticBite = new()
    {
        ActionId = 7406,
        Name = "Caustic Bite",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 150,
        AppliedStatusId = StatusIds.CausticBite,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Windbite - Wind DoT (Lv.30)
    /// 45s duration, chance to grant Straight Shot Ready
    /// Replaced by Stormbite at Lv.64
    /// </summary>
    public static readonly ActionDefinition Windbite = new()
    {
        ActionId = 113,
        Name = "Windbite",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 60,
        AppliedStatusId = StatusIds.Windbite,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Stormbite - Enhanced wind DoT (Lv.64)
    /// 45s duration, chance to grant Straight Shot Ready
    /// Replaces Windbite
    /// </summary>
    public static readonly ActionDefinition Stormbite = new()
    {
        ActionId = 7407,
        Name = "Stormbite",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.Stormbite,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Iron Jaws - DoT refresh (Lv.56)
    /// Refreshes both DoTs, snapshots current buffs
    /// </summary>
    public static readonly ActionDefinition IronJaws = new()
    {
        ActionId = 3560,
        Name = "Iron Jaws",
        MinLevel = 56,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 100
    };

    #endregion

    #region Soul Voice GCDs

    /// <summary>
    /// Apex Arrow - Soul Voice spender (Lv.80)
    /// Uses 20-100 Soul Voice, damage scales with gauge
    /// Grants Blast Arrow Ready at 80+ Soul Voice
    /// </summary>
    public static readonly ActionDefinition ApexArrow = new()
    {
        ActionId = 16496,
        Name = "Apex Arrow",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 500 // Base potency at 100 gauge
    };

    /// <summary>
    /// Blast Arrow - Follow-up to Apex Arrow (Lv.86)
    /// Requires Blast Arrow Ready from 80+ Soul Voice Apex Arrow
    /// </summary>
    public static readonly ActionDefinition BlastArrow = new()
    {
        ActionId = 25784,
        Name = "Blast Arrow",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 600
    };

    #endregion

    #region Proc GCDs

    /// <summary>
    /// Resonant Arrow - Follow-up to Barrage (Lv.96)
    /// Requires Resonant Arrow Ready from Barrage
    /// </summary>
    public static readonly ActionDefinition ResonantArrow = new()
    {
        ActionId = 36976,
        Name = "Resonant Arrow",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Radiant Encore - Follow-up to Radiant Finale (Lv.100)
    /// Requires Radiant Encore Ready from Radiant Finale
    /// Damage scales with number of Coda used
    /// </summary>
    public static readonly ActionDefinition RadiantEncore = new()
    {
        ActionId = 36977,
        Name = "Radiant Encore",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 900 // At 3 Coda
    };

    #endregion

    #region AoE GCDs

    /// <summary>
    /// Quick Nock - AoE filler (Lv.18)
    /// 35% chance to grant Straight Shot Ready
    /// Replaced by Ladonsbite at Lv.82
    /// </summary>
    public static readonly ActionDefinition QuickNock = new()
    {
        ActionId = 106,
        Name = "Quick Nock",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 12f,
        Radius = 12f,
        MpCost = 0,
        DamagePotency = 110
    };

    /// <summary>
    /// Ladonsbite - Enhanced AoE filler (Lv.82)
    /// 35% chance to grant Straight Shot Ready
    /// Replaces Quick Nock
    /// </summary>
    public static readonly ActionDefinition Ladonsbite = new()
    {
        ActionId = 25783,
        Name = "Ladonsbite",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 12f,
        Radius = 12f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Shadowbite - AoE proc consumer (Lv.72)
    /// Requires Straight Shot Ready (Hawk's Eye) buff
    /// </summary>
    public static readonly ActionDefinition Shadowbite = new()
    {
        ActionId = 16494,
        Name = "Shadowbite",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 170
    };

    #endregion

    #region Songs (oGCD)

    /// <summary>
    /// Mage's Ballad - First song (Lv.30)
    /// +1% damage party buff, 45s duration
    /// Resets Bloodletter CD on proc
    /// </summary>
    public static readonly ActionDefinition MagesBallad = new()
    {
        ActionId = 114,
        Name = "Mage's Ballad",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.MagesBallad,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Army's Paeon - Second song (Lv.40)
    /// +3% direct hit party buff, 45s duration
    /// Grants Repertoire stacks for speed buff
    /// </summary>
    public static readonly ActionDefinition ArmysPaeon = new()
    {
        ActionId = 116,
        Name = "Army's Paeon",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.ArmysPaeon,
        AppliedStatusDuration = 45f
    };

    /// <summary>
    /// Wanderer's Minuet - Third song (Lv.52)
    /// +2% crit rate party buff, 45s duration
    /// Grants Pitch Perfect stacks
    /// </summary>
    public static readonly ActionDefinition WanderersMinuet = new()
    {
        ActionId = 3559,
        Name = "The Wanderer's Minuet",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.WanderersMinuet,
        AppliedStatusDuration = 45f
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Bloodletter - Charge-based oGCD (Lv.12)
    /// 3 charges, 15s recast per charge
    /// Replaced by Heartbreak Shot at Lv.92
    /// </summary>
    public static readonly ActionDefinition Bloodletter = new()
    {
        ActionId = 110,
        Name = "Bloodletter",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 130
    };

    /// <summary>
    /// Heartbreak Shot - Enhanced Bloodletter (Lv.92)
    /// 3 charges, 15s recast per charge
    /// Replaces Bloodletter
    /// </summary>
    public static readonly ActionDefinition HeartbreakShot = new()
    {
        ActionId = 36975,
        Name = "Heartbreak Shot",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 180
    };

    /// <summary>
    /// Rain of Death - AoE oGCD (Lv.45)
    /// 3 charges shared with Bloodletter
    /// </summary>
    public static readonly ActionDefinition RainOfDeath = new()
    {
        ActionId = 117,
        Name = "Rain of Death",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 25f,
        Radius = 8f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Empyreal Arrow - Guaranteed Repertoire proc (Lv.54)
    /// 15s cooldown, always grants Repertoire in song
    /// </summary>
    public static readonly ActionDefinition EmpyrealArrow = new()
    {
        ActionId = 3558,
        Name = "Empyreal Arrow",
        MinLevel = 54,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 15f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 260
    };

    /// <summary>
    /// Sidewinder - Burst damage oGCD (Lv.60)
    /// 60s cooldown, high potency
    /// </summary>
    public static readonly ActionDefinition Sidewinder = new()
    {
        ActionId = 3562,
        Name = "Sidewinder",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 400
    };

    /// <summary>
    /// Pitch Perfect - Wanderer's Minuet finisher (Lv.52)
    /// Uses Repertoire stacks (1-3), best at 3 stacks
    /// Only usable during Wanderer's Minuet
    /// </summary>
    public static readonly ActionDefinition PitchPerfect = new()
    {
        ActionId = 7404,
        Name = "Pitch Perfect",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 360 // At 3 stacks
    };

    #endregion

    #region Buff oGCDs

    /// <summary>
    /// Raging Strikes - Self damage buff (Lv.4)
    /// +15% damage for 20s, 120s cooldown
    /// </summary>
    public static readonly ActionDefinition RagingStrikes = new()
    {
        ActionId = 101,
        Name = "Raging Strikes",
        MinLevel = 4,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RagingStrikes,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Battle Voice - Party buff (Lv.50)
    /// +20% direct hit rate for 15s, 120s cooldown
    /// </summary>
    public static readonly ActionDefinition BattleVoice = new()
    {
        ActionId = 118,
        Name = "Battle Voice",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.BattleVoice,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Radiant Finale - Party buff (Lv.90)
    /// +2-6% damage based on Coda count, 110s cooldown
    /// Grants Radiant Encore Ready
    /// </summary>
    public static readonly ActionDefinition RadiantFinale = new()
    {
        ActionId = 25785,
        Name = "Radiant Finale",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 110f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RadiantFinale,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Barrage - Triple attack buff (Lv.38)
    /// Next single-target GCD hits 3 times
    /// Grants Resonant Arrow Ready at Lv.96
    /// </summary>
    public static readonly ActionDefinition Barrage = new()
    {
        ActionId = 107,
        Name = "Barrage",
        MinLevel = 38,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Barrage,
        AppliedStatusDuration = 10f
    };

    #endregion

    #region Utility oGCDs

    /// <summary>
    /// Troubadour - Party mitigation (Lv.62)
    /// -15% damage taken for 15s, 90s cooldown
    /// </summary>
    public static readonly ActionDefinition Troubadour = new()
    {
        ActionId = 7405,
        Name = "Troubadour",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Troubadour,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Nature's Minne - Party healing buff (Lv.66)
    /// +15% healing received for 15s, 120s cooldown
    /// </summary>
    public static readonly ActionDefinition NaturesMinne = new()
    {
        ActionId = 7408,
        Name = "Nature's Minne",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.NaturesMinne,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Warden's Paean - Cleanse/prevention (Lv.35)
    /// Removes one cleansable debuff or prevents next
    /// </summary>
    public static readonly ActionDefinition WardensPaean = new()
    {
        ActionId = 3561,
        Name = "The Warden's Paean",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 45f,
        Range = 30f,
        MpCost = 0
    };

    /// <summary>
    /// Repelling Shot - Backstep (Lv.15)
    /// Jump backwards 10y
    /// </summary>
    public static readonly ActionDefinition RepellingShot = new()
    {
        ActionId = 112,
        Name = "Repelling Shot",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        MpCost = 0
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
    /// Head Graze - Interrupt (Lv.24)
    /// </summary>
    public static readonly ActionDefinition HeadGraze = new()
    {
        ActionId = 7551,
        Name = "Head Graze",
        MinLevel = 24,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0
    };

    /// <summary>
    /// Peloton - Party movement speed (Lv.20)
    /// </summary>
    public static readonly ActionDefinition Peloton = new()
    {
        ActionId = 7557,
        Name = "Peloton",
        MinLevel = 20,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 5f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Peloton,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Self buffs
        public const uint StraightShotReady = 122;  // Also called Hawk's Eye
        public const uint RagingStrikes = 125;
        public const uint BattleVoice = 141;
        public const uint Barrage = 128;
        public const uint RadiantFinale = 2964;
        public const uint BlastArrowReady = 2692;
        public const uint ResonantArrowReady = 3862;
        public const uint RadiantEncoreReady = 3863;

        // Song buffs (on self)
        public const uint MagesBallad = 135;
        public const uint ArmysPaeon = 137;
        public const uint WanderersMinuet = 2009;

        // Target DoTs
        public const uint VenomousBite = 124;
        public const uint Windbite = 129;
        public const uint CausticBite = 1200;
        public const uint Stormbite = 1201;

        // Party buffs
        public const uint Troubadour = 1934;
        public const uint NaturesMinne = 1202;

        // Role buffs
        public const uint ArmsLength = 1209;
        public const uint Peloton = 1199;
    }

    #endregion

    #region Song Enum

    /// <summary>
    /// Bard song types from the game gauge.
    /// </summary>
    public enum Song : byte
    {
        None = 0,
        MagesBallad = 1,
        ArmysPaeon = 2,
        WanderersMinuet = 3
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best filler action for the player's level.
    /// </summary>
    public static ActionDefinition GetFiller(byte level)
    {
        if (level >= BurstShot.MinLevel)
            return BurstShot;
        return HeavyShot;
    }

    /// <summary>
    /// Gets the best proc action for the player's level.
    /// </summary>
    public static ActionDefinition GetProcAction(byte level)
    {
        if (level >= RefulgentArrow.MinLevel)
            return RefulgentArrow;
        return StraightShot;
    }

    /// <summary>
    /// Gets the best Caustic Bite action for the player's level.
    /// </summary>
    public static ActionDefinition GetCausticBite(byte level)
    {
        if (level >= CausticBite.MinLevel)
            return CausticBite;
        return VenomousBite;
    }

    /// <summary>
    /// Gets the best Stormbite action for the player's level.
    /// </summary>
    public static ActionDefinition GetStormbite(byte level)
    {
        if (level >= Stormbite.MinLevel)
            return Stormbite;
        return Windbite;
    }

    /// <summary>
    /// Gets the best AoE filler action for the player's level.
    /// </summary>
    public static ActionDefinition GetAoeFiller(byte level)
    {
        if (level >= Ladonsbite.MinLevel)
            return Ladonsbite;
        return QuickNock;
    }

    /// <summary>
    /// Gets the best Bloodletter action for the player's level.
    /// </summary>
    public static ActionDefinition GetBloodletter(byte level)
    {
        if (level >= HeartbreakShot.MinLevel)
            return HeartbreakShot;
        return Bloodletter;
    }

    /// <summary>
    /// Gets the Caustic Bite status ID for the player's level.
    /// </summary>
    public static uint GetCausticBiteStatusId(byte level)
    {
        if (level >= CausticBite.MinLevel)
            return StatusIds.CausticBite;
        return StatusIds.VenomousBite;
    }

    /// <summary>
    /// Gets the Stormbite status ID for the player's level.
    /// </summary>
    public static uint GetStormbiteStatusId(byte level)
    {
        if (level >= Stormbite.MinLevel)
            return StatusIds.Stormbite;
        return StatusIds.Windbite;
    }

    #endregion
}
