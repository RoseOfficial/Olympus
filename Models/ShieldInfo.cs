using System;

namespace Olympus.Models;

/// <summary>
/// Represents an active shield on a party member.
/// Shields absorb damage before HP is reduced.
/// </summary>
public sealed record ShieldInfo
{
    /// <summary>
    /// The entity ID of the target with this shield.
    /// </summary>
    public uint TargetId { get; init; }

    /// <summary>
    /// The status ID of the shield effect.
    /// </summary>
    public uint StatusId { get; init; }

    /// <summary>
    /// The estimated shield value in HP.
    /// This is calculated from the shield potency and source stats.
    /// </summary>
    public int ShieldValue { get; init; }

    /// <summary>
    /// The remaining duration of the shield in seconds.
    /// </summary>
    public float RemainingDuration { get; init; }

    /// <summary>
    /// When this shield was applied.
    /// </summary>
    public DateTime AppliedTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The source entity ID (who applied the shield).
    /// </summary>
    public uint SourceId { get; init; }

    /// <summary>
    /// Display name of the shield effect.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Whether this is a percentage-based shield (like Divine Benison)
    /// vs a flat value shield (like Galvanize).
    /// </summary>
    public bool IsPercentageBased { get; init; }
}

/// <summary>
/// Known shield status IDs from FFXIV.
/// </summary>
public static class ShieldStatusIds
{
    // White Mage shields
    public const uint DivineBenison = 1218;  // 15% max HP shield
    public const uint Aquaveil = 2708;       // Damage reduction (not technically a shield, but reduces damage)

    // Scholar shields
    public const uint Galvanize = 297;       // From Adloquium/Succor
    public const uint Catalyze = 1918;       // Crit Adlo shield
    public const uint SeraphicVeil = 1917;   // From Seraph
    public const uint SacredSoil = 299;      // Ground effect + regen (Lv78+)

    // Sage shields
    public const uint EukrasianDiagnosis = 2607;  // Single target barrier
    public const uint EukrasianPrognosis = 2609;  // AoE barrier
    public const uint Kerachole = 2618;           // Party mitigation + regen

    // Astrologian shields (technically not shields but damage reduction)
    public const uint CollectiveUnconscious = 849;  // Damage reduction
    public const uint CelestialOpposition = 1879;   // Heal + barrier (with AST trait)
    public const uint Exaltation = 2717;            // Single target damage reduction + heal

    // Tank self-shields
    public const uint Sheltron = 1856;       // PLD - blocks incoming damage
    public const uint HolySheltron = 2674;   // PLD - enhanced version
    public const uint Bloodwhetting = 2678;  // WAR - HP shield on damage
    public const uint StemTheFlow = 2677;    // WAR - from Raw Intuition at 82
    public const uint TheBlackestNight = 1178; // DRK - 25% HP shield
    public const uint HeartOfCorundum = 2683;  // GNB - damage reduction + heal
    public const uint Camouflage = 1832;       // GNB - parry rate up
}
