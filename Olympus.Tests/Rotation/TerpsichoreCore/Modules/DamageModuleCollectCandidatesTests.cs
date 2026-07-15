using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
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
/// CollectCandidates queue-content tests for Terpsichore (DNC) DamageModule.
/// Covers Saber Dance Esprit threshold (non-default config value locks in the
/// field is actually read), Fan Dance feather threshold, proc GCDs, AoE toggle,
/// and filler combo. Uses InspectGcdQueue / InspectOgcdQueue; never calls TryExecute.
/// </summary>
public class DamageModuleCollectCandidatesTests
{
    private readonly DamageModule _module = new();

    // -------------------------------------------------------------------------
    // 1. Saber Dance: Esprit threshold uses SaberDanceMinGauge during burst,
    //    EspritOvercapThreshold outside burst. Test both with non-default values.
    // -------------------------------------------------------------------------

    [Fact]
    public void SaberDance_Pushed_AtPriority5_WhenEspritMeetsOvercapThreshold_OutOfBurst()
    {
        // Non-default threshold: EspritOvercapThreshold = 80, esprit = 80.
        // inBurst = false so EspritOvercapThreshold is the gate.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.EspritOvercapThreshold = 80;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            esprit: 80,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.SaberDance && c.Priority == 5);
    }

    [Fact]
    public void SaberDance_NotPushed_WhenEspritBelowOvercapThreshold_OutOfBurst()
    {
        // Non-default threshold: EspritOvercapThreshold = 80, esprit = 75.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.EspritOvercapThreshold = 80;
        config.Dancer.SaveEspritForBurst = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            esprit: 75,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.SaberDance);
    }

    [Fact]
    public void SaberDance_Pushed_WhenEspritMeetsSaberDanceMinGauge_DuringBurst()
    {
        // Non-default burst threshold: SaberDanceMinGauge = 70, esprit = 70, inBurst via hasTechnicalFinish.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.SaberDanceMinGauge = 70;
        config.Dancer.SaveEspritForBurst = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            esprit: 70,
            hasDevilment: false,
            hasTechnicalFinish: true, // inBurst = true
            hasDanceOfTheDawnReady: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.SaberDance && c.Priority == 5);
    }

    [Fact]
    public void SaberDance_NotPushed_WhenEspritBelowSaberDanceMinGauge_DuringBurst()
    {
        // SaberDanceMinGauge = 70, esprit = 65, inBurst = true.
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.SaberDanceMinGauge = 70;
        config.Dancer.SaveEspritForBurst = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            esprit: 65,
            hasDevilment: false,
            hasTechnicalFinish: true,
            hasDanceOfTheDawnReady: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.SaberDance);
    }

    [Fact]
    public void SaberDance_NotPushed_WhenDisabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = false;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            esprit: 90,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.SaberDance);
    }

    // -------------------------------------------------------------------------
    // 2. Proc GCDs: Fountainfall (Silken Flow) and Reverse Cascade (Silken Symmetry)
    // -------------------------------------------------------------------------

    [Fact]
    public void Fountainfall_Pushed_AtPriority7_WhenSilkenFlowActive()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableProcs = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            hasSilkenFlow: true,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.Fountainfall && c.Priority == 7);
    }

    [Fact]
    public void Fountainfall_NotPushed_WhenNoSilkenFlow()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableProcs = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            hasSilkenFlow: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.Fountainfall);
    }

    [Fact]
    public void ReverseCascade_Pushed_AtPriority7_WhenSilkenSymmetryActive()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableProcs = true;

        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            hasSilkenSymmetry: true,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.ReverseCascade && c.Priority == 7);
    }

    // -------------------------------------------------------------------------
    // 3. AoE branch: EnableAoERotation = false forces enemyCount = 0
    // -------------------------------------------------------------------------

    [Fact]
    public void AoE_Windmill_NotPushed_WhenToggleOff_EvenAtHighEnemyCount()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableAoERotation = false;

        var target = CreateTarget();
        // 5 enemies but AoE is disabled — module will use enemyCount = 0
        var targeting = BuildTargeting(target, enemyCount: 5);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == TerpsichoreAbilities.Windmill);
        Assert.DoesNotContain(gcd, c => c.Behavior == TerpsichoreAbilities.Bladeshower);
    }

    [Fact]
    public void AoE_Windmill_Pushed_WhenToggleOn_AndEnemiesAtThreshold()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableAoERotation = true;
        config.Dancer.AoEMinTargets = 3;

        var target = CreateTarget();
        var targeting = BuildTargeting(target, enemyCount: 3);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            inCombat: true,
            // No procs or special GCDs so module falls through to combo filler
            hasSilkenFlow: false,
            hasSilkenSymmetry: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.Windmill && c.Priority == 9);
    }

    // -------------------------------------------------------------------------
    // 4. Filler combo: Cascade at lowest priority
    // -------------------------------------------------------------------------

    [Fact]
    public void Cascade_Pushed_AtPriority9_AsFillerWhenNoProcs()
    {
        var target = CreateTarget();
        var targeting = BuildTargeting(target);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            hasSilkenFlow: false,
            hasSilkenSymmetry: false,
            hasFlourishingStarfall: false,
            hasLastDanceReady: false,
            hasFinishingMoveReady: false,
            hasDanceOfTheDawnReady: false,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == TerpsichoreAbilities.Cascade && c.Priority == 9);
    }

    // -------------------------------------------------------------------------
    // 5. Not-in-combat: no candidates pushed
    // -------------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_NoCandidates_WhenNotInCombat()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = TerpsichoreTestContext.Create(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    // -------------------------------------------------------------------------
    // 6. Dancing: no damage candidates pushed while executing dance steps
    // -------------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_NoDamageCandidates_WhenDancing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            isDancing: true);

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
