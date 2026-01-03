using System;

namespace Olympus.Models.Action;

/// <summary>
/// Categorizes actions for execution timing decisions.
/// </summary>
public enum ActionCategory
{
    /// <summary>
    /// Global Cooldown action (Stone, Cure, Medica, etc.)
    /// Triggers the 2.5s GCD timer.
    /// </summary>
    GCD,

    /// <summary>
    /// Off-Global Cooldown action (Assize, Benediction, etc.)
    /// Can be used during GCD animation lock window.
    /// </summary>
    oGCD,

    /// <summary>
    /// Ability that doesn't lock GCD and has no animation lock.
    /// (Sprint, Mount, etc.) - typically not used in combat rotation.
    /// </summary>
    Utility
}

/// <summary>
/// The type of effect an action has. Flags can be combined.
/// </summary>
[Flags]
public enum ActionEffectType
{
    None = 0,
    Damage = 1 << 0,
    Heal = 1 << 1,
    Shield = 1 << 2,
    Buff = 1 << 3,
    Debuff = 1 << 4,
    DoT = 1 << 5,
    HoT = 1 << 6,
    Cleanse = 1 << 7,
    Raise = 1 << 8,
    MpRestore = 1 << 9,
    Movement = 1 << 10
}

/// <summary>
/// How the action selects its target(s).
/// </summary>
public enum ActionTargetType
{
    /// <summary>Targets self only (Medica, Swiftcast).</summary>
    Self,

    /// <summary>Single ally target (Cure, Benediction).</summary>
    SingleAlly,

    /// <summary>Single enemy target (Stone, Dia).</summary>
    SingleEnemy,

    /// <summary>Party AoE centered on self (Medica, Assize).</summary>
    PartyAoE,

    /// <summary>Ground-targeted AoE (Asylum).</summary>
    GroundAoE,

    /// <summary>Cone AoE (some enemy abilities).</summary>
    Cone,

    /// <summary>Line AoE (some enemy abilities).</summary>
    Line
}

