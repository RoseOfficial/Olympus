using System;

namespace Olympus.Services.Movement.Humanization;

public sealed class MovementClock : IMovementClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
