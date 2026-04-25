using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Interface for healing sub-handlers within the Asclepius HealingModule.
/// Each handler pushes scheduler candidates instead of dispatching directly.
/// Priority values are list-local (oGCD list and GCD list each have independent sequences).
/// </summary>
public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving);
}
