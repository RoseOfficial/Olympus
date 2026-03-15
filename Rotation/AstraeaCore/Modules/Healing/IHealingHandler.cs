using Olympus.Rotation.AstraeaCore.Context;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    bool TryExecute(IAstraeaContext context, bool isMoving);
}
