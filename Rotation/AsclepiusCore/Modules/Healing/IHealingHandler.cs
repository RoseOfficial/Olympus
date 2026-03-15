using Olympus.Rotation.AsclepiusCore.Context;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Interface for healing sub-handlers within the Asclepius HealingModule.
/// Each handler is responsible for one ability or closely related ability group.
/// Priority values are list-local (oGCD list and GCD list each have independent sequences).
/// </summary>
public interface IHealingHandler
{
    int Priority { get; }
    string Name { get; }
    bool TryExecute(IAsclepiusContext context, bool isMoving);
}
