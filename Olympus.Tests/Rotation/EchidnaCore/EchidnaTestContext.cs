using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.EchidnaCore;

/// <summary>
/// Factory for creating IEchidnaContext mocks for use in Echidna module tests.
/// </summary>
public static class EchidnaTestContext
{
    /// <summary>
    /// Creates an IEchidnaContext mock with configurable state for module tests.
    /// </summary>
    public static IEchidnaContext Create(
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
        // Gauge state
        int serpentOffering = 0,
        int anguineTribute = 0,
        int rattlingCoils = 0,
        bool isReawakened = false,
        VPRActions.DreadCombo dreadCombo = VPRActions.DreadCombo.None,
        VPRActions.SerpentCombo serpentCombo = VPRActions.SerpentCombo.None,
        // Buff state
        bool hasHuntersInstinct = false,
        float huntersInstinctRemaining = 0f,
        bool hasSwiftscaled = false,
        float swiftscaledRemaining = 0f,
        bool hasHonedSteel = false,
        bool hasHonedReavers = false,
        bool hasReadyToReawaken = false,
        // Venom buffs
        bool hasFlankstungVenom = false,
        bool hasHindstungVenom = false,
        bool hasFlanksbaneVenom = false,
        bool hasHindsbaneVenom = false,
        bool hasGrimskinsVenom = false,
        bool hasGrimhuntersVenom = false,
        // oGCD proc state
        bool hasPoisedForTwinfang = false,
        bool hasPoisedForTwinblood = false,
        // Target state
        bool hasNoxiousGnash = false,
        float noxiousGnashRemaining = 0f,
        EchidnaDebugState? debugState = null)
    {
        config ??= CreateDefaultViperConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new EchidnaStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new EchidnaDebugState();

        var mock = new Mock<IEchidnaContext>();

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

        // Gauge state
        mock.Setup(x => x.SerpentOffering).Returns(serpentOffering);
        mock.Setup(x => x.AnguineTribute).Returns(anguineTribute);
        mock.Setup(x => x.RattlingCoils).Returns(rattlingCoils);
        mock.Setup(x => x.IsReawakened).Returns(isReawakened);
        mock.Setup(x => x.DreadCombo).Returns(dreadCombo);
        mock.Setup(x => x.SerpentCombo).Returns(serpentCombo);

        // Buff state
        mock.Setup(x => x.HasHuntersInstinct).Returns(hasHuntersInstinct);
        mock.Setup(x => x.HuntersInstinctRemaining).Returns(huntersInstinctRemaining);
        mock.Setup(x => x.HasSwiftscaled).Returns(hasSwiftscaled);
        mock.Setup(x => x.SwiftscaledRemaining).Returns(swiftscaledRemaining);
        mock.Setup(x => x.HasHonedSteel).Returns(hasHonedSteel);
        mock.Setup(x => x.HasHonedReavers).Returns(hasHonedReavers);
        mock.Setup(x => x.HasReadyToReawaken).Returns(hasReadyToReawaken);

        // Venom buffs
        mock.Setup(x => x.HasFlankstungVenom).Returns(hasFlankstungVenom);
        mock.Setup(x => x.HasHindstungVenom).Returns(hasHindstungVenom);
        mock.Setup(x => x.HasFlanksbaneVenom).Returns(hasFlanksbaneVenom);
        mock.Setup(x => x.HasHindsbaneVenom).Returns(hasHindsbaneVenom);
        mock.Setup(x => x.HasGrimskinsVenom).Returns(hasGrimskinsVenom);
        mock.Setup(x => x.HasGrimhuntersVenom).Returns(hasGrimhuntersVenom);

        // oGCD proc state
        mock.Setup(x => x.HasPoisedForTwinfang).Returns(hasPoisedForTwinfang);
        mock.Setup(x => x.HasPoisedForTwinblood).Returns(hasPoisedForTwinblood);

        // Target state
        mock.Setup(x => x.HasNoxiousGnash).Returns(hasNoxiousGnash);
        mock.Setup(x => x.NoxiousGnashRemaining).Returns(noxiousGnashRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Viper tests.
    /// </summary>
    public static Configuration CreateDefaultViperConfiguration()
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
