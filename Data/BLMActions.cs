using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Black Mage (BLM) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Hecate, the Greek goddess of magic and witchcraft.
/// </summary>
public static class BLMActions
{
    #region Fire Phase GCDs

    /// <summary>
    /// Fire - Basic fire spell (Lv.2)
    /// Grants Astral Fire or removes Umbral Ice
    /// </summary>
    public static readonly ActionDefinition Fire = new()
    {
        ActionId = 141,
        Name = "Fire",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800,
        DamagePotency = 180
    };

    /// <summary>
    /// Fire III - Fire phase entry spell (Lv.35)
    /// Grants 3 stacks of Astral Fire
    /// </summary>
    public static readonly ActionDefinition Fire3 = new()
    {
        ActionId = 152,
        Name = "Fire III",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 2000,
        DamagePotency = 280
    };

    /// <summary>
    /// Fire IV - Main damage spell in Astral Fire (Lv.60)
    /// Does NOT refresh Astral Fire timer
    /// </summary>
    public static readonly ActionDefinition Fire4 = new()
    {
        ActionId = 3577,
        Name = "Fire IV",
        MinLevel = 60,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.8f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800,
        DamagePotency = 640
    };

    /// <summary>
    /// Despair - Fire phase finisher (Lv.72)
    /// Uses all remaining MP, grants Astral Fire III
    /// </summary>
    public static readonly ActionDefinition Despair = new()
    {
        ActionId = 16505,
        Name = "Despair",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.0f, // Patch 7.2 reduced from 3.0s to 2.0s; instant at Lv.100 with Enhanced Astral Fire trait.
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800, // Uses all remaining MP
        DamagePotency = 380
    };

    /// <summary>
    /// High Fire II - Enhanced AoE fire (Lv.82)
    /// Upgrades Fire II
    /// </summary>
    public static readonly ActionDefinition HighFire2 = new()
    {
        ActionId = 25794,
        Name = "High Fire II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 1500,
        DamagePotency = 140
    };

    /// <summary>
    /// Fire II - AoE fire spell (Lv.18)
    /// </summary>
    public static readonly ActionDefinition Fire2 = new()
    {
        ActionId = 147,
        Name = "Fire II",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 1500,
        DamagePotency = 80
    };

    /// <summary>
    /// Flare - Massive AoE fire (Lv.50)
    /// Uses all remaining MP
    /// </summary>
    public static readonly ActionDefinition Flare = new()
    {
        ActionId = 162,
        Name = "Flare",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 4.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 800, // Uses all remaining MP
        DamagePotency = 240
    };

    /// <summary>
    /// Flare Star - Ultimate fire finisher (Lv.100)
    /// Requires 6 Astral Soul stacks
    /// </summary>
    public static readonly ActionDefinition FlareStar = new()
    {
        ActionId = 36989,
        Name = "Flare Star",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 400
    };

    #endregion

    #region Ice Phase GCDs

    /// <summary>
    /// Blizzard - Basic ice spell (Lv.1)
    /// Grants Umbral Ice or removes Astral Fire
    /// </summary>
    public static readonly ActionDefinition Blizzard = new()
    {
        ActionId = 142,
        Name = "Blizzard",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 180
    };

    /// <summary>
    /// Blizzard III - Ice phase entry spell (Lv.35)
    /// Grants 3 stacks of Umbral Ice
    /// </summary>
    public static readonly ActionDefinition Blizzard3 = new()
    {
        ActionId = 154,
        Name = "Blizzard III",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800,
        DamagePotency = 280
    };

    /// <summary>
    /// Blizzard IV - Grants Umbral Hearts (Lv.58)
    /// Requires Umbral Ice III
    /// </summary>
    public static readonly ActionDefinition Blizzard4 = new()
    {
        ActionId = 3576,
        Name = "Blizzard IV",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800,
        DamagePotency = 320
    };

    /// <summary>
    /// High Blizzard II - Enhanced AoE ice (Lv.82)
    /// Upgrades Blizzard II, grants Umbral Hearts
    /// </summary>
    public static readonly ActionDefinition HighBlizzard2 = new()
    {
        ActionId = 25795,
        Name = "High Blizzard II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 800,
        DamagePotency = 140
    };

    /// <summary>
    /// Blizzard II - AoE ice spell (Lv.12)
    /// </summary>
    public static readonly ActionDefinition Blizzard2 = new()
    {
        ActionId = 25793,
        Name = "Blizzard II",
        MinLevel = 12,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 800,
        DamagePotency = 80
    };

    /// <summary>
    /// Freeze - AoE ice with Umbral Hearts (Lv.40)
    /// Grants Umbral Hearts in Umbral Ice
    /// </summary>
    public static readonly ActionDefinition Freeze = new()
    {
        ActionId = 159,
        Name = "Freeze",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.8f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 1000,
        DamagePotency = 120
    };

    /// <summary>
    /// Umbral Soul - MP and element refresh out of combat (Lv.76)
    /// Only usable in Umbral Ice, outside of combat
    /// </summary>
    public static readonly ActionDefinition UmbralSoul = new()
    {
        ActionId = 16506,
        Name = "Umbral Soul",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        MpCost = 0
    };

    #endregion

    #region Thunder GCDs

    /// <summary>
    /// Thunder - Single target DoT (Lv.6)
    /// </summary>
    public static readonly ActionDefinition Thunder = new()
    {
        ActionId = 144,
        Name = "Thunder",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 30,
        AppliedStatusId = StatusIds.Thunder,
        AppliedStatusDuration = 21f
    };

    /// <summary>
    /// Thunder III - Enhanced single target DoT (Lv.45)
    /// </summary>
    public static readonly ActionDefinition Thunder3 = new()
    {
        ActionId = 153,
        Name = "Thunder III",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 50,
        AppliedStatusId = StatusIds.Thunder3,
        AppliedStatusDuration = 27f
    };

    /// <summary>
    /// High Thunder - Lv.92 upgrade of Thunder III (Lv.92)
    /// </summary>
    public static readonly ActionDefinition HighThunder = new()
    {
        ActionId = 36986,
        Name = "High Thunder",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f, // Instant with Thunderhead
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 150,
        AppliedStatusId = StatusIds.HighThunder,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Thunder II - AoE DoT (Lv.26)
    /// </summary>
    public static readonly ActionDefinition Thunder2 = new()
    {
        ActionId = 7447,
        Name = "Thunder II",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 30,
        AppliedStatusId = StatusIds.Thunder2,
        AppliedStatusDuration = 18f
    };

    /// <summary>
    /// Thunder IV - Enhanced AoE DoT (Lv.64)
    /// </summary>
    public static readonly ActionDefinition Thunder4 = new()
    {
        ActionId = 7420,
        Name = "Thunder IV",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 50,
        AppliedStatusId = StatusIds.Thunder4,
        AppliedStatusDuration = 18f
    };

    /// <summary>
    /// High Thunder II - Lv.92 upgrade of Thunder IV (Lv.92)
    /// </summary>
    public static readonly ActionDefinition HighThunder2 = new()
    {
        ActionId = 36987,
        Name = "High Thunder II",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f, // Instant with Thunderhead
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100,
        AppliedStatusId = StatusIds.HighThunder2,
        AppliedStatusDuration = 24f
    };

    #endregion

    #region Polyglot GCDs

    /// <summary>
    /// Xenoglossy - Powerful instant single target (Lv.80)
    /// Consumes 1 Polyglot stack
    /// </summary>
    public static readonly ActionDefinition Xenoglossy = new()
    {
        ActionId = 16507,
        Name = "Xenoglossy",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 880
    };

    /// <summary>
    /// Foul - AoE Polyglot spender (Lv.70)
    /// Consumes 1 Polyglot stack
    /// </summary>
    public static readonly ActionDefinition Foul = new()
    {
        ActionId = 7422,
        Name = "Foul",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    #endregion

    #region Special GCDs

    /// <summary>
    /// Paradox - Element timer refresh (Lv.90)
    /// Instant in Umbral Ice, refreshes element timer
    /// </summary>
    public static readonly ActionDefinition Paradox = new()
    {
        ActionId = 25797,
        Name = "Paradox",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.5f, // Instant in UI, 2.5s in AF
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 1600,
        DamagePotency = 520
    };

    /// <summary>
    /// Transpose - Swap element stacks (Lv.4)
    /// Switches between Astral Fire and Umbral Ice
    /// </summary>
    public static readonly ActionDefinition Transpose = new()
    {
        ActionId = 149,
        Name = "Transpose",
        MinLevel = 4,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 5f,
        MpCost = 0
    };

    /// <summary>
    /// Scathe - Weak instant damage (Lv.15)
    /// Emergency movement filler
    /// </summary>
    public static readonly ActionDefinition Scathe = new()
    {
        ActionId = 156,
        Name = "Scathe",
        MinLevel = 15,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 800,
        DamagePotency = 100
    };

    #endregion

    #region oGCDs

    /// <summary>
    /// Triplecast - Make next 3 spells instant (Lv.66)
    /// 2 charges at Lv.86
    /// </summary>
    public static readonly ActionDefinition Triplecast = new()
    {
        ActionId = 7421,
        Name = "Triplecast",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Triplecast,
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Manafont - Restore MP and reset Fire cooldowns (Lv.30)
    /// </summary>
    public static readonly ActionDefinition Manafont = new()
    {
        ActionId = 158,
        Name = "Manafont",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
    };

    /// <summary>
    /// Amplifier - Gain 1 Polyglot stack (Lv.86)
    /// </summary>
    public static readonly ActionDefinition Amplifier = new()
    {
        ActionId = 25796,
        Name = "Amplifier",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
    };

    /// <summary>
    /// Ley Lines - Buff zone for faster casting (Lv.52)
    /// </summary>
    public static readonly ActionDefinition LeyLines = new()
    {
        ActionId = 3573,
        Name = "Ley Lines",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.LeyLines,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Between the Lines - Teleport to Ley Lines (Lv.62)
    /// </summary>
    public static readonly ActionDefinition BetweenTheLines = new()
    {
        ActionId = 7419,
        Name = "Between the Lines",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 3f,
        MpCost = 0
    };

    /// <summary>
    /// Retrace - Relocate Ley Lines to self (Lv.96)
    /// </summary>
    public static readonly ActionDefinition Retrace = new()
    {
        ActionId = 36988,
        Name = "Retrace",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 3f,
        MpCost = 0
    };

    /// <summary>
    /// Manaward - Self shield (Lv.30)
    /// </summary>
    public static readonly ActionDefinition Manaward = new()
    {
        ActionId = 157,
        Name = "Manaward",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.Manaward,
        AppliedStatusDuration = 20f
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Element states (tracked via gauge, not status)
        public const uint AstralFire = 0; // Not a real status - use gauge
        public const uint UmbralIce = 0;  // Not a real status - use gauge

        // Procs
        public const uint Firestarter = 165;   // Instant Fire III proc
        public const uint Thunderhead = 3870;  // Instant Thunder proc (Lv.92+)

        // Buffs
        public const uint LeyLines = 737;      // Circle of Power (speed buff)
        public const uint Triplecast = 1211;   // Instant cast stacks
        public const uint Sharpcast = 867;     // Guaranteed proc (removed in DT)
        public const uint Manaward = 168;      // Shield
        public const uint Swiftcast = 167;     // Next spell instant
        public const uint Surecast = 160;      // Knockback immunity
        public const uint LucidDreaming = 1204;

        // Thunder DoTs
        public const uint Thunder = 161;       // Thunder I DoT
        public const uint Thunder3 = 163;      // Thunder III DoT
        public const uint HighThunder = 3871;  // High Thunder DoT
        public const uint Thunder2 = 162;      // Thunder II DoT (AoE)
        public const uint Thunder4 = 1210;     // Thunder IV DoT (AoE)
        public const uint HighThunder2 = 3872; // High Thunder II DoT

        // Debuffs
        public const uint Addle = 1203;
        public const uint Sleep = 3;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best Fire spell for the player's level.
    /// </summary>
    public static ActionDefinition GetFireSpell(byte level, bool inAstralFire)
    {
        // If in Astral Fire and have Fire IV, use it
        if (inAstralFire && level >= Fire4.MinLevel)
            return Fire4;

        if (level >= Fire3.MinLevel)
            return Fire3;

        return Fire;
    }

    /// <summary>
    /// Gets the best Blizzard spell for transitioning to Ice phase.
    /// </summary>
    public static ActionDefinition GetIceTransition(byte level)
    {
        if (level >= Blizzard3.MinLevel)
            return Blizzard3;
        return Blizzard;
    }

    /// <summary>
    /// Gets the best Fire transition spell for entering Fire phase.
    /// </summary>
    public static ActionDefinition GetFireTransition(byte level)
    {
        if (level >= Fire3.MinLevel)
            return Fire3;
        return Fire;
    }

    /// <summary>
    /// Gets the best Thunder spell for the player's level (single target).
    /// </summary>
    public static ActionDefinition GetThunderST(byte level)
    {
        if (level >= HighThunder.MinLevel)
            return HighThunder;
        if (level >= Thunder3.MinLevel)
            return Thunder3;
        return Thunder;
    }

    /// <summary>
    /// Gets the best Thunder spell for AoE.
    /// </summary>
    public static ActionDefinition GetThunderAoe(byte level)
    {
        if (level >= HighThunder2.MinLevel)
            return HighThunder2;
        if (level >= Thunder4.MinLevel)
            return Thunder4;
        if (level >= Thunder2.MinLevel)
            return Thunder2;
        return Thunder; // Fallback to single target
    }

    /// <summary>
    /// Gets the best AoE Fire action for the player's level.
    /// </summary>
    public static ActionDefinition GetFireAoe(byte level)
    {
        if (level >= HighFire2.MinLevel)
            return HighFire2;
        if (level >= Fire2.MinLevel)
            return Fire2;
        return Fire; // Fallback
    }

    /// <summary>
    /// Gets the best AoE Ice action for the player's level.
    /// </summary>
    public static ActionDefinition GetIceAoe(byte level)
    {
        if (level >= HighBlizzard2.MinLevel)
            return HighBlizzard2;
        if (level >= Freeze.MinLevel)
            return Freeze;
        if (level >= Blizzard2.MinLevel)
            return Blizzard2;
        return Blizzard; // Fallback
    }

    /// <summary>
    /// Gets the Polyglot spender action based on situation.
    /// </summary>
    public static ActionDefinition GetPolyglotSpender(byte level, bool useAoe)
    {
        if (useAoe && level >= Foul.MinLevel)
            return Foul;
        if (level >= Xenoglossy.MinLevel)
            return Xenoglossy;
        if (level >= Foul.MinLevel)
            return Foul;
        return Scathe; // Shouldn't happen - no Polyglot before Foul
    }

    /// <summary>
    /// Gets the best instant movement option available.
    /// Priority: Xenoglossy > Paradox (UI3) > Scathe
    /// </summary>
    public static ActionDefinition GetMovementFiller(byte level, bool hasPolyglot, bool inUmbralIce3)
    {
        if (hasPolyglot && level >= Xenoglossy.MinLevel)
            return Xenoglossy;

        if (inUmbralIce3 && level >= Paradox.MinLevel)
            return Paradox;

        return Scathe;
    }

    #endregion
}
