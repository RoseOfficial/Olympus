using Olympus.Rotation.Common;
using Olympus.Rotation.IrisCore.Context;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Interface for Iris rotation modules.
/// Extends the base rotation module with Pictomancer-specific context.
/// </summary>
public interface IIrisModule : IRotationModule<IIrisContext>
{
}
