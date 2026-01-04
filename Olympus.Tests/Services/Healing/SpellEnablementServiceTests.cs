using Olympus.Data;
using Olympus.Services.Healing;
using Xunit;

namespace Olympus.Tests.Services.Healing;

/// <summary>
/// Tests for SpellEnablementService.
/// Verifies correct mapping of action IDs to configuration settings.
/// </summary>
public class SpellEnablementServiceTests
{
    private static SpellEnablementService CreateService(Configuration? config = null)
    {
        return new SpellEnablementService(config ?? new Configuration());
    }

    #region Single Target Healing

    [Fact]
    public void IsSpellEnabled_Cure_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableCure = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Cure.ActionId));

        config.Healing.EnableCure = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Cure.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_CureII_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableCureII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.CureII.ActionId));

        config.Healing.EnableCureII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.CureII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Regen_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableRegen = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Regen.ActionId));

        config.Healing.EnableRegen = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Regen.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_AfflatusSolace_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableAfflatusSolace = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.AfflatusSolace.ActionId));

        config.Healing.EnableAfflatusSolace = true;
        Assert.True(service.IsSpellEnabled(WHMActions.AfflatusSolace.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Tetragrammaton_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableTetragrammaton = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Tetragrammaton.ActionId));

        config.Healing.EnableTetragrammaton = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Tetragrammaton.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Benediction_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableBenediction = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Benediction.ActionId));

        config.Healing.EnableBenediction = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Benediction.ActionId));
    }

    #endregion

    #region AoE Healing

    [Fact]
    public void IsSpellEnabled_CureIII_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableCureIII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.CureIII.ActionId));

        config.Healing.EnableCureIII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.CureIII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Medica_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableMedica = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Medica.ActionId));

        config.Healing.EnableMedica = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Medica.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_MedicaII_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableMedicaII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.MedicaII.ActionId));

        config.Healing.EnableMedicaII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.MedicaII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_MedicaIII_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableMedicaIII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.MedicaIII.ActionId));

        config.Healing.EnableMedicaIII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.MedicaIII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_AfflatusRapture_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableAfflatusRapture = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.AfflatusRapture.ActionId));

        config.Healing.EnableAfflatusRapture = true;
        Assert.True(service.IsSpellEnabled(WHMActions.AfflatusRapture.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Assize_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableAssize = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Assize.ActionId));

        config.Healing.EnableAssize = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Assize.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Asylum_ReadsHealingConfig()
    {
        var config = new Configuration();
        config.Healing.EnableAsylum = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Asylum.ActionId));

        config.Healing.EnableAsylum = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Asylum.ActionId));
    }

    #endregion

    #region Damage Spells

    [Fact]
    public void IsSpellEnabled_Stone_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableStone = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Stone.ActionId));

        config.Damage.EnableStone = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Stone.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_StoneII_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableStoneII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.StoneII.ActionId));

        config.Damage.EnableStoneII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.StoneII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_StoneIII_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableStoneIII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.StoneIII.ActionId));

        config.Damage.EnableStoneIII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.StoneIII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_StoneIV_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableStoneIV = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.StoneIV.ActionId));

        config.Damage.EnableStoneIV = true;
        Assert.True(service.IsSpellEnabled(WHMActions.StoneIV.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Glare_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableGlare = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Glare.ActionId));

        config.Damage.EnableGlare = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Glare.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_GlareIII_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableGlareIII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.GlareIII.ActionId));

        config.Damage.EnableGlareIII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.GlareIII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_GlareIV_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableGlareIV = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.GlareIV.ActionId));

        config.Damage.EnableGlareIV = true;
        Assert.True(service.IsSpellEnabled(WHMActions.GlareIV.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Holy_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableHoly = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Holy.ActionId));

        config.Damage.EnableHoly = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Holy.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_HolyIII_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableHolyIII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.HolyIII.ActionId));

        config.Damage.EnableHolyIII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.HolyIII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_AfflatusMisery_ReadsDamageConfig()
    {
        var config = new Configuration();
        config.Damage.EnableAfflatusMisery = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.AfflatusMisery.ActionId));

        config.Damage.EnableAfflatusMisery = true;
        Assert.True(service.IsSpellEnabled(WHMActions.AfflatusMisery.ActionId));
    }

    #endregion

    #region DoT Spells

    [Fact]
    public void IsSpellEnabled_Aero_ReadsDotConfig()
    {
        var config = new Configuration();
        config.Dot.EnableAero = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Aero.ActionId));

        config.Dot.EnableAero = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Aero.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_AeroII_ReadsDotConfig()
    {
        var config = new Configuration();
        config.Dot.EnableAeroII = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.AeroII.ActionId));

        config.Dot.EnableAeroII = true;
        Assert.True(service.IsSpellEnabled(WHMActions.AeroII.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Dia_ReadsDotConfig()
    {
        var config = new Configuration();
        config.Dot.EnableDia = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Dia.ActionId));

        config.Dot.EnableDia = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Dia.ActionId));
    }

    #endregion

    #region Defensive Spells

    [Fact]
    public void IsSpellEnabled_DivineBenison_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnableDivineBenison = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.DivineBenison.ActionId));

        config.Defensive.EnableDivineBenison = true;
        Assert.True(service.IsSpellEnabled(WHMActions.DivineBenison.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_PlenaryIndulgence_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnablePlenaryIndulgence = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.PlenaryIndulgence.ActionId));

        config.Defensive.EnablePlenaryIndulgence = true;
        Assert.True(service.IsSpellEnabled(WHMActions.PlenaryIndulgence.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Temperance_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnableTemperance = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Temperance.ActionId));

        config.Defensive.EnableTemperance = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Temperance.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_Aquaveil_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnableAquaveil = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Aquaveil.ActionId));

        config.Defensive.EnableAquaveil = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Aquaveil.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_LiturgyOfTheBell_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnableLiturgyOfTheBell = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.LiturgyOfTheBell.ActionId));

        config.Defensive.EnableLiturgyOfTheBell = true;
        Assert.True(service.IsSpellEnabled(WHMActions.LiturgyOfTheBell.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_DivineCaress_ReadsDefensiveConfig()
    {
        var config = new Configuration();
        config.Defensive.EnableDivineCaress = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.DivineCaress.ActionId));

        config.Defensive.EnableDivineCaress = true;
        Assert.True(service.IsSpellEnabled(WHMActions.DivineCaress.ActionId));
    }

    #endregion

    #region Buff Spells

    [Fact]
    public void IsSpellEnabled_PresenceOfMind_ReadsBuffsConfig()
    {
        var config = new Configuration();
        config.Buffs.EnablePresenceOfMind = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.PresenceOfMind.ActionId));

        config.Buffs.EnablePresenceOfMind = true;
        Assert.True(service.IsSpellEnabled(WHMActions.PresenceOfMind.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_ThinAir_ReadsBuffsConfig()
    {
        var config = new Configuration();
        config.Buffs.EnableThinAir = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.ThinAir.ActionId));

        config.Buffs.EnableThinAir = true;
        Assert.True(service.IsSpellEnabled(WHMActions.ThinAir.ActionId));
    }

    [Fact]
    public void IsSpellEnabled_AetherialShift_ReadsBuffsConfig()
    {
        var config = new Configuration();
        config.Buffs.EnableAetherialShift = false;
        var service = CreateService(config);

        // Note: AetherialShift is 37012 in SpellEnablementService
        Assert.False(service.IsSpellEnabled(37012));

        config.Buffs.EnableAetherialShift = true;
        Assert.True(service.IsSpellEnabled(37012));
    }

    #endregion

    #region Role Actions

    [Fact]
    public void IsSpellEnabled_Esuna_ReadsRoleActionsConfig()
    {
        var config = new Configuration();
        config.RoleActions.EnableEsuna = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Esuna.ActionId));

        config.RoleActions.EnableEsuna = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Esuna.ActionId));
    }

    #endregion

    #region Resurrection

    [Fact]
    public void IsSpellEnabled_Raise_ReadsResurrectionConfig()
    {
        var config = new Configuration();
        config.Resurrection.EnableRaise = false;
        var service = CreateService(config);

        Assert.False(service.IsSpellEnabled(WHMActions.Raise.ActionId));

        config.Resurrection.EnableRaise = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Raise.ActionId));
    }

    #endregion

    #region Unknown Spells

    [Fact]
    public void IsSpellEnabled_UnknownActionId_ReturnsTrue()
    {
        var config = new Configuration();
        var service = CreateService(config);

        // Unknown action ID should default to enabled
        Assert.True(service.IsSpellEnabled(99999));
        Assert.True(service.IsSpellEnabled(0));
    }

    #endregion

    #region Default Configuration Tests

    [Fact]
    public void DefaultConfiguration_AllSpellsEnabled()
    {
        var service = CreateService();

        // Verify default configuration has all spells enabled
        Assert.True(service.IsSpellEnabled(WHMActions.Cure.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.CureII.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Regen.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Medica.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Stone.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Aero.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Benediction.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Tetragrammaton.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Raise.ActionId));
        Assert.True(service.IsSpellEnabled(WHMActions.Esuna.ActionId));
    }

    #endregion

    #region Configuration Reference Tests

    [Fact]
    public void ConfigurationChanges_ReflectedImmediately()
    {
        var config = new Configuration();
        var service = CreateService(config);

        // Initially enabled
        Assert.True(service.IsSpellEnabled(WHMActions.Cure.ActionId));

        // Disable via config
        config.Healing.EnableCure = false;

        // Should immediately reflect change
        Assert.False(service.IsSpellEnabled(WHMActions.Cure.ActionId));

        // Re-enable
        config.Healing.EnableCure = true;
        Assert.True(service.IsSpellEnabled(WHMActions.Cure.ActionId));
    }

    #endregion
}
