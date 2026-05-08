using System.Numerics;
using Olympus.Services.Movement;

namespace Olympus.Tests.Services.Movement.Mocks;

public sealed class TestableTrashAvoidanceService : TrashAvoidanceService
{
    public Vector2 OverridePlayerPos2D { get; set; } = new(0, 0);
    public Vector3 OverridePlayerPos3D { get; set; } = new(0, 0, 0);
    public bool OverrideHighEndZone { get; set; }
    public bool OverridePlayerDead { get; set; }
    public bool OverridePlayerMounted { get; set; }
    public bool OverridePlayerInCutscene { get; set; }

    public TestableTrashAvoidanceService(MovementTestContext ctx)
        : base(ctx.Hook.Object, ctx.Tracker.Object, ctx.Boss.Object, ctx.Collision.Object, ctx.Clock.Object,
              () => ctx.Config.Movement, log: null!) { }

    protected override Vector2 GetPlayerPos2D() => OverridePlayerPos2D;
    protected override Vector3 GetPlayerPos3D() => OverridePlayerPos3D;
    protected override bool IsHighEndZone() => OverrideHighEndZone;
    protected override bool IsPlayerUnavailable() => OverridePlayerDead || OverridePlayerMounted || OverridePlayerInCutscene;
}
