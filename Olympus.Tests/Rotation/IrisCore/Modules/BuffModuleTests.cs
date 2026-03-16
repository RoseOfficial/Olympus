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

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = IrisTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_OgcdNotReady_ReturnsFalse()
    {
        var context = IrisTestContext.Create(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_Madeen_WhenMadeenReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RetributionOfTheMadeen.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RetributionOfTheMadeen.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MogOfTheAges_WhenMogReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MogOfTheAges.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,   // Madeen not ready — uses Mog instead
            mogReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MogOfTheAges.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_StarryMuse_WithLandscapeCanvas_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: true,
            hasLandscapeCanvas: true,
            hasStarryMuse: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_StarryMuse_WithoutLandscapeCanvas_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: true,
            hasLandscapeCanvas: false,   // No landscape — cannot use Starry Muse
            hasStarryMuse: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LivingMuse_WithCreatureCanvas_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // PomMuse is the default at Lv.100 with Pom creature motif
        actionService.Setup(x => x.ExecuteOgcd(
                It.IsAny<ActionDefinition>(),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: true,
            hasCreatureCanvas: true,
            creatureMotifType: PCTActions.CreatureMotifType.Pom,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.IsAny<ActionDefinition>(),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoBuffsNeeded_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    // ---- Task 4: SubtractiveSpectrum bypasses burst hold ----

    [Fact]
    public void TrySubtractivePalette_WithSubtractiveSpectrum_BypassesBurstHold()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.SubtractivePalette.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: true,
            canUseSubtractivePalette: true,
            paletteGauge: 50,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: true,
            isInBurstWindow: false,
            lucidDreamingReady: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.SubtractivePalette.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TrySubtractivePalette_WithoutSpectrum_HoldsForBurst_WhenGaugeLow()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: true,
            canUseSubtractivePalette: true,
            paletteGauge: 50,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: false,
            isInBurstWindow: false,
            lucidDreamingReady: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        // Held for burst window — should not fire
        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.SubtractivePalette.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // ---- Task 5: Tempera Grassa and Tempera Coat ----

    [Fact]
    public void TryExecute_TemperaGrassa_WhenPartyInjured_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaGrassa.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: true,
            temperaCoatReady: false,
            maxHp: 100000,
            currentHp: 95000,      // player healthy — party is injured
            partyHealthMetrics: (0.80f, 0.70f, 2),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaGrassa.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TemperaGrassa_WhenPartyHealthy_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: true,
            maxHp: 100000,
            currentHp: 100000,
            partyHealthMetrics: (0.95f, 0.90f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaGrassa.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TemperaCoat_WhenPlayerInjured_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaCoat.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: false,
            temperaCoatReady: true,
            maxHp: 100000,
            currentHp: 70000,      // 70% HP — below 80% threshold
            partyHealthMetrics: (0.95f, 0.90f, 0),  // party healthy
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaCoat.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TemperaCoat_ConfigDisabled_DoesNotFire()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableTemperaCoat = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: false,
            temperaCoatReady: true,
            maxHp: 100000,
            currentHp: 60000,
            partyHealthMetrics: (0.95f, 0.90f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.TemperaCoat.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // ---- Task 6: Smudge ----

    [Fact]
    public void TryExecute_Smudge_WhenMoving_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Smudge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: false,
            temperaCoatReady: false,
            smudgeReady: true,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Smudge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Smudge_WhenNotMoving_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            smudgeReady: true,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Smudge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // ---- Task 7: Swiftcast ----

    [Fact]
    public void TryExecute_Swiftcast_WhenMovingWithNoInstants_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Swiftcast.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            temperaGrassaReady: false,
            temperaCoatReady: false,
            smudgeReady: false,
            swiftcastReady: true,
            hasInstantCast: false,
            hasWhitePaint: false,
            hasBlackPaint: false,
            hasRainbowBright: false,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Swiftcast_WhenMovingButHasWhitePaint_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            swiftcastReady: true,
            hasInstantCast: false,
            hasWhitePaint: true,   // has instant via HolyInWhite
            hasBlackPaint: false,
            hasRainbowBright: false,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Swiftcast_WhenNotMoving_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
            lucidDreamingReady: false,
            swiftcastReady: true,
            hasInstantCast: false,
            hasWhitePaint: false,
            hasBlackPaint: false,
            hasRainbowBright: false,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // ---- Task 8: Living Muse charge cap ----

    [Fact]
    public void TryLivingMuse_AtChargeCap_FiresImmediately()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.PomMuse.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: true,
            livingMuseCharges: 2,  // at cap
            hasCreatureCanvas: true,
            creatureMotifType: PCTActions.CreatureMotifType.Pom,
            isInBurstWindow: false,  // outside burst — still fires at cap
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.PomMuse.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryLivingMuse_WithNoCanvas_DoesNotFire()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: true,
            livingMuseCharges: 1,
            hasCreatureCanvas: false,  // no canvas — cannot fire
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.IsAny<ActionDefinition>(),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryLivingMuse_AtLevelCapWithMawCanvas_FiresFangedMuse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FangedMuse.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            level: 100,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: true,
            livingMuseCharges: 1,
            hasCreatureCanvas: true,
            creatureMotifType: PCTActions.CreatureMotifType.Maw,
            partyHealthMetrics: (1.0f, 1.0f, 0),
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.FangedMuse.ActionId),
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
