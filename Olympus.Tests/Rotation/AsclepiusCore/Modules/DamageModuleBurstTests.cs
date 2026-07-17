using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Scheduler-push tests for DamageModule burst-pooling branches.
/// Covers Phlegma (GCD queue) and Psyche (oGCD queue) hold/fire decisions.
/// </summary>
public class DamageModuleBurstTests
{
    // -----------------------------------------------------------------------
    // Phlegma burst-hold / burst-fire / overcap tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Phlegma_HeldWhenBurstImminentAndOneCharge()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // 1 charge available, recharging slowly (not about to overcap)
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(1u);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(30f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcdQueue, c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
        Assert.Equal("Phlegma held — burst imminent", context.Debug.PhlegmaState);
    }

    [Fact]
    public void Phlegma_FiresInBurstWindowWithOneCharge()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(true);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(42ul);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(1u);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(30f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        Assert.Contains(gcdQueue, c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
    }

    [Fact]
    public void Phlegma_FiresAtTwoChargesRegardlessOfBurstState()
    {
        // Overcap prevention: burst pooling must NOT block Phlegma at 2 charges.
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(42ul);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // 2 charges — at cap, always fire regardless of burst state
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(2u);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(30f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        Assert.Contains(gcdQueue, c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
    }

    // -----------------------------------------------------------------------
    // Psyche burst-hold tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Psyche_HeldWhenBurstImminentAndPoolingEnabled()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePsyche = true;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Psyche.ActionId)).Returns(true);

        // No targetingService setup intentionally: FindEnemy must never be reached
        // when the burst-hold guard fires first.
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcdQueue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcdQueue, c => c.Behavior.Action.ActionId == SGEActions.Psyche.ActionId);
        Assert.Equal("Psyche held — burst imminent", context.Debug.PsycheState);
    }

    [Fact]
    public void Psyche_FiresWhenBurstNotImminentAndPoolingEnabled()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePsyche = true;
        config.HealerShared.EnableBurstPooling = true;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(42ul);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Psyche.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcdQueue = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcdQueue, c => c.Behavior.Action.ActionId == SGEActions.Psyche.ActionId);
    }
}
