using System;

namespace Olympus.Services.Movement;

/// <summary>
/// Per-frame orchestrator that converts active enemy AoEs into a humanized movement input
/// vector that <see cref="IRMIWalkHookService"/> dispatches.
/// </summary>
public interface ITrashAvoidanceService : IDisposable
{
    void Update();
}
