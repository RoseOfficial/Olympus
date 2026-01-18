namespace Olympus.Rotation.TerpsichoreCore.Context;

/// <summary>
/// Debug state for the Terpsichore (Dancer) rotation.
/// Tracks all relevant state for debugging and visualization.
/// </summary>
public sealed class TerpsichoreDebugState
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
    /// Current Esprit gauge value.
    /// </summary>
    public int Esprit { get; set; }

    /// <summary>
    /// Current Feather count.
    /// </summary>
    public int Feathers { get; set; }

    /// <summary>
    /// Whether currently dancing.
    /// </summary>
    public bool IsDancing { get; set; }

    /// <summary>
    /// Current step index in dance.
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// Next dance step to execute.
    /// </summary>
    public string CurrentStep { get; set; } = "None";

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Silken Symmetry is active.
    /// </summary>
    public bool HasSilkenSymmetry { get; set; }

    /// <summary>
    /// Whether Silken Flow is active.
    /// </summary>
    public bool HasSilkenFlow { get; set; }

    /// <summary>
    /// Whether Threefold Fan Dance is active.
    /// </summary>
    public bool HasThreefoldFanDance { get; set; }

    /// <summary>
    /// Whether Fourfold Fan Dance is active.
    /// </summary>
    public bool HasFourfoldFanDance { get; set; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Flourishing Finish is active.
    /// </summary>
    public bool HasFlourishingFinish { get; set; }

    /// <summary>
    /// Whether Flourishing Starfall is active.
    /// </summary>
    public bool HasFlourishingStarfall { get; set; }

    /// <summary>
    /// Whether Devilment is active.
    /// </summary>
    public bool HasDevilment { get; set; }

    /// <summary>
    /// Remaining Devilment duration.
    /// </summary>
    public float DevilmentRemaining { get; set; }

    /// <summary>
    /// Whether Standard Finish buff is active.
    /// </summary>
    public bool HasStandardFinish { get; set; }

    /// <summary>
    /// Whether Technical Finish buff is active.
    /// </summary>
    public bool HasTechnicalFinish { get; set; }

    #endregion

    #region High-Level Procs

    /// <summary>
    /// Whether Last Dance Ready is active.
    /// </summary>
    public bool HasLastDanceReady { get; set; }

    /// <summary>
    /// Whether Finishing Move Ready is active.
    /// </summary>
    public bool HasFinishingMoveReady { get; set; }

    /// <summary>
    /// Whether Dance of the Dawn Ready is active.
    /// </summary>
    public bool HasDanceOfTheDawnReady { get; set; }

    #endregion

    #region Partner State

    /// <summary>
    /// Whether we have a dance partner.
    /// </summary>
    public bool HasDancePartner { get; set; }

    /// <summary>
    /// Dance partner name.
    /// </summary>
    public string DancePartner { get; set; } = "None";

    #endregion

    #region Combat State

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
