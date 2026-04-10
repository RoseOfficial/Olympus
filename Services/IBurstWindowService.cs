using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services;

/// <summary>
/// Detects and tracks raid buff burst windows for DPS resource pooling.
/// Combines local status effect scanning with IPC data from PartyCoordinationService.
/// </summary>
public interface IBurstWindowService
{
    /// <summary>
    /// Updates burst window state for the current frame.
    /// Call once per frame from the DPS base class.
    /// When <paramref name="currentTarget"/> is provided, also checks the target's
    /// status list for raid debuffs (Chain Stratagem, Dokumori, VulnerabilityUp).
    /// </summary>
    void Update(IPlayerCharacter player, IBattleChara? currentTarget = null);

    /// <summary>
    /// Whether party raid buffs are currently active on the player.
    /// </summary>
    bool IsInBurstWindow { get; }

    /// <summary>
    /// Seconds remaining in the current burst window.
    /// Returns 0 if not in a burst window.
    /// </summary>
    float SecondsRemainingInBurst { get; }

    /// <summary>
    /// Whether a burst window is imminent (starting within the given threshold).
    /// </summary>
    /// <param name="thresholdSeconds">Seconds ahead to consider "imminent" (default 5s).</param>
    bool IsBurstImminent(float thresholdSeconds = 5f);

    /// <summary>
    /// Seconds until the next burst window.
    /// Returns 0 if currently active, -1 if unknown.
    /// </summary>
    float SecondsUntilNextBurst { get; }

    /// <summary>
    /// History of burst windows recorded during the current fight.
    /// Each entry is a (Start, End) pair in UTC.
    /// </summary>
    IReadOnlyList<(DateTime Start, DateTime End)> BurstWindowHistory { get; }

    /// <summary>
    /// Resets burst window history and transition tracking state.
    /// Call when starting a new fight or clearing analytics data.
    /// </summary>
    void ResetHistory();
}
