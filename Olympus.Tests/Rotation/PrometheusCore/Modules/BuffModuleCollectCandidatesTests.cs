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
/// CollectCandidates queue-content tests for Prometheus (MCH) BuffModule.
/// Covers Wildfire toggle, Hypercharge heat gate (including non-default config
/// threshold), Gauss Round/Ricochet charge gate, Automaton Queen battery gate,
/// and Peloton pre-combat. Uses InspectOgcdQueue snapshots; never calls TryExecute.
/// </summary>
public class BuffModuleCollectCandidatesTests
{
    private readonly BuffModule _module = new();

    // -------------------------------------------------------------------------
    // 1. Wildfire: config toggle
    // -------------------------------------------------------------------------

    [Fact]
    public void Wildfire_Pushed_AtPriority1_WhenEnabled_AndOverheated()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableWildfire = true;

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
            hasWildfire: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Wildfire && c.Priority == 1);
    }

    [Fact]
    public void Wildfire_NotPushed_WhenDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableWildfire = false;

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
            hasWildfire: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Wildfire);
    }

    [Fact]
    public void Wildfire_NotPushed_WhenAlreadyActive()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableWildfire = true;

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
            hasWildfire: true,
            wildfireRemaining: 5f,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Wildfire);
    }

    // -------------------------------------------------------------------------
    // 2. Hypercharge: heat gauge gate including non-default threshold
    // -------------------------------------------------------------------------

    [Fact]
    public void Hypercharge_Pushed_AtPriority4_WhenHeatAtDefaultMinGauge()
    {
        // Default HeatMinGauge = 50; heat = 50 meets the threshold exactly.
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = true;
        config.Machinist.EnableBurstPooling = false; // disable burst hold so no pooling interference

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // Return 0 for all cooldowns so no tool-coming-during-window hold fires
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 50,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge && c.Priority == 4);
    }

    [Fact]
    public void Hypercharge_NotPushed_WhenHeatBelowDefaultMinGauge()
    {
        // Default HeatMinGauge = 50; heat = 49 is below threshold.
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = true;
        config.Machinist.EnableBurstPooling = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 49,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge);
    }

    [Fact]
    public void Hypercharge_NotPushed_WhenHeatBelowCustomThreshold()
    {
        // Non-default threshold: set HeatMinGauge = 60, heat = 55.
        // Verifies the config field is actually read, not a hardcoded literal.
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = true;
        config.Machinist.EnableBurstPooling = false;
        config.Machinist.HeatMinGauge = 60;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 55,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge);
    }

    [Fact]
    public void Hypercharge_Pushed_WhenHeatMeetsCustomThreshold()
    {
        // Non-default threshold: set HeatMinGauge = 60, heat = 60.
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = true;
        config.Machinist.EnableBurstPooling = false;
        config.Machinist.HeatMinGauge = 60;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 60,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge && c.Priority == 4);
    }

    [Fact]
    public void Hypercharge_NotPushed_WhenDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 50,
            isOverheated: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge);
    }

    [Fact]
    public void Hypercharge_NotPushed_WhenAlreadyOverheated()
    {
        // Cannot enter Hypercharge while already Overheated.
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableHypercharge = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            heat: 80,
            isOverheated: true,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Hypercharge);
    }

    // -------------------------------------------------------------------------
    // 3. Gauss Round: charge gate (overheated or 2+ charges)
    // -------------------------------------------------------------------------

    [Fact]
    public void GaussRound_Pushed_AtPriority6_WhenOverheated_AndOneCharge()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableGaussRicochet = true;

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
            gaussRoundCharges: 1,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.GaussRound && c.Priority == 6);
    }

    [Fact]
    public void GaussRound_Pushed_AtPriority6_WhenAtTwoCharges_NotOverheated()
    {
        // Not overheated but 2 charges: shouldUse = (IsOverheated || charges >= 2) = true
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableGaussRicochet = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            isOverheated: false,
            gaussRoundCharges: 2,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.GaussRound && c.Priority == 6);
    }

    [Fact]
    public void GaussRound_NotPushed_WhenNotOverheated_AndOnlyOneCharge()
    {
        // shouldUse = (IsOverheated || charges >= 2) = (false || false) = false
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableGaussRicochet = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            isOverheated: false,
            gaussRoundCharges: 1,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.GaussRound);
    }

    [Fact]
    public void GaussRound_NotPushed_WhenGaussRicochetDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableGaussRicochet = false;

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
            gaussRoundCharges: 2,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.GaussRound);
    }

    // -------------------------------------------------------------------------
    // 4. Automaton Queen: battery gauge gate
    // -------------------------------------------------------------------------

    [Fact]
    public void AutomatonQueen_Pushed_AtPriority5_WhenBatteryAtOvercapThreshold()
    {
        // battery = 90 >= BatteryOvercapThreshold (90 default) → shouldSummon = true
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAutomatonQueen = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            battery: 90,
            isQueenActive: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.AutomatonQueen && c.Priority == 5);
    }

    [Fact]
    public void AutomatonQueen_NotPushed_WhenQueenAlreadyActive()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAutomatonQueen = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            battery: 90,
            isQueenActive: true,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.AutomatonQueen);
    }

    [Fact]
    public void AutomatonQueen_NotPushed_WhenBatteryBelowMinGauge()
    {
        // battery = 49 < BatteryMinGauge (default 50) → returns early before shouldSummon check
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAutomatonQueen = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            battery: 49,
            isQueenActive: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.AutomatonQueen);
    }

    [Fact]
    public void AutomatonQueen_NotPushed_WhenDisabled()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableAutomatonQueen = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            battery: 90,
            isQueenActive: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.AutomatonQueen);
    }

    // -------------------------------------------------------------------------
    // 5. Peloton: pre-combat moving gate
    // -------------------------------------------------------------------------

    [Fact]
    public void Peloton_Pushed_AtPriority10_WhenPreCombat_AndMoving()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.RangedShared.EnablePeloton = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Peloton && c.Priority == 10);
    }

    [Fact]
    public void Peloton_NotPushed_WhenInCombat()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.RangedShared.EnablePeloton = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            isMoving: true);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Peloton);
    }

    [Fact]
    public void Peloton_NotPushed_WhenPreCombat_AndNotMoving()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.RangedShared.EnablePeloton = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == PrometheusAbilities.Peloton);
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
