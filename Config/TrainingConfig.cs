namespace Olympus.Config;

using System;
using System.Collections.Generic;

/// <summary>
/// Configuration for Training Mode - intelligent coaching and decision explanations.
/// </summary>
public sealed class TrainingConfig
{
    /// <summary>
    /// Master toggle for training mode. When enabled, captures decision explanations.
    /// </summary>
    public bool EnableTraining { get; set; } = false;

    /// <summary>
    /// Whether the training window is currently visible.
    /// </summary>
    public bool TrainingWindowVisible { get; set; } = false;

    /// <summary>
    /// Maximum number of explanations to keep in memory (oldest are discarded).
    /// </summary>
    private int _maxExplanationsToShow = 15;
    public int MaxExplanationsToShow
    {
        get => _maxExplanationsToShow;
        set => _maxExplanationsToShow = Math.Clamp(value, 5, 50);
    }

    /// <summary>
    /// Show alternative actions that were considered but not chosen.
    /// </summary>
    public bool ShowAlternatives { get; set; } = true;

    /// <summary>
    /// Show learning tips with explanations.
    /// </summary>
    public bool ShowTips { get; set; } = true;

    /// <summary>
    /// How detailed explanations should be.
    /// </summary>
    public ExplanationVerbosity Verbosity { get; set; } = ExplanationVerbosity.Normal;

    /// <summary>
    /// Filter explanations by minimum priority level.
    /// </summary>
    public ExplanationPriority MinimumPriorityToShow { get; set; } = ExplanationPriority.Low;

    /// <summary>
    /// Concepts the user has marked as "learned" (shown checkmark in UI).
    /// </summary>
    public HashSet<string> LearnedConcepts { get; set; } = new();

    /// <summary>
    /// Lessons the user has marked as "completed" (shown in Lessons tab).
    /// </summary>
    public HashSet<string> CompletedLessons { get; set; } = new();

    /// <summary>
    /// How many times each concept has been shown to the user.
    /// Used to identify concepts that may need more attention.
    /// </summary>
    public Dictionary<string, int> ConceptExposureCount { get; set; } = new();

    /// <summary>
    /// Section visibility toggles for the training window.
    /// </summary>
    public Dictionary<string, bool> SectionVisibility { get; set; } = new()
    {
        { "CurrentAction", true },
        { "DecisionFactors", true },
        { "Alternatives", true },
        { "Tips", true },
        { "RecentHistory", true },
    };
}

/// <summary>
/// Controls how detailed the explanations are.
/// </summary>
public enum ExplanationVerbosity
{
    /// <summary>
    /// Brief explanations - just the essential info.
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard explanations with key factors.
    /// </summary>
    Normal,

    /// <summary>
    /// Detailed explanations with all decision factors and numbers.
    /// </summary>
    Detailed,
}

/// <summary>
/// How important an explanation is to show to the user.
/// </summary>
public enum ExplanationPriority
{
    /// <summary>
    /// Routine actions (basic DPS, simple heals).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Standard priority actions (most healing decisions).
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Important decisions (cooldown usage, emergency heals).
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical decisions (emergency response, life-saving actions).
    /// </summary>
    Critical = 3,
}
