using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_DamageDisabled_ReturnsFalse()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDamage = false;

        var context = CreateContext(inCombat: true, canExecuteOgcd: true, config: config);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Damage disabled", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_FightOrFlight_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(PLDActions.FightOrFlight.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFightOrFlight: false,
            hasRequiescat: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FightOrFlight_SkipsIfAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // All actions not ready
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFightOrFlight: true,
            fightOrFlightRemaining: 15f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_FightOrFlight_SkipsIfRequiescatActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(PLDActions.FightOrFlight.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFightOrFlight: false,
            hasRequiescat: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Never);

        Assert.Contains("Requiescat", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_FightOrFlight_BelowMinLevel_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 1,   // far below FightOrFlight's MinLevel
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_IronWill_TankStance_ActivatedWhenMissing()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // IronWill ready, all others not
        actionService.Setup(x => x.IsActionReady(PLDActions.IronWill.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(PLDActions.FightOrFlight.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(PLDActions.Requiescat.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PLDActions.IronWill.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.IronWill.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static IThemisContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasTankStance = true,
        bool hasFightOrFlight = false,
        float fightOrFlightRemaining = 0f,
        bool hasRequiescat = false,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= ThemisTestContext.CreateDefaultPaladinConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IThemisContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // PLD-specific state
        mock.Setup(x => x.HasTankStance).Returns(hasTankStance);
        mock.Setup(x => x.HasFightOrFlight).Returns(hasFightOrFlight);
        mock.Setup(x => x.FightOrFlightRemaining).Returns(fightOrFlightRemaining);
        mock.Setup(x => x.HasRequiescat).Returns(hasRequiescat);
        mock.Setup(x => x.RequiescatStacks).Returns(0);
        mock.Setup(x => x.HasSwordOath).Returns(false);
        mock.Setup(x => x.SwordOathStacks).Returns(0);
        mock.Setup(x => x.HasBladeOfHonor).Returns(false);
        mock.Setup(x => x.ConfiteorStep).Returns(0);
        mock.Setup(x => x.AtonementStep).Returns(0);
        mock.Setup(x => x.GoringBladeRemaining).Returns(30f);
        mock.Setup(x => x.ComboStep).Returns(0);

        var debugState = new ThemisDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
