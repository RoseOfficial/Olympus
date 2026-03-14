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

public class MitigationModuleTests
{
    private readonly MitigationModule _module = new();

    [Fact]
    public void TryExecute_MitigationDisabled_ReturnsFalse()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableMitigation = false;

        var context = CreateContext(inCombat: true, canExecuteOgcd: true, config: config);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_EmergencyHallowedGround_CriticallyLowHp_ReturnsTrue()
    {
        // HP at 10% — Hallowed Ground should fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(PLDActions.HallowedGround.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PLDActions.HallowedGround.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 5000,    // 10% of 50000
            maxHp: 50000,
            hasHallowedGround: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.HallowedGround.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HallowedGround_HealthyHp_SkipsHallowedGround()
    {
        // HP at 50% — Hallowed Ground threshold is 15%, should not fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Nothing ready
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 25000,   // 50%
            maxHp: 50000,
            hasHallowedGround: false,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.HallowedGround.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HallowedGround_AlreadyActive_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(PLDActions.HallowedGround.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 5000,    // critical HP
            maxHp: 50000,
            hasHallowedGround: true,    // already active
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.HallowedGround.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_FullHp_NoMitigationFires()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 50000,   // 100% HP
            maxHp: 50000,
            config: config,
            actionService: actionService,
            tankCooldownShouldUseMitigation: false,
            tankCooldownShouldUseMajor: false);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.IsCastInterruptible).Returns(false);
        return mock;
    }

    private static IThemisContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        uint currentHp = 50000,
        uint maxHp = 50000,
        bool hasHallowedGround = false,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        bool tankCooldownShouldUseMitigation = false,
        bool tankCooldownShouldUseMajor = false)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= ThemisTestContext.CreateDefaultPaladinConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(currentHp: currentHp, maxHp: maxHp);
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
        mock.Setup(x => x.TimelineService).Returns((Olympus.Timeline.ITimelineService?)null);
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // PLD-specific state
        mock.Setup(x => x.HasHallowedGround).Returns(hasHallowedGround);
        mock.Setup(x => x.HasActiveMitigation).Returns(false);
        mock.Setup(x => x.OathGauge).Returns(0);
        mock.Setup(x => x.PartyHealthMetrics).Returns((1.0f, 1.0f, 0));
        mock.Setup(x => x.HasTankStance).Returns(true);
        mock.Setup(x => x.HasFightOrFlight).Returns(false);
        mock.Setup(x => x.FightOrFlightRemaining).Returns(0f);
        mock.Setup(x => x.HasRequiescat).Returns(false);
        mock.Setup(x => x.RequiescatStacks).Returns(0);

        var statusHelper = new Olympus.Rotation.ThemisCore.Helpers.ThemisStatusHelper();
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(tankCooldownShouldUseMitigation);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(tankCooldownShouldUseMajor);
        tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);

        var partyHelper = new Olympus.Rotation.ThemisCore.Helpers.ThemisPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        var debugState = new ThemisDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
