using System;

namespace Olympus.Services.Movement;

/// <summary>
/// Per-frame orchestrator that converts active enemy AoEs into a humanized movement input
/// vector that <see cref="IRMIWalkHookService"/> dispatches.
/// </summary>
public interface ITrashAvoidanceService : IDisposable
{
    void Update();

    /// <summary>True when the service wrote a movement vector this frame.</summary>
    bool IsInjectingMovement { get; }

    /// <summary>Threats currently containing the player (0 when idle).</summary>
    int ActiveThreatCount { get; }

    /// <summary>Short reason for the current state (e.g. "dodging", "player-unavailable", "idle").</summary>
    string LastDecision { get; }
}
