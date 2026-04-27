using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ThanatosCore.Abilities;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Helpers;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Services;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

/// <summary>
/// Smoke test: Thanatos (RPR) DamageModule pushes SecondWind via RoleActionPushers
/// at oGCD priority 6 when HP is below the configured threshold and the action is ready.
/// </summary>
public class DamageModuleSecondWindTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void CollectCandidates_HpBelowThreshold_PushesSecondWindAtPriority6()
    {
        var context = CreateContext(currentHp: 20000, maxHp: 50000, threshold: 0.50f, enableSecondWind: true, level: 100);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        context.Setup(x => x.ActionService).Returns(actionService.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);

        _module.CollectCandidates(context.Object, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ThanatosAbilities.SecondWind && c.Priority == 6);
    }

    [Fact]
    public void CollectCandidates_SecondWindDisabled_DoesNotPush()
    {
        var context = CreateContext(currentHp: 20000, maxHp: 50000, threshold: 0.50f, enableSecondWind: false, level: 100);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        context.Setup(x => x.ActionService).Returns(actionService.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);

        _module.CollectCandidates(context.Object, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThanatosAbilities.SecondWind);
    }

    private static Mock<IThanatosContext> CreateContext(
        uint currentHp, uint maxHp, float threshold, bool enableSecondWind, byte level)
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableDamage = true,
            MeleeShared = new MeleeSharedConfig
            {
                EnableSecondWind = enableSecondWind,
                SecondWindHpThreshold = threshold,
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
        var statusHelper = new ThanatosStatusHelper();
        var partyHelper = new Olympus.Rotation.Common.Helpers.MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = new ThanatosDebugState();
        var actionService = MockBuilders.CreateMockActionService();

        var mock = new Mock<IThanatosContext>();
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
        // Combo defaults
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);
        // Positional defaults
        mock.Setup(x => x.IsAtRear).Returns(false);
        mock.Setup(x => x.IsAtFlank).Returns(false);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(false);
        mock.Setup(x => x.HasTrueNorth).Returns(false);
        // Gauge defaults
        mock.Setup(x => x.Soul).Returns(0);
        mock.Setup(x => x.Shroud).Returns(0);
        mock.Setup(x => x.LemureShroud).Returns(0);
        mock.Setup(x => x.VoidShroud).Returns(0);
        mock.Setup(x => x.IsEnshrouded).Returns(false);
        mock.Setup(x => x.EnshroudTimer).Returns(0f);
        // Soul Reaver defaults
        mock.Setup(x => x.HasSoulReaver).Returns(false);
        mock.Setup(x => x.SoulReaverStacks).Returns(0);
        mock.Setup(x => x.HasEnhancedGibbet).Returns(false);
        mock.Setup(x => x.HasEnhancedGallows).Returns(false);
        mock.Setup(x => x.HasEnhancedVoidReaping).Returns(false);
        mock.Setup(x => x.HasEnhancedCrossReaping).Returns(false);
        // Buff defaults
        mock.Setup(x => x.HasArcaneCircle).Returns(false);
        mock.Setup(x => x.ArcaneCircleRemaining).Returns(0f);
        mock.Setup(x => x.HasBloodsownCircle).Returns(false);
        mock.Setup(x => x.ImmortalSacrificeStacks).Returns(0);
        mock.Setup(x => x.HasSoulsow).Returns(false);
        // Proc defaults
        mock.Setup(x => x.HasPerfectioParata).Returns(false);
        mock.Setup(x => x.HasOblatio).Returns(false);
        mock.Setup(x => x.HasIdealHost).Returns(false);
        mock.Setup(x => x.HasEnhancedHarpe).Returns(false);
        // Target debuff defaults
        mock.Setup(x => x.HasDeathsDesign).Returns(false);
        mock.Setup(x => x.DeathsDesignRemaining).Returns(0f);
        mock.Setup(x => x.Debug).Returns(debug);

        return mock;
    }
}
