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
}
