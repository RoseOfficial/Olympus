namespace Olympus.Rotation.HecateCore.Context;

/// <summary>
/// Debug state for the Hecate (Black Mage) rotation.
/// Tracks all relevant state for debugging and visualization.
/// </summary>
public sealed class HecateDebugState
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

    #region Element State

    /// <summary>
    /// Whether in Astral Fire.
    /// </summary>
    public bool InAstralFire { get; set; }

    /// <summary>
    /// Whether in Umbral Ice.
    /// </summary>
    public bool InUmbralIce { get; set; }

    /// <summary>
    /// Current element stacks.
    /// </summary>
    public int ElementStacks { get; set; }

    /// <summary>
    /// Remaining element timer.
    /// </summary>
    public float ElementTimer { get; set; }

    /// <summary>
    /// Whether Enochian is active.
    /// </summary>
    public bool IsEnochianActive { get; set; }

    #endregion

    #region Resource State

    /// <summary>
    /// Current MP value.
    /// </summary>
    public int CurrentMp { get; set; }

    /// <summary>
    /// Maximum MP value.
    /// </summary>
    public int MaxMp { get; set; }

    /// <summary>
    /// Umbral Heart count.
    /// </summary>
    public int UmbralHearts { get; set; }

    /// <summary>
    /// Polyglot stacks.
    /// </summary>
    public int PolyglotStacks { get; set; }

    /// <summary>
    /// Astral Soul stacks.
    /// </summary>
    public int AstralSoulStacks { get; set; }

    /// <summary>
    /// Whether Paradox is available.
    /// </summary>
    public bool HasParadox { get; set; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Firestarter proc is active.
    /// </summary>
    public bool HasFirestarter { get; set; }

    /// <summary>
    /// Remaining Firestarter duration.
    /// </summary>
    public float FirestarterRemaining { get; set; }

    /// <summary>
    /// Whether Thunderhead proc is active.
    /// </summary>
    public bool HasThunderhead { get; set; }

    /// <summary>
    /// Remaining Thunderhead duration.
    /// </summary>
    public float ThunderheadRemaining { get; set; }

    /// <summary>
    /// Whether Ley Lines is active.
    /// </summary>
    public bool HasLeyLines { get; set; }

    /// <summary>
    /// Remaining Ley Lines duration.
    /// </summary>
    public float LeyLinesRemaining { get; set; }

    /// <summary>
    /// Triplecast stacks remaining.
    /// </summary>
    public int TriplecastStacks { get; set; }

    /// <summary>
    /// Whether Swiftcast is active.
    /// </summary>
    public bool HasSwiftcast { get; set; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Thunder DoT is on target.
    /// </summary>
    public bool HasThunderDoT { get; set; }

    /// <summary>
    /// Remaining Thunder DoT duration.
    /// </summary>
    public float ThunderDoTRemaining { get; set; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Triplecast charges available.
    /// </summary>
    public int TriplecastCharges { get; set; }

    /// <summary>
    /// Whether Manafont is ready.
    /// </summary>
    public bool ManafontReady { get; set; }

    /// <summary>
    /// Whether Amplifier is ready.
    /// </summary>
    public bool AmplifierReady { get; set; }

    /// <summary>
    /// Whether Ley Lines is ready.
    /// </summary>
    public bool LeyLinesReady { get; set; }

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

    /// <summary>
    /// Current rotation phase description.
    /// </summary>
    public string Phase { get; set; } = "None";

    #endregion
}
