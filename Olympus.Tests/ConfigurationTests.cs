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
            Enabled = true,
            EnableCure = false // Changed from default
        };

        config.ResetToDefaults();

        Assert.True(config.Enabled);
    }

    [Fact]
    public void ResetToDefaults_PreservesEnabledState_WhenFalse()
    {
        var config = new Configuration
        {
            Enabled = false,
            EnableCure = false
        };

        config.ResetToDefaults();

        Assert.False(config.Enabled);
    }

    [Fact]
    public void ResetToDefaults_PreservesMainWindowVisible()
    {
        var config = new Configuration
        {
            MainWindowVisible = false,
            EnableCure = false
        };

        config.ResetToDefaults();

        Assert.False(config.MainWindowVisible);
    }

    [Fact]
    public void ResetToDefaults_PreservesDebugWindowVisible()
    {
        var config = new Configuration
        {
            DebugWindowVisible = true,
            EnableCure = false
        };

        config.ResetToDefaults();

        Assert.True(config.DebugWindowVisible);
    }

    #endregion

    #region ResetToDefaults - Spell Toggles

    [Fact]
    public void ResetToDefaults_ResetsHealingSpells()
    {
        var config = new Configuration
        {
            EnableCure = false,
            EnableCureII = false,
            EnableMedica = false,
            EnableMedicaII = false,
            EnableMedicaIII = false,
            EnableCureIII = false,
            EnableRegen = false,
            EnableAfflatusSolace = false,
            EnableAfflatusRapture = false
        };

        config.ResetToDefaults();

        Assert.True(config.EnableCure);
        Assert.True(config.EnableCureII);
        Assert.True(config.EnableMedica);
        Assert.True(config.EnableMedicaII);
        Assert.True(config.EnableMedicaIII);
        Assert.True(config.EnableCureIII);
        Assert.True(config.EnableRegen);
        Assert.True(config.EnableAfflatusSolace);
        Assert.True(config.EnableAfflatusRapture);
    }

    [Fact]
    public void ResetToDefaults_ResetsOgcdHeals()
    {
        var config = new Configuration
        {
            EnableTetragrammaton = false,
            EnableBenediction = false,
            EnableAssize = false,
            EnableAsylum = false
        };

        config.ResetToDefaults();

        Assert.True(config.EnableTetragrammaton);
        Assert.True(config.EnableBenediction);
        Assert.True(config.EnableAssize);
        Assert.True(config.EnableAsylum);
    }

    [Fact]
    public void ResetToDefaults_ResetsDamageSpells()
    {
        var config = new Configuration
        {
            EnableStone = false,
            EnableStoneII = false,
            EnableStoneIII = false,
            EnableStoneIV = false,
            EnableGlare = false,
            EnableGlareIII = false,
            EnableGlareIV = false,
            EnableHoly = false,
            EnableHolyIII = false
        };

        config.ResetToDefaults();

        Assert.True(config.EnableStone);
        Assert.True(config.EnableStoneII);
        Assert.True(config.EnableStoneIII);
        Assert.True(config.EnableStoneIV);
        Assert.True(config.EnableGlare);
        Assert.True(config.EnableGlareIII);
        Assert.True(config.EnableGlareIV);
        Assert.True(config.EnableHoly);
        Assert.True(config.EnableHolyIII);
    }

    [Fact]
    public void ResetToDefaults_ResetsDotSpells()
    {
        var config = new Configuration
        {
            EnableAero = false,
            EnableAeroII = false,
            EnableDia = false
        };

        config.ResetToDefaults();

        Assert.True(config.EnableAero);
        Assert.True(config.EnableAeroII);
        Assert.True(config.EnableDia);
    }

    [Fact]
    public void ResetToDefaults_ResetsDefensiveSpells()
    {
        var config = new Configuration
        {
            EnableDivineBenison = false,
            EnablePlenaryIndulgence = false,
            EnableTemperance = false,
            EnableAquaveil = false,
            EnableLiturgyOfTheBell = false,
            EnableDivineCaress = false
        };

        config.ResetToDefaults();

        Assert.True(config.EnableDivineBenison);
        Assert.True(config.EnablePlenaryIndulgence);
        Assert.True(config.EnableTemperance);
        Assert.True(config.EnableAquaveil);
        Assert.True(config.EnableLiturgyOfTheBell);
        Assert.True(config.EnableDivineCaress);
    }

    #endregion

    #region ResetToDefaults - Thresholds

    [Fact]
    public void ResetToDefaults_ResetsBenedictionThreshold()
    {
        var config = new Configuration
        {
            BenedictionEmergencyThreshold = 0.10f
        };

        config.ResetToDefaults();

        Assert.Equal(0.30f, config.BenedictionEmergencyThreshold);
    }

    [Fact]
    public void ResetToDefaults_ResetsAoEHealMinTargets()
    {
        var config = new Configuration
        {
            AoEHealMinTargets = 5
        };

        config.ResetToDefaults();

        Assert.Equal(3, config.AoEHealMinTargets);
    }

    [Fact]
    public void ResetToDefaults_ResetsAoEDamageMinTargets()
    {
        var config = new Configuration
        {
            AoEDamageMinTargets = 5
        };

        config.ResetToDefaults();

        Assert.Equal(3, config.AoEDamageMinTargets);
    }

    [Fact]
    public void ResetToDefaults_ResetsDefensiveCooldownThreshold()
    {
        var config = new Configuration
        {
            DefensiveCooldownThreshold = 0.50f
        };

        config.ResetToDefaults();

        Assert.Equal(0.80f, config.DefensiveCooldownThreshold);
    }

    [Fact]
    public void ResetToDefaults_ResetsRaiseMpThreshold()
    {
        var config = new Configuration
        {
            RaiseMpThreshold = 0.50f
        };

        config.ResetToDefaults();

        Assert.Equal(0.25f, config.RaiseMpThreshold);
    }

    #endregion

    #region ResetToDefaults - Targeting & Role Actions

    [Fact]
    public void ResetToDefaults_ResetsEnemyStrategy()
    {
        var config = new Configuration
        {
            EnemyStrategy = EnemyTargetingStrategy.TankAssist
        };

        config.ResetToDefaults();

        Assert.Equal(EnemyTargetingStrategy.LowestHp, config.EnemyStrategy);
    }

    [Fact]
    public void ResetToDefaults_ResetsRoleActions()
    {
        var config = new Configuration
        {
            EnableEsuna = false,
            EsunaPriorityThreshold = 0,
            EnableSurecast = true,
            SurecastMode = 1,
            EnableRescue = true,
            RescueMode = 1
        };

        config.ResetToDefaults();

        Assert.True(config.EnableEsuna);
        Assert.Equal(2, config.EsunaPriorityThreshold);
        Assert.False(config.EnableSurecast);
        Assert.Equal(0, config.SurecastMode);
        Assert.False(config.EnableRescue);
        Assert.Equal(0, config.RescueMode);
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

        Assert.Equal(0.30f, config.BenedictionEmergencyThreshold);
        Assert.Equal(0.80f, config.DefensiveCooldownThreshold);
        Assert.Equal(0.25f, config.RaiseMpThreshold);
        Assert.Equal(3, config.AoEHealMinTargets);
        Assert.Equal(3, config.AoEDamageMinTargets);
    }

    [Fact]
    public void DefaultConfiguration_HasSafeRoleActionDefaults()
    {
        var config = new Configuration();

        // Esuna enabled with medium priority
        Assert.True(config.EnableEsuna);
        Assert.Equal(2, config.EsunaPriorityThreshold);

        // Surecast disabled by default
        Assert.False(config.EnableSurecast);
        Assert.Equal(0, config.SurecastMode);

        // Rescue disabled by default (dangerous)
        Assert.False(config.EnableRescue);
        Assert.Equal(0, config.RescueMode);
    }

    [Fact]
    public void DefaultConfiguration_HasResurrectionEnabled()
    {
        var config = new Configuration();

        Assert.True(config.EnableRaise);
        Assert.False(config.AllowHardcastRaise); // Hardcast disabled by default
    }

    #endregion

    #region Debug Settings

    [Fact]
    public void DefaultConfiguration_HasDebugSectionVisibility()
    {
        var config = new Configuration();

        Assert.NotNull(config.DebugSectionVisibility);
        Assert.NotEmpty(config.DebugSectionVisibility);
        Assert.True(config.DebugSectionVisibility.ContainsKey("GcdPlanning"));
        Assert.True(config.DebugSectionVisibility.ContainsKey("QuickStats"));
    }

    [Fact]
    public void ResetToDefaults_ResetsDebugSectionVisibility()
    {
        var config = new Configuration();
        config.DebugSectionVisibility["GcdPlanning"] = false;
        config.DebugSectionVisibility["CustomSection"] = true;

        config.ResetToDefaults();

        Assert.True(config.DebugSectionVisibility["GcdPlanning"]);
        Assert.False(config.DebugSectionVisibility.ContainsKey("CustomSection"));
    }

    #endregion
}
