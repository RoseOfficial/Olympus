namespace Olympus.Rotation.HephaestusCore.Context;

/// <summary>
/// Debug state for Gunbreaker (Hephaestus) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class HephaestusDebugState
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

    // Cartridge resource
    public int Cartridges { get; set; }

    // Gnashing Fang combo state
    public int GnashingFangStep { get; set; }
    public bool IsInGnashingFangCombo { get; set; }

    // Continuation ready states
    public bool IsReadyToRip { get; set; }
    public bool IsReadyToTear { get; set; }
    public bool IsReadyToGouge { get; set; }
    public bool IsReadyToBlast { get; set; }
    public bool IsReadyToReign { get; set; }

    // Tank stance
    public bool HasRoyalGuard { get; set; }

    // Buff tracking
    public bool HasNoMercy { get; set; }
    public float NoMercyRemaining { get; set; }

    // Defensive tracking
    public bool HasActiveMitigation { get; set; }
    public string ActiveMitigations { get; set; } = "";
    public bool HasSuperbolide { get; set; }
    public bool HasNebula { get; set; }
    public bool HasHeartOfCorundum { get; set; }
    public bool HasCamouflage { get; set; }
    public bool HasAurora { get; set; }

    // DoT tracking
    public bool HasSonicBreakDot { get; set; }
    public bool HasBowShockDot { get; set; }

    // Enmity tracking
    public bool IsMainTank { get; set; }
    public string CurrentTarget { get; set; } = "";

    // Targeting
    public int NearbyEnemies { get; set; }
}
