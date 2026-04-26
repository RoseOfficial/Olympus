using Olympus.Data;
using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.CirceCore;

/// <summary>
/// Unit tests for Circe.ComputeMeleeComboStep and Circe.ComputeMoulinetStep.
/// Exercises the pure translation from action-replacement / ManaStacks /
/// vanilla-combo-field state into the 5-step Enchanted melee chain index
/// and the 2-step Moulinet AoE chain index. Locks in the precedence rule
/// that action replacement shadows ManaStacks shadows the vanilla combo --
/// a regression here causes the rotation to pick the wrong action mid-chain.
/// </summary>
public class CirceGaugeTests
{
    // ----- ComputeMeleeComboStep -----

    [Fact]
    public void ComputeMeleeComboStep_Returns_0_When_Idle()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 0,
            comboAction: 0,
            comboTimer: 0f);

        Assert.Equal(0, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_1_When_RiposteAdjustedToZwerchhau()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedZwerchhau.ActionId,
            manaStacks: 0,
            comboAction: 0,
            comboTimer: 0f);

        Assert.Equal(1, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_2_When_RiposteAdjustedToRedoublement()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRedoublement.ActionId,
            manaStacks: 0,
            comboAction: 0,
            comboTimer: 0f);

        Assert.Equal(2, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_3_When_ManaStacks_AtLeast_3()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 3,
            comboAction: 0,
            comboTimer: 0f);

        Assert.Equal(3, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_4_When_Verflare_And_TimerActive()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 0,
            comboAction: RDMActions.Verflare.ActionId,
            comboTimer: 5f);

        Assert.Equal(4, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_4_When_Verholy_And_TimerActive()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 0,
            comboAction: RDMActions.Verholy.ActionId,
            comboTimer: 5f);

        Assert.Equal(4, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_5_When_Scorch_And_TimerActive()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 0,
            comboAction: RDMActions.Scorch.ActionId,
            comboTimer: 5f);

        Assert.Equal(5, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Returns_0_When_TimerExpired_For_LateChain()
    {
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRiposte.ActionId,
            manaStacks: 0,
            comboAction: RDMActions.Scorch.ActionId,
            comboTimer: 0f);

        Assert.Equal(0, step);
    }

    [Fact]
    public void ComputeMeleeComboStep_Prefers_AdjustedAction_Over_ManaStacks()
    {
        // Both action-replacement (would say step 2) and ManaStacks (would say
        // step 3) are present. The implementation must prefer action replacement,
        // otherwise the rotation skips Redoublement and tries to fire a Finisher
        // mid-chain with no Verfire/Verstone proc available.
        var step = Circe.ComputeMeleeComboStep(
            adjustedRiposteId: RDMActions.EnchantedRedoublement.ActionId,
            manaStacks: 3,
            comboAction: 0,
            comboTimer: 0f);

        Assert.Equal(2, step);
    }

    // ----- ComputeMoulinetStep -----

    [Fact]
    public void ComputeMoulinetStep_Returns_0_When_NoReplacement()
    {
        var step = Circe.ComputeMoulinetStep(adjustedMoulinetId: RDMActions.EnchantedMoulinet.ActionId);

        Assert.Equal(0, step);
    }

    [Fact]
    public void ComputeMoulinetStep_Returns_1_When_AdjustedToDeux()
    {
        var step = Circe.ComputeMoulinetStep(adjustedMoulinetId: RDMActions.EnchantedMoulinetDeux.ActionId);

        Assert.Equal(1, step);
    }

    [Fact]
    public void ComputeMoulinetStep_Returns_2_When_AdjustedToTrois()
    {
        var step = Circe.ComputeMoulinetStep(adjustedMoulinetId: RDMActions.EnchantedMoulinetTrois.ActionId);

        Assert.Equal(2, step);
    }
}
