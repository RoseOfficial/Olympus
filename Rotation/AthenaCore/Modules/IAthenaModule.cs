using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Interface for Athena (Scholar) rotation modules.
/// Inherits from IHealerRotationModule for consistent module patterns across healers.
/// </summary>
public interface IAthenaModule : IHealerRotationModule<AthenaContext>
{
}
