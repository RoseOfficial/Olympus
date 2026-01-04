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

    #region Legacy Property Wrappers (for backward compatibility)
    // These properties delegate to nested configs for backward compatibility.
    // Modules can be updated incrementally to use nested configs directly.

    // Healing spells
    public bool EnableCure { get => Healing.EnableCure; set => Healing.EnableCure = value; }
    public bool EnableCureII { get => Healing.EnableCureII; set => Healing.EnableCureII = value; }
    public bool EnableMedica { get => Healing.EnableMedica; set => Healing.EnableMedica = value; }
    public bool EnableMedicaII { get => Healing.EnableMedicaII; set => Healing.EnableMedicaII = value; }
    public bool EnableMedicaIII { get => Healing.EnableMedicaIII; set => Healing.EnableMedicaIII = value; }
    public bool EnableCureIII { get => Healing.EnableCureIII; set => Healing.EnableCureIII = value; }
    public bool EnableAfflatusSolace { get => Healing.EnableAfflatusSolace; set => Healing.EnableAfflatusSolace = value; }
    public bool EnableAfflatusRapture { get => Healing.EnableAfflatusRapture; set => Healing.EnableAfflatusRapture = value; }
    public bool EnableRegen { get => Healing.EnableRegen; set => Healing.EnableRegen = value; }
    public bool EnableTetragrammaton { get => Healing.EnableTetragrammaton; set => Healing.EnableTetragrammaton = value; }
    public bool EnableBenediction { get => Healing.EnableBenediction; set => Healing.EnableBenediction = value; }
    public bool EnableAssize { get => Healing.EnableAssize; set => Healing.EnableAssize = value; }
    public bool EnableAsylum { get => Healing.EnableAsylum; set => Healing.EnableAsylum = value; }
    public int AoEHealMinTargets { get => Healing.AoEHealMinTargets; set => Healing.AoEHealMinTargets = value; }
    public float BenedictionEmergencyThreshold { get => Healing.BenedictionEmergencyThreshold; set => Healing.BenedictionEmergencyThreshold = value; }

    // Buff spells
    public bool EnablePresenceOfMind { get => Buffs.EnablePresenceOfMind; set => Buffs.EnablePresenceOfMind = value; }
    public bool EnableThinAir { get => Buffs.EnableThinAir; set => Buffs.EnableThinAir = value; }
    public bool EnableAetherialShift { get => Buffs.EnableAetherialShift; set => Buffs.EnableAetherialShift = value; }

    // Damage spells
    public bool EnableStone { get => Damage.EnableStone; set => Damage.EnableStone = value; }
    public bool EnableStoneII { get => Damage.EnableStoneII; set => Damage.EnableStoneII = value; }
    public bool EnableStoneIII { get => Damage.EnableStoneIII; set => Damage.EnableStoneIII = value; }
    public bool EnableStoneIV { get => Damage.EnableStoneIV; set => Damage.EnableStoneIV = value; }
    public bool EnableGlare { get => Damage.EnableGlare; set => Damage.EnableGlare = value; }
    public bool EnableGlareIII { get => Damage.EnableGlareIII; set => Damage.EnableGlareIII = value; }
    public bool EnableGlareIV { get => Damage.EnableGlareIV; set => Damage.EnableGlareIV = value; }
    public bool EnableHoly { get => Damage.EnableHoly; set => Damage.EnableHoly = value; }
    public bool EnableHolyIII { get => Damage.EnableHolyIII; set => Damage.EnableHolyIII = value; }
    public bool EnableAfflatusMisery { get => Damage.EnableAfflatusMisery; set => Damage.EnableAfflatusMisery = value; }
    public int AoEDamageMinTargets { get => Damage.AoEDamageMinTargets; set => Damage.AoEDamageMinTargets = value; }

    // DoT spells
    public bool EnableAero { get => Dot.EnableAero; set => Dot.EnableAero = value; }
    public bool EnableAeroII { get => Dot.EnableAeroII; set => Dot.EnableAeroII = value; }
    public bool EnableDia { get => Dot.EnableDia; set => Dot.EnableDia = value; }

    // Defensive cooldowns
    public bool EnableDivineBenison { get => Defensive.EnableDivineBenison; set => Defensive.EnableDivineBenison = value; }
    public bool EnablePlenaryIndulgence { get => Defensive.EnablePlenaryIndulgence; set => Defensive.EnablePlenaryIndulgence = value; }
    public bool EnableTemperance { get => Defensive.EnableTemperance; set => Defensive.EnableTemperance = value; }
    public bool EnableAquaveil { get => Defensive.EnableAquaveil; set => Defensive.EnableAquaveil = value; }
    public bool EnableLiturgyOfTheBell { get => Defensive.EnableLiturgyOfTheBell; set => Defensive.EnableLiturgyOfTheBell = value; }
    public bool EnableDivineCaress { get => Defensive.EnableDivineCaress; set => Defensive.EnableDivineCaress = value; }
    public float DefensiveCooldownThreshold { get => Defensive.DefensiveCooldownThreshold; set => Defensive.DefensiveCooldownThreshold = value; }
    public bool UseDefensivesWithAoEHeals { get => Defensive.UseDefensivesWithAoEHeals; set => Defensive.UseDefensivesWithAoEHeals = value; }

    // Resurrection
    public bool EnableRaise { get => Resurrection.EnableRaise; set => Resurrection.EnableRaise = value; }
    public bool AllowHardcastRaise { get => Resurrection.AllowHardcastRaise; set => Resurrection.AllowHardcastRaise = value; }
    public float RaiseMpThreshold { get => Resurrection.RaiseMpThreshold; set => Resurrection.RaiseMpThreshold = value; }

    // Targeting
    public EnemyTargetingStrategy EnemyStrategy { get => Targeting.EnemyStrategy; set => Targeting.EnemyStrategy = value; }
    public bool UseTankAssistFallback { get => Targeting.UseTankAssistFallback; set => Targeting.UseTankAssistFallback = value; }
    public int TargetCacheTtlMs { get => Targeting.TargetCacheTtlMs; set => Targeting.TargetCacheTtlMs = value; }

    // Role actions
    public bool EnableEsuna { get => RoleActions.EnableEsuna; set => RoleActions.EnableEsuna = value; }
    public int EsunaPriorityThreshold { get => RoleActions.EsunaPriorityThreshold; set => RoleActions.EsunaPriorityThreshold = value; }
    public bool EnableSurecast { get => RoleActions.EnableSurecast; set => RoleActions.EnableSurecast = value; }
    public int SurecastMode { get => RoleActions.SurecastMode; set => RoleActions.SurecastMode = value; }
    public bool EnableRescue { get => RoleActions.EnableRescue; set => RoleActions.EnableRescue = value; }
    public int RescueMode { get => RoleActions.RescueMode; set => RoleActions.RescueMode = value; }

    // Debug
    public bool DebugWindowVisible { get => Debug.DebugWindowVisible; set => Debug.DebugWindowVisible = value; }
    public int ActionHistorySize { get => Debug.ActionHistorySize; set => Debug.ActionHistorySize = value; }
    public System.Collections.Generic.Dictionary<string, bool> DebugSectionVisibility
    {
        get => Debug.DebugSectionVisibility;
        set => Debug.DebugSectionVisibility = value;
    }

    #endregion

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

        // Restore preserved values
        Enabled = wasEnabled;
        MainWindowVisible = mainVisible;
        Debug.DebugWindowVisible = debugVisible;
    }
}
