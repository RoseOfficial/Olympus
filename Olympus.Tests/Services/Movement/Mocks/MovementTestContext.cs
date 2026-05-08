using System;
using System.Collections.Generic;
using System.Numerics;
using Moq;
using Olympus.Config;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;

namespace Olympus.Tests.Services.Movement.Mocks;

public sealed class MovementTestContext
{
    public Mock<IRMIWalkHookService> Hook { get; } = new();
    public Mock<IEnemyAOECastTracker> Tracker { get; } = new();
    public Mock<IBossCombatDetector> Boss { get; } = new();
    public Mock<IBGCollisionProbe> Collision { get; } = new();
    public Mock<IMovementClock> Clock { get; } = new();
    public List<TrackedAOE> ActiveAOEs { get; } = new();
    public DateTime Now { get; set; } = DateTime.UnixEpoch.AddSeconds(1000);
    public Vector2 PlayerPos2D { get; set; } = new(0, 0);
    public Vector3 PlayerPos3D { get; set; } = new(0, 0, 0);
    public bool IsHighEndZone { get; set; } = false;
    public bool PlayerDead { get; set; } = false;
    public bool PlayerMounted { get; set; } = false;
    public bool PlayerInCutscene { get; set; } = false;
    public Configuration Config { get; } = new() { Movement = { EnableTrashAoEAvoidance = true } };

    public MovementTestContext()
    {
        Hook.SetupGet(h => h.HookInstalled).Returns(true);
        Hook.SetupProperty(h => h.DesiredInputVector);
        Tracker.SetupGet(t => t.ActiveAOEs).Returns(() => ActiveAOEs);
        Boss.SetupGet(b => b.IsBossEngaged).Returns(false);
        Collision.Setup(c => c.IsPathBlocked(It.IsAny<Vector3>(), It.IsAny<Vector3>())).Returns(false);
        Clock.SetupGet(c => c.UtcNow).Returns(() => Now);
    }
}
