namespace Olympus.Rotation.PrometheusCore.Context;

/// <summary>
/// Debug state for the Prometheus (Machinist) rotation.
/// Tracks all relevant state for debugging and visualization.
/// </summary>
public sealed class PrometheusDebugState
{
    #region Module States

    /// <summary>
    /// Current planning state description.
    /// </summary>
    public string PlanningState { get; set; } = string.Empty;

    /// <summary>
    /// Name of the currently planned action.
    /// </summary>
    public string PlannedAction { get; set; } = string.Empty;

    /// <summary>
    /// Current buff module state description.
    /// </summary>
    public string BuffState { get; set; } = string.Empty;

    /// <summary>
    /// Current damage module state description.
    /// </summary>
    public string DamageState { get; set; } = string.Empty;

    #endregion

    #region Gauge State

    /// <summary>
    /// Current Heat gauge value.
    /// </summary>
    public int Heat { get; set; }

    /// <summary>
    /// Current Battery gauge value.
    /// </summary>
    public int Battery { get; set; }

    /// <summary>
    /// Whether currently Overheated.
    /// </summary>
    public bool IsOverheated { get; set; }

    /// <summary>
    /// Remaining Overheated duration.
    /// </summary>
    public float OverheatRemaining { get; set; }

    /// <summary>
    /// Whether Automaton Queen is active.
    /// </summary>
    public bool IsQueenActive { get; set; }

    /// <summary>
    /// Remaining Queen duration.
    /// </summary>
    public float QueenRemaining { get; set; }

    /// <summary>
    /// Battery used for last Queen summon.
    /// </summary>
    public int LastQueenBattery { get; set; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Reassembled is active.
    /// </summary>
    public bool HasReassemble { get; set; }

    /// <summary>
    /// Whether Hypercharged is active.
    /// </summary>
    public bool HasHypercharged { get; set; }

    /// <summary>
    /// Whether Full Metal Machinist is active.
    /// </summary>
    public bool HasFullMetalMachinist { get; set; }

    /// <summary>
    /// Whether Excavator Ready is active.
    /// </summary>
    public bool HasExcavatorReady { get; set; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Wildfire is on target.
    /// </summary>
    public bool HasWildfire { get; set; }

    /// <summary>
    /// Remaining Wildfire duration.
    /// </summary>
    public float WildfireRemaining { get; set; }

    /// <summary>
    /// Whether Bioblaster DoT is on target.
    /// </summary>
    public bool HasBioblaster { get; set; }

    /// <summary>
    /// Remaining Bioblaster duration.
    /// </summary>
    public float BioblasterRemaining { get; set; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Drill charges available.
    /// </summary>
    public int DrillCharges { get; set; }

    /// <summary>
    /// Reassemble charges available.
    /// </summary>
    public int ReassembleCharges { get; set; }

    /// <summary>
    /// Gauss Round charges available.
    /// </summary>
    public int GaussRoundCharges { get; set; }

    /// <summary>
    /// Ricochet charges available.
    /// </summary>
    public int RicochetCharges { get; set; }

    #endregion

    #region Combat State

    /// <summary>
    /// Combo step (0-3).
    /// </summary>
    public int ComboStep { get; set; }

    /// <summary>
    /// Nearby enemy count for AoE decisions.
    /// </summary>
    public int NearbyEnemies { get; set; }

    /// <summary>
    /// Current target name.
    /// </summary>
    public string CurrentTarget { get; set; } = "None";

    #endregion
}
