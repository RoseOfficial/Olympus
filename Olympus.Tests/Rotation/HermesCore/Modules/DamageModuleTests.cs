using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    #region Combo Rotation — Single Target

    [Fact]
    public void TryExecute_SpinningEdge_UsedAtComboStep0()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.SpinningEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.SpinningEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.SpinningEdge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_GustSlash_UsedAtComboStep1_AfterSpinningEdge()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.GustSlash.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.GustSlash.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 1,
            lastComboAction: NINActions.SpinningEdge.ActionId,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.GustSlash.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AeolianEdge_UsedAtComboStep2_AfterGustSlash_WithKazematoi()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.AeolianEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.AeolianEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 2,
            lastComboAction: NINActions.GustSlash.ActionId,
            kazematoi: 3, // Above KazematoiLowThreshold — use Aeolian Edge
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.AeolianEdge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ArmorCrush_UsedAtComboStep2_WhenKazematoiLow()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.ArmorCrush.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.ArmorCrush.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 2,
            lastComboAction: NINActions.GustSlash.ActionId,
            kazematoi: 1, // At KazematoiLowThreshold — use Armor Crush
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.ArmorCrush.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ArmorCrush_NotUsedBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Aeolian Edge available as fallback
        actionService.Setup(x => x.IsActionReady(NINActions.AeolianEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.AeolianEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 50, // Below ArmorCrush MinLevel (54)
            comboStep: 2,
            lastComboAction: NINActions.GustSlash.ActionId,
            kazematoi: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        // Should use Aeolian Edge, not Armor Crush
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.ArmorCrush.ActionId),
            It.IsAny<ulong>()), Times.Never);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.AeolianEdge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region AoE Combo Rotation

    [Fact]
    public void TryExecute_DeathBlossom_UsedWhenAoEThreshold()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3); // 3 enemies = AoE

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.DeathBlossom.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.DeathBlossom.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.DeathBlossom.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HakkeMujinsatsu_UsedAtAoEComboStep1()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.HakkeMujinsatsu.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.HakkeMujinsatsu.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            comboStep: 1,
            lastComboAction: NINActions.DeathBlossom.ActionId,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.HakkeMujinsatsu.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Raiju (Raiton proc)

    [Fact]
    public void TryExecute_Raiju_FiresWhenRaijuReadyAtLevel100()
    {
        // DistanceHelper.IsActionInRange uses the game API; in tests it returns false
        // (out-of-range for the melee SpinningEdge check), so ForkedRaiju is selected.
        // We verify that one of the two Raiju variants fires when Raiju Ready is active.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.ForkedRaiju.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(NINActions.FleetingRaiju.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a =>
                    a.ActionId == NINActions.ForkedRaiju.ActionId ||
                    a.ActionId == NINActions.FleetingRaiju.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasRaijuReady: true,
            raijuStacks: 1,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        // Verify that exactly one Raiju variant fired
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a =>
                a.ActionId == NINActions.ForkedRaiju.ActionId ||
                a.ActionId == NINActions.FleetingRaiju.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Raiju_SkipsWhenNotReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.SpinningEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.SpinningEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasRaijuReady: false, // Not ready
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // Should use SpinningEdge combo, not Raiju
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.FleetingRaiju.ActionId),
            It.IsAny<ulong>()), Times.Never);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.ForkedRaiju.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Raiju_SkipsBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.SpinningEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.SpinningEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 80, // Below ForkedRaiju MinLevel (90)
            hasRaijuReady: true,
            raijuStacks: 1,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.FleetingRaiju.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Phantom Kamaitachi

    [Fact]
    public void TryExecute_PhantomKamaitachi_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.PhantomKamaitachi.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.PhantomKamaitachi.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasRaijuReady: false, // Raiju not ready so Phantom has priority
            hasPhantomKamaitachiReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.PhantomKamaitachi.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PhantomKamaitachi_SkipsBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.SpinningEdge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.SpinningEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 80, // Below PhantomKamaitachi MinLevel (82)
            hasPhantomKamaitachiReady: true,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.PhantomKamaitachi.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Ninki Spenders (oGCD)

    [Fact]
    public void TryExecute_Bhavacakra_FiresWhenNinki50OrMore_SingleTarget()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(NINActions.Bhavacakra.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bhavacakra.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            ninki: 50, // At threshold
            hasMeisui: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bhavacakra.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Bhavacakra_SkipsWhenNinkiBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            ninki: 40, // Below threshold
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Bhavacakra.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ZeshoMeppo_UsedWithMeisui_Level96()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(NINActions.ZeshoMeppo.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.ZeshoMeppo.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            ninki: 50,
            hasMeisui: true, // Meisui active — should use ZeshoMeppo
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.ZeshoMeppo.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_HellfrogMedium_UsedForAoE_WhenNinki50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(NINActions.HellfrogMedium.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.HellfrogMedium.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            ninki: 50,
            hasMeisui: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.HellfrogMedium.ActionId),
            It.IsAny<ulong>()), Times.Once);
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

    private static IHermesContext CreateContext(
        bool inCombat,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        byte level = 100,
        int ninki = 0,
        int kazematoi = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool hasRaijuReady = false,
        int raijuStacks = 0,
        bool hasPhantomKamaitachiReady = false,
        bool hasMeisui = false,
        bool hasBunshin = false,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return HermesTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            ninki: ninki,
            kazematoi: kazematoi,
            comboStep: comboStep,
            lastComboAction: lastComboAction,
            comboTimeRemaining: comboTimeRemaining,
            hasRaijuReady: hasRaijuReady,
            raijuStacks: raijuStacks,
            hasPhantomKamaitachiReady: hasPhantomKamaitachiReady,
            hasMeisui: hasMeisui,
            hasBunshin: hasBunshin,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
