using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.PrometheusCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.PrometheusCore;

/// <summary>
/// Factory for creating IPrometheusContext mocks for use in Prometheus module tests.
/// </summary>
public static class PrometheusTestContext
{
    /// <summary>
    /// Creates an IPrometheusContext mock with configurable state for module tests.
    /// </summary>
    public static IPrometheusContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        // Gauge state
        int heat = 0,
        int battery = 0,
        bool isOverheated = false,
        float overheatRemaining = 0f,
        bool isQueenActive = false,
        float queenRemaining = 0f,
        int lastQueenBattery = 0,
        // Buff state
        bool hasReassemble = false,
        float reassembleRemaining = 0f,
        bool hasHypercharged = false,
        bool hasFullMetalMachinist = false,
        bool hasExcavatorReady = false,
        // Target state
        bool hasWildfire = false,
        float wildfireRemaining = 0f,
        bool hasBioblaster = false,
        float bioblasterRemaining = 0f,
        // Cooldown tracking
        int drillCharges = 0,
        int reassembleCharges = 0,
        int gaussRoundCharges = 0,
        int ricochetCharges = 0,
        // Combo state
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        PrometheusDebugState? debugState = null)
    {
        config ??= CreateDefaultMachinistConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new PrometheusStatusHelper();
        var partyHelper = new RangedDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new PrometheusDebugState();

        var mock = new Mock<IPrometheusContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(isMoving);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        mock.Setup(x => x.TimelineService).Returns((ITimelineService?)null);
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);
        mock.Setup(x => x.PartyList).Returns(partyList.Object);

        // Gauge state
        mock.Setup(x => x.Heat).Returns(heat);
        mock.Setup(x => x.Battery).Returns(battery);
        mock.Setup(x => x.IsOverheated).Returns(isOverheated);
        mock.Setup(x => x.OverheatRemaining).Returns(overheatRemaining);
        mock.Setup(x => x.IsQueenActive).Returns(isQueenActive);
        mock.Setup(x => x.QueenRemaining).Returns(queenRemaining);
        mock.Setup(x => x.LastQueenBattery).Returns(lastQueenBattery);

        // Buff state
        mock.Setup(x => x.HasReassemble).Returns(hasReassemble);
        mock.Setup(x => x.ReassembleRemaining).Returns(reassembleRemaining);
        mock.Setup(x => x.HasHypercharged).Returns(hasHypercharged);
        mock.Setup(x => x.HasFullMetalMachinist).Returns(hasFullMetalMachinist);
        mock.Setup(x => x.HasExcavatorReady).Returns(hasExcavatorReady);

        // Target state
        mock.Setup(x => x.HasWildfire).Returns(hasWildfire);
        mock.Setup(x => x.WildfireRemaining).Returns(wildfireRemaining);
        mock.Setup(x => x.HasBioblaster).Returns(hasBioblaster);
        mock.Setup(x => x.BioblasterRemaining).Returns(bioblasterRemaining);

        // Cooldown tracking
        mock.Setup(x => x.DrillCharges).Returns(drillCharges);
        mock.Setup(x => x.ReassembleCharges).Returns(reassembleCharges);
        mock.Setup(x => x.GaussRoundCharges).Returns(gaussRoundCharges);
        mock.Setup(x => x.RicochetCharges).Returns(ricochetCharges);

        // Combo state
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Machinist tests.
    /// </summary>
    public static Configuration CreateDefaultMachinistConfiguration()
    {
        return new Configuration
        {
            Enabled = true,
            EnableDamage = true,
            EnableHealing = false,
            EnableDoT = false,
        };
    }
}
