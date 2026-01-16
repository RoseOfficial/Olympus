using Olympus.Rotation.Common;
using Olympus.Rotation.AresCore.Helpers;

namespace Olympus.Rotation.AresCore.Context;

/// <summary>
/// Warrior-specific rotation context interface.
/// Extends ITankRotationContext with Warrior-specific state.
/// </summary>
public interface IAresContext : ITankRotationContext
{
    #region Warrior Gauge

    /// <summary>
    /// Current Beast Gauge value (0-100).
    /// Used for Fell Cleave/Steel Cyclone.
    /// </summary>
    int BeastGauge { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Defiance (tank stance) is currently active.
    /// </summary>
    bool HasDefiance { get; }

    /// <summary>
    /// Whether Surging Tempest (+10% damage) is currently active.
    /// </summary>
    bool HasSurgingTempest { get; }

    /// <summary>
    /// Remaining duration of Surging Tempest buff (seconds).
    /// </summary>
    float SurgingTempestRemaining { get; }

    /// <summary>
    /// Whether Inner Release is currently active.
    /// Beast Gauge abilities cost 0 and are guaranteed crit/direct hit.
    /// </summary>
    bool HasInnerRelease { get; }

    /// <summary>
    /// Remaining stacks of Inner Release.
    /// Each Fell Cleave/Decimate consumes one stack.
    /// </summary>
    int InnerReleaseStacks { get; }

    /// <summary>
    /// Whether Nascent Chaos is active (enables Inner Chaos/Chaotic Cyclone).
    /// </summary>
    bool HasNascentChaos { get; }

    /// <summary>
    /// Whether Primal Rend Ready buff is active.
    /// </summary>
    bool HasPrimalRendReady { get; }

    /// <summary>
    /// Whether Primal Ruination Ready buff is active.
    /// </summary>
    bool HasPrimalRuinationReady { get; }

    #endregion

    #region Defensive State

    /// <summary>
    /// Whether any defensive cooldown is currently active.
    /// </summary>
    bool HasActiveMitigation { get; }

    /// <summary>
    /// Whether Holmgang (cannot drop below 1 HP) is active.
    /// </summary>
    bool HasHolmgang { get; }

    /// <summary>
    /// Whether Vengeance/Damnation is active.
    /// </summary>
    bool HasVengeance { get; }

    /// <summary>
    /// Whether Bloodwhetting/Raw Intuition is active.
    /// </summary>
    bool HasBloodwhetting { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    AresStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    AresPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    AresDebugState Debug { get; }

    #endregion
}
