namespace Olympus.Rotation.CalliopeCore.Context;

/// <summary>
/// Debug state for the Calliope (Bard) rotation.
/// Tracks all relevant state for debugging and visualization.
/// </summary>
public sealed class CalliopeDebugState
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
    /// Current Soul Voice gauge value.
    /// </summary>
    public int SoulVoice { get; set; }

    /// <summary>
    /// Current song timer in seconds.
    /// </summary>
    public float SongTimer { get; set; }

    /// <summary>
    /// Current Repertoire stacks.
    /// </summary>
    public int Repertoire { get; set; }

    /// <summary>
    /// Current active song name.
    /// </summary>
    public string CurrentSong { get; set; } = "None";

    /// <summary>
    /// Number of Coda available.
    /// </summary>
    public int CodaCount { get; set; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Hawk's Eye (Straight Shot Ready) is active.
    /// </summary>
    public bool HasHawksEye { get; set; }

    /// <summary>
    /// Whether Raging Strikes is active.
    /// </summary>
    public bool HasRagingStrikes { get; set; }

    /// <summary>
    /// Remaining Raging Strikes duration.
    /// </summary>
    public float RagingStrikesRemaining { get; set; }

    /// <summary>
    /// Whether Battle Voice is active.
    /// </summary>
    public bool HasBattleVoice { get; set; }

    /// <summary>
    /// Whether Barrage is active.
    /// </summary>
    public bool HasBarrage { get; set; }

    /// <summary>
    /// Whether Radiant Finale is active.
    /// </summary>
    public bool HasRadiantFinale { get; set; }

    /// <summary>
    /// Whether Blast Arrow Ready is active.
    /// </summary>
    public bool HasBlastArrowReady { get; set; }

    /// <summary>
    /// Whether Resonant Arrow Ready is active.
    /// </summary>
    public bool HasResonantArrowReady { get; set; }

    /// <summary>
    /// Whether Radiant Encore Ready is active.
    /// </summary>
    public bool HasRadiantEncoreReady { get; set; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether Caustic Bite is on target.
    /// </summary>
    public bool HasCausticBite { get; set; }

    /// <summary>
    /// Remaining Caustic Bite duration.
    /// </summary>
    public float CausticBiteRemaining { get; set; }

    /// <summary>
    /// Whether Stormbite is on target.
    /// </summary>
    public bool HasStormbite { get; set; }

    /// <summary>
    /// Remaining Stormbite duration.
    /// </summary>
    public float StormbiteRemaining { get; set; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Bloodletter/Heartbreak Shot charges available.
    /// </summary>
    public int BloodletterCharges { get; set; }

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
