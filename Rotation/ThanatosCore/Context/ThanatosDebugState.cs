namespace Olympus.Rotation.ThanatosCore.Context;

/// <summary>
/// Debug state for Reaper (Thanatos) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class ThanatosDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Gauge tracking
    public int Soul { get; set; }
    public int Shroud { get; set; }
    public int LemureShroud { get; set; }
    public int VoidShroud { get; set; }
    public float EnshroudTimer { get; set; }

    // State tracking
    public bool IsEnshrouded { get; set; }
    public bool HasSoulReaver { get; set; }
    public int SoulReaverStacks { get; set; }

    // Enhanced buff tracking
    public bool HasEnhancedGibbet { get; set; }
    public bool HasEnhancedGallows { get; set; }
    public bool HasEnhancedVoidReaping { get; set; }
    public bool HasEnhancedCrossReaping { get; set; }

    // Proc tracking
    public bool HasPerfectioParata { get; set; }
    public bool HasOblatio { get; set; }
    public bool HasSoulsow { get; set; }

    // Arcane Circle
    public bool HasArcaneCircle { get; set; }
    public int ImmortalSacrificeStacks { get; set; }
    public bool HasBloodsownCircle { get; set; }

    // Target debuff
    public bool HasDeathsDesign { get; set; }
    public float DeathsDesignRemaining { get; set; }

    // Positional tracking
    public bool IsAtRear { get; set; }
    public bool IsAtFlank { get; set; }
    public bool HasTrueNorth { get; set; }
    public bool TargetHasPositionalImmunity { get; set; }

    // Targeting
    public string CurrentTarget { get; set; } = "";
    public int NearbyEnemies { get; set; }

    // Combo state
    public int ComboStep { get; set; }

    /// <summary>
    /// Gets a formatted string of the Enshroud state.
    /// </summary>
    public string GetEnshroudState()
    {
        if (!IsEnshrouded)
            return "Not Enshrouded";

        return $"Enshroud: L{LemureShroud}/V{VoidShroud} ({EnshroudTimer:F1}s)";
    }

    /// <summary>
    /// Gets a formatted string of the gauge state.
    /// </summary>
    public string GetGaugeState()
    {
        return $"Soul: {Soul}/100 | Shroud: {Shroud}/100";
    }
}
