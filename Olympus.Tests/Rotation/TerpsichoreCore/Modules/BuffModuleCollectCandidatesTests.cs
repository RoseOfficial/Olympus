using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// CollectCandidates queue-content tests for Terpsichore (DNC) BuffModule.
/// Covers Standard Step pre-combat gate, Technical Step in-combat only,
/// Devilment Technical Finish gate, Fan Dance feather threshold (non-default
/// config value verifies field is read), and Peloton pre-combat moving gate.
/// Uses InspectOgcdQueue / InspectGcdQueue; never calls TryExecute.
/// </summary>
public class BuffModuleCollectCandidatesTests
{
    private readonly BuffModule _module = new();

    // -------------------------------------------------------------------------
    // 1. Standard Step: pre-combat gate
    // -------------------------------------------------------------------------

    [Fact]
    public void StandardStep_Pushed_AtPriority4_WhenPreCombat_AndNoStandardFinish()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableStandardStep = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // Manual partner mode so ClosedPosition is not attempted (would need party members)
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isDancing: false,
            hasStandardFinish: false,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.StandardStep && c.Priority == 4);
    }

    [Fact]
    public void StandardStep_NotPushed_WhenPreCombat_AndHasStandardFinish()
    {
        // When Standard Finish buff is already active pre-combat, don't push.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableStandardStep = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isDancing: false,
            hasStandardFinish: true, // buff already active
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.StandardStep);
    }

    [Fact]
    public void TechnicalStep_NotPushed_WhenPreCombat()
    {
        // TechnicalStep is not called in the pre-combat block — only StandardStep is.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableTechnicalStep = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isDancing: false,
            hasStandardFinish: false,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.TechnicalStep);
    }

    // -------------------------------------------------------------------------
    // 2. Technical Step: in-combat push
    // -------------------------------------------------------------------------

    [Fact]
    public void TechnicalStep_Pushed_AtPriority2_WhenInCombat_AndReady()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableTechnicalStep = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            isDancing: false,
            hasStandardFinish: true, // skip first StandardStep push (HasStandardFinish = true)
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.TechnicalStep && c.Priority == 2);
    }

    [Fact]
    public void TechnicalStep_NotPushed_WhenDisabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableTechnicalStep = false;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.TechnicalStep);
    }

    // -------------------------------------------------------------------------
    // 3. Devilment: gated on HasTechnicalFinish (or TechnicalStep unavailable)
    // -------------------------------------------------------------------------

    [Fact]
    public void Devilment_Pushed_AtPriority3_WhenHasTechnicalFinish()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = true;
        config.Dancer.UseDevilmentAfterTechnical = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            hasTechnicalFinish: true,
            hasDevilment: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Devilment && c.Priority == 3);
    }

    [Fact]
    public void Devilment_NotPushed_WhenNoTechnicalFinish_AndTechnicalStepReady()
    {
        // shouldUse = HasTechnicalFinish || level < TS.MinLevel || !IsActionReady(TS)
        // = false || false || !true = false → does not push
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = true;
        config.Dancer.UseDevilmentAfterTechnical = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true); // TechStep is ready
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            hasTechnicalFinish: false,
            hasDevilment: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Devilment);
    }

    [Fact]
    public void Devilment_NotPushed_WhenDisabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = false;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            hasTechnicalFinish: true,
            hasDevilment: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Devilment);
    }

    [Fact]
    public void Devilment_NotPushed_WhenUseDevilmentAfterTechnical_IsFalse()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = true;
        config.Dancer.UseDevilmentAfterTechnical = false; // gate disabled
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            hasTechnicalFinish: true,
            hasDevilment: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Devilment);
    }

    // -------------------------------------------------------------------------
    // 4. Fan Dance: feather threshold (non-default FeatherOvercapThreshold)
    // -------------------------------------------------------------------------

    [Fact]
    public void FanDance_Pushed_AtPriority7_WhenFeathersAtCustomOvercapThreshold()
    {
        // Non-default threshold: FeatherOvercapThreshold = 4, feathers = 4.
        // Verifies FeatherOvercapThreshold is actually read (was historically defined-but-unused).
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.FeatherOvercapThreshold = 4;
        config.Dancer.FanDanceMinFeathers = 1;
        config.Dancer.SaveFeathersForBurst = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            feathers: 4, // meets the overcap threshold exactly
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasStandardFinish: true,
            hasFourfoldFanDance: false,
            hasThreefoldFanDance: false,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDance && c.Priority == 7);
    }

    [Fact]
    public void FanDance_NotPushed_WhenFeathersBelowCustomOvercapThreshold_AndNotInBurst()
    {
        // FeatherOvercapThreshold = 4, feathers = 3, not in burst → shouldUse = false → SaveFeathersForBurst returns early
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.FeatherOvercapThreshold = 4;
        config.Dancer.FanDanceMinFeathers = 1;
        config.Dancer.SaveFeathersForBurst = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            feathers: 3, // below overcap threshold of 4
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDance);
    }

    [Fact]
    public void FanDance_NotPushed_WhenNoFeathers()
    {
        // feathers = 0 < FanDanceMinFeathers (1) → returns early
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            feathers: 0,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDance);
    }

    [Fact]
    public void FanDance_NotPushed_WhenDisabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = false;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            feathers: 4,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDance);
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDanceII);
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.FanDanceIII);
    }

    // -------------------------------------------------------------------------
    // 5. Peloton: pre-combat moving gate
    // -------------------------------------------------------------------------

    [Fact]
    public void Peloton_Pushed_AtPriority10_WhenPreCombat_AndMoving()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.RangedShared.EnablePeloton = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: true,
            isDancing: false,
            hasStandardFinish: true, // skip StandardStep push to isolate Peloton
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Peloton && c.Priority == 10);
    }

    [Fact]
    public void Peloton_NotPushed_WhenInCombat()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.RangedShared.EnablePeloton = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            isMoving: true,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Peloton);
    }

    [Fact]
    public void Peloton_NotPushed_WhenPreCombat_AndNotMoving()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.RangedShared.EnablePeloton = true;
        config.Dancer.PartnerSelectionMode = PartnerSelection.Manual;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: false,
            isDancing: false,
            hasStandardFinish: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == TerpsichoreAbilities.Peloton);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateTarget()
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(88888UL);
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
