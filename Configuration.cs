using System.Collections.Generic;
using Dalamud.Configuration;
using Olympus.Services.Targeting;

namespace Olympus;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = false;

    public bool MainWindowVisible { get; set; } = true;

    // Category toggles (master switches)
    public bool EnableHealing { get; set; } = true;
    public bool EnableDamage { get; set; } = true;
    public bool EnableDoT { get; set; } = true;

    // Individual healing spells
    public bool EnableCure { get; set; } = true;
    public bool EnableCureII { get; set; } = true;

    // AoE healing spells
    public bool EnableMedica { get; set; } = true;
    public bool EnableMedicaII { get; set; } = true;
    public bool EnableMedicaIII { get; set; } = true;
    public bool EnableCureIII { get; set; } = true;

    // Lily heals (Afflatus)
    public bool EnableAfflatusSolace { get; set; } = true;
    public bool EnableAfflatusRapture { get; set; } = true;

    // HoT heals
    public bool EnableRegen { get; set; } = true;

    // oGCD heals
    public bool EnableTetragrammaton { get; set; } = true;
    public bool EnableBenediction { get; set; } = true;
    public bool EnableAssize { get; set; } = true;
    public bool EnableAsylum { get; set; } = true;
    public bool EnablePresenceOfMind { get; set; } = true;
    public bool EnableThinAir { get; set; } = true;

    // Movement abilities
    public bool EnableAetherialShift { get; set; } = true;

    // Blood Lily damage
    public bool EnableAfflatusMisery { get; set; } = true;

    // Defensive oGCDs
    public bool EnableDivineBenison { get; set; } = true;
    public bool EnablePlenaryIndulgence { get; set; } = true;
    public bool EnableTemperance { get; set; } = true;
    public bool EnableAquaveil { get; set; } = true;
    public bool EnableLiturgyOfTheBell { get; set; } = true;
    public bool EnableDivineCaress { get; set; } = true;

    /// <summary>
    /// HP percentage threshold to use defensive cooldowns proactively.
    /// When party average HP falls below this, start using mitigation.
    /// Default 0.80 means use when average HP &lt; 80%.
    /// </summary>
    public float DefensiveCooldownThreshold { get; set; } = 0.80f;

    /// <summary>
    /// Use defensive cooldowns during AoE heals for synergy.
    /// E.g., Plenary Indulgence before Medica/Cure III.
    /// </summary>
    public bool UseDefensivesWithAoEHeals { get; set; } = true;

    /// <summary>
    /// HP percentage threshold for Benediction (emergency heal).
    /// Only use Benediction when target HP is below this threshold.
    /// Default 0.30 means only use when below 30% HP.
    /// </summary>
    public float BenedictionEmergencyThreshold { get; set; } = 0.30f;

    // AoE healing settings
    /// <summary>
    /// Minimum number of party members below threshold to trigger AoE healing.
    /// Default 3 means use AoE heal when 3+ party members need healing.
    /// </summary>
    public int AoEHealMinTargets { get; set; } = 3;

    // Individual damage spells (Stone/Glare progression)
    public bool EnableStone { get; set; } = true;
    public bool EnableStoneII { get; set; } = true;
    public bool EnableStoneIII { get; set; } = true;
    public bool EnableStoneIV { get; set; } = true;
    public bool EnableGlare { get; set; } = true;
    public bool EnableGlareIII { get; set; } = true;
    public bool EnableGlareIV { get; set; } = true;

    // AoE damage spells (Holy progression)
    public bool EnableHoly { get; set; } = true;
    public bool EnableHolyIII { get; set; } = true;

    /// <summary>
    /// Minimum number of enemies in range to trigger AoE damage (Holy).
    /// Default 3 means use Holy when 3+ enemies are within 8y radius.
    /// </summary>
    public int AoEDamageMinTargets { get; set; } = 3;

    // Individual DoT spells (Aero/Dia progression)
    public bool EnableAero { get; set; } = true;
    public bool EnableAeroII { get; set; } = true;
    public bool EnableDia { get; set; } = true;

    // Resurrection settings
    /// <summary>
    /// Enable automatic resurrection of dead party members.
    /// </summary>
    public bool EnableRaise { get; set; } = true;

    /// <summary>
    /// Allow hardcasting Raise when Swiftcast is on cooldown.
    /// Hardcast Raise takes 8 seconds and should only be used when safe.
    /// </summary>
    public bool AllowHardcastRaise { get; set; } = false;

    /// <summary>
    /// Minimum MP percentage required before attempting to raise (0.0 - 1.0).
    /// Default 0.25 means 25% MP minimum (2400 MP for Raise + buffer).
    /// </summary>
    public float RaiseMpThreshold { get; set; } = 0.25f;

    // Targeting settings
    /// <summary>
    /// Strategy for selecting enemy targets during combat.
    /// </summary>
    public EnemyTargetingStrategy EnemyStrategy { get; set; } = EnemyTargetingStrategy.LowestHp;

    /// <summary>
    /// When using TankAssist strategy, fall back to LowestHp if no tank target is found.
    /// </summary>
    public bool UseTankAssistFallback { get; set; } = true;

    /// <summary>
    /// How long to cache valid enemy list in milliseconds.
    /// Higher values improve performance but may delay target switching.
    /// </summary>
    public int TargetCacheTtlMs { get; set; } = 100;

    // Role Actions - Esuna
    /// <summary>
    /// Enable automatic Esuna usage to cleanse debuffs.
    /// </summary>
    public bool EnableEsuna { get; set; } = true;

    /// <summary>
    /// Minimum debuff priority to auto-cleanse (0-3).
    /// 0 = Lethal only (Doom/Throttle)
    /// 1 = High+ (also Vulnerability Up)
    /// 2 = Medium+ (also Paralysis/Silence/Pacification)
    /// 3 = All dispellable debuffs
    /// </summary>
    public int EsunaPriorityThreshold { get; set; } = 2;

    // Role Actions - Surecast
    /// <summary>
    /// Enable Surecast role action.
    /// </summary>
    public bool EnableSurecast { get; set; } = false;

    /// <summary>
    /// Surecast usage mode:
    /// 0 = Manual only (never auto-use)
    /// 1 = Use on cooldown in combat
    /// </summary>
    public int SurecastMode { get; set; } = 0;

    // Role Actions - Rescue
    /// <summary>
    /// Enable Rescue role action. Disabled by default - use with extreme caution.
    /// Rescue can kill party members if used incorrectly.
    /// </summary>
    public bool EnableRescue { get; set; } = false;

    /// <summary>
    /// Rescue mode:
    /// 0 = Manual only (never auto-use)
    /// Note: Automatic rescue is not implemented due to extreme risk.
    /// </summary>
    public int RescueMode { get; set; } = 0;

    // Debug settings
    public bool DebugWindowVisible { get; set; } = false;
    public int ActionHistorySize { get; set; } = 100;

    /// <summary>
    /// Visibility settings for debug window sections.
    /// Key is section name, value is whether it's visible.
    /// </summary>
    public Dictionary<string, bool> DebugSectionVisibility { get; set; } = new()
    {
        // Overview tab
        ["GcdPlanning"] = true,
        ["QuickStats"] = true,

        // Healing tab
        ["HpPrediction"] = true,
        ["AoEHealing"] = true,
        ["RecentHeals"] = true,
        ["ShadowHp"] = true,

        // Actions tab
        ["GcdDetails"] = true,
        ["SpellUsage"] = true,
        ["ActionHistory"] = true,

        // Performance tab
        ["Statistics"] = true,
        ["Downtime"] = true,
    };

    /// <summary>
    /// Resets all configuration values to their defaults.
    /// Preserves Enabled state and window visibility settings.
    /// </summary>
    public void ResetToDefaults()
    {
        // Preserve runtime state
        var wasEnabled = Enabled;
        var mainVisible = MainWindowVisible;
        var debugVisible = DebugWindowVisible;

        // Create a fresh instance to get all default values
        var defaults = new Configuration();

        // Copy all default values from fresh instance
        // Category toggles
        EnableHealing = defaults.EnableHealing;
        EnableDamage = defaults.EnableDamage;
        EnableDoT = defaults.EnableDoT;

        // Healing spells
        EnableCure = defaults.EnableCure;
        EnableCureII = defaults.EnableCureII;
        EnableMedica = defaults.EnableMedica;
        EnableMedicaII = defaults.EnableMedicaII;
        EnableMedicaIII = defaults.EnableMedicaIII;
        EnableCureIII = defaults.EnableCureIII;
        EnableAfflatusSolace = defaults.EnableAfflatusSolace;
        EnableAfflatusRapture = defaults.EnableAfflatusRapture;
        EnableRegen = defaults.EnableRegen;
        EnableTetragrammaton = defaults.EnableTetragrammaton;
        EnableBenediction = defaults.EnableBenediction;
        EnableAssize = defaults.EnableAssize;
        EnableAsylum = defaults.EnableAsylum;
        EnablePresenceOfMind = defaults.EnablePresenceOfMind;
        EnableThinAir = defaults.EnableThinAir;
        EnableAetherialShift = defaults.EnableAetherialShift;
        EnableAfflatusMisery = defaults.EnableAfflatusMisery;

        // Defensive
        EnableDivineBenison = defaults.EnableDivineBenison;
        EnablePlenaryIndulgence = defaults.EnablePlenaryIndulgence;
        EnableTemperance = defaults.EnableTemperance;
        EnableAquaveil = defaults.EnableAquaveil;
        EnableLiturgyOfTheBell = defaults.EnableLiturgyOfTheBell;
        EnableDivineCaress = defaults.EnableDivineCaress;
        DefensiveCooldownThreshold = defaults.DefensiveCooldownThreshold;
        UseDefensivesWithAoEHeals = defaults.UseDefensivesWithAoEHeals;

        // Thresholds
        BenedictionEmergencyThreshold = defaults.BenedictionEmergencyThreshold;
        AoEHealMinTargets = defaults.AoEHealMinTargets;
        AoEDamageMinTargets = defaults.AoEDamageMinTargets;

        // Damage spells
        EnableStone = defaults.EnableStone;
        EnableStoneII = defaults.EnableStoneII;
        EnableStoneIII = defaults.EnableStoneIII;
        EnableStoneIV = defaults.EnableStoneIV;
        EnableGlare = defaults.EnableGlare;
        EnableGlareIII = defaults.EnableGlareIII;
        EnableGlareIV = defaults.EnableGlareIV;
        EnableHoly = defaults.EnableHoly;
        EnableHolyIII = defaults.EnableHolyIII;

        // DoT
        EnableAero = defaults.EnableAero;
        EnableAeroII = defaults.EnableAeroII;
        EnableDia = defaults.EnableDia;

        // Resurrection
        EnableRaise = defaults.EnableRaise;
        AllowHardcastRaise = defaults.AllowHardcastRaise;
        RaiseMpThreshold = defaults.RaiseMpThreshold;

        // Targeting
        EnemyStrategy = defaults.EnemyStrategy;
        UseTankAssistFallback = defaults.UseTankAssistFallback;
        TargetCacheTtlMs = defaults.TargetCacheTtlMs;

        // Role actions
        EnableEsuna = defaults.EnableEsuna;
        EsunaPriorityThreshold = defaults.EsunaPriorityThreshold;
        EnableSurecast = defaults.EnableSurecast;
        SurecastMode = defaults.SurecastMode;
        EnableRescue = defaults.EnableRescue;
        RescueMode = defaults.RescueMode;

        // Debug (reset section visibility but not window state)
        ActionHistorySize = defaults.ActionHistorySize;
        DebugSectionVisibility = new Dictionary<string, bool>(defaults.DebugSectionVisibility);

        // Restore preserved values
        Enabled = wasEnabled;
        MainWindowVisible = mainVisible;
        DebugWindowVisible = debugVisible;
    }
}
