namespace Olympus.Config;

/// <summary>
/// Built-in configuration presets for different content types.
/// </summary>
public enum ConfigurationPreset
{
    /// <summary>Custom/manual configuration.</summary>
    Custom,

    /// <summary>Optimized for 8-player raids with co-healer support.</summary>
    Raid,

    /// <summary>Optimized for 4-player dungeons with aggressive DPS.</summary>
    Dungeon,

    /// <summary>Safe mode for casual content with conservative healing.</summary>
    Casual
}

/// <summary>
/// Provides preset configurations for different content types.
/// Presets modify behavior settings but preserve spell toggles and targeting preferences.
/// </summary>
public static class ConfigurationPresets
{
    /// <summary>
    /// Gets a human-readable description for a preset.
    /// </summary>
    public static string GetDescription(ConfigurationPreset preset) => preset switch
    {
        ConfigurationPreset.Raid => "Balanced healing/DPS for 8-player raids. Co-healer aware, proactive cooldowns.",
        ConfigurationPreset.Dungeon => "Aggressive DPS for 4-player dungeons. Solo healer mode, reactive healing.",
        ConfigurationPreset.Casual => "Safe mode for easy content. Conservative thresholds, healing priority.",
        _ => "Custom settings. Manually configured."
    };

    /// <summary>
    /// Applies a preset to the configuration.
    /// Does not modify spell toggles, targeting, debug, or calibration settings.
    /// </summary>
    public static void ApplyPreset(Configuration config, ConfigurationPreset preset)
    {
        if (preset == ConfigurationPreset.Custom)
            return;

        switch (preset)
        {
            case ConfigurationPreset.Raid:
                ApplyRaidPreset(config);
                break;
            case ConfigurationPreset.Dungeon:
                ApplyDungeonPreset(config);
                break;
            case ConfigurationPreset.Casual:
                ApplyCasualPreset(config);
                break;
        }

        config.ActivePreset = preset;
    }

    /// <summary>
    /// Raid preset: Balanced healing/DPS for 8-player content.
    /// Co-healer aware, proactive cooldowns, moderate thresholds.
    /// </summary>
    private static void ApplyRaidPreset(Configuration config)
    {
        // Core behavior - balanced
        config.Damage.DpsPriority = DpsPriorityMode.Balanced;
        config.MovementTolerance = 0.1f;

        // Healing behavior - co-healer aware, proactive
        config.Healing.EnableCoHealerAwareness = true;
        config.Healing.EnableMechanicAwareness = true;
        config.Healing.EnablePreemptiveHealing = true;
        config.Healing.BenedictionEmergencyThreshold = 0.25f;
        config.Healing.OgcdEmergencyThreshold = 0.45f;
        config.Healing.GcdEmergencyThreshold = 0.35f;
        config.Healing.AoEHealMinTargets = 3;
        config.Healing.LilyStrategy = LilyGenerationStrategy.Balanced;
        config.Healing.TriagePreset = TriagePreset.ShieldAware;

        // Defensive - proactive
        config.Defensive.EnableProactiveCooldowns = true;
        config.Defensive.UseDynamicDefensiveThresholds = true;
        config.Defensive.DefensiveCooldownThreshold = 0.75f;

        // Resurrection - balanced
        config.Resurrection.RaiseMode = RaiseExecutionMode.Balanced;
        config.Resurrection.AllowHardcastRaise = false;

        // Damage - standard AoE threshold
        config.Damage.AoEDamageMinTargets = 3;

        // Scholar - balanced Aetherflow
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.Balanced;
        config.Scholar.AetherflowReserve = 1;
        config.Scholar.EnableEnergyDrain = true;

        // Astrologian - DPS-focused cards
        config.Astrologian.CardStrategy = CardPlayStrategy.DpsFocused;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;

        // Sage - moderate Addersgall reserve
        config.Sage.AddersgallReserve = 1;
        config.Sage.PreventAddersgallCap = true;
    }

    /// <summary>
    /// Dungeon preset: Aggressive DPS for 4-player content.
    /// Solo healer mode, reactive healing, lower AoE thresholds.
    /// </summary>
    private static void ApplyDungeonPreset(Configuration config)
    {
        // Core behavior - aggressive DPS
        config.Damage.DpsPriority = DpsPriorityMode.DpsFirst;
        config.MovementTolerance = 0.05f;

        // Healing behavior - reactive, solo healer
        config.Healing.EnableCoHealerAwareness = false;
        config.Healing.EnableMechanicAwareness = false;
        config.Healing.EnablePreemptiveHealing = false;
        config.Healing.BenedictionEmergencyThreshold = 0.35f;
        config.Healing.OgcdEmergencyThreshold = 0.55f;
        config.Healing.GcdEmergencyThreshold = 0.45f;
        config.Healing.AoEHealMinTargets = 2;
        config.Healing.LilyStrategy = LilyGenerationStrategy.Aggressive;
        config.Healing.TriagePreset = TriagePreset.Balanced;

        // Defensive - reactive
        config.Defensive.EnableProactiveCooldowns = false;
        config.Defensive.UseDynamicDefensiveThresholds = false;
        config.Defensive.DefensiveCooldownThreshold = 0.85f;

        // Resurrection - prioritize getting party back up
        config.Resurrection.RaiseMode = RaiseExecutionMode.RaiseFirst;
        config.Resurrection.AllowHardcastRaise = true;

        // Damage - lower AoE threshold for smaller pulls
        config.Damage.AoEDamageMinTargets = 2;

        // Scholar - aggressive DPS
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.AggressiveDps;
        config.Scholar.AetherflowReserve = 0;
        config.Scholar.EnableEnergyDrain = true;

        // Astrologian - DPS cards, use Minor Arcana freely
        config.Astrologian.CardStrategy = CardPlayStrategy.DpsFocused;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.OnCooldown;

        // Sage - no Addersgall reserve, maximize throughput
        config.Sage.AddersgallReserve = 0;
        config.Sage.PreventAddersgallCap = true;
    }

    /// <summary>
    /// Casual preset: Safe mode for easy content.
    /// Conservative thresholds, healing priority, safety first.
    /// </summary>
    private static void ApplyCasualPreset(Configuration config)
    {
        // Core behavior - safety first
        config.Damage.DpsPriority = DpsPriorityMode.HealFirst;
        config.MovementTolerance = 0.15f;

        // Healing behavior - conservative, proactive
        config.Healing.EnableCoHealerAwareness = true;
        config.Healing.EnableMechanicAwareness = true;
        config.Healing.EnablePreemptiveHealing = true;
        config.Healing.BenedictionEmergencyThreshold = 0.40f;
        config.Healing.OgcdEmergencyThreshold = 0.60f;
        config.Healing.GcdEmergencyThreshold = 0.50f;
        config.Healing.AoEHealMinTargets = 3;
        config.Healing.LilyStrategy = LilyGenerationStrategy.Conservative;
        config.Healing.TriagePreset = TriagePreset.TankFocus;

        // Defensive - proactive
        config.Defensive.EnableProactiveCooldowns = true;
        config.Defensive.UseDynamicDefensiveThresholds = true;
        config.Defensive.DefensiveCooldownThreshold = 0.70f;

        // Resurrection - don't sacrifice party stability
        config.Resurrection.RaiseMode = RaiseExecutionMode.HealFirst;
        config.Resurrection.AllowHardcastRaise = false;

        // Damage - standard threshold
        config.Damage.AoEDamageMinTargets = 3;

        // Scholar - healing priority, reserve stacks
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.HealingPriority;
        config.Scholar.AetherflowReserve = 2;
        config.Scholar.EnableEnergyDrain = false;

        // Astrologian - safety focused
        config.Astrologian.CardStrategy = CardPlayStrategy.SafetyFocused;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;

        // Sage - reserve Addersgall for emergencies
        config.Sage.AddersgallReserve = 2;
        config.Sage.PreventAddersgallCap = false;
    }
}
