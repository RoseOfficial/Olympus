using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Sage;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AsclepiusCore;

/// <summary>
/// Factory for creating AsclepiusContext instances with mocked dependencies.
/// Mirrors the ApolloContext pattern used in ApolloCore tests.
/// </summary>
public static class AsclepiusTestContext
{
    /// <summary>
    /// Creates an AsclepiusContext with fully mocked dependencies.
    /// </summary>
    public static IAsclepiusContext Create(
        Configuration? config = null,
        Mock<IPartyHelper>? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<IDebuffDetectionService>? debuffDetectionService = null,
        Mock<IAddersgallTrackingService>? addersgallService = null,
        Mock<IAdderstingTrackingService>? adderstingService = null,
        Mock<IKardiaManager>? kardiaManager = null,
        Mock<IEukrasiaStateService>? eukrasiaService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool inCombat = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false,
        int addersgallStacks = 3,
        float addersgallTimer = 0f,
        int adderstingStacks = 0,
        bool hasEukrasia = false,
        bool hasZoe = false,
        bool hasKardiaPlaced = false,
        ulong kardiaTargetId = 0,
        bool canSwapKardia = true,
        bool hasSoteria = false,
        bool hasPhilosophia = false,
        bool hasSwiftcast = false,
        AsclepiusDebugState? debugState = null)
    {
        config ??= CreateDefaultSageConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level,
            currentHp: currentHp,
            maxHp: maxHp,
            currentMp: currentMp);

        // Set up swiftcast status on player if requested
        if (hasSwiftcast)
        {
            player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        }

        actionService ??= MockBuilders.CreateMockActionService(
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);

        partyHelper ??= MockBuilders.CreateMockPartyHelper();
        debuffDetectionService ??= MockBuilders.CreateMockDebuffDetectionService();
        targetingService ??= MockBuilders.CreateMockTargetingService();

        // SGE-specific services
        addersgallService ??= CreateMockAddersgallService(addersgallStacks, addersgallTimer);
        adderstingService ??= CreateMockAdderstingService(adderstingStacks);
        kardiaManager ??= CreateMockKardiaManager(
            hasKardia: hasKardiaPlaced,
            kardiaTargetId: kardiaTargetId,
            canSwap: canSwapKardia);
        eukrasiaService ??= CreateMockEukrasiaService();

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
        var statusHelper = new AsclepiusStatusHelper();

        // Create a minimal IHealingSpellSelector mock
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

        return new AsclepiusContext(
            player.Object,
            inCombat,
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
            healingSpellSelector.Object,
            cooldownPlanner.Object,
            addersgallService.Object,
            adderstingService.Object,
            kardiaManager.Object,
            eukrasiaService.Object,
            statusHelper,
            partyHelper.Object,
            debugState: debugState ?? new AsclepiusDebugState());
    }

    /// <summary>
    /// Creates a default Configuration with Sage settings enabled.
    /// </summary>
    public static Configuration CreateDefaultSageConfiguration()
    {
        var config = new Configuration
        {
            Enabled = true,
            EnableHealing = true,
            EnableDamage = true,
            EnableDoT = true,
        };

        // Sage config - enable all by default
        config.Sage.AutoKardia = true;
        config.Sage.KardiaSwapEnabled = true;
        config.Sage.EnableSoteria = true;
        config.Sage.EnablePhilosophia = true;
        config.Sage.EnableDruochole = true;
        config.Sage.EnableTaurochole = true;
        config.Sage.EnableIxochole = true;
        config.Sage.EnableKerachole = true;
        config.Sage.EnablePhysisII = true;
        config.Sage.EnableHolos = true;
        config.Sage.EnablePepsis = true;
        config.Sage.EnablePneuma = true;
        config.Sage.EnableHaima = true;
        config.Sage.EnablePanhaima = true;
        config.Sage.EnableZoe = true;
        config.Sage.EnableKrasis = true;
        config.Sage.EnablePhlegma = true;
        config.Sage.EnableToxikon = true;
        config.Sage.EnablePsyche = true;
        config.Sage.EnableDiagnosis = true;
        config.Sage.EnableEukrasianDiagnosis = true;
        config.Sage.EnablePrognosis = true;
        config.Sage.EnableEukrasianPrognosis = true;
        config.Sage.AddersgallReserve = 0;
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.TaurocholeThreshold = 0.75f;
        config.Sage.SoteriaThreshold = 0.65f;
        config.Sage.PhilosophiaThreshold = 0.75f;

        // Resurrection
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        // Role actions
        config.RoleActions.EnableEsuna = true;
        config.RoleActions.EsunaPriorityThreshold = 2;

        return config;
    }

    /// <summary>
    /// Creates a mock IAddersgallTrackingService.
    /// </summary>
    public static Mock<IAddersgallTrackingService> CreateMockAddersgallService(
        int currentStacks = 3,
        float timerRemaining = 0f)
    {
        var mock = new Mock<IAddersgallTrackingService>();
        mock.Setup(x => x.CurrentStacks).Returns(currentStacks);
        mock.Setup(x => x.TimerRemaining).Returns(timerRemaining);
        mock.Setup(x => x.HasStacks).Returns(currentStacks > 0);
        mock.Setup(x => x.IsAtMax).Returns(currentStacks >= 3);
        mock.Setup(x => x.CanAfford(It.IsAny<int>())).Returns((int cost) => currentStacks >= cost);
        mock.Setup(x => x.HasStacksAboveReserve(It.IsAny<int>())).Returns((int reserve) => currentStacks > reserve);
        return mock;
    }

    /// <summary>
    /// Creates a mock IAdderstingTrackingService.
    /// </summary>
    public static Mock<IAdderstingTrackingService> CreateMockAdderstingService(int currentStacks = 0)
    {
        var mock = new Mock<IAdderstingTrackingService>();
        mock.Setup(x => x.CurrentStacks).Returns(currentStacks);
        mock.Setup(x => x.HasStacks).Returns(currentStacks > 0);
        mock.Setup(x => x.IsAtMax).Returns(currentStacks >= 3);
        mock.Setup(x => x.CanAfford(It.IsAny<int>())).Returns((int cost) => currentStacks >= cost);
        return mock;
    }

    /// <summary>
    /// Creates a mock IKardiaManager.
    /// </summary>
    public static Mock<IKardiaManager> CreateMockKardiaManager(
        bool hasKardia = false,
        ulong kardiaTargetId = 0,
        bool canSwap = true)
    {
        var mock = new Mock<IKardiaManager>();
        mock.Setup(x => x.HasKardia).Returns(hasKardia);
        mock.Setup(x => x.CurrentKardiaTarget).Returns(kardiaTargetId);
        mock.Setup(x => x.CanSwapKardia).Returns(canSwap);
        mock.Setup(x => x.ShouldSwapKardia(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns(false);
        mock.Setup(x => x.RecordSwap(It.IsAny<ulong>()));
        mock.Setup(x => x.GetSoteriaStacks(It.IsAny<IPlayerCharacter>())).Returns(0);
        return mock;
    }

    /// <summary>
    /// Creates a mock IEukrasiaStateService.
    /// </summary>
    public static Mock<IEukrasiaStateService> CreateMockEukrasiaService(bool isActive = false)
    {
        var mock = new Mock<IEukrasiaStateService>();
        mock.Setup(x => x.IsEukrasiaActive(It.IsAny<IPlayerCharacter>())).Returns(isActive);
        mock.Setup(x => x.IsZoeActive(It.IsAny<IPlayerCharacter>())).Returns(false);
        return mock;
    }
}
