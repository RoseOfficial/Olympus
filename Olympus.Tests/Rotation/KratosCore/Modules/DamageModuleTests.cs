using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.KratosCore.Modules;

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

    #region GCD — Form Rotation

    [Fact]
    public void TryExecute_DragonKick_FiresInOpoOpoFormWithoutLeadenFist()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.DragonKick.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.DragonKick.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            currentForm: MonkForm.OpoOpo,
            beastChakraCount: 0, // no Blitz
            hasLeadenFist: false,
            hasOpooposFury: false,
            hasFiresRumination: false,
            hasWindsRumination: false,
            hasPerfectBalance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.DragonKick.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LeapingOpo_FiresInOpoOpoFormWithLeadenFist()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.LeapingOpo.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.LeapingOpo.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            currentForm: MonkForm.OpoOpo,
            beastChakraCount: 0,
            hasLeadenFist: true, // Leaden Fist → use Leaping Opo at level 100
            hasOpooposFury: false,
            hasFiresRumination: false,
            hasWindsRumination: false,
            hasPerfectBalance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.LeapingOpo.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TrueStrike_FiresInRaptorForm()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.TrueStrike.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.TrueStrike.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 90, // Below RisingRaptor (96), so TrueStrike is used
            currentForm: MonkForm.Raptor,
            beastChakraCount: 0,
            hasDisciplinedFist: true,     // Buff healthy → skip TwinSnakes refresh
            disciplinedFistRemaining: 10f,
            hasFiresRumination: false,
            hasWindsRumination: false,
            hasPerfectBalance: false,
            hasRaptorsFury: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.TrueStrike.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SnapPunch_FiresInCoeurlForm()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.SnapPunch.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.SnapPunch.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 90, // Below PouncingCoeurl (96)
            currentForm: MonkForm.Coeurl,
            beastChakraCount: 0,
            hasFiresRumination: false,
            hasWindsRumination: false,
            hasPerfectBalance: false,
            hasCoeurlsFury: false,
            hasDemolishOnTarget: true, // Demolish up, so SnapPunch fires
            demolishRemaining: 12f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.SnapPunch.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region GCD — Masterful Blitz

    [Fact]
    public void TryExecute_PhantomRush_FiresWhenBothNadiAndThreeChakra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.PhantomRush.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PhantomRush.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            beastChakraCount: 3,
            beastChakra1: 1, // OpoOpo
            beastChakra2: 2, // Raptor
            beastChakra3: 3, // Coeurl
            hasLunarNadi: true,
            hasSolarNadi: true,
            hasBothNadi: true,
            hasFiresRumination: false,
            hasWindsRumination: false,
            hasPerfectBalance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PhantomRush.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MasterfulBlitz_SkipsWhenFewerThanThreeChakra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Allow form GCDs to fall through
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            beastChakraCount: 2, // Not enough
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PhantomRush.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region GCD — Rumination Procs

    [Fact]
    public void TryExecute_FiresReply_FiresWhenFiresRuminationActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.FiresReply.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.FiresReply.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            beastChakraCount: 0,
            hasFiresRumination: true, // Has proc
            hasWindsRumination: false,
            hasPerfectBalance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.FiresReply.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WindsReply_FiresWhenWindsRuminationActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.FiresReply.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.WindsReply.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.WindsReply.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            beastChakraCount: 0,
            hasFiresRumination: false,
            hasWindsRumination: true, // Has proc
            hasPerfectBalance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.WindsReply.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region oGCD — Chakra Spender

    [Fact]
    public void TryExecute_TheForbiddenChakra_FiresAtFiveChakra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.TheForbiddenChakra.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.TheForbiddenChakra.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            chakra: 5, // 5 Chakra → fire TheForbiddenChakra
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.TheForbiddenChakra.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ChakraSpender_SkipsWhenFewerThanFiveChakra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            chakra: 4, // Not enough
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.TheForbiddenChakra.ActionId),
            It.IsAny<ulong>()), Times.Never);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.Enlightenment.ActionId),
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

    private static IKratosContext CreateContext(
        bool inCombat,
        bool canExecuteGcd = false,
        bool canExecuteOgcd = false,
        byte level = 100,
        MonkForm currentForm = MonkForm.None,
        int chakra = 0,
        int beastChakraCount = 0,
        byte beastChakra1 = 0,
        byte beastChakra2 = 0,
        byte beastChakra3 = 0,
        bool hasLunarNadi = false,
        bool hasSolarNadi = false,
        bool hasBothNadi = false,
        bool hasDisciplinedFist = false,
        float disciplinedFistRemaining = 0f,
        bool hasLeadenFist = false,
        bool hasFiresRumination = false,
        bool hasWindsRumination = false,
        bool hasPerfectBalance = false,
        int perfectBalanceStacks = 0,
        bool hasRaptorsFury = false,
        bool hasCoeurlsFury = false,
        bool hasOpooposFury = false,
        bool hasDemolishOnTarget = false,
        float demolishRemaining = 0f,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return KratosTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            currentForm: currentForm,
            chakra: chakra,
            beastChakraCount: beastChakraCount,
            beastChakra1: beastChakra1,
            beastChakra2: beastChakra2,
            beastChakra3: beastChakra3,
            hasLunarNadi: hasLunarNadi,
            hasSolarNadi: hasSolarNadi,
            hasBothNadi: hasBothNadi,
            hasDisciplinedFist: hasDisciplinedFist,
            disciplinedFistRemaining: disciplinedFistRemaining,
            hasLeadenFist: hasLeadenFist,
            hasFiresRumination: hasFiresRumination,
            hasWindsRumination: hasWindsRumination,
            hasPerfectBalance: hasPerfectBalance,
            perfectBalanceStacks: perfectBalanceStacks,
            hasRaptorsFury: hasRaptorsFury,
            hasCoeurlsFury: hasCoeurlsFury,
            hasOpooposFury: hasOpooposFury,
            hasDemolishOnTarget: hasDemolishOnTarget,
            demolishRemaining: demolishRemaining,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
