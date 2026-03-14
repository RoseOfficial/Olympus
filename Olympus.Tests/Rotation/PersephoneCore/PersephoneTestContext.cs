using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PersephoneCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.PersephoneCore;

/// <summary>
/// Factory for creating IPersephoneContext mocks for use in Persephone module tests.
/// </summary>
public static class PersephoneTestContext
{
    /// <summary>
    /// Creates an IPersephoneContext mock with configurable state for module tests.
    /// </summary>
    public static IPersephoneContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
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
        // Demi-summon state
        bool isBahamutActive = false,
        bool isPhoenixActive = false,
        bool isSolarBahamutActive = false,
        bool isDemiSummonActive = false,
        float demiSummonTimer = 0f,
        int demiSummonGcdsRemaining = 0,
        // Primal attunement
        int currentAttunement = 0,
        int attunementStacks = 0,
        float attunementTimer = 0f,
        bool isIfritAttuned = false,
        bool isTitanAttuned = false,
        bool isGarudaAttuned = false,
        // Primal availability
        bool canSummonIfrit = false,
        bool canSummonTitan = false,
        bool canSummonGaruda = false,
        int primalsAvailable = 0,
        // Aetherflow
        int aetherflowStacks = 0,
        bool hasAetherflow = false,
        // Buffs
        bool hasFurtherRuin = false,
        float furtherRuinRemaining = 0f,
        bool hasSearingLight = false,
        float searingLightRemaining = 0f,
        bool hasIfritsFavor = false,
        bool hasTitansFavor = false,
        bool hasGarudasFavor = false,
        bool hasRadiantAegis = false,
        // Cooldowns
        bool searingLightReady = false,
        bool energyDrainReady = false,
        bool enkindleReady = false,
        bool astralFlowReady = false,
        int radiantAegisCharges = 0,
        bool swiftcastReady = false,
        bool lucidDreamingReady = false,
        // Phase tracking
        bool hasUsedEnkindleThisPhase = false,
        bool hasUsedAstralFlowThisPhase = false,
        bool hasPetSummoned = false,
        PersephoneDebugState? debugState = null)
    {
        config ??= CreateDefaultSmnConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentMp: (uint)currentMp, maxMp: (uint)maxMp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new PersephoneStatusHelper();
        var partyHelper = new PersephonePartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new PersephoneDebugState();

        var mock = new Mock<IPersephoneContext>();

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
        mock.Setup(x => x.TimelineService).Returns((ITimelineService?)null);
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

        // Demi-summon state
        mock.Setup(x => x.IsBahamutActive).Returns(isBahamutActive);
        mock.Setup(x => x.IsPhoenixActive).Returns(isPhoenixActive);
        mock.Setup(x => x.IsSolarBahamutActive).Returns(isSolarBahamutActive);
        mock.Setup(x => x.IsDemiSummonActive).Returns(isDemiSummonActive);
        mock.Setup(x => x.DemiSummonTimer).Returns(demiSummonTimer);
        mock.Setup(x => x.DemiSummonGcdsRemaining).Returns(demiSummonGcdsRemaining);

        // Primal attunement
        mock.Setup(x => x.CurrentAttunement).Returns(currentAttunement);
        mock.Setup(x => x.AttunementStacks).Returns(attunementStacks);
        mock.Setup(x => x.AttunementTimer).Returns(attunementTimer);
        mock.Setup(x => x.IsIfritAttuned).Returns(isIfritAttuned);
        mock.Setup(x => x.IsTitanAttuned).Returns(isTitanAttuned);
        mock.Setup(x => x.IsGarudaAttuned).Returns(isGarudaAttuned);

        // Primal availability
        mock.Setup(x => x.CanSummonIfrit).Returns(canSummonIfrit);
        mock.Setup(x => x.CanSummonTitan).Returns(canSummonTitan);
        mock.Setup(x => x.CanSummonGaruda).Returns(canSummonGaruda);
        mock.Setup(x => x.PrimalsAvailable).Returns(primalsAvailable);

        // Aetherflow
        mock.Setup(x => x.AetherflowStacks).Returns(aetherflowStacks);
        mock.Setup(x => x.HasAetherflow).Returns(hasAetherflow);

        // Buffs
        mock.Setup(x => x.HasFurtherRuin).Returns(hasFurtherRuin);
        mock.Setup(x => x.FurtherRuinRemaining).Returns(furtherRuinRemaining);
        mock.Setup(x => x.HasSearingLight).Returns(hasSearingLight);
        mock.Setup(x => x.SearingLightRemaining).Returns(searingLightRemaining);
        mock.Setup(x => x.HasIfritsFavor).Returns(hasIfritsFavor);
        mock.Setup(x => x.HasTitansFavor).Returns(hasTitansFavor);
        mock.Setup(x => x.HasGarudasFavor).Returns(hasGarudasFavor);
        mock.Setup(x => x.HasRadiantAegis).Returns(hasRadiantAegis);

        // Cooldowns
        mock.Setup(x => x.SearingLightReady).Returns(searingLightReady);
        mock.Setup(x => x.EnergyDrainReady).Returns(energyDrainReady);
        mock.Setup(x => x.EnkindleReady).Returns(enkindleReady);
        mock.Setup(x => x.AstralFlowReady).Returns(astralFlowReady);
        mock.Setup(x => x.RadiantAegisCharges).Returns(radiantAegisCharges);
        mock.Setup(x => x.SwiftcastReady).Returns(swiftcastReady);
        mock.Setup(x => x.LucidDreamingReady).Returns(lucidDreamingReady);

        // Phase tracking
        mock.Setup(x => x.HasUsedEnkindleThisPhase).Returns(hasUsedEnkindleThisPhase);
        mock.Setup(x => x.HasUsedAstralFlowThisPhase).Returns(hasUsedAstralFlowThisPhase);
        mock.Setup(x => x.HasPetSummoned).Returns(hasPetSummoned);

        // Coordination / party
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    public static Configuration CreateDefaultSmnConfiguration()
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
