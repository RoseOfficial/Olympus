using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving);
}
