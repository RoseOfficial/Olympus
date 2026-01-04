using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;
using Olympus.Services.Action;

namespace Olympus.Services.Healing;

/// <summary>
/// Result of evaluating a spell candidate.
/// </summary>
public record SpellEvaluationResult
{
    public bool IsValid { get; init; }
    public ActionDefinition? Action { get; init; }
    public int HealAmount { get; init; }
    public string? RejectionReason { get; init; }
}

/// <summary>
/// Evaluates spell candidates for healing decisions.
/// Handles level checks, config enablement, cooldowns, and overheal prevention.
/// </summary>
public class SpellCandidateEvaluator
{
    private readonly IActionService actionService;
    private readonly ISpellEnablementService enablementService;
    private readonly List<SpellCandidateDebug> candidates = [];

    public SpellCandidateEvaluator(
        IActionService actionService,
        ISpellEnablementService enablementService)
    {
        this.actionService = actionService;
        this.enablementService = enablementService;
    }

    /// <summary>
    /// Gets all candidates evaluated in the current session.
    /// </summary>
    public IReadOnlyList<SpellCandidateDebug> Candidates => candidates;

    /// <summary>
    /// Clears all tracked candidates for a new evaluation session.
    /// </summary>
    public void ClearCandidates()
    {
        candidates.Clear();
    }

    /// <summary>
    /// Evaluates a single-target heal spell.
    /// </summary>
    /// <param name="action">The heal action to evaluate.</param>
    /// <param name="playerLevel">The player's current level.</param>
    /// <param name="mind">The player's Mind stat.</param>
    /// <param name="det">The player's Determination stat.</param>
    /// <param name="wd">The player's weapon damage.</param>
    /// <param name="target">The target to heal (optional, for overheal check).</param>
    /// <param name="missingHp">The target's missing HP for overheal prevention. Use 0 to skip overheal check.</param>
    /// <returns>Evaluation result indicating if spell is valid.</returns>
    public SpellEvaluationResult EvaluateSingleTarget(
        ActionDefinition action,
        byte playerLevel,
        int mind, int det, int wd,
        IBattleChara? target = null,
        int missingHp = 0)
    {
        // Check level
        if (playerLevel < action.MinLevel)
        {
            var reason = $"Level too low ({playerLevel} < {action.MinLevel})";
            TrackRejected(action, 0, reason);
            return new SpellEvaluationResult { IsValid = false, RejectionReason = reason };
        }

        // Check config enabled
        if (!enablementService.IsSpellEnabled(action.ActionId))
        {
            TrackRejected(action, 0, "Disabled in config");
            return new SpellEvaluationResult { IsValid = false, RejectionReason = "Disabled in config" };
        }

        // Check cooldown
        if (!actionService.IsActionReady(action.ActionId))
        {
            TrackRejected(action, 0, "On cooldown");
            return new SpellEvaluationResult { IsValid = false, RejectionReason = "On cooldown" };
        }

        // Calculate heal amount
        var healAmount = action.EstimateHealAmount(mind, det, wd, playerLevel);

        // Check for overheal (only for potency-based heals, not Benediction)
        if (missingHp > 0 && action.HealPotency > 0 && healAmount > missingHp)
        {
            var reason = $"Would overheal ({healAmount} > {missingHp} missing)";
            TrackRejected(action, healAmount, reason);
            return new SpellEvaluationResult { IsValid = false, RejectionReason = reason };
        }

        // Track as valid candidate
        TrackValid(action, healAmount);

        return new SpellEvaluationResult
        {
            IsValid = true,
            Action = action,
            HealAmount = healAmount
        };
    }

    /// <summary>
    /// Evaluates an AoE heal spell (no overheal check).
    /// </summary>
    public SpellEvaluationResult EvaluateAoE(
        ActionDefinition action,
        byte playerLevel,
        int mind, int det, int wd)
    {
        // Check level
        if (playerLevel < action.MinLevel)
        {
            var reason = $"Level too low ({playerLevel} < {action.MinLevel})";
            TrackRejected(action, 0, reason);
            return new SpellEvaluationResult { IsValid = false, RejectionReason = reason };
        }

        // Check config enabled
        if (!enablementService.IsSpellEnabled(action.ActionId))
        {
            TrackRejected(action, 0, "Disabled in config");
            return new SpellEvaluationResult { IsValid = false, RejectionReason = "Disabled in config" };
        }

        // Check cooldown
        if (!actionService.IsActionReady(action.ActionId))
        {
            TrackRejected(action, 0, "On cooldown");
            return new SpellEvaluationResult { IsValid = false, RejectionReason = "On cooldown" };
        }

        // Calculate heal amount
        var healAmount = action.EstimateHealAmount(mind, det, wd, playerLevel);

        // Track as valid candidate
        TrackValid(action, healAmount);

        return new SpellEvaluationResult
        {
            IsValid = true,
            Action = action,
            HealAmount = healAmount
        };
    }

    /// <summary>
    /// Tracks a spell that was rejected during evaluation.
    /// </summary>
    public void TrackRejected(ActionDefinition action, int healAmount, string reason)
    {
        candidates.Add(new SpellCandidateDebug
        {
            SpellName = action.Name,
            ActionId = action.ActionId,
            HealAmount = healAmount,
            Efficiency = 0,
            Score = 0,
            Bonuses = "",
            WasSelected = false,
            RejectionReason = reason
        });
    }

    private void TrackValid(ActionDefinition action, int healAmount)
    {
        candidates.Add(new SpellCandidateDebug
        {
            SpellName = action.Name,
            ActionId = action.ActionId,
            HealAmount = healAmount,
            Efficiency = 1.0f,
            Score = 1.0f,
            Bonuses = "Tiered priority",
            WasSelected = false,
            RejectionReason = null
        });
    }

    /// <summary>
    /// Marks a candidate as selected in the tracking list.
    /// </summary>
    public void MarkAsSelected(uint actionId)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].ActionId == actionId)
            {
                candidates[i] = candidates[i] with { WasSelected = true };
                break;
            }
        }
    }

    /// <summary>
    /// Gets a copy of candidates for debug output.
    /// </summary>
    public List<SpellCandidateDebug> GetCandidatesCopy()
    {
        return [.. candidates];
    }
}
