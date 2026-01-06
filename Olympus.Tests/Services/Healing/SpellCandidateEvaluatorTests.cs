using System.Numerics;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Xunit;

namespace Olympus.Tests.Services.Healing;

/// <summary>
/// Tests for SpellCandidateEvaluator.
/// Uses mock implementations to test evaluation logic without Dalamud runtime.
/// </summary>
public class SpellCandidateEvaluatorTests
{
    #region Test Helpers

    /// <summary>
    /// Mock action service for testing.
    /// </summary>
    private class MockActionService : IActionService
    {
        public GcdState CurrentGcdState => GcdState.Ready;
        public float GcdRemaining => 0f;
        public float AnimationLockRemaining => 0f;
        public bool IsCasting => false;
        public bool CanExecuteGcd => true;
        public bool CanExecuteOgcd => true;

        private readonly HashSet<uint> readyActions = [];
        private readonly Dictionary<uint, float> cooldowns = [];

        public void SetActionReady(uint actionId, bool ready = true)
        {
            if (ready)
                readyActions.Add(actionId);
            else
                readyActions.Remove(actionId);
        }

        public void SetCooldown(uint actionId, float remaining)
        {
            cooldowns[actionId] = remaining;
            if (remaining <= 0)
                readyActions.Add(actionId);
            else
                readyActions.Remove(actionId);
        }

        public bool IsActionReady(uint actionId) => readyActions.Contains(actionId);
        public float GetCooldownRemaining(uint actionId) => cooldowns.GetValueOrDefault(actionId, 0f);
        public int GetAvailableWeaveSlots() => 2;
        public bool ExecuteGcd(ActionDefinition action, ulong targetId) => true;
        public bool ExecuteOgcd(ActionDefinition action, ulong targetId) => true;
        public bool ExecuteGroundTargetedOgcd(ActionDefinition action, Vector3 targetPosition) => true;
        public bool CanExecuteAction(ActionDefinition action) => true;
        public uint GetCurrentCharges(uint actionId) => readyActions.Contains(actionId) ? 1u : 0u;
        public ushort GetMaxCharges(uint actionId, uint level) => 1;
    }

    /// <summary>
    /// Mock enablement service for testing.
    /// </summary>
    private class MockEnablementService : ISpellEnablementService
    {
        private readonly HashSet<uint> enabledSpells = [];
        private bool defaultEnabled = true;

        public void SetSpellEnabled(uint actionId, bool enabled)
        {
            if (enabled)
                enabledSpells.Add(actionId);
            else
                enabledSpells.Remove(actionId);
        }

        public void SetDefaultEnabled(bool enabled)
        {
            defaultEnabled = enabled;
        }

        public bool IsSpellEnabled(uint actionId)
        {
            // If explicitly set, use that value
            if (enabledSpells.Contains(actionId))
                return true;
            // Otherwise use default
            return defaultEnabled;
        }
    }

    private static SpellCandidateEvaluator CreateEvaluator(
        out MockActionService actionService,
        out MockEnablementService enablementService)
    {
        actionService = new MockActionService();
        enablementService = new MockEnablementService();
        return new SpellCandidateEvaluator(actionService, enablementService);
    }

    // Standard test stats
    private const int TestMind = 3000;
    private const int TestDet = 2000;
    private const int TestWd = 130;

    #endregion

    #region EvaluateSingleTarget Tests

    [Fact]
    public void EvaluateSingleTarget_ValidSpell_ReturnsSuccess()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetSpellEnabled(WHMActions.CureII.ActionId, true);

        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.True(result.IsValid);
        Assert.Equal(WHMActions.CureII, result.Action);
        Assert.True(result.HealAmount > 0);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void EvaluateSingleTarget_LevelTooLow_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetSpellEnabled(WHMActions.CureII.ActionId, true);

        // Cure II requires level 30
        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 20,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Contains("Level too low", result.RejectionReason);
    }

    [Fact]
    public void EvaluateSingleTarget_DisabledInConfig_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetDefaultEnabled(false);

        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Equal("Disabled in config", result.RejectionReason);
    }

    [Fact]
    public void EvaluateSingleTarget_OnCooldown_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetCooldown(WHMActions.CureII.ActionId, 2.5f);
        enablementService.SetSpellEnabled(WHMActions.CureII.ActionId, true);

        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Equal("On cooldown", result.RejectionReason);
    }

    [Fact]
    public void EvaluateSingleTarget_WouldOverheal_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetSpellEnabled(WHMActions.CureII.ActionId, true);

        // Cure II heals for ~800 potency worth, we'll say only 100 HP missing
        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd,
            missingHp: 100);

        Assert.False(result.IsValid);
        Assert.Contains("Would overheal", result.RejectionReason);
    }

    [Fact]
    public void EvaluateSingleTarget_ZeroMissingHp_SkipsOverhealCheck()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetSpellEnabled(WHMActions.CureII.ActionId, true);

        // MissingHp = 0 means skip overheal check
        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd,
            missingHp: 0);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EvaluateSingleTarget_Benediction_SkipsOverhealCheck()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Benediction.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Benediction.ActionId, true);

        // Benediction has 0 potency (heals to full) so overheal check should be skipped
        var result = evaluator.EvaluateSingleTarget(
            WHMActions.Benediction,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd,
            missingHp: 100);

        // Should pass because Benediction has HealPotency = 0
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EvaluateSingleTarget_TracksCandidate()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Cure.ActionId, true);

        evaluator.EvaluateSingleTarget(
            WHMActions.Cure,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.Single(evaluator.Candidates);
        Assert.Equal("Cure", evaluator.Candidates[0].SpellName);
        Assert.Equal(WHMActions.Cure.ActionId, evaluator.Candidates[0].ActionId);
    }

    [Fact]
    public void EvaluateSingleTarget_RejectedSpell_TracksWithReason()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetCooldown(WHMActions.Cure.ActionId, 2.5f);
        enablementService.SetSpellEnabled(WHMActions.Cure.ActionId, true);

        evaluator.EvaluateSingleTarget(
            WHMActions.Cure,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.Single(evaluator.Candidates);
        Assert.False(evaluator.Candidates[0].WasSelected);
        Assert.Equal("On cooldown", evaluator.Candidates[0].RejectionReason);
    }

    #endregion

    #region EvaluateAoE Tests

    [Fact]
    public void EvaluateAoE_ValidSpell_ReturnsSuccess()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Medica.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Medica.ActionId, true);

        var result = evaluator.EvaluateAoE(
            WHMActions.Medica,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.True(result.IsValid);
        Assert.Equal(WHMActions.Medica, result.Action);
        Assert.True(result.HealAmount > 0);
    }

    [Fact]
    public void EvaluateAoE_LevelTooLow_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.MedicaII.ActionId);
        enablementService.SetSpellEnabled(WHMActions.MedicaII.ActionId, true);

        // Medica II requires level 50
        var result = evaluator.EvaluateAoE(
            WHMActions.MedicaII,
            playerLevel: 40,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Contains("Level too low", result.RejectionReason);
    }

    [Fact]
    public void EvaluateAoE_DisabledInConfig_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Medica.ActionId);
        enablementService.SetDefaultEnabled(false);

        var result = evaluator.EvaluateAoE(
            WHMActions.Medica,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Equal("Disabled in config", result.RejectionReason);
    }

    [Fact]
    public void EvaluateAoE_OnCooldown_ReturnsFailure()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetCooldown(WHMActions.Medica.ActionId, 1.5f);
        enablementService.SetSpellEnabled(WHMActions.Medica.ActionId, true);

        var result = evaluator.EvaluateAoE(
            WHMActions.Medica,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Equal("On cooldown", result.RejectionReason);
    }

    [Fact]
    public void EvaluateAoE_NoOverhealCheck()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Medica.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Medica.ActionId, true);

        // AoE heals don't check overheal
        var result = evaluator.EvaluateAoE(
            WHMActions.Medica,
            playerLevel: 50,
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.True(result.IsValid);
    }

    #endregion

    #region ClearCandidates Tests

    [Fact]
    public void ClearCandidates_RemovesAllTrackedCandidates()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetDefaultEnabled(true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);
        evaluator.EvaluateSingleTarget(WHMActions.CureII, 50, TestMind, TestDet, TestWd);

        Assert.Equal(2, evaluator.Candidates.Count);

        evaluator.ClearCandidates();

        Assert.Empty(evaluator.Candidates);
    }

    #endregion

    #region MarkAsSelected Tests

    [Fact]
    public void MarkAsSelected_SetsWasSelectedTrue()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Cure.ActionId, true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);

        Assert.False(evaluator.Candidates[0].WasSelected);

        evaluator.MarkAsSelected(WHMActions.Cure.ActionId);

        Assert.True(evaluator.Candidates[0].WasSelected);
    }

    [Fact]
    public void MarkAsSelected_OnlyMarksMatchingAction()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        enablementService.SetDefaultEnabled(true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);
        evaluator.EvaluateSingleTarget(WHMActions.CureII, 50, TestMind, TestDet, TestWd);

        evaluator.MarkAsSelected(WHMActions.CureII.ActionId);

        var cure = evaluator.Candidates.First(c => c.ActionId == WHMActions.Cure.ActionId);
        var cureII = evaluator.Candidates.First(c => c.ActionId == WHMActions.CureII.ActionId);

        Assert.False(cure.WasSelected);
        Assert.True(cureII.WasSelected);
    }

    [Fact]
    public void MarkAsSelected_NonExistentAction_NoException()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Cure.ActionId, true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);

        // Should not throw
        evaluator.MarkAsSelected(99999);

        Assert.False(evaluator.Candidates[0].WasSelected);
    }

    #endregion

    #region GetCandidatesCopy Tests

    [Fact]
    public void GetCandidatesCopy_ReturnsIndependentList()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        enablementService.SetSpellEnabled(WHMActions.Cure.ActionId, true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);

        var copy = evaluator.GetCandidatesCopy();
        Assert.Single(copy);

        evaluator.ClearCandidates();

        // Copy should still have the item
        Assert.Single(copy);
        Assert.Empty(evaluator.Candidates);
    }

    #endregion

    #region TrackRejected Tests

    [Fact]
    public void TrackRejected_AddsToCandiates()
    {
        var evaluator = CreateEvaluator(out _, out _);

        evaluator.TrackRejected(WHMActions.Regen, 0, "Target already has Regen");

        Assert.Single(evaluator.Candidates);
        Assert.Equal("Regen", evaluator.Candidates[0].SpellName);
        Assert.Equal(WHMActions.Regen.ActionId, evaluator.Candidates[0].ActionId);
        Assert.Equal("Target already has Regen", evaluator.Candidates[0].RejectionReason);
        Assert.False(evaluator.Candidates[0].WasSelected);
    }

    #endregion

    #region Multiple Evaluations Tests

    [Fact]
    public void MultipleEvaluations_AccumulateCandidates()
    {
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);
        actionService.SetActionReady(WHMActions.Cure.ActionId);
        actionService.SetActionReady(WHMActions.CureII.ActionId);
        actionService.SetActionReady(WHMActions.Medica.ActionId);
        enablementService.SetDefaultEnabled(true);

        evaluator.EvaluateSingleTarget(WHMActions.Cure, 50, TestMind, TestDet, TestWd);
        evaluator.EvaluateSingleTarget(WHMActions.CureII, 50, TestMind, TestDet, TestWd);
        evaluator.EvaluateAoE(WHMActions.Medica, 50, TestMind, TestDet, TestWd);

        Assert.Equal(3, evaluator.Candidates.Count);
    }

    [Fact]
    public void EvaluationOrder_RespectsCheckPriority()
    {
        // Verify that level is checked before config, config before cooldown
        var evaluator = CreateEvaluator(out var actionService, out var enablementService);

        // Spell is disabled and on cooldown, but level too low should be first rejection
        actionService.SetCooldown(WHMActions.CureII.ActionId, 5f);
        enablementService.SetDefaultEnabled(false);

        var result = evaluator.EvaluateSingleTarget(
            WHMActions.CureII,
            playerLevel: 10, // Too low for Cure II (requires 30)
            mind: TestMind, det: TestDet, wd: TestWd);

        Assert.False(result.IsValid);
        Assert.Contains("Level too low", result.RejectionReason);
    }

    #endregion

    #region SpellEvaluationResult Tests

    [Fact]
    public void SpellEvaluationResult_DefaultValues()
    {
        var result = new SpellEvaluationResult();

        Assert.False(result.IsValid);
        Assert.Null(result.Action);
        Assert.Equal(0, result.HealAmount);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void SpellEvaluationResult_WithValues()
    {
        var result = new SpellEvaluationResult
        {
            IsValid = true,
            Action = WHMActions.CureII,
            HealAmount = 5000,
            RejectionReason = null
        };

        Assert.True(result.IsValid);
        Assert.Equal(WHMActions.CureII, result.Action);
        Assert.Equal(5000, result.HealAmount);
        Assert.Null(result.RejectionReason);
    }

    #endregion
}
