using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    bool TryExecute(IAthenaContext context, bool isMoving);
}
