using System;
using Dalamud.Configuration;
using Olympus.Config;
using Olympus.Services.Targeting;

namespace Olympus;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    // Runtime state
    public bool Enabled { get; set; } = false;
    public bool MainWindowVisible { get; set; } = true;
    public bool IsDebugWindowOpen { get; set; } = false;

    // Community & telemetry
    public bool HasSeenWelcome { get; set; } = false;
    public bool TelemetryEnabled { get; set; } = true;
    public string TelemetryEndpoint { get; set; } = "https://olympus-telemetry.christopherscottkeller.workers.dev/";

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

    // Job-specific configuration
    public ScholarConfig Scholar { get; set; } = new();

    /// <summary>
    /// Resets all configuration values to their defaults.
    /// Preserves Enabled state and window visibility settings.
    /// </summary>
    public void ResetToDefaults()
    {
        // Preserve runtime state
        var wasEnabled = Enabled;
        var mainVisible = MainWindowVisible;
        var debugVisible = Debug.DebugWindowVisible;
        var seenWelcome = HasSeenWelcome;

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
        Scholar = new ScholarConfig();

        // Reset telemetry to defaults
        TelemetryEnabled = true;
        TelemetryEndpoint = "https://olympus-telemetry.christopherscottkeller.workers.dev/";

        // Restore preserved values
        Enabled = wasEnabled;
        MainWindowVisible = mainVisible;
        Debug.DebugWindowVisible = debugVisible;
        HasSeenWelcome = seenWelcome;
    }
}
