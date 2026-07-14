using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Tests for Nike (SAM) BuffModule True North gating during Meikyo Shisui.
/// True North should only fire when Gekko (rear) or Kasha (flank) is the
/// next GCD — not when Yukikaze (no positional) is selected.
/// </summary>
public class BuffModuleTrueNorthTests
{
    private readonly BuffModule _module = new();

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
        var cfg = NikeTestContext.CreateDefaultSamuraiConfiguration();
        cfg.MeleeShared.EnableTrueNorth = enableTrueNorth;
        return cfg;
    }

    // ---------------------------------------------------------------------------
    // Yukikaze case — should NOT fire True North
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_NotPushed_DuringMeikyo_WhenYukikazeNext()
    {
        // HasGetsu=true, HasKa=true, HasSetsu=false → DamageModule picks Yukikaze (no positional).
        // True North would be wasted on a non-positional GCD.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            // Getsu and Ka held, only Setsu missing → Yukikaze next
            sen: SAMActions.SenType.Getsu | SAMActions.SenType.Ka,
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.TrueNorth);
    }

    // ---------------------------------------------------------------------------
    // Gekko case — should fire True North when not at rear
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_DuringMeikyo_WhenGekkoNext_AndNotAtRear()
    {
        // !HasGetsu → DamageModule picks Gekko (rear positional). Player out of position.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            // Getsu missing → Gekko next (rear positional)
            sen: SAMActions.SenType.Ka | SAMActions.SenType.Setsu,
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.TrueNorth && c.Priority == 5);
    }

    [Fact]
    public void TrueNorth_NotPushed_DuringMeikyo_WhenGekkoNext_AndAlreadyAtRear()
    {
        // !HasGetsu → Gekko next (rear), but player is already at rear. True North not needed.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            sen: SAMActions.SenType.Ka | SAMActions.SenType.Setsu,
            isAtRear: true,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.TrueNorth);
    }

    // ---------------------------------------------------------------------------
    // Kasha case — should fire True North when not at flank
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_DuringMeikyo_WhenKashaNext_AndNotAtFlank()
    {
        // HasGetsu=true, !HasKa → DamageModule picks Kasha (flank positional). Player out of position.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            // Getsu held, Ka missing → Kasha next (flank positional)
            sen: SAMActions.SenType.Getsu | SAMActions.SenType.Setsu,
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.TrueNorth && c.Priority == 5);
    }

    // ---------------------------------------------------------------------------
    // Overflow case — all Sen held, DamageModule falls through to overflow Gekko (rear)
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_DuringMeikyo_WhenAllSenHeld_AndNotAtRear()
    {
        // All three Sen held (overflow) → DamageModule picks overflow Gekko (rear positional).
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            sen: SAMActions.SenType.Getsu | SAMActions.SenType.Ka | SAMActions.SenType.Setsu,
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.TrueNorth && c.Priority == 5);
    }

    // ---------------------------------------------------------------------------
    // Non-Meikyo combo path: existing behavior preserved
    // ---------------------------------------------------------------------------

    [Fact]
    public void TrueNorth_Pushed_WhenComboStep2AfterJinpu_AndNotAtRear()
    {
        // Normal combo path: Jinpu → Gekko (rear). Player at step 2, not at rear.
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: false,
            sen: SAMActions.SenType.None, // no Getsu → !HasGetsu
            comboStep: 2,
            lastComboAction: SAMActions.Jinpu.ActionId,
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.TrueNorth && c.Priority == 5);
    }

    [Fact]
    public void TrueNorth_NotPushed_WhenDisabledInConfig()
    {
        var enemy = CreateEnemy();
        var targeting = CreateTargetingWithEnemy(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: BuildConfig(enableTrueNorth: false),
            actionService: actionService,
            targetingService: targeting,
            hasMeikyoShisui: true,
            meikyoStacks: 1,
            sen: SAMActions.SenType.Ka | SAMActions.SenType.Setsu, // Gekko next
            isAtRear: false,
            isAtFlank: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.TrueNorth);
    }
}
