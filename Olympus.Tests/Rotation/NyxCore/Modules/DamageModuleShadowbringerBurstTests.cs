using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Verifies Shadowbringer burst-window charge pooling:
///   - Hold when burst is imminent and only one charge is available.
///   - Fire when the burst window is active (no hold needed).
///   - Charge-cap escape: fire when both charges are capped, even if burst is imminent.
/// </summary>
public class DamageModuleShadowbringerBurstTests
{
    // -----------------------------------------------------------------------
    // 1. Burst imminent, one charge -- hold (do not push)
    // -----------------------------------------------------------------------

    [Fact]
    public void Shadowbringer_Hold_WhenBurstImminentAndOneCharge()
    {
        var burstService = CreateHoldingBurstService();  // imminent, not yet active

        var module = new DamageModule(burstService.Object);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowbringer.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(DRKActions.Shadowbringer.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Shadowbringer);
    }

    // -----------------------------------------------------------------------
    // 2. Burst window active, one charge -- push
    // -----------------------------------------------------------------------

    [Fact]
    public void Shadowbringer_Pushes_WhenInActiveBurstWindow()
    {
        var burstService = CreateActiveBurstService();  // burst is live now

        var module = new DamageModule(burstService.Object);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowbringer.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(DRKActions.Shadowbringer.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Shadowbringer && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // 3. Burst imminent but both charges capped -- charge-cap escape fires
    // -----------------------------------------------------------------------

    [Fact]
    public void Shadowbringer_ChargeCapEscape_FiresWhenBothChargesCapped()
    {
        var burstService = CreateHoldingBurstService();  // imminent, not yet active

        var module = new DamageModule(burstService.Object);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowbringer.ActionId)).Returns(true);
        // Both charges at max -- must fire to avoid overcap
        actionService.Setup(x => x.GetCurrentCharges(DRKActions.Shadowbringer.ActionId)).Returns(2u);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Shadowbringer && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Burst is imminent but not yet active -- ShouldHoldForBurst returns true.</summary>
    private static Mock<IBurstWindowService> CreateHoldingBurstService()
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(x => x.IsInBurstWindow).Returns(false);
        svc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        return svc;
    }

    /// <summary>Burst window is currently active -- ShouldHoldForBurst returns false.</summary>
    private static Mock<IBurstWindowService> CreateActiveBurstService()
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(x => x.IsInBurstWindow).Returns(true);
        svc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        return svc;
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 100ul)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.GameObjectId).Returns(objectId);
        m.Setup(x => x.CurrentHp).Returns(10000u);
        m.Setup(x => x.MaxHp).Returns(10000u);
        return m;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safety = new Mock<IGapCloserSafetyService>();
        safety.Setup(x => x.ShouldBlockGapCloser(
            It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>())).Returns(false);
        safety.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safety.Object);

        return targeting;
    }

    private static INyxContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.Debug).Returns(new NyxDebugState());

        // DRK-specific state: defaults that short-circuit other TryPush* paths
        // so only Shadowbringer reaches the queue in these focused tests.
        mock.Setup(x => x.HasDarkArts).Returns(false);        // skip TryPushDarkArtsProc
        mock.Setup(x => x.HasSaltedEarth).Returns(true);      // skip TryPushSaltedEarth (already active)
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(false);  // skip TryPushDarksideMaintenance
        mock.Setup(x => x.HasDelirium).Returns(false);         // skip TryPushDeliriumCombo + free Bloodspiller
        mock.Setup(x => x.DeliriumStacks).Returns(0);
        mock.Setup(x => x.HasScornfulEdge).Returns(false);     // skip TryPushDisesteem
        mock.Setup(x => x.BloodGauge).Returns(0);              // skip TryPushBloodSpender (< 50)
        mock.Setup(x => x.HasDarkside).Returns(false);
        mock.Setup(x => x.DarksideRemaining).Returns(0f);
        mock.Setup(x => x.CurrentMp).Returns(0);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);

        return mock.Object;
    }
}
