using System;
using System.Numerics;

namespace Olympus.Services.Movement;

/// <summary>
/// Owns the native RMIWalk hook. Consumers set <see cref="DesiredInputVector"/> each frame;
/// the hook reads it and overrides player movement input when (a) the field is non-null AND
/// (b) the player is not currently driving via WASD.
/// </summary>
public interface IRMIWalkHookService : IDisposable
{
    /// <summary>(Forward, Left, Turn) in player-relative space, or null to pass through.</summary>
    Vector3? DesiredInputVector { get; set; }

    /// <summary>False if the sigscan failed at construction (patch-day compat break).</summary>
    bool HookInstalled { get; }
}
