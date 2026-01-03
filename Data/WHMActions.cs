using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// White Mage (WHM) and Conjurer (CNJ) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// </summary>
public static class WHMActions
{
    #region GCD Heals

    public static readonly ActionDefinition Cure = new()
    {
        ActionId = 120,
        Name = "Cure",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 500
    };

    public static readonly ActionDefinition CureII = new()
    {
        ActionId = 135,
        Name = "Cure II",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 1000,
        HealPotency = 800
    };

    public static readonly ActionDefinition CureIII = new()
    {
        ActionId = 131,
        Name = "Cure III",
        MinLevel = 40,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly, // Targets ally, AoE around them
        EffectTypes = ActionEffectType.Heal,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 30f,
        Radius = 10f,
        MpCost = 1500,
        HealPotency = 600
    };

    public static readonly ActionDefinition Regen = new()
    {
        ActionId = 137,
        Name = "Regen",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.HoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 250, // Potency per tick
        AppliedStatusId = 158, // Regen status
        AppliedStatusDuration = 18f
    };

    public static readonly ActionDefinition Medica = new()
    {
        ActionId = 124,
        Name = "Medica",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 20f,
        MpCost = 1000,
        HealPotency = 400
    };

    public static readonly ActionDefinition MedicaII = new()
    {
        ActionId = 133,
        Name = "Medica II",
        MinLevel = 50,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 20f,
        MpCost = 1300,
        HealPotency = 250,
        AppliedStatusId = 150, // Medica II regen
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition MedicaIII = new()
    {
        ActionId = 37010,
        Name = "Medica III",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.HoT,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 20f,
        MpCost = 1300,
        HealPotency = 300,
        AppliedStatusId = 3986, // Medica III regen
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition AfflatusSolace = new()
    {
        ActionId = 16531,
        Name = "Afflatus Solace",
        MinLevel = 52,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 800
    };

    public static readonly ActionDefinition AfflatusRapture = new()
    {
        ActionId = 16534,
        Name = "Afflatus Rapture",
        MinLevel = 76,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 400
    };

    #endregion

    #region GCD Damage

    public static readonly ActionDefinition Stone = new()
    {
        ActionId = 119,
        Name = "Stone",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 140
    };

    public static readonly ActionDefinition StoneII = new()
    {
        ActionId = 127,
        Name = "Stone II",
        MinLevel = 18,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 190
    };

    public static readonly ActionDefinition StoneIII = new()
    {
        ActionId = 3568,
        Name = "Stone III",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 220
    };

    public static readonly ActionDefinition StoneIV = new()
    {
        ActionId = 7431,
        Name = "Stone IV",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 260
    };

    public static readonly ActionDefinition Glare = new()
    {
        ActionId = 16533,
        Name = "Glare",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 290
    };

    public static readonly ActionDefinition GlareIII = new()
    {
        ActionId = 25859,
        Name = "Glare III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 310
    };

    /// <summary>
    /// Glare IV - Targeted AoE damage spell. Requires Sacred Sight (from Presence of Mind).
    /// Deals 350 potency to target and all enemies within 5y radius.
    /// Sacred Sight makes this spell instant cast.
    /// </summary>
    public static readonly ActionDefinition GlareIV = new()
    {
        ActionId = 37009,
        Name = "Glare IV",
        MinLevel = 92,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Targets enemy, AoE around them
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f, // Instant when Sacred Sight active
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // AoE radius around target
        MpCost = 400,
        DamagePotency = 350
    };

    public static readonly ActionDefinition Holy = new()
    {
        ActionId = 139,
        Name = "Holy",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self, // Self-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f, // Reduced from 2.5s in Patch 7.1
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 8f,
        MpCost = 400,
        DamagePotency = 140
    };

    public static readonly ActionDefinition HolyIII = new()
    {
        ActionId = 25860,
        Name = "Holy III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f, // Reduced from 2.5s in Patch 7.1
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 8f,
        MpCost = 400,
        DamagePotency = 150
    };

    public static readonly ActionDefinition AfflatusMisery = new()
    {
        ActionId = 16535,
        Name = "Afflatus Misery",
        MinLevel = 74,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // AoE around target
        MpCost = 0,
        DamagePotency = 1240
    };

    #endregion

    #region GCD DoTs

    public static readonly ActionDefinition Aero = new()
    {
        ActionId = 121,
        Name = "Aero",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 50,
        AppliedStatusId = 143, // Aero DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition AeroII = new()
    {
        ActionId = 132,
        Name = "Aero II",
        MinLevel = 46,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 60,
        AppliedStatusId = 144, // Aero II DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition Dia = new()
    {
        ActionId = 16532,
        Name = "Dia",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 65,
        AppliedStatusId = 1871, // Dia DoT
        AppliedStatusDuration = 30f
    };

    #endregion

    #region oGCD Heals

    public static readonly ActionDefinition Benediction = new()
    {
        ActionId = 140,
        Name = "Benediction",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 180f, // 3 minute cooldown
        Range = 30f,
        MpCost = 0,
        HealPotency = 0 // Special: heals to full HP
    };

    public static readonly ActionDefinition Tetragrammaton = new()
    {
        ActionId = 3570,
        Name = "Tetragrammaton",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 700
    };

    public static readonly ActionDefinition DivineBenison = new()
    {
        ActionId = 7432,
        Name = "Divine Benison",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 30f,
        MpCost = 0,
        ShieldPotency = 500,
        AppliedStatusId = 1218,
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition Aquaveil = new()
    {
        ActionId = 25861,
        Name = "Aquaveil",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 2708,
        AppliedStatusDuration = 8f
    };

    public static readonly ActionDefinition Asylum = new()
    {
        ActionId = 3569,
        Name = "Asylum",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 30f,
        Radius = 10f,
        MpCost = 0,
        HealPotency = 100, // Per tick
        AppliedStatusDuration = 24f
    };

    public static readonly ActionDefinition Assize = new()
    {
        ActionId = 3571,
        Name = "Assize",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self, // Self-centered AoE
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal | ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 400,
        DamagePotency = 400
    };

    public static readonly ActionDefinition PlenaryIndulgence = new()
    {
        ActionId = 7433,
        Name = "Plenary Indulgence",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        AppliedStatusId = 1219,
        AppliedStatusDuration = 10f
    };

    public static readonly ActionDefinition Temperance = new()
    {
        ActionId = 16536,
        Name = "Temperance",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = 1872,
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition LiturgyOfTheBell = new()
    {
        ActionId = 25862,
        Name = "Liturgy of the Bell",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 180f,
        Range = 30f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 400,
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition DivineCaress = new()
    {
        ActionId = 37011,
        Name = "Divine Caress",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Shield | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 1f, // Very short CD, triggered after Temperance expires
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        ShieldPotency = 400,
        HealPotency = 100, // HoT component
        AppliedStatusId = 3987,
        AppliedStatusDuration = 15f
    };

    #endregion

    #region oGCD Buffs

    public static readonly ActionDefinition PresenceOfMind = new()
    {
        ActionId = 136,
        Name = "Presence of Mind",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 157,
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition ThinAir = new()
    {
        ActionId = 7430,
        Name = "Thin Air",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // 60s per charge, 2 charges max
        MpCost = 0,
        AppliedStatusId = 1217,
        AppliedStatusDuration = 12f
    };

    #endregion

    #region Movement

    public static readonly ActionDefinition AetherialShift = new()
    {
        ActionId = 37008,
        Name = "Aetherial Shift",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 15f, // Dash distance
        MpCost = 0
    };

    #endregion

    #region Role Actions

    public static readonly ActionDefinition Swiftcast = new()
    {
        ActionId = 7561,
        Name = "Swiftcast",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = 167,
        AppliedStatusDuration = 10f
    };

    public static readonly ActionDefinition LucidDreaming = new()
    {
        ActionId = 7562,
        Name = "Lucid Dreaming",
        MinLevel = 14,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = 1204,
        AppliedStatusDuration = 21f
    };

    public static readonly ActionDefinition Surecast = new()
    {
        ActionId = 7559,
        Name = "Surecast",
        MinLevel = 44,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = 160,
        AppliedStatusDuration = 6f
    };

    public static readonly ActionDefinition Rescue = new()
    {
        ActionId = 7571,
        Name = "Rescue",
        MinLevel = 48,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.None, // Movement ability
        CastTime = 0f,
        RecastTime = 120f,
        Range = 30f,
        MpCost = 0
    };

    public static readonly ActionDefinition Esuna = new()
    {
        ActionId = 7568,
        Name = "Esuna",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Cleanse,
        CastTime = 1.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400
    };

    public static readonly ActionDefinition Raise = new()
    {
        ActionId = 125,
        Name = "Raise",
        MinLevel = 12,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Raise,
        CastTime = 8.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 2400
    };

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// All WHM GCD damage spells in level order (highest first).
    /// Note: GlareIV is excluded because it requires Sacred Sight stacks (from Presence of Mind).
    /// GlareIV is handled separately as a proc-based spell in the rotation.
    /// </summary>
    public static readonly ActionDefinition[] DamageGcds =
    {
        GlareIII, Glare, StoneIV, StoneIII, StoneII, Stone
    };

    /// <summary>
    /// All WHM GCD DoT spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DotGcds =
    {
        Dia, AeroII, Aero
    };

    /// <summary>
    /// All WHM GCD single-target heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] SingleHealGcds =
    {
        AfflatusSolace, CureII, Cure
    };

    /// <summary>
    /// All WHM GCD AoE heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEHealGcds =
    {
        AfflatusRapture, MedicaIII, MedicaII, Medica
    };

    /// <summary>
    /// All WHM oGCD single-target heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] SingleHealOgcds =
    {
        Tetragrammaton, Benediction
    };

    /// <summary>
    /// All WHM oGCD AoE heals/utility in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEHealOgcds =
    {
        LiturgyOfTheBell, Asylum, Assize
    };

    /// <summary>
    /// Gets the appropriate damage GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetDamageGcdForLevel(byte level)
    {
        foreach (var action in DamageGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Stone;
    }

    /// <summary>
    /// Gets the appropriate DoT for the player's level.
    /// </summary>
    public static ActionDefinition GetDotForLevel(byte level)
    {
        foreach (var action in DotGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Aero;
    }

    /// <summary>
    /// Gets the appropriate single-target heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetSingleHealGcdForLevel(byte level)
    {
        foreach (var action in SingleHealGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Cure;
    }

    /// <summary>
    /// Gets the appropriate AoE heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetAoEHealGcdForLevel(byte level)
    {
        foreach (var action in AoEHealGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return Medica;
    }

    /// <summary>
    /// All WHM GCD AoE damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEDamageGcds =
    {
        HolyIII, Holy
    };

    /// <summary>
    /// Gets the appropriate AoE damage GCD for the player's level.
    /// Returns null if player level is below Holy (level 45).
    /// </summary>
    public static ActionDefinition? GetAoEDamageGcdForLevel(byte level)
    {
        foreach (var action in AoEDamageGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return null; // No AoE damage available below level 45
    }

    #endregion
}
