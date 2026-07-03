using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for BenedictionHandler.CollectCandidates.
///
/// Critical path under test: the damage-rate-aware threshold escalation.
///   base threshold (config.Healing.BenedictionEmergencyThreshold, default 0.30)
///   damage rate > 500 DPS  → threshold + 0.10, capped at 0.50
///   damage rate > 800 DPS  → threshold + 0.20, capped at 0.50
///
/// Each test mocks IApolloContext directly because DamageIntakeService is not
/// exposed by ApolloTestContext.Create() and controls the threshold branch.
/// </summary>
public class BenedictionHandlerTests
{
    private readonly BenedictionHandler _handler = new();

    // -----------------------------------------------------------------------
    // 1. HP below base emergency threshold with no damage escalation → push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_HpBelowBaseThreshold_PushesOgcdAtPriority10()
    {
        var target = CreateTarget(currentHp: 14000, maxHp: 50000); // 28% HP
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;

        var context = BuildContext(target: target, hpPercent: 0.28f, damageRate: 0f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.Benediction &&
            c.Priority == (int)HealingPriority.Benediction &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 2. HP above base threshold, no damage rate: no escalation, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_HpAboveBaseThreshold_ZeroDamageRate_NoPush()
    {
        var target = CreateTarget(currentHp: 18000, maxHp: 50000); // 36%
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.EnableProactiveBenediction = false; // isolate emergency path

        var context = BuildContext(target: target, hpPercent: 0.36f, damageRate: 0f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 3. Damage rate > 500 escalates threshold +0.10; HP in (base, base+0.10) → push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_DamageRateAbove500_HpInEscalatedZone_Pushes()
    {
        // base 0.30 → escalated 0.40; hpPercent 0.38 is in (0.30, 0.40)
        var target = CreateTarget(currentHp: 19000, maxHp: 50000);
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.EnableProactiveBenediction = false;

        var context = BuildContext(target: target, hpPercent: 0.38f, damageRate: 600f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 4. Damage rate > 500 escalation: HP above escalated threshold → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_DamageRateAbove500_HpAboveEscalatedThreshold_NoPush()
    {
        // base 0.30 → escalated 0.40; hpPercent 0.42 > 0.40
        var target = CreateTarget(currentHp: 21000, maxHp: 50000);
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.EnableProactiveBenediction = false;

        var context = BuildContext(target: target, hpPercent: 0.42f, damageRate: 600f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 5. Damage rate > 800 escalates threshold +0.20; HP below 0.50 → push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_DamageRateAbove800_ThresholdEscalatedBy20Pct_Pushes()
    {
        // base 0.30 → escalated min(0.50, 0.50) = 0.50; hpPercent 0.48 < 0.50
        var target = CreateTarget(currentHp: 24000, maxHp: 50000);
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.EnableProactiveBenediction = false;

        var context = BuildContext(target: target, hpPercent: 0.48f, damageRate: 900f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 6. Cap at 0.50: base 0.40 + 0.20 = 0.60, but capped → 0.50. HP at 0.52 → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_DamageRateAbove800_ThresholdCappedAt50Pct_HpAboveCap_NoPush()
    {
        // base 0.40, damage rate 900 → base + 0.20 = 0.60, capped to 0.50
        // hpPercent 0.52 > 0.50 → no emergency push
        var target = CreateTarget(currentHp: 26000, maxHp: 50000);
        var config = CreateConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.40f;
        config.Healing.EnableProactiveBenediction = false;

        var context = BuildContext(target: target, hpPercent: 0.52f, damageRate: 900f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 7. Global healing master switch disabled → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_HealingMasterDisabled_NoPush()
    {
        var target = CreateTarget(currentHp: 5000, maxHp: 50000);
        var config = CreateConfig();
        config.EnableHealing = false;

        var context = BuildContext(target: target, hpPercent: 0.10f, damageRate: 0f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 8. Benediction per-ability toggle disabled → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BenedictionToggleDisabled_NoPush()
    {
        var target = CreateTarget(currentHp: 5000, maxHp: 50000);
        var config = CreateConfig();
        config.Healing.EnableBenediction = false;

        var context = BuildContext(target: target, hpPercent: 0.10f, damageRate: 0f, config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 9. Player level below Benediction minimum (50) → ActionValidator rejects, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LevelTooLow_NoPush()
    {
        var target = CreateTarget(currentHp: 5000, maxHp: 50000);
        var context = BuildContext(target: target, hpPercent: 0.10f, damageRate: 0f, playerLevel: 49);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 10. Benediction on cooldown → ActionValidator.IsReady false, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BenedictionOnCooldown_NoPush()
    {
        var target = CreateTarget(currentHp: 5000, maxHp: 50000);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(WHMActions.Benediction.ActionId)).Returns(false);

        var context = BuildContext(target: target, hpPercent: 0.10f, damageRate: 0f,
            actionService: actionService);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // 11. No injured party member (FindLowestHpPartyMember returns null) → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_NoTarget_NoPush()
    {
        var context = BuildContext(target: null, hpPercent: 0f, damageRate: 0f);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Benediction);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Configuration CreateConfig()
    {
        var cfg = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        cfg.EnableHealing = true;
        cfg.Healing.EnableBenediction = true;
        cfg.Healing.BenedictionEmergencyThreshold = 0.30f;
        return cfg;
    }

    /// <summary>
    /// Creates a mock IBattleChara target at origin (always in-range of Benediction's 30y).
    /// StatusList = null ensures HasNoHealStatus returns false (safe to heal).
    /// </summary>
    private static Mock<IBattleChara> CreateTarget(
        uint currentHp = 10000, uint maxHp = 50000, uint entityId = 99u)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.CurrentHp).Returns(currentHp);
        mock.Setup(x => x.MaxHp).Returns(maxHp);
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        mock.Setup(x => x.StatusList).Returns(
            (Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    /// <summary>
    /// Builds a mock IApolloContext wired for BenedictionHandler.
    /// Only the properties accessed by the handler before the scheduler push are set up.
    /// Properties only accessed inside onDispatched (HpPredictionService, Debug, Log, etc.)
    /// are left at Moq defaults — onDispatched is never called during queue-inspect tests.
    /// </summary>
    private static IApolloContext BuildContext(
        Mock<IBattleChara>? target,
        float hpPercent,
        float damageRate,
        Configuration? config = null,
        byte playerLevel = 90,
        Mock<IActionService>? actionService = null)
    {
        config ??= CreateConfig();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper
            .Setup(p => p.FindLowestHpPartyMember(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns(target?.Object);
        if (target != null)
        {
            partyHelper
                .Setup(p => p.GetHpPercent(It.IsAny<IBattleChara>()))
                .Returns(hpPercent);
        }

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService(
            getDamageRate: (_, _) => damageRate);

        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.ActionService).Returns(actionService.Object);
        ctx.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        ctx.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);
        ctx.Setup(x => x.HpPredictionService).Returns(hpPredictionService.Object);
        ctx.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        ctx.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.Debug).Returns(new DebugState());

        return ctx.Object;
    }
}
