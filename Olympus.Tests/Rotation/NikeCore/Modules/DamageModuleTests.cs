using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    #region Combo Rotation

    [Fact]
    public void TryExecute_Hakaze_UsedAtComboStep0()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Hakaze.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Hakaze.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Low level to use Hakaze (not Gyofu)
        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 80, // Before Gyofu (92)
            comboStep: 0,
            sen: SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka, // All Sen = skip Iaijutsu path
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Hakaze.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Gyofu_UsedAtLevel92()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Gyofu.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Gyofu.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 100, // Gyofu available
            comboStep: 0,
            sen: SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka, // Full Sen — skip Iaijutsu paths at this step
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Gyofu.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Iaijutsu — Sen Count

    [Fact]
    public void TryExecute_Higanbana_FiresAt1Sen_WhenNotOnTarget()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Higanbana.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 30, // Higanbana MinLevel
            sen: SAMActions.SenType.Setsu, // 1 Sen
            hasHiganbanaOnTarget: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Higanbana_SkipsWhenAlreadyOnTargetWithTimeRemaining()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // All actions not ready by default — DamageModule falls through to combo
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 100,
            sen: SAMActions.SenType.Setsu, // 1 Sen, would try Higanbana
            hasHiganbanaOnTarget: true,
            higanbanaRemaining: 30f, // Well above refresh threshold (5f)
            actionService: actionService,
            targetingService: targeting);

        // Higanbana should NOT be executed
        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Higanbana_RefreshedWhenAboutToExpire()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Higanbana.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 100,
            sen: SAMActions.SenType.Setsu, // 1 Sen
            hasHiganbanaOnTarget: true,
            higanbanaRemaining: 3f, // Below refresh threshold (5f) — should refresh
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MidareSetsugekka_FiresAt3Sen()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.MidareSetsugekka.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MidareSetsugekka.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 50, // Midare MinLevel
            sen: SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka, // 3 Sen
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MidareSetsugekka.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MidareSetsugekka_BelowMinLevel_Skips()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 40, // Below Midare MinLevel (50)
            sen: SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MidareSetsugekka.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Kaeshi and Ogi Namikiri

    [Fact]
    public void TryExecute_KaeshiNamikiri_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.KaeshiNamikiri.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiNamikiri.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90,
            hasKaeshiNamikiriReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiNamikiri.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_OgiNamikiri_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.OgiNamikiri.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.OgiNamikiri.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90,
            hasKaeshiNamikiriReady: false,
            hasOgiNamikiriReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.OgiNamikiri.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TsubameGaeshi_HighbananaFollowUp_UsesKaeshiHiganbana()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.KaeshiHiganbana.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiHiganbana.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 76,
            hasKaeshiNamikiriReady: false,
            hasTsubameGaeshiReady: true,
            lastIaijutsu: SAMActions.IaijutsuType.Higanbana,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiHiganbana.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TsubameGaeshi_MidareFollowUp_UsesKaeshiSetsugekka()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.KaeshiSetsugekka.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiSetsugekka.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 76,
            hasKaeshiNamikiriReady: false,
            hasTsubameGaeshiReady: true,
            lastIaijutsu: SAMActions.IaijutsuType.MidareSetsugekka,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.KaeshiSetsugekka.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Kenki Spenders (oGCD)

    [Fact]
    public void TryExecute_Shinten_FiresWhenKenki50OrMore()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.Shinten.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shinten.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 52, // Shinten MinLevel
            kenki: 50, // At threshold
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shinten.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Shinten_SkipsWhenKenkiBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 52,
            kenki: 40, // Below threshold (not >= 85 and not >= 50)
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Shinten.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Regression — Sen overcap guard (Higanbana vs Midare choice)

    /// <summary>
    /// Regression test: With 3 Sen and Higanbana NOT on target, Midare Setsugekka should fire
    /// (not Higanbana) because DamageModule uses SenCount=3 for Midare path.
    /// </summary>
    [Fact]
    public void TryExecute_3Sen_UsesMidare_NotHiganbana()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(SAMActions.MidareSetsugekka.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MidareSetsugekka.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 50,
            sen: SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka, // 3 Sen
            hasHiganbanaOnTarget: false, // Not on target — but Midare takes 3-Sen path
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // Should use Midare (3-Sen), not Higanbana (1-Sen path is guarded by SenCount==1)
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.MidareSetsugekka.ActionId),
            It.IsAny<ulong>()), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SAMActions.Higanbana.ActionId),
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

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService(countEnemiesInRange: enemyCount);

        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        return targeting;
    }

    private static INikeContext CreateContext(
        bool inCombat,
        bool canExecuteGcd,
        bool canExecuteOgcd = false,
        byte level = 100,
        int kenki = 0,
        SAMActions.SenType sen = SAMActions.SenType.None,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool hasKaeshiNamikiriReady = false,
        bool hasTsubameGaeshiReady = false,
        bool hasOgiNamikiriReady = false,
        bool hasMeikyoShisui = false,
        int meikyoStacks = 0,
        bool hasHiganbanaOnTarget = false,
        float higanbanaRemaining = 0f,
        SAMActions.IaijutsuType lastIaijutsu = SAMActions.IaijutsuType.None,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return NikeTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            kenki: kenki,
            sen: sen,
            comboStep: comboStep,
            lastComboAction: lastComboAction,
            comboTimeRemaining: comboTimeRemaining,
            hasKaeshiNamikiriReady: hasKaeshiNamikiriReady,
            hasTsubameGaeshiReady: hasTsubameGaeshiReady,
            hasOgiNamikiriReady: hasOgiNamikiriReady,
            hasMeikyoShisui: hasMeikyoShisui,
            meikyoStacks: meikyoStacks,
            hasHiganbanaOnTarget: hasHiganbanaOnTarget,
            higanbanaRemaining: higanbanaRemaining,
            lastIaijutsu: lastIaijutsu,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
