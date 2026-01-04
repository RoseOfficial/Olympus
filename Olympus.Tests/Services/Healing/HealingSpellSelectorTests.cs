using Olympus.Data;
using Olympus.Services.Healing;
using Xunit;

namespace Olympus.Tests.Services.Healing;

/// <summary>
/// Tests for HealingSpellSelector.
///
/// NOTE: Most HealingSpellSelector functionality requires IPlayerCharacter and IBattleChara
/// from Dalamud, which cannot be mocked without the Dalamud runtime. These tests verify
/// the portions that don't require Dalamud types.
///
/// Full integration testing requires the Dalamud runtime environment.
/// </summary>
public class HealingSpellSelectorTests
{
    #region SpellCandidateDebug Tests

    [Fact]
    public void SpellCandidateDebug_RecordDefaultValues()
    {
        var debug = new SpellCandidateDebug();

        Assert.Equal("", debug.SpellName);
        Assert.Equal(0u, debug.ActionId);
        Assert.Equal(0, debug.HealAmount);
        Assert.Equal(0f, debug.Efficiency);
        Assert.Equal(0f, debug.Score);
        Assert.Equal("", debug.Bonuses);
        Assert.False(debug.WasSelected);
        Assert.Null(debug.RejectionReason);
    }

    [Fact]
    public void SpellCandidateDebug_WithValues()
    {
        var debug = new SpellCandidateDebug
        {
            SpellName = "Cure II",
            ActionId = 135,
            HealAmount = 5000,
            Efficiency = 0.95f,
            Score = 100f,
            Bonuses = "Freecure",
            WasSelected = true,
            RejectionReason = null
        };

        Assert.Equal("Cure II", debug.SpellName);
        Assert.Equal(135u, debug.ActionId);
        Assert.Equal(5000, debug.HealAmount);
        Assert.Equal(0.95f, debug.Efficiency);
        Assert.Equal(100f, debug.Score);
        Assert.Equal("Freecure", debug.Bonuses);
        Assert.True(debug.WasSelected);
        Assert.Null(debug.RejectionReason);
    }

    [Fact]
    public void SpellCandidateDebug_WithRejectionReason()
    {
        var debug = new SpellCandidateDebug
        {
            SpellName = "Regen",
            ActionId = 137,
            HealAmount = 0,
            Efficiency = 0f,
            Score = 0f,
            WasSelected = false,
            RejectionReason = "Target already has Regen"
        };

        Assert.Equal("Regen", debug.SpellName);
        Assert.False(debug.WasSelected);
        Assert.Equal("Target already has Regen", debug.RejectionReason);
    }

    [Fact]
    public void SpellCandidateDebug_RecordEquality()
    {
        var debug1 = new SpellCandidateDebug
        {
            SpellName = "Cure",
            ActionId = 120,
            HealAmount = 3000
        };

        var debug2 = new SpellCandidateDebug
        {
            SpellName = "Cure",
            ActionId = 120,
            HealAmount = 3000
        };

        Assert.Equal(debug1, debug2);
    }

    [Fact]
    public void SpellCandidateDebug_RecordWithMutation()
    {
        var original = new SpellCandidateDebug
        {
            SpellName = "Cure",
            ActionId = 120,
            WasSelected = false
        };

        var modified = original with { WasSelected = true };

        Assert.False(original.WasSelected);
        Assert.True(modified.WasSelected);
        Assert.Equal(original.ActionId, modified.ActionId);
    }

    #endregion

    #region SpellSelectionDebug Tests

    [Fact]
    public void SpellSelectionDebug_DefaultValues()
    {
        var debug = new SpellSelectionDebug();

        Assert.Equal("", debug.SelectionType);
        Assert.Equal("", debug.TargetName);
        Assert.Equal(0, debug.MissingHp);
        Assert.Equal(0f, debug.TargetHpPercent);
        Assert.False(debug.IsWeaveWindow);
        Assert.Equal(0, debug.LilyCount);
        Assert.NotNull(debug.Candidates);
        Assert.Empty(debug.Candidates);
        Assert.Null(debug.SelectedSpell);
        Assert.Null(debug.SelectionReason);
    }

    [Fact]
    public void SpellSelectionDebug_WithValues()
    {
        var debug = new SpellSelectionDebug
        {
            SelectionType = "Single",
            TargetName = "Tank",
            MissingHp = 5000,
            TargetHpPercent = 0.5f,
            IsWeaveWindow = true,
            LilyCount = 2,
            SelectedSpell = "Afflatus Solace",
            SelectionReason = "Tier 1: Lily heal"
        };

        Assert.Equal("Single", debug.SelectionType);
        Assert.Equal("Tank", debug.TargetName);
        Assert.Equal(5000, debug.MissingHp);
        Assert.Equal(0.5f, debug.TargetHpPercent);
        Assert.True(debug.IsWeaveWindow);
        Assert.Equal(2, debug.LilyCount);
        Assert.Equal("Afflatus Solace", debug.SelectedSpell);
        Assert.Equal("Tier 1: Lily heal", debug.SelectionReason);
    }

    [Fact]
    public void SpellSelectionDebug_SecondsAgo_CalculatesCorrectly()
    {
        var debug = new SpellSelectionDebug
        {
            Timestamp = System.DateTime.Now.AddSeconds(-5)
        };

        // Should be approximately 5 seconds
        Assert.True(debug.SecondsAgo >= 4.9f && debug.SecondsAgo <= 5.5f);
    }

    [Fact]
    public void SpellSelectionDebug_WithCandidates()
    {
        var debug = new SpellSelectionDebug
        {
            SelectionType = "Single",
            Candidates = new System.Collections.Generic.List<SpellCandidateDebug>
            {
                new SpellCandidateDebug { SpellName = "Cure", ActionId = 120 },
                new SpellCandidateDebug { SpellName = "Cure II", ActionId = 135, WasSelected = true },
                new SpellCandidateDebug { SpellName = "Regen", ActionId = 137, RejectionReason = "Disabled" }
            }
        };

        Assert.Equal(3, debug.Candidates.Count);
        Assert.Single(debug.Candidates.FindAll(c => c.WasSelected));
        Assert.Single(debug.Candidates.FindAll(c => c.RejectionReason != null));
    }

    #endregion

    #region WHMActions Definition Tests

    [Fact]
    public void WHMActions_Cure_CorrectDefinition()
    {
        Assert.Equal(120u, WHMActions.Cure.ActionId);
        Assert.Equal("Cure", WHMActions.Cure.Name);
        Assert.Equal(2, WHMActions.Cure.MinLevel);
        Assert.Equal(500, WHMActions.Cure.HealPotency);
        Assert.True(WHMActions.Cure.IsGCD);
        Assert.True(WHMActions.Cure.IsHeal);
    }

    [Fact]
    public void WHMActions_CureII_CorrectDefinition()
    {
        Assert.Equal(135u, WHMActions.CureII.ActionId);
        Assert.Equal("Cure II", WHMActions.CureII.Name);
        Assert.Equal(30, WHMActions.CureII.MinLevel);
        Assert.Equal(800, WHMActions.CureII.HealPotency);
        Assert.True(WHMActions.CureII.IsGCD);
    }

    [Fact]
    public void WHMActions_AfflatusSolace_CorrectDefinition()
    {
        Assert.Equal(16531u, WHMActions.AfflatusSolace.ActionId);
        Assert.Equal("Afflatus Solace", WHMActions.AfflatusSolace.Name);
        Assert.Equal(52, WHMActions.AfflatusSolace.MinLevel);
        Assert.Equal(800, WHMActions.AfflatusSolace.HealPotency);
        Assert.Equal(0, WHMActions.AfflatusSolace.MpCost); // Free
        Assert.True(WHMActions.AfflatusSolace.IsInstantCast);
    }

    [Fact]
    public void WHMActions_Regen_CorrectDefinition()
    {
        Assert.Equal(137u, WHMActions.Regen.ActionId);
        Assert.Equal("Regen", WHMActions.Regen.Name);
        Assert.Equal(35, WHMActions.Regen.MinLevel);
        Assert.Equal(158u, WHMActions.Regen.AppliedStatusId);
        Assert.Equal(18f, WHMActions.Regen.AppliedStatusDuration);
    }

    [Fact]
    public void WHMActions_Medica_CorrectDefinition()
    {
        Assert.Equal(124u, WHMActions.Medica.ActionId);
        Assert.Equal("Medica", WHMActions.Medica.Name);
        Assert.Equal(10, WHMActions.Medica.MinLevel);
        Assert.Equal(400, WHMActions.Medica.HealPotency);
        Assert.True(WHMActions.Medica.IsAoE);
    }

    [Fact]
    public void WHMActions_AfflatusRapture_CorrectDefinition()
    {
        Assert.Equal(16534u, WHMActions.AfflatusRapture.ActionId);
        Assert.Equal("Afflatus Rapture", WHMActions.AfflatusRapture.Name);
        Assert.Equal(76, WHMActions.AfflatusRapture.MinLevel);
        Assert.Equal(400, WHMActions.AfflatusRapture.HealPotency);
        Assert.True(WHMActions.AfflatusRapture.IsAoE);
    }

    [Fact]
    public void WHMActions_Tetragrammaton_CorrectDefinition()
    {
        Assert.Equal(3570u, WHMActions.Tetragrammaton.ActionId);
        Assert.Equal("Tetragrammaton", WHMActions.Tetragrammaton.Name);
        Assert.Equal(60, WHMActions.Tetragrammaton.MinLevel);
        Assert.Equal(700, WHMActions.Tetragrammaton.HealPotency);
        Assert.True(WHMActions.Tetragrammaton.IsOGCD);
    }

    [Fact]
    public void WHMActions_Benediction_CorrectDefinition()
    {
        Assert.Equal(140u, WHMActions.Benediction.ActionId);
        Assert.Equal("Benediction", WHMActions.Benediction.Name);
        Assert.Equal(50, WHMActions.Benediction.MinLevel);
        Assert.Equal(0, WHMActions.Benediction.HealPotency); // Special: heals to full
        Assert.True(WHMActions.Benediction.IsOGCD);
        Assert.Equal(180f, WHMActions.Benediction.RecastTime); // 3 minute cooldown
    }

    [Fact]
    public void WHMActions_GetDamageGcdForLevel_ReturnsAppropriateSpell()
    {
        Assert.Equal(WHMActions.Stone, WHMActions.GetDamageGcdForLevel(1));
        Assert.Equal(WHMActions.StoneII, WHMActions.GetDamageGcdForLevel(18));
        Assert.Equal(WHMActions.Glare, WHMActions.GetDamageGcdForLevel(72));
        Assert.Equal(WHMActions.GlareIII, WHMActions.GetDamageGcdForLevel(82));
    }

    [Fact]
    public void WHMActions_GetDotForLevel_ReturnsAppropriateSpell()
    {
        Assert.Equal(WHMActions.Aero, WHMActions.GetDotForLevel(4));
        Assert.Equal(WHMActions.AeroII, WHMActions.GetDotForLevel(46));
        Assert.Equal(WHMActions.Dia, WHMActions.GetDotForLevel(72));
    }

    [Fact]
    public void WHMActions_GetSingleHealGcdForLevel_ReturnsAppropriateSpell()
    {
        Assert.Equal(WHMActions.Cure, WHMActions.GetSingleHealGcdForLevel(2));
        Assert.Equal(WHMActions.CureII, WHMActions.GetSingleHealGcdForLevel(30));
        Assert.Equal(WHMActions.AfflatusSolace, WHMActions.GetSingleHealGcdForLevel(52));
    }

    [Fact]
    public void WHMActions_GetAoEHealGcdForLevel_ReturnsAppropriateSpell()
    {
        Assert.Equal(WHMActions.Medica, WHMActions.GetAoEHealGcdForLevel(10));
        Assert.Equal(WHMActions.MedicaII, WHMActions.GetAoEHealGcdForLevel(50));
        Assert.Equal(WHMActions.AfflatusRapture, WHMActions.GetAoEHealGcdForLevel(76));
    }

    #endregion
}
