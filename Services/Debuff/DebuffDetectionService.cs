using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Olympus.Services.Debuff;

/// <summary>
/// Priority tiers for cleansable debuffs (lower number = higher priority).
/// </summary>
public enum DebuffPriority
{
    /// <summary>Lethal debuffs (Doom, Throttle) - cleanse immediately or player dies.</summary>
    Lethal = 0,

    /// <summary>High priority (Vulnerability Up before tankbusters).</summary>
    High = 1,

    /// <summary>Medium priority (Paralysis, Silence, Pacification).</summary>
    Medium = 2,

    /// <summary>Low priority (Bind, Heavy, Blind).</summary>
    Low = 3,

    /// <summary>Not prioritized / not cleansable.</summary>
    None = int.MaxValue
}

/// <summary>
/// Service for detecting and prioritizing cleansable debuffs on party members.
/// Uses Lumina Excel data to determine if a status is dispellable.
/// </summary>
public sealed class DebuffDetectionService : IDebuffDetectionService
{
    private readonly IDataManager _dataManager;

    // Lethal debuffs - must be cleansed immediately or player dies
    private static readonly HashSet<uint> LethalDebuffs = new()
    {
        910,   // Doom (common version)
        1769,  // Throttle
        2519,  // Doom (Bozjan version)
        3364,  // Doom (variant dungeon version)
    };

    // High priority - significant combat impact
    private static readonly HashSet<uint> HighPriorityDebuffs = new()
    {
        714,   // Vulnerability Up
        638,   // Damage Down
        1195,  // Vulnerability Up (alternate)
    };

    // Medium priority - impairs actions
    private static readonly HashSet<uint> MediumPriorityDebuffs = new()
    {
        17,    // Paralysis
        7,     // Silence
        6,     // Pacification
        3,     // Sleep
        18,    // Stun (if cleansable)
    };

    // Low priority - movement/utility impairment
    private static readonly HashSet<uint> LowPriorityDebuffs = new()
    {
        13,    // Bind
        14,    // Heavy
        15,    // Blind
        564,   // Leaden (movement debuff)
    };

    public DebuffDetectionService(IDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    /// <summary>
    /// Checks if a status effect can be cleansed by Esuna.
    /// Uses the CanDispel flag from the game's Status excel sheet.
    /// </summary>
    public bool IsDispellable(uint statusId)
    {
        var statusSheet = _dataManager.GetExcelSheet<Status>();
        if (statusSheet == null)
            return false;

        var status = statusSheet.GetRowOrDefault(statusId);
        return status.HasValue && status.Value.CanDispel;
    }

    /// <summary>
    /// Gets the priority tier for a dispellable debuff.
    /// </summary>
    public DebuffPriority GetDebuffPriority(uint statusId)
    {
        if (LethalDebuffs.Contains(statusId))
            return DebuffPriority.Lethal;
        if (HighPriorityDebuffs.Contains(statusId))
            return DebuffPriority.High;
        if (MediumPriorityDebuffs.Contains(statusId))
            return DebuffPriority.Medium;
        if (LowPriorityDebuffs.Contains(statusId))
            return DebuffPriority.Low;

        // Unknown dispellable debuff - treat as low priority
        return DebuffPriority.Low;
    }

    /// <summary>
    /// Finds the highest priority dispellable debuff on a target.
    /// Returns the status ID, priority, and remaining time.
    /// </summary>
    /// <param name="target">The battle character to scan for debuffs.</param>
    /// <returns>Tuple of (statusId, priority, remainingTime). Returns (0, None, 0) if no dispellable debuff found.</returns>
    public (uint statusId, DebuffPriority priority, float remainingTime) FindHighestPriorityDebuff(IBattleChara target)
    {
        uint bestStatusId = 0;
        var bestPriority = DebuffPriority.None;
        float bestRemainingTime = float.MaxValue;

        foreach (var status in target.StatusList)
        {
            // Skip if not dispellable
            if (!IsDispellable(status.StatusId))
                continue;

            var priority = GetDebuffPriority(status.StatusId);

            // Take higher priority (lower enum value), or same priority with less time remaining
            if (priority < bestPriority ||
                (priority == bestPriority && status.RemainingTime < bestRemainingTime))
            {
                bestStatusId = status.StatusId;
                bestPriority = priority;
                bestRemainingTime = status.RemainingTime;
            }
        }

        return (bestStatusId, bestPriority, bestRemainingTime);
    }
}
