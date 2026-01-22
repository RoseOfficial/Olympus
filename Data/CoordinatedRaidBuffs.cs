using System.Collections.Generic;

namespace Olympus.Data;

/// <summary>
/// Registry of DPS raid buffs that benefit from synchronization between Olympus instances.
/// Unlike defensive mitigations (which should be staggered), raid buffs are most effective
/// when used simultaneously to maximize party burst damage.
/// </summary>
public static class CoordinatedRaidBuffs
{
    /// <summary>
    /// Party-wide raid buffs that should be synchronized.
    /// These abilities benefit the entire party and are most effective when stacked.
    /// </summary>
    public static readonly HashSet<uint> PartyRaidBuffs = new()
    {
        // Dragoon
        DRGActions.BattleLitany.ActionId,   // 3557 - +10% crit rate (120s CD, 20s duration)

        // Bard
        BRDActions.BattleVoice.ActionId,    // 118 - +20% DH rate (120s CD, 15s duration)
        BRDActions.RadiantFinale.ActionId,  // 25785 - +2-6% damage (110s CD, 20s duration)

        // Summoner
        SMNActions.SearingLight.ActionId,   // 25801 - +5% damage (120s CD, 20s duration)

        // Red Mage
        RDMActions.Embolden.ActionId,       // 7520 - +5% damage (120s CD, 20s duration)

        // Dancer
        DNCActions.TechnicalFinish.ActionId, // 16004 - +5% damage (120s CD, 20s duration)

        // Reaper
        RPRActions.ArcaneCircle.ActionId,   // 24405 - +3% damage (120s CD, 20s duration)

        // Monk
        MNKActions.Brotherhood.ActionId,    // 7396 - +5% damage (120s CD, 20s duration)

        // Pictomancer
        PCTActions.StarryMuse.ActionId,     // 34675 - +5% damage (120s CD, 20s duration)
    };

    /// <summary>
    /// Maps action IDs to their recast time in milliseconds.
    /// Used when recast time isn't available from the game API.
    /// </summary>
    public static readonly Dictionary<uint, int> DefaultRecastTimes = new()
    {
        { DRGActions.BattleLitany.ActionId, 120_000 },   // 2 minutes
        { BRDActions.BattleVoice.ActionId, 120_000 },    // 2 minutes
        { BRDActions.RadiantFinale.ActionId, 110_000 },  // 110 seconds
        { SMNActions.SearingLight.ActionId, 120_000 },   // 2 minutes
        { RDMActions.Embolden.ActionId, 120_000 },       // 2 minutes
        { DNCActions.TechnicalFinish.ActionId, 120_000 }, // 2 minutes
        { RPRActions.ArcaneCircle.ActionId, 120_000 },   // 2 minutes
        { MNKActions.Brotherhood.ActionId, 120_000 },    // 2 minutes
        { PCTActions.StarryMuse.ActionId, 120_000 },      // 2 minutes
    };

    /// <summary>
    /// Maps action IDs to their buff duration in seconds.
    /// Used for calculating burst window timing.
    /// </summary>
    public static readonly Dictionary<uint, float> BuffDurations = new()
    {
        { DRGActions.BattleLitany.ActionId, 20f },
        { BRDActions.BattleVoice.ActionId, 15f },
        { BRDActions.RadiantFinale.ActionId, 20f },
        { SMNActions.SearingLight.ActionId, 20f },
        { RDMActions.Embolden.ActionId, 20f },
        { DNCActions.TechnicalFinish.ActionId, 20f },
        { RPRActions.ArcaneCircle.ActionId, 20f },
        { MNKActions.Brotherhood.ActionId, 20f },
        { PCTActions.StarryMuse.ActionId, 20f },
    };

    /// <summary>
    /// Checks if an action is a coordinated raid buff that should be synchronized.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if this action should be synchronized between instances.</returns>
    public static bool IsCoordinatedRaidBuff(uint actionId)
    {
        return PartyRaidBuffs.Contains(actionId);
    }

    /// <summary>
    /// Gets the default recast time for a raid buff action.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>Recast time in milliseconds, or 120000 (2 min) as default.</returns>
    public static int GetDefaultRecastTime(uint actionId)
    {
        return DefaultRecastTimes.TryGetValue(actionId, out var recast) ? recast : 120_000;
    }

    /// <summary>
    /// Gets the buff duration for a raid buff action.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>Buff duration in seconds, or 20f as default.</returns>
    public static float GetBuffDuration(uint actionId)
    {
        return BuffDurations.TryGetValue(actionId, out var duration) ? duration : 20f;
    }

    /// <summary>
    /// Gets information about a raid buff.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>RaidBuffInfo if the action is a coordinated raid buff, null otherwise.</returns>
    public static RaidBuffInfo? GetBuffInfo(uint actionId)
    {
        if (!IsCoordinatedRaidBuff(actionId))
            return null;

        return new RaidBuffInfo
        {
            ActionId = actionId,
            RecastTimeMs = GetDefaultRecastTime(actionId),
            DurationSeconds = GetBuffDuration(actionId)
        };
    }
}

/// <summary>
/// Information about a coordinated raid buff.
/// </summary>
public readonly struct RaidBuffInfo
{
    /// <summary>Action ID of the buff.</summary>
    public uint ActionId { get; init; }

    /// <summary>Recast time in milliseconds.</summary>
    public int RecastTimeMs { get; init; }

    /// <summary>Buff duration in seconds.</summary>
    public float DurationSeconds { get; init; }
}
