using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.ThanatosCore;

/// <summary>
/// Factory for creating IThanatosContext mocks for use in Thanatos module tests.
/// </summary>
public static class ThanatosTestContext
{
    /// <summary>
    /// Creates an IThanatosContext mock with configurable state for module tests.
    /// </summary>
    public static IThanatosContext Create(
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
        // Gauge
        int soul = 0,
        int shroud = 0,
        int lemureShroud = 0,
        int voidShroud = 0,
        bool isEnshrouded = false,
        float enshroudTimer = 0f,
        // Soul Reaver
        bool hasSoulReaver = false,
        int soulReaverStacks = 0,
        bool hasEnhancedGibbet = false,
        bool hasEnhancedGallows = false,
        bool hasEnhancedVoidReaping = false,
        bool hasEnhancedCrossReaping = false,
        // Buffs
        bool hasArcaneCircle = false,
        float arcaneCircleRemaining = 0f,
        bool hasBloodsownCircle = false,
        int immortalSacrificeStacks = 0,
        bool hasSoulsow = false,
        // Procs
        bool hasPerfectioParata = false,
        bool hasOblatio = false,
        bool hasIdealHost = false,
        bool hasEnhancedHarpe = false,
        // Target debuff
        bool hasDeathsDesign = false,
        float deathsDesignRemaining = 0f,
        ThanatosDebugState? debugState = null)
    {
        config ??= CreateDefaultReaperConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new ThanatosStatusHelper();
        var partyHelper = new MeleeDpsPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new ThanatosDebugState();

        var mock = new Mock<IThanatosContext>();

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
        mock.Setup(x => x.Soul).Returns(soul);
        mock.Setup(x => x.Shroud).Returns(shroud);
        mock.Setup(x => x.LemureShroud).Returns(lemureShroud);
        mock.Setup(x => x.VoidShroud).Returns(voidShroud);
        mock.Setup(x => x.IsEnshrouded).Returns(isEnshrouded);
        mock.Setup(x => x.EnshroudTimer).Returns(enshroudTimer);

        // Soul Reaver state
        mock.Setup(x => x.HasSoulReaver).Returns(hasSoulReaver);
        mock.Setup(x => x.SoulReaverStacks).Returns(soulReaverStacks);
        mock.Setup(x => x.HasEnhancedGibbet).Returns(hasEnhancedGibbet);
        mock.Setup(x => x.HasEnhancedGallows).Returns(hasEnhancedGallows);
        mock.Setup(x => x.HasEnhancedVoidReaping).Returns(hasEnhancedVoidReaping);
        mock.Setup(x => x.HasEnhancedCrossReaping).Returns(hasEnhancedCrossReaping);

        // Buff state
        mock.Setup(x => x.HasArcaneCircle).Returns(hasArcaneCircle);
        mock.Setup(x => x.ArcaneCircleRemaining).Returns(arcaneCircleRemaining);
        mock.Setup(x => x.HasBloodsownCircle).Returns(hasBloodsownCircle);
        mock.Setup(x => x.ImmortalSacrificeStacks).Returns(immortalSacrificeStacks);
        mock.Setup(x => x.HasSoulsow).Returns(hasSoulsow);

        // Proc state
        mock.Setup(x => x.HasPerfectioParata).Returns(hasPerfectioParata);
        mock.Setup(x => x.HasOblatio).Returns(hasOblatio);
        mock.Setup(x => x.HasIdealHost).Returns(hasIdealHost);
        mock.Setup(x => x.HasEnhancedHarpe).Returns(hasEnhancedHarpe);

        // Target debuff
        mock.Setup(x => x.HasDeathsDesign).Returns(hasDeathsDesign);
        mock.Setup(x => x.DeathsDesignRemaining).Returns(deathsDesignRemaining);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration for Reaper tests.
    /// </summary>
    public static Configuration CreateDefaultReaperConfiguration()
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
