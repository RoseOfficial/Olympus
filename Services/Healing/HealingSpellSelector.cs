using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Action;
using Olympus.Services.Healing.Models;
using Olympus.Services.Healing.Strategies;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;

namespace Olympus.Services.Healing;

/// <summary>
/// Debug information about a candidate spell evaluation.
/// </summary>
public sealed record SpellCandidateDebug
{
    public string SpellName { get; init; } = "";
    public uint ActionId { get; init; }
    public int HealAmount { get; init; }
    public float Efficiency { get; init; }
    public float Score { get; init; }
    public string Bonuses { get; init; } = "";
    public bool WasSelected { get; init; }
    public string? RejectionReason { get; init; }
}

/// <summary>
/// Debug information about the last spell selection decision.
/// </summary>
public sealed class SpellSelectionDebug
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string SelectionType { get; init; } = ""; // "Single" or "AoE"
    public string TargetName { get; init; } = "";
    public int MissingHp { get; init; }
    public float TargetHpPercent { get; init; }
    public bool IsWeaveWindow { get; init; }
    public int LilyCount { get; init; }
    public int BloodLilyCount { get; init; }
    public string LilyStrategy { get; init; } = "";
    public List<SpellCandidateDebug> Candidates { get; init; } = new();
    public string? SelectedSpell { get; init; }
    public string? SelectionReason { get; init; }
    public float SecondsAgo => (float)(DateTime.Now - Timestamp).TotalSeconds;
}

/// <summary>
/// Intelligent healing spell selector that coordinates between different
/// selection strategies (tiered vs scored) based on configuration.
/// </summary>
public class HealingSpellSelector : IHealingSpellSelector
{
    private readonly IPlayerStatsService playerStatsService;
    private readonly IHpPredictionService hpPredictionService;
    private readonly ICombatEventService combatEventService;
    private readonly Configuration configuration;
    private readonly IErrorMetricsService? errorMetrics;
    private readonly SpellCandidateEvaluator evaluator;

    // Strategies
    private readonly TieredHealSelectionStrategy tieredStrategy = new();
    private readonly ScoredHealSelectionStrategy scoredStrategy = new();

    // Debug tracking for last selection
    private SpellSelectionDebug? lastSelection;

    /// <summary>
    /// Gets the last spell selection decision for debugging.
    /// </summary>
    public SpellSelectionDebug? LastSelection => lastSelection;

    public HealingSpellSelector(
        IActionService actionService,
        IPlayerStatsService playerStatsService,
        IHpPredictionService hpPredictionService,
        ICombatEventService combatEventService,
        Configuration configuration,
        IErrorMetricsService? errorMetrics = null)
        : this(actionService, playerStatsService, hpPredictionService, combatEventService, configuration,
               new SpellEnablementService(configuration), errorMetrics)
    {
    }

    public HealingSpellSelector(
        IActionService actionService,
        IPlayerStatsService playerStatsService,
        IHpPredictionService hpPredictionService,
        ICombatEventService combatEventService,
        Configuration configuration,
        ISpellEnablementService enablementService,
        IErrorMetricsService? errorMetrics = null)
    {
        this.playerStatsService = playerStatsService;
        this.hpPredictionService = hpPredictionService;
        this.combatEventService = combatEventService;
        this.configuration = configuration;
        this.errorMetrics = errorMetrics;
        this.evaluator = new SpellCandidateEvaluator(actionService, enablementService);
    }

    /// <summary>
    /// Gets the currently active selection strategy based on configuration.
    /// </summary>
    private IHealSelectionStrategy ActiveStrategy =>
        configuration.Healing.EnableScoredHealSelection ? scoredStrategy : tieredStrategy;

    /// <summary>
    /// Selects the best single-target heal for a given target.
    /// Delegates to the active strategy (tiered or scored).
    /// </summary>
    public (ActionDefinition? action, int healAmount) SelectBestSingleHeal(
        IPlayerCharacter player,
        IBattleChara target,
        bool isWeaveWindow,
        bool hasFreecure = false,
        bool hasRegen = false,
        float regenRemaining = 0f,
        bool isInMpConservationMode = false)
    {
        evaluator.ClearCandidates();

        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var missingHp = (int)(target.MaxHp - hpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp));
        var hpPercent = hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        var combatDuration = combatEventService.GetCombatDurationSeconds();

        // Build context for strategy
        var context = new HealSelectionContext
        {
            Player = player,
            Target = target,
            Mind = mind,
            Det = det,
            Wd = wd,
            MissingHp = missingHp,
            HpPercent = hpPercent,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            IsWeaveWindow = isWeaveWindow,
            HasFreecure = hasFreecure,
            HasRegen = hasRegen,
            RegenRemaining = regenRemaining,
            IsInMpConservationMode = isInMpConservationMode &&
                configuration.Healing.EnableMpAwareSpellSelection,
            LilyStrategy = configuration.Healing.LilyStrategy,
            CombatDuration = combatDuration,
            Config = configuration.Healing
        };

        // Delegate to active strategy
        var (action, healAmount, selectionReason) = ActiveStrategy.SelectBestSingleHeal(context, evaluator);

        // Build debug info
        lastSelection = new SpellSelectionDebug
        {
            SelectionType = $"Single ({ActiveStrategy.StrategyName})",
            TargetName = target.Name.TextValue,
            MissingHp = missingHp,
            TargetHpPercent = hpPercent,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = configuration.Healing.LilyStrategy.ToString(),
            Candidates = evaluator.GetCandidatesCopy(),
            SelectedSpell = action?.Name,
            SelectionReason = selectionReason
        };

        return (action, healAmount);
    }

    /// <summary>
    /// Selects the best AoE heal when multiple party members need healing.
    /// Delegates to the active strategy (tiered or scored).
    /// </summary>
    public (ActionDefinition? action, int healAmount, IBattleChara? cureIIITarget) SelectBestAoEHeal(
        IPlayerCharacter player,
        int averageMissingHp,
        int injuredCount,
        bool anyHaveRegen,
        bool isWeaveWindow,
        int cureIIITargetCount = 0,
        IBattleChara? cureIIITarget = null,
        bool isInMpConservationMode = false)
    {
        evaluator.ClearCandidates();

        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        var combatDuration = combatEventService.GetCombatDurationSeconds();

        // Build context for strategy
        var context = new AoEHealSelectionContext
        {
            Player = player,
            Mind = mind,
            Det = det,
            Wd = wd,
            AverageMissingHp = averageMissingHp,
            InjuredCount = injuredCount,
            AnyHaveRegen = anyHaveRegen,
            IsWeaveWindow = isWeaveWindow,
            CureIIITargetCount = cureIIITargetCount,
            CureIIITarget = cureIIITarget,
            IsInMpConservationMode = isInMpConservationMode &&
                configuration.Healing.EnableMpAwareSpellSelection,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = configuration.Healing.LilyStrategy,
            CombatDuration = combatDuration,
            Config = configuration.Healing
        };

        // Delegate to active strategy
        var (action, healAmount, selectedCureIIITarget, selectionReason) =
            ActiveStrategy.SelectBestAoEHeal(context, evaluator);

        // Build debug info
        lastSelection = new SpellSelectionDebug
        {
            SelectionType = $"AoE ({ActiveStrategy.StrategyName})",
            TargetName = selectedCureIIITarget is not null
                ? $"{cureIIITargetCount} stacked around {selectedCureIIITarget.Name}"
                : $"{injuredCount} injured",
            MissingHp = averageMissingHp,
            TargetHpPercent = 0,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = configuration.Healing.LilyStrategy.ToString(),
            Candidates = evaluator.GetCandidatesCopy(),
            SelectedSpell = action?.Name,
            SelectionReason = selectionReason
        };

        return (action, healAmount, selectedCureIIITarget);
    }

    /// <summary>
    /// Gets the current Lily count from the WHM job gauge.
    /// Virtual to allow testing with mocked lily counts.
    /// </summary>
    protected virtual int GetLilyCount()
    {
        return SafeGameAccess.GetWhmLilyCount(errorMetrics);
    }

    /// <summary>
    /// Gets the current Blood Lily count from the WHM job gauge.
    /// Virtual to allow testing with mocked Blood Lily counts.
    /// </summary>
    protected virtual int GetBloodLilyCount()
    {
        return SafeGameAccess.GetWhmBloodLilyCount(errorMetrics);
    }

    /// <summary>
    /// Debug info showing current spell selection state.
    /// </summary>
    public string GetDebugInfo(IPlayerCharacter player)
    {
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        return $"Lilies: {lilyCount}/3 | Blood: {bloodLilyCount}/3 | Strategy: {configuration.Healing.LilyStrategy} | Selection: {ActiveStrategy.StrategyName} | BeneThreshold: {configuration.Healing.BenedictionEmergencyThreshold:P0}";
    }
}
