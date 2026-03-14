using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.HermesCore;

/// <summary>
/// Factory for creating IHermesContext mocks for use in Hermes module tests.
/// </summary>
public static class HermesTestContext
{
    /// <summary>
    /// Creates an IHermesContext mock with configurable state for module tests.
    /// </summary>
    public static IHermesContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        MudraHelper? mudraHelper = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        int ninki = 0,
        int kazematoi = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool isAtRear = false,
        bool isAtFlank = false,
        bool targetHasPositionalImmunity = false,
        // Mudra state
        bool isMudraActive = false,
        int mudraCount = 0,
        NINActions.MudraType mudra1 = NINActions.MudraType.None,
        NINActions.MudraType mudra2 = NINActions.MudraType.None,
        NINActions.MudraType mudra3 = NINActions.MudraType.None,
        // Buff state
        bool hasKassatsu = false,
        bool hasTenChiJin = false,
        int tenChiJinStacks = 0,
        bool hasSuiton = false,
        float suitonRemaining = 0f,
        bool hasBunshin = false,
        int bunshinStacks = 0,
        bool hasPhantomKamaitachiReady = false,
        bool hasRaijuReady = false,
        int raijuStacks = 0,
        bool hasMeisui = false,
        bool hasTenriJindoReady = false,
        // Debuff state
        bool hasKunaisBaneOnTarget = false,
        float kunaisBaneRemaining = 0f,
        bool hasDokumoriOnTarget = false,
        float dokumoriRemaining = 0f,
        HermesDebugState? debugState = null)
    {
        config ??= CreateDefaultNinjaConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new HermesStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var helper = mudraHelper ?? new MudraHelper();
        var debug = debugState ?? new HermesDebugState();

        var mock = new Mock<IHermesContext>();

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
        mock.Setup(x => x.MudraHelper).Returns(helper);
        mock.Setup(x => x.PartyList).Returns(partyList.Object);

        // Gauge state
        mock.Setup(x => x.Ninki).Returns(ninki);
        mock.Setup(x => x.Kazematoi).Returns(kazematoi);

        // Mudra state
        mock.Setup(x => x.IsMudraActive).Returns(isMudraActive || helper.IsSequenceActive);
        mock.Setup(x => x.MudraCount).Returns(mudraCount > 0 ? mudraCount : helper.MudraCount);
        mock.Setup(x => x.Mudra1).Returns(mudra1 != NINActions.MudraType.None ? mudra1 : helper.Mudra1);
        mock.Setup(x => x.Mudra2).Returns(mudra2 != NINActions.MudraType.None ? mudra2 : helper.Mudra2);
        mock.Setup(x => x.Mudra3).Returns(mudra3 != NINActions.MudraType.None ? mudra3 : helper.Mudra3);

        // Buff state
        mock.Setup(x => x.HasKassatsu).Returns(hasKassatsu);
        mock.Setup(x => x.HasTenChiJin).Returns(hasTenChiJin);
        mock.Setup(x => x.TenChiJinStacks).Returns(tenChiJinStacks);
        mock.Setup(x => x.HasSuiton).Returns(hasSuiton);
        mock.Setup(x => x.SuitonRemaining).Returns(suitonRemaining);
        mock.Setup(x => x.HasBunshin).Returns(hasBunshin);
        mock.Setup(x => x.BunshinStacks).Returns(bunshinStacks);
        mock.Setup(x => x.HasPhantomKamaitachiReady).Returns(hasPhantomKamaitachiReady);
        mock.Setup(x => x.HasRaijuReady).Returns(hasRaijuReady);
        mock.Setup(x => x.RaijuStacks).Returns(raijuStacks);
        mock.Setup(x => x.HasMeisui).Returns(hasMeisui);
        mock.Setup(x => x.HasTenriJindoReady).Returns(hasTenriJindoReady);
        mock.Setup(x => x.HasTrueNorth).Returns(false);

        // Debuff state
        mock.Setup(x => x.HasKunaisBaneOnTarget).Returns(hasKunaisBaneOnTarget);
        mock.Setup(x => x.KunaisBaneRemaining).Returns(kunaisBaneRemaining);
        mock.Setup(x => x.HasDokumoriOnTarget).Returns(hasDokumoriOnTarget);
        mock.Setup(x => x.DokumoriRemaining).Returns(dokumoriRemaining);

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
    /// Creates a default Configuration for Ninja tests.
    /// </summary>
    public static Configuration CreateDefaultNinjaConfiguration()
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
