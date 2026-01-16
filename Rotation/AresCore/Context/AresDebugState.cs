namespace Olympus.Rotation.AresCore.Context;

/// <summary>
/// Debug state for Warrior (Ares) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class AresDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string MitigationState { get; set; } = "";
    public string BuffState { get; set; } = "";
    public string EnmityState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Combo tracking
    public int ComboStep { get; set; }
    public string LastComboAction { get; set; } = "";
    public float ComboTimeRemaining { get; set; }

    // Gauge
    public int BeastGauge { get; set; }

    // Buff tracking
    public bool HasDefiance { get; set; }
    public bool HasSurgingTempest { get; set; }
    public float SurgingTempestRemaining { get; set; }
    public bool HasInnerRelease { get; set; }
    public int InnerReleaseStacks { get; set; }
    public bool HasNascentChaos { get; set; }
    public bool HasPrimalRendReady { get; set; }
    public bool HasPrimalRuinationReady { get; set; }

    // Defensive tracking
    public bool HasActiveMitigation { get; set; }
    public string ActiveMitigations { get; set; } = "";

    // Enmity tracking
    public bool IsMainTank { get; set; }
    public string CurrentTarget { get; set; } = "";

    // Targeting
    public int NearbyEnemies { get; set; }
}
