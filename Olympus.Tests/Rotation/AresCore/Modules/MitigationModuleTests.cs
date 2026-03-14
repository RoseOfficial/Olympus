using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AresCore.Modules;

public class MitigationModuleTests
{
    private readonly MitigationModule _module = new();

    [Fact]
    public void TryExecute_MitigationDisabled_ReturnsFalse()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
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
    public void TryExecute_EmergencyHolmgang_CriticallyLowHp_ReturnsTrue()
    {
        // HP at 10% — Holmgang should fire
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WARActions.Holmgang.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.Holmgang.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 5000,     // 10% of 50000
            maxHp: 50000,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Holmgang.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Holmgang_HealthyHp_SkipsHolmgang()
    {
        // HP at 50% — Holmgang threshold is 15%, should not fire
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // All actions not ready — nothing fires
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 25000,   // 50%
            maxHp: 50000,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Holmgang.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Bloodwhetting_LowHp_ReturnsTrue()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.MitigationThreshold = 0.80f;

        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Holmgang not ready (HP above threshold)
        actionService.Setup(x => x.IsActionReady(WARActions.Holmgang.ActionId)).Returns(false);
        // Vengeance not triggering (cooldown service returns false)
        // Bloodwhetting ready
        actionService.Setup(x => x.IsActionReady(WARActions.Bloodwhetting.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(WARActions.RawIntuition.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.Bloodwhetting.ActionId || a.ActionId == WARActions.RawIntuition.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 30000,   // 60% — below 80% threshold
            maxHp: 50000,
            config: config,
            actionService: actionService,
            targetingService: targeting,
            tankCooldownShouldUseMitigation: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a =>
                a.ActionId == WARActions.Bloodwhetting.ActionId ||
                a.ActionId == WARActions.RawIntuition.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FullHp_NoMitigationFires()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false); // nothing ready

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 50000,  // 100% HP
            maxHp: 50000,
            config: config,
            actionService: actionService,
            tankCooldownShouldUseMitigation: false,
            tankCooldownShouldUseMajor: false);

        // At full HP with nothing ready, module should return false
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

    private static IAresContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        uint currentHp = 50000,
        uint maxHp = 50000,
        byte level = 100,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        bool tankCooldownShouldUseMitigation = false,
        bool tankCooldownShouldUseMajor = false)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= AresTestContext.CreateDefaultWarriorConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentHp: currentHp, maxHp: maxHp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IAresContext>();

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

        // WAR-specific state
        mock.Setup(x => x.HasHolmgang).Returns(false);
        mock.Setup(x => x.HasVengeance).Returns(false);
        mock.Setup(x => x.HasBloodwhetting).Returns(false);
        mock.Setup(x => x.HasActiveMitigation).Returns(false);
        mock.Setup(x => x.PartyHealthMetrics).Returns((1.0f, 1.0f, 0));

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(tankCooldownShouldUseMitigation);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(tankCooldownShouldUseMajor);
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);

        var statusHelper = new Olympus.Rotation.AresCore.Helpers.AresStatusHelper();
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);

        var partyHelper = new Olympus.Rotation.AresCore.Helpers.AresPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        var debugState = new AresDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
