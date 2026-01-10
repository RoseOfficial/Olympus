using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Sage (SGE) action definitions.
/// Action IDs and data sourced from FFXIV game data.
/// Named after Asclepius, the god of medicine.
/// </summary>
public static class SGEActions
{
    #region GCD Heals

    public static readonly ActionDefinition Diagnosis = new()
    {
        ActionId = 24284,
        Name = "Diagnosis",
        MinLevel = 2,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 400,
        HealPotency = 450
    };

    public static readonly ActionDefinition Prognosis = new()
    {
        ActionId = 24286,
        Name = "Prognosis",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 2.0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 800,
        HealPotency = 300
    };

    /// <summary>
    /// Eukrasian Diagnosis - Shield heal requiring Eukrasia active.
    /// Generates Addersting when shield fully breaks.
    /// </summary>
    public static readonly ActionDefinition EukrasianDiagnosis = new()
    {
        ActionId = 24291,
        Name = "Eukrasian Diagnosis",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f, // Instant (Eukrasia modified)
        RecastTime = 2.5f, // Fixed 2.5s when Eukrasian
        Range = 30f,
        MpCost = 900,
        HealPotency = 300,
        ShieldPotency = 540, // 180% of heal potency
        AppliedStatusId = 2607, // Eukrasian Diagnosis shield
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Eukrasian Prognosis - AoE shield requiring Eukrasia active.
    /// </summary>
    public static readonly ActionDefinition EukrasianPrognosis = new()
    {
        ActionId = 24292,
        Name = "Eukrasian Prognosis",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f, // Instant (Eukrasia modified)
        RecastTime = 2.5f, // Fixed 2.5s when Eukrasian
        Range = 0f,
        Radius = 15f,
        MpCost = 900,
        HealPotency = 100,
        ShieldPotency = 360, // 360% of heal potency for shield portion
        AppliedStatusId = 2609, // Eukrasian Prognosis shield
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Eukrasian Prognosis II - Upgraded AoE shield at level 96.
    /// </summary>
    public static readonly ActionDefinition EukrasianPrognosisII = new()
    {
        ActionId = 37033,
        Name = "Eukrasian Prognosis II",
        MinLevel = 96,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield,
        CastTime = 0f, // Instant (Eukrasia modified)
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 15f,
        MpCost = 900,
        HealPotency = 100,
        ShieldPotency = 360,
        AppliedStatusId = 2609,
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Pneuma - Line AoE that damages enemies and heals party.
    /// </summary>
    public static readonly ActionDefinition Pneuma = new()
    {
        ActionId = 24318,
        Name = "Pneuma",
        MinLevel = 90,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Line AoE through target
        EffectTypes = ActionEffectType.Damage | ActionEffectType.Heal,
        CastTime = 1.5f,
        RecastTime = 120f, // 2 minute cooldown
        Range = 25f,
        Radius = 5f, // Width of line
        MpCost = 0,
        DamagePotency = 600,
        HealPotency = 600 // Party-wide heal
    };

    #endregion

    #region GCD Damage

    public static readonly ActionDefinition Dosis = new()
    {
        ActionId = 24283,
        Name = "Dosis",
        MinLevel = 1,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 200,
        DamagePotency = 250
    };

    public static readonly ActionDefinition DosisII = new()
    {
        ActionId = 24306,
        Name = "Dosis II",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 320
    };

    public static readonly ActionDefinition DosisIII = new()
    {
        ActionId = 24312,
        Name = "Dosis III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 1.5f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 300,
        DamagePotency = 380
    };

    public static readonly ActionDefinition Dyskrasia = new()
    {
        ActionId = 24297,
        Name = "Dyskrasia",
        MinLevel = 46,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self, // Self-centered PBAoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 160
    };

    public static readonly ActionDefinition DyskrasiaII = new()
    {
        ActionId = 24315,
        Name = "Dyskrasia II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self, // Self-centered PBAoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 170
    };

    /// <summary>
    /// Toxikon - Instant cast, consumes Addersting.
    /// Good for movement or when shields break.
    /// </summary>
    public static readonly ActionDefinition Toxikon = new()
    {
        ActionId = 24304,
        Name = "Toxikon",
        MinLevel = 66,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // AoE around target
        MpCost = 0,
        DamagePotency = 330
    };

    /// <summary>
    /// Toxikon II - Upgraded Toxikon at level 82.
    /// </summary>
    public static readonly ActionDefinition ToxikonII = new()
    {
        ActionId = 24316,
        Name = "Toxikon II",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 2.5f,
        Range = 25f,
        Radius = 5f, // AoE around target
        MpCost = 0,
        DamagePotency = 380
    };

    /// <summary>
    /// Phlegma - Frontal cone oGCD damage with charges.
    /// </summary>
    public static readonly ActionDefinition Phlegma = new()
    {
        ActionId = 24289,
        Name = "Phlegma",
        MinLevel = 26,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy, // Frontal AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f, // Instant
        RecastTime = 40f, // Charge time
        Range = 6f, // Close range
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 400
    };

    public static readonly ActionDefinition PhlegmaII = new()
    {
        ActionId = 24307,
        Name = "Phlegma II",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 6f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 510
    };

    public static readonly ActionDefinition PhlegmaIII = new()
    {
        ActionId = 24313,
        Name = "Phlegma III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 40f, // 2 charges, 40s per charge
        Range = 6f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    #endregion

    #region GCD DoTs

    /// <summary>
    /// Eukrasian Dosis - DoT requiring Eukrasia active.
    /// </summary>
    public static readonly ActionDefinition EukrasianDosis = new()
    {
        ActionId = 24293,
        Name = "Eukrasian Dosis",
        MinLevel = 30,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f, // Instant (Eukrasia modified)
        RecastTime = 2.5f, // Fixed 2.5s when Eukrasian
        Range = 25f,
        MpCost = 400,
        DamagePotency = 0, // No initial damage
        AppliedStatusId = 2614, // Eukrasian Dosis DoT
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition EukrasianDosisII = new()
    {
        ActionId = 24308,
        Name = "Eukrasian Dosis II",
        MinLevel = 72,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 0,
        AppliedStatusId = 2615,
        AppliedStatusDuration = 30f
    };

    public static readonly ActionDefinition EukrasianDosisIII = new()
    {
        ActionId = 24314,
        Name = "Eukrasian Dosis III",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 25f,
        MpCost = 400,
        DamagePotency = 0,
        AppliedStatusId = 2616, // Eukrasian Dosis III DoT
        AppliedStatusDuration = 30f
    };

    /// <summary>
    /// Eukrasian Dyskrasia - AoE DoT requiring Eukrasia.
    /// Does NOT stack with Eukrasian Dosis on same target.
    /// </summary>
    public static readonly ActionDefinition EukrasianDyskrasia = new()
    {
        ActionId = 37032,
        Name = "Eukrasian Dyskrasia",
        MinLevel = 82,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.Self, // Self-centered PBAoE
        EffectTypes = ActionEffectType.DoT,
        CastTime = 0f,
        RecastTime = 2.5f,
        Range = 0f,
        Radius = 5f,
        MpCost = 400,
        DamagePotency = 0,
        AppliedStatusId = 2616, // Same DoT status - does not stack
        AppliedStatusDuration = 30f
    };

    #endregion

    #region oGCD Heals - Addersgall

    /// <summary>
    /// Druochole - No cooldown, consumes Addersgall.
    /// Best for pure single-target healing.
    /// </summary>
    public static readonly ActionDefinition Druochole = new()
    {
        ActionId = 24296,
        Name = "Druochole",
        MinLevel = 45,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 1f, // Addersgall abilities share short recast
        Range = 30f,
        MpCost = 0, // Restores 7% MP
        HealPotency = 600
    };

    /// <summary>
    /// Taurochole - Single target heal + mitigation.
    /// Shares 45s CD with Kerachole.
    /// </summary>
    public static readonly ActionDefinition Taurochole = new()
    {
        ActionId = 24303,
        Name = "Taurochole",
        MinLevel = 62,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 45f,
        Range = 30f,
        MpCost = 0, // Restores 7% MP
        HealPotency = 700,
        AppliedStatusId = 2618, // 10% damage reduction
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Ixochole - AoE heal, consumes Addersgall.
    /// </summary>
    public static readonly ActionDefinition Ixochole = new()
    {
        ActionId = 24299,
        Name = "Ixochole",
        MinLevel = 52,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0, // Restores 7% MP
        HealPotency = 400
    };

    /// <summary>
    /// Kerachole - AoE regen + mitigation. Best Addersgall value.
    /// Shares 30s CD with Taurochole's mitigation.
    /// </summary>
    public static readonly ActionDefinition Kerachole = new()
    {
        ActionId = 24298,
        Name = "Kerachole",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.HoT | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0, // Restores 7% MP
        HealPotency = 100, // Per tick (5 ticks = 500 total)
        AppliedStatusId = 2618, // 10% damage reduction
        AppliedStatusDuration = 15f
    };

    #endregion

    #region oGCD Heals - Free

    /// <summary>
    /// Physis II - Party HoT + healing received boost.
    /// </summary>
    public static readonly ActionDefinition PhysisII = new()
    {
        ActionId = 24302,
        Name = "Physis II",
        MinLevel = 60,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.HoT | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 130, // Per tick (5 ticks = 650 total)
        AppliedStatusId = 2620, // Physis II HoT + 10% healing boost
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Holos - AoE heal + shield + mitigation.
    /// </summary>
    public static readonly ActionDefinition Holos = new()
    {
        ActionId = 24310,
        Name = "Holos",
        MinLevel = 76,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Heal | ActionEffectType.Shield | ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 300,
        ShieldPotency = 300,
        AppliedStatusId = 3003, // 10% damage reduction
        AppliedStatusDuration = 20f
    };

    /// <summary>
    /// Pepsis - Consume shields for healing.
    /// </summary>
    public static readonly ActionDefinition Pepsis = new()
    {
        ActionId = 24301,
        Name = "Pepsis",
        MinLevel = 58,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Heal,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        HealPotency = 350 // 350 for E.Diag shields, 100 for E.Prog shields
    };

    /// <summary>
    /// Rhizomata - Grants 1 Addersgall stack.
    /// </summary>
    public static readonly ActionDefinition Rhizomata = new()
    {
        ActionId = 24309,
        Name = "Rhizomata",
        MinLevel = 74,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.None, // Resource generation
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0
    };

    #endregion

    #region Multi-Hit Shields

    /// <summary>
    /// Haima - Multi-hit shield on single target.
    /// 5 stacks of 300 potency each (1800 total potential).
    /// </summary>
    public static readonly ActionDefinition Haima = new()
    {
        ActionId = 24305,
        Name = "Haima",
        MinLevel = 70,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 30f,
        MpCost = 0,
        ShieldPotency = 300, // Per stack (5 stacks + initial)
        AppliedStatusId = 2621, // Haima
        AppliedStatusDuration = 15f
    };

    /// <summary>
    /// Panhaima - Party-wide multi-hit shield.
    /// 5 stacks of 200 potency each (1200 total potential per member).
    /// </summary>
    public static readonly ActionDefinition Panhaima = new()
    {
        ActionId = 24311,
        Name = "Panhaima",
        MinLevel = 80,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.PartyAoE,
        EffectTypes = ActionEffectType.Shield,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 0f,
        Radius = 15f,
        MpCost = 0,
        ShieldPotency = 200, // Per stack (5 stacks + initial)
        AppliedStatusId = 2613, // Panhaima
        AppliedStatusDuration = 15f
    };

    #endregion

    #region Kardia System

    /// <summary>
    /// Kardia - Places Kardion on target, heals them when you deal damage.
    /// </summary>
    public static readonly ActionDefinition Kardia = new()
    {
        ActionId = 24285,
        Name = "Kardia",
        MinLevel = 4,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 5f, // 5s swap cooldown
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 2604, // Kardion on target
        AppliedStatusDuration = 0f // Permanent until changed
    };

    /// <summary>
    /// Soteria - Boosts Kardia healing for 4 stacks.
    /// </summary>
    public static readonly ActionDefinition Soteria = new()
    {
        ActionId = 24294,
        Name = "Soteria",
        MinLevel = 35,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f, // 90s at base, 60s with trait
        MpCost = 0,
        AppliedStatusId = 2610, // Soteria (4 stacks, +70% Kardia each)
        AppliedStatusDuration = 15f
    };

    #endregion

    #region Buffs

    /// <summary>
    /// Eukrasia - Modifies next Diagnosis, Dosis, or Prognosis.
    /// </summary>
    public static readonly ActionDefinition Eukrasia = new()
    {
        ActionId = 24290,
        Name = "Eukrasia",
        MinLevel = 30,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 1f,
        MpCost = 0,
        AppliedStatusId = 2606, // Eukrasia active
        AppliedStatusDuration = 30f // Until used
    };

    /// <summary>
    /// Krasis - Increases healing received by target.
    /// </summary>
    public static readonly ActionDefinition Krasis = new()
    {
        ActionId = 24317,
        Name = "Krasis",
        MinLevel = 86,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 30f,
        MpCost = 0,
        AppliedStatusId = 2622, // +20% healing received
        AppliedStatusDuration = 10f
    };

    /// <summary>
    /// Zoe - Next GCD heal potency +50%.
    /// </summary>
    public static readonly ActionDefinition Zoe = new()
    {
        ActionId = 24300,
        Name = "Zoe",
        MinLevel = 56,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f, // 120s at base, 90s with trait
        MpCost = 0,
        AppliedStatusId = 2611, // Zoe
        AppliedStatusDuration = 30f // Until next GCD heal
    };

    /// <summary>
    /// Philosophia - Level 100 ability. Party-wide Kardia effect.
    /// </summary>
    public static readonly ActionDefinition Philosophia = new()
    {
        ActionId = 37035,
        Name = "Philosophia",
        MinLevel = 100,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 0f,
        Radius = 30f,
        MpCost = 0,
        AppliedStatusId = 3898, // Philosophia
        AppliedStatusDuration = 20f
    };

    #endregion

    #region oGCD Damage

    /// <summary>
    /// Psyche - AoE oGCD damage.
    /// </summary>
    public static readonly ActionDefinition Psyche = new()
    {
        ActionId = 37034,
        Name = "Psyche",
        MinLevel = 92,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy, // Target-centered AoE
        EffectTypes = ActionEffectType.Damage,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        DamagePotency = 600
    };

    #endregion

    #region Movement

    /// <summary>
    /// Icarus - Gap closer to party member.
    /// </summary>
    public static readonly ActionDefinition Icarus = new()
    {
        ActionId = 24295,
        Name = "Icarus",
        MinLevel = 40,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Movement,
        CastTime = 0f,
        RecastTime = 45f,
        Range = 25f,
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

    public static readonly ActionDefinition Egeiro = new()
    {
        ActionId = 24287,
        Name = "Egeiro",
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
    /// All SGE GCD damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DamageGcds =
    {
        DosisIII, DosisII, Dosis
    };

    /// <summary>
    /// All SGE GCD DoT spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] DotGcds =
    {
        EukrasianDosisIII, EukrasianDosisII, EukrasianDosis
    };

    /// <summary>
    /// All SGE GCD single-target heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] SingleHealGcds =
    {
        EukrasianDiagnosis, Diagnosis
    };

    /// <summary>
    /// All SGE GCD AoE heals in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEHealGcds =
    {
        EukrasianPrognosisII, EukrasianPrognosis, Prognosis
    };

    /// <summary>
    /// All SGE GCD AoE damage spells in level order (highest first).
    /// </summary>
    public static readonly ActionDefinition[] AoEDamageGcds =
    {
        DyskrasiaII, Dyskrasia
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
        return Dosis;
    }

    /// <summary>
    /// Gets the appropriate DoT for the player's level.
    /// Requires Eukrasia to be active.
    /// </summary>
    public static ActionDefinition GetDotForLevel(byte level)
    {
        foreach (var action in DotGcds)
        {
            if (level >= action.MinLevel)
                return action;
        }
        return EukrasianDosis;
    }

    /// <summary>
    /// Gets the DoT status ID for the player's level.
    /// </summary>
    public static uint GetDotStatusId(byte level)
    {
        if (level >= EukrasianDosisIII.MinLevel)
            return EukrasianDosisIII.AppliedStatusId ?? 2616;
        if (level >= EukrasianDosisII.MinLevel)
            return EukrasianDosisII.AppliedStatusId ?? 2615;
        return EukrasianDosis.AppliedStatusId ?? 2614;
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
        return Diagnosis;
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
        return Prognosis;
    }

    /// <summary>
    /// Gets the appropriate AoE damage GCD for the player's level.
    /// Returns null if player level is below Dyskrasia (level 46).
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
    /// Gets the appropriate Phlegma for the player's level.
    /// Returns null if below level 26.
    /// </summary>
    public static ActionDefinition? GetPhlegmaForLevel(byte level)
    {
        if (level >= PhlegmaIII.MinLevel) return PhlegmaIII;
        if (level >= PhlegmaII.MinLevel) return PhlegmaII;
        if (level >= Phlegma.MinLevel) return Phlegma;
        return null;
    }

    /// <summary>
    /// Gets the appropriate Toxikon for the player's level.
    /// Returns null if below level 66.
    /// </summary>
    public static ActionDefinition? GetToxikonForLevel(byte level)
    {
        if (level >= ToxikonII.MinLevel) return ToxikonII;
        if (level >= Toxikon.MinLevel) return Toxikon;
        return null;
    }

    #endregion

    #region Status IDs

    /// <summary>
    /// Kardion (Kardia target) status ID.
    /// </summary>
    public const ushort KardionStatusId = 2604;

    /// <summary>
    /// Kardia (self buff indicating Kardia is placed) status ID.
    /// </summary>
    public const ushort KardiaStatusId = 2605;

    /// <summary>
    /// Eukrasia buff status ID.
    /// </summary>
    public const ushort EukrasiaStatusId = 2606;

    /// <summary>
    /// Eukrasian Diagnosis shield status ID.
    /// </summary>
    public const ushort EukrasianDiagnosisStatusId = 2607;

    /// <summary>
    /// Eukrasian Prognosis shield status ID.
    /// </summary>
    public const ushort EukrasianPrognosisStatusId = 2609;

    /// <summary>
    /// Soteria buff status ID.
    /// </summary>
    public const ushort SoteriaStatusId = 2610;

    /// <summary>
    /// Zoe buff status ID.
    /// </summary>
    public const ushort ZoeStatusId = 2611;

    /// <summary>
    /// Panhaima status ID.
    /// </summary>
    public const ushort PanhaimaStatusId = 2613;

    /// <summary>
    /// Eukrasian Dosis DoT status ID.
    /// </summary>
    public const ushort EukrasianDosisStatusId = 2614;

    /// <summary>
    /// Eukrasian Dosis II DoT status ID.
    /// </summary>
    public const ushort EukrasianDosisIIStatusId = 2615;

    /// <summary>
    /// Eukrasian Dosis III DoT status ID.
    /// </summary>
    public const ushort EukrasianDosisIIIStatusId = 2616;

    /// <summary>
    /// Kerachole/Taurochole mitigation buff status ID.
    /// </summary>
    public const ushort KeracholeStatusId = 2618;

    /// <summary>
    /// Physis II HoT + healing boost status ID.
    /// </summary>
    public const ushort PhysisIIStatusId = 2620;

    /// <summary>
    /// Haima status ID.
    /// </summary>
    public const ushort HaimaStatusId = 2621;

    /// <summary>
    /// Krasis buff status ID.
    /// </summary>
    public const ushort KrasisStatusId = 2622;

    /// <summary>
    /// Holos mitigation buff status ID.
    /// </summary>
    public const ushort HolosStatusId = 3003;

    /// <summary>
    /// Philosophia status ID.
    /// </summary>
    public const ushort PhilosophiaStatusId = 3898;

    /// <summary>
    /// Addersting buff status ID (indicates available Toxikon charges).
    /// </summary>
    public const ushort AdderstingStatusId = 2604;

    #endregion
}
