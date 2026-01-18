using Olympus.Rotation.Common;
using Olympus.Rotation.PrometheusCore.Context;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Interface for Machinist (Prometheus) rotation modules.
/// </summary>
public interface IPrometheusModule : IRangedDpsRotationModule<IPrometheusContext>
{
}
