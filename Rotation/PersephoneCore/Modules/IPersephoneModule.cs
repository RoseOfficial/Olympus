using Olympus.Rotation.Common;
using Olympus.Rotation.PersephoneCore.Context;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Interface for Summoner rotation modules.
/// Each module handles a specific aspect of the rotation (buffs, damage, etc.).
/// </summary>
public interface IPersephoneModule : IRotationModule<IPersephoneContext>
{
    // Inherits from IRotationModule<IPersephoneContext>:
    // - int Priority { get; }
    // - string Name { get; }
    // - bool TryExecute(IPersephoneContext context, bool isMoving);
    // - void UpdateDebugState(IPersephoneContext context);
}
