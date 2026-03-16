// Rotation/Common/DebugState.cs
namespace Olympus.Rotation.Common;

/// <summary>
/// Shared debug state for all healer rotations.
/// Extends BaseDebugState with WHM-specific display fields.
/// Non-WHM healers inherit this so the debug UI can read LilyCount/BloodLilyCount/LilyStrategy
/// regardless of job (mapped from job-specific resources in UpdateJobSpecificServices).
/// </summary>
public class DebugState : BaseDebugState
{
    // Party (extra metadata used by Apollo and available to all healers)
    public int BattleNpcCount { get; set; }
    public string NpcInfo { get; set; } = "";

    // WHM Buffs
    public string AsylumState { get; set; } = "Idle";
    public string AsylumTarget { get; set; } = "None";
    public string ThinAirState { get; set; } = "Idle";
    public string PoMState { get; set; } = "Idle";
    public string AssizeState { get; set; } = "Idle";

    // WHM Defensive
    public string TemperanceState { get; set; } = "Idle";

    // Resources (WHM: Lily/BloodLily; non-WHM: job-specific mapped here for UI)
    public int LilyCount { get; set; }
    public int BloodLilyCount { get; set; }
    public string LilyStrategy { get; set; } = "Balanced";
    public int SacredSightStacks { get; set; }
    public string MiseryState { get; set; } = "Idle";
}
