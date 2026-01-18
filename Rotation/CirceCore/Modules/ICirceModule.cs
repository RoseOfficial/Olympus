using Olympus.Rotation.Common;
using Olympus.Rotation.CirceCore.Context;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Interface for Red Mage rotation modules.
/// Each module handles a specific aspect of the rotation (buffs, damage, etc.).
/// </summary>
public interface ICirceModule : IRotationModule<ICirceContext>
{
    // Inherits from IRotationModule<ICirceContext>:
    // - int Priority { get; }
    // - string Name { get; }
    // - bool TryExecute(ICirceContext context, bool isMoving);
    // - void UpdateDebugState(ICirceContext context);
}
