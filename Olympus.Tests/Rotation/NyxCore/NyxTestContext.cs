using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NyxCore;

/// <summary>
/// Factory for creating NyxContext instances with mocked dependencies.
/// </summary>
public static class NyxTestContext
{
    /// <summary>
    /// Creates a NyxContext with fully mocked dependencies.
    /// </summary>
    public static NyxContext Create(
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
        int bloodGauge = 0,
        float darksideTimer = 0f,
        int comboStep = 0,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        NyxDebugState? debugState = null)
    {
        config ??= CreateDefaultDarkKnightConfiguration();

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
        var statusHelper = new NyxStatusHelper();

        var enmityService = new Mock<IEnmityService>();
        enmityService.Setup(x => x.IsMainTankOn(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(false);
        enmityService.Setup(x => x.IsLosingAggro(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        enmityService.Setup(x => x.GetEnmityPosition(It.IsAny<Dalamud.Game.ClientState.Objects.Types.IBattleChara>(), It.IsAny<uint>())).Returns(1);

        var tankCooldownService = new Mock<ITankCooldownService>();
        tankCooldownService.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(false);
        tankCooldownService.Setup(x => x.ShouldUseShortCooldown(It.IsAny<float>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        var partyHelper = new NyxPartyHelper(objectTable.Object, partyList.Object);

        return new NyxContext(
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
            debugState ?? new NyxDebugState(),
            bloodGauge,
            darksideTimer,
            comboStep,
            lastComboAction,
            comboTimeRemaining);
    }

    /// <summary>
    /// Creates a default Configuration with all Dark Knight settings enabled.
    /// </summary>
    public static Configuration CreateDefaultDarkKnightConfiguration()
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
