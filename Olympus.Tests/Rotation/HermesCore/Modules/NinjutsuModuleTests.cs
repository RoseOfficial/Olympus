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
/// 3. Input the first mudra via ExecuteOgcd (the module blocks all other modules while sequencing).
/// 4. Each frame: if IsSequenceActive but not IsReadyToExecute, continue calling InputNextMudra.
/// 5. When IsReadyToExecute, call ExecuteGcd with the appropriate Ninjutsu action.
/// 6. On success, MudraHelper.CompleteSequence() resets the state.
/// Ten Chi Jin (TCJ) bypasses the normal mudra system — uses separate GCD-based sequence.
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
            hasSuiton: false,   // Needs Suiton → will choose Suiton (if KunaisBane ready)
            // Make Ten Chi Jin not active, Kassatsu not active
            hasKassatsu: false,
            hasTenChiJin: false,
            actionService: actionService,
            targetingService: targeting);

        // Let the context use a real MudraHelper so we can verify state changes
        _module.TryExecute(context, isMoving: false);

        // Ten should have been used as first mudra
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
            It.IsAny<ulong>()), Times.AtLeastOnce);
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

        // No mudra should have been input
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Continue Mudra Sequence

    [Fact]
    public void TryExecute_SequenceAtFirstMudra_InputsNextMudra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Pre-start a Raiton sequence — first mudra already done
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton); // Ten-Chi
        mudraHelper.AdvanceSequence(); // Ten already input, now at SecondMudra

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(NINActions.Chi.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Chi.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Chi.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SequenceAtSecondMudra_ThreeMudra_InputsThirdMudra()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Suiton: Ten-Chi-Jin. Ten and Chi already done.
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Suiton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Chi — now at ThirdMudra (Jin)

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.IsActionReady(NINActions.Jin.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Jin.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            canExecuteGcd: false,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Jin.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SequenceReadyToExecute_ExecutesNinjutsu_GCD()
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
        actionService.Setup(x => x.IsActionReady(NINActions.Raiton.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
    public void TryExecute_KassatsuActive_Raiton_ExecutesKassatsuRaiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Raiton sequence ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Raiton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Chi → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        // With Kassatsu, Raiton is still Raiton (but enhanced damage)
        actionService.Setup(x => x.IsActionReady(NINActions.Raiton.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true, // Kassatsu active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_KassatsuActive_KatonSequence_ExecutesGokaMekkyaku()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Katon sequence (Chi-Ten) ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Katon);
        mudraHelper.AdvanceSequence(); // Chi
        mudraHelper.AdvanceSequence(); // Ten → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.GokaMekkyaku.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.GokaMekkyaku.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100, // level >= 76 for GokaMekkyaku
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true, // Kassatsu upgrades Katon → GokaMekkyaku
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.GokaMekkyaku.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_KassatsuActive_HyotonSequence_ExecutesHyoshoRanryu()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        // Kassatsu + Hyoton sequence (Ten-Jin) ready
        var mudraHelper = new MudraHelper();
        mudraHelper.StartSequence(NINActions.NinjutsuType.Hyoton);
        mudraHelper.AdvanceSequence(); // Ten
        mudraHelper.AdvanceSequence(); // Jin → ReadyToExecute

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.HyoshoRanryu.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.HyoshoRanryu.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100, // level >= 76 for HyoshoRanryu
            mudraHelper: mudraHelper,
            isMudraActive: true,
            hasKassatsu: true, // Kassatsu upgrades Hyoton → HyoshoRanryu
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.HyoshoRanryu.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
        // Hyoton needs Ten-Jin. With Kassatsu, recommendation is HyoshoRanryu which uses Hyoton sequence.
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
            hasKassatsu: true,
            hasTenChiJin: false,
            hasSuiton: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        // Should have started with Ten (first mudra for HyoshoRanryu = Hyoton = Ten-Jin)
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Ten.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
    public void TryExecute_TenChiJin_3Stacks_ExecutesFumaShuriken()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.FumaShuriken.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.FumaShuriken.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 3, // First TCJ action
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.FumaShuriken.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenChiJin_2Stacks_SingleTarget_ExecutesRaiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Raiton.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 2, // Second TCJ action
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Raiton.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenChiJin_2Stacks_AoE_ExecutesKaton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3); // AoE threshold

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Katon.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Katon.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

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

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Katon.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenChiJin_1Stack_SingleTarget_ExecutesSuiton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, 1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Suiton.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Suiton.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: false,
            canExecuteGcd: true,
            level: 100,
            hasTenChiJin: true,
            tenChiJinStacks: 1, // Third TCJ action
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.True(result);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Suiton.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TenChiJin_1Stack_AoE_ExecutesDoton()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(NINActions.Doton.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == NINActions.Doton.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

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

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.Doton.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
