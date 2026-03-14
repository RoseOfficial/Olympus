using System;
using Dalamud.Configuration;
using Olympus.Config;
using Olympus.Config.DPS;
using Olympus.Services.Targeting;

namespace Olympus;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    // Runtime state
    public bool Enabled { get; set; } = false;
    public bool MainWindowVisible { get; set; } = true;
    public bool IsDebugWindowOpen { get; set; } = false;

    /// <summary>
    /// When true, pressing Escape will not close the Olympus main window.
    /// </summary>
    public bool PreventEscapeClose { get; set; } = false;

    /// <summary>
    /// When true, Olympus windows remain visible during in-game cutscenes.
    /// Default false (windows hide during cutscenes).
    /// </summary>
    public bool ShowDuringCutscenes { get; set; } = false;

    // Community & telemetry
    public bool HasSeenWelcome { get; set; } = false;
    public bool TelemetryEnabled { get; set; } = true;
    public string TelemetryEndpoint { get; set; } = "https://olympus-telemetry.christopherscottkeller.workers.dev/";

    /// <summary>
    /// Optional language override. When set, uses this language instead of the game client language.
    /// Valid values: "en", "ja", "de", "fr", "zh", "tw", "ko", "es", "pt", "ru"
    /// Empty string or null means use game client language.
    /// </summary>
    public string LanguageOverride { get; set; } = string.Empty;

    /// <summary>
    /// The currently active configuration preset.
    /// Set to Custom when user modifies individual settings after applying a preset.
    /// </summary>
    public ConfigurationPreset ActivePreset { get; set; } = ConfigurationPreset.Custom;

    // General behavior
    /// <summary>
    /// How long to wait after movement stops before casting (in seconds).
    /// Higher values = more conservative (safer), lower = more aggressive (faster DPS).
    /// Valid range: 0.0 to 2.0 seconds.
    /// </summary>
    private float _movementTolerance = 0.1f;
    public float MovementTolerance
    {
        get => _movementTolerance;
        set => _movementTolerance = Math.Clamp(value, 0.0f, 2.0f);
    }

    /// <summary>
    /// When true, Olympus will start executing the rotation when auto-attack is active
    /// on the target, even before the InCombat flag is set.
    /// </summary>
    public bool EnableOnAutoAttack { get; set; } = false;

    // Master category toggles
    public bool EnableHealing { get; set; } = true;
    public bool EnableDamage { get; set; } = true;
    public bool EnableDoT { get; set; } = true;

    // Nested configuration groups
    public HealingConfig Healing { get; set; } = new();
    public DamageConfig Damage { get; set; } = new();
    public DotConfig Dot { get; set; } = new();
    public DefensiveConfig Defensive { get; set; } = new();
    public BuffConfig Buffs { get; set; } = new();
    public ResurrectionConfig Resurrection { get; set; } = new();
    public TargetingConfig Targeting { get; set; } = new();
    public RoleActionConfig RoleActions { get; set; } = new();
    public DebugConfig Debug { get; set; } = new();
    public CalibrationConfig Calibration { get; set; } = new();
    public AnalyticsConfig Analytics { get; set; } = new();
    public FFlogsConfig FFLogs { get; set; } = new();
    public TrainingConfig Training { get; set; } = new();
    public OverlayConfig Overlay { get; set; } = new();
    public DrawHelperConfig DrawHelper { get; set; } = new();

    // Job-specific configuration - Healers
    public ScholarConfig Scholar { get; set; } = new();
    public AstrologianConfig Astrologian { get; set; } = new();
    public SageConfig Sage { get; set; } = new();

    // Party coordination (multi-Olympus IPC)
    public PartyCoordinationConfig PartyCoordination { get; set; } = new();

    // Role-specific configuration - Tanks
    public TankConfig Tank { get; set; } = new();

    // Job-specific configuration - Melee DPS
    public DragoonConfig Dragoon { get; set; } = new();
    public NinjaConfig Ninja { get; set; } = new();
    public SamuraiConfig Samurai { get; set; } = new();
    public MonkConfig Monk { get; set; } = new();
    public ReaperConfig Reaper { get; set; } = new();
    public ViperConfig Viper { get; set; } = new();

    // Job-specific configuration - Ranged Physical DPS
    public MachinistConfig Machinist { get; set; } = new();
    public BardConfig Bard { get; set; } = new();
    public DancerConfig Dancer { get; set; } = new();

    // Job-specific configuration - Casters
    public BlackMageConfig BlackMage { get; set; } = new();
    public SummonerConfig Summoner { get; set; } = new();
    public RedMageConfig RedMage { get; set; } = new();
    public PictomancerConfig Pictomancer { get; set; } = new();

    /// <summary>
    /// Resets all configuration values to their defaults.
    /// Preserves Enabled state and window visibility settings.
    /// </summary>
    public void ResetToDefaults()
    {
        // Preserve runtime state
        var wasEnabled = Enabled;
        var mainVisible = MainWindowVisible;
        var analyticsVisible = Analytics.AnalyticsWindowVisible;
        var trainingVisible = Training.TrainingWindowVisible;
        var seenWelcome = HasSeenWelcome;
        var languageOverride = LanguageOverride;
        var preventEscapeClose = PreventEscapeClose;
        var showDuringCutscenes = ShowDuringCutscenes;

        // Reset general behavior
        MovementTolerance = 0.1f;

        // Reset master toggles
        EnableHealing = true;
        EnableDamage = true;
        EnableDoT = true;

        // Reset all nested configs to fresh instances
        Healing = new HealingConfig();
        Damage = new DamageConfig();
        Dot = new DotConfig();
        Defensive = new DefensiveConfig();
        Buffs = new BuffConfig();
        Resurrection = new ResurrectionConfig();
        Targeting = new TargetingConfig();
        RoleActions = new RoleActionConfig();
        Debug = new DebugConfig();
        Calibration = new CalibrationConfig();
        Analytics = new AnalyticsConfig();
        FFLogs = new FFlogsConfig();
        Training = new TrainingConfig();
        Overlay = new OverlayConfig();
        DrawHelper = new DrawHelperConfig();
        Scholar = new ScholarConfig();
        Astrologian = new AstrologianConfig();
        Sage = new SageConfig();
        Tank = new TankConfig();
        PartyCoordination = new PartyCoordinationConfig();

        // Reset DPS configs
        Dragoon = new DragoonConfig();
        Ninja = new NinjaConfig();
        Samurai = new SamuraiConfig();
        Monk = new MonkConfig();
        Reaper = new ReaperConfig();
        Viper = new ViperConfig();
        Machinist = new MachinistConfig();
        Bard = new BardConfig();
        Dancer = new DancerConfig();
        BlackMage = new BlackMageConfig();
        Summoner = new SummonerConfig();
        RedMage = new RedMageConfig();
        Pictomancer = new PictomancerConfig();

        // Reset telemetry to defaults
        TelemetryEnabled = true;
        TelemetryEndpoint = "https://olympus-telemetry.christopherscottkeller.workers.dev/";

        // Restore preserved values
        Enabled = wasEnabled;
        MainWindowVisible = mainVisible;
        Analytics.AnalyticsWindowVisible = analyticsVisible;
        Training.TrainingWindowVisible = trainingVisible;
        HasSeenWelcome = seenWelcome;
        LanguageOverride = languageOverride;
        PreventEscapeClose = preventEscapeClose;
        ShowDuringCutscenes = showDuringCutscenes;
    }
}
