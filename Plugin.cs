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
using Olympus.Windows;

namespace Olympus;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginVersion = "3.42.0";
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
    private readonly Apollo apollo;
    private readonly Athena athena;
    private readonly Astraea astraea;
    private readonly Asclepius asclepius;
    private readonly Themis themis;
    private readonly Ares ares;
    private readonly Nyx nyx;
    private readonly Hephaestus hephaestus;
    private readonly Kratos kratos;
    private readonly Zeus zeus;
    private readonly Hermes hermes;
    private readonly Nike nike;
    private readonly Thanatos thanatos;
    private readonly Echidna echidna;
    private readonly Prometheus prometheus;
    private readonly Calliope calliope;
    private readonly Terpsichore terpsichore;
    private readonly Hecate hecate;
    private readonly Persephone persephone;
    private readonly Circe circe;
    private readonly Iris iris;

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
    private readonly TrainingService trainingService;

    private readonly WindowSystem windowSystem = new("Olympus");
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly DebugWindow debugWindow;
    private readonly WelcomeWindow welcomeWindow;
    private readonly AnalyticsWindow analyticsWindow;
    private readonly TrainingWindow trainingWindow;
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
            dataManager);

        // FFLogs integration
        this.fflogsService = new FFlogsService(configuration.FFLogs, log);

        // Training mode
        this.trainingService = new TrainingService(configuration.Training, objectTable, log);

        // Connect analytics to training recommendations (v3.10.0)
        this.performanceTracker.OnSessionCompleted += session =>
        {
            this.trainingService.UpdateRecommendations(session);
        };

        // Create and register rotation modules via factory
        this.rotationManager = new RotationManager();
        this.apollo = CreateApolloRotation();
        this.athena = CreateAthenaRotation();
        this.astraea = CreateAstraeaRotation();
        this.asclepius = CreateAsclepiusRotation();
        this.themis = CreateThemisRotation();
        this.ares = CreateAresRotation();
        this.nyx = CreateNyxRotation();
        this.hephaestus = CreateHephaestusRotation();
        this.kratos = CreateKratosRotation();
        this.zeus = CreateZeusRotation();
        this.hermes = CreateHermesRotation();
        this.nike = CreateNikeRotation();
        this.thanatos = CreateThanatosRotation();
        this.echidna = CreateEchidnaRotation();
        this.prometheus = CreatePrometheusRotation();
        this.calliope = CreateCalliopeRotation();
        this.terpsichore = CreateTerpsichoreRotation();
        this.hecate = CreateHecateRotation();
        this.persephone = CreatePersephoneRotation();
        this.circe = CreateCirceRotation();
        this.iris = CreateIrisRotation();
        RegisterAvailableRotations();

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
            apollo,
            objectTable,
            dataManager,
            athena,
            astraea);

        this.configWindow = new ConfigWindow(configuration, SaveConfiguration);
        this.mainWindow = new MainWindow(configuration, SaveConfiguration, OpenConfigUI, OpenDebugUI, OpenAnalyticsUI, OpenTrainingUI, PluginVersion, rotationManager);
        this.debugWindow = new DebugWindow(debugService, configuration, timelineService);
        this.welcomeWindow = new WelcomeWindow(configuration, SaveConfiguration);
        this.analyticsWindow = new AnalyticsWindow(performanceTracker, configuration, fflogsService);
        this.trainingWindow = new TrainingWindow(trainingService, configuration);

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
    /// Registers all available rotation modules with the rotation manager.
    /// Add new rotation registrations here as they are implemented.
    /// </summary>
    private void RegisterAvailableRotations()
    {
        // Healers
        rotationManager.Register(apollo);
        rotationManager.Register(athena);
        rotationManager.Register(astraea);
        rotationManager.Register(asclepius);

        // Tanks
        rotationManager.Register(themis);
        rotationManager.Register(ares);
        rotationManager.Register(nyx);
        rotationManager.Register(hephaestus);

        // Melee DPS
        rotationManager.Register(kratos);
        rotationManager.Register(zeus);
        rotationManager.Register(hermes);
        rotationManager.Register(nike);
        rotationManager.Register(thanatos);
        rotationManager.Register(echidna);

        // Ranged Physical DPS
        rotationManager.Register(prometheus);
        rotationManager.Register(calliope);
        rotationManager.Register(terpsichore);

        // Casters
        rotationManager.Register(hecate);
        rotationManager.Register(persephone);
        rotationManager.Register(circe);
        rotationManager.Register(iris);
    }

    /// <summary>
    /// Factory method for creating rotation modules by job ID.
    /// Returns null if no rotation is available for the specified job.
    /// </summary>
    /// <param name="jobId">The job ID to create a rotation for.</param>
    /// <returns>The rotation instance, or null if not supported.</returns>
    private IRotation? CreateRotationModule(uint jobId) => jobId switch
    {
        JobRegistry.WhiteMage or JobRegistry.Conjurer => CreateApolloRotation(),
        JobRegistry.Scholar or JobRegistry.Arcanist => CreateAthenaRotation(),
        JobRegistry.Astrologian => CreateAstraeaRotation(),
        JobRegistry.Sage => CreateAsclepiusRotation(),
        JobRegistry.Paladin or JobRegistry.Gladiator => CreateThemisRotation(),
        JobRegistry.Warrior or JobRegistry.Marauder => CreateAresRotation(),
        JobRegistry.DarkKnight => CreateNyxRotation(),
        JobRegistry.Gunbreaker => CreateHephaestusRotation(),
        JobRegistry.Monk or JobRegistry.Pugilist => CreateKratosRotation(),
        JobRegistry.Dragoon or JobRegistry.Lancer => CreateZeusRotation(),
        JobRegistry.Ninja or JobRegistry.Rogue => CreateHermesRotation(),
        JobRegistry.Samurai => CreateNikeRotation(),
        JobRegistry.Reaper => CreateThanatosRotation(),
        JobRegistry.Viper => CreateEchidnaRotation(),
        JobRegistry.Machinist => CreatePrometheusRotation(),
        JobRegistry.Bard or JobRegistry.Archer => CreateCalliopeRotation(),
        JobRegistry.Dancer => CreateTerpsichoreRotation(),
        JobRegistry.BlackMage or JobRegistry.Thaumaturge => CreateHecateRotation(),
        JobRegistry.Summoner or JobRegistry.Arcanist => CreatePersephoneRotation(),
        JobRegistry.RedMage => CreateCirceRotation(),
        JobRegistry.Pictomancer => CreateIrisRotation(),
        _ => null
    };

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
            partyCoordinationService);
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
            partyCoordinationService: partyCoordinationService);
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

        // Dispose healer rotations (they have event subscriptions)
        apollo.Dispose();
        athena.Dispose();
        astraea.Dispose();
        asclepius.Dispose();

        // Dispose melee DPS rotations that have event subscriptions
        nike.Dispose();

        damageIntakeService.Dispose();
        healingIntakeService.Dispose();
        hpPredictionService.Dispose();
        performanceTracker.Dispose();
        timelineService.Dispose();
        combatEventService.Dispose();
    }
}
