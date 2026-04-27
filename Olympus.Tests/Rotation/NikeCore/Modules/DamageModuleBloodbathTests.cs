using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Helpers;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Smoke test: Nike (SAM) DamageModule pushes Bloodbath via RoleActionPushers
/// at oGCD priority 6 when HP is below the configured threshold and the action is ready.
/// </summary>
public class DamageModuleBloodbathTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void CollectCandidates_HpBelowThreshold_PushesBloodbathAtPriority6()
    {
        var context = CreateContext(currentHp: 20000, maxHp: 50000, threshold: 0.50f, enableBloodbath: true, level: 100);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);
        context.Setup(x => x.ActionService).Returns(actionService.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);

        _module.CollectCandidates(context.Object, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NikeAbilities.Bloodbath && c.Priority == 6);
    }

    [Fact]
    public void CollectCandidates_BloodbathDisabled_DoesNotPush()
    {
        var context = CreateContext(currentHp: 20000, maxHp: 50000, threshold: 0.50f, enableBloodbath: false, level: 100);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);
        context.Setup(x => x.ActionService).Returns(actionService.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);

        _module.CollectCandidates(context.Object, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NikeAbilities.Bloodbath);
    }

    private static Mock<INikeContext> CreateContext(
        uint currentHp, uint maxHp, float threshold, bool enableBloodbath, byte level)
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableDamage = true,
            MeleeShared = new MeleeSharedConfig
            {
                EnableBloodbath = enableBloodbath,
                BloodbathHpThreshold = threshold,
            },
        };

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentHp: currentHp, maxHp: maxHp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        enemy.Setup(x => x.CurrentHp).Returns(10000u);
        enemy.Setup(x => x.MaxHp).Returns(10000u);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new NikeStatusHelper();
        var partyHelper = new Olympus.Rotation.Common.Helpers.MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = new NikeDebugState();
        var actionService = MockBuilders.CreateMockActionService();

        var mock = new Mock<INikeContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        mock.Setup(x => x.TimelineService).Returns((Olympus.Timeline.ITimelineService?)null);
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);
        mock.Setup(x => x.PartyList).Returns(partyList.Object);
        // Gauge defaults
        mock.Setup(x => x.Kenki).Returns(0);
        mock.Setup(x => x.Sen).Returns(SAMActions.SenType.None);
        mock.Setup(x => x.SenCount).Returns(0);
        mock.Setup(x => x.HasSetsu).Returns(false);
        mock.Setup(x => x.HasGetsu).Returns(false);
        mock.Setup(x => x.HasKa).Returns(false);
        mock.Setup(x => x.Meditation).Returns(0);
        // Buff defaults
        mock.Setup(x => x.HasFugetsu).Returns(false);
        mock.Setup(x => x.FugetsuRemaining).Returns(0f);
        mock.Setup(x => x.HasFuka).Returns(false);
        mock.Setup(x => x.FukaRemaining).Returns(0f);
        mock.Setup(x => x.HasMeikyoShisui).Returns(false);
        mock.Setup(x => x.MeikyoStacks).Returns(0);
        mock.Setup(x => x.HasOgiNamikiriReady).Returns(false);
        mock.Setup(x => x.HasKaeshiNamikiriReady).Returns(false);
        mock.Setup(x => x.HasTsubameGaeshiReady).Returns(false);
        mock.Setup(x => x.HasZanshinReady).Returns(false);
        mock.Setup(x => x.HasTrueNorth).Returns(false);
        // DoT defaults
        mock.Setup(x => x.HasHiganbanaOnTarget).Returns(false);
        mock.Setup(x => x.HiganbanaRemaining).Returns(0f);
        // Iaijutsu tracking
        mock.Setup(x => x.LastIaijutsu).Returns(SAMActions.IaijutsuType.None);
        // Combo defaults
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);
        // Positional defaults
        mock.Setup(x => x.IsAtRear).Returns(false);
        mock.Setup(x => x.IsAtFlank).Returns(false);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mock.Setup(x => x.Debug).Returns(debug);

        return mock;
    }
}
