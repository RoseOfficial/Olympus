using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HephaestusCore;

/// <summary>
/// Factory for creating HephaestusContext instances with mocked dependencies.
/// </summary>
public static class HephaestusTestContext
{
    /// <summary>
    /// Creates a HephaestusContext with fully mocked dependencies.
    /// </summary>
    public static HephaestusContext Create(
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
        int cartridges = 0,
        int gnashingFangStep = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        HephaestusDebugState? debugState = null)
    {
        config ??= CreateDefaultGunbreakerConfiguration();

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
        var statusHelper = new HephaestusStatusHelper();

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsMainTankOn(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(false);
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        var partyHelper = new HephaestusPartyHelper(objectTable.Object, partyList.Object);

        return new HephaestusContext(
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
            debugState ?? new HephaestusDebugState(),
            cartridges,
            gnashingFangStep,
            comboStep,
            lastComboAction,
            comboTimeRemaining);
    }

    /// <summary>
    /// Creates a default Configuration with all Gunbreaker settings enabled.
    /// </summary>
    public static Configuration CreateDefaultGunbreakerConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = false,
            EnableDamage = true,
            EnableDoT = true,
        };

        config.Tank.EnableMitigation = true;
        config.Tank.EnableDamage = true;
        config.Tank.MitigationThreshold = 0.80f;
        config.Tank.UseRampartOnCooldown = false;
        config.Tank.AutoProvoke = true;
        config.Tank.AutoShirk = false;
        config.Tank.IsMainTankOverride = true;
        config.Tank.EnableDefensiveCoordination = false;
        config.Tank.EnableInvulnerabilityCoordination = false;

        return config;
    }
}
