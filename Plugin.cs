using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Calculation;
using Olympus.Services.Debuff;
using Olympus.Services.Debug;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Windows;

namespace Olympus;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginVersion = "1.5.10";
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
    private readonly TargetingService targetingService;
    private readonly HpPredictionService hpPredictionService;
    private readonly ActionService actionService;
    private readonly PlayerStatsService playerStatsService;
    private readonly HealingSpellSelector healingSpellSelector;
    private readonly SpellStatusService spellStatusService;
    private readonly DebugService debugService;
    private readonly DebuffDetectionService debuffDetectionService;
    private readonly RotationManager rotationManager;
    private readonly Apollo apollo;

    private readonly WindowSystem windowSystem = new("Olympus");
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly DebugWindow debugWindow;

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
        this.targetingService = new TargetingService(objectTable, partyList, targetManager, configuration);

        // New action system services
        this.hpPredictionService = new HpPredictionService(combatEventService);
        this.actionService = new ActionService(actionTracker);
        this.playerStatsService = new PlayerStatsService(log, dataManager);

        // Healing spell selector (evaluates all heals and picks the best)
        this.healingSpellSelector = new HealingSpellSelector(
            actionService,
            playerStatsService,
            hpPredictionService,
            configuration);

        // Spell status service (provides real-time status of all WHM spells)
        this.spellStatusService = new SpellStatusService(actionService);

        // Debuff detection service for Esuna
        this.debuffDetectionService = new DebuffDetectionService(dataManager);

        this.apollo = new Apollo(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            healingSpellSelector,
            debuffDetectionService);

        // Register rotations with the rotation manager
        this.rotationManager = new RotationManager();
        this.rotationManager.Register(apollo);

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
            dataManager);

        this.configWindow = new ConfigWindow(configuration, SaveConfiguration);
        this.mainWindow = new MainWindow(configuration, SaveConfiguration, OpenConfigUI, OpenDebugUI, PluginVersion);
        this.debugWindow = new DebugWindow(debugService, configuration);

        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(debugWindow);

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
        damageIntakeService.Dispose();
        hpPredictionService.Dispose();
        combatEventService.Dispose();
    }
}
