using Olympus.Data;
using Xunit;

namespace Olympus.Tests.Data;

public class SpellChecklistRegistryTests
{
    // ── GetChecklist routing ───────────────────────────────────────────────

    [Fact]
    public void GetChecklist_UnknownJob_ReturnsEmptyArray()
    {
        var result = SpellChecklistRegistry.GetChecklist(0);
        Assert.Empty(result);
    }

    [Fact]
    public void GetChecklist_StarterClass_ReturnsEmptyArray()
    {
        // Gladiator, Marauder, etc. have no checklist
        Assert.Empty(SpellChecklistRegistry.GetChecklist(JobRegistry.Gladiator));
        Assert.Empty(SpellChecklistRegistry.GetChecklist(JobRegistry.Marauder));
        Assert.Empty(SpellChecklistRegistry.GetChecklist(JobRegistry.Lancer));
        Assert.Empty(SpellChecklistRegistry.GetChecklist(JobRegistry.Pugilist));
    }

    [Theory]
    [InlineData(JobRegistry.WhiteMage)]
    [InlineData(JobRegistry.Conjurer)]  // CNJ maps to WHM entry
    public void GetChecklist_WhiteMageOrConjurer_ReturnsSameNonEmptyChecklist(uint jobId)
    {
        var result = SpellChecklistRegistry.GetChecklist(jobId);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(JobRegistry.Scholar)]
    [InlineData(JobRegistry.Arcanist)]  // ACN maps to SCH entry
    public void GetChecklist_ScholarOrArcanist_ReturnsSameNonEmptyChecklist(uint jobId)
    {
        var result = SpellChecklistRegistry.GetChecklist(jobId);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(JobRegistry.Sage)]
    [InlineData(JobRegistry.Astrologian)]
    [InlineData(JobRegistry.Warrior)]
    [InlineData(JobRegistry.DarkKnight)]
    [InlineData(JobRegistry.Paladin)]
    [InlineData(JobRegistry.Gunbreaker)]
    [InlineData(JobRegistry.Dragoon)]
    [InlineData(JobRegistry.Monk)]
    [InlineData(JobRegistry.Ninja)]
    [InlineData(JobRegistry.Samurai)]
    [InlineData(JobRegistry.Reaper)]
    [InlineData(JobRegistry.Viper)]
    [InlineData(JobRegistry.Bard)]
    [InlineData(JobRegistry.Machinist)]
    [InlineData(JobRegistry.Dancer)]
    [InlineData(JobRegistry.BlackMage)]
    [InlineData(JobRegistry.Summoner)]
    [InlineData(JobRegistry.RedMage)]
    [InlineData(JobRegistry.Pictomancer)]
    public void GetChecklist_AllAdvancedJobs_ReturnNonEmptyChecklist(uint jobId)
    {
        var result = SpellChecklistRegistry.GetChecklist(jobId);
        Assert.NotEmpty(result);
    }

    // ── Level-aware filtering ─────────────────────────────────────────────

    [Fact]
    public void WHM_GcdDamage_AtLevel1_ReturnsStone()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.WhiteMage);
        var gcdDamage = Array.Find(groups, g => g.Name == "GCD Damage");
        Assert.NotNull(gcdDamage);
        var actions = gcdDamage!.GetActions(1);
        Assert.Single(actions);
        Assert.Equal(WHMActions.Stone.ActionId, actions[0].ActionId);
    }

    [Fact]
    public void WHM_GcdDamage_AtLevel100_ReturnsGlareIII()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.WhiteMage);
        var gcdDamage = Array.Find(groups, g => g.Name == "GCD Damage");
        Assert.NotNull(gcdDamage);
        var actions = gcdDamage!.GetActions(100);
        Assert.Single(actions);
        Assert.Equal(WHMActions.GlareIII.ActionId, actions[0].ActionId);
    }

    [Fact]
    public void WHM_GcdAoEDamage_BelowLevel45_ReturnsEmpty()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.WhiteMage);
        var aoe = Array.Find(groups, g => g.Name == "GCD AoE Damage");
        Assert.NotNull(aoe);
        // Holy unlocks at 45
        var actions = aoe!.GetActions(44);
        Assert.Empty(actions);
    }

    [Fact]
    public void WHM_GcdAoEDamage_AtLevel45_ReturnsHoly()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.WhiteMage);
        var aoe = Array.Find(groups, g => g.Name == "GCD AoE Damage");
        var actions = aoe!.GetActions(45);
        Assert.Single(actions);
        Assert.Equal(WHMActions.Holy.ActionId, actions[0].ActionId);
    }

    [Fact]
    public void WAR_GaugeSpenders_BelowLevel35_ReturnsEmpty()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.Warrior);
        var spenders = Array.Find(groups, g => g.Name == "Gauge Spenders");
        Assert.NotNull(spenders);
        var actions = spenders!.GetActions(34);
        Assert.Empty(actions);
    }

    [Fact]
    public void WAR_GaugeSpenders_AtLevel35_ReturnsInnerBeast()
    {
        var groups = SpellChecklistRegistry.GetChecklist(JobRegistry.Warrior);
        var spenders = Array.Find(groups, g => g.Name == "Gauge Spenders");
        var actions = spenders!.GetActions(35);
        Assert.Contains(actions, a => a.ActionId == WARActions.InnerBeast.ActionId);
    }

    [Fact]
    public void AllJobs_AtLevel100_AllGroupsHaveAtLeastOneAction()
    {
        uint[] allJobs =
        {
            JobRegistry.WhiteMage, JobRegistry.Scholar, JobRegistry.Sage, JobRegistry.Astrologian,
            JobRegistry.Warrior, JobRegistry.DarkKnight, JobRegistry.Paladin, JobRegistry.Gunbreaker,
            JobRegistry.Dragoon, JobRegistry.Monk, JobRegistry.Ninja, JobRegistry.Samurai,
            JobRegistry.Reaper, JobRegistry.Viper, JobRegistry.Bard, JobRegistry.Machinist,
            JobRegistry.Dancer, JobRegistry.BlackMage, JobRegistry.Summoner, JobRegistry.RedMage,
            JobRegistry.Pictomancer
        };

        foreach (var jobId in allJobs)
        {
            var groups = SpellChecklistRegistry.GetChecklist(jobId);
            foreach (var group in groups)
            {
                var actions = group.GetActions(100);
                Assert.True(actions.Length > 0,
                    $"Job {jobId}: group '{group.Name}' returned no actions at level 100");
            }
        }
    }

    [Fact]
    public void AllJobs_AtLevel1_NoGroupThrows()
    {
        uint[] allJobs =
        {
            JobRegistry.WhiteMage, JobRegistry.Scholar, JobRegistry.Sage, JobRegistry.Astrologian,
            JobRegistry.Warrior, JobRegistry.DarkKnight, JobRegistry.Paladin, JobRegistry.Gunbreaker,
            JobRegistry.Dragoon, JobRegistry.Monk, JobRegistry.Ninja, JobRegistry.Samurai,
            JobRegistry.Reaper, JobRegistry.Viper, JobRegistry.Bard, JobRegistry.Machinist,
            JobRegistry.Dancer, JobRegistry.BlackMage, JobRegistry.Summoner, JobRegistry.RedMage,
            JobRegistry.Pictomancer
        };

        foreach (var jobId in allJobs)
        {
            var groups = SpellChecklistRegistry.GetChecklist(jobId);
            foreach (var group in groups)
            {
                // Should not throw — empty is valid for low-level groups
                var exception = Record.Exception(() => group.GetActions(1));
                Assert.Null(exception);
            }
        }
    }
}
