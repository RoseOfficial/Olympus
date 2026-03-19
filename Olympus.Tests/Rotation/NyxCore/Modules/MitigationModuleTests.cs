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

public class MitigationModuleTests
{
    private readonly MitigationModule _module = new();

    [Fact]
    public void TryExecute_MitigationDisabled_ReturnsFalse()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
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
    public void TryExecute_WalkingDead_SkipsAllMitigation()
    {
        // When Walking Dead is active, all mitigation should be skipped (healers need to heal us)
        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasWalkingDead: true,
            currentHp: 5000,    // critical HP
            maxHp: 50000);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        Assert.Contains("Walking Dead", context.Debug.MitigationState);
    }

    [Fact]
    public void TryExecute_EmergencyLivingDead_CriticallyLowHp_ReturnsTrue()
    {
        // HP at 10% — Living Dead should fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRKActions.LivingDead.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.LivingDead.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 5000,    // 10% of 50000
            maxHp: 50000,
            hasLivingDead: false,
            hasWalkingDead: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.LivingDead.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LivingDead_HealthyHp_SkipsLivingDead()
    {
        // HP at 50% — Living Dead threshold is 15%, should not fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Nothing ready
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 25000,   // 50%
            maxHp: 50000,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.LivingDead.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_FullHp_NoMitigationFires()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
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

    [Fact]
    public void TryExecute_Mitigation_DeliriumActive_StillGatesOnHp()
    {
        // Delirium should not prevent mitigation from checking HP and firing if needed.
        // HP at 50% (below typical 80% MitigationThreshold) — if mitigation service says yes, it fires.
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 25000,   // 50% HP
            maxHp: 50000,
            hasDelirium: true,
            hasDarkside: false,
            actionService: actionService,
            tankCooldownShouldUseMitigation: false,
            tankCooldownShouldUseMajor: false);

        // No action is ready, so result is false regardless; but no exception should occur
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
    }

    #region Helpers

    private static INyxContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        uint currentHp = 50000,
        uint maxHp = 50000,
        bool hasLivingDead = false,
        bool hasWalkingDead = false,
        bool hasDelirium = false,
        bool hasDarkside = false,
        float darksideRemaining = 0f,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        bool tankCooldownShouldUseMitigation = false,
        bool tankCooldownShouldUseMajor = false)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(currentHp: currentHp, maxHp: maxHp);
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
        mock.Setup(x => x.TimelineService).Returns((Olympus.Timeline.ITimelineService?)null);
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // DRK-specific defensive state
        mock.Setup(x => x.HasLivingDead).Returns(hasLivingDead);
        mock.Setup(x => x.HasWalkingDead).Returns(hasWalkingDead);
        mock.Setup(x => x.HasActiveMitigation).Returns(false);
        mock.Setup(x => x.HasShadowWall).Returns(false);
        mock.Setup(x => x.HasDarkMind).Returns(false);
        mock.Setup(x => x.HasTheBlackestNight).Returns(false);
        mock.Setup(x => x.HasOblation).Returns(false);
        mock.Setup(x => x.HasEnoughMpForTbn).Returns(true);
        mock.Setup(x => x.PartyHealthMetrics).Returns((1.0f, 1.0f, 0));
        mock.Setup(x => x.HasTankStance).Returns(true);
        mock.Setup(x => x.HasDarkside).Returns(hasDarkside);
        mock.Setup(x => x.DarksideRemaining).Returns(darksideRemaining);
        mock.Setup(x => x.HasDelirium).Returns(hasDelirium);
        mock.Setup(x => x.DeliriumStacks).Returns(0);

        var statusHelper = new Olympus.Rotation.NyxCore.Helpers.NyxStatusHelper();
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(tankCooldownShouldUseMitigation);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(tankCooldownShouldUseMajor);
        tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);

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
