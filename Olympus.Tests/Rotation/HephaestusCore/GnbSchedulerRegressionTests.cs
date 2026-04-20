using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HephaestusCore;

/// <summary>
/// Regression suite for GNB bugs fixed in v4.13 to v4.15.
/// Each test documents a previously shipped bug and asserts the correct
/// scheduler-path behavior that fixes it. A regression shows up as a
/// failing test before any other symptom appears.
/// </summary>
public class GnbSchedulerRegressionTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // Regression 1 (v4.15): Gnashing Fang combo persistence after Continuation weave
    //
    // Bug: AmmoComboStep advanced to 1 after Gnashing Fang, but the Continuation
    // oGCD (JugularRip) consumed the ReadyToRip buff before the next GCD frame.
    // The old code polled the buff instead of AmmoComboStep, so it saw the combo
    // as dropped and reverted to Keen Edge.
    //
    // Fix: scheduler gates SavageClaw/WickedTalon via AmmoComboStep, not buff poll.
    // Both are pushed unconditionally; the scheduler ComboStep predicate selects
    // the right one at dispatch time regardless of whether the buff still exists.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_GnashingFangCombo_BothStepsPushed_Regardless_Of_Continuation_Weave()
    {
        // Simulate: JugularRip was just weaved (buff gone), AmmoComboStep = 1
        // Old code would see no ReadyToRip and treat combo as dropped.
        // Correct code: SavageClaw is still in queue because it relies on AmmoComboStep gate.
        var enemy = CreateMockEnemy(11111UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // No procs active (as if Continuation was already weaved), combo step 1 in gauge
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 1, level: 60,
            isReadyToRip: false, isReadyToTear: false, isReadyToGouge: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        // Regression check: both combo-continuation steps must be queued
        // regardless of whether their corresponding buffs are still present.
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.SavageClaw && c.Priority == 1);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.WickedTalon && c.Priority == 1);

        // The ability definitions must use AmmoComboStep predicates, not buff polls
        Assert.NotNull(GnbAbilities.SavageClaw.ComboStep);
        Assert.NotNull(GnbAbilities.WickedTalon.ComboStep);
    }

    // -----------------------------------------------------------------------
    // Regression 2 (v4.14): Reign chain persistence
    //
    // Bug: After Reign of Beasts fired, Noble Blood and Lion Heart did not queue
    // because the old code called GetAdjustedActionId(ReignOfBeasts) which silently
    // returns ReignOfBeasts (not Noble Blood) once the action-replacement chain
    // advances -- the game does not expose the replacement via that API.
    //
    // Fix: Noble Blood and Lion Heart are tracked via AmmoComboStep 3 and 4.
    // All three are pushed unconditionally; scheduler picks via ComboStep/ProcBuff.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_ReignChain_AllThreeStepsPushed_AtPriority2()
    {
        var enemy = CreateMockEnemy(22222UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // Level 100 required for ReignOfBeasts; isReadyToReign=false simulates mid-chain
        // (ReignOfBeasts gate is ProcBuff, Noble/Lion use ComboStep -- they must still be pushed)
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 100, isReadyToReign: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.ReignOfBeasts && c.Priority == 2);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.NobleBlood    && c.Priority == 2);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.LionHeart     && c.Priority == 2);

        // Regression: the step predicates must exist (they were missing in the bug)
        Assert.NotNull(GnbAbilities.NobleBlood.ComboStep);
        Assert.NotNull(GnbAbilities.LionHeart.ComboStep);
    }

    // -----------------------------------------------------------------------
    // Regression 3 (v4.13): Gnashing Fang held indefinitely when No Mercy
    // cooldown is very long (> 30s)
    //
    // Bug: Original hold logic checked "is No Mercy on cooldown?" without a
    // time cap. GnashingFang would never fire between No Mercy windows because
    // the CD was always non-zero.
    //
    // Fix: hold only when nmCd > 0 && nmCd < 8f (imminent window).
    // When CD > 30s the hold is skipped and GnashingFang fires freely.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_GnashingFang_NotHeld_When_NoMercyCooldown_Exceeds_30s()
    {
        var enemy = CreateMockEnemy(33333UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // NoMercy is a long way off -- must NOT suppress GnashingFang
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(35f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 1, level: 60,
            hasNoMercy: false, hasMaxCartridges: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        // Regression: GnashingFang must be in the queue even when No Mercy CD > 30s
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.GnashingFang && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // Regression 4 (v4.15): Fated Brand (Continuation for Fated Circle) missing
    //
    // Bug: The Continuation push block did not include FatedBrand (added in v4.15).
    // Fated Circle would fire but the ReadyToBrand proc was never consumed.
    //
    // Fix: FatedBrand pushed at priority 1 alongside the other four Continuations
    // when player level >= FatedBrand.MinLevel (96).
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_FatedBrand_PushedAsContinuation_AtLevel96Plus()
    {
        var enemy = CreateMockEnemy(44444UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableContinuation = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 96, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        // Regression: FatedBrand must be present at priority 1 (was missing pre-v4.15)
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.FatedBrand && c.Priority == 1);

        // Verify the gate is a ProcBuff (ReadyToBrand), not absent
        Assert.Equal(GNBActions.StatusIds.ReadyToBrand, GnbAbilities.FatedBrand.ProcBuff);
    }

    // -----------------------------------------------------------------------
    // Regression 5 (v4.13): Cartridge overcap: BurstStrike not queued at
    // higher priority than SolidBarrel when at 2 cartridges on combo step 2
    //
    // Bug: SolidBarrel would fire and push cartridges to 3 (or wrap), overwriting
    // the cap check that was meant to spend first.
    //
    // Fix: TryPushCartridgeSpender runs before TryPushBasicCombo and pushes
    // BurstStrike at priority 6; SolidBarrel is priority 7 (loses to BurstStrike).
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_CartridgeOvercap_BurstStrike_OutprioritizesSolidBarrel_AtTwoCartridges()
    {
        var enemy = CreateMockEnemy(55555UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 2 cartridges + comboStep 2 = aboutToOvercap (SolidBarrel would yield 3)
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 2, comboStep: 2, level: 80);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        // Regression: BurstStrike must be present to absorb the cartridge before SolidBarrel fires
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.BurstStrike && c.Priority == 6);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.SolidBarrel  && c.Priority == 7);

        var burstStrike = gcd.First(c => c.Behavior == GnbAbilities.BurstStrike);
        var solidBarrel = gcd.First(c => c.Behavior == GnbAbilities.SolidBarrel);
        Assert.True(burstStrike.Priority < solidBarrel.Priority,
            "BurstStrike (overcap prevention) must outrank SolidBarrel so cartridges are spent before the finisher generates another");
    }

    // -----------------------------------------------------------------------
    // Regression 6 (v4.13): EnableDamage=false must suppress all damage GCDs
    //
    // Bug: Some GCD paths (e.g. TryPushGnashingFangCombo) did not respect the
    // EnableDamage toggle and would push actions into the queue.
    //
    // Fix: DamageModule.CollectCandidates returns immediately when EnableDamage=false.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_EnableDamageFalse_NoDamageGcdsPushed()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, config: config,
            cartridges: 3, level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        var ogcd = scheduler.InspectOgcdQueue();

        // Regression: no damage GCDs or oGCDs when toggle is off
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.GnashingFang);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.SavageClaw);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.WickedTalon);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.BurstStrike);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.ReignOfBeasts);

        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    // -----------------------------------------------------------------------
    // Regression 7 (v4.14): EnableContinuation=false must suppress all
    // Continuation oGCDs (JugularRip, AbdomenTear, EyeGouge, Hypervelocity,
    // FatedBrand)
    //
    // Bug: The EnableContinuation guard was applied only to JugularRip; the other
    // four procs were pushed unconditionally.
    //
    // Fix: Single guard at the top of TryPushContinuations covers all five.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_EnableContinuationFalse_NoContinuationOgcdsPushed()
    {
        var enemy = CreateMockEnemy(77777UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableContinuation = false;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 100, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        // Regression: every Continuation ability must be absent
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.JugularRip);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.AbdomenTear);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.EyeGouge);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.Hypervelocity);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.FatedBrand);
    }

    // -----------------------------------------------------------------------
    // Regression 8 (v4.14): Reign chain steps 2-3 must dispatch via the base
    // ReignOfBeasts action ID (ReplacementBaseId), not the displayed ability ID
    //
    // Bug: UseAction was called with NobleBlood.ActionId / LionHeart.ActionId
    // directly. The game's ActionManager rejects replacement action IDs in UseAction
    // and silently fails (returns false), so those hits never fired.
    //
    // Fix: AbilityBehavior.ReplacementBaseId is set to GNBActions.ReignOfBeasts.ActionId
    // on both NobleBlood and LionHeart; the scheduler dispatches via that base ID.
    // -----------------------------------------------------------------------
    [Fact]
    public void Regression_ReignChainSteps_UseReplacementBaseId_EqualToReignOfBeasts()
    {
        // This is a declaration-level regression guard. If a future refactor
        // accidentally clears ReplacementBaseId, UseAction will receive the
        // wrong ID and the abilities will silently fail in-game with no compile error.
        Assert.Equal(GNBActions.ReignOfBeasts.ActionId, GnbAbilities.NobleBlood.ReplacementBaseId);
        Assert.Equal(GNBActions.ReignOfBeasts.ActionId, GnbAbilities.LionHeart.ReplacementBaseId);

        // ReignOfBeasts itself must NOT have a ReplacementBaseId (it is the base)
        Assert.Null(GnbAbilities.ReignOfBeasts.ReplacementBaseId);
    }

    // -----------------------------------------------------------------------
    // Helpers (same shape as DamageModuleCollectCandidatesTests)
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        safetyMock.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        return targeting;
    }

    private static IHephaestusContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        int cartridges = 0,
        bool hasMaxCartridges = false,
        bool hasNoMercy = false,
        bool isReadyToReign = false,
        bool isReadyToRip = false,
        bool isReadyToTear = false,
        bool isReadyToGouge = false,
        int comboStep = 0,
        uint lastComboAction = 0,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null)
    {
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.HasMaxCartridges).Returns(hasMaxCartridges || cartridges >= 3);
        mock.Setup(x => x.CanUseGnashingFang).Returns(cartridges >= 1);
        mock.Setup(x => x.CanUseDoubleDown).Returns(cartridges >= 2);
        mock.Setup(x => x.HasNoMercy).Returns(hasNoMercy);
        mock.Setup(x => x.NoMercyRemaining).Returns(0f);
        mock.Setup(x => x.HasSonicBreakDot).Returns(false);
        mock.Setup(x => x.HasBowShockDot).Returns(false);
        mock.Setup(x => x.IsInGnashingFangCombo).Returns(false);
        mock.Setup(x => x.GnashingFangStep).Returns(0);
        mock.Setup(x => x.IsReadyToRip).Returns(isReadyToRip);
        mock.Setup(x => x.IsReadyToTear).Returns(isReadyToTear);
        mock.Setup(x => x.IsReadyToGouge).Returns(isReadyToGouge);
        mock.Setup(x => x.IsReadyToBlast).Returns(false);
        mock.Setup(x => x.IsReadyToBrand).Returns(false);
        mock.Setup(x => x.IsReadyToReign).Returns(isReadyToReign);
        mock.Setup(x => x.HasAnyContinuationReady).Returns(false);
        mock.Setup(x => x.IsInReignCombo).Returns(false);
        mock.Setup(x => x.ReignComboStep).Returns(0);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);

        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());

        return mock.Object;
    }
}
