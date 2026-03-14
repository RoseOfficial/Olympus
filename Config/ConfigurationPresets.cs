namespace Olympus.Config;

/// <summary>
/// Built-in configuration presets for different content types and playstyles.
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
    Casual,

    /// <summary>Safety first. Higher thresholds, defensive priority, resource reserves.</summary>
    Conservative,

    /// <summary>Middle-ground settings. Suitable for most content.</summary>
    Balanced,

    /// <summary>DPS maximization. Lower thresholds, offensive priority, no reserves.</summary>
    Aggressive,

    /// <summary>Timeline-aware. Pre-emptive abilities, burst window coordination.</summary>
    Proactive
}

/// <summary>
/// Job role categories for role-aware preset application.
/// </summary>
public enum JobRole
{
    Healer,
    Tank,
    MeleeDps,
    RangedDps,
    CasterDps
}

/// <summary>
/// Provides preset configurations for different content types and playstyles.
/// Presets modify behavior settings but preserve spell toggles and targeting preferences.
/// </summary>
public static class ConfigurationPresets
{
    /// <summary>
    /// Gets the job role for a given job ID.
    /// </summary>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>The job role, or null if not a combat job.</returns>
    public static JobRole? GetJobRole(uint jobId) => jobId switch
    {
        // Healers: WHM (24), SCH (28), AST (33), SGE (40)
        24 or 28 or 33 or 40 => JobRole.Healer,
        // Tanks: PLD (19), WAR (21), DRK (32), GNB (37)
        19 or 21 or 32 or 37 => JobRole.Tank,
        // Melee DPS: MNK (20), DRG (22), NIN (30), SAM (34), RPR (39), VPR (41)
        20 or 22 or 30 or 34 or 39 or 41 => JobRole.MeleeDps,
        // Ranged Physical: BRD (23), MCH (31), DNC (38)
        23 or 31 or 38 => JobRole.RangedDps,
        // Casters: BLM (25), SMN (27), RDM (35), PCT (42)
        25 or 27 or 35 or 42 => JobRole.CasterDps,
        _ => null
    };

    /// <summary>
    /// Gets a human-readable description for a preset.
    /// </summary>
    public static string GetDescription(ConfigurationPreset preset) => preset switch
    {
        ConfigurationPreset.Raid => "Balanced healing/DPS for 8-player raids. Co-healer aware, proactive cooldowns.",
        ConfigurationPreset.Dungeon => "Aggressive DPS for 4-player dungeons. Solo healer mode, reactive healing.",
        ConfigurationPreset.Casual => "Safe mode for easy content. Conservative thresholds, healing priority.",
        ConfigurationPreset.Conservative => "Safety first. Higher thresholds, defensive priority, resource reserves.",
        ConfigurationPreset.Balanced => "Middle-ground settings. Suitable for most content.",
        ConfigurationPreset.Aggressive => "DPS maximization. Lower thresholds, offensive priority, no reserves.",
        ConfigurationPreset.Proactive => "Timeline-aware. Pre-emptive abilities, burst window coordination.",
        _ => "Custom settings. Manually configured."
    };

    /// <summary>
    /// Applies a preset to the configuration.
    /// Does not modify spell toggles, targeting, debug, or calibration settings.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="preset">The preset to apply.</param>
    /// <param name="currentRole">Optional current job role for role-aware presets.</param>
    public static void ApplyPreset(Configuration config, ConfigurationPreset preset, JobRole? currentRole = null)
    {
        if (preset == ConfigurationPreset.Custom)
            return;

        switch (preset)
        {
            // Content-type presets (existing)
            case ConfigurationPreset.Raid:
                ApplyRaidPreset(config);
                break;
            case ConfigurationPreset.Dungeon:
                ApplyDungeonPreset(config);
                break;
            case ConfigurationPreset.Casual:
                ApplyCasualPreset(config);
                break;
            // Playstyle presets (new, role-aware)
            case ConfigurationPreset.Conservative:
                ApplyConservativePreset(config, currentRole);
                break;
            case ConfigurationPreset.Balanced:
                ApplyBalancedPreset(config, currentRole);
                break;
            case ConfigurationPreset.Aggressive:
                ApplyAggressivePreset(config, currentRole);
                break;
            case ConfigurationPreset.Proactive:
                ApplyProactivePreset(config, currentRole);
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
        config.Healing.AoEHealHpThreshold = 0.85f;
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
        config.Healing.AoEHealHpThreshold = 0.90f;
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
        config.Healing.AoEHealHpThreshold = 0.90f;
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

    /// <summary>
    /// Conservative preset: Safety-focused settings for all roles.
    /// Higher thresholds, defensive priority, resource reserves.
    /// </summary>
    private static void ApplyConservativePreset(Configuration config, JobRole? role)
    {
        // All roles - conservative movement tolerance
        config.MovementTolerance = 0.15f;

        // Healers
        if (role is null or JobRole.Healer)
        {
            config.Damage.DpsPriority = DpsPriorityMode.HealFirst;
            config.Healing.BenedictionEmergencyThreshold = 0.40f;
            config.Healing.OgcdEmergencyThreshold = 0.60f;
            config.Healing.GcdEmergencyThreshold = 0.50f;
            config.Healing.LilyStrategy = LilyGenerationStrategy.Conservative;
            config.Healing.EnablePreemptiveHealing = true;
            config.Healing.EnableMechanicAwareness = true;
            config.Defensive.DefensiveCooldownThreshold = 0.70f;
            config.Scholar.AetherflowReserve = 2;
            config.Scholar.EnableEnergyDrain = false;
            config.Sage.AddersgallReserve = 2;
        }

        // Tanks
        if (role is null or JobRole.Tank)
        {
            config.Tank.MitigationThreshold = 0.80f;
            config.Tank.UseRampartOnCooldown = true;
            config.Tank.SheltronMinGauge = 30;
        }

        // Party Coordination - wider overlap window to avoid stacking
        config.PartyCoordination.CooldownOverlapWindowSeconds = 4.0f;
    }

    /// <summary>
    /// Balanced preset: Middle-ground settings for all roles.
    /// Suitable for most content types.
    /// </summary>
    private static void ApplyBalancedPreset(Configuration config, JobRole? role)
    {
        // All roles - moderate movement tolerance
        config.MovementTolerance = 0.10f;

        // Healers
        if (role is null or JobRole.Healer)
        {
            config.Damage.DpsPriority = DpsPriorityMode.Balanced;
            config.Healing.BenedictionEmergencyThreshold = 0.30f;
            config.Healing.OgcdEmergencyThreshold = 0.50f;
            config.Healing.GcdEmergencyThreshold = 0.40f;
            config.Healing.LilyStrategy = LilyGenerationStrategy.Balanced;
            config.Healing.EnablePreemptiveHealing = true;
            config.Healing.EnableMechanicAwareness = true;
            config.Defensive.DefensiveCooldownThreshold = 0.75f;
            config.Scholar.AetherflowReserve = 1;
            config.Scholar.EnableEnergyDrain = true;
            config.Sage.AddersgallReserve = 1;
        }

        // Tanks
        if (role is null or JobRole.Tank)
        {
            config.Tank.MitigationThreshold = 0.70f;
            config.Tank.UseRampartOnCooldown = false;
            config.Tank.SheltronMinGauge = 50;
        }

        // Party Coordination - moderate overlap window
        config.PartyCoordination.CooldownOverlapWindowSeconds = 3.0f;
    }

    /// <summary>
    /// Aggressive preset: DPS maximization for all roles.
    /// Lower thresholds, offensive priority, no resource reserves.
    /// </summary>
    private static void ApplyAggressivePreset(Configuration config, JobRole? role)
    {
        // All roles - tight movement tolerance for maximum uptime
        config.MovementTolerance = 0.05f;

        // Healers
        if (role is null or JobRole.Healer)
        {
            config.Damage.DpsPriority = DpsPriorityMode.DpsFirst;
            config.Healing.BenedictionEmergencyThreshold = 0.25f;
            config.Healing.OgcdEmergencyThreshold = 0.40f;
            config.Healing.GcdEmergencyThreshold = 0.30f;
            config.Healing.LilyStrategy = LilyGenerationStrategy.Aggressive;
            config.Healing.EnablePreemptiveHealing = false;
            config.Healing.EnableMechanicAwareness = false;
            config.Defensive.DefensiveCooldownThreshold = 0.85f;
            config.Scholar.AetherflowReserve = 0;
            config.Scholar.EnableEnergyDrain = true;
            config.Sage.AddersgallReserve = 0;
        }

        // Tanks
        if (role is null or JobRole.Tank)
        {
            config.Tank.MitigationThreshold = 0.60f;
            config.Tank.UseRampartOnCooldown = false;
            config.Tank.SheltronMinGauge = 50;
        }

        // Party Coordination - narrower overlap window for more cooldown usage
        config.PartyCoordination.CooldownOverlapWindowSeconds = 2.0f;
    }

    /// <summary>
    /// Proactive preset: Timeline-aware settings for all roles.
    /// Pre-emptive ability usage, burst window coordination.
    /// </summary>
    private static void ApplyProactivePreset(Configuration config, JobRole? role)
    {
        // All roles - moderate movement tolerance
        config.MovementTolerance = 0.10f;

        // Healers
        if (role is null or JobRole.Healer)
        {
            config.Damage.DpsPriority = DpsPriorityMode.Balanced;
            config.Healing.EnablePreemptiveHealing = true;
            config.Healing.EnableMechanicAwareness = true;
            config.Healing.BenedictionEmergencyThreshold = 0.30f;
            config.Healing.OgcdEmergencyThreshold = 0.50f;
            config.Healing.GcdEmergencyThreshold = 0.40f;
            config.Healing.LilyStrategy = LilyGenerationStrategy.Balanced;
            config.Defensive.EnableProactiveCooldowns = true;
            config.Defensive.UseDynamicDefensiveThresholds = true;
            config.Scholar.AetherflowReserve = 1;
            config.Sage.AddersgallReserve = 1;
        }

        // Tanks
        if (role is null or JobRole.Tank)
        {
            config.Tank.EnableMitigation = true;
            config.Tank.MitigationThreshold = 0.75f;
            config.Tank.UseRampartOnCooldown = false; // Save for tankbusters
        }

        // Party Coordination - enable burst awareness and buff coordination
        config.PartyCoordination.EnableHealerBurstAwareness = true;
        config.PartyCoordination.EnableRaidBuffCoordination = true;
        config.PartyCoordination.CooldownOverlapWindowSeconds = 3.0f;
    }
}
