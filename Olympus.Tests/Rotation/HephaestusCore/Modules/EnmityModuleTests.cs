using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

public class EnmityModuleTests
{
    private readonly EnmityModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.EnmityState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_AutoProvokeDisabled_SkipsProvoke()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.AutoProvoke = false;

        var context = CreateContext(inCombat: true, canExecuteOgcd: true, config: config);
        _module.TryExecute(context, isMoving: false);

        Assert.Equal("AutoProvoke disabled", context.Debug.EnmityState);
    }

    [Fact]
    public void TryExecute_NoTarget_SkipsProvoke()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(inCombat: true, canExecuteOgcd: true, targetingService: targeting);
        _module.TryExecute(context, isMoving: false);

        Assert.Equal("No target", context.Debug.EnmityState);
    }

    [Fact]
    public void TryExecute_BelowProvokeMinLevel_SkipsProvoke()
    {
        // Provoke is Lv.15 — test below that level
        var context = CreateContext(inCombat: true, canExecuteOgcd: true, level: 10);
        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_HasAggro_DoesNotProvoke()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            targetingService: targeting,
            enmityService: enmityService,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        // Should not use Provoke when we have aggro
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Provoke.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.EntityId).Returns((uint)objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static IHephaestusContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        Configuration? config = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null,
        Mock<IEnmityService>? enmityService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();

        enmityService ??= new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.EnmityService).Returns(enmityService.Object);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        var partyHelper = new Olympus.Rotation.HephaestusCore.Helpers.HephaestusPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        var debugState = new HephaestusDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}

public class EnmityModuleCollectCandidatesTests
{
    private readonly EnmityModule _module = new();

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.EnmityState);
    }

    [Fact]
    public void CollectCandidates_AutoProvokeDisabled_NoProvokePushed()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.AutoProvoke = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("AutoProvoke disabled", context.Debug.EnmityState);
    }

    [Fact]
    public void CollectCandidates_NoTarget_NoProvokePushed()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, targetingService: targeting);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("No target", context.Debug.EnmityState);
    }

    [Fact]
    public void CollectCandidates_HasAggro_NoProvokePushed()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, targetingService: targeting, enmityService: enmityService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_LosingAggro_PushesProvokeAtPriority1()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(true);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(2);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, targetingService: targeting, enmityService: enmityService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcdQueue = scheduler.InspectOgcdQueue();
        Assert.Single(ogcdQueue);
        Assert.Equal(GnbAbilities.Provoke, ogcdQueue[0].Behavior);
        Assert.Equal(1, ogcdQueue[0].Priority);
        Assert.Equal(enemy.Object.GameObjectId, ogcdQueue[0].TargetId);
    }

    [Fact]
    public void CollectCandidates_BelowProvokeMinLevel_NoProvokePushed()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, level: 10);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_AutoShirkDisabled_NoShirkPushed()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.AutoProvoke = false;
        config.Tank.AutoShirk = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_AutoShirkEnabled_NoCoTank_NoShirkPushed()
    {
        // AutoShirk enabled but party is empty so FindCoTank returns null.
        // HasCoTankAggro must return true to reach the FindCoTank call.
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.AutoProvoke = false;
        config.Tank.AutoShirk = true;

        var enemy = CreateMockEnemy();

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);
        enmityService.Setup(x => x.HasCoTankAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(true);
        enmityService.Setup(x => x.GetEnmityPosition(enemy.Object, It.IsAny<uint>())).Returns(2);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(
            inCombat: true,
            config: config,
            targetingService: targeting,
            enmityService: enmityService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        // No co-tank in empty party, so Shirk cannot be pushed
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Contains("No co-tank", context.Debug.EnmityState);
    }

    [Fact]
    public void CollectCandidates_AutoShirkEnabled_NoCoTankAggro_NoShirkPushed()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.AutoProvoke = false;
        config.Tank.AutoShirk = true;

        var enemy = CreateMockEnemy();

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);
        // Co-tank does NOT have aggro, so Shirk path short-circuits before FindCoTank
        enmityService.Setup(x => x.HasCoTankAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(
            inCombat: true,
            config: config,
            targetingService: targeting,
            enmityService: enmityService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.EntityId).Returns((uint)objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static IHephaestusContext CreateContext(
        bool inCombat,
        byte level = 100,
        Configuration? config = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<IEnmityService>? enmityService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();

        if (enmityService == null)
        {
            enmityService = new Mock<IEnmityService>();
            enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
            enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);
        }

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        player.Setup(x => x.Position).Returns(System.Numerics.Vector3.Zero);

        var mock = new Mock<IHephaestusContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(MockBuilders.CreateMockActionService().Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.EnmityService).Returns(enmityService.Object);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        var defaultPartyHelper = new Olympus.Rotation.HephaestusCore.Helpers.HephaestusPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(defaultPartyHelper);

        var debugState = new HephaestusDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
