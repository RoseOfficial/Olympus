namespace Olympus.Rotation.ThemisCore.Context;

/// <summary>
/// Debug state for Paladin (Themis) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class ThemisDebugState
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
    public int OathGauge { get; set; }

    // Buff tracking
    public bool HasFightOrFlight { get; set; }
    public float FightOrFlightRemaining { get; set; }
    public bool HasRequiescat { get; set; }
    public int RequiescatStacks { get; set; }
    public int SwordOathStacks { get; set; }
    public int AtonementStep { get; set; }
    public int ConfiteorStep { get; set; }

    // DoT tracking
    public float GoringBladeRemaining { get; set; }

    // Defensive tracking
    public bool HasActiveMitigation { get; set; }
    public string ActiveMitigations { get; set; } = "";

    // Enmity tracking
    public bool IsMainTank { get; set; }
    public string CurrentTarget { get; set; } = "";
}
