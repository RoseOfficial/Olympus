using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.ZeusCore;

/// <summary>
/// Factory for creating IZeusContext mocks for use in Zeus module tests.
/// </summary>
public static class ZeusTestContext
{
    /// <summary>
    /// Creates an IZeusContext mock with configurable state for module tests.
    /// </summary>
    public static IZeusContext Create(
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
        int firstmindsFocus = 0,
        int eyeCount = 0,
        bool isLifeOfDragonActive = false,
        float lifeOfDragonRemaining = 0f,
        bool isInVorpalCombo = false,
        bool isInDisembowelCombo = false,
        bool isInAoeCombo = false,
        bool hasPowerSurge = false,
        float powerSurgeRemaining = 0f,
        bool hasLanceCharge = false,
        float lanceChargeRemaining = 0f,
        bool hasLifeSurge = false,
        bool hasBattleLitany = false,
        float battleLitanyRemaining = 0f,
        bool hasRightEye = false,
        bool hasDiveReady = false,
        bool hasFangAndClawBared = false,
        bool hasWheelInMotion = false,
        bool hasDraconianFire = false,
        bool hasNastrondReady = false,
        bool hasStardiverReady = false,
        bool hasStarcrossReady = false,
        bool hasDotOnTarget = false,
        float dotRemaining = 0f,
        ZeusDebugState? debugState = null)
    {
        config ??= CreateDefaultDragoonConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new ZeusStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new ZeusDebugState();

        var mock = new Mock<IZeusContext>();

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
        mock.Setup(x => x.FirstmindsFocus).Returns(firstmindsFocus);
        mock.Setup(x => x.EyeCount).Returns(eyeCount);
        mock.Setup(x => x.IsLifeOfDragonActive).Returns(isLifeOfDragonActive);
        mock.Setup(x => x.LifeOfDragonRemaining).Returns(lifeOfDragonRemaining);

        // Combo state
        mock.Setup(x => x.IsInVorpalCombo).Returns(isInVorpalCombo);
        mock.Setup(x => x.IsInDisembowelCombo).Returns(isInDisembowelCombo);
        mock.Setup(x => x.IsInAoeCombo).Returns(isInAoeCombo);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        // Positional state
        mock.Setup(x => x.IsAtRear).Returns(isAtRear);
        mock.Setup(x => x.IsAtFlank).Returns(isAtFlank);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(targetHasPositionalImmunity);
        mock.Setup(x => x.HasTrueNorth).Returns(false);

        // Buff state
        mock.Setup(x => x.HasPowerSurge).Returns(hasPowerSurge);
        mock.Setup(x => x.PowerSurgeRemaining).Returns(powerSurgeRemaining);
        mock.Setup(x => x.HasLanceCharge).Returns(hasLanceCharge);
        mock.Setup(x => x.LanceChargeRemaining).Returns(lanceChargeRemaining);
        mock.Setup(x => x.HasLifeSurge).Returns(hasLifeSurge);
        mock.Setup(x => x.HasBattleLitany).Returns(hasBattleLitany);
        mock.Setup(x => x.BattleLitanyRemaining).Returns(battleLitanyRemaining);
        mock.Setup(x => x.HasRightEye).Returns(hasRightEye);

        // Proc state
        mock.Setup(x => x.HasDiveReady).Returns(hasDiveReady);
        mock.Setup(x => x.HasFangAndClawBared).Returns(hasFangAndClawBared);
        mock.Setup(x => x.HasWheelInMotion).Returns(hasWheelInMotion);
        mock.Setup(x => x.HasDraconianFire).Returns(hasDraconianFire);
        mock.Setup(x => x.HasNastrondReady).Returns(hasNastrondReady);
        mock.Setup(x => x.HasStardiverReady).Returns(hasStardiverReady);
        mock.Setup(x => x.HasStarcrossReady).Returns(hasStarcrossReady);

        // DoT state
        mock.Setup(x => x.HasDotOnTarget).Returns(hasDotOnTarget);
        mock.Setup(x => x.DotRemaining).Returns(dotRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Dragoon tests.
    /// </summary>
    public static Configuration CreateDefaultDragoonConfiguration()
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
