using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Scholar (SCH) and Arcanist (ACN) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Athena, the goddess of wisdom and strategy.
/// </summary>
public static class SCHActions
{
    #region GCD Heals

    public static readonly ActionDefinition Physick = new()
    {
        ActionId = 190,
        Name = "Physick",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 450
    };

    public static readonly ActionDefinition Adloquium = new()
    {
        ActionId = 185,
        Name = "Adloquium",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 1000,
        HealPotency = 300,
        ShieldPotency = 540, // 180% of heal potency
        AppliedStatusId = 297, // Galvanize
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Manifestation - Upgraded Adloquium during Seraphism.
    /// 360 potency heal + shield.
    /// </summary>
    public static readonly ActionDefinition Manifestation = new()
    {
        ActionId = 37015,
        Name = "Manifestation",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f, // Instant during Seraphism
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 1000,
        HealPotency = 360,
        ShieldPotency = 648, // 180% of heal potency
        AppliedStatusId = 297, // Galvanize
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition Succor = new()
    {
        ActionId = 186,
        Name = "Succor",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 1300,
        HealPotency = 200,
        ShieldPotency = 320, // 160% of heal potency
        AppliedStatusId = 297, // Galvanize
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Concitation - Upgraded Succor at level 96.
    /// Same effect but better scaling.
    /// </summary>
    public static readonly ActionDefinition Concitation = new()
    {
        ActionId = 37013,
        Name = "Concitation",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 1300,
        HealPotency = 200,
        ShieldPotency = 320, // 160% of heal potency
        AppliedStatusId = 297, // Galvanize
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Accession - Upgraded Concitation during Seraphism.
    /// 240 potency heal + shield, instant cast.
    /// </summary>
    public static readonly ActionDefinition Accession = new()
    {
        ActionId = 37016,
        Name = "Accession",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f, // Instant during Seraphism
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 1300,
        HealPotency = 240,
        ShieldPotency = 384, // 160% of heal potency
        AppliedStatusId = 297, // Galvanize
        AppliedStatusDuration = 30f
    };

    #endregion

    #region GCD Damage

    public static readonly ActionDefinition Ruin = new()
    {
        ActionId = 17869,
        Name = "Ruin",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 150
    };

    public static readonly ActionDefinition RuinII = new()
    {
        ActionId = 17870,
        Name = "Ruin II",
        MinLevel = 38,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 220
    };

    public static readonly ActionDefinition Broil = new()
    {
        ActionId = 3584,
        Name = "Broil",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 220
    };

    public static readonly ActionDefinition BroilII = new()
    {
        ActionId = 7435,
        Name = "Broil II",
        MinLevel = 64,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 240
    };

    public static readonly ActionDefinition BroilIII = new()
    {
        ActionId = 16541,
        Name = "Broil III",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 255
    };

    public static readonly ActionDefinition BroilIV = new()
    {
        ActionId = 25865,
        Name = "Broil IV",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 320
    };

    public static readonly ActionDefinition ArtOfWar = new()
    {
        ActionId = 16539,
        Name = "Art of War",
        MinLevel = 46,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self, // Self-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 165
    };

    public static readonly ActionDefinition ArtOfWarII = new()
    {
        ActionId = 25866,
        Name = "Art of War II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 180
    };

    #endregion

    #region GCD DoTs

    public static readonly ActionDefinition Bio = new()
    {
        ActionId = 17864,
        Name = "Bio",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 0, // No initial damage
        AppliedStatusId = 179, // Bio DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition BioII = new()
    {
        ActionId = 17865,
        Name = "Bio II",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 0,
        AppliedStatusId = 189, // Bio II DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition Biolysis = new()
    {
        ActionId = 16540,
        Name = "Biolysis",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 0,
        AppliedStatusId = 1895, // Biolysis DoT
        AppliedStatusDuration = 30f
    };

    #endregion

    #region oGCD Heals - Aetherflow

    public static readonly ActionDefinition Lustrate = new()
    {
        ActionId = 189,
        Name = "Lustrate",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 1f, // Aetherflow abilities share 1s recast
        Range = 30f,
        MpCost = 0,
        HealPotency = 600
    };

    public static readonly ActionDefinition Indomitability = new()
    {
        ActionId = 3583,
        Name = "Indomitability",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 400
    };

    public static readonly ActionDefinition Excogitation = new()
    {
        ActionId = 7434,
        Name = "Excogitation",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 45f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 800,
        AppliedStatusId = 1220, // Excogitation buff
        AppliedStatusDuration = 45f
    };

    public static readonly ActionDefinition SacredSoil = new()
    {
        ActionId = 188,
        Name = "Sacred Soil",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.GroundAoE,
        EffectTypes = ActionEffectType.HoT | ActionEffectType.Buff, // Damage reduction + regen at 78
        CastTime = 0f,
        RecastTime = 30f,
        Range = 30f,
        Radius = 10f,
        MpCost = 0,
        HealPotency = 100, // Per tick (at level 78+)
        AppliedStatusDuration = 15f
    };

    #endregion

    #region oGCD Heals - Free

    public static readonly ActionDefinition Protraction = new()
    {
        ActionId = 25867,
        Name = "Protraction",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 2710, // Protraction buff
        AppliedStatusDuration = 10f
    };

    #endregion

    #region oGCD Utility

    public static readonly ActionDefinition Aetherflow = new()
    {
        ActionId = 166,
        Name = "Aetherflow",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.MpRestore,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    public static readonly ActionDefinition EnergyDrain = new()
    {
        ActionId = 167,
        Name = "Energy Drain",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f, // Aetherflow abilities share 1s recast
        Range = 25f,
        MpCost = 0,
        DamagePotency = 100
    };

    public static readonly ActionDefinition Recitation = new()
    {
        ActionId = 16542,
        Name = "Recitation",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = 1896, // Recitation buff
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition EmergencyTactics = new()
    {
        ActionId = 3586,
        Name = "Emergency Tactics",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 15f,
        MpCost = 0,
        AppliedStatusId = 792, // Emergency Tactics buff
        AppliedStatusDuration = 15f
    };

    public static readonly ActionDefinition DeploymentTactics = new()
    {
        ActionId = 3585,
        Name = "Deployment Tactics",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 30f,
        Radius = 15f,
        MpCost = 0
    };

    public static readonly ActionDefinition Dissipation = new()
    {
        ActionId = 3587,
        Name = "Dissipation",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 180f,
        MpCost = 0,
        AppliedStatusId = 791, // Dissipation buff (+20% GCD heal potency)
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition ChainStratagem = new()
    {
        ActionId = 7436,
        Name = "Chain Stratagem",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 25f,
        MpCost = 0,
        AppliedStatusId = 1221, // Chain Stratagem debuff (+10% crit)
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition Expedient = new()
    {
        ActionId = 25868,
        Name = "Expedient",
        MinLevel = 90,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = 2711, // Expedience (speed) + Desperate Measures (mitigation)
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Baneful Impaction - AoE DoT attack from Chain Stratagem.
    /// Only available when Impact Imminent buff is active.
    /// </summary>
    public static readonly ActionDefinition BanefulImpaction = new()
    {
        ActionId = 37012,
        Name = "Baneful Impaction",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage | ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 140,
        AppliedStatusDuration = 15f // DoT duration
    };

    #endregion

    #region Fairy Abilities

    public static readonly ActionDefinition SummonEos = new()
    {
        ActionId = 17215,
        Name = "Summon Eos",
        MinLevel = 4,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        MpCost = 200
    };

    public static readonly ActionDefinition WhisperingDawn = new()
    {
        ActionId = 16537,
        Name = "Whispering Dawn",
        MinLevel = 20,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self, // Fairy-centered AoE
        EffectTypes = ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 80, // Per tick
        AppliedStatusDuration = 21f
    };

    public static readonly ActionDefinition FeyIllumination = new()
    {
        ActionId = 16538,
        Name = "Fey Illumination",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        AppliedStatusId = 317, // Fey Illumination buff
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition FeyBlessing = new()
    {
        ActionId = 16543,
        Name = "Fey Blessing",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self, // Fairy-centered AoE
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 350
    };

    public static readonly ActionDefinition Aetherpact = new()
    {
        ActionId = 7437,
        Name = "Aetherpact",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 3f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 480, // Per tick (Fey Union)
        AppliedStatusId = 1223 // Fey Union status on target
    };

    /// <summary>
    /// Alias for Aetherpact (Fey Union command).
    /// </summary>
    public static readonly ActionDefinition FeyUnion = Aetherpact;

    public static readonly ActionDefinition DissolveUnion = new()
    {
        ActionId = 7869,
        Name = "Dissolve Union",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0
    };

    #endregion

    #region Seraph Abilities

    public static readonly ActionDefinition SummonSeraph = new()
    {
        ActionId = 16545,
        Name = "Summon Seraph",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0
    };

    public static readonly ActionDefinition Consolation = new()
    {
        ActionId = 16546,
        Name = "Consolation",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self, // Seraph-centered AoE
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 30f, // Has 2 charges
        Range = 0f,
        Radius = 20f,
        MpCost = 0,
        HealPotency = 250,
        ShieldPotency = 250
    };

    public static readonly ActionDefinition Seraphism = new()
    {
        ActionId = 37014,
        Name = "Seraphism",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff | ActionEffectType.HoT,
        CastTime = 0f,
        RecastTime = 180f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        HealPotency = 100, // Party-wide regen
        AppliedStatusId = 3963, // Seraphism buff
        AppliedStatusDuration = 20f
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
        EffectTypes = ActionEffectType.None,
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

    public static readonly ActionDefinition Resurrection = new()
    {
        ActionId = 173,
        Name = "Resurrection",
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
    /// All SCH GCD damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DamageGcds =
    {
        BroilIV, BroilIII, BroilII, Broil, RuinII, Ruin
    };

    /// <summary>
    /// All SCH GCD DoT spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DotGcds =
    {
        Biolysis, BioII, Bio
    };

    /// <summary>
    /// All SCH GCD single-target heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] SingleHealGcds =
    {
        Manifestation, Adloquium, Physick
    };

    /// <summary>
    /// All SCH GCD AoE heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEHealGcds =
    {
        Accession, Concitation, Succor
    };

    /// <summary>
    /// All SCH GCD AoE damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEDamageGcds =
    {
        ArtOfWarII, ArtOfWar
    };

    /// <summary>
    /// Gets the appropriate damage GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetDamageGcdForLevel(byte level, bool isMoving)
    {
        // If moving, prefer Ruin II for instant cast
        if (isMoving && level >= RuinII.MinLevel)
            return RuinII;

        foreach (var action in DamageGcds)
        {
            // Skip Ruin II when not moving (lower potency than Broil)
            if (action == RuinII && !isMoving)
                continue;

            if (level >= action.MinLevel)
                return action;
        }
        return Ruin;
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
        return Bio;
    }

    /// <summary>
    /// Gets the appropriate single-target heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetSingleHealGcdForLevel(byte level, bool hasSeraphism = false)
    {
        if (hasSeraphism && level >= Manifestation.MinLevel)
            return Manifestation;

        foreach (var action in SingleHealGcds)
        {
            if (action == Manifestation)
                continue; // Skip Manifestation unless Seraphism is active

            if (level >= action.MinLevel)
                return action;
        }
        return Physick;
    }

    /// <summary>
    /// Gets the appropriate AoE heal GCD for the player's level.
    /// </summary>
    public static ActionDefinition GetAoEHealGcdForLevel(byte level, bool hasSeraphism = false)
    {
        if (hasSeraphism && level >= Accession.MinLevel)
            return Accession;

        foreach (var action in AoEHealGcds)
        {
            if (action == Accession)
                continue; // Skip Accession unless Seraphism is active

            if (level >= action.MinLevel)
                return action;
        }
        return Succor;
    }

    /// <summary>
    /// Gets the appropriate AoE damage GCD for the player's level.
    /// Returns null if player level is below Art of War (level 46).
    /// </summary>
    public static ActionDefinition? GetAoEDamageGcdForLevel(byte level)
    {
        foreach (var action in AoEDamageGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return null;
    }

    /// <summary>
    /// Alias for GetAoEDamageGcdForLevel.
    /// </summary>
    public static ActionDefinition? GetAoEDamageForLevel(byte level) => GetAoEDamageGcdForLevel(level);

    /// <summary>
    /// Gets the DoT status ID for the player's level.
    /// </summary>
    public static uint GetDotStatusId(byte level)
    {
        if (level >= Biolysis.MinLevel)
            return Biolysis.AppliedStatusId ?? 1895;
        if (level >= BioII.MinLevel)
            return BioII.AppliedStatusId ?? 189;
        return Bio.AppliedStatusId ?? 179;
    }

    #endregion

    #region Status IDs

    /// <summary>
    /// Galvanize shield status ID.
    /// </summary>
    public const ushort GalvanizeStatusId = 297;

    /// <summary>
    /// Catalyze shield status ID (critical Adloquium bonus shield).
    /// </summary>
    public const ushort CatalyzeStatusId = 1918;

    /// <summary>
    /// Excogitation buff status ID.
    /// </summary>
    public const ushort ExcogitationStatusId = 1220;

    /// <summary>
    /// Recitation buff status ID.
    /// </summary>
    public const ushort RecitationStatusId = 1896;

    /// <summary>
    /// Emergency Tactics buff status ID.
    /// </summary>
    public const ushort EmergencyTacticsStatusId = 792;

    /// <summary>
    /// Dissipation buff status ID.
    /// </summary>
    public const ushort DissipationStatusId = 791;

    /// <summary>
    /// Chain Stratagem debuff status ID.
    /// </summary>
    public const ushort ChainStratagemStatusId = 1221;

    /// <summary>
    /// Fey Union tether status ID.
    /// </summary>
    public const ushort FeyUnionStatusId = 1223;

    /// <summary>
    /// Seraphism buff status ID.
    /// </summary>
    public const ushort SeraphismStatusId = 3963;

    /// <summary>
    /// Protraction buff status ID.
    /// </summary>
    public const ushort ProtractionStatusId = 2710;

    /// <summary>
    /// Impact Imminent buff status ID (from Chain Stratagem).
    /// </summary>
    public const ushort ImpactImminentStatusId = 3961;

    #endregion
}
