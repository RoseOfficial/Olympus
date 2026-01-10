using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Interface for Apollo (White Mage) rotation modules.
/// Inherits from IHealerRotationModule for consistent module patterns across healers.
/// </summary>
public interface IApolloModule : IHealerRotationModule<ApolloContext>
{
}
