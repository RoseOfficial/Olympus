using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Rotation;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Calculation;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Debug;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Scholar;
using Olympus.Services.Cache;
using Olympus.Services.Tank;
using Olympus.Services.Positional;
using Olympus.Services.Analytics;
using Olympus.Services.FFLogs;
using Olympus.Services.Training;
using Olympus.Timeline;
using Olympus.Training;
using Olympus.Windows;
using Olympus.Windows.Training;

namespace Olympus;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginVersion = "4.1.1";
    private const string CommandName = "/olympus";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IFramework framework;
    private readonly IObjectTable objectTable;
    private readonly IPartyList partyList;
    private readonly IPluginLog log;
    private readonly IClientState clientState;
    private readonly ICommandManager commandManager;
    private readonly IChatGui chatGui;
    private readonly IDataManager dataManager;
    private readonly ICondition condition;
    private readonly IJobGauges jobGauges;

    private readonly Configuration configuration;
    private readonly ActionTracker actionTracker;
    private readonly CombatEventService combatEventService;
    private readonly DamageIntakeService damageIntakeService;
    private readonly HealingIntakeService healingIntakeService;
    private readonly DamageTrendService damageTrendService;
    private readonly CooldownPlanner cooldownPlanner;
    private readonly TargetingService targetingService;
    private readonly ShieldTrackingService shieldTrackingService;
    private readonly HpPredictionService hpPredictionService;
    private readonly ActionService actionService;
    private readonly PlayerStatsService playerStatsService;
    private readonly HealingSpellSelector healingSpellSelector;
    private readonly SpellStatusService spellStatusService;
    private readonly DebugService debugService;
    private readonly DebuffDetectionService debuffDetectionService;
    private readonly RotationManager rotationManager;

    // Tank services
    private readonly EnmityService enmityService;
    private readonly TankCooldownService tankCooldownService;

    // Melee DPS services
    private readonly PositionalService positionalService;

    // Timeline service
    private readonly TimelineService timelineService;

    // Party coordination (multi-Olympus IPC)
    private readonly PartyCoordinationService? partyCoordinationService;
    private readonly PartyCoordinationIpc? partyCoordinationIpc;

    // Performance analytics
    private readonly PerformanceTracker performanceTracker;

    // FFLogs integration
    private readonly FFlogsService? fflogsService;

    // Training mode
    private readonly TrainingDataRegistry trainingDataRegistry;
    private readonly TrainingService trainingService;
    private readonly RealTimeCoachingService realTimeCoachingService;
    private readonly DecisionValidationService decisionValidationService;
    private readonly SpacedRepetitionService spacedRepetitionService;

    private readonly WindowSystem windowSystem = new("Olympus");
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly DebugWindow debugWindow;
    private readonly WelcomeWindow welcomeWindow;
    private readonly AnalyticsWindow analyticsWindow;
    private readonly TrainingWindow trainingWindow;
    private readonly HintOverlay hintOverlay;
    private readonly TelemetryService telemetryService;

    private readonly OlympusIpc olympusIpc;
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IObjectTable objectTable,
        IPartyList partyList,
        IPluginLog log,
        IClientState clientState,
        ICommandManager commandManager,
        IChatGui chatGui,
        IDataManager dataManager,
        ICondition condition,
        IGameInteropProvider gameInteropProvider,
        ITargetManager targetManager,
        IJobGauges jobGauges)
    {
        this.pluginInterface = pluginInterface;
        this.framework = framework;
        this.objectTable = objectTable;
        this.partyList = partyList;
        this.log = log;
        this.clientState = clientState;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.dataManager = dataManager;
        this.condition = condition;
        this.jobGauges = jobGauges;

        this.configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Load persisted calibration data for healing calculations
        HealingCalculator.LoadCalibration(configuration.Calibration);

        this.actionTracker = new ActionTracker(dataManager, configuration);
        this.combatEventService = new CombatEventService(gameInteropProvider, log, objectTable);
        this.damageIntakeService = new DamageIntakeService(combatEventService);
        this.healingIntakeService = new HealingIntakeService(combatEventService);
        this.damageTrendService = new DamageTrendService(damageIntakeService, healingIntakeService);
        this.cooldownPlanner = new CooldownPlanner(damageIntakeService, damageTrendService, configuration);
        this.targetingService = new TargetingService(objectTable, partyList, targetManager, configuration);
        this.shieldTrackingService = new ShieldTrackingService(objectTable, partyList, log);

        // New action system services
        this.hpPredictionService = new HpPredictionService(
            combatEventService,
            configuration,
            shieldTrackingService,
            damageTrendService);
        this.actionService = new ActionService(actionTracker);
        this.playerStatsService = new PlayerStatsService(log, dataManager);

        // Healing spell selector (evaluates all heals and picks the best)
        this.healingSpellSelector = new HealingSpellSelector(
            actionService,
            playerStatsService,
            hpPredictionService,
            combatEventService,
            configuration,
            damageTrendService);

        // Spell status service (provides real-time status of all WHM spells)
        this.spellStatusService = new SpellStatusService(actionService);

        // Debuff detection service for Esuna
        this.debuffDetectionService = new DebuffDetectionService(dataManager);

        // Tank services
        this.enmityService = new EnmityService(objectTable, partyList);
        this.tankCooldownService = new TankCooldownService(configuration.Tank);

        // Melee DPS services
        this.positionalService = new PositionalService();

        // Timeline service for fight-aware predictions
        this.timelineService = new TimelineService(log, combatEventService);
        combatEventService.OnAbilityUsed += (sourceId, actionId) => timelineService.OnAbilityUsed(sourceId, actionId);

        // Party coordination service (multi-Olympus IPC)
        if (configuration.PartyCoordination.EnablePartyCoordination)
        {
            this.partyCoordinationService = new PartyCoordinationService(configuration.PartyCoordination, log);
            this.partyCoordinationIpc = new PartyCoordinationIpc(pluginInterface, partyCoordinationService, log);
        }

        // Performance analytics
        this.performanceTracker = new PerformanceTracker(
            configuration.Analytics,
            actionTracker,
            combatEventService,
            objectTable,
            partyList,
            log,
            dataManager,
            partyCoordinationService);

        // FFLogs integration
        this.fflogsService = new FFlogsService(configuration.FFLogs, log);

        // Training mode
        this.trainingDataRegistry = new TrainingDataRegistry(log);
        this.trainingService = new TrainingService(configuration.Training, objectTable, trainingDataRegistry, log);

        // Real-time coaching hints (v3.49.0)
        this.realTimeCoachingService = new RealTimeCoachingService(
            configuration.Training,
            trainingService,
            log);

        // Decision validation (v3.50.0)
        this.decisionValidationService = new DecisionValidationService(
            configuration.Training,
            log);

        // Spaced repetition (v3.52.0)
        this.spacedRepetitionService = new SpacedRepetitionService(
            configuration.Training,
            trainingService,
            log);

        // Connect spaced repetition to training service for retention tracking (v4.0.0)
        this.trainingService.SetSpacedRepetitionService(this.spacedRepetitionService);

        // Connect analytics to training recommendations (v3.10.0)
        this.performanceTracker.OnSessionCompleted += session =>
        {
            this.trainingService.UpdateRecommendations(session);
        };

        // Create rotation manager and register factories (lazy loading)
        this.rotationManager = new RotationManager();
        RegisterRotationFactories();

        // Debug service aggregates all debug data
        this.debugService = new DebugService(
            actionTracker,
            actionService,
            combatEventService,
            hpPredictionService,
            playerStatsService,
            healingSpellSelector,
            spellStatusService,
            rotationManager,
            objectTable,
            dataManager);

        this.configWindow = new ConfigWindow(configuration, SaveConfiguration);
        this.mainWindow = new MainWindow(configuration, SaveConfiguration, OpenConfigUI, OpenDebugUI, OpenAnalyticsUI, OpenTrainingUI, PluginVersion, rotationManager);
        this.debugWindow = new DebugWindow(debugService, configuration, timelineService);
        this.welcomeWindow = new WelcomeWindow(configuration, SaveConfiguration);
        this.analyticsWindow = new AnalyticsWindow(performanceTracker, configuration, fflogsService);
        this.trainingWindow = new TrainingWindow(trainingService, configuration, decisionValidationService, spacedRepetitionService);
        this.hintOverlay = new HintOverlay(realTimeCoachingService, configuration.Training);

        // Telemetry service for anonymous usage tracking
        this.telemetryService = new TelemetryService(configuration, log);

        // IPC interface for external plugin integration
        this.olympusIpc = new OlympusIpc(
            pluginInterface,
            configuration,
            SaveConfiguration,
            log,
            PluginVersion,
            () => rotationManager);

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(debugWindow);
        windowSystem.AddWindow(welcomeWindow);
        windowSystem.AddWindow(analyticsWindow);
        windowSystem.AddWindow(trainingWindow);
        windowSystem.AddWindow(hintOverlay);

        mainWindow.IsOpen = configuration.MainWindowVisible;
        // Debug window always starts closed - user must explicitly open it
        debugWindow.IsOpen = false;

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUI;

        this.commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Olympus window. Args: toggle (enable/disable), debug (show debug window)"
        });

        this.framework.Update += OnFrameworkUpdate;

        // Hook territory changed to load timelines for the current zone
        clientState.TerritoryChanged += OnTerritoryChanged;

        // Load timeline for current zone if already in one
        if (clientState.TerritoryType != 0)
        {
            timelineService.LoadForZone(clientState.TerritoryType);
        }

        // Send anonymous telemetry ping (fire-and-forget)
        telemetryService.SendStartupPing(PluginVersion);
    }

    private void OnTerritoryChanged(ushort zoneId)
    {
        timelineService.LoadForZone(zoneId);
    }

    private void SaveConfiguration()
    {
        configuration.MainWindowVisible = mainWindow.IsOpen;
        configuration.Debug.DebugWindowVisible = debugWindow.IsOpen;
        configuration.Analytics.AnalyticsWindowVisible = analyticsWindow.IsOpen;
        configuration.Training.TrainingWindowVisible = trainingWindow.IsOpen;
        pluginInterface.SavePluginConfig(configuration);
    }

    private void DrawUI()
    {
        // Only draw windows when logged in (not on login/character select screen)
        if (!clientState.IsLoggedIn)
            return;

        // Show welcome window on first run
        welcomeWindow.ShowIfNeeded();

        windowSystem.Draw();
    }

    private void OpenConfigUI() => configWindow.Toggle();

    private void OpenMainUI() => mainWindow.Toggle();

    private void OpenDebugUI() => debugWindow.Toggle();

    private void OpenAnalyticsUI() => analyticsWindow.Toggle();

    private void OpenTrainingUI() => trainingWindow.Toggle();

    private void OnCommand(string command, string args)
    {
        var arg = args.Trim().ToLowerInvariant();

        switch (arg)
        {
            case "toggle":
                configuration.Enabled = !configuration.Enabled;
                SaveConfiguration();
                olympusIpc.NotifyStateChanged(configuration.Enabled);
                var status = configuration.Enabled ? "enabled" : "disabled";
                chatGui.Print($"Olympus {status}");
                log.Info($"Olympus {status}");
                break;

            case "debug":
                debugWindow.Toggle();
                SaveConfiguration();
                break;

            default:
                mainWindow.Toggle();
                break;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Always update debug service frame counter
        debugService.Update();

        // Always update shield tracking for accurate HP predictions
        shieldTrackingService.Update();

        // Update timeline service for sync and predictions
        timelineService.Update();

        // Update performance analytics (tracks combat state independently)
        performanceTracker.Update();

        // Update training mode
        trainingService.Update();

        // Update coaching hints (v3.49.0)
        realTimeCoachingService.Update();
        hintOverlay.HandleInput();

        if (!configuration.Enabled)
            return;

        if (!clientState.IsLoggedIn)
            return;

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer == null)
            return;

        // Update party coordination service (heartbeat, cleanup)
        partyCoordinationService?.Update(
            localPlayer.EntityId,
            localPlayer.ClassJob.RowId,
            configuration.Enabled);

        // Check if we have a rotation for the current job
        var jobId = localPlayer.ClassJob.RowId;
        if (!rotationManager.UpdateActiveRotation(jobId))
            return;

        rotationManager.Execute(localPlayer);
    }

    #region Rotation Factory

    /// <summary>
    /// Registers factory functions for all rotation modules.
    /// Rotations are created lazily when the player switches to that job.
    /// </summary>
    private void RegisterRotationFactories()
    {
        // Healers
        rotationManager.RegisterFactory(JobRegistry.WhiteMage, CreateApolloRotation);
        rotationManager.RegisterFactory(JobRegistry.Conjurer, CreateApolloRotation);
        rotationManager.RegisterFactory(JobRegistry.Scholar, CreateAthenaRotation);
        rotationManager.RegisterFactory(JobRegistry.Arcanist, CreateAthenaRotation);
        rotationManager.RegisterFactory(JobRegistry.Astrologian, CreateAstraeaRotation);
        rotationManager.RegisterFactory(JobRegistry.Sage, CreateAsclepiusRotation);

        // Tanks
        rotationManager.RegisterFactory(JobRegistry.Paladin, CreateThemisRotation);
        rotationManager.RegisterFactory(JobRegistry.Gladiator, CreateThemisRotation);
        rotationManager.RegisterFactory(JobRegistry.Warrior, CreateAresRotation);
        rotationManager.RegisterFactory(JobRegistry.Marauder, CreateAresRotation);
        rotationManager.RegisterFactory(JobRegistry.DarkKnight, CreateNyxRotation);
        rotationManager.RegisterFactory(JobRegistry.Gunbreaker, CreateHephaestusRotation);

        // Melee DPS
        rotationManager.RegisterFactory(JobRegistry.Monk, CreateKratosRotation);
        rotationManager.RegisterFactory(JobRegistry.Pugilist, CreateKratosRotation);
        rotationManager.RegisterFactory(JobRegistry.Dragoon, CreateZeusRotation);
        rotationManager.RegisterFactory(JobRegistry.Lancer, CreateZeusRotation);
        rotationManager.RegisterFactory(JobRegistry.Ninja, CreateHermesRotation);
        rotationManager.RegisterFactory(JobRegistry.Rogue, CreateHermesRotation);
        rotationManager.RegisterFactory(JobRegistry.Samurai, CreateNikeRotation);
        rotationManager.RegisterFactory(JobRegistry.Reaper, CreateThanatosRotation);
        rotationManager.RegisterFactory(JobRegistry.Viper, CreateEchidnaRotation);

        // Ranged Physical DPS
        rotationManager.RegisterFactory(JobRegistry.Machinist, CreatePrometheusRotation);
        rotationManager.RegisterFactory(JobRegistry.Bard, CreateCalliopeRotation);
        rotationManager.RegisterFactory(JobRegistry.Archer, CreateCalliopeRotation);
        rotationManager.RegisterFactory(JobRegistry.Dancer, CreateTerpsichoreRotation);

        // Casters
        rotationManager.RegisterFactory(JobRegistry.BlackMage, CreateHecateRotation);
        rotationManager.RegisterFactory(JobRegistry.Thaumaturge, CreateHecateRotation);
        rotationManager.RegisterFactory(JobRegistry.Summoner, CreatePersephoneRotation);
        rotationManager.RegisterFactory(JobRegistry.RedMage, CreateCirceRotation);
        rotationManager.RegisterFactory(JobRegistry.Pictomancer, CreateIrisRotation);
    }

    /// <summary>
    /// Creates the Apollo (White Mage) rotation module.
    /// </summary>
    private Apollo CreateApolloRotation()
    {
        return new Apollo(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            healingSpellSelector,
            debuffDetectionService,
            cooldownPlanner,
            shieldTrackingService,
            timelineService,
            partyCoordinationService,
            trainingService);
    }

    /// <summary>
    /// Creates the Athena (Scholar) rotation module.
    /// </summary>
    private Athena CreateAthenaRotation()
    {
        return new Athena(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            cooldownPlanner,
            healingSpellSelector,
            shieldTrackingService,
            timelineService,
            partyCoordinationService,
            trainingService);
    }

    /// <summary>
    /// Creates the Astraea (Astrologian) rotation module.
    /// </summary>
    private Astraea CreateAstraeaRotation()
    {
        return new Astraea(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            cooldownPlanner,
            healingSpellSelector,
            shieldTrackingService,
            jobGauges,
            timelineService,
            partyCoordinationService,
            trainingService);
    }

    /// <summary>
    /// Creates the Asclepius (Sage) rotation module.
    /// </summary>
    private Asclepius CreateAsclepiusRotation()
    {
        return new Asclepius(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            cooldownPlanner,
            healingSpellSelector,
            shieldTrackingService,
            timelineService,
            partyCoordinationService,
            trainingService);
    }

    /// <summary>
    /// Creates the Themis (Paladin) rotation module.
    /// </summary>
    private Themis CreateThemisRotation()
    {
        return new Themis(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            enmityService,
            tankCooldownService,
            timelineService);
    }

    /// <summary>
    /// Creates the Ares (Warrior) rotation module.
    /// </summary>
    private Ares CreateAresRotation()
    {
        return new Ares(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            enmityService,
            tankCooldownService,
            timelineService);
    }

    /// <summary>
    /// Creates the Nyx (Dark Knight) rotation module.
    /// </summary>
    private Nyx CreateNyxRotation()
    {
        return new Nyx(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            enmityService,
            tankCooldownService,
            timelineService);
    }

    /// <summary>
    /// Creates the Hephaestus (Gunbreaker) rotation module.
    /// </summary>
    private Hephaestus CreateHephaestusRotation()
    {
        return new Hephaestus(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            enmityService,
            tankCooldownService,
            timelineService);
    }

    /// <summary>
    /// Creates the Kratos (Monk) rotation module.
    /// </summary>
    private Kratos CreateKratosRotation()
    {
        return new Kratos(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Zeus (Dragoon) rotation module.
    /// </summary>
    private Zeus CreateZeusRotation()
    {
        return new Zeus(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Hermes (Ninja) rotation module.
    /// </summary>
    private Hermes CreateHermesRotation()
    {
        return new Hermes(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Nike (Samurai) rotation module.
    /// </summary>
    private Nike CreateNikeRotation()
    {
        return new Nike(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Thanatos (Reaper) rotation module.
    /// </summary>
    private Thanatos CreateThanatosRotation()
    {
        return new Thanatos(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Echidna (Viper) rotation module.
    /// </summary>
    private Echidna CreateEchidnaRotation()
    {
        return new Echidna(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            positionalService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Prometheus (Machinist) rotation module.
    /// </summary>
    private Prometheus CreatePrometheusRotation()
    {
        return new Prometheus(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Calliope (Bard) rotation module.
    /// </summary>
    private Calliope CreateCalliopeRotation()
    {
        return new Calliope(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Terpsichore (Dancer) rotation module.
    /// </summary>
    private Terpsichore CreateTerpsichoreRotation()
    {
        return new Terpsichore(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Hecate (Black Mage) rotation module.
    /// </summary>
    private Hecate CreateHecateRotation()
    {
        return new Hecate(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService);
    }

    /// <summary>
    /// Creates the Persephone (Summoner) rotation module.
    /// </summary>
    private Persephone CreatePersephoneRotation()
    {
        return new Persephone(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService);
    }

    /// <summary>
    /// Creates the Circe (Red Mage) rotation module.
    /// </summary>
    private Circe CreateCirceRotation()
    {
        return new Circe(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService,
            trainingService);
    }

    /// <summary>
    /// Creates the Iris (Pictomancer) rotation module.
    /// </summary>
    private Iris CreateIrisRotation()
    {
        return new Iris(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            timelineService,
            partyCoordinationService: partyCoordinationService,
            trainingService: trainingService);
    }

    #endregion

    public void Dispose()
    {
        // Save calibration data before shutdown
        HealingCalculator.SaveCalibration(configuration.Calibration);
        pluginInterface.SavePluginConfig(configuration);

        framework.Update -= OnFrameworkUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;
        commandManager.RemoveHandler(CommandName);

        pluginInterface.UiBuilder.Draw -= DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;

        windowSystem.RemoveAllWindows();
        olympusIpc.Dispose();
        partyCoordinationIpc?.Dispose();
        fflogsService?.Dispose();
        telemetryService.Dispose();

        // Dispose rotation manager (handles all instantiated rotations)
        rotationManager.Dispose();

        damageIntakeService.Dispose();
        healingIntakeService.Dispose();
        hpPredictionService.Dispose();
        performanceTracker.Dispose();
        timelineService.Dispose();
        combatEventService.Dispose();
    }
}
