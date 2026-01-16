namespace Olympus.Rotation.NyxCore.Context;

/// <summary>
/// Debug state for Dark Knight (Nyx) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class NyxDebugState
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

    // Resources
    public int BloodGauge { get; set; }
    public int CurrentMp { get; set; }
    public int MaxMp { get; set; }

    // Darkside tracking (critical for DRK)
    public bool HasDarkside { get; set; }
    public float DarksideRemaining { get; set; }

    // Dark Arts (TBN proc)
    public bool HasDarkArts { get; set; }

    // Tank stance
    public bool HasGrit { get; set; }

    // Buff tracking
    public bool HasBloodWeapon { get; set; }
    public float BloodWeaponRemaining { get; set; }
    public bool HasDelirium { get; set; }
    public int DeliriumStacks { get; set; }
    public bool HasScornfulEdge { get; set; }
    public bool HasLivingShadow { get; set; }

    // Defensive tracking
    public bool HasActiveMitigation { get; set; }
    public string ActiveMitigations { get; set; } = "";
    public bool HasLivingDead { get; set; }
    public bool HasWalkingDead { get; set; }
    public bool HasTheBlackestNight { get; set; }
    public bool HasShadowWall { get; set; }
    public bool HasDarkMind { get; set; }
    public bool HasOblation { get; set; }

    // Ground DoT tracking
    public bool HasSaltedEarth { get; set; }

    // Enmity tracking
    public bool IsMainTank { get; set; }
    public string CurrentTarget { get; set; } = "";

    // Targeting
    public int NearbyEnemies { get; set; }
}
