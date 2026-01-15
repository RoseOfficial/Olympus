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
using Olympus.Windows;

namespace Olympus;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginVersion = "1.22.0";
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

    // Tank services
    private readonly EnmityService enmityService;
    private readonly TankCooldownService tankCooldownService;

    private readonly WindowSystem windowSystem = new("Olympus");
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly DebugWindow debugWindow;
    private readonly WelcomeWindow welcomeWindow;
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
        ITargetManager targetManager)
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

        // Create and register rotation modules via factory
        this.rotationManager = new RotationManager();
        this.apollo = CreateApolloRotation();
        this.athena = CreateAthenaRotation();
        this.astraea = CreateAstraeaRotation();
        this.asclepius = CreateAsclepiusRotation();
        this.themis = CreateThemisRotation();
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
            apollo,
            objectTable,
            dataManager,
            athena);

        this.configWindow = new ConfigWindow(configuration, SaveConfiguration);
        this.mainWindow = new MainWindow(configuration, SaveConfiguration, OpenConfigUI, OpenDebugUI, PluginVersion);
        this.debugWindow = new DebugWindow(debugService, configuration);
        this.welcomeWindow = new WelcomeWindow(configuration, SaveConfiguration);

        // Telemetry service for anonymous usage tracking
        this.telemetryService = new TelemetryService(configuration, log);
        
        // IPC interface for external plugin integration
        this.olympusIpc = new OlympusIpc(pluginInterface, configuration, SaveConfiguration, log);

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(debugWindow);
        windowSystem.AddWindow(welcomeWindow);

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

        // Send anonymous telemetry ping (fire-and-forget)
        telemetryService.SendStartupPing(PluginVersion);
    }

    private void SaveConfiguration()
    {
        configuration.MainWindowVisible = mainWindow.IsOpen;
        configuration.Debug.DebugWindowVisible = debugWindow.IsOpen;
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

    private void OnCommand(string command, string args)
    {
        var arg = args.Trim().ToLowerInvariant();

        switch (arg)
        {
            case "toggle":
                configuration.Enabled = !configuration.Enabled;
                SaveConfiguration();
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

        if (!configuration.Enabled)
            return;

        if (!clientState.IsLoggedIn)
            return;

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer == null)
            return;

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
            shieldTrackingService);
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
            shieldTrackingService);
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
            shieldTrackingService);
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
            shieldTrackingService);
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
            tankCooldownService);
    }

    #endregion

    public void Dispose()
    {
        // Save calibration data before shutdown
        HealingCalculator.SaveCalibration(configuration.Calibration);
        pluginInterface.SavePluginConfig(configuration);

        framework.Update -= OnFrameworkUpdate;
        commandManager.RemoveHandler(CommandName);

        pluginInterface.UiBuilder.Draw -= DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;

        windowSystem.RemoveAllWindows();
        olympusIpc.Dispose();
        telemetryService.Dispose();

        // Dispose healer rotations (they have event subscriptions)
        apollo.Dispose();
        athena.Dispose();
        astraea.Dispose();
        asclepius.Dispose();

        damageIntakeService.Dispose();
        healingIntakeService.Dispose();
        hpPredictionService.Dispose();
        combatEventService.Dispose();
    }
}
