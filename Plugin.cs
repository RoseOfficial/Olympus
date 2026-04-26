using System;
using Dalamud.Game.ClientState.Objects.Types;
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
using Olympus.Services.Input;
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
using Olympus.Localization;
using Olympus.Services.Drawing;
using Olympus.Rotation.Common.Helpers;
using Olympus.Windows;
using Olympus.Windows.Debug.Tabs;
using Olympus.Windows.Training;

namespace Olympus;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginVersion = "4.16.0";
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
    private readonly ITargetManager targetManager;

    private readonly Configuration configuration;
    private readonly ActionTracker actionTracker;
    private readonly CombatEventService combatEventService;
    private readonly DamageIntakeService damageIntakeService;
    private readonly DoTTrackingService dotTrackingService;
    private readonly HealingIntakeService healingIntakeService;
    private readonly DamageTrendService damageTrendService;
    private readonly CooldownPlanner cooldownPlanner;
    private readonly TargetingService targetingService;
    private readonly GapCloserSafetyService gapCloserSafetyService;
    private readonly ShieldTrackingService shieldTrackingService;
    private readonly HpPredictionService hpPredictionService;
    private readonly ActionService actionService;
    private readonly PlayerStatsService playerStatsService;
    private readonly HealingSpellSelector healingSpellSelector;
    private readonly SpellStatusService spellStatusService;
    private readonly DebugService debugService;
    private readonly DebuffDetectionService debuffDetectionService;
    private readonly RotationManager rotationManager;
    private readonly ServiceContainer serviceContainer;
    private readonly RotationFactory rotationFactory;

    // Tank services
    private readonly EnmityService enmityService;
    private readonly TankCooldownService tankCooldownService;

    // Melee DPS services
    private readonly PositionalService positionalService;

    // Burst window tracking for DPS rotations
    private readonly BurstWindowService burstWindowService;

    // Player-intent overrides (Shift = burst now, Ctrl = conservative)
    private readonly ModifierKeyService modifierKeyService;

    // Timeline service
    private readonly TimelineService timelineService;

    // Party coordination (multi-Olympus IPC)
    private readonly PartyCoordinationService? partyCoordinationService;
    private readonly PartyCoordinationIpc? partyCoordinationIpc;

    // Performance analytics
    private readonly PerformanceTracker performanceTracker;

    // FFLogs integration
    private readonly FFlogsService? fflogsService;

    // Post-combat coaching summaries
    private readonly FightSummaryService? fightSummaryService;

    // Training mode
    private readonly TrainingDataRegistry trainingDataRegistry;
    private readonly TrainingService trainingService;
    private readonly RealTimeCoachingService realTimeCoachingService;
    private readonly DecisionValidationService decisionValidationService;
    private readonly SpacedRepetitionService spacedRepetitionService;

    // Localization
    private readonly OlympusLocalization localization;
    private readonly GameDataLocalizer gameDataLocalizer;

    private readonly WindowSystem windowSystem = new("Olympus");
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly DebugWindow debugWindow;
    private readonly WelcomeWindow welcomeWindow;
    private readonly AnalyticsWindow analyticsWindow;
    private readonly TrainingWindow trainingWindow;
    private readonly ChangelogWindow changelogWindow;
    private readonly HintOverlay hintOverlay;
    private readonly OverlayWindow overlayWindow;
    private readonly ActionFeedWindow actionFeedWindow;
    private readonly TelemetryService telemetryService;
    private readonly DrawCanvas drawCanvas;
    private readonly DrawingService drawingService;
    private readonly AoETracker aoeTracker;
    private readonly SmartAoEService smartAoEService;

    private readonly OlympusIpc olympusIpc;
    private readonly UpdateCheckerService updateCheckerService;

    // Pull-intent state machine + consumable services (tincture automation)
    private readonly Olympus.Services.Pull.PullIntentService pullIntentService;
    private readonly Olympus.Services.Content.HighEndContentService highEndContentService;
    private readonly Olympus.Services.Consumables.DalamudInventoryProbe inventoryProbe;
    private readonly Olympus.Services.Consumables.DalamudTinctureCooldownProbe tinctureCooldownProbe;
    private readonly Olympus.Services.Consumables.ConsumableService consumableService;
    private readonly Olympus.Services.Consumables.TinctureDispatcher tinctureDispatcher;

    // Error metrics
    private readonly ErrorMetricsService errorMetricsService;

    // Stored event handler delegates to allow removal in Dispose
    private readonly Action<uint, uint> onAbilityUsedHandler;
    private readonly Action<FightSession> onSessionCompletedHandler;
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
        IJobGauges jobGauges,
        ITextureProvider textureProvider,
        IGameGui gameGui,
        INotificationManager notificationManager,
        IKeyState keyState)
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
        this.targetManager = targetManager;

        this.configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize localization (must be early, before UI construction)
        this.localization = new OlympusLocalization(clientState, configuration, log);
        this.gameDataLocalizer = new GameDataLocalizer(dataManager);

        // Load persisted calibration data for healing calculations
        HealingCalculator.LoadCalibration(configuration.Calibration);

        this.actionTracker = new ActionTracker(dataManager, configuration);
        this.combatEventService = new CombatEventService(gameInteropProvider, log, objectTable);
        this.damageIntakeService = new DamageIntakeService(combatEventService);
        this.dotTrackingService = new DoTTrackingService(combatEventService, damageIntakeService);
        this.healingIntakeService = new HealingIntakeService(combatEventService);
        this.damageTrendService = new DamageTrendService(damageIntakeService, healingIntakeService);
        this.cooldownPlanner = new CooldownPlanner(damageIntakeService, damageTrendService, configuration);
        this.gapCloserSafetyService = new GapCloserSafetyService(configuration, targetManager);
        this.targetingService = new TargetingService(objectTable, partyList, targetManager, configuration, gapCloserSafetyService);
        this.shieldTrackingService = new ShieldTrackingService(objectTable, partyList, log);

        // New action system services
        this.hpPredictionService = new HpPredictionService(
            combatEventService,
            configuration,
            shieldTrackingService,
            damageTrendService);
        this.actionService = new ActionService(actionTracker, objectTable: objectTable);
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

        // Timeline service for fight-aware predictions (must precede TankCooldownService)
        this.timelineService = new TimelineService(log, combatEventService);
        this.onAbilityUsedHandler = (sourceId, actionId) => timelineService.OnAbilityUsed(sourceId, actionId);
        combatEventService.OnAbilityUsed += this.onAbilityUsedHandler;

        // Wire timeline into damage prediction service
        this.damageIntakeService.SetTimelineService(this.timelineService);

        // Tank services
        this.enmityService = new EnmityService(objectTable, partyList);
        this.tankCooldownService = new TankCooldownService(configuration.Tank, this.timelineService);

        // Melee DPS services
        this.positionalService = new PositionalService();

        // Party coordination service (multi-Olympus IPC)
        if (configuration.PartyCoordination.EnablePartyCoordination)
        {
            this.partyCoordinationService = new PartyCoordinationService(configuration.PartyCoordination, log);
            this.partyCoordinationIpc = new PartyCoordinationIpc(pluginInterface, partyCoordinationService, log);
        }

        // Burst window service (DPS resource pooling during raid buffs)
        // Created after partyCoordinationService so it can use IPC data when available.
        // Wired to CombatEventService for low-latency cast-event burst detection: when
        // a party member casts a known raid buff, the window opens immediately rather
        // than waiting for the per-frame status scan to see the buff land.
        this.burstWindowService = new BurstWindowService(
            this.partyCoordinationService,
            this.combatEventService,
            partyList,
            clientState);

        // Modifier-key overrides (Shift = burst, Ctrl = conservative). Static accessor
        // on BurstHoldHelper means all 30+ pooling decision sites pick up the override
        // automatically without threading the service through every constructor.
        this.modifierKeyService = new ModifierKeyService(keyState, configuration);
        BurstHoldHelper.ModifierKeys = this.modifierKeyService;

        // Performance analytics
        this.performanceTracker = new PerformanceTracker(
            configuration.Analytics,
            actionTracker,
            combatEventService,
            objectTable,
            partyList,
            log,
            dataManager,
            partyCoordinationService,
            pluginInterface.ConfigDirectory.FullName);

        // FFLogs integration
        this.fflogsService = new FFlogsService(configuration.FFLogs, log);

        // Post-combat coaching summaries
        this.fightSummaryService = new FightSummaryService(
            performanceTracker, actionTracker, burstWindowService, fflogsService, configuration);

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
        this.onSessionCompletedHandler = session => this.trainingService.UpdateRecommendations(session);
        this.performanceTracker.OnSessionCompleted += this.onSessionCompletedHandler;

        // Error metrics service (aggregates suppressed errors for debugging)
        this.errorMetricsService = new ErrorMetricsService();

        // Pull-intent state machine. Driven each frame by Plugin.Update from
        // LocalPlayer.IsCasting + ActionManager.QueuedActionId + InCombat.
        this.pullIntentService = new Olympus.Services.Pull.PullIntentService();

        // High-end content classifier. Hooked to TerritoryChanged and primed for
        // current zone in case the plugin loads while already in a duty.
        this.highEndContentService = new Olympus.Services.Content.HighEndContentService(dataManager);
        this.highEndContentService.OnTerritoryChanged(clientState.TerritoryType);

        // Inventory and tincture-cooldown probes (production-side wrappers).
        this.inventoryProbe = new Olympus.Services.Consumables.DalamudInventoryProbe(errorMetricsService);
        this.tinctureCooldownProbe = new Olympus.Services.Consumables.DalamudTinctureCooldownProbe(errorMetricsService);

        // Consumable service: inventory probing + recast cooldown + ShouldUseTinctureNow gate.
        // Per-fight inventory-empty warning routed through chatGui.
        this.consumableService = new Olympus.Services.Consumables.ConsumableService(
            configuration.Consumables,
            pullIntentService,
            highEndContentService,
            inventoryProbe,
            tinctureCooldownProbe,
            chatGui);

        // Tincture dispatcher: shared by Path 1 (TinctureCandidate in PrePullModule)
        // and Path 2 (in-combat re-pot push from BaseRotation).
        this.tinctureDispatcher = new Olympus.Services.Consumables.TinctureDispatcher(
            consumableService,
            burstWindowService,
            actionService,
            objectTable);

        // Smart AoE service (must be created before service container)
        this.aoeTracker = new AoETracker();
        this.smartAoEService = new SmartAoEService(targetingService, dataManager, aoeTracker, log);
        this.smartAoEService.SubscribeToCombatEvents(combatEventService);

        // Create service container for rotation dependency injection
        this.serviceContainer = CreateServiceContainer();

        // Create rotation manager and factory, then auto-discover rotations
        this.rotationManager = new RotationManager();
        this.rotationFactory = new RotationFactory(serviceContainer, log);
        var rotationCount = rotationFactory.DiscoverAndRegisterFactories(rotationManager);
        log.Information("Registered {Count} rotation modules via auto-discovery", rotationCount);

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

        this.drawingService = new DrawingService(pluginInterface, configuration.DrawHelper, log);
        this.drawCanvas = new DrawCanvas(drawingService, configuration, objectTable, clientState, targetManager, gameGui, positionalService, rotationManager);
        this.updateCheckerService = new UpdateCheckerService(PluginVersion, notificationManager, log);
        this.configWindow = new ConfigWindow(configuration, SaveConfiguration, updateCheckerService, textureProvider);
        this.mainWindow = new MainWindow(configuration, SaveConfiguration, OpenConfigUI, OpenDebugUI, OpenAnalyticsUI, OpenTrainingUI, OpenChangelogUI, OpenOverlayUI, PluginVersion, rotationManager, textureProvider);
        var smartAoETab = new SmartAoETab(aoeTracker, drawCanvas, objectTable);
        this.debugWindow = new DebugWindow(debugService, configuration, timelineService, smartAoETab);
        this.welcomeWindow = new WelcomeWindow(configuration, SaveConfiguration, OpenConfigUI);
        this.analyticsWindow = new AnalyticsWindow(performanceTracker, configuration, SaveConfiguration, fflogsService, fightSummaryService);
        this.trainingWindow = new TrainingWindow(trainingService, configuration, decisionValidationService, spacedRepetitionService);
        this.changelogWindow = new ChangelogWindow();
        this.hintOverlay = new HintOverlay(realTimeCoachingService, configuration.Training);
        this.overlayWindow = new OverlayWindow(configuration, SaveConfiguration, rotationManager, partyList, this.timelineService);
        this.actionFeedWindow = new ActionFeedWindow(configuration, SaveConfiguration, actionService, textureProvider);

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

        if (fightSummaryService != null)
        {
            var fightSummaryWindow = new FightSummaryWindow(
                fightSummaryService, framework, configuration,
                () => { analyticsWindow.IsOpen = true; });
            windowSystem.AddWindow(fightSummaryWindow);
        }

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(debugWindow);
        windowSystem.AddWindow(welcomeWindow);
        windowSystem.AddWindow(analyticsWindow);
        windowSystem.AddWindow(trainingWindow);
        windowSystem.AddWindow(changelogWindow);
        windowSystem.AddWindow(hintOverlay);
        windowSystem.AddWindow(overlayWindow);
        overlayWindow.IsOpen = configuration.Overlay.IsVisible;
        windowSystem.AddWindow(actionFeedWindow);
        // Visibility is gated by DrawConditions via ActionFeed.IsVisible; keep the window open
        // so the config toggle takes effect without needing to reopen the window.
        actionFeedWindow.IsOpen = true;

        windowSystem.AddWindow(drawCanvas);

        mainWindow.IsOpen = configuration.MainWindowVisible;
        mainWindow.RespectCloseHotkey = !configuration.PreventEscapeClose;
        // Debug window always starts closed - user must explicitly open it
        debugWindow.IsOpen = false;

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUI;
        pluginInterface.UiBuilder.DisableCutsceneUiHide = configuration.ShowDuringCutscenes;

        this.commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Olympus window. Subcommands: toggle | debug | hardcast [on|off|toggle]"
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

        // Check for updates in the background (delayed 15s)
        updateCheckerService.StartupCheck();
    }

    private void OnTerritoryChanged(ushort zoneId)
    {
        highEndContentService.OnTerritoryChanged(zoneId);
        consumableService.OnTerritoryChanged();
        timelineService.LoadForZone(zoneId);
        combatEventService.Clear();
        hpPredictionService.ClearPendingHeals();
        damageIntakeService.Clear();
        damageIntakeService.CleanupExpiredEntries();
        healingIntakeService.Clear();
        healingIntakeService.CleanupExpiredEntries();
    }

    /// <summary>
    /// Creates and populates the service container for rotation dependency injection.
    /// </summary>
    private ServiceContainer CreateServiceContainer()
    {
        var container = new ServiceContainer();

        // Dalamud services
        container.Register<IPluginLog>(log);
        container.Register<IObjectTable>(objectTable);
        container.Register<IPartyList>(partyList);
        container.Register<IJobGauges>(jobGauges);

        // Core services (register both interface and concrete where interface exists)
        container.Register(configuration);
        container.Register<IActionTracker, ActionTracker>(actionTracker);
        container.Register(actionService);
        container.Register<ICombatEventService, CombatEventService>(combatEventService);
        container.Register<IDamageIntakeService, DamageIntakeService>(damageIntakeService);
        container.Register<IDamageTrendService, DamageTrendService>(damageTrendService);
        container.Register<ITargetingService, TargetingService>(targetingService);
        container.Register<IGapCloserSafetyService, GapCloserSafetyService>(gapCloserSafetyService);
        container.Register<IHpPredictionService, HpPredictionService>(hpPredictionService);
        container.Register<IPlayerStatsService, PlayerStatsService>(playerStatsService);
        container.Register<IDebuffDetectionService, DebuffDetectionService>(debuffDetectionService);

        // Healer services
        container.Register(healingSpellSelector);
        container.Register<ICooldownPlanner, CooldownPlanner>(cooldownPlanner);
        container.Register<IShieldTrackingService, ShieldTrackingService>(shieldTrackingService);

        // Tank services
        container.Register<IEnmityService, EnmityService>(enmityService);
        container.Register<ITankCooldownService, TankCooldownService>(tankCooldownService);
        // Melee DPS services
        container.Register<IPositionalService, PositionalService>(positionalService);

        // DPS burst window service
        container.Register<IBurstWindowService, BurstWindowService>(burstWindowService);

        // Tincture automation
        container.Register<Olympus.Services.Pull.IPullIntentService, Olympus.Services.Pull.PullIntentService>(pullIntentService);
        container.Register<Olympus.Services.Consumables.ITinctureDispatcher, Olympus.Services.Consumables.TinctureDispatcher>(tinctureDispatcher);

        // Player-intent override service
        container.Register<IModifierKeyService, ModifierKeyService>(modifierKeyService);

        // Smart AoE service for directional ability optimization
        container.Register<ISmartAoEService, SmartAoEService>(smartAoEService);

        // Optional services (rotations have default null parameters)
        container.Register<ITimelineService, TimelineService>(timelineService);
        if (partyCoordinationService != null)
            container.Register<IPartyCoordinationService, PartyCoordinationService>(partyCoordinationService);
        container.Register<ITrainingService, TrainingService>(trainingService);
        container.Register<IErrorMetricsService, ErrorMetricsService>(errorMetricsService);
        if (fightSummaryService != null)
            container.Register<IFightSummaryService, FightSummaryService>(fightSummaryService);

        return container;
    }

    private void SaveConfiguration()
    {
        configuration.MainWindowVisible = mainWindow.IsOpen;
        mainWindow.RespectCloseHotkey = !configuration.PreventEscapeClose;
        pluginInterface.UiBuilder.DisableCutsceneUiHide = configuration.ShowDuringCutscenes;
        configuration.Analytics.AnalyticsWindowVisible = analyticsWindow.IsOpen;
        configuration.Training.TrainingWindowVisible = trainingWindow.IsOpen;
        configuration.Overlay.IsVisible = overlayWindow.IsOpen;
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

    private void OpenChangelogUI() => changelogWindow.Toggle();

    private void OpenOverlayUI() => overlayWindow.Toggle();

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim().ToLowerInvariant();
        var spaceIdx = trimmed.IndexOf(' ');
        var subcommand = spaceIdx >= 0 ? trimmed[..spaceIdx] : trimmed;
        var subArg = spaceIdx >= 0 ? trimmed[(spaceIdx + 1)..].Trim() : string.Empty;

        switch (subcommand)
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

            case "hardcast":
                HandleHardcastCommand(subArg);
                break;

            default:
                mainWindow.Toggle();
                break;
        }
    }

    private void HandleHardcastCommand(string subArg)
    {
        var current = configuration.Resurrection.AllowHardcastRaise;
        bool newValue;

        switch (subArg)
        {
            case "on":
            case "enable":
            case "true":
                newValue = true;
                break;
            case "off":
            case "disable":
            case "false":
                newValue = false;
                break;
            case "":
            case "toggle":
                newValue = !current;
                break;
            default:
                chatGui.Print($"Usage: /olympus hardcast [on|off|toggle]. Currently {(current ? "on" : "off")}.");
                return;
        }

        configuration.Resurrection.AllowHardcastRaise = newValue;
        SaveConfiguration();
        var state = newValue ? "enabled" : "disabled";
        chatGui.Print($"Olympus hardcast raise {state}.");
        log.Info($"Hardcast raise {state}");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Always update debug service frame counter
            debugService.Update();

            // Always update shield tracking for accurate HP predictions
            shieldTrackingService.Update();

            // Update timeline service for sync and predictions
            timelineService.Update();

            // Refresh modifier-key state. Cheap (~3 lookups), runs every frame so
            // BurstHoldHelper sees the latest player intent before the rotation runs.
            modifierKeyService.Update();

            // Update combat state for analytics — must run before performanceTracker
            // and outside the dead-player gate so sessions end correctly when the
            // player dies mid-fight. Covers all 21 jobs (previously only healers).
            {
                var lp = objectTable.LocalPlayer;
                if (lp != null)
                {
                    var inCombat = (lp.StatusFlags & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) != 0;
                    combatEventService.UpdateCombatState(inCombat);
                }
                else
                {
                    combatEventService.UpdateCombatState(false);
                }
            }

            // Update performance analytics (tracks combat state independently)
            performanceTracker.Update();

            // Update training mode
            trainingService.Update();

            // Update coaching hints (v3.49.0)
            realTimeCoachingService.Update();
            hintOverlay.HandleInput();

            if (!clientState.IsLoggedIn)
                return;

            var localPlayer = objectTable.LocalPlayer;
            if (localPlayer == null)
                return;

            // Tincture automation: drive PullIntentService state machine and notify
            // ConsumableService of combat-state changes so the per-fight warning
            // throttle resets correctly. Runs outside the Enabled gate so the latch
            // state stays correct when re-enabled mid-fight, and outside the dead-player
            // gate so the combat-entry edge after a wipe correctly resets the latch.
            {
                var inCombatNow = (localPlayer.StatusFlags & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) != 0;
                consumableService.OnCombatStateChanged(inCombatNow);

                var (queuedId, queuedHostile) = ReadQueuedActionInfo();
                var castTargetHostile = false;
                if (localPlayer.IsCasting && localPlayer.CastTargetObjectId != 0)
                {
                    var castTargetObj = objectTable.SearchById(localPlayer.CastTargetObjectId);
                    castTargetHostile = castTargetObj is Dalamud.Game.ClientState.Objects.Types.IBattleNpc bn
                                        && bn.SubKind == (byte)Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Enemy;
                }

                pullIntentService.Update(
                    isPlayerCasting: localPlayer.IsCasting,
                    isCastTargetHostile: castTargetHostile,
                    queuedActionId: queuedId,
                    isQueuedActionHostile: queuedHostile,
                    isInCombat: inCombatNow,
                    utcNow: DateTime.UtcNow);
            }

            if (!configuration.Enabled)
                return;

            if (localPlayer.CurrentHp == 0)
                return;

            // Update party coordination service (heartbeat, cleanup)
            partyCoordinationService?.Update(
                localPlayer.EntityId,
                localPlayer.ClassJob.RowId,
                configuration.Enabled);

            // Track player-to-target distance for gap closer safety heuristics.
            gapCloserSafetyService.Update(localPlayer, targetManager.Target as IBattleChara);

            // Check if we have a rotation for the current job
            var jobId = localPlayer.ClassJob.RowId;
            if (!rotationManager.UpdateActiveRotation(jobId))
                return;

            rotationManager.Execute(localPlayer);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Error in OnFrameworkUpdate");
        }
    }

    /// <summary>
    /// Reads the currently queued action's ID and hostility for PullIntentService.
    /// Returns (null, false) if no action is queued or the lookup fails.
    /// QueuedActionId confirmed present on ActionManager at offset 0x70 in current FFXIVClientStructs.
    /// </summary>
    private unsafe (uint? queuedId, bool queuedHostile) ReadQueuedActionInfo()
    {
        var am = Olympus.Services.SafeGameAccess.GetActionManager(errorMetricsService);
        if (am == null) return (null, false);

        var queuedId = am->QueuedActionId;
        if (queuedId == 0) return (null, false);

        var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (sheet is null) return (queuedId, false);

        var rowOpt = sheet.GetRowOrDefault(queuedId);
        if (!rowOpt.HasValue) return (queuedId, false);

        return (queuedId, rowOpt.Value.CanTargetHostile);
    }

    public void Dispose()
    {
        // Save calibration data before shutdown
        HealingCalculator.SaveCalibration(configuration.Calibration);
        pluginInterface.SavePluginConfig(configuration);

        framework.Update -= OnFrameworkUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;
        combatEventService.OnAbilityUsed -= this.onAbilityUsedHandler;
        performanceTracker.OnSessionCompleted -= this.onSessionCompletedHandler;
        commandManager.RemoveHandler(CommandName);

        pluginInterface.UiBuilder.Draw -= DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;

        actionFeedWindow.Dispose();
        windowSystem.RemoveAllWindows();
        olympusIpc.Dispose();
        partyCoordinationIpc?.Dispose();
        fflogsService?.Dispose();
        telemetryService.Dispose();
        updateCheckerService.Dispose();

        // Dispose rotation manager (handles all instantiated rotations)
        rotationManager.Dispose();

        // Dispose event subscribers before the container disposes their event sources
        // (dotTrackingService and healingIntakeService subscribe to CombatEventService)
        dotTrackingService.Dispose();
        healingIntakeService.Dispose();

        // Dispose container-registered services (e.g., CombatEventService)
        serviceContainer?.Dispose();

        performanceTracker.Dispose();
        drawingService.Dispose();
        localization.Dispose();
    }
}
