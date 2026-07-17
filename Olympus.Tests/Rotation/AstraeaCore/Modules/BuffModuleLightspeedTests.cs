using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Scheduler-push tests for BuffModule Lightspeed Divination-lead logic.
///
/// When LightspeedStrategy is OnCooldown and EnableBurstPooling is true:
///   - Divination CD &lt;= 5s (two GCDs) =&gt; push Lightspeed as lead-in
///   - Divination CD &gt; 5s and charges not at cap =&gt; hold (do not push)
///   - Divination CD &gt; 5s but charges at max =&gt; push (charge-cap escape)
/// When pooling is off =&gt; push on cooldown regardless of Divination CD.
/// SaveForMovement / SaveForRaise paths are not touched by this change.
/// </summary>
public class BuffModuleLightspeedTests
{
    private readonly BuffModule _module = new();

    // Helper: creates an actionService where Lightspeed is ready and
    // Divination has the specified cooldown remaining.
    private static Mock<IActionService> MakeActionService(float divinationCdRemaining)
    {
        var svc = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        svc.Setup(x => x.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(true);
        svc.Setup(x => x.GetCooldownRemaining(ASTActions.Divination.ActionId))
           .Returns(divinationCdRemaining);
        return svc;
    }

    [Fact]
    public void OnCooldown_PoolingOn_DivinationWithin5s_PushesLightspeed()
    {
        // Divination is 4 s away -- inside the 2-GCD lead window.
        // Lightspeed MUST be pushed so it fires before Divination.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MakeActionService(divinationCdRemaining: 4f);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == ASTActions.Lightspeed.ActionId);
    }

    [Fact]
    public void OnCooldown_PoolingOn_DivinationFarAway_HoldsLightspeed()
    {
        // Divination is 60 s away -- well outside the lead window.
        // With pooling on and charges not at cap, Lightspeed must NOT be pushed.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MakeActionService(divinationCdRemaining: 60f);
        // Default: GetCurrentCharges = 0, GetMaxCharges = 2 (from MockBuilders defaults).
        // 0 < 2 means charges are not at cap, so the hold applies.

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == ASTActions.Lightspeed.ActionId);
    }

    [Fact]
    public void OnCooldown_PoolingOff_DivinationFarAway_PushesLightspeed()
    {
        // Pooling disabled -- Lightspeed fires on cooldown regardless of Divination timing.
        // This test shares the hold test's setup; pooling toggle is the only variable.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.HealerShared.EnableBurstPooling = false;

        var actionService = MakeActionService(divinationCdRemaining: 60f);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == ASTActions.Lightspeed.ActionId);
    }

    [Fact]
    public void OnCooldown_PoolingOn_DivinationFarAway_ChargesAtMax_PushesLightspeed()
    {
        // Pooling on, Divination far away -- normally a hold.
        // But charges are at max (2/2): fire now to avoid capping a charge.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MakeActionService(divinationCdRemaining: 60f);
        actionService.Setup(x => x.GetCurrentCharges(ASTActions.Lightspeed.ActionId)).Returns(2u);
        actionService.Setup(x => x.GetMaxCharges(ASTActions.Lightspeed.ActionId, It.IsAny<uint>())).Returns((ushort)2);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c => c.Behavior.Action.ActionId == ASTActions.Lightspeed.ActionId);
    }
}
