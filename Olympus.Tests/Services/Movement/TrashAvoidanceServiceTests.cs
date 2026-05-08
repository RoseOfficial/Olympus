using System.Numerics;
using Moq;
using Olympus.Services.Movement;
using Olympus.Tests.Services.Movement.Mocks;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class TrashAvoidanceServiceTests
{
    [Fact]
    public void Update_FeatureDisabled_DoesNotWriteVector()
    {
        var ctx = new MovementTestContext();
        ctx.Config.Movement.EnableTrashAoEAvoidance = false;
        var svc = new TestableTrashAvoidanceService(ctx);
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = null, Times.AtLeastOnce);
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_HookUnavailable_DoesNotWriteVector()
    {
        var ctx = new MovementTestContext();
        ctx.Hook.SetupGet(h => h.HookInstalled).Returns(false);
        var svc = new TestableTrashAvoidanceService(ctx);
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_HighEndZone_DoesNotWriteVector()
    {
        var ctx = new MovementTestContext();
        var svc = new TestableTrashAvoidanceService(ctx) { OverrideHighEndZone = true };
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_BossEngaged_DoesNotWriteVector()
    {
        var ctx = new MovementTestContext();
        ctx.Boss.SetupGet(b => b.IsBossEngaged).Returns(true);
        var svc = new TestableTrashAvoidanceService(ctx);
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_PlayerDead_DoesNotWriteVector()
    {
        var ctx = new MovementTestContext();
        var svc = new TestableTrashAvoidanceService(ctx) { OverridePlayerDead = true };
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_NoActiveThreats_ClearsVector()
    {
        var ctx = new MovementTestContext();
        var svc = new TestableTrashAvoidanceService(ctx);
        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = null, Times.AtLeastOnce);
    }

    [Fact]
    public void Update_ActiveCircleThreat_AfterReactionDelay_WritesVectorTowardSafeEdge()
    {
        var ctx = new MovementTestContext();
        ctx.ActiveAOEs.Add(new TrackedAOE(
            CasterId: 100,
            Origin: new Vector2(0, 0),
            RotationRadians: 0f,
            Shape: new Olympus.Services.Movement.Geometry.AOEShapeCircle(5f),
            ResolveAt: ctx.Now.AddSeconds(2)));

        var svc = new TestableTrashAvoidanceService(ctx);
        svc.OverridePlayerPos2D = new Vector2(0, 0); // inside threat
        svc.OverridePlayerPos3D = new Vector3(0, 0, 0);

        // First update: register first-seen, return null (reaction delay)
        svc.Update();

        // Advance time past max reaction delay
        ctx.Now = ctx.Now.AddMilliseconds(800);

        svc.Update();

        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.AtLeastOnce);
    }

    [Fact]
    public void Update_PlayerOutsideAllThreats_ClearsVector()
    {
        var ctx = new MovementTestContext();
        ctx.ActiveAOEs.Add(new TrackedAOE(
            100, new Vector2(0, 0), 0f,
            new Olympus.Services.Movement.Geometry.AOEShapeCircle(3f),
            ctx.Now.AddSeconds(2)));

        var svc = new TestableTrashAvoidanceService(ctx);
        svc.OverridePlayerPos2D = new Vector2(20, 0); // far outside
        svc.OverridePlayerPos3D = new Vector3(20, 0, 0);

        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = null, Times.AtLeastOnce);
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_AllPathsBlocked_ClearsVector()
    {
        var ctx = new MovementTestContext();
        ctx.Collision.Setup(c => c.IsPathBlocked(It.IsAny<Vector3>(), It.IsAny<Vector3>())).Returns(true);
        ctx.ActiveAOEs.Add(new TrackedAOE(
            100, new Vector2(0, 0), 0f,
            new Olympus.Services.Movement.Geometry.AOEShapeCircle(5f),
            ctx.Now.AddSeconds(2)));

        var svc = new TestableTrashAvoidanceService(ctx);
        svc.OverridePlayerPos2D = new Vector2(0, 0);
        svc.OverridePlayerPos3D = new Vector3(0, 0, 0);
        ctx.Now = ctx.Now.AddMilliseconds(1000); // past reaction window

        svc.Update();
        ctx.Hook.VerifySet(h => h.DesiredInputVector = It.Is<Vector3?>(v => v != null), Times.Never);
    }

    [Fact]
    public void Update_RemovedFromTracker_PrunesPerCastState()
    {
        var ctx = new MovementTestContext();
        var threat = new TrackedAOE(
            100, new Vector2(0, 0), 0f,
            new Olympus.Services.Movement.Geometry.AOEShapeCircle(5f),
            ctx.Now.AddSeconds(2));
        ctx.ActiveAOEs.Add(threat);

        var svc = new TestableTrashAvoidanceService(ctx);
        svc.OverridePlayerPos2D = new Vector2(0, 0);
        svc.OverridePlayerPos3D = new Vector3(0, 0, 0);

        svc.Update();
        Assert.True(svc.HasFirstSeenEntry(100));

        // Cast finished -- tracker no longer reports it
        ctx.ActiveAOEs.Clear();
        svc.Update();

        Assert.False(svc.HasFirstSeenEntry(100));
    }

    [Fact]
    public void OnTerritoryChanged_ClearsAllState()
    {
        var ctx = new MovementTestContext();
        ctx.ActiveAOEs.Add(new TrackedAOE(
            100, new Vector2(0, 0), 0f,
            new Olympus.Services.Movement.Geometry.AOEShapeCircle(5f),
            ctx.Now.AddSeconds(2)));

        var svc = new TestableTrashAvoidanceService(ctx);
        svc.OverridePlayerPos2D = new Vector2(0, 0);
        svc.OverridePlayerPos3D = new Vector3(0, 0, 0);

        svc.Update();
        Assert.True(svc.HasFirstSeenEntry(100));

        svc.OnTerritoryChanged(0);
        Assert.False(svc.HasFirstSeenEntry(100));
    }
}
