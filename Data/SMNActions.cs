using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Summoner (SMN) and Arcanist (ACN) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Persephone, the Greek queen of the underworld who commands souls.
/// </summary>
public static class SMNActions
{
    #region Basic GCDs

    /// <summary>
    /// Ruin - Basic single target spell (Lv.1)
    /// </summary>
    public static readonly ActionDefinition Ruin = new()
    {
        ActionId = 163,
        Name = "Ruin",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 240
    };

    /// <summary>
    /// Ruin II - Instant basic damage (Lv.30)
    /// </summary>
    public static readonly ActionDefinition Ruin2 = new()
    {
        ActionId = 172,
        Name = "Ruin II",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 270
    };

    /// <summary>
    /// Ruin III - Primary single target spell (Lv.54)
    /// </summary>
    public static readonly ActionDefinition Ruin3 = new()
    {
        ActionId = 3579,
        Name = "Ruin III",
        MinLevel = 54,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 360
    };

    /// <summary>
    /// Ruin IV - Powerful instant proc spell (Lv.62)
    /// Requires Further Ruin buff
    /// </summary>
    public static readonly ActionDefinition Ruin4 = new()
    {
        ActionId = 7426,
        Name = "Ruin IV",
        MinLevel = 62,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 490
    };

    /// <summary>
    /// Outburst - AoE damage spell (Lv.26)
    /// </summary>
    public static readonly ActionDefinition Outburst = new()
    {
        ActionId = 16511,
        Name = "Outburst",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 100
    };

    /// <summary>
    /// Tri-disaster - Enhanced AoE damage (Lv.74)
    /// Upgrades Outburst
    /// </summary>
    public static readonly ActionDefinition TriDisaster = new()
    {
        ActionId = 25826,
        Name = "Tri-disaster",
        MinLevel = 74,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 300,
        DamagePotency = 120
    };

    #endregion

    #region Demi-Summon GCDs

    /// <summary>
    /// Astral Impulse - Demi-Bahamut single target (Lv.58)
    /// Replaces Ruin III during Demi-Bahamut
    /// </summary>
    public static readonly ActionDefinition AstralImpulse = new()
    {
        ActionId = 25820,
        Name = "Astral Impulse",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 500
    };

    /// <summary>
    /// Astral Flare - Demi-Bahamut AoE (Lv.58)
    /// Replaces Tri-disaster during Demi-Bahamut
    /// </summary>
    public static readonly ActionDefinition AstralFlare = new()
    {
        ActionId = 25821,
        Name = "Astral Flare",
        MinLevel = 58,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 180
    };

    /// <summary>
    /// Fountain of Fire - Demi-Phoenix single target (Lv.80)
    /// Replaces Ruin III during Demi-Phoenix
    /// </summary>
    public static readonly ActionDefinition FountainOfFire = new()
    {
        ActionId = 16514,
        Name = "Fountain of Fire",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 580
    };

    /// <summary>
    /// Brand of Purgatory - Demi-Phoenix AoE (Lv.80)
    /// Replaces Tri-disaster during Demi-Phoenix
    /// </summary>
    public static readonly ActionDefinition BrandOfPurgatory = new()
    {
        ActionId = 16515,
        Name = "Brand of Purgatory",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 240
    };

    /// <summary>
    /// Umbral Impulse - Solar Bahamut single target (Lv.100)
    /// Replaces Ruin III during Solar Bahamut
    /// </summary>
    public static readonly ActionDefinition UmbralImpulse = new()
    {
        ActionId = 36994,
        Name = "Umbral Impulse",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Umbral Flare - Solar Bahamut AoE (Lv.100)
    /// Replaces Tri-disaster during Solar Bahamut
    /// </summary>
    public static readonly ActionDefinition UmbralFlare = new()
    {
        ActionId = 36995,
        Name = "Umbral Flare",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 280
    };

    #endregion

    #region Gemshine (Primal Attunement GCDs)

    /// <summary>
    /// Ruby Rite - Ifrit single target (Lv.6)
    /// Uses Ruby Arcanum attunement
    /// </summary>
    public static readonly ActionDefinition RubyRite = new()
    {
        ActionId = 25823,
        Name = "Ruby Rite",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.8f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 540
    };

    /// <summary>
    /// Ruby Catastrophe - Ifrit AoE (Lv.6)
    /// Uses Ruby Arcanum attunement
    /// </summary>
    public static readonly ActionDefinition RubyCatastrophe = new()
    {
        ActionId = 25832,
        Name = "Ruby Catastrophe",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 2.8f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 210
    };

    /// <summary>
    /// Topaz Rite - Titan single target (Lv.6)
    /// Uses Topaz Arcanum attunement
    /// Grants Titan's Favor for Mountain Buster
    /// </summary>
    public static readonly ActionDefinition TopazRite = new()
    {
        ActionId = 25824,
        Name = "Topaz Rite",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 340
    };

    /// <summary>
    /// Topaz Catastrophe - Titan AoE (Lv.6)
    /// Uses Topaz Arcanum attunement
    /// Grants Titan's Favor for Mountain Buster
    /// </summary>
    public static readonly ActionDefinition TopazCatastrophe = new()
    {
        ActionId = 25833,
        Name = "Topaz Catastrophe",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 150
    };

    /// <summary>
    /// Emerald Rite - Garuda single target (Lv.6)
    /// Uses Emerald Arcanum attunement
    /// </summary>
    public static readonly ActionDefinition EmeraldRite = new()
    {
        ActionId = 25825,
        Name = "Emerald Rite",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 240
    };

    /// <summary>
    /// Emerald Catastrophe - Garuda AoE (Lv.6)
    /// Uses Emerald Arcanum attunement
    /// </summary>
    public static readonly ActionDefinition EmeraldCatastrophe = new()
    {
        ActionId = 25834,
        Name = "Emerald Catastrophe",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 100
    };

    #endregion

    #region Primal Favor GCDs

    /// <summary>
    /// Crimson Cyclone - Ifrit melee GCD (Lv.86)
    /// Requires Ifrit's Favor, gap closer
    /// </summary>
    public static readonly ActionDefinition CrimsonCyclone = new()
    {
        ActionId = 25835,
        Name = "Crimson Cyclone",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f, // Gap closer range
        MpCost = 0,
        DamagePotency = 490
    };

    /// <summary>
    /// Crimson Strike - Ifrit follow-up melee GCD (Lv.86)
    /// Only usable after Crimson Cyclone
    /// </summary>
    public static readonly ActionDefinition CrimsonStrike = new()
    {
        ActionId = 25836,
        Name = "Crimson Strike",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 3f, // Melee range
        MpCost = 0,
        DamagePotency = 490
    };

    /// <summary>
    /// Slipstream - Garuda channeled AoE (Lv.86)
    /// Requires Garuda's Favor
    /// </summary>
    public static readonly ActionDefinition Slipstream = new()
    {
        ActionId = 25837,
        Name = "Slipstream",
        MinLevel = 86,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 3.0f,
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 490
    };

    #endregion

    #region Summon oGCDs

    /// <summary>
    /// Summon Carbuncle - Base summon (Lv.2)
    /// Required before other summons
    /// </summary>
    public static readonly ActionDefinition SummonCarbuncle = new()
    {
        ActionId = 25798,
        Name = "Summon Carbuncle",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Aethercharge - Resets primal summons (Lv.6)
    /// Upgraded to Dreadwyrm Trance at Lv.58, then Summon Bahamut at Lv.70.
    /// Grants Further Ruin proc.
    /// </summary>
    public static readonly ActionDefinition Aethercharge = new()
    {
        ActionId = 25800,
        Name = "Aethercharge",
        MinLevel = 6,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Bahamut - Demi-Bahamut (Lv.70)
    /// 15 second duration with auto-attacks
    /// </summary>
    public static readonly ActionDefinition SummonBahamut = new()
    {
        ActionId = 7427,
        Name = "Summon Bahamut",
        MinLevel = 70,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // 60s total cycle time
        MpCost = 0
    };

    /// <summary>
    /// Summon Phoenix - Demi-Phoenix (Lv.80)
    /// Alternates with Bahamut
    /// </summary>
    public static readonly ActionDefinition SummonPhoenix = new()
    {
        ActionId = 25831,
        Name = "Summon Phoenix",
        MinLevel = 80,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Solar Bahamut - Enhanced Demi-Bahamut (Lv.100)
    /// Replaces every other Bahamut summon
    /// </summary>
    public static readonly ActionDefinition SummonSolarBahamut = new()
    {
        ActionId = 36992,
        Name = "Summon Solar Bahamut",
        MinLevel = 100,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Ifrit - Fire primal (Lv.30)
    /// Grants 2 Ruby Arcanum attunements
    /// </summary>
    public static readonly ActionDefinition SummonIfrit = new()
    {
        ActionId = 25838,
        Name = "Summon Ifrit",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Ifrit II - Enhanced fire primal (Lv.90)
    /// Grants Ifrit's Favor for Crimson Cyclone
    /// </summary>
    public static readonly ActionDefinition SummonIfrit2 = new()
    {
        ActionId = 25838, // Same ID, upgraded
        Name = "Summon Ifrit II",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Titan - Earth primal (Lv.35)
    /// Grants 4 Topaz Arcanum attunements
    /// </summary>
    public static readonly ActionDefinition SummonTitan = new()
    {
        ActionId = 25839,
        Name = "Summon Titan",
        MinLevel = 35,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Titan II - Enhanced earth primal (Lv.90)
    /// Grants Titan's Favor for Mountain Buster
    /// </summary>
    public static readonly ActionDefinition SummonTitan2 = new()
    {
        ActionId = 25839, // Same ID, upgraded
        Name = "Summon Titan II",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Garuda - Wind primal (Lv.45)
    /// Grants 4 Emerald Arcanum attunements
    /// </summary>
    public static readonly ActionDefinition SummonGaruda = new()
    {
        ActionId = 25840,
        Name = "Summon Garuda",
        MinLevel = 45,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    /// <summary>
    /// Summon Garuda II - Enhanced wind primal (Lv.90)
    /// Grants Garuda's Favor for Slipstream
    /// </summary>
    public static readonly ActionDefinition SummonGaruda2 = new()
    {
        ActionId = 25840, // Same ID, upgraded
        Name = "Summon Garuda II",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 2.5f,
        MpCost = 0
    };

    #endregion

    #region Aetherflow oGCDs

    /// <summary>
    /// Energy Drain - Aetherflow generator, single target (Lv.10)
    /// Grants 2 Aetherflow stacks and Further Ruin
    /// </summary>
    public static readonly ActionDefinition EnergyDrain = new()
    {
        ActionId = 16508,
        Name = "Energy Drain",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 100
    };

    /// <summary>
    /// Energy Siphon - Aetherflow generator, AoE (Lv.52)
    /// Grants 2 Aetherflow stacks and Further Ruin
    /// </summary>
    public static readonly ActionDefinition EnergySiphon = new()
    {
        ActionId = 16510,
        Name = "Energy Siphon",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 40
    };

    /// <summary>
    /// Necrotize - Aetherflow spender, single target (Lv.92)
    /// Upgrades Fester
    /// </summary>
    public static readonly ActionDefinition Necrotize = new()
    {
        ActionId = 36990,
        Name = "Necrotize",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 440
    };

    /// <summary>
    /// Fester - Aetherflow spender, single target (Lv.10)
    /// </summary>
    public static readonly ActionDefinition Fester = new()
    {
        ActionId = 181,
        Name = "Fester",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 340
    };

    /// <summary>
    /// Painflare - Aetherflow spender, AoE (Lv.52)
    /// </summary>
    public static readonly ActionDefinition Painflare = new()
    {
        ActionId = 3578,
        Name = "Painflare",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 150
    };

    #endregion

    #region Buff oGCDs

    /// <summary>
    /// Searing Light - Party damage buff (Lv.66)
    /// 5% damage increase for 20s
    /// </summary>
    public static readonly ActionDefinition SearingLight = new()
    {
        ActionId = 25801,
        Name = "Searing Light",
        MinLevel = 66,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        MpCost = 0,
        AppliedStatusId = StatusIds.SearingLight,
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Searing Flash - Damage action during Searing Light (Lv.96)
    /// Requires Searing Light active on self
    /// </summary>
    public static readonly ActionDefinition SearingFlash = new()
    {
        ActionId = 36991,
        Name = "Searing Flash",
        MinLevel = 96,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    /// <summary>
    /// Radiant Aegis - Self shield (Lv.2)
    /// 2 charges
    /// </summary>
    public static readonly ActionDefinition RadiantAegis = new()
    {
        ActionId = 25799,
        Name = "Radiant Aegis",
        MinLevel = 2,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        MpCost = 0,
        AppliedStatusId = StatusIds.RadiantAegis,
        AppliedStatusDuration = 30f
    };

    #endregion

    #region Demi Enkindle oGCDs

    /// <summary>
    /// Enkindle Bahamut - Akh Morn (Lv.70)
    /// Powerful attack during Demi-Bahamut
    /// </summary>
    public static readonly ActionDefinition EnkindleBahamut = new()
    {
        ActionId = 7429,
        Name = "Enkindle Bahamut",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 900 // Variable based on party size
    };

    /// <summary>
    /// Enkindle Phoenix - Revelation (Lv.80)
    /// Powerful attack during Demi-Phoenix
    /// </summary>
    public static readonly ActionDefinition EnkindlePhoenix = new()
    {
        ActionId = 16516,
        Name = "Enkindle Phoenix",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 900 // Variable based on party size
    };

    /// <summary>
    /// Enkindle Solar Bahamut - Luxwave (Lv.100)
    /// Powerful attack during Solar Bahamut
    /// </summary>
    public static readonly ActionDefinition EnkindleSolarBahamut = new()
    {
        ActionId = 36998,
        Name = "Enkindle Solar Bahamut",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 1000
    };

    #endregion

    #region Astral Flow oGCDs

    /// <summary>
    /// Deathflare - Demi-Bahamut finisher (Lv.60)
    /// </summary>
    public static readonly ActionDefinition Deathflare = new()
    {
        ActionId = 3582,
        Name = "Deathflare",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 500
    };

    /// <summary>
    /// Rekindle - Demi-Phoenix heal (Lv.80)
    /// Heals target and grants Rekindle HoT
    /// </summary>
    public static readonly ActionDefinition Rekindle = new()
    {
        ActionId = 25830,
        Name = "Rekindle",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 30f,
        MpCost = 0,
        HealPotency = 400
    };

    /// <summary>
    /// Sunflare - Solar Bahamut finisher (Lv.100)
    /// </summary>
    public static readonly ActionDefinition Sunflare = new()
    {
        ActionId = 36996,
        Name = "Sunflare",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 20f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 700
    };

    /// <summary>
    /// Lux Solaris - Solar Bahamut party heal (Lv.100)
    /// </summary>
    public static readonly ActionDefinition LuxSolaris = new()
    {
        ActionId = 36997,
        Name = "Lux Solaris",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 60f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 500
    };

    /// <summary>
    /// Mountain Buster - Titan favor oGCD (Lv.86)
    /// Usable after each Topaz Rite/Catastrophe
    /// </summary>
    public static readonly ActionDefinition MountainBuster = new()
    {
        ActionId = 25836,
        Name = "Mountain Buster",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 1f,
        Range = 25f,
        MpCost = 0,
        DamagePotency = 160
    };

    #endregion

    #region Status IDs

    /// <summary>
    /// Status IDs for buff/debuff tracking.
    /// </summary>
    public static class StatusIds
    {
        // Self buffs
        public const uint Swiftcast = 167;
        public const uint LucidDreaming = 1204;
        public const uint Surecast = 160;
        public const uint FurtherRuin = 2701;       // Enables Ruin IV
        public const uint SearingLight = 2703;      // Party damage buff
        public const uint RadiantAegis = 2702;      // Shield

        // Primal attunements (tracked via gauge, but useful for reference)
        public const uint IfritsFavor = 2724;       // Enables Crimson Cyclone
        public const uint GarudasFavor = 2725;      // Enables Slipstream
        public const uint TitansFavor = 2853;       // Enables Mountain Buster

        // Demi states (tracked via gauge)
        public const uint EverlastingFlight = 16517; // Phoenix regen buff on party

        // Debuffs
        public const uint Addle = 1203;

        // DoT (removed in Endwalker but kept for reference)
        public const uint Bio = 179;
        public const uint Bio2 = 189;
        public const uint Miasma = 180;
        public const uint Miasma3 = 1215;
    }

    #endregion

    #region Lookup Helpers

    /// <summary>
    /// Gets the best single target Ruin spell for the player's level.
    /// </summary>
    public static ActionDefinition GetRuinSpell(byte level)
    {
        if (level >= Ruin3.MinLevel)
            return Ruin3;
        return Ruin;
    }

    /// <summary>
    /// Gets the best AoE spell for the player's level.
    /// </summary>
    public static ActionDefinition GetAoeSpell(byte level)
    {
        if (level >= TriDisaster.MinLevel)
            return TriDisaster;
        if (level >= Outburst.MinLevel)
            return Outburst;
        return Ruin; // Fallback
    }

    /// <summary>
    /// Gets the Aetherflow spender for single target.
    /// </summary>
    public static ActionDefinition GetAetherflowSpenderST(byte level)
    {
        if (level >= Necrotize.MinLevel)
            return Necrotize;
        return Fester;
    }

    /// <summary>
    /// Gets the Aetherflow spender for AoE.
    /// </summary>
    public static ActionDefinition GetAetherflowSpenderAoe(byte level)
    {
        if (level >= Painflare.MinLevel)
            return Painflare;
        return GetAetherflowSpenderST(level); // Fallback to ST
    }

    /// <summary>
    /// Gets the Energy Drain variant based on AoE situation.
    /// </summary>
    public static ActionDefinition GetEnergyDrain(byte level, bool useAoe)
    {
        if (useAoe && level >= EnergySiphon.MinLevel)
            return EnergySiphon;
        return EnergyDrain;
    }

    /// <summary>
    /// Gets the appropriate Gemshine action based on attunement type.
    /// </summary>
    /// <param name="attunement">1=Ifrit, 2=Titan, 3=Garuda</param>
    /// <param name="useAoe">Whether to use AoE variant</param>
    public static ActionDefinition? GetGemshinAction(int attunement, bool useAoe)
    {
        return attunement switch
        {
            1 => useAoe ? RubyCatastrophe : RubyRite,
            2 => useAoe ? TopazCatastrophe : TopazRite,
            3 => useAoe ? EmeraldCatastrophe : EmeraldRite,
            _ => null
        };
    }

    /// <summary>
    /// Gets the demi-summon GCD based on active summon.
    /// </summary>
    /// <param name="isBahamut">True for Bahamut, false for Phoenix</param>
    /// <param name="isSolarBahamut">True for Solar Bahamut</param>
    /// <param name="useAoe">Whether to use AoE variant</param>
    public static ActionDefinition GetDemiSummonGcd(bool isBahamut, bool isSolarBahamut, bool useAoe)
    {
        if (isSolarBahamut)
            return useAoe ? UmbralFlare : UmbralImpulse;
        if (isBahamut)
            return useAoe ? AstralFlare : AstralImpulse;
        return useAoe ? BrandOfPurgatory : FountainOfFire; // Phoenix
    }

    /// <summary>
    /// Gets the Enkindle action for the active demi-summon.
    /// </summary>
    public static ActionDefinition? GetEnkindleAction(bool isBahamut, bool isPhoenix, bool isSolarBahamut)
    {
        if (isSolarBahamut)
            return EnkindleSolarBahamut;
        if (isBahamut)
            return EnkindleBahamut;
        if (isPhoenix)
            return EnkindlePhoenix;
        return null;
    }

    /// <summary>
    /// Gets the Astral Flow action for the active demi-summon.
    /// </summary>
    public static ActionDefinition? GetAstralFlowAction(bool isBahamut, bool isPhoenix, bool isSolarBahamut)
    {
        if (isSolarBahamut)
            return Sunflare;
        if (isBahamut)
            return Deathflare;
        if (isPhoenix)
            return Rekindle;
        return null;
    }

    #endregion
}
