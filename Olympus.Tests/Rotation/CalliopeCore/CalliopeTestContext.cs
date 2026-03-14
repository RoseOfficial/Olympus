using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.CalliopeCore;

/// <summary>
/// Factory for creating ICalliopeContext mocks for use in Calliope module tests.
/// </summary>
public static class CalliopeTestContext
{
    /// <summary>
    /// Creates an ICalliopeContext mock with configurable state for module tests.
    /// </summary>
    public static ICalliopeContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        // Gauge state
        int soulVoice = 0,
        float songTimer = 0f,
        int repertoire = 0,
        byte currentSong = (byte)BRDActions.Song.None,
        int codaCount = 0,
        // Song state
        bool isWanderersMinuetActive = false,
        bool isMagesBalladActive = false,
        bool isArmysPaeonActive = false,
        bool noSongActive = true,
        // Buff state
        bool hasHawksEye = false,
        bool hasRagingStrikes = false,
        float ragingStrikesRemaining = 0f,
        bool hasBattleVoice = false,
        bool hasBarrage = false,
        bool hasRadiantFinale = false,
        bool hasBlastArrowReady = false,
        bool hasResonantArrowReady = false,
        bool hasRadiantEncoreReady = false,
        // DoT state
        bool hasCausticBite = false,
        float causticBiteRemaining = 0f,
        bool hasStormbite = false,
        float stormbiteRemaining = 0f,
        // Cooldown tracking
        int bloodletterCharges = 0,
        // Combo state
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        CalliopeDebugState? debugState = null)
    {
        config ??= CreateDefaultBardConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new CalliopeStatusHelper();
        var partyHelper = new RangedDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new CalliopeDebugState();

        var mock = new Mock<ICalliopeContext>();

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
        mock.Setup(x => x.SoulVoice).Returns(soulVoice);
        mock.Setup(x => x.SongTimer).Returns(songTimer);
        mock.Setup(x => x.Repertoire).Returns(repertoire);
        mock.Setup(x => x.CurrentSong).Returns(currentSong);
        mock.Setup(x => x.CodaCount).Returns(codaCount);

        // Song state
        mock.Setup(x => x.IsWanderersMinuetActive).Returns(isWanderersMinuetActive);
        mock.Setup(x => x.IsMagesBalladActive).Returns(isMagesBalladActive);
        mock.Setup(x => x.IsArmysPaeonActive).Returns(isArmysPaeonActive);
        mock.Setup(x => x.NoSongActive).Returns(noSongActive);

        // Buff state
        mock.Setup(x => x.HasHawksEye).Returns(hasHawksEye);
        mock.Setup(x => x.HasRagingStrikes).Returns(hasRagingStrikes);
        mock.Setup(x => x.RagingStrikesRemaining).Returns(ragingStrikesRemaining);
        mock.Setup(x => x.HasBattleVoice).Returns(hasBattleVoice);
        mock.Setup(x => x.HasBarrage).Returns(hasBarrage);
        mock.Setup(x => x.HasRadiantFinale).Returns(hasRadiantFinale);
        mock.Setup(x => x.HasBlastArrowReady).Returns(hasBlastArrowReady);
        mock.Setup(x => x.HasResonantArrowReady).Returns(hasResonantArrowReady);
        mock.Setup(x => x.HasRadiantEncoreReady).Returns(hasRadiantEncoreReady);

        // DoT state
        mock.Setup(x => x.HasCausticBite).Returns(hasCausticBite);
        mock.Setup(x => x.CausticBiteRemaining).Returns(causticBiteRemaining);
        mock.Setup(x => x.HasStormbite).Returns(hasStormbite);
        mock.Setup(x => x.StormbiteRemaining).Returns(stormbiteRemaining);

        // Cooldown tracking
        mock.Setup(x => x.BloodletterCharges).Returns(bloodletterCharges);

        // Combo state
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(comboTimeRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Bard tests.
    /// </summary>
    public static Configuration CreateDefaultBardConfiguration()
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
