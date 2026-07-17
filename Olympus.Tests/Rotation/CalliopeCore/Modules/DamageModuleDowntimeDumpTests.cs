using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Abilities;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Helpers;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Rotation.Common.Helpers;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.CalliopeCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Apex Arrow to fire
/// at 80+ Soul Voice before a boss untargetable phase, bypassing the normal gauge threshold.
/// </summary>
public sealed class DamageModuleDowntimeDumpTests : IDisposable
{
    public DamageModuleDowntimeDumpTests() => BurstHoldHelper.ModifierKeys = null;

    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    // -----------------------------------------------------------------------
    // 1. Downtime imminent + SoulVoice >= 80 + normal threshold not met -> fires
    // -----------------------------------------------------------------------

    [Fact]
    public void ApexArrow_FiresDespiteThreshold_WhenDowntimeImminentAndSoulVoice80()
    {
        var module = new DamageModule();

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnableApexArrow = true;
        config.Bard.ApexArrowMinGauge = 100; // normal threshold not met at 80
        config.Bard.UseApexDuringBurst = false;
        config.Bard.EnableBurstPooling = true;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targetingService: targeting, timelineService: timeline, soulVoice: 80);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == CalliopeAbilities.ApexArrow);
    }

    // -----------------------------------------------------------------------
    // 2. No downtime + SoulVoice = 80 + threshold 100 -> does NOT fire
    // -----------------------------------------------------------------------

    [Fact]
    public void ApexArrow_NotFired_WhenNoDowntimeAndSoulVoiceBelow100()
    {
        var module = new DamageModule();

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnableApexArrow = true;
        config.Bard.ApexArrowMinGauge = 100;
        config.Bard.EnableBurstPooling = false;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targetingService: targeting, timelineService: null, soulVoice: 80);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == CalliopeAbilities.ApexArrow);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + SoulVoice < 80 -> does NOT fire (threshold not met)
    // -----------------------------------------------------------------------

    [Fact]
    public void ApexArrow_NotFired_WhenDowntimeImminentButSoulVoiceBelow80()
    {
        var module = new DamageModule();

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnableApexArrow = true;
        config.Bard.ApexArrowMinGauge = 100;
        config.Bard.EnableBurstPooling = false;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targetingService: targeting, timelineService: timeline, soulVoice: 79);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == CalliopeAbilities.ApexArrow);
    }

    // -----------------------------------------------------------------------
    // 4. Normal path: SoulVoice >= 100 -> fires even without downtime
    // -----------------------------------------------------------------------

    [Fact]
    public void ApexArrow_Fires_WhenSoulVoiceAt100_NoDowntime()
    {
        var module = new DamageModule();

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnableApexArrow = true;
        config.Bard.ApexArrowMinGauge = 100;
        config.Bard.EnableBurstPooling = false;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);
        // RagingStrikes not ready — normal fire condition via overcap
        actionService.Setup(x => x.IsActionReady(BRDActions.RagingStrikes.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targetingService: targeting, timelineService: null, soulVoice: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == CalliopeAbilities.ApexArrow);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static Mock<IBattleNpc> CreateEnemy(uint id)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.EntityId).Returns(id);
        m.Setup(x => x.GameObjectId).Returns((ulong)id);
        m.Setup(x => x.MaxHp).Returns(100000u);
        m.Setup(x => x.CurrentHp).Returns(100000u);
        m.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return m;
    }

    private static Mock<ITargetingService> CreateTargetingWith(IBattleNpc enemy)
    {
        var t = new Mock<ITargetingService>();
        t.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        t.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy);
        t.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);
        t.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        return t;
    }

    private static ICalliopeContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<ITimelineService>? timelineService = null,
        int soulVoice = 0,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= CalliopeTestContext.CreateDefaultBardConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targetingService ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<ICalliopeContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new Olympus.Rotation.CalliopeCore.Context.CalliopeDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        mock.Setup(x => x.StatusHelper).Returns(new CalliopeStatusHelper());

        mock.Setup(x => x.SoulVoice).Returns(soulVoice);
        mock.Setup(x => x.HasRagingStrikes).Returns(false);

        // Suppress earlier-priority pushes
        mock.Setup(x => x.HasResonantArrowReady).Returns(false);
        mock.Setup(x => x.HasRadiantEncoreReady).Returns(false);
        mock.Setup(x => x.HasBlastArrowReady).Returns(false);
        mock.Setup(x => x.HasBarrage).Returns(false);
        mock.Setup(x => x.HasHawksEye).Returns(false);
        mock.Setup(x => x.HasCausticBite).Returns(false);
        mock.Setup(x => x.HasStormbite).Returns(false);
        mock.Setup(x => x.HasSwiftcast).Returns(false);

        return mock.Object;
    }
}
