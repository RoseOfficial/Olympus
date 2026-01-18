namespace Olympus.Rotation.PersephoneCore.Context;

/// <summary>
/// Debug state for the Persephone (Summoner) rotation.
/// Tracks all relevant state for debugging and visualization.
/// </summary>
public sealed class PersephoneDebugState
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

    #region Demi-Summon State

    /// <summary>
    /// Whether Demi-Bahamut is active.
    /// </summary>
    public bool IsBahamutActive { get; set; }

    /// <summary>
    /// Whether Demi-Phoenix is active.
    /// </summary>
    public bool IsPhoenixActive { get; set; }

    /// <summary>
    /// Whether Solar Bahamut is active.
    /// </summary>
    public bool IsSolarBahamutActive { get; set; }

    /// <summary>
    /// Remaining demi-summon timer.
    /// </summary>
    public float DemiSummonTimer { get; set; }

    /// <summary>
    /// GCDs remaining in demi-summon phase.
    /// </summary>
    public int DemiSummonGcdsRemaining { get; set; }

    #endregion

    #region Primal Attunement State

    /// <summary>
    /// Current attunement type (0=None, 1=Ifrit, 2=Titan, 3=Garuda).
    /// </summary>
    public int CurrentAttunement { get; set; }

    /// <summary>
    /// Current attunement stacks remaining.
    /// </summary>
    public int AttunementStacks { get; set; }

    /// <summary>
    /// Remaining attunement timer.
    /// </summary>
    public float AttunementTimer { get; set; }

    /// <summary>
    /// Name of current attunement for display.
    /// </summary>
    public string AttunementName => CurrentAttunement switch
    {
        1 => "Ifrit",
        2 => "Titan",
        3 => "Garuda",
        _ => "None"
    };

    #endregion

    #region Primal Availability

    /// <summary>
    /// Whether Ifrit can be summoned.
    /// </summary>
    public bool CanSummonIfrit { get; set; }

    /// <summary>
    /// Whether Titan can be summoned.
    /// </summary>
    public bool CanSummonTitan { get; set; }

    /// <summary>
    /// Whether Garuda can be summoned.
    /// </summary>
    public bool CanSummonGaruda { get; set; }

    /// <summary>
    /// Count of available primals.
    /// </summary>
    public int PrimalsAvailable { get; set; }

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
    /// Aetherflow stacks.
    /// </summary>
    public int AetherflowStacks { get; set; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Further Ruin is active.
    /// </summary>
    public bool HasFurtherRuin { get; set; }

    /// <summary>
    /// Remaining Further Ruin duration.
    /// </summary>
    public float FurtherRuinRemaining { get; set; }

    /// <summary>
    /// Whether Searing Light is active.
    /// </summary>
    public bool HasSearingLight { get; set; }

    /// <summary>
    /// Remaining Searing Light duration.
    /// </summary>
    public float SearingLightRemaining { get; set; }

    /// <summary>
    /// Whether Ifrit's Favor is active.
    /// </summary>
    public bool HasIfritsFavor { get; set; }

    /// <summary>
    /// Whether Titan's Favor is active.
    /// </summary>
    public bool HasTitansFavor { get; set; }

    /// <summary>
    /// Whether Garuda's Favor is active.
    /// </summary>
    public bool HasGarudasFavor { get; set; }

    /// <summary>
    /// Whether Swiftcast is active.
    /// </summary>
    public bool HasSwiftcast { get; set; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Whether Searing Light is ready.
    /// </summary>
    public bool SearingLightReady { get; set; }

    /// <summary>
    /// Whether Energy Drain is ready.
    /// </summary>
    public bool EnergyDrainReady { get; set; }

    /// <summary>
    /// Whether Enkindle is ready.
    /// </summary>
    public bool EnkindleReady { get; set; }

    /// <summary>
    /// Whether Astral Flow is ready.
    /// </summary>
    public bool AstralFlowReady { get; set; }

    /// <summary>
    /// Radiant Aegis charges available.
    /// </summary>
    public int RadiantAegisCharges { get; set; }

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

    #region Tracking State

    /// <summary>
    /// Whether Enkindle was used this demi-summon phase.
    /// </summary>
    public bool HasUsedEnkindleThisPhase { get; set; }

    /// <summary>
    /// Whether Astral Flow was used this demi-summon phase.
    /// </summary>
    public bool HasUsedAstralFlowThisPhase { get; set; }

    #endregion
}
