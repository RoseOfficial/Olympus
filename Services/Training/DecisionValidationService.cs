namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;

/// <summary>
/// Service for validating rotation decisions and tracking optimal play statistics.
/// Analyzes whether player decisions were optimal and provides feedback.
/// </summary>
public sealed class DecisionValidationService
{
    private readonly TrainingConfig config;
    private readonly IPluginLog? log;

    private readonly List<DecisionResult> recentValidations = new();
    private readonly Dictionary<string, DecisionStatistics> statisticsByConcept = new();
    private readonly object validationsLock = new();

    private const int MaxRecentValidations = 50;

    public DecisionValidationService(TrainingConfig config, IPluginLog? log = null)
    {
        this.config = config;
        this.log = log;
    }

    /// <summary>
    /// Whether decision validation is enabled.
    /// </summary>
    public bool IsEnabled => config.EnableTraining;

    /// <summary>
    /// Recent validation results (most recent first).
    /// </summary>
    public IReadOnlyList<DecisionResult> RecentValidations
    {
        get
        {
            lock (this.validationsLock)
            {
                return this.recentValidations.ToList();
            }
        }
    }

    /// <summary>
    /// The most recent validation result.
    /// </summary>
    public DecisionResult? CurrentValidation
    {
        get
        {
            lock (this.validationsLock)
            {
                return this.recentValidations.FirstOrDefault();
            }
        }
    }

    /// <summary>
    /// Gets statistics for a specific concept.
    /// </summary>
    public DecisionStatistics? GetStatistics(string conceptId)
    {
        lock (this.validationsLock)
        {
            return this.statisticsByConcept.TryGetValue(conceptId, out var stats) ? stats : null;
        }
    }

    /// <summary>
    /// Gets all tracked statistics.
    /// </summary>
    public IReadOnlyDictionary<string, DecisionStatistics> AllStatistics
    {
        get
        {
            lock (this.validationsLock)
            {
                return new Dictionary<string, DecisionStatistics>(this.statisticsByConcept);
            }
        }
    }

    /// <summary>
    /// Records a decision validation result.
    /// </summary>
    public void RecordValidation(DecisionResult result)
    {
        if (!IsEnabled)
            return;

        lock (this.validationsLock)
        {
            // Add to recent validations
            this.recentValidations.Insert(0, result);

            // Trim to max size
            while (this.recentValidations.Count > MaxRecentValidations)
            {
                this.recentValidations.RemoveAt(this.recentValidations.Count - 1);
            }

            // Update statistics for the concept
            if (!string.IsNullOrEmpty(result.ConceptId))
            {
                if (!this.statisticsByConcept.TryGetValue(result.ConceptId, out var stats))
                {
                    stats = new DecisionStatistics { ConceptId = result.ConceptId };
                    this.statisticsByConcept[result.ConceptId] = stats;
                }

                stats.RecordDecision(result.Outcome);
            }
        }

        this.log?.Debug(
            "Decision validation: {Action} = {Symbol} ({Outcome})",
            result.ActualActionName,
            result.Symbol,
            result.Outcome);
    }

    /// <summary>
    /// Validates a decision and records the result.
    /// Call this when an action is executed to evaluate if it was optimal.
    /// </summary>
    public DecisionResult ValidateAndRecord(
        uint actualActionId,
        string actualActionName,
        ValidationOutcome outcome,
        string explanation,
        uint? optimalActionId = null,
        string? optimalActionName = null,
        string? whatWouldBeBetter = null,
        string? conceptId = null,
        int? potencyDifference = null,
        string? contextInfo = null)
    {
        var result = outcome switch
        {
            ValidationOutcome.Optimal => DecisionResult.Optimal(
                actualActionId,
                actualActionName,
                explanation,
                conceptId),
            ValidationOutcome.Acceptable => DecisionResult.Acceptable(
                actualActionId,
                actualActionName,
                optimalActionId ?? 0,
                optimalActionName ?? string.Empty,
                explanation,
                whatWouldBeBetter ?? string.Empty,
                conceptId),
            ValidationOutcome.Suboptimal => DecisionResult.Suboptimal(
                actualActionId,
                actualActionName,
                optimalActionId ?? 0,
                optimalActionName ?? string.Empty,
                explanation,
                whatWouldBeBetter ?? string.Empty,
                conceptId,
                potencyDifference),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

        RecordValidation(result);
        return result;
    }

    /// <summary>
    /// Records an optimal decision.
    /// </summary>
    public DecisionResult RecordOptimal(
        uint actionId,
        string actionName,
        string explanation,
        string? conceptId = null)
    {
        var result = DecisionResult.Optimal(actionId, actionName, explanation, conceptId);
        RecordValidation(result);
        return result;
    }

    /// <summary>
    /// Records an acceptable decision.
    /// </summary>
    public DecisionResult RecordAcceptable(
        uint actionId,
        string actionName,
        uint optimalActionId,
        string optimalActionName,
        string explanation,
        string whatWouldBeBetter,
        string? conceptId = null)
    {
        var result = DecisionResult.Acceptable(
            actionId,
            actionName,
            optimalActionId,
            optimalActionName,
            explanation,
            whatWouldBeBetter,
            conceptId);
        RecordValidation(result);
        return result;
    }

    /// <summary>
    /// Records a suboptimal decision.
    /// </summary>
    public DecisionResult RecordSuboptimal(
        uint actionId,
        string actionName,
        uint optimalActionId,
        string optimalActionName,
        string explanation,
        string whatWouldBeBetter,
        string? conceptId = null,
        int? potencyDifference = null)
    {
        var result = DecisionResult.Suboptimal(
            actionId,
            actionName,
            optimalActionId,
            optimalActionName,
            explanation,
            whatWouldBeBetter,
            conceptId,
            potencyDifference);
        RecordValidation(result);
        return result;
    }

    /// <summary>
    /// Gets the overall optimal decision rate across all concepts.
    /// </summary>
    public float GetOverallOptimalRate()
    {
        lock (this.validationsLock)
        {
            var total = this.statisticsByConcept.Values.Sum(s => s.TotalDecisions);
            if (total == 0)
                return 0f;

            var optimal = this.statisticsByConcept.Values.Sum(s => s.OptimalDecisions);
            return (float)optimal / total;
        }
    }

    /// <summary>
    /// Gets the overall acceptable-or-better rate across all concepts.
    /// </summary>
    public float GetOverallAcceptableRate()
    {
        lock (this.validationsLock)
        {
            var total = this.statisticsByConcept.Values.Sum(s => s.TotalDecisions);
            if (total == 0)
                return 0f;

            var acceptableOrBetter = this.statisticsByConcept.Values.Sum(
                s => s.OptimalDecisions + s.AcceptableDecisions);
            return (float)acceptableOrBetter / total;
        }
    }

    /// <summary>
    /// Gets concepts with the lowest optimal decision rates.
    /// </summary>
    public IEnumerable<(string ConceptId, float Rate)> GetWeakestConcepts(int count = 5)
    {
        lock (this.validationsLock)
        {
            return this.statisticsByConcept
                .Where(kvp => kvp.Value.TotalDecisions >= 5) // Need enough data
                .OrderBy(kvp => kvp.Value.OptimalRate)
                .Take(count)
                .Select(kvp => (kvp.Key, kvp.Value.OptimalRate))
                .ToList();
        }
    }

    /// <summary>
    /// Gets concepts with the highest optimal decision rates.
    /// </summary>
    public IEnumerable<(string ConceptId, float Rate)> GetStrongestConcepts(int count = 5)
    {
        lock (this.validationsLock)
        {
            return this.statisticsByConcept
                .Where(kvp => kvp.Value.TotalDecisions >= 5) // Need enough data
                .OrderByDescending(kvp => kvp.Value.OptimalRate)
                .Take(count)
                .Select(kvp => (kvp.Key, kvp.Value.OptimalRate))
                .ToList();
        }
    }

    /// <summary>
    /// Clears all validation history and statistics.
    /// </summary>
    public void ClearAll()
    {
        lock (this.validationsLock)
        {
            this.recentValidations.Clear();
            this.statisticsByConcept.Clear();
        }
    }

    /// <summary>
    /// Clears validation history but keeps statistics.
    /// </summary>
    public void ClearHistory()
    {
        lock (this.validationsLock)
        {
            this.recentValidations.Clear();
        }
    }

    /// <summary>
    /// Resets statistics for a specific concept.
    /// </summary>
    public void ResetConceptStatistics(string conceptId)
    {
        lock (this.validationsLock)
        {
            if (this.statisticsByConcept.TryGetValue(conceptId, out var stats))
            {
                stats.Reset();
            }
        }
    }

    /// <summary>
    /// Gets summary statistics for display.
    /// </summary>
    public (int TotalDecisions, int Optimal, int Acceptable, int Suboptimal) GetSummary()
    {
        lock (this.validationsLock)
        {
            var total = this.statisticsByConcept.Values.Sum(s => s.TotalDecisions);
            var optimal = this.statisticsByConcept.Values.Sum(s => s.OptimalDecisions);
            var acceptable = this.statisticsByConcept.Values.Sum(s => s.AcceptableDecisions);
            var suboptimal = this.statisticsByConcept.Values.Sum(s => s.SuboptimalDecisions);
            return (total, optimal, acceptable, suboptimal);
        }
    }

    #region Personality-Aware Feedback (v3.51.0)

    /// <summary>
    /// Determines if feedback should be shown for a validation result based on personality.
    /// Silent personality suppresses non-suboptimal feedback.
    /// </summary>
    public bool ShouldShowFeedback(DecisionResult result)
    {
        return PersonalityTextGenerator.ShouldShowFeedback(config.CoachingPersonality, result.Outcome);
    }

    /// <summary>
    /// Gets a personality-appropriate feedback message for a validation result.
    /// Returns empty string for Silent personality on non-suboptimal results.
    /// </summary>
    public string GetFeedbackMessage(DecisionResult result)
    {
        var personality = config.CoachingPersonality;

        return result.Outcome switch
        {
            ValidationOutcome.Optimal => PersonalityTextGenerator.GetOptimalMessage(
                personality, result.ActualActionName),
            ValidationOutcome.Acceptable => PersonalityTextGenerator.GetAcceptableMessage(
                personality, result.ActualActionName, result.OptimalActionName ?? string.Empty),
            ValidationOutcome.Suboptimal => PersonalityTextGenerator.GetSuboptimalMessage(
                personality, result.ActualActionName, result.OptimalActionName ?? string.Empty, result.ShortExplanation),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Gets the current coaching personality setting.
    /// </summary>
    public CoachingPersonality CurrentPersonality => config.CoachingPersonality;

    #endregion
}
