using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.AresCore;

/// <summary>
/// Factory for creating AresContext instances with mocked dependencies.
/// </summary>
public static class AresTestContext
{
    /// <summary>
    /// Creates an AresContext with fully mocked dependencies.
    /// </summary>
    public static AresContext Create(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 100,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        uint maxMp = 10000,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        int beastGauge = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool hasDefiance = false,
        AresDebugState? debugState = null)
    {
        config ??= CreateDefaultWarriorConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level,
            currentHp: currentHp,
            maxHp: maxHp,
            currentMp: currentMp,
            maxMp: maxMp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        targetingService ??= MockBuilders.CreateMockTargetingService();

        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var debuffDetectionService = MockBuilders.CreateMockDebuffDetectionService();

        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new AresStatusHelper();

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsMainTankOn(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(false);
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        var partyHelper = new AresPartyHelper(objectTable.Object, partyList.Object);

        return new AresContext(
            player.Object,
            inCombat,
            isMoving,
            canExecuteGcd,
            canExecuteOgcd,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            damageIntakeService.Object,
            damageTrendService.Object,
            frameCache.Object,
            config,
            debuffDetectionService.Object,
            hpPredictionService.Object,
            mpForecastService.Object,
            playerStatsService.Object,
            targetingService.Object,
            objectTable.Object,
            partyList.Object,
            enmityService.Object,
            tankCooldownService.Object,
            statusHelper,
            partyHelper,
            debugState ?? new AresDebugState(),
            beastGauge,
            comboStep,
            lastComboAction,
            comboTimeRemaining);
    }

    /// <summary>
    /// Creates a Mock&lt;IAresContext&gt; with configurable WAR-specific state.
    /// Use this overload when tests need non-default WAR state (e.g., HasInnerRelease,
    /// HasNascentChaos) that cannot be set on the concrete AresContext via constructor.
    /// </summary>
    public static IAresContext CreateMock(
        bool inCombat = true,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        byte level = 100,
        uint currentHp = 50000,
        uint maxHp = 50000,
        int comboStep = 0,
        uint lastComboAction = 0,
        int beastGauge = 0,
        bool hasInnerRelease = false,
        int innerReleaseStacks = 0,
        bool hasNascentChaos = false,
        bool hasPrimalRendReady = false,
        bool hasPrimalRuinationReady = false,
        bool hasWrathful = false,
        bool hasSurgingTempest = true,
        float surgingTempestRemaining = 30f,
        bool hasHolmgang = false,
        bool hasVengeance = false,
        bool hasBloodwhetting = false,
        bool hasActiveMitigation = false,
        (float, float, int) partyHealthMetrics = default,
        int enemyCount = 1,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<ITankCooldownService>? tankCooldownService = null,
        bool tankCooldownShouldUseMitigation = false,
        bool tankCooldownShouldUseMajor = false)
    {
        config ??= CreateDefaultWarriorConfiguration();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteGcd: canExecuteGcd, canExecuteOgcd: canExecuteOgcd);
        targetingService ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level, currentHp: currentHp, maxHp: maxHp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IAresContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns((ITimelineService?)null);
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);

        // WAR gauge / combo
        mock.Setup(x => x.BeastGauge).Returns(beastGauge);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);

        // WAR damage-buff state
        mock.Setup(x => x.HasInnerRelease).Returns(hasInnerRelease);
        mock.Setup(x => x.InnerReleaseStacks).Returns(innerReleaseStacks);
        mock.Setup(x => x.HasNascentChaos).Returns(hasNascentChaos);
        mock.Setup(x => x.HasPrimalRendReady).Returns(hasPrimalRendReady);
        mock.Setup(x => x.HasPrimalRuinationReady).Returns(hasPrimalRuinationReady);
        mock.Setup(x => x.HasWrathful).Returns(hasWrathful);
        mock.Setup(x => x.HasSurgingTempest).Returns(hasSurgingTempest);
        mock.Setup(x => x.SurgingTempestRemaining).Returns(surgingTempestRemaining);

        // WAR defensive state
        mock.Setup(x => x.HasHolmgang).Returns(hasHolmgang);
        mock.Setup(x => x.HasVengeance).Returns(hasVengeance);
        mock.Setup(x => x.HasBloodwhetting).Returns(hasBloodwhetting);
        mock.Setup(x => x.HasActiveMitigation).Returns(hasActiveMitigation);

        // Party health
        var metrics = partyHealthMetrics == default ? (1.0f, 1.0f, 0) : partyHealthMetrics;
        mock.Setup(x => x.PartyHealthMetrics).Returns(metrics);

        // Tank services
        if (tankCooldownService == null)
        {
            tankCooldownService = new Mock<ITankCooldownService>();
            tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(tankCooldownShouldUseMitigation);
            tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(tankCooldownShouldUseMajor);
            tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        }
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);

        var statusHelper = new AresStatusHelper();
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);

        var partyHelper = new AresPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);

        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemyCount);

        var debugState = new AresDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    /// <summary>
    /// Creates a default Configuration with all Warrior settings enabled.
    /// </summary>
    public static Configuration CreateDefaultWarriorConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = false,
            EnableDamage = true,
            EnableDoT = false,
        };

        config.Tank.EnableMitigation = true;
        config.Tank.EnableDamage = true;
        config.Tank.MitigationThreshold = 0.80f;
        config.Tank.UseRampartOnCooldown = false;
        config.Tank.AutoProvoke = true;
        config.Tank.AutoShirk = false;
        config.Tank.AutoPrimalRend = true; // Tests exercise rotation behavior; player-agency gate off for coverage
        config.Tank.IsMainTankOverride = true; // Force main tank in tests
        config.Tank.EnableDefensiveCoordination = false;
        config.Tank.EnableInvulnerabilityCoordination = false;

        return config;
    }
}
