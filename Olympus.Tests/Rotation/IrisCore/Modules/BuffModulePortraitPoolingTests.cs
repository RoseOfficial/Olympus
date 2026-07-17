using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Gate tests for BuffModule.TryPushPortrait.
/// Portraits (Mog/Madeen) should only be pushed when:
///   (a) inside the Starry Muse buff window (StarryMuseRemaining > AnimationLockBase), OR
///   (b) Starry Muse cooldown > 30s (lost-use escape), OR
///   (c) EnableBurstPooling is false (pooling disabled - fire unconditionally).
/// </summary>
public class BuffModulePortraitPoolingTests
{
    private readonly BuffModule _module = new();

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    [Fact]
    public void Portrait_Pushed_WhenInsideStarryMuseWindow()
    {
        // Starry Muse buff is active with > 0.6s remaining -> portraits fire immediately.
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnablePortraits = true;
        config.Pictomancer.EnableBurstPooling = true;

        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        // GetCooldownRemaining defaults to 0f (doesn't matter - inStarryWindow is true).

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            mogReady: true,
            hasStarryMuse: true,
            starryMuseRemaining: 1.0f);  // > FFXIVTimings.AnimationLockBase (0.6f)

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == IrisAbilities.MogOfTheAges);
    }

    [Fact]
    public void Portrait_Held_WhenOutsideWindow_AndStarryCDNearby()
    {
        // No Starry Muse buff, CD only 10s away -> portraits held for the imminent window.
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnablePortraits = true;
        config.Pictomancer.EnableBurstPooling = true;

        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        // Starry Muse CD = 10s -> lostUseEscape = false.
        actionService.Setup(x => x.GetCooldownRemaining(PCTActions.StarryMuse.ActionId)).Returns(10f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            mogReady: true,
            hasStarryMuse: false,
            starryMuseRemaining: 0f);   // not in window

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == IrisAbilities.MogOfTheAges);
        Assert.DoesNotContain(ogcd, c => c.Behavior == IrisAbilities.RetributionOfTheMadeen);
    }

    [Fact]
    public void Portrait_Pushed_WhenStarryCDExceedsEscapeThreshold()
    {
        // No Starry Muse buff, CD is 60s away (> 30s) -> fire now so portrait doesn't strand.
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnablePortraits = true;
        config.Pictomancer.EnableBurstPooling = true;

        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        // Starry Muse CD = 60s -> lostUseEscape = true.
        actionService.Setup(x => x.GetCooldownRemaining(PCTActions.StarryMuse.ActionId)).Returns(60f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            mogReady: true,
            hasStarryMuse: false,
            starryMuseRemaining: 0f);   // not in window

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == IrisAbilities.MogOfTheAges);
    }

    [Fact]
    public void Portrait_Pushed_Unconditionally_WhenBurstPoolingDisabled()
    {
        // EnableBurstPooling = false -> old behavior, portraits always fire when ready.
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnablePortraits = true;
        config.Pictomancer.EnableBurstPooling = false;

        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        // CD = 10s (would normally hold), but pooling is off so gate is skipped.
        actionService.Setup(x => x.GetCooldownRemaining(PCTActions.StarryMuse.ActionId)).Returns(10f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            mogReady: true,
            hasStarryMuse: false,
            starryMuseRemaining: 0f);   // not in window, but gate is bypassed

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == IrisAbilities.MogOfTheAges);
    }
}
