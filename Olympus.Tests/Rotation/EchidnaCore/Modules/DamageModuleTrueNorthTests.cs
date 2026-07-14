using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.EchidnaCore.Abilities;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.EchidnaCore.Modules;

/// <summary>
/// Tests for Echidna (VPR) DamageModule True North imminence gating.
/// True North should only fire when a positional dual-wield finisher is actually next
/// (signalled by a venom buff), and never during Reawaken sequences.
/// </summary>
public class DamageModuleTrueNorthTests
{
    private readonly DamageModule _module = new();

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateEnemy(ulong id = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(id);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(IBattleNpc enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy);
        return targeting;
    }

    private static Configuration BuildConfig(bool enableTrueNorth = true)
    {
        var cfg = EchidnaTestContext.CreateDefaultViperConfiguration();
        cfg.MeleeShared.EnableTrueNorth = enableTrueNorth;
        return cfg;
    }

    // ---------------------------------------------------------------------------
    // Reawaken suppression
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_NotPushed_DuringReawaken_EvenWithVenomActive()
    {
        // During a 5-GCD Reawaken sequence the 10s True North buff expires before
        // the first post-Reawaken positional finisher — the charge would be wasted.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: true,
            isAtRear: false,
            isAtFlank: false,
            hasHindstungVenom: true); // positional would be imminent if not for Reawaken

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth);
    }

    // ---------------------------------------------------------------------------
    // Positional-imminence: HindstungVenom → rear positional
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_WhenHindstungVenomActive_AndNotAtRear()
    {
        // HindstungVenom means HindstingStrike (rear positional) is the next finisher.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasHindstungVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth && c.Priority == 6);
    }

    [Fact]
    public void TrueNorth_NotPushed_WhenHindstungVenomActive_AndAlreadyAtRear()
    {
        // Already at rear — True North is unnecessary for a rear positional.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: true,
            isAtFlank: false,
            hasHindstungVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth);
    }

    // ---------------------------------------------------------------------------
    // Positional-imminence: FlankstungVenom → flank positional
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_WhenFlankstungVenomActive_AndNotAtFlank()
    {
        // FlankstungVenom means FlankstingStrike (flank positional) is the next finisher.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasFlankstungVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth && c.Priority == 6);
    }

    // ---------------------------------------------------------------------------
    // No venom — no positional imminent (includes twinblade coil GCDs)
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_NotPushed_WhenNoVenomActive_NotInReawaken()
    {
        // No venom buffs active: True North fires only when a positional is imminent.
        // This covers twinblade coil states (HuntersCoil, SwiftskinsCoil have no positionals)
        // and combo starters where the finisher is still 2 GCDs away.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false
            // all venom flags default to false
        );

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth);
    }

    // ---------------------------------------------------------------------------
    // Hindsbane / Flanksbane variants (Swiftskin's Sting path)
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_WhenHindsbaneVenomActive_AndNotAtRear()
    {
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasHindsbaneVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth && c.Priority == 6);
    }

    [Fact]
    public void TrueNorth_Pushed_WhenFlanksbaneVenomActive_AndNotAtFlank()
    {
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasFlanksbaneVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth && c.Priority == 6);
    }

    // ---------------------------------------------------------------------------
    // Config/readiness gates still respected
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_NotPushed_WhenDisabledInConfig()
    {
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(enableTrueNorth: false),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasHindstungVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth);
    }

    [Fact]
    public void TrueNorth_NotPushed_WhenIsActionReadyReturnsFalse()
    {
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            isReawakened: false,
            isAtRear: false,
            isAtFlank: false,
            hasHindstungVenom: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.TrueNorth);
    }
}
