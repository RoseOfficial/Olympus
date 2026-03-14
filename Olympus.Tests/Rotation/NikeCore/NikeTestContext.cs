using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.NikeCore;

/// <summary>
/// Factory for creating INikeContext mocks for use in Nike module tests.
/// </summary>
public static class NikeTestContext
{
    /// <summary>
    /// Creates an INikeContext mock with configurable state for module tests.
    /// </summary>
    public static INikeContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        int kenki = 0,
        SAMActions.SenType sen = SAMActions.SenType.None,
        int meditation = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool isAtRear = false,
        bool isAtFlank = false,
        bool targetHasPositionalImmunity = false,
        SAMActions.IaijutsuType lastIaijutsu = SAMActions.IaijutsuType.None,
        bool hasFugetsu = false,
        float fugetsuRemaining = 0f,
        bool hasFuka = false,
        float fukaRemaining = 0f,
        bool hasMeikyoShisui = false,
        int meikyoStacks = 0,
        bool hasOgiNamikiriReady = false,
        bool hasKaeshiNamikiriReady = false,
        bool hasTsubameGaeshiReady = false,
        bool hasZanshinReady = false,
        bool hasHiganbanaOnTarget = false,
        float higanbanaRemaining = 0f,
        NikeDebugState? debugState = null)
    {
        config ??= CreateDefaultSamuraiConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new NikeStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new NikeDebugState();

        var mock = new Mock<INikeContext>();

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
        mock.Setup(x => x.Kenki).Returns(kenki);
        mock.Setup(x => x.Sen).Returns(sen);
        mock.Setup(x => x.SenCount).Returns(SAMActions.CountSen(sen));
        mock.Setup(x => x.HasSetsu).Returns((sen & SAMActions.SenType.Setsu) != 0);
        mock.Setup(x => x.HasGetsu).Returns((sen & SAMActions.SenType.Getsu) != 0);
        mock.Setup(x => x.HasKa).Returns((sen & SAMActions.SenType.Ka) != 0);
        mock.Setup(x => x.Meditation).Returns(meditation);

        // Buff state
        mock.Setup(x => x.HasFugetsu).Returns(hasFugetsu);
        mock.Setup(x => x.FugetsuRemaining).Returns(fugetsuRemaining);
        mock.Setup(x => x.HasFuka).Returns(hasFuka);
        mock.Setup(x => x.FukaRemaining).Returns(fukaRemaining);
        mock.Setup(x => x.HasMeikyoShisui).Returns(hasMeikyoShisui);
        mock.Setup(x => x.MeikyoStacks).Returns(meikyoStacks);
        mock.Setup(x => x.HasOgiNamikiriReady).Returns(hasOgiNamikiriReady);
        mock.Setup(x => x.HasKaeshiNamikiriReady).Returns(hasKaeshiNamikiriReady);
        mock.Setup(x => x.HasTsubameGaeshiReady).Returns(hasTsubameGaeshiReady);
        mock.Setup(x => x.HasZanshinReady).Returns(hasZanshinReady);
        mock.Setup(x => x.HasTrueNorth).Returns(false);

        // DoT state
        mock.Setup(x => x.HasHiganbanaOnTarget).Returns(hasHiganbanaOnTarget);
        mock.Setup(x => x.HiganbanaRemaining).Returns(higanbanaRemaining);

        // Iaijutsu tracking
        mock.Setup(x => x.LastIaijutsu).Returns(lastIaijutsu);

        // Combo state
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        // Positional state
        mock.Setup(x => x.IsAtRear).Returns(isAtRear);
        mock.Setup(x => x.IsAtFlank).Returns(isAtFlank);
        mock.Setup(x => x.TargetHasPositionalImmunity).Returns(targetHasPositionalImmunity);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Samurai tests.
    /// </summary>
    public static Configuration CreateDefaultSamuraiConfiguration()
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
