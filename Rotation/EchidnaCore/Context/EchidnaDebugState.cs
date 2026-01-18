using Olympus.Data;

namespace Olympus.Rotation.EchidnaCore.Context;

/// <summary>
/// Debug state for Viper (Echidna) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class EchidnaDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Gauge tracking
    public int SerpentOffering { get; set; }
    public int AnguineTribute { get; set; }
    public int RattlingCoils { get; set; }
    public VPRActions.DreadCombo DreadCombo { get; set; }

    // State tracking
    public bool IsReawakened { get; set; }
    public bool HasHuntersInstinct { get; set; }
    public bool HasSwiftscaled { get; set; }
    public float HuntersInstinctRemaining { get; set; }
    public float SwiftscaledRemaining { get; set; }

    // Combo enhancement
    public bool HasHonedSteel { get; set; }
    public bool HasHonedReavers { get; set; }
    public bool HasReadyToReawaken { get; set; }

    // Venom tracking
    public bool HasFlankstungVenom { get; set; }
    public bool HasHindstungVenom { get; set; }
    public bool HasFlanksbaneVenom { get; set; }
    public bool HasHindsbaneVenom { get; set; }

    // oGCD procs
    public bool HasPoisedForTwinfang { get; set; }
    public bool HasPoisedForTwinblood { get; set; }

    // Target debuff
    public bool HasNoxiousGnash { get; set; }
    public float NoxiousGnashRemaining { get; set; }

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
    /// Gets a formatted string of the Reawaken state.
    /// </summary>
    public string GetReawakenState()
    {
        if (!IsReawakened)
            return "Not Reawakened";

        return $"Reawaken: {AnguineTribute} Tribute";
    }

    /// <summary>
    /// Gets a formatted string of the gauge state.
    /// </summary>
    public string GetGaugeState()
    {
        return $"Offerings: {SerpentOffering}/100 | Coils: {RattlingCoils}/3";
    }

    /// <summary>
    /// Gets a formatted string of the buff state.
    /// </summary>
    public string GetBuffState()
    {
        var buffs = new System.Collections.Generic.List<string>();

        if (HasHuntersInstinct)
            buffs.Add($"Hunter({HuntersInstinctRemaining:F0}s)");
        if (HasSwiftscaled)
            buffs.Add($"Swift({SwiftscaledRemaining:F0}s)");
        if (HasReadyToReawaken)
            buffs.Add("ReadyReawaken");

        return buffs.Count > 0 ? string.Join(" | ", buffs) : "No buffs";
    }

    /// <summary>
    /// Gets a formatted string of the current venom state.
    /// </summary>
    public string GetVenomState()
    {
        if (HasFlankstungVenom) return "Flankstung (use Rear)";
        if (HasHindstungVenom) return "Hindstung (use Flank)";
        if (HasFlanksbaneVenom) return "Flanksbane (use Rear)";
        if (HasHindsbaneVenom) return "Hindsbane (use Flank)";
        return "No venom";
    }
}
