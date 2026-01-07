using System;

namespace Olympus.Models;

/// <summary>
/// Represents an active mitigation buff on a party member.
/// Mitigation buffs reduce incoming damage by a percentage.
/// </summary>
public sealed record MitigationBuff
{
    /// <summary>
    /// The entity ID of the target with this mitigation.
    /// </summary>
    public uint TargetId { get; init; }

    /// <summary>
    /// The status ID of the mitigation effect.
    /// </summary>
    public uint StatusId { get; init; }

    /// <summary>
    /// The damage reduction percentage (0.0 to 1.0).
    /// e.g., 0.10 = 10% damage reduction.
    /// </summary>
    public float MitigationPercent { get; init; }

    /// <summary>
    /// The remaining duration of the mitigation in seconds.
    /// </summary>
    public float RemainingDuration { get; init; }

    /// <summary>
    /// When this mitigation was applied.
    /// </summary>
    public DateTime AppliedTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The source entity ID (who applied the mitigation).
    /// </summary>
    public uint SourceId { get; init; }

    /// <summary>
    /// Display name of the mitigation effect.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Whether this mitigation affects only the target (true)
    /// or all party members in range (false = party-wide).
    /// </summary>
    public bool IsSelfOnly { get; init; }
}

/// <summary>
/// Known mitigation status IDs and their damage reduction percentages.
/// </summary>
public static class MitigationStatusIds
{
    // Role actions (available to all)
    public const uint Rampart = 1191;           // 20% mitigation
    public const uint Reprisal = 1193;          // 10% enemy damage down (party-wide effect)
    public const uint Addle = 1203;             // 10% magic damage down on enemy
    public const uint Feint = 1195;             // 10% physical damage down on enemy

    // Paladin
    public const uint Sentinel = 74;            // 30% mitigation
    public const uint HallowedGround = 82;      // Invulnerable
    public const uint Bulwark = 77;             // Block rate up
    public const uint DivineVeil = 727;         // Party shield when healed
    public const uint PassageOfArms = 1175;     // 15% party mitigation
    public const uint GuardianJTitan = 2883;    // 15% mitigation (from Guardian)

    // Warrior
    public const uint Vengeance = 89;           // 30% mitigation + thorns
    public const uint Holmgang = 409;           // Cannot drop below 1 HP
    public const uint ThrillOfBattle = 87;      // Max HP increase
    public const uint ShakeItOff = 1457;        // Party shield + dispel
    public const uint Bloodwhetting = 2678;     // 10% mitigation + heal

    // Dark Knight
    public const uint ShadowWall = 747;         // 30% mitigation
    public const uint DarkMind = 746;           // 20% magic mitigation
    public const uint LivingDead = 810;         // Invuln phase
    public const uint WalkingDead = 811;        // Living Dead active
    public const uint DarkMissionary = 1894;    // 10% party magic mitigation
    public const uint Oblation = 2682;          // 10% mitigation

    // Gunbreaker
    public const uint Nebula = 1834;            // 30% mitigation
    public const uint Superbolide = 1836;       // Invulnerable at 1 HP
    public const uint HeartOfLight = 1839;      // 10% party magic mitigation
    public const uint HeartOfStone = 1840;      // 15% mitigation
    public const uint Aurora = 1835;            // HoT (not mitigation but defensive)

    // Healer mitigation
    public const uint Temperance = 1872;        // WHM - 10% party mitigation
    public const uint Expedient = 2634;         // SCH - 10% party mitigation + speed
    public const uint Kerachole = 2618;         // SGE - 10% party mitigation
    public const uint Taurochole = 2619;        // SGE - 10% mitigation
    public const uint Holos = 3003;             // SGE - 10% party mitigation
    public const uint Panhaima = 2613;          // SGE - stacking shields
    public const uint CollectiveUnconscious = 849; // AST - 10% party mitigation

    // DPS mitigation (less common)
    public const uint Troubadour = 1934;        // BRD - 10% party mitigation
    public const uint Tactician = 1951;         // MCH - 10% party mitigation
    public const uint ShieldSamba = 1826;       // DNC - 10% party mitigation
    public const uint Magick_Barrier = 2707;    // RDM - 10% party mitigation
}

/// <summary>
/// Helper class to get mitigation percentages for known buffs.
/// </summary>
public static class MitigationValues
{
    /// <summary>
    /// Gets the mitigation percentage for a known status ID.
    /// Returns 0 if the status is not a known mitigation buff.
    /// </summary>
    public static float GetMitigationPercent(uint statusId) => statusId switch
    {
        // Role actions
        MitigationStatusIds.Rampart => 0.20f,
        MitigationStatusIds.Reprisal => 0.10f,
        MitigationStatusIds.Addle => 0.10f,
        MitigationStatusIds.Feint => 0.10f,

        // Paladin
        MitigationStatusIds.Sentinel => 0.30f,
        MitigationStatusIds.HallowedGround => 1.00f,  // Full immunity
        MitigationStatusIds.PassageOfArms => 0.15f,
        MitigationStatusIds.GuardianJTitan => 0.15f,
        MitigationStatusIds.DivineVeil => 0.00f,      // Shield, not mitigation

        // Warrior
        MitigationStatusIds.Vengeance => 0.30f,
        MitigationStatusIds.Holmgang => 1.00f,        // Cannot die
        MitigationStatusIds.Bloodwhetting => 0.10f,

        // Dark Knight
        MitigationStatusIds.ShadowWall => 0.30f,
        MitigationStatusIds.DarkMind => 0.20f,
        MitigationStatusIds.LivingDead => 1.00f,      // Invuln phase
        MitigationStatusIds.WalkingDead => 1.00f,
        MitigationStatusIds.DarkMissionary => 0.10f,
        MitigationStatusIds.Oblation => 0.10f,

        // Gunbreaker
        MitigationStatusIds.Nebula => 0.30f,
        MitigationStatusIds.Superbolide => 1.00f,     // Invulnerable
        MitigationStatusIds.HeartOfLight => 0.10f,
        MitigationStatusIds.HeartOfStone => 0.15f,

        // Healer mitigation
        MitigationStatusIds.Temperance => 0.10f,
        MitigationStatusIds.Expedient => 0.10f,
        MitigationStatusIds.Kerachole => 0.10f,
        MitigationStatusIds.Taurochole => 0.10f,
        MitigationStatusIds.Holos => 0.10f,
        MitigationStatusIds.CollectiveUnconscious => 0.10f,

        // DPS mitigation
        MitigationStatusIds.Troubadour => 0.10f,
        MitigationStatusIds.Tactician => 0.10f,
        MitigationStatusIds.ShieldSamba => 0.10f,
        MitigationStatusIds.Magick_Barrier => 0.10f,

        _ => 0f
    };

    /// <summary>
    /// Gets the display name for a known mitigation status.
    /// </summary>
    public static string GetMitigationName(uint statusId) => statusId switch
    {
        MitigationStatusIds.Rampart => "Rampart",
        MitigationStatusIds.Reprisal => "Reprisal",
        MitigationStatusIds.Addle => "Addle",
        MitigationStatusIds.Feint => "Feint",
        MitigationStatusIds.Sentinel => "Sentinel",
        MitigationStatusIds.HallowedGround => "Hallowed Ground",
        MitigationStatusIds.Vengeance => "Vengeance",
        MitigationStatusIds.Holmgang => "Holmgang",
        MitigationStatusIds.ShadowWall => "Shadow Wall",
        MitigationStatusIds.DarkMind => "Dark Mind",
        MitigationStatusIds.LivingDead => "Living Dead",
        MitigationStatusIds.Nebula => "Nebula",
        MitigationStatusIds.Superbolide => "Superbolide",
        MitigationStatusIds.Temperance => "Temperance",
        MitigationStatusIds.Expedient => "Expedient",
        MitigationStatusIds.Kerachole => "Kerachole",
        MitigationStatusIds.CollectiveUnconscious => "Collective Unconscious",
        MitigationStatusIds.Troubadour => "Troubadour",
        MitigationStatusIds.Tactician => "Tactician",
        MitigationStatusIds.ShieldSamba => "Shield Samba",
        MitigationStatusIds.Magick_Barrier => "Magick Barrier",
        _ => $"Unknown ({statusId})"
    };
}
