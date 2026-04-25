using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

/// <summary>
/// Interface for healing sub-handlers. Migrated handlers override CollectCandidates
/// and stub TryExecute -> false. Three handlers stay on legacy TryExecute because
/// EarthlyStarPlacement uses ExecuteGroundTargetedOgcd which the scheduler can't
/// dispatch -- and to preserve priority ordering, LadyOfCrowns and HoroscopePreparation
/// (lower priority than EarthlyStarPlacement) also stay legacy.
/// </summary>
public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    bool TryExecute(IAstraeaContext context, bool isMoving);

    /// <summary>Default empty body - legacy handlers don't override; migrated handlers do.</summary>
    void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving) { }
}
