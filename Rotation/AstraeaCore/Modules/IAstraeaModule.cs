using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Interface for Astraea (Astrologian) rotation modules.
/// Inherits from IHealerRotationModule for consistent module patterns across healers.
/// </summary>
public interface IAstraeaModule : IHealerRotationModule<AstraeaContext>
{
}
