using Olympus.Services.Targeting;

namespace Olympus.Tests;

/// <summary>
/// Tests for Configuration class, focusing on ResetToDefaults behavior.
/// </summary>
public class ConfigurationTests
{
    #region ResetToDefaults - State Preservation

    [Fact]
    public void ResetToDefaults_PreservesEnabledState_WhenTrue()
    {
        var config = new Configuration
        {
            Enabled = true
        };
        config.Healing.EnableCure = false; // Changed from default

        config.ResetToDefaults();

        Assert.True(config.Enabled);
    }

    [Fact]
    public void ResetToDefaults_PreservesEnabledState_WhenFalse()
    {
        var config = new Configuration
        {
            Enabled = false
        };
        config.Healing.EnableCure = false;

        config.ResetToDefaults();

        Assert.False(config.Enabled);
    }

    [Fact]
    public void ResetToDefaults_PreservesMainWindowVisible()
    {
        var config = new Configuration
        {
            MainWindowVisible = false
        };
        config.Healing.EnableCure = false;

        config.ResetToDefaults();

        Assert.False(config.MainWindowVisible);
    }

    [Fact]
    public void ResetToDefaults_PreservesDebugWindowVisible()
    {
        var config = new Configuration();
        config.Debug.DebugWindowVisible = true;
        config.Healing.EnableCure = false;

        config.ResetToDefaults();

        Assert.True(config.Debug.DebugWindowVisible);
    }

    #endregion

    #region ResetToDefaults - Spell Toggles

    [Fact]
    public void ResetToDefaults_ResetsHealingSpells()
    {
        var config = new Configuration();
        config.Healing.EnableCure = false;
        config.Healing.EnableCureII = false;
        config.Healing.EnableMedica = false;
        config.Healing.EnableMedicaII = false;
        config.Healing.EnableMedicaIII = false;
        config.Healing.EnableCureIII = false;
        config.Healing.EnableRegen = false;
        config.Healing.EnableAfflatusSolace = false;
        config.Healing.EnableAfflatusRapture = false;

        config.ResetToDefaults();

        Assert.True(config.Healing.EnableCure);
        Assert.True(config.Healing.EnableCureII);
        Assert.True(config.Healing.EnableMedica);
        Assert.True(config.Healing.EnableMedicaII);
        Assert.True(config.Healing.EnableMedicaIII);
        Assert.True(config.Healing.EnableCureIII);
        Assert.True(config.Healing.EnableRegen);
        Assert.True(config.Healing.EnableAfflatusSolace);
        Assert.True(config.Healing.EnableAfflatusRapture);
    }

    [Fact]
    public void ResetToDefaults_ResetsOgcdHeals()
    {
        var config = new Configuration();
        config.Healing.EnableTetragrammaton = false;
        config.Healing.EnableBenediction = false;
        config.Healing.EnableAssize = false;
        config.Healing.EnableAsylum = false;

        config.ResetToDefaults();

        Assert.True(config.Healing.EnableTetragrammaton);
        Assert.True(config.Healing.EnableBenediction);
        Assert.True(config.Healing.EnableAssize);
        Assert.True(config.Healing.EnableAsylum);
    }

    [Fact]
    public void ResetToDefaults_ResetsDamageSpells()
    {
        var config = new Configuration();
        config.Damage.EnableStone = false;
        config.Damage.EnableStoneII = false;
        config.Damage.EnableStoneIII = false;
        config.Damage.EnableStoneIV = false;
        config.Damage.EnableGlare = false;
        config.Damage.EnableGlareIII = false;
        config.Damage.EnableGlareIV = false;
        config.Damage.EnableHoly = false;
        config.Damage.EnableHolyIII = false;

        config.ResetToDefaults();

        Assert.True(config.Damage.EnableStone);
        Assert.True(config.Damage.EnableStoneII);
        Assert.True(config.Damage.EnableStoneIII);
        Assert.True(config.Damage.EnableStoneIV);
        Assert.True(config.Damage.EnableGlare);
        Assert.True(config.Damage.EnableGlareIII);
        Assert.True(config.Damage.EnableGlareIV);
        Assert.True(config.Damage.EnableHoly);
        Assert.True(config.Damage.EnableHolyIII);
    }

    [Fact]
    public void ResetToDefaults_ResetsDotSpells()
    {
        var config = new Configuration();
        config.Dot.EnableAero = false;
        config.Dot.EnableAeroII = false;
        config.Dot.EnableDia = false;

        config.ResetToDefaults();

        Assert.True(config.Dot.EnableAero);
        Assert.True(config.Dot.EnableAeroII);
        Assert.True(config.Dot.EnableDia);
    }

    [Fact]
    public void ResetToDefaults_ResetsDefensiveSpells()
    {
        var config = new Configuration();
        config.Defensive.EnableDivineBenison = false;
        config.Defensive.EnablePlenaryIndulgence = false;
        config.Defensive.EnableTemperance = false;
        config.Defensive.EnableAquaveil = false;
        config.Defensive.EnableLiturgyOfTheBell = false;
        config.Defensive.EnableDivineCaress = false;

        config.ResetToDefaults();

        Assert.True(config.Defensive.EnableDivineBenison);
        Assert.True(config.Defensive.EnablePlenaryIndulgence);
        Assert.True(config.Defensive.EnableTemperance);
        Assert.True(config.Defensive.EnableAquaveil);
        Assert.True(config.Defensive.EnableLiturgyOfTheBell);
        Assert.True(config.Defensive.EnableDivineCaress);
    }

    #endregion

    #region ResetToDefaults - Thresholds

    [Fact]
    public void ResetToDefaults_ResetsBenedictionThreshold()
    {
        var config = new Configuration();
        config.Healing.BenedictionEmergencyThreshold = 0.10f;

        config.ResetToDefaults();

        Assert.Equal(0.30f, config.Healing.BenedictionEmergencyThreshold);
    }

    [Fact]
    public void ResetToDefaults_ResetsAoEHealMinTargets()
    {
        var config = new Configuration();
        config.Healing.AoEHealMinTargets = 5;

        config.ResetToDefaults();

        Assert.Equal(3, config.Healing.AoEHealMinTargets);
    }

    [Fact]
    public void ResetToDefaults_ResetsAoEDamageMinTargets()
    {
        var config = new Configuration();
        config.Damage.AoEDamageMinTargets = 5;

        config.ResetToDefaults();

        Assert.Equal(3, config.Damage.AoEDamageMinTargets);
    }

    [Fact]
    public void ResetToDefaults_ResetsDefensiveCooldownThreshold()
    {
        var config = new Configuration();
        config.Defensive.DefensiveCooldownThreshold = 0.50f;

        config.ResetToDefaults();

        Assert.Equal(0.80f, config.Defensive.DefensiveCooldownThreshold);
    }

    [Fact]
    public void ResetToDefaults_ResetsRaiseMpThreshold()
    {
        var config = new Configuration();
        config.Resurrection.RaiseMpThreshold = 0.50f;

        config.ResetToDefaults();

        Assert.Equal(0.25f, config.Resurrection.RaiseMpThreshold);
    }

    #endregion

    #region ResetToDefaults - Targeting & Role Actions

    [Fact]
    public void ResetToDefaults_ResetsEnemyStrategy()
    {
        var config = new Configuration();
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.TankAssist;

        config.ResetToDefaults();

        Assert.Equal(EnemyTargetingStrategy.LowestHp, config.Targeting.EnemyStrategy);
    }

    [Fact]
    public void ResetToDefaults_ResetsRoleActions()
    {
        var config = new Configuration();
        config.RoleActions.EnableEsuna = false;
        config.RoleActions.EsunaPriorityThreshold = 0;
        config.RoleActions.EnableSurecast = true;
        config.RoleActions.SurecastMode = 1;
        config.RoleActions.EnableRescue = true;
        config.RoleActions.RescueMode = 1;

        config.ResetToDefaults();

        Assert.True(config.RoleActions.EnableEsuna);
        Assert.Equal(2, config.RoleActions.EsunaPriorityThreshold);
        Assert.False(config.RoleActions.EnableSurecast);
        Assert.Equal(0, config.RoleActions.SurecastMode);
        Assert.False(config.RoleActions.EnableRescue);
        Assert.Equal(0, config.RoleActions.RescueMode);
    }

    #endregion

    #region Default Values

    [Fact]
    public void DefaultConfiguration_HasCorrectMasterToggles()
    {
        var config = new Configuration();

        Assert.False(config.Enabled); // Disabled by default for safety
        Assert.True(config.EnableHealing);
        Assert.True(config.EnableDamage);
        Assert.True(config.EnableDoT);
    }

    [Fact]
    public void DefaultConfiguration_HasCorrectThresholds()
    {
        var config = new Configuration();

        Assert.Equal(0.30f, config.Healing.BenedictionEmergencyThreshold);
        Assert.Equal(0.80f, config.Defensive.DefensiveCooldownThreshold);
        Assert.Equal(0.25f, config.Resurrection.RaiseMpThreshold);
        Assert.Equal(3, config.Healing.AoEHealMinTargets);
        Assert.Equal(3, config.Damage.AoEDamageMinTargets);
    }

    [Fact]
    public void DefaultConfiguration_HasSafeRoleActionDefaults()
    {
        var config = new Configuration();

        // Esuna enabled with medium priority
        Assert.True(config.RoleActions.EnableEsuna);
        Assert.Equal(2, config.RoleActions.EsunaPriorityThreshold);

        // Surecast disabled by default
        Assert.False(config.RoleActions.EnableSurecast);
        Assert.Equal(0, config.RoleActions.SurecastMode);

        // Rescue disabled by default (dangerous)
        Assert.False(config.RoleActions.EnableRescue);
        Assert.Equal(0, config.RoleActions.RescueMode);
    }

    [Fact]
    public void DefaultConfiguration_HasResurrectionEnabled()
    {
        var config = new Configuration();

        Assert.True(config.Resurrection.EnableRaise);
        Assert.False(config.Resurrection.AllowHardcastRaise); // Hardcast disabled by default
    }

    #endregion

    #region Debug Settings

    [Fact]
    public void DefaultConfiguration_HasDebugSectionVisibility()
    {
        var config = new Configuration();

        Assert.NotNull(config.Debug.DebugSectionVisibility);
        Assert.NotEmpty(config.Debug.DebugSectionVisibility);
        Assert.True(config.Debug.DebugSectionVisibility.ContainsKey("GcdPlanning"));
        Assert.True(config.Debug.DebugSectionVisibility.ContainsKey("QuickStats"));
    }

    [Fact]
    public void ResetToDefaults_ResetsDebugSectionVisibility()
    {
        var config = new Configuration();
        config.Debug.DebugSectionVisibility["GcdPlanning"] = false;
        config.Debug.DebugSectionVisibility["CustomSection"] = true;

        config.ResetToDefaults();

        Assert.True(config.Debug.DebugSectionVisibility["GcdPlanning"]);
        Assert.False(config.Debug.DebugSectionVisibility.ContainsKey("CustomSection"));
    }

    #endregion
}
