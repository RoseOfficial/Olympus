using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        // Default targeting service returns null for FindEnemyForAction
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.BuffState);
    }

    #region Shoha

    [Fact]
    public void TryExecute_Shoha_FiresAt3MeditationStacks()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Shoha.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shoha.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            meditation: 3,
            level: 80, // Shoha MinLevel
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shoha.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Shoha_SkipsWhenMeditationBelow3()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            meditation: 2, // Not enough
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shoha.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Shoha_BelowMinLevel_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            meditation: 3,
            level: 70, // Below Shoha MinLevel (80)
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shoha.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Meikyo Shisui

    [Fact]
    public void TryExecute_MeikyoShisui_FiresWhenFugetsuMissing()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Shoha not ready (meditation < 3 via default context), Zanshin not ready, Ikishoten not ready
        actionService.Setup(x => x.IsActionReady(SAMActions.Shoha.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.Zanshin.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.Ikishoten.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.MeikyoShisui.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFugetsu: false, // Missing Fugetsu — should trigger Meikyo
            hasMeikyoShisui: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MeikyoShisui_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFugetsu: false,
            hasMeikyoShisui: true, // Already active
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_MeikyoShisui_BelowMinLevel_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasFugetsu: false,
            level: 40, // Below MeikyoShisui MinLevel (50)
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MeikyoShisui.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Ikishoten

    [Fact]
    public void TryExecute_Ikishoten_FiresWhenKenkiLow()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Shoha and Zanshin not applicable
        actionService.Setup(x => x.IsActionReady(SAMActions.Shoha.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.Zanshin.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.Ikishoten.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Ikishoten.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 30, // Below threshold (50), so Ikishoten is okay
            sen: SAMActions.SenType.Setsu, // 1 Sen — suppresses zero-Sen Meikyo trigger
            hasOgiNamikiriReady: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Ikishoten.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Ikishoten_SkipsWhenKenkiHighEnoughToOvercap()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 60, // Above threshold (50)
            hasOgiNamikiriReady: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Ikishoten.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Ikishoten_SkipsWhenOgiNamikiriAlreadyReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 0,
            hasOgiNamikiriReady: true, // Already have it
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Ikishoten.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Ikishoten_BelowMinLevel_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 0,
            level: 60, // Below Ikishoten MinLevel (68)
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Ikishoten.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Zanshin

    [Fact]
    public void TryExecute_Zanshin_FiresWhenReadyAndHasKenki()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Shoha.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(SAMActions.Zanshin.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Zanshin.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 50,
            hasZanshinReady: true,
            level: 96,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Zanshin.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Zanshin_SkipsWhenKenkiBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 40, // Below 50
            hasZanshinReady: true,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Zanshin.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Zanshin_WhenDisabled_NeverFires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableZanshin = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Zanshin.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            kenki: 50,
            hasZanshinReady: true,
            level: 96,
            actionService: actionService,
            targetingService: targeting,
            config: config);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Zanshin.ActionId),
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

    private static INikeContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        int kenki = 0,
        int meditation = 0,
        SAMActions.SenType sen = SAMActions.SenType.None,
        bool hasFugetsu = true,
        float fugetsuRemaining = 30f,
        bool hasFuka = true,
        float fukaRemaining = 30f,
        bool hasMeikyoShisui = false,
        bool hasOgiNamikiriReady = false,
        bool hasZanshinReady = false,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Configuration? config = null)
    {
        return NikeTestContext.Create(
            config: config,
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            kenki: kenki,
            meditation: meditation,
            sen: sen,
            hasFugetsu: hasFugetsu,
            fugetsuRemaining: fugetsuRemaining,
            hasFuka: hasFuka,
            fukaRemaining: fukaRemaining,
            hasMeikyoShisui: hasMeikyoShisui,
            hasOgiNamikiriReady: hasOgiNamikiriReady,
            hasZanshinReady: hasZanshinReady,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
