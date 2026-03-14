using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region GCD Combo — Single Target

    [Fact]
    public void TryExecute_TrueThrust_FiresAsComboStarter()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.TrueThrust.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.TrueThrust.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            lastComboAction: 0,
            comboTimeRemaining: 0f,
            hasFangAndClawBared: false,
            hasWheelInMotion: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.TrueThrust.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_VorpalThrust_FiresAfterTrueThrust()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Vorpal combo step
        actionService.Setup(x => x.IsActionReady(DRGActions.VorpalThrust.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.VorpalThrust.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            lastComboAction: DRGActions.TrueThrust.ActionId,
            comboTimeRemaining: 25f,
            hasFangAndClawBared: false,
            hasWheelInMotion: false,
            hasPowerSurge: true, // don't need disembowel path
            hasDotOnTarget: true,
            dotRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.VorpalThrust.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HeavensThrust_FiresAfterVorpalAtLevel86()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.HeavensThrust.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.HeavensThrust.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            lastComboAction: DRGActions.VorpalThrust.ActionId,
            comboTimeRemaining: 25f,
            hasFangAndClawBared: false,
            hasWheelInMotion: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.HeavensThrust.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Disembowel_FiresAfterTrueThrustWhenNeedsPowerSurge()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.Disembowel.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Disembowel.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            lastComboAction: DRGActions.TrueThrust.ActionId,
            comboTimeRemaining: 25f,
            hasFangAndClawBared: false,
            hasWheelInMotion: false,
            hasPowerSurge: false, // No power surge → use Disembowel path
            hasDotOnTarget: false,
            dotRemaining: 0f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Disembowel.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Positional Procs

    [Fact]
    public void TryExecute_Drakesbane_FiresWhenFangAndClawBaredAtLevel92()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.Drakesbane.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Drakesbane.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasFangAndClawBared: true,
            hasWheelInMotion: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Drakesbane.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FangAndClaw_FiresWhenBaredBeforeLevel92()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.FangAndClaw.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.FangAndClaw.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 91, // Below Drakesbane (92)
            hasFangAndClawBared: true,
            isAtFlank: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.FangAndClaw.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region oGCD — Life of the Dragon

    [Fact]
    public void TryExecute_MirageDive_FiresWhenDiveReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.MirageDive.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.MirageDive.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: true,
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.MirageDive.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MirageDive_SkipsWhenNoDiveReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.MirageDive.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Nastrond_FiresDuringLifeOfTheDragon()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // No Mirage Dive, Starcross, RiseOfDragon, WyrmwindThrust
        actionService.Setup(x => x.IsActionReady(DRGActions.MirageDive.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.Starcross.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.RiseOfTheDragon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.WyrmwindThrust.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.Nastrond.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Nastrond.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            hasStarcrossReady: false,
            hasDraconianFire: false,
            firstmindsFocus: 0,
            isLifeOfDragonActive: true,
            lifeOfDragonRemaining: 20f,
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Nastrond.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WyrmwindThrust_FiresWhenTwoFirstmindsStacks()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.MirageDive.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.Starcross.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.RiseOfTheDragon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.WyrmwindThrust.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.WyrmwindThrust.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            hasStarcrossReady: false,
            hasDraconianFire: false,
            firstmindsFocus: 2, // 2 stacks → fire WyrmwindThrust
            isLifeOfDragonActive: false,
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.WyrmwindThrust.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WyrmwindThrust_SkipsWhenOnlyOneStack()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            hasStarcrossReady: false,
            hasDraconianFire: false,
            firstmindsFocus: 1, // Only 1 stack — not enough
            isLifeOfDragonActive: false,
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.WyrmwindThrust.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Geirskogul_FiresWhenNotInLife()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.MirageDive.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.Starcross.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.RiseOfTheDragon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.WyrmwindThrust.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.Geirskogul.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Geirskogul.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            hasStarcrossReady: false,
            hasDraconianFire: false,
            firstmindsFocus: 0,
            isLifeOfDragonActive: false, // Not in Life → use Geirskogul
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Geirskogul.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Geirskogul_SkipsDuringLifeOfDragon()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDiveReady: false,
            hasStarcrossReady: false,
            hasDraconianFire: false,
            firstmindsFocus: 0,
            isLifeOfDragonActive: true, // In Life → skip Geirskogul
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.Geirskogul.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    private static IZeusContext CreateContext(
        bool inCombat,
        bool canExecuteGcd = false,
        bool canExecuteOgcd = false,
        byte level = 100,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool hasFangAndClawBared = false,
        bool hasWheelInMotion = false,
        bool hasDiveReady = false,
        bool hasStarcrossReady = false,
        bool hasDraconianFire = false,
        bool isAtFlank = false,
        bool isAtRear = false,
        int firstmindsFocus = 0,
        bool isLifeOfDragonActive = false,
        float lifeOfDragonRemaining = 0f,
        bool hasPowerSurge = true,
        bool hasDotOnTarget = true,
        float dotRemaining = 15f,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return ZeusTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            lastComboAction: lastComboAction,
            comboTimeRemaining: comboTimeRemaining,
            hasFangAndClawBared: hasFangAndClawBared,
            hasWheelInMotion: hasWheelInMotion,
            hasDiveReady: hasDiveReady,
            hasStarcrossReady: hasStarcrossReady,
            hasDraconianFire: hasDraconianFire,
            isAtFlank: isAtFlank,
            isAtRear: isAtRear,
            firstmindsFocus: firstmindsFocus,
            isLifeOfDragonActive: isLifeOfDragonActive,
            lifeOfDragonRemaining: lifeOfDragonRemaining,
            hasPowerSurge: hasPowerSurge,
            hasDotOnTarget: hasDotOnTarget,
            dotRemaining: dotRemaining,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
