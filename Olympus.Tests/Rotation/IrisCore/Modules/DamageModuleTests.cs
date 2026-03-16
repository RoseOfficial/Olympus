using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        // Out of combat with nothing to prepaint — returns false
        var context = IrisTestContext.Create(
            inCombat: false,
            canExecuteGcd: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            needsLandscapeMotif: false);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_GcdNotReady_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_StarPrism_WhenStarstruckActive_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarPrism.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasStarstruck: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarPrism.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RainbowDrip_WithRainbowBright_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RainbowDrip.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasStarstruck: false,
            hasRainbowBright: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RainbowDrip.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HammerStamp_WhenHammerTimeActive_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.HammerStamp.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: true,
            hammerTimeStacks: 3,
            isInHammerCombo: false,
            hammerComboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.HammerStamp.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_CometInBlack_WithBlackPaint_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.CometInBlack.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            isInHammerCombo: false,
            hasBlackPaint: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.CometInBlack.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PrepaintLandscape_OutOfCombat_WhenNeeded_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarrySkyMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: false,
            canExecuteGcd: true,
            isCasting: false,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarrySkyMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    // ---- Task 2-A: Creature motif selection uses LivingMuseCharges ----

    [Fact]
    public void TryPrepaintCreature_Lv96_WithChargesAtZero_PaintsClawMotif()
    {
        // creatureCount = 0 (even) → ClawMotif at 96+
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.ClawMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: false,
            level: 100,
            canExecuteGcd: true,
            isCasting: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            livingMuseCharges: 0,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.ClawMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryPrepaintCreature_Lv96_WithChargesAtOne_PaintsMawMotif()
    {
        // creatureCount = 1 (odd) → MawMotif at 96+
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MawMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: false,
            level: 100,
            canExecuteGcd: true,
            isCasting: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            livingMuseCharges: 1,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MawMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryPrepaintCreature_Lv50_WithChargesAtZero_PaintsPomMotif()
    {
        // creatureCount = 0 (even) → PomMotif at level < 96
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.PomMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: false,
            level: 50,
            canExecuteGcd: true,
            isCasting: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            livingMuseCharges: 0,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.PomMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryPrepaintCreature_Lv50_WithChargesAtOne_PaintsWingMotif()
    {
        // creatureCount = 1 (odd) → WingMotif at level < 96
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.WingMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: false,
            level: 50,
            canExecuteGcd: true,
            isCasting: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            livingMuseCharges: 1,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.WingMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    // ---- Task 2-B: In-combat Inspiration motif painting ----

    [Fact]
    public void TryGcdDamage_WithInspiration_LandscapeNeeded_PaintsStarrySkyMotif()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarrySkyMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarrySkyMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryGcdDamage_WithInspiration_NoMotifsNeeded_SkipsToNormalRotation()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Set up base combo (FireInRed) to fire — so the normal rotation proceeds
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FireInRed.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: false,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            baseComboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        // Confirmed Inspiration short-circuit was skipped and normal combo fired
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FireInRed.ActionId),
            It.IsAny<ulong>()), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarrySkyMotif.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // ---- Fix 1: isMoving guard in TryMotifDuringInspiration ----

    [Fact]
    public void TryGcdDamage_WithInspiration_CreatureNeeded_WhileMoving_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: true);

        // Moving — motif cannot be cast (cast time, not instant)
        Assert.False(result);
    }

    [Fact]
    public void TryGcdDamage_WithInspiration_WeaponNeeded_WhileMoving_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: false,
            needsCreatureMotif: false,
            needsWeaponMotif: true,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: true);

        // Moving — motif cannot be cast (cast time, not instant)
        Assert.False(result);
    }

    [Fact]
    public void TryGcdDamage_WithInspiration_CreatureNeeded_NotMoving_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // At level 100 with 0 charges: ClawMotif (even charge count)
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.ClawMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            level: 100,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: false,
            needsCreatureMotif: true,
            needsWeaponMotif: false,
            livingMuseCharges: 0,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.ClawMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryGcdDamage_WithInspiration_WeaponNeeded_NotMoving_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.HammerMotif.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasInspiration: true,
            needsLandscapeMotif: false,
            needsCreatureMotif: false,
            needsWeaponMotif: true,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.HammerMotif.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    // ---- Task 3: Hyperphantasia movement override ----

    [Fact]
    public void TryGcdDamage_WithHyperphantasia_WhileMoving_AllowsBaseCombo()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FireInRed.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isMoving: true,
            hasHyperphantasia: true,
            hyperphantasiaStacks: 3,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            isInHammerCombo: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: false,
            baseComboStep: 0,
            hasInspiration: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FireInRed.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryGcdDamage_WithoutHyperphantasia_WhileMoving_SkipsBaseCombo()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isMoving: true,
            hasHyperphantasia: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            hasHammerTime: false,
            isInHammerCombo: false,
            hasBlackPaint: false,
            hasWhitePaint: false,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: false,
            hasInspiration: false,
            needsLandscapeMotif: false,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: true);

        // No instants available; base combo blocked by movement
        Assert.False(result);
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

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    #endregion
}
