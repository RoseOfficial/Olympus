using System.Linq;
using Olympus.Data;
using Olympus.Services.Action;
using Xunit;

namespace Olympus.Tests.Services.Action;

public sealed class ActionLibraryServiceTests
{
    private readonly ActionLibraryService _sut = new();

    // ── Healers ────────────────────────────────────────────────────────────

    [Fact]
    public void Scholar_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Scholar));
    }

    [Fact]
    public void Scholar_PhysickIsHealingGcd()
    {
        var action = _sut.GetAction(SCHActions.Physick.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsHeal);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Scholar_HasHealingActions()
    {
        Assert.NotEmpty(_sut.GetHealingActions(JobRegistry.Scholar));
    }

    [Fact]
    public void Scholar_HasDamageActions()
    {
        Assert.NotEmpty(_sut.GetDamageActions(JobRegistry.Scholar));
    }

    [Fact]
    public void Arcanist_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Arcanist));
    }

    [Fact]
    public void Astrologian_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Astrologian));
    }

    [Fact]
    public void Astrologian_BeneficIsHealingGcd()
    {
        var action = _sut.GetAction(ASTActions.Benefic.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsHeal);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Astrologian_HasHealingActions()
    {
        Assert.NotEmpty(_sut.GetHealingActions(JobRegistry.Astrologian));
    }

    [Fact]
    public void Sage_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Sage));
    }

    [Fact]
    public void Sage_DiagnosisIsHealingGcd()
    {
        var action = _sut.GetAction(SGEActions.Diagnosis.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsHeal);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Sage_HasHealingActions()
    {
        Assert.NotEmpty(_sut.GetHealingActions(JobRegistry.Sage));
    }

    // ── Tanks ──────────────────────────────────────────────────────────────

    [Fact]
    public void Warrior_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Warrior));
    }

    [Fact]
    public void Warrior_HeavySwingIsDamageGcd()
    {
        var action = _sut.GetAction(WARActions.HeavySwing.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Warrior_HasDamageActions()
    {
        Assert.NotEmpty(_sut.GetDamageActions(JobRegistry.Warrior));
    }

    [Fact]
    public void Marauder_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Marauder));
    }

    [Fact]
    public void Paladin_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Paladin));
    }

    [Fact]
    public void Paladin_FastBladeIsDamageGcd()
    {
        var action = _sut.GetAction(PLDActions.FastBlade.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Gladiator_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Gladiator));
    }

    [Fact]
    public void DarkKnight_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.DarkKnight));
    }

    [Fact]
    public void DarkKnight_HardSlashIsDamageGcd()
    {
        var action = _sut.GetAction(DRKActions.HardSlash.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Gunbreaker_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Gunbreaker));
    }

    [Fact]
    public void Gunbreaker_KeenEdgeIsDamageGcd()
    {
        var action = _sut.GetAction(GNBActions.KeenEdge.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    // ── Melee DPS ──────────────────────────────────────────────────────────

    [Fact]
    public void Monk_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Monk));
    }

    [Fact]
    public void Monk_BootshineIsDamageGcd()
    {
        var action = _sut.GetAction(MNKActions.Bootshine.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Pugilist_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Pugilist));
    }

    [Fact]
    public void Dragoon_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Dragoon));
    }

    [Fact]
    public void Dragoon_TrueThrustIsDamageGcd()
    {
        var action = _sut.GetAction(DRGActions.TrueThrust.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Lancer_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Lancer));
    }

    [Fact]
    public void Ninja_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Ninja));
    }

    [Fact]
    public void Ninja_SpinningEdgeIsDamageGcd()
    {
        var action = _sut.GetAction(NINActions.SpinningEdge.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Rogue_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Rogue));
    }

    [Fact]
    public void Samurai_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Samurai));
    }

    [Fact]
    public void Samurai_HakazeIsDamageGcd()
    {
        var action = _sut.GetAction(SAMActions.Hakaze.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Reaper_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Reaper));
    }

    [Fact]
    public void Reaper_SliceIsDamageGcd()
    {
        var action = _sut.GetAction(RPRActions.Slice.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Viper_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Viper));
    }

    [Fact]
    public void Viper_SteelFangsIsDamageGcd()
    {
        var action = _sut.GetAction(VPRActions.SteelFangs.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    // ── Ranged Physical DPS ────────────────────────────────────────────────

    [Fact]
    public void Bard_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Bard));
    }

    [Fact]
    public void Bard_BurstShotIsDamageGcd()
    {
        var action = _sut.GetAction(BRDActions.BurstShot.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Archer_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Archer));
    }

    [Fact]
    public void Machinist_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Machinist));
    }

    [Fact]
    public void Machinist_DrillIsDamageGcd()
    {
        var action = _sut.GetAction(MCHActions.Drill.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Dancer_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Dancer));
    }

    [Fact]
    public void Dancer_CascadeIsDamageGcd()
    {
        var action = _sut.GetAction(DNCActions.Cascade.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    // ── Caster DPS ─────────────────────────────────────────────────────────

    [Fact]
    public void BlackMage_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.BlackMage));
    }

    [Fact]
    public void BlackMage_FireIsDamageGcd()
    {
        var action = _sut.GetAction(BLMActions.Fire.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Thaumaturge_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Thaumaturge));
    }

    [Fact]
    public void Summoner_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Summoner));
    }

    [Fact]
    public void Summoner_RuinIsDamageGcd()
    {
        var action = _sut.GetAction(SMNActions.Ruin.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void RedMage_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.RedMage));
    }

    [Fact]
    public void RedMage_JoltIsDamageGcd()
    {
        var action = _sut.GetAction(RDMActions.Jolt.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    [Fact]
    public void Pictomancer_HasActionsRegistered()
    {
        Assert.NotEmpty(_sut.GetJobActions(JobRegistry.Pictomancer));
    }

    [Fact]
    public void Pictomancer_FireInRedIsDamageGcd()
    {
        var action = _sut.GetAction(PCTActions.FireInRed.ActionId);
        Assert.NotNull(action);
        Assert.True(action.IsDamage);
        Assert.True(action.IsGCD);
    }

    // ── GetActionsAtLevel ──────────────────────────────────────────────────

    [Fact]
    public void GetActionsAtLevel_Warrior_Level1_ReturnsHeavySwing()
    {
        var actions = _sut.GetActionsAtLevel(JobRegistry.Warrior, 1).ToList();
        Assert.Contains(actions, a => a.ActionId == WARActions.HeavySwing.ActionId);
    }

    [Fact]
    public void GetActionsAtLevel_Scholar_Level4_ReturnsPhysick()
    {
        var actions = _sut.GetActionsAtLevel(JobRegistry.Scholar, 4).ToList();
        Assert.Contains(actions, a => a.ActionId == SCHActions.Physick.ActionId);
    }

    // ── HasAction ──────────────────────────────────────────────────────────

    [Fact]
    public void HasAction_ScholarAdloquium_ReturnsTrue()
    {
        Assert.True(_sut.HasAction(SCHActions.Adloquium.ActionId));
    }

    [Fact]
    public void HasAction_UnknownActionId_ReturnsFalse()
    {
        Assert.False(_sut.HasAction(999999u));
    }
}
