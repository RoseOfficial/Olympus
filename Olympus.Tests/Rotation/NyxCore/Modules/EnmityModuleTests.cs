using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

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
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
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

    private static INyxContext CreateContext(
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
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();

        enmityService ??= new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();

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

        var partyHelper = new Olympus.Rotation.NyxCore.Helpers.NyxPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        var debugState = new NyxDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
