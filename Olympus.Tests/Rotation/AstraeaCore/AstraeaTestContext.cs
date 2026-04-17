using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Astrologian;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AstraeaCore;

/// <summary>
/// Factory for creating AstraeaContext instances with mocked dependencies.
/// </summary>
public static class AstraeaTestContext
{
    /// <summary>
    /// Creates an AstraeaContext with fully mocked dependencies.
    /// The partyHelper parameter accepts a TestableAstraeaPartyHelper with pre-configured members.
    /// </summary>
    public static AstraeaContext Create(
        Configuration? config = null,
        TestableAstraeaPartyHelper? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<IDebuffDetectionService>? debuffDetectionService = null,
        Mock<ICardTrackingService>? cardService = null,
        Mock<IEarthlyStarService>? earthlyStarService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        uint maxMp = 10000,
        bool inCombat = true,
        bool isMoving = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        bool hasCard = false,
        int sealCount = 0,
        int uniqueSealCount = 0,
        AstraeaDebugState? debugState = null)
    {
        config ??= CreateDefaultAstrologianConfiguration();

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

        cardService ??= CreateMockCardService(
            hasCard: hasCard,
            sealCount: sealCount,
            uniqueSealCount: uniqueSealCount);
        earthlyStarService ??= CreateMockEarthlyStarService();

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
        var statusHelper = new AstraeaStatusHelper();

        // Default party helper with empty members (no one needs healing)
        partyHelper ??= new TestableAstraeaPartyHelper(
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

        return new AstraeaContext(
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
            cardService.Object,
            earthlyStarService.Object,
            statusHelper,
            partyHelper,
            cooldownPlanner.Object,
            healingSpellSelector.Object,
            debugState: debugState ?? new AstraeaDebugState());
    }

    /// <summary>
    /// Creates an AstraeaContext with a real TestableAstraeaPartyHelper using the given members.
    /// Use this overload for regression tests that need real party counting logic.
    /// All other dependencies are still mocked.
    /// </summary>
    public static AstraeaContext CreateWithRealPartyHelper(
        TestableAstraeaPartyHelper realPartyHelper,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        byte level = 100,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        config ??= CreateDefaultAstrologianConfiguration();

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
        var cardService = CreateMockCardService();
        var earthlyStarService = CreateMockEarthlyStarService();

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
        var statusHelper = new AstraeaStatusHelper();

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

        return new AstraeaContext(
            player.Object,
            inCombat: false,
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
            cardService.Object,
            earthlyStarService.Object,
            statusHelper,
            realPartyHelper,
            cooldownPlanner.Object,
            healingSpellSelector.Object,
            debugState: new AstraeaDebugState());
    }

    /// <summary>
    /// Creates a default Configuration with all Astrologian settings enabled.
    /// </summary>
    public static Configuration CreateDefaultAstrologianConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,
        };

        config.Astrologian.EnableBenefic = true;
        config.Astrologian.EnableBeneficII = true;
        config.Astrologian.EnableAspectedBenefic = true;
        config.Astrologian.EnableHelios = true;
        config.Astrologian.EnableAspectedHelios = true;
        config.Astrologian.EnableEssentialDignity = true;
        config.Astrologian.EnableCelestialIntersection = true;
        config.Astrologian.EnableCelestialOpposition = true;
        config.Astrologian.EnableExaltation = true;
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f;
        config.Astrologian.BeneficThreshold = 0.50f;
        config.Astrologian.BeneficIIThreshold = 0.60f;
        config.Astrologian.AspectedBeneficThreshold = 0.75f;
        config.Astrologian.EssentialDignityThreshold = 0.40f;
        config.Astrologian.CelestialIntersectionThreshold = 0.70f;
        config.Astrologian.ExaltationThreshold = 0.75f;

        config.Astrologian.EnableCards = true;
        config.Astrologian.EnableDivination = true;
        config.Astrologian.EnableAstrodyne = true;
        config.Astrologian.AstrodyneMinSeals = 2;
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;
        config.Astrologian.EnableOracle = true;

        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.SaveForDamage;
        config.Astrologian.NeutralSectThreshold = 0.65f;
        config.Astrologian.EnableSunSign = true;
        config.Astrologian.EnableCollectiveUnconscious = false;

        config.Astrologian.EnableLightspeed = true;
        config.Astrologian.LightspeedStrategy = LightspeedUsageStrategy.OnCooldown;
        config.HealerShared.EnableLucidDreaming = true;
        config.HealerShared.LucidDreamingThreshold = 0.70f;

        config.Astrologian.EnableSingleTargetDamage = true;
        config.Astrologian.EnableAoEDamage = true;
        config.Astrologian.EnableDot = true;
        config.Astrologian.DotRefreshThreshold = 3f;
        config.Astrologian.AoEDamageMinTargets = 3;
        config.Astrologian.EnableEarthlyStar = true;

        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        config.RoleActions.EnableEsuna = true;
        config.RoleActions.EsunaPriorityThreshold = 2;

        return config;
    }

    /// <summary>
    /// Creates a mock ICardTrackingService with configurable card state.
    /// </summary>
    public static Mock<ICardTrackingService> CreateMockCardService(
        bool hasCard = false,
        int sealCount = 0,
        int uniqueSealCount = 0,
        bool hasLord = false,
        bool hasLady = false,
        ASTActions.CardType currentCard = ASTActions.CardType.None,
        ASTActions.CardType minorArcana = ASTActions.CardType.None)
    {
        var mock = new Mock<ICardTrackingService>();
        mock.Setup(x => x.HasCard).Returns(hasCard);
        mock.Setup(x => x.SealCount).Returns(sealCount);
        mock.Setup(x => x.UniqueSealCount).Returns(uniqueSealCount);
        mock.Setup(x => x.HasBalance).Returns(hasCard && currentCard == ASTActions.CardType.TheBalance);
        mock.Setup(x => x.HasSpear).Returns(hasCard && currentCard == ASTActions.CardType.TheSpear);
        mock.Setup(x => x.HasLord).Returns(hasLord);
        mock.Setup(x => x.HasLady).Returns(hasLady);
        mock.Setup(x => x.HasMinorArcana).Returns(hasLord || hasLady);
        mock.Setup(x => x.CurrentCard).Returns(currentCard);
        mock.Setup(x => x.MinorArcanaCard).Returns(minorArcana);
        mock.Setup(x => x.CanUseAstrodyne).Returns(sealCount >= 3);
        mock.Setup(x => x.HasDiviningStatus).Returns(false);
        mock.Setup(x => x.IsMeleeCard).Returns(false);
        mock.Setup(x => x.IsRangedCard).Returns(false);
        mock.Setup(x => x.BalanceCount).Returns(0);
        mock.Setup(x => x.SpearCount).Returns(0);
        mock.Setup(x => x.TotalCardsInHand).Returns(hasCard ? 1 : 0);
        mock.Setup(x => x.RawCardTypes).Returns("");
        mock.Setup(x => x.HasLunarSeal).Returns(false);
        mock.Setup(x => x.HasSolarSeal).Returns(false);
        mock.Setup(x => x.HasCelestialSeal).Returns(false);
        return mock;
    }

    /// <summary>
    /// Creates a mock IEarthlyStarService with configurable star state.
    /// </summary>
    public static Mock<IEarthlyStarService> CreateMockEarthlyStarService(
        bool isStarPlaced = false,
        bool isStarMature = false,
        float timeRemaining = 0f,
        float timeUntilMature = 0f)
    {
        var mock = new Mock<IEarthlyStarService>();
        mock.Setup(x => x.IsStarPlaced).Returns(isStarPlaced);
        mock.Setup(x => x.IsStarMature).Returns(isStarMature);
        mock.Setup(x => x.TimeRemaining).Returns(timeRemaining);
        mock.Setup(x => x.TimeUntilMature).Returns(timeUntilMature);
        mock.Setup(x => x.StarPosition).Returns((System.Numerics.Vector3?)null);
        return mock;
    }

    /// <summary>
    /// Creates a party of N healthy members (96% HP) plus M injured members (50% HP).
    /// The real CalculatePartyHealthMetrics counts them using the 0.95 injured threshold.
    /// </summary>
    public static TestableAstraeaPartyHelper CreatePartyWithInjured(
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

        return new TestableAstraeaPartyHelper(members, config);
    }
}
