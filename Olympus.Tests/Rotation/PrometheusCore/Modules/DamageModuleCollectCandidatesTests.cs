using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PrometheusCore.Abilities;
using Olympus.Rotation.PrometheusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.PrometheusCore.Modules;

/// <summary>
/// CollectCandidates queue-content tests for Prometheus (MCH) DamageModule.
/// Verifies that each branch's ability lands in the GCD or oGCD queue at the
/// expected priority. Uses Assert.Contains / DoesNotContain on queue snapshots
/// from InspectGcdQueue / InspectOgcdQueue; never calls TryExecute.
/// </summary>
public class DamageModuleCollectCandidatesTests
{
    private readonly DamageModule _module = new();

    // -------------------------------------------------------------------------
    // 1. Drill: charge gate
    // -------------------------------------------------------------------------

    [Fact]
    public void Drill_Pushed_AtPriority4_WhenChargesAvailable()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            drillCharges: 1,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.Drill && c.Priority == 4);
    }

    [Fact]
    public void Drill_NotPushed_WhenChargesEmpty()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            drillCharges: 0,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.Drill);
    }

    [Fact]
    public void Drill_NotPushed_WhenDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableDrill = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            drillCharges: 2,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.Drill);
    }

    // -------------------------------------------------------------------------
    // 2. HeatBlast (Overheated GCD): overheated gate
    // -------------------------------------------------------------------------

    [Fact]
    public void HeatBlast_Pushed_AtPriority1_WhenOverheated()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHeatBlast = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            isOverheated: true,
            overheatRemaining: 5f,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.HeatBlast && c.Priority == 1);
    }

    [Fact]
    public void HeatBlast_NotPushed_WhenNotOverheated()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.HeatBlast);
    }

    [Fact]
    public void HeatBlast_NotPushed_WhenDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHeatBlast = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            isOverheated: true,
            overheatRemaining: 5f,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.HeatBlast);
    }

    // -------------------------------------------------------------------------
    // 3. AoE branch: EnableAoERotation = false forces enemyCount = 0,
    //    so SpreadShot/Bioblaster never appear even with many enemies.
    // -------------------------------------------------------------------------

    [Fact]
    public void AoE_SpreadShot_NotPushed_WhenToggleOff_EvenAtHighEnemyCount()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAoERotation = false;

        var target = CreateTarget();
        // CountEnemiesInRange returns 5 — above the default 3 threshold
        var targeting = BuildTargeting(target, enemyCount: 5);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PrometheusAbilities.SpreadShot);
        Assert.DoesNotContain(gcd, c => c.Behavior == PrometheusAbilities.Bioblaster);
    }

    [Fact]
    public void AoE_SpreadShot_Pushed_WhenToggleOn_AndEnemiesAtThreshold()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAoERotation = true;
        config.Machinist.AoEMinTargets = 3;

        var target = CreateTarget();
        var targeting = BuildTargeting(target, enemyCount: 3);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        // SpreadShot is the AoE combo GCD (level-replaced by Scattergun at Lv.82+)
        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.SpreadShot && c.Priority == 8);
    }

    // -------------------------------------------------------------------------
    // 4. Single-target combo: correct ability pushed at each combo step
    // -------------------------------------------------------------------------

    [Fact]
    public void Combo_SplitShot_Pushed_AtPriority9_AsFiller()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // comboStep = 0 falls through to the else branch (SplitShot starter)
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            comboStep: 0,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.SplitShot && c.Priority == 9);
    }

    [Fact]
    public void Combo_SlugShot_Pushed_AtPriority9_WhenComboStep1()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            comboStep: 1,
            lastComboAction: MCHActions.HeatedSplitShot.ActionId,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.SlugShot && c.Priority == 9);
    }

    [Fact]
    public void Combo_CleanShot_Pushed_AtPriority9_WhenComboStep2()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            comboStep: 2,
            lastComboAction: MCHActions.HeatedSlugShot.ActionId,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == PrometheusAbilities.CleanShot && c.Priority == 9);
    }

    // -------------------------------------------------------------------------
    // 5. Not-in-combat: no candidates pushed
    // -------------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_NoCandidates_WhenNotInCombat()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PrometheusTestContext.Create(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateTarget()
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(99999UL);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.IsCastInterruptible).Returns(false);
        return mock;
    }

    private static Mock<ITargetingService> BuildTargeting(Mock<IBattleNpc> enemy, int enemyCount = 0)
    {
        var targeting = MockBuilders.CreateMockTargetingService(countEnemiesInRange: enemyCount);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        return targeting;
    }
}
