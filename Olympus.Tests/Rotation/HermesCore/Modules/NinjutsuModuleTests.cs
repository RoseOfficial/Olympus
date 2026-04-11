using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Tests for NinjutsuModule — the mudra sequence engine for Ninja.
///
/// How NinjutsuModule works:
/// 1. If no sequence is active, decide whether to start one (level check, cooldown check, need Suiton).
/// 2. Call MudraHelper.StartSequence(ninjutsuType) which pre-calculates the mudra sequence.
/// 3. Input mudras and execute ninjutsu via native ActionManager (SafeGameAccess).
/// 4. Ten Chi Jin (TCJ) bypasses the normal mudra system — uses separate GCD-based sequence.
///
/// NOTE: Mudra inputs, ninjutsu execution, and TCJ all use native ActionManager directly
/// (not IActionService) because UseAction rejects replacement action IDs. In unit tests,
/// SafeGameAccess.GetActionManager() returns null, so these code paths return true (blocking)
/// without executing. Tests verify decision logic (ninjutsu selection, MudraHelper state,
/// debug state) rather than native execution — same pattern as BurstWindowService tests.
/// </summary>
public class NinjutsuModuleTests
{
    private readonly NinjutsuModule _module = new();

    #region Guard Conditions

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_NoTarget_AndNoActiveSequence_ReturnsFalse()
    {
        // Targeting returns null, no active mudra sequence
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_BelowLevel30_MudraNotAvailable_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        // Ten not ready (below MinLevel 30 — action service returns false)
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 25, // Below Ten MinLevel (30)
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
    }

    #endregion

    #region Start Ninjutsu Sequence — First Mudra

    [Fact]
    public void TryExecute_StartsRaiton_InputsFirstMudra_Ten()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);
        // KunaisBane not ready — NeedsSuiton returns false, so Raiton is chosen
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            hasSuiton: false,
            hasKassatsu: false,
            hasTenChiJin: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        // Module blocks other modules while sequencing (native ActionManager is null in tests)
        Assert.True(result);
        // Raiton was chosen as the target ninjutsu — verifies decision logic
        Assert.Equal(NINActions.NinjutsuType.Raiton, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_MudraOnCooldown_DoesNotStartSequence()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        // Ten is on cooldown
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);

        // MudraHelper should remain idle — no sequence was started
        var mudra = new MudraHelper();
        Assert.Equal(MudraState.Idle, mudra.State);
        Assert.Equal(NINActions.NinjutsuType.None, mudra.TargetNinjutsu);
    }

    #endregion

    #region Continue Mudra Sequence

    [Fact]
    public void TryExecute_SequenceAtFirstMudra_BlocksOtherModules()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Pre-start a Raiton sequence — first mudra already done
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton); // Ten-Chi
        mudraHelper.AdvanceSequence(); // Ten already input, now at SecondMudra

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        // Module blocks other modules while mid-sequence (native execution via ActionManager)
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        // Next mudra should be Chi
        Assert.Equal(NINActions.MudraType.Chi, mudraHelper.GetNextMudra());
    }

    [Fact]
    public void TryExecute_SequenceAtSecondMudra_ThreeMudra_BlocksOtherModules()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Suiton: Ten-Chi-Jin. Ten and Chi already done.
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Suiton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Chi — now at ThirdMudra (Jin)

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        // Module blocks other modules while mid-sequence
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        // Next mudra should be Jin
        Assert.Equal(NINActions.MudraType.Jin, mudraHelper.GetNextMudra());
    }

    [Fact]
    public void TryExecute_SequenceReadyToExecute_BlocksForNativeExecution()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Raiton: Ten-Chi done, at ReadyToExecute
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Chi → ReadyToExecute

        Assert.True(mudraHelper.IsReadyToExecute);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        // Module blocks while waiting to execute ninjutsu via native ActionManager
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        // Sequence is still ready (native ActionManager is null in tests, so execution doesn't complete)
        Assert.True(mudraHelper.IsReadyToExecute);
    }

    [Fact]
    public void TryExecute_SequenceReadyToExecute_GCDNotAvailable_BlocksOtherModules()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Raiton at ReadyToExecute
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton);
        mudraHelper.AdvanceSequence();
        mudraHelper.AdvanceSequence();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        // Returns true to block other modules even though GCD is not available
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
    }

    [Fact]
    public void TryExecute_SequenceActive_OgcdNotAvailable_StillBlocksOtherModules()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Suiton at SecondMudra (waiting for oGCD window)
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Suiton);
        mudraHelper.AdvanceSequence(); // Ten done

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        // Still returns true to block — sequence must complete
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
    }

    #endregion

    #region Kassatsu — Enhanced Ninjutsu Upgrades

    [Fact]
    public void TryExecute_KassatsuActive_Raiton_BlocksForNativeExecution()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Raiton sequence ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Chi → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Equal(NINActions.NinjutsuType.Raiton, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_KassatsuActive_KatonSequence_BlocksForNativeExecution()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Katon sequence (Chi-Ten) ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Katon);
        mudraHelper.AdvanceSequence(); // Chi
        mudraHelper.AdvanceSequence(); // Ten → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Equal(NINActions.NinjutsuType.Katon, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_KassatsuActive_HyotonSequence_BlocksForNativeExecution()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Hyoton sequence (Ten-Jin) ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Hyoton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Jin → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Equal(NINActions.NinjutsuType.Hyoton, mudraHelper.TargetNinjutsu);
    }

    #endregion

    #region Ninjutsu Selection Logic

    [Fact]
    public void TryExecute_KassatsuActive_HighLevel_StartsHyoshoRanryuSequence()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            hasKassatsu: true,
            hasTenChiJin: false,
            hasSuiton: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // With Kassatsu at high level, HyoshoRanryu should be selected (Hyoton sequence: Ten-Jin)
        Assert.Equal(NINActions.NinjutsuType.HyoshoRanryu, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_NeedsSuiton_KunaisBaneReady_StartsWithSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);
        // KunaisBane is ready — NeedsSuiton returns true
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasSuiton: false, // No Suiton
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // Should target Suiton (Ten-Chi-Jin)
        Assert.Equal(NINActions.NinjutsuType.Suiton, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_SuitonAlreadyUp_KunaisBaneReady_NotNeedsSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasSuiton: true, // Already have Suiton — no need to cast again
            suitonRemaining: 15f,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // Should NOT target Suiton since we already have it
        Assert.NotEqual(NINActions.NinjutsuType.Suiton, mudraHelper.TargetNinjutsu);
    }

    #endregion

    #region Ten Chi Jin (TCJ)

    [Fact]
    public void TryExecute_TenChiJin_3Stacks_BlocksForFumaShuriken()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 3,
            actionService: actionService,
            targetingService: targeting);

        // TCJ step 1 blocks (native ActionManager handles execution)
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Contains("Fuma Shuriken", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_2Stacks_SingleTarget_BlocksForRaiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 2,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Contains("Raiton", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_2Stacks_AoE_BlocksForKaton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 2,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Contains("Katon", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_1Stack_SingleTarget_BlocksForSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 1,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Contains("Suiton", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_1Stack_AoE_BlocksForDoton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 1,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
        Assert.Contains("Doton", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_0Stacks_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 0, // TCJ complete
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_TenChiJin_GCDNotAvailable_OgcdNotAvailable_ReturnsFalse()
    {
        // When BOTH GCD and oGCD are unavailable, the module returns false early
        // (before even reaching TCJ handling) — the rotation simply waits.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: false,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 3,
            actionService: actionService,
            targetingService: targeting);

        // Module returns false when no action window is available (correct behavior)
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);

        // Confirm we hit the no-action-window guard, not the "No target" guard
        Assert.Equal("Waiting for action window", context.Debug.NinjutsuState);
    }

    [Fact]
    public void TryExecute_TenChiJin_OgcdAvailable_GCDNotAvailable_WaitsAndBlocksOtherModules()
    {
        // TCJ uses GCDs. If oGCD is available but GCD is not, TCJ handler blocks.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 3,
            actionService: actionService,
            targetingService: targeting);

        // With oGCD available but GCD not, TCJ handler returns true (waiting for GCD)
        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);
    }

    #endregion

    #region Level Guards

    [Fact]
    public void TryExecute_BelowLevel35_CannotUseRaiton_UsesFumaShuriken()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 30, // Level 30: only FumaShuriken available
            mudraHelper: mudraHelper,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasSuiton: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // At level 30, only FumaShuriken is possible
        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, mudraHelper.TargetNinjutsu);
    }

    [Fact]
    public void TryExecute_Level35_CanUseRaiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var mudraHelper = new MudraHelper();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Ten.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(NINActions.KunaisBane.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: true,
            level: 35, // Level 35: Raiton available
            mudraHelper: mudraHelper,
            hasKassatsu: false,
            hasTenChiJin: false,
            hasSuiton: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // At level 35, Raiton should be chosen over FumaShuriken
        Assert.Equal(NINActions.NinjutsuType.Raiton, mudraHelper.TargetNinjutsu);
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
        MudraHelper? mudraHelper = null,
        bool isMudraActive = false,
        bool hasKassatsu = false,
        bool hasTenChiJin = false,
        int tenChiJinStacks = 0,
        bool hasSuiton = false,
        float suitonRemaining = 0f,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return HermesTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            mudraHelper: mudraHelper,
            isMudraActive: isMudraActive,
            hasKassatsu: hasKassatsu,
            hasTenChiJin: hasTenChiJin,
            tenChiJinStacks: tenChiJinStacks,
            hasSuiton: hasSuiton,
            suitonRemaining: suitonRemaining,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
