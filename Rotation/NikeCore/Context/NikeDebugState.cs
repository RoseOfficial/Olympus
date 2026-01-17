using Olympus.Data;

namespace Olympus.Rotation.NikeCore.Context;

/// <summary>
/// Debug state for Samurai (Nike) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class NikeDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Gauge tracking
    public int Kenki { get; set; }
    public SAMActions.SenType Sen { get; set; }
    public int SenCount { get; set; }
    public int Meditation { get; set; }

    // Buff tracking
    public bool HasFugetsu { get; set; }
    public float FugetsuRemaining { get; set; }
    public bool HasFuka { get; set; }
    public float FukaRemaining { get; set; }
    public bool HasMeikyoShisui { get; set; }
    public int MeikyoStacks { get; set; }
    public bool HasOgiNamikiriReady { get; set; }
    public bool HasKaeshiNamikiriReady { get; set; }
    public bool HasTsubameGaeshiReady { get; set; }
    public bool HasZanshinReady { get; set; }

    // DoT tracking
    public bool HasHiganbanaOnTarget { get; set; }
    public float HiganbanaRemaining { get; set; }

    // Last Iaijutsu for Kaeshi
    public SAMActions.IaijutsuType LastIaijutsu { get; set; }

    // Combo tracking
    public int ComboStep { get; set; }
    public float ComboTimeRemaining { get; set; }

    // Positional tracking
    public bool IsAtRear { get; set; }
    public bool IsAtFlank { get; set; }
    public bool HasTrueNorth { get; set; }
    public bool TargetHasPositionalImmunity { get; set; }

    // Targeting
    public string CurrentTarget { get; set; } = "";
    public int NearbyEnemies { get; set; }

    /// <summary>
    /// Gets a formatted string of the current Sen state.
    /// </summary>
    public static string FormatSen(SAMActions.SenType sen)
    {
        return SAMActions.FormatSen(sen);
    }

    /// <summary>
    /// Gets a formatted gauge summary.
    /// </summary>
    public string GetGaugeSummary()
    {
        return $"Sen:{FormatSen(Sen)} Kenki:{Kenki} Med:{Meditation}";
    }
}
