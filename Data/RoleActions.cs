using Olympus.Models.Action;

namespace Olympus.Data;

/// <summary>
/// Shared role actions available to multiple jobs.
/// Consolidated from per-job action files to eliminate duplication.
/// </summary>
public static class RoleActions
{
    // -------------------------------------------------------------------------
    // Healer Role Actions
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Resurrection spells — each healer/caster has a unique spell with different
    // ActionId, so these are kept with job-specific names.
    // -------------------------------------------------------------------------

    /// <summary>WHM/CNJ resurrection.</summary>
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

    /// <summary>SCH/SMN Arcanist-line resurrection.</summary>
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

    /// <summary>SGE resurrection.</summary>
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

    /// <summary>AST resurrection.</summary>
    public static readonly ActionDefinition Ascend = new()
    {
        ActionId = 3603,
        Name = "Ascend",
        MinLevel = 12,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.Raise,
        CastTime = 8.0f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 2400
    };

    // -------------------------------------------------------------------------
    // Caster Role Actions (BLM, SMN, RDM, PCT — overlapping with healer role)
    // -------------------------------------------------------------------------

    /// <summary>Target magic damage reduction debuff (Lv.8) — caster role.</summary>
    public static readonly ActionDefinition Addle = new()
    {
        ActionId = 7560,
        Name = "Addle",
        MinLevel = 8,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 25f,
        MpCost = 0,
        AppliedStatusId = 1203,
        AppliedStatusDuration = 15f
    };

    // BLM-exclusive — not available to SMN, RDM, or PCT.
    // Listed here because it was historically in the caster role action group; do not use generically.
    /// <summary>Sleep — put target to sleep (Lv.10) — BLM-only caster role action.</summary>
    public static readonly ActionDefinition Sleep = new()
    {
        ActionId = 25880,
        Name = "Sleep",
        MinLevel = 10,
        Category = ActionCategory.GCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 2.5f,
        RecastTime = 2.5f,
        Range = 30f,
        MpCost = 800,
        AppliedStatusId = 3,
        AppliedStatusDuration = 30f
    };

    // -------------------------------------------------------------------------
    // Melee DPS Role Actions
    // -------------------------------------------------------------------------

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

    public static readonly ActionDefinition Bloodbath = new()
    {
        ActionId = 7542,
        Name = "Bloodbath",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = 84,
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition Feint = new()
    {
        ActionId = 7549,
        Name = "Feint",
        MinLevel = 22,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 90f,
        Range = 10f,
        MpCost = 0,
        AppliedStatusId = 1195,
        AppliedStatusDuration = 15f
    };

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
        AppliedStatusId = 1209,
        AppliedStatusDuration = 6f
    };

    public static readonly ActionDefinition TrueNorth = new()
    {
        ActionId = 7546,
        Name = "True North",
        MinLevel = 50,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 45f, // 2 charges
        MpCost = 0,
        AppliedStatusId = 1250,
        AppliedStatusDuration = 10f
    };

    public static readonly ActionDefinition LegSweep = new()
    {
        ActionId = 7863,
        Name = "Leg Sweep",
        MinLevel = 10,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 40f,
        Range = 3f,
        MpCost = 0
    };

    // -------------------------------------------------------------------------
    // Physical Ranged DPS Role Actions (BRD, MCH, DNC)
    // -------------------------------------------------------------------------

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
        AppliedStatusId = 1199,
        AppliedStatusDuration = 30f
    };

    // -------------------------------------------------------------------------
    // Tank Role Actions
    // -------------------------------------------------------------------------

    public static readonly ActionDefinition Rampart = new()
    {
        ActionId = 7531,
        Name = "Rampart",
        MinLevel = 8,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.Self,
        EffectTypes = ActionEffectType.Buff,
        CastTime = 0f,
        RecastTime = 90f,
        MpCost = 0,
        AppliedStatusId = 1191,
        AppliedStatusDuration = 20f
    };

    public static readonly ActionDefinition Reprisal = new()
    {
        ActionId = 7535,
        Name = "Reprisal",
        MinLevel = 22,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.Debuff,
        CastTime = 0f,
        RecastTime = 60f,
        Range = 25f,
        Radius = 5f,
        MpCost = 0,
        AppliedStatusId = 1193,
        AppliedStatusDuration = 10f
    };

    public static readonly ActionDefinition Provoke = new()
    {
        ActionId = 7533,
        Name = "Provoke",
        MinLevel = 15,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 25f,
        MpCost = 0
    };

    public static readonly ActionDefinition Shirk = new()
    {
        ActionId = 7537,
        Name = "Shirk",
        MinLevel = 48,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleAlly,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 120f,
        Range = 25f,
        MpCost = 0
    };

    public static readonly ActionDefinition LowBlow = new()
    {
        ActionId = 7540,
        Name = "Low Blow",
        MinLevel = 12,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 25f,
        Range = 3f,
        MpCost = 0
    };

    public static readonly ActionDefinition Interject = new()
    {
        ActionId = 7538,
        Name = "Interject",
        MinLevel = 18,
        Category = ActionCategory.oGCD,
        TargetType = ActionTargetType.SingleEnemy,
        EffectTypes = ActionEffectType.None,
        CastTime = 0f,
        RecastTime = 30f,
        Range = 3f,
        MpCost = 0
    };
}
