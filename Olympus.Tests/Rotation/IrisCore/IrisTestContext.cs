using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Rotation.IrisCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;
using static Olympus.Data.PCTActions;

namespace Olympus.Tests.Rotation.IrisCore;

/// <summary>
/// Factory for creating IIrisContext mocks for use in Iris module tests.
/// </summary>
public static class IrisTestContext
{
    /// <summary>
    /// Creates an IIrisContext mock with configurable state for module tests.
    /// </summary>
    public static IIrisContext Create(
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
        // Palette gauge
        int paletteGauge = 0,
        bool canUseSubtractivePalette = false,
        // Paint stacks
        int whitePaint = 0,
        bool hasWhitePaint = false,
        bool hasBlackPaint = false,
        // Canvas state
        bool hasCreatureCanvas = false,
        CreatureMotifType creatureMotifType = CreatureMotifType.None,
        bool hasWeaponCanvas = false,
        bool hasLandscapeCanvas = false,
        // Muse state
        int livingMuseCharges = 0,
        bool livingMuseReady = false,
        bool strikingMuseReady = false,
        bool starryMuseReady = false,
        // Portrait state
        bool mogReady = false,
        bool madeenReady = false,
        // Hammer combo
        bool isInHammerCombo = false,
        int hammerComboStep = 0,
        int hammerTimeStacks = 0,
        // Base combo state
        int baseComboStep = 0,
        bool isInSubtractiveCombo = false,
        // Buffs
        bool hasStarryMuse = false,
        float starryMuseRemaining = 0f,
        bool hasHyperphantasia = false,
        int hyperphantasiaStacks = 0,
        bool hasInspiration = false,
        bool hasSubtractiveSpectrum = false,
        bool hasStarstruck = false,
        bool hasRainbowBright = false,
        bool hasHammerTime = false,
        bool hasSubtractivePalette = false,
        float subtractivePaletteRemaining = 0f,
        bool hasMonochromeTones = false,
        // Cooldowns
        bool subtractivePaletteReady = false,
        bool temperaCoatReady = false,
        bool temperaGrassaReady = false,
        bool smudgeReady = false,
        bool swiftcastReady = false,
        bool lucidDreamingReady = false,
        // Utility
        bool shouldUseAoe = false,
        int nearbyEnemyCount = 1,
        bool isInBurstWindow = false,
        bool canPaintMotif = false,
        bool needsCreatureMotif = false,
        bool needsWeaponMotif = false,
        bool needsLandscapeMotif = false,
        (float avgHpPercent, float lowestHpPercent, int injuredCount) partyHealthMetrics = default,
        uint currentHp = 50000,
        uint maxHp = 50000,
        IrisDebugState? debugState = null)
    {
        config ??= CreateDefaultPctConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentHp: currentHp, maxHp: maxHp, currentMp: (uint)currentMp, maxMp: (uint)maxMp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var statusHelper = new IrisStatusHelper();
        var partyHelper = new CasterPartyHelper(objectTable.Object, partyList.Object);
        var debug = debugState ?? new IrisDebugState();

        var mock = new Mock<IIrisContext>();

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

        // Palette gauge
        mock.Setup(x => x.PaletteGauge).Returns(paletteGauge);
        mock.Setup(x => x.CanUseSubtractivePalette).Returns(canUseSubtractivePalette);

        // Paint stacks
        mock.Setup(x => x.WhitePaint).Returns(whitePaint);
        mock.Setup(x => x.HasWhitePaint).Returns(hasWhitePaint);
        mock.Setup(x => x.HasBlackPaint).Returns(hasBlackPaint);

        // Canvas state
        mock.Setup(x => x.HasCreatureCanvas).Returns(hasCreatureCanvas);
        mock.Setup(x => x.CreatureMotifType).Returns(creatureMotifType);
        mock.Setup(x => x.HasWeaponCanvas).Returns(hasWeaponCanvas);
        mock.Setup(x => x.HasLandscapeCanvas).Returns(hasLandscapeCanvas);

        // Muse state
        mock.Setup(x => x.LivingMuseCharges).Returns(livingMuseCharges);
        mock.Setup(x => x.LivingMuseReady).Returns(livingMuseReady);
        mock.Setup(x => x.StrikingMuseReady).Returns(strikingMuseReady);
        mock.Setup(x => x.StarryMuseReady).Returns(starryMuseReady);

        // Portrait state
        mock.Setup(x => x.MogReady).Returns(mogReady);
        mock.Setup(x => x.MadeenReady).Returns(madeenReady);

        // Hammer combo
        mock.Setup(x => x.IsInHammerCombo).Returns(isInHammerCombo);
        mock.Setup(x => x.HammerComboStep).Returns(hammerComboStep);
        mock.Setup(x => x.HammerTimeStacks).Returns(hammerTimeStacks);

        // Base combo state
        mock.Setup(x => x.BaseComboStep).Returns(baseComboStep);
        mock.Setup(x => x.IsInSubtractiveCombo).Returns(isInSubtractiveCombo);

        // Buffs
        mock.Setup(x => x.HasStarryMuse).Returns(hasStarryMuse);
        mock.Setup(x => x.StarryMuseRemaining).Returns(starryMuseRemaining);
        mock.Setup(x => x.HasHyperphantasia).Returns(hasHyperphantasia);
        mock.Setup(x => x.HyperphantasiaStacks).Returns(hyperphantasiaStacks);
        mock.Setup(x => x.HasInspiration).Returns(hasInspiration);
        mock.Setup(x => x.HasSubtractiveSpectrum).Returns(hasSubtractiveSpectrum);
        mock.Setup(x => x.HasStarstruck).Returns(hasStarstruck);
        mock.Setup(x => x.HasRainbowBright).Returns(hasRainbowBright);
        mock.Setup(x => x.HasHammerTime).Returns(hasHammerTime);
        mock.Setup(x => x.HasSubtractivePalette).Returns(hasSubtractivePalette);
        mock.Setup(x => x.SubtractivePaletteRemaining).Returns(subtractivePaletteRemaining);
        mock.Setup(x => x.HasMonochromeTones).Returns(hasMonochromeTones);

        // Cooldowns
        mock.Setup(x => x.SubtractivePaletteReady).Returns(subtractivePaletteReady);
        mock.Setup(x => x.TemperaCoatReady).Returns(temperaCoatReady);
        mock.Setup(x => x.TemperaGrassaReady).Returns(temperaGrassaReady);
        mock.Setup(x => x.SmudgeReady).Returns(smudgeReady);
        mock.Setup(x => x.SwiftcastReady).Returns(swiftcastReady);
        mock.Setup(x => x.LucidDreamingReady).Returns(lucidDreamingReady);

        // Utility
        mock.Setup(x => x.ShouldUseAoe).Returns(shouldUseAoe);
        mock.Setup(x => x.NearbyEnemyCount).Returns(nearbyEnemyCount);
        mock.Setup(x => x.IsInBurstWindow).Returns(isInBurstWindow);
        mock.Setup(x => x.CanPaintMotif).Returns(canPaintMotif);
        mock.Setup(x => x.NeedsCreatureMotif).Returns(needsCreatureMotif);
        mock.Setup(x => x.NeedsWeaponMotif).Returns(needsWeaponMotif);
        mock.Setup(x => x.NeedsLandscapeMotif).Returns(needsLandscapeMotif);

        // Party health metrics (default: all healthy)
        var healthMetrics = partyHealthMetrics == default
            ? (1.0f, 1.0f, 0)
            : partyHealthMetrics;
        mock.Setup(x => x.PartyHealthMetrics).Returns(healthMetrics);

        // Coordination / party
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        mock.Setup(x => x.Debug).Returns(debug);

        return mock.Object;
    }

    public static Configuration CreateDefaultPctConfiguration()
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
