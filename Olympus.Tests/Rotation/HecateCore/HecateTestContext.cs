using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HecateCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.HecateCore;

/// <summary>
/// Factory for creating IHecateContext mocks for use in Hecate module tests.
/// </summary>
public static class HecateTestContext
{
    /// <summary>
    /// Creates an IHecateContext mock with configurable state for module tests.
    /// </summary>
    public static IHecateContext Create(
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
        // Element state
        bool inAstralFire = false,
        bool inUmbralIce = false,
        int elementStacks = 0,
        float elementTimer = 0f,
        bool isEnochianActive = false,
        int astralFireStacks = 0,
        int umbralIceStacks = 0,
        // Resources
        int umbralHearts = 0,
        int polyglotStacks = 0,
        int astralSoulStacks = 0,
        bool hasParadox = false,
        // Buffs
        bool hasFirestarter = false,
        float firestarterRemaining = 0f,
        bool hasThunderhead = false,
        float thunderheadRemaining = 0f,
        bool hasLeyLines = false,
        float leyLinesRemaining = 0f,
        // Target
        bool hasThunderDoT = false,
        float thunderDoTRemaining = 0f,
        // Movement
        bool needsInstant = false,
        // Cooldowns
        int triplecastCharges = 0,
        bool swiftcastReady = false,
        bool manafontReady = false,
        bool amplifierReady = false,
        bool leyLinesReady = false,
        HecateDebugState? debugState = null)
    {
        config ??= CreateDefaultBlmConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentMp: (uint)currentMp, maxMp: (uint)maxMp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new HecateStatusHelper();
        var partyHelper = new CasterPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new HecateDebugState();

        var mock = new Mock<IHecateContext>();

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

        // Element state
        mock.Setup(x => x.InAstralFire).Returns(inAstralFire);
        mock.Setup(x => x.InUmbralIce).Returns(inUmbralIce);
        mock.Setup(x => x.ElementStacks).Returns(elementStacks);
        mock.Setup(x => x.ElementTimer).Returns(elementTimer);
        mock.Setup(x => x.IsEnochianActive).Returns(isEnochianActive);
        mock.Setup(x => x.AstralFireStacks).Returns(astralFireStacks);
        mock.Setup(x => x.UmbralIceStacks).Returns(umbralIceStacks);

        // Resources
        mock.Setup(x => x.UmbralHearts).Returns(umbralHearts);
        mock.Setup(x => x.PolyglotStacks).Returns(polyglotStacks);
        mock.Setup(x => x.AstralSoulStacks).Returns(astralSoulStacks);
        mock.Setup(x => x.HasParadox).Returns(hasParadox);

        // Buffs
        mock.Setup(x => x.HasFirestarter).Returns(hasFirestarter);
        mock.Setup(x => x.FirestarterRemaining).Returns(firestarterRemaining);
        mock.Setup(x => x.HasThunderhead).Returns(hasThunderhead);
        mock.Setup(x => x.ThunderheadRemaining).Returns(thunderheadRemaining);
        mock.Setup(x => x.HasLeyLines).Returns(hasLeyLines);
        mock.Setup(x => x.LeyLinesRemaining).Returns(leyLinesRemaining);

        // Target state
        mock.Setup(x => x.HasThunderDoT).Returns(hasThunderDoT);
        mock.Setup(x => x.ThunderDoTRemaining).Returns(thunderDoTRemaining);

        // Movement
        mock.Setup(x => x.NeedsInstant).Returns(needsInstant);

        // Cooldowns
        mock.Setup(x => x.TriplecastCharges).Returns(triplecastCharges);
        mock.Setup(x => x.SwiftcastReady).Returns(swiftcastReady);
        mock.Setup(x => x.ManafontReady).Returns(manafontReady);
        mock.Setup(x => x.AmplifierReady).Returns(amplifierReady);
        mock.Setup(x => x.LeyLinesReady).Returns(leyLinesReady);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    public static Configuration CreateDefaultBlmConfiguration()
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
