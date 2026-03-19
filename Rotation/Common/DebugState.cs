// Rotation/Common/DebugState.cs
namespace Olympus.Rotation.Common;

/// <summary>
/// Shared healer debug state used by all four healer rotations (WHM, SCH, AST, SGE).
/// Includes some WHM-exclusive fields that non-WHM healers leave at their default values;
/// the debug UI reads them uniformly regardless of job.
/// Resource fields (LilyCount, BloodLilyCount, LilyStrategy) are mapped from each job's
/// native gauge values in UpdateJobSpecificServices so the same UI works across all healers.
/// </summary>
public class DebugState : BaseDebugState
{
    // Party (extra metadata used by Apollo and available to all healers)
    public int BattleNpcCount { get; set; }
    public string NpcInfo { get; set; } = "";

    // WHM-only — unused by AST/SCH/SGE
    public string AsylumState { get; set; } = "Idle";
    // WHM-only — unused by AST/SCH/SGE
    public string AsylumTarget { get; set; } = "None";
    // WHM-only — unused by AST/SCH/SGE
    public string ThinAirState { get; set; } = "Idle";
    // WHM-only — unused by AST/SCH/SGE
    public string PoMState { get; set; } = "Idle";
    // WHM-only — unused by AST/SCH/SGE
    public string AssizeState { get; set; } = "Idle";

    // WHM-only — unused by AST/SCH/SGE
    public string TemperanceState { get; set; } = "Idle";

    // Resources: WHM uses Lily/BloodLily directly; non-WHM maps job-specific values here for UI
    public int LilyCount { get; set; }
    public int BloodLilyCount { get; set; }
    public string LilyStrategy { get; set; } = "Balanced";
    public int SacredSightStacks { get; set; }
    public string MiseryState { get; set; } = "Idle";
}
