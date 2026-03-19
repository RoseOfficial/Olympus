using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

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

    #region GCD — Enshroud Sequence

    [Fact]
    public void TryExecute_Perfectio_FiresWhenPerfectioParataActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Perfectio.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Perfectio.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasPerfectioParata: true, // Proc ready → Perfectio fires
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Perfectio.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Communio_FiresAtOneLemureShroud()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Communio.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Communio.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: true,
            lemureShroud: 1,    // 1 remaining → use Communio
            enshroudTimer: 20f,
            hasPerfectioParata: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Communio.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_VoidReaping_FiresDuringEnshroud()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.VoidReaping.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.VoidReaping.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: true,
            lemureShroud: 3,       // 3 remaining — Communio not yet (>1)
            enshroudTimer: 20f,
            hasPerfectioParata: false,
            hasEnhancedVoidReaping: false,
            hasEnhancedCrossReaping: false, // No enhanced → default VoidReaping
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.VoidReaping.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region GCD — Soul Reaver

    [Fact]
    public void TryExecute_Gibbet_FiresWhenEnhancedGibbetAndSoulReaver()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Gibbet.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gibbet.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasSoulReaver: true,
            soulReaverStacks: 1,
            hasEnhancedGibbet: true,   // Enhanced → use Gibbet
            hasEnhancedGallows: false,
            isEnshrouded: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,      // Design up, no need to reapply
            deathsDesignRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gibbet.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SoulReaver_SkipsWhenNotActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            hasSoulReaver: false, // No Soul Reaver
            isEnshrouded: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 0,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a =>
                a.ActionId == RPRActions.Gibbet.ActionId ||
                a.ActionId == RPRActions.Gallows.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Plentiful Harvest

    [Fact]
    public void TryExecute_PlentifulHarvest_FiresWithImmortalSacrificeStacks()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.PlentifulHarvest.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.PlentifulHarvest.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 4, // Stacks ready
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.PlentifulHarvest.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PlentifulHarvest_SkipsWithNoStacks()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            hasSoulReaver: false,
            isEnshrouded: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0, // No stacks
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 0,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.PlentifulHarvest.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Death's Design

    [Fact]
    public void TryExecute_ShadowOfDeath_FiresWhenNoneOnTarget()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ShadowOfDeath.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ShadowOfDeath.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: false, // No debuff → apply it
            soul: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ShadowOfDeath.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ShadowOfDeath_SkipsWhenActiveWithGoodDuration()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f, // Good duration — don't refresh
            soul: 0,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ShadowOfDeath.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Soul Builder

    [Fact]
    public void TryExecute_SoulSlice_FiresWhenSoulBelowFifty()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.SoulSlice.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.SoulSlice.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 0, // Below 50 → use Soul Slice
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.SoulSlice.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SoulSlice_SkipsWhenSoulAtOrAboveFifty()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 50, // 50 Soul → don't build more
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.SoulSlice.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Basic Combo

    [Fact]
    public void TryExecute_Slice_FiresAsComboStarter()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Slice.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Slice.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 50, // Enough to skip SoulSlice
            comboStep: 0, // No combo in progress → Slice
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Slice.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WaxingSlice_FiresAfterSlice()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.WaxingSlice.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.WaxingSlice.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isEnshrouded: false,
            hasSoulReaver: false,
            hasPerfectioParata: false,
            immortalSacrificeStacks: 0,
            hasDeathsDesign: true,
            deathsDesignRemaining: 15f,
            soul: 50,
            comboStep: 1,
            lastComboAction: RPRActions.Slice.ActionId, // After Slice → WaxingSlice
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.WaxingSlice.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region oGCD — Gluttony

    [Fact]
    public void TryExecute_Gluttony_FiresWhenSoulAtFiftyAndReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Gluttony.ActionId)).Returns(true);
        // Enshroud is NOT ready (Shroud < 50 condition)
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gluttony.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            soul: 50,             // Enough Soul
            shroud: 40,           // Below 50 → Enshroud path not blocking Gluttony
            isEnshrouded: false,
            hasSoulReaver: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gluttony.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Gluttony_SkipsWhenSoulBelowFifty()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            soul: 40, // Below 50
            isEnshrouded: false,
            hasSoulReaver: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gluttony.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region oGCD — Lemure's Slice

    [Fact]
    public void TryExecute_LemuresSlice_FiresDuringEnshroudWithTwoVoidShroud()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.LemuresSlice.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.LemuresSlice.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            isEnshrouded: true,
            lemureShroud: 3,
            voidShroud: 2,       // Enough to spend
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.LemuresSlice.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LemuresSlice_SkipsWhenVoidShroudBelowTwo()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            isEnshrouded: true,
            lemureShroud: 3,
            voidShroud: 1, // Not enough
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.LemuresSlice.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Config Toggle Tests

    [Fact]
    public void TryExecute_SoulReaverDisabled_DoesNotExecuteGibbet()
    {
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableSoulReaver = false;

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Gibbet.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 70, // Gibbet min level
            hasSoulReaver: true,
            hasEnhancedGibbet: true,
            config: config,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Gibbet.ActionId),
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

    private static IThanatosContext CreateContext(
        bool inCombat,
        bool canExecuteGcd = false,
        bool canExecuteOgcd = false,
        byte level = 100,
        int soul = 0,
        int shroud = 0,
        int lemureShroud = 0,
        int voidShroud = 0,
        bool isEnshrouded = false,
        float enshroudTimer = 0f,
        bool hasSoulReaver = false,
        int soulReaverStacks = 0,
        bool hasEnhancedGibbet = false,
        bool hasEnhancedGallows = false,
        bool hasEnhancedVoidReaping = false,
        bool hasEnhancedCrossReaping = false,
        bool hasArcaneCircle = false,
        int immortalSacrificeStacks = 0,
        bool hasPerfectioParata = false,
        bool hasOblatio = false,
        bool hasDeathsDesign = false,
        float deathsDesignRemaining = 0f,
        int comboStep = 0,
        uint lastComboAction = 0,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Configuration? config = null)
    {
        return ThanatosTestContext.Create(
            config: config,
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            soul: soul,
            shroud: shroud,
            lemureShroud: lemureShroud,
            voidShroud: voidShroud,
            isEnshrouded: isEnshrouded,
            enshroudTimer: enshroudTimer,
            hasSoulReaver: hasSoulReaver,
            soulReaverStacks: soulReaverStacks,
            hasEnhancedGibbet: hasEnhancedGibbet,
            hasEnhancedGallows: hasEnhancedGallows,
            hasEnhancedVoidReaping: hasEnhancedVoidReaping,
            hasEnhancedCrossReaping: hasEnhancedCrossReaping,
            hasArcaneCircle: hasArcaneCircle,
            immortalSacrificeStacks: immortalSacrificeStacks,
            hasPerfectioParata: hasPerfectioParata,
            hasOblatio: hasOblatio,
            hasDeathsDesign: hasDeathsDesign,
            deathsDesignRemaining: deathsDesignRemaining,
            comboStep: comboStep,
            lastComboAction: lastComboAction,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
