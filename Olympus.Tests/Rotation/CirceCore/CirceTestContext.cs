using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.CirceCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.CirceCore;

/// <summary>
/// Factory for creating ICirceContext mocks for use in Circe module tests.
/// </summary>
public static class CirceTestContext
{
    /// <summary>
    /// Creates an ICirceContext mock with configurable state for module tests.
    /// </summary>
    public static ICirceContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        ITimelineService? timelineService = null,
        byte level = 100,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        // ICasterDpsRotationContext base
        int currentMp = 10000,
        int maxMp = 10000,
        float mpPercent = 1f,
        bool isCasting = false,
        float castRemaining = 0f,
        bool canSlidecast = false,
        bool hasTriplecast = false,
        int triplecastStacks = 0,
        bool hasInstantCast = false,
        // IRotationContext base
        bool hasSwiftcast = false,
        // Mana state
        int blackMana = 0,
        int whiteMana = 0,
        int manaImbalance = 0,
        int absoluteManaImbalance = 0,
        bool isManaBalanced = true,
        int manaStacks = 0,
        bool canStartMeleeCombo = false,
        int lowerMana = 0,
        // Dualcast state
        bool hasDualcast = false,
        float dualcastRemaining = 0f,
        bool shouldHardcast = true,
        // Procs
        bool hasVerfire = false,
        float verfireRemaining = 0f,
        bool hasVerstone = false,
        float verstoneRemaining = 0f,
        bool hasAnyProc = false,
        bool hasBothProcs = false,
        // Melee combo state
        bool isInMeleeCombo = false,
        int meleeComboStep = 0,
        bool isFinisherReady = false,
        bool isScorchReady = false,
        bool isResolutionReady = false,
        bool isGrandImpactReady = false,
        // Buffs
        bool hasEmbolden = false,
        float emboldenRemaining = 0f,
        bool hasManafication = false,
        float manaficationRemaining = 0f,
        bool hasAcceleration = false,
        float accelerationRemaining = 0f,
        bool hasThornedFlourish = false,
        bool hasPrefulgenceReady = false,
        bool hasMagickBarrier = false,
        // Cooldowns
        bool flecheReady = false,
        bool contreSixteReady = false,
        bool emboldenReady = false,
        bool manaficationReady = false,
        int corpsACorpsCharges = 0,
        int engagementCharges = 0,
        int accelerationCharges = 0,
        bool swiftcastReady = false,
        bool lucidDreamingReady = false,
        CirceDebugState? debugState = null)
    {
        config ??= CreateDefaultRdmConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentMp: (uint)currentMp, maxMp: (uint)maxMp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new CirceStatusHelper();
        var partyHelper = new CasterPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new CirceDebugState();

        var mock = new Mock<ICirceContext>();

        // IRotationContext
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(isMoving);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService);
        mock.Setup(x => x.PartyList).Returns(partyList.Object);
        mock.Setup(x => x.HasSwiftcast).Returns(hasSwiftcast);

        // ICasterDpsRotationContext
        mock.Setup(x => x.CurrentMp).Returns(currentMp);
        mock.Setup(x => x.MaxMp).Returns(maxMp);
        mock.Setup(x => x.MpPercent).Returns(mpPercent);
        mock.Setup(x => x.IsCasting).Returns(isCasting);
        mock.Setup(x => x.CastRemaining).Returns(castRemaining);
        mock.Setup(x => x.CanSlidecast).Returns(canSlidecast);
        mock.Setup(x => x.HasTriplecast).Returns(hasTriplecast);
        mock.Setup(x => x.TriplecastStacks).Returns(triplecastStacks);
        mock.Setup(x => x.HasInstantCast).Returns(hasInstantCast);

        // Helpers
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        // Mana state
        mock.Setup(x => x.BlackMana).Returns(blackMana);
        mock.Setup(x => x.WhiteMana).Returns(whiteMana);
        mock.Setup(x => x.ManaImbalance).Returns(manaImbalance);
        mock.Setup(x => x.AbsoluteManaImbalance).Returns(absoluteManaImbalance);
        mock.Setup(x => x.IsManaBalanced).Returns(isManaBalanced);
        mock.Setup(x => x.ManaStacks).Returns(manaStacks);
        mock.Setup(x => x.CanStartMeleeCombo).Returns(canStartMeleeCombo);
        mock.Setup(x => x.LowerMana).Returns(lowerMana);

        // Dualcast state
        mock.Setup(x => x.HasDualcast).Returns(hasDualcast);
        mock.Setup(x => x.DualcastRemaining).Returns(dualcastRemaining);
        mock.Setup(x => x.ShouldHardcast).Returns(shouldHardcast);

        // Procs
        mock.Setup(x => x.HasVerfire).Returns(hasVerfire);
        mock.Setup(x => x.VerfireRemaining).Returns(verfireRemaining);
        mock.Setup(x => x.HasVerstone).Returns(hasVerstone);
        mock.Setup(x => x.VerstoneRemaining).Returns(verstoneRemaining);
        mock.Setup(x => x.HasAnyProc).Returns(hasAnyProc);
        mock.Setup(x => x.HasBothProcs).Returns(hasBothProcs);

        // Melee combo state
        mock.Setup(x => x.IsInMeleeCombo).Returns(isInMeleeCombo);
        mock.Setup(x => x.MeleeComboStep).Returns(meleeComboStep);
        mock.Setup(x => x.IsFinisherReady).Returns(isFinisherReady);
        mock.Setup(x => x.IsScorchReady).Returns(isScorchReady);
        mock.Setup(x => x.IsResolutionReady).Returns(isResolutionReady);
        mock.Setup(x => x.IsGrandImpactReady).Returns(isGrandImpactReady);

        // Buffs
        mock.Setup(x => x.HasEmbolden).Returns(hasEmbolden);
        mock.Setup(x => x.EmboldenRemaining).Returns(emboldenRemaining);
        mock.Setup(x => x.HasManafication).Returns(hasManafication);
        mock.Setup(x => x.ManaficationRemaining).Returns(manaficationRemaining);
        mock.Setup(x => x.HasAcceleration).Returns(hasAcceleration);
        mock.Setup(x => x.AccelerationRemaining).Returns(accelerationRemaining);
        mock.Setup(x => x.HasThornedFlourish).Returns(hasThornedFlourish);
        mock.Setup(x => x.HasPrefulgenceReady).Returns(hasPrefulgenceReady);
        mock.Setup(x => x.HasMagickBarrier).Returns(hasMagickBarrier);

        // Cooldowns
        mock.Setup(x => x.FlecheReady).Returns(flecheReady);
        mock.Setup(x => x.ContreSixteReady).Returns(contreSixteReady);
        mock.Setup(x => x.EmboldenReady).Returns(emboldenReady);
        mock.Setup(x => x.ManaficationReady).Returns(manaficationReady);
        mock.Setup(x => x.CorpsACorpsCharges).Returns(corpsACorpsCharges);
        mock.Setup(x => x.EngagementCharges).Returns(engagementCharges);
        mock.Setup(x => x.AccelerationCharges).Returns(accelerationCharges);
        mock.Setup(x => x.SwiftcastReady).Returns(swiftcastReady);
        mock.Setup(x => x.LucidDreamingReady).Returns(lucidDreamingReady);

        // Coordination / party
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    public static Configuration CreateDefaultRdmConfiguration()
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
