using System;

namespace Olympus.Services.Movement.Humanization;

/// <summary>
/// Test-injectable clock for movement humanization. Production reads system time; tests inject a stub.
/// </summary>
public interface IMovementClock
{
    DateTime UtcNow { get; }
}
