namespace Olympus.Rotation.ZeusCore.Context;

/// <summary>
/// Debug state for Dragoon (Zeus) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class ZeusDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Gauge tracking
    public int FirstmindsFocus { get; set; }
    public int EyeCount { get; set; }
    public bool IsLifeOfDragonActive { get; set; }
    public float LifeOfDragonRemaining { get; set; }

    // Combo tracking
    public int ComboStep { get; set; }
    public uint LastComboAction { get; set; }
    public float ComboTimeRemaining { get; set; }
    public string ComboState { get; set; } = "";

    // Buff tracking
    public bool HasPowerSurge { get; set; }
    public float PowerSurgeRemaining { get; set; }
    public bool HasLanceCharge { get; set; }
    public float LanceChargeRemaining { get; set; }
    public bool HasLifeSurge { get; set; }
    public bool HasBattleLitany { get; set; }
    public float BattleLitanyRemaining { get; set; }
    public bool HasRightEye { get; set; }

    // Proc tracking
    public bool HasDiveReady { get; set; }
    public bool HasFangAndClawBared { get; set; }
    public bool HasWheelInMotion { get; set; }
    public bool HasDraconianFire { get; set; }
    public bool HasNastrondReady { get; set; }
    public bool HasStardiverReady { get; set; }
    public bool HasStarcrossReady { get; set; }

    // DoT tracking
    public bool HasDotOnTarget { get; set; }
    public float DotRemaining { get; set; }

    // Positional tracking
    public bool IsAtRear { get; set; }
    public bool IsAtFlank { get; set; }
    public bool HasTrueNorth { get; set; }
    public bool TargetHasPositionalImmunity { get; set; }

    // Targeting
    public string CurrentTarget { get; set; } = "";
    public int NearbyEnemies { get; set; }

    /// <summary>
    /// Gets a formatted string of the current combo state.
    /// </summary>
    public static string FormatComboState(uint lastAction, int step)
    {
        if (step == 0 || lastAction == 0)
            return "[---]";

        // Format based on last action
        return lastAction switch
        {
            75 => "[TT-]",   // True Thrust
            78 => "[TTV]",   // Vorpal Thrust
            87 => "[TTD]",   // Disembowel
            84 or 25771 => "[TTV*]",  // Full/Heavens' Thrust
            88 or 25772 => "[TTD*]",  // Chaos/Chaotic Spring
            3554 => "[F&C]",  // Fang and Claw
            3556 => "[WT]",   // Wheeling Thrust
            36952 => "[DB]",  // Drakesbane
            86 => "[DS-]",   // Doom Spike
            7397 => "[DSS]", // Sonic Thrust
            16477 => "[DSC]", // Coerthan Torment
            _ => $"[{lastAction}]"
        };
    }

    /// <summary>
    /// Gets a formatted string of the Life of the Dragon state.
    /// </summary>
    public string FormatLifeState()
    {
        if (IsLifeOfDragonActive)
            return $"LOTD ({LifeOfDragonRemaining:F1}s)";
        if (EyeCount > 0)
            return $"Eyes: {EyeCount}/2";
        return "No Eyes";
    }
}
