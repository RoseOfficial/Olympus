using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.KratosCore;

/// <summary>
/// Factory for creating IKratosContext mocks for use in Kratos module tests.
/// </summary>
public static class KratosTestContext
{
    /// <summary>
    /// Creates an IKratosContext mock with configurable state for module tests.
    /// </summary>
    public static IKratosContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool isAtRear = false,
        bool isAtFlank = false,
        bool targetHasPositionalImmunity = false,
        MonkForm currentForm = MonkForm.None,
        bool hasFormlessFist = false,
        bool hasPerfectBalance = false,
        int perfectBalanceStacks = 0,
        int chakra = 0,
        byte beastChakra1 = 0,
        byte beastChakra2 = 0,
        byte beastChakra3 = 0,
        int beastChakraCount = 0,
        bool hasLunarNadi = false,
        bool hasSolarNadi = false,
        bool hasBothNadi = false,
        bool hasDisciplinedFist = false,
        float disciplinedFistRemaining = 0f,
        bool hasLeadenFist = false,
        bool hasRiddleOfFire = false,
        float riddleOfFireRemaining = 0f,
        bool hasBrotherhood = false,
        bool hasRiddleOfWind = false,
        bool hasRaptorsFury = false,
        bool hasCoeurlsFury = false,
        bool hasOpooposFury = false,
        bool hasFiresRumination = false,
        bool hasWindsRumination = false,
        bool hasDemolishOnTarget = false,
        float demolishRemaining = 0f,
        KratosDebugState? debugState = null)
    {
        config ??= CreateDefaultMonkConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new KratosStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new KratosDebugState();

        var mock = new Mock<IKratosContext>();

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

        // Combo state
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        // Positional state
        mock.Setup(x => x.IsAtRear).Returns(isAtRear);
        mock.Setup(x => x.IsAtFlank).Returns(isAtFlank);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(targetHasPositionalImmunity);
        mock.Setup(x => x.HasTrueNorth).Returns(false);

        // Form state
        mock.Setup(x => x.CurrentForm).Returns(currentForm);
        mock.Setup(x => x.HasFormlessFist).Returns(hasFormlessFist);
        mock.Setup(x => x.HasPerfectBalance).Returns(hasPerfectBalance);
        mock.Setup(x => x.PerfectBalanceStacks).Returns(perfectBalanceStacks);

        // Chakra gauge
        mock.Setup(x => x.Chakra).Returns(chakra);
        mock.Setup(x => x.BeastChakra1).Returns(beastChakra1);
        mock.Setup(x => x.BeastChakra2).Returns(beastChakra2);
        mock.Setup(x => x.BeastChakra3).Returns(beastChakra3);
        mock.Setup(x => x.BeastChakraCount).Returns(beastChakraCount);
        mock.Setup(x => x.HasLunarNadi).Returns(hasLunarNadi);
        mock.Setup(x => x.HasSolarNadi).Returns(hasSolarNadi);
        mock.Setup(x => x.HasBothNadi).Returns(hasBothNadi);

        // Buff state
        mock.Setup(x => x.HasDisciplinedFist).Returns(hasDisciplinedFist);
        mock.Setup(x => x.DisciplinedFistRemaining).Returns(disciplinedFistRemaining);
        mock.Setup(x => x.HasLeadenFist).Returns(hasLeadenFist);
        mock.Setup(x => x.HasRiddleOfFire).Returns(hasRiddleOfFire);
        mock.Setup(x => x.RiddleOfFireRemaining).Returns(riddleOfFireRemaining);
        mock.Setup(x => x.HasBrotherhood).Returns(hasBrotherhood);
        mock.Setup(x => x.HasRiddleOfWind).Returns(hasRiddleOfWind);

        // Proc state
        mock.Setup(x => x.HasRaptorsFury).Returns(hasRaptorsFury);
        mock.Setup(x => x.HasCoeurlsFury).Returns(hasCoeurlsFury);
        mock.Setup(x => x.HasOpooposFury).Returns(hasOpooposFury);
        mock.Setup(x => x.HasFiresRumination).Returns(hasFiresRumination);
        mock.Setup(x => x.HasWindsRumination).Returns(hasWindsRumination);

        // DoT state
        mock.Setup(x => x.HasDemolishOnTarget).Returns(hasDemolishOnTarget);
        mock.Setup(x => x.DemolishRemaining).Returns(demolishRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Monk tests.
    /// </summary>
    public static Configuration CreateDefaultMonkConfiguration()
    {
        return new Configuration
        {
            Enabled = true,
            EnableDamage = true,
            EnableHealing = false,
            EnableDoT = true,
        };
    }
}
