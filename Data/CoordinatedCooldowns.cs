using System.Collections.Generic;

namespace Olympus.Data;

/// <summary>
/// Registry of cooldowns that should be coordinated between Olympus instances.
/// These are party-wide defensive abilities that benefit from not being stacked.
/// </summary>
public static class CoordinatedCooldowns
{
    /// <summary>
    /// Party-wide mitigation cooldowns that should be coordinated.
    /// These abilities affect multiple party members and are most effective when staggered.
    /// </summary>
    public static readonly HashSet<uint> PartyMitigations = new()
    {
        // Tank Role Actions
        ActionIds.Reprisal,                 // All tanks - 10% damage reduction to enemies

        // Paladin
        ActionIds.DivineVeil,               // Party shield (requires heal trigger)
        ActionIds.PassageOfArms,            // 15% party mitigation (channeled)

        // Warrior
        ActionIds.ShakeItOff,               // Party barrier + self cleanse

        // Dark Knight (using literal IDs since not all are in ActionIds.cs)
        36927,                              // Dark Missionary - 10% magic damage reduction

        // Gunbreaker
        36934,                              // Heart of Light - 10% magic damage reduction

        // White Mage
        ActionIds.Temperance,               // 10% mitigation + healing boost
        ActionIds.LiturgyOfTheBell,         // Reactive party healing

        // Scholar
        ActionIds.SacredSoil,               // 10% mitigation zone
        ActionIds.Expedient,                // Movement speed + 10% mitigation

        // Astrologian
        ActionIds.CollectiveUnconscious,    // 10% mitigation (channeled)
        ActionIds.NeutralSect,              // Healing boost + shields
        ActionIds.Macrocosmos,              // Delayed party heal

        // Sage
        ActionIds.Panhaima,                 // Party shields (multi-stack)
        ActionIds.Holos,                    // 10% mitigation + party heal
    };

    /// <summary>
    /// Personal defensive cooldowns that tanks should coordinate.
    /// When two tanks run Olympus, they stagger major mitigations to maximize uptime.
    /// </summary>
    public static readonly HashSet<uint> PersonalDefensives = new()
    {
        // Role Action (all tanks)
        ActionIds.Rampart,                  // 20% mit, 90s CD

        // Paladin
        ActionIds.Sentinel,                 // 30% mit, 120s CD
        ActionIds.Guardian,                 // Sentinel upgrade at 92

        // Warrior
        ActionIds.Vengeance,                // 30% mit, 120s CD
        ActionIds.Damnation,                // Vengeance upgrade at 92
        ActionIds.Bloodwhetting,            // 10% mit + heal, 25s CD

        // Dark Knight
        ActionIds.ShadowWall,               // 30% mit, 120s CD
        ActionIds.ShadowedVigil,            // Shadow Wall upgrade at 92

        // Gunbreaker
        ActionIds.Nebula,                   // 30% mit, 120s CD
        ActionIds.GreatNebula,              // Nebula upgrade at 92
    };

    /// <summary>
    /// Tank invulnerability abilities that should be coordinated.
    /// When two tanks run Olympus, they avoid using invulns simultaneously.
    /// </summary>
    public static readonly HashSet<uint> Invulnerabilities = new()
    {
        ActionIds.HallowedGround,           // PLD - 420s CD
        ActionIds.Holmgang,                 // WAR - 240s CD
        ActionIds.LivingDead,               // DRK - 300s CD
        ActionIds.Superbolide,              // GNB - 360s CD
    };

    /// <summary>
    /// Checks if an action is a personal defensive that should be coordinated between tanks.
    /// </summary>
    public static bool IsPersonalDefensive(uint actionId)
        => PersonalDefensives.Contains(actionId);

    /// <summary>
    /// Checks if an action is a tank invulnerability that should be coordinated.
    /// </summary>
    public static bool IsInvulnerability(uint actionId)
        => Invulnerabilities.Contains(actionId);

    /// <summary>
    /// Maps action IDs to their recast time in milliseconds.
    /// Used when recast time isn't available from the game API.
    /// </summary>
    public static readonly Dictionary<uint, int> DefaultRecastTimes = new()
    {
        // Tank cooldowns
        { ActionIds.Reprisal, 60_000 },
        { ActionIds.DivineVeil, 90_000 },
        { ActionIds.PassageOfArms, 120_000 },
        { ActionIds.ShakeItOff, 90_000 },
        { 36927, 90_000 },                  // Dark Missionary
        { 36934, 90_000 },                  // Heart of Light

        // Healer cooldowns
        { ActionIds.Temperance, 120_000 },
        { ActionIds.LiturgyOfTheBell, 180_000 },
        { ActionIds.SacredSoil, 30_000 },
        { ActionIds.Expedient, 120_000 },
        { ActionIds.CollectiveUnconscious, 60_000 },
        { ActionIds.NeutralSect, 120_000 },
        { ActionIds.Macrocosmos, 180_000 },
        { ActionIds.Panhaima, 120_000 },
        { ActionIds.Holos, 120_000 },

        // Personal defensive cooldowns (for tank coordination)
        { ActionIds.Rampart, 90_000 },
        { ActionIds.Sentinel, 120_000 },
        { ActionIds.Guardian, 120_000 },
        { ActionIds.Vengeance, 120_000 },
        { ActionIds.Damnation, 120_000 },
        { ActionIds.Bloodwhetting, 25_000 },
        { ActionIds.ShadowWall, 120_000 },
        { ActionIds.ShadowedVigil, 120_000 },
        { ActionIds.Nebula, 120_000 },
        { ActionIds.GreatNebula, 120_000 },

        // Tank invulnerability cooldowns
        { ActionIds.HallowedGround, 420_000 },  // PLD - 7 min
        { ActionIds.Holmgang, 240_000 },        // WAR - 4 min
        { ActionIds.LivingDead, 300_000 },      // DRK - 5 min
        { ActionIds.Superbolide, 360_000 },     // GNB - 6 min
    };

    /// <summary>
    /// Checks if an action is a coordinated cooldown that should be tracked.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if this action should be coordinated between instances.</returns>
    public static bool IsCoordinatedCooldown(uint actionId)
    {
        return PartyMitigations.Contains(actionId);
    }

    /// <summary>
    /// Gets the default recast time for an action.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>Recast time in milliseconds, or 120000 (2 min) as default.</returns>
    public static int GetDefaultRecastTime(uint actionId)
    {
        return DefaultRecastTimes.TryGetValue(actionId, out var recast) ? recast : 120_000;
    }
}
