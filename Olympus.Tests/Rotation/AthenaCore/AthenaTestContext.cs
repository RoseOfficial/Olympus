using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Scholar;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AthenaCore;

/// <summary>
/// Factory for creating AthenaContext instances with mocked dependencies.
/// </summary>
public static class AthenaTestContext
{
    /// <summary>
    /// Creates an AthenaContext with fully mocked dependencies.
    /// The partyHelper parameter accepts a TestableAthenaPartyHelper with pre-configured members.
    /// </summary>
    public static AthenaContext Create(
        Configuration? config = null,
        TestableAthenaPartyHelper? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<IDebuffDetectionService>? debuffDetectionService = null,
        Mock<IAetherflowTrackingService>? aetherflowService = null,
        Mock<IFairyGaugeService>? fairyGaugeService = null,
        Mock<IFairyStateManager>? fairyStateManager = null,
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
        int aetherflowStacks = 3,
        int fairyGauge = 50,
        AthenaDebugState? debugState = null)
    {
        config ??= CreateDefaultScholarConfiguration();

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

        debuffDetectionService ??= MockBuilders.CreateMockDebuffDetectionService();
        targetingService ??= MockBuilders.CreateMockTargetingService();

        aetherflowService ??= CreateMockAetherflowService(aetherflowStacks);
        fairyGaugeService ??= CreateMockFairyGaugeService(fairyGauge);
        fairyStateManager ??= CreateMockFairyStateManager();

        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var cooldownPlanner = MockBuilders.CreateMockCooldownPlanner();

        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new AthenaStatusHelper();

        // Default party helper with empty members (no one needs healing)
        partyHelper ??= new TestableAthenaPartyHelper(
            new List<IBattleChara>(), config);

        var healingSpellSelector = new Mock<IHealingSpellSelector>();
        healingSpellSelector.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns(((Olympus.Models.Action.ActionDefinition?)null, 0));
        healingSpellSelector.Setup(x => x.SelectBestAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns(((Olympus.Models.Action.ActionDefinition?)null, 0, (IBattleChara?)null));

        return new AthenaContext(
            player.Object,
            inCombat,
            isMoving: isMoving,
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
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            aetherflowService.Object,
            fairyGaugeService.Object,
            fairyStateManager.Object,
            statusHelper,
            partyHelper,
            cooldownPlanner.Object,
            healingSpellSelector.Object,
            debugState: debugState ?? new AthenaDebugState());
    }

    /// <summary>
    /// Creates an AthenaContext with a real TestableAthenaPartyHelper using the given members.
    /// Use this overload for regression tests that need real party counting logic.
    /// All other dependencies are still mocked.
    /// </summary>
    public static AthenaContext CreateWithRealPartyHelper(
        TestableAthenaPartyHelper realPartyHelper,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        byte level = 100,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        config ??= CreateDefaultScholarConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level,
            currentHp: 50000,
            maxHp: 50000,
            currentMp: 10000);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        var debuffDetectionService = MockBuilders.CreateMockDebuffDetectionService();
        var targetingService = MockBuilders.CreateMockTargetingService();
        var aetherflowService = CreateMockAetherflowService(3);
        var fairyGaugeService = CreateMockFairyGaugeService(50);
        var fairyStateManager = CreateMockFairyStateManager();

        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var cooldownPlanner = MockBuilders.CreateMockCooldownPlanner();

        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new AthenaStatusHelper();

        var healingSpellSelector = new Mock<IHealingSpellSelector>();
        healingSpellSelector.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns(((Olympus.Models.Action.ActionDefinition?)null, 0));
        healingSpellSelector.Setup(x => x.SelectBestAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns(((Olympus.Models.Action.ActionDefinition?)null, 0, (IBattleChara?)null));

        return new AthenaContext(
            player.Object,
            inCombat: true,
            isMoving: false,
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
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            aetherflowService.Object,
            fairyGaugeService.Object,
            fairyStateManager.Object,
            statusHelper,
            realPartyHelper,
            cooldownPlanner.Object,
            healingSpellSelector.Object,
            debugState: new AthenaDebugState());
    }

    /// <summary>
    /// Creates a default Configuration with all Scholar settings enabled.
    /// </summary>
    public static Configuration CreateDefaultScholarConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,
        };

        config.Scholar.EnablePhysick = true;
        config.Scholar.EnableAdloquium = true;
        config.Scholar.EnableSuccor = true;
        config.Scholar.EnableLustrate = true;
        config.Scholar.EnableExcogitation = true;
        config.Scholar.EnableIndomitability = true;
        config.Scholar.EnableProtraction = true;
        config.Scholar.EnableRecitation = true;

        config.Scholar.PhysickThreshold = 0.50f;
        config.Scholar.AdloquiumThreshold = 0.65f;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.ExcogitationThreshold = 0.85f;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AoEHealThreshold = 0.70f;

        config.Scholar.EnableEmergencyTactics = true;
        config.Scholar.EmergencyTacticsThreshold = 0.40f;
        config.Scholar.EnableDeploymentTactics = true;
        config.Scholar.DeploymentMinTargets = 4;

        config.Scholar.AetherflowReserve = 1;
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.Balanced;
        config.Scholar.EnableEnergyDrain = true;
        config.Scholar.EnableAetherflow = true;

        config.Scholar.AutoSummonFairy = true;
        config.Scholar.EnableFairyAbilities = true;
        config.Scholar.WhisperingDawnThreshold = 0.80f;
        config.Scholar.WhisperingDawnMinTargets = 2;
        config.Scholar.FeyBlessingThreshold = 0.70f;
        config.Scholar.FeyUnionThreshold = 0.65f;
        config.Scholar.FeyUnionMinGauge = 30;

        config.Scholar.EnableConsolation = true;
        config.Scholar.SeraphStrategy = SeraphUsageStrategy.OnCooldown;
        config.Scholar.SeraphPartyHpThreshold = 0.70f;
        config.Scholar.SeraphismStrategy = SeraphismUsageStrategy.SaveForDamage;

        config.Scholar.EnableDissipation = false;
        config.Scholar.DissipationMaxFairyGauge = 30;
        config.Scholar.DissipationSafePartyHp = 0.80f;

        config.Scholar.EnableSingleTargetDamage = true;
        config.Scholar.EnableAoEDamage = true;
        config.Scholar.EnableRuinII = true;
        config.Scholar.EnableDot = true;
        config.Scholar.DotRefreshThreshold = 3f;
        config.Scholar.AoEDamageMinTargets = 3;
        config.Scholar.EnableChainStratagem = true;
        config.Scholar.EnableBanefulImpaction = true;

        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;

        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;

        config.Scholar.EnableLucidDreaming = true;
        config.Scholar.LucidDreamingThreshold = 0.70f;

        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        config.RoleActions.EnableEsuna = true;
        config.RoleActions.EsunaPriorityThreshold = 2;

        return config;
    }

    /// <summary>
    /// Creates a mock IAetherflowTrackingService with configurable stacks.
    /// </summary>
    public static Mock<IAetherflowTrackingService> CreateMockAetherflowService(int currentStacks = 3)
    {
        var mock = new Mock<IAetherflowTrackingService>();
        mock.Setup(x => x.CurrentStacks).Returns(currentStacks);
        mock.Setup(x => x.HasStacks).Returns(currentStacks > 0);
        mock.Setup(x => x.IsAtMax).Returns(currentStacks >= 3);
        mock.Setup(x => x.ShouldRefreshAetherflow).Returns(currentStacks == 0);
        mock.Setup(x => x.CanAfford(It.IsAny<int>())).Returns((int cost) => currentStacks >= cost);
        mock.Setup(x => x.ShouldDumpStacks(It.IsAny<float>(), It.IsAny<int>())).Returns(false);
        mock.Setup(x => x.ShouldReserveForHealing(It.IsAny<int>(), It.IsAny<bool>())).Returns(false);
        mock.Setup(x => x.GetCooldownRemaining()).Returns(60f);
        mock.Setup(x => x.ConsumeStack());
        return mock;
    }

    /// <summary>
    /// Creates a mock IFairyGaugeService with configurable gauge.
    /// </summary>
    public static Mock<IFairyGaugeService> CreateMockFairyGaugeService(int currentGauge = 50)
    {
        var mock = new Mock<IFairyGaugeService>();
        mock.Setup(x => x.CurrentGauge).Returns(currentGauge);
        mock.Setup(x => x.HasGauge).Returns(currentGauge > 0);
        mock.Setup(x => x.CanUseFeyUnion).Returns(currentGauge >= 10);
        mock.Setup(x => x.IsNearMax).Returns(currentGauge >= 90);
        mock.Setup(x => x.EstimatedFeyUnionTicks).Returns(currentGauge / 10);
        mock.Setup(x => x.EstimatedFeyUnionDuration).Returns((currentGauge / 10) * 3f);
        mock.Setup(x => x.ShouldUseFeyUnionToPreventOvercap(It.IsAny<int>())).Returns(false);
        return mock;
    }

    /// <summary>
    /// Creates a mock IFairyStateManager defaulting to Eos present.
    /// </summary>
    public static Mock<IFairyStateManager> CreateMockFairyStateManager(
        FairyState state = FairyState.Eos)
    {
        var mock = new Mock<IFairyStateManager>();
        mock.Setup(x => x.CurrentState).Returns(state);
        mock.Setup(x => x.IsFairyAvailable).Returns(
            state is FairyState.Eos or FairyState.Seraph or FairyState.Seraphism);
        mock.Setup(x => x.IsSeraphOrSeraphismActive).Returns(
            state is FairyState.Seraph or FairyState.Seraphism);
        mock.Setup(x => x.IsDissipationActive).Returns(state == FairyState.Dissipated);
        mock.Setup(x => x.NeedsSummon).Returns(state == FairyState.None);
        mock.Setup(x => x.CanUseSeraphAbilities).Returns(
            state is FairyState.Seraph or FairyState.Seraphism);
        mock.Setup(x => x.CanUseEosAbilities).Returns(state == FairyState.Eos);
        mock.Setup(x => x.ShouldAvoidDissipation).Returns(
            state is FairyState.Seraph or FairyState.Seraphism);
        mock.Setup(x => x.GetSpecialStateDuration(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(0f);
        return mock;
    }

    /// <summary>
    /// Creates a party of N healthy members (96% HP) plus M injured members (50% HP).
    /// The real CalculatePartyHealthMetrics counts them using the 0.95 injured threshold.
    /// </summary>
    public static TestableAthenaPartyHelper CreatePartyWithInjured(
        int healthyCount,
        int injuredCount,
        Configuration? config = null)
    {
        var members = new List<IBattleChara>();
        uint id = 1u;

        for (int i = 0; i < healthyCount; i++)
        {
            members.Add(MockBuilders.CreateMockBattleChara(
                entityId: id++, currentHp: 48000, maxHp: 50000).Object); // 96% — not injured
        }

        for (int i = 0; i < injuredCount; i++)
        {
            members.Add(MockBuilders.CreateMockBattleChara(
                entityId: id++, currentHp: 25000, maxHp: 50000).Object); // 50% — injured
        }

        return new TestableAthenaPartyHelper(members, config);
    }
}
