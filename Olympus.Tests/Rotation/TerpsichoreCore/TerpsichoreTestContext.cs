using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.TerpsichoreCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.TerpsichoreCore;

/// <summary>
/// Factory for creating ITerpsichoreContext mocks for use in Terpsichore module tests.
/// </summary>
public static class TerpsichoreTestContext
{
    /// <summary>
    /// Creates an ITerpsichoreContext mock with configurable state for module tests.
    /// </summary>
    public static ITerpsichoreContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        // Gauge state
        int esprit = 0,
        int feathers = 0,
        bool isDancing = false,
        int stepIndex = 0,
        byte currentStep = 0,
        byte[]? danceSteps = null,
        // Proc state
        bool hasSilkenSymmetry = false,
        bool hasSilkenFlow = false,
        bool hasThreefoldFanDance = false,
        bool hasFourfoldFanDance = false,
        // Buff state
        bool hasFlourishingFinish = false,
        bool hasFlourishingStarfall = false,
        bool hasDevilment = false,
        float devilmentRemaining = 0f,
        bool hasStandardFinish = false,
        bool hasTechnicalFinish = false,
        // High-level procs
        bool hasLastDanceReady = false,
        bool hasFinishingMoveReady = false,
        bool hasDanceOfTheDawnReady = false,
        // Partner state
        bool hasDancePartner = false,
        uint dancePartnerId = 0,
        // Combo state
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        TerpsichoreDebugState? debugState = null)
    {
        config ??= CreateDefaultDancerConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new TerpsichoreStatusHelper();
        var partyHelper = new TerpsichorePartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new TerpsichoreDebugState();

        var mock = new Mock<ITerpsichoreContext>();

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
        mock.Setup(x => x.Esprit).Returns(esprit);
        mock.Setup(x => x.Feathers).Returns(feathers);
        mock.Setup(x => x.IsDancing).Returns(isDancing);
        mock.Setup(x => x.StepIndex).Returns(stepIndex);
        mock.Setup(x => x.CurrentStep).Returns(currentStep);
        mock.Setup(x => x.DanceSteps).Returns(danceSteps ?? new byte[4]);

        // Proc state
        mock.Setup(x => x.HasSilkenSymmetry).Returns(hasSilkenSymmetry);
        mock.Setup(x => x.HasSilkenFlow).Returns(hasSilkenFlow);
        mock.Setup(x => x.HasThreefoldFanDance).Returns(hasThreefoldFanDance);
        mock.Setup(x => x.HasFourfoldFanDance).Returns(hasFourfoldFanDance);

        // Buff state
        mock.Setup(x => x.HasFlourishingFinish).Returns(hasFlourishingFinish);
        mock.Setup(x => x.HasFlourishingStarfall).Returns(hasFlourishingStarfall);
        mock.Setup(x => x.HasDevilment).Returns(hasDevilment);
        mock.Setup(x => x.DevilmentRemaining).Returns(devilmentRemaining);
        mock.Setup(x => x.HasStandardFinish).Returns(hasStandardFinish);
        mock.Setup(x => x.HasTechnicalFinish).Returns(hasTechnicalFinish);

        // High-level procs
        mock.Setup(x => x.HasLastDanceReady).Returns(hasLastDanceReady);
        mock.Setup(x => x.HasFinishingMoveReady).Returns(hasFinishingMoveReady);
        mock.Setup(x => x.HasDanceOfTheDawnReady).Returns(hasDanceOfTheDawnReady);

        // Partner state
        mock.Setup(x => x.HasDancePartner).Returns(hasDancePartner);
        mock.Setup(x => x.DancePartnerId).Returns(dancePartnerId);

        // Combo state
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Dancer tests.
    /// </summary>
    public static Configuration CreateDefaultDancerConfiguration()
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
