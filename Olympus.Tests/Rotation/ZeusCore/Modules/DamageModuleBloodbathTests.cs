using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ZeusCore.Abilities;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Helpers;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

/// <summary>
/// Smoke test: Zeus (DRG) DamageModule pushes Bloodbath via RoleActionPushers
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
            c => c.Behavior == ZeusAbilities.Bloodbath && c.Priority == 6);
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

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ZeusAbilities.Bloodbath);
    }

    private static Mock<IZeusContext> CreateContext(
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
        var statusHelper = new ZeusStatusHelper();
        var partyHelper = new Olympus.Rotation.Common.Helpers.MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = new ZeusDebugState();
        var actionService = MockBuilders.CreateMockActionService();

        var mock = new Mock<IZeusContext>();
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
        mock.Setup(x => x.FirstmindsFocus).Returns(0);
        mock.Setup(x => x.EyeCount).Returns(0);
        mock.Setup(x => x.IsLifeOfDragonActive).Returns(false);
        mock.Setup(x => x.LifeOfDragonRemaining).Returns(0f);
        // Combo defaults
        mock.Setup(x => x.IsInVorpalCombo).Returns(false);
        mock.Setup(x => x.IsInDisembowelCombo).Returns(false);
        mock.Setup(x => x.IsInAoeCombo).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);
        // Positional defaults
        mock.Setup(x => x.IsAtRear).Returns(false);
        mock.Setup(x => x.IsAtFlank).Returns(false);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mock.Setup(x => x.HasTrueNorth).Returns(false);
        // Buff defaults
        mock.Setup(x => x.HasPowerSurge).Returns(false);
        mock.Setup(x => x.PowerSurgeRemaining).Returns(0f);
        mock.Setup(x => x.HasLanceCharge).Returns(false);
        mock.Setup(x => x.LanceChargeRemaining).Returns(0f);
        mock.Setup(x => x.HasLifeSurge).Returns(false);
        mock.Setup(x => x.HasBattleLitany).Returns(false);
        mock.Setup(x => x.BattleLitanyRemaining).Returns(0f);
        mock.Setup(x => x.HasRightEye).Returns(false);
        // Proc defaults
        mock.Setup(x => x.HasDiveReady).Returns(false);
        mock.Setup(x => x.HasFangAndClawBared).Returns(false);
        mock.Setup(x => x.HasWheelInMotion).Returns(false);
        mock.Setup(x => x.HasDraconianFire).Returns(false);
        mock.Setup(x => x.HasNastrondReady).Returns(false);
        mock.Setup(x => x.HasStardiverReady).Returns(false);
        mock.Setup(x => x.HasStarcrossReady).Returns(false);
        // DoT defaults
        mock.Setup(x => x.HasDotOnTarget).Returns(false);
        mock.Setup(x => x.DotRemaining).Returns(0f);
        mock.Setup(x => x.Debug).Returns(debug);

        return mock;
    }
}
