using Olympus.Rotation.Common;
using Olympus.Rotation.PrometheusCore.Helpers;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.PrometheusCore.Context;

/// <summary>
/// Machinist-specific rotation context interface.
/// Extends IRangedDpsRotationContext with Machinist-specific state.
/// </summary>
public interface IPrometheusContext : IRangedDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Heat gauge (0-100).
    /// Built by weapon skills (+5 each), spent by Hypercharge (50).
    /// </summary>
    int Heat { get; }

    /// <summary>
    /// Current Battery gauge (0-100).
    /// Built by certain actions (+10-20), spent by Automaton Queen (50-100).
    /// </summary>
    int Battery { get; }

    /// <summary>
    /// Whether currently in Overheated state (can use Heat Blast).
    /// </summary>
    bool IsOverheated { get; }

    /// <summary>
    /// Remaining duration of Overheated state in seconds.
    /// </summary>
    float OverheatRemaining { get; }

    /// <summary>
    /// Whether Automaton Queen is currently active.
    /// </summary>
    bool IsQueenActive { get; }

    /// <summary>
    /// Remaining duration of Automaton Queen in seconds.
    /// </summary>
    float QueenRemaining { get; }

    /// <summary>
    /// Battery value used to summon the current/last Queen (affects damage).
    /// </summary>
    int LastQueenBattery { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Reassembled buff is active (next weaponskill guaranteed crit/DH).
    /// </summary>
    bool HasReassemble { get; }

    /// <summary>
    /// Remaining duration of Reassembled in seconds.
    /// </summary>
    float ReassembleRemaining { get; }

    /// <summary>
    /// Whether Hypercharged buff is active (from Barrel Stabilizer).
    /// </summary>
    bool HasHypercharged { get; }

    /// <summary>
    /// Whether Full Metal Machinist buff is active (can use Full Metal Field).
    /// </summary>
    bool HasFullMetalMachinist { get; }

    /// <summary>
    /// Whether Excavator Ready buff is active (can use Excavator).
    /// </summary>
    bool HasExcavatorReady { get; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Wildfire debuff is active on current target.
    /// </summary>
    bool HasWildfire { get; }

    /// <summary>
    /// Remaining duration of Wildfire on target in seconds.
    /// </summary>
    float WildfireRemaining { get; }

    /// <summary>
    /// Whether Bioblaster DoT is active on current target.
    /// </summary>
    bool HasBioblaster { get; }

    /// <summary>
    /// Remaining duration of Bioblaster DoT on target in seconds.
    /// </summary>
    float BioblasterRemaining { get; }

    #endregion

    #region Cooldown Tracking

    /// <summary>
    /// Charges of Drill available (1-2 at Lv.98+).
    /// </summary>
    int DrillCharges { get; }

    /// <summary>
    /// Charges of Reassemble available (1-2 at Lv.84+).
    /// </summary>
    int ReassembleCharges { get; }

    /// <summary>
    /// Charges of Gauss Round available (0-3).
    /// </summary>
    int GaussRoundCharges { get; }

    /// <summary>
    /// Charges of Ricochet available (0-3).
    /// </summary>
    int RicochetCharges { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    PrometheusStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    PrometheusPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    PrometheusDebugState Debug { get; }

    #endregion

    #region Party Coordination

    /// <summary>
    /// Service for coordinating burst windows with other Olympus instances.
    /// Null if party coordination is disabled or unavailable.
    /// </summary>
    IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion

    #region Training

    /// <summary>
    /// Service for recording training decisions and explanations.
    /// Null if Training Mode is disabled.
    /// </summary>
    ITrainingService? TrainingService { get; }

    #endregion
}
