namespace Olympus.Services.Training;

/// <summary>
/// Coaching personality types that control the tone and style of feedback.
/// </summary>
public enum CoachingPersonality
{
    /// <summary>
    /// Positive reinforcement, gentle corrections, supportive tone.
    /// Default personality for most players.
    /// </summary>
    Encouraging,

    /// <summary>
    /// Data-focused, minimal emotion, just facts and numbers.
    /// For players who prefer objective feedback.
    /// </summary>
    Analytical,

    /// <summary>
    /// Direct corrections, high standards, no sugarcoating.
    /// For experienced players seeking improvement.
    /// </summary>
    Strict,

    /// <summary>
    /// Minimal feedback, only critical errors shown.
    /// For players who want minimal distraction.
    /// </summary>
    Silent,
}

/// <summary>
/// Generates coaching messages with personality-appropriate tone.
/// </summary>
public static class PersonalityTextGenerator
{
    /// <summary>
    /// Generates a positive message for optimal decisions.
    /// </summary>
    public static string GetOptimalMessage(CoachingPersonality personality, string actionName) => personality switch
    {
        CoachingPersonality.Encouraging => $"Nice! {actionName} was the right call.",
        CoachingPersonality.Analytical => $"{actionName}: Optimal decision.",
        CoachingPersonality.Strict => $"{actionName}: Correct.",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{actionName} was optimal.",
    };

    /// <summary>
    /// Generates a message for acceptable decisions.
    /// </summary>
    public static string GetAcceptableMessage(
        CoachingPersonality personality,
        string actionName,
        string optimalAction) => personality switch
    {
        CoachingPersonality.Encouraging => $"Good choice with {actionName}! {optimalAction} would have been slightly better.",
        CoachingPersonality.Analytical => $"{actionName}: Acceptable. Optimal: {optimalAction}.",
        CoachingPersonality.Strict => $"{actionName} works, but {optimalAction} was better.",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{actionName} acceptable; {optimalAction} preferred.",
    };

    /// <summary>
    /// Generates a message for suboptimal decisions.
    /// </summary>
    public static string GetSuboptimalMessage(
        CoachingPersonality personality,
        string actionName,
        string optimalAction,
        string? reason = null) => personality switch
    {
        CoachingPersonality.Encouraging => $"{actionName} wasn't ideal here. Try {optimalAction} next time!",
        CoachingPersonality.Analytical => $"{actionName}: Suboptimal. Should use: {optimalAction}.{(reason != null ? $" Reason: {reason}" : string.Empty)}",
        CoachingPersonality.Strict => $"Wrong. {optimalAction} was correct, not {actionName}.{(reason != null ? $" {reason}" : string.Empty)}",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{actionName} suboptimal; use {optimalAction}.",
    };

    /// <summary>
    /// Generates a coaching hint message.
    /// </summary>
    public static string GetHintMessage(
        CoachingPersonality personality,
        string conceptName,
        string tip) => personality switch
    {
        CoachingPersonality.Encouraging => $"Quick tip for {conceptName}: {tip}",
        CoachingPersonality.Analytical => $"[{conceptName}] {tip}",
        CoachingPersonality.Strict => $"{conceptName}: {tip}",
        CoachingPersonality.Silent => string.Empty,
        _ => tip,
    };

    /// <summary>
    /// Generates a recommended action message.
    /// </summary>
    public static string GetRecommendedActionMessage(
        CoachingPersonality personality,
        string action) => personality switch
    {
        CoachingPersonality.Encouraging => $"Try this: {action}",
        CoachingPersonality.Analytical => $"Recommended: {action}",
        CoachingPersonality.Strict => $"Do: {action}",
        CoachingPersonality.Silent => string.Empty,
        _ => action,
    };

    /// <summary>
    /// Generates a success celebration message.
    /// </summary>
    public static string GetSuccessMessage(
        CoachingPersonality personality,
        string conceptName) => personality switch
    {
        CoachingPersonality.Encouraging => $"Great job with {conceptName}!",
        CoachingPersonality.Analytical => $"{conceptName}: Success recorded.",
        CoachingPersonality.Strict => $"{conceptName}: Correct execution.",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{conceptName} executed successfully.",
    };

    /// <summary>
    /// Generates a struggle detection message.
    /// </summary>
    public static string GetStruggleMessage(
        CoachingPersonality personality,
        string conceptName,
        float successRate) => personality switch
    {
        CoachingPersonality.Encouraging => $"Keep practicing {conceptName} - you're at {successRate:P0} and improving!",
        CoachingPersonality.Analytical => $"{conceptName}: {successRate:P0} success rate. Below 60% threshold.",
        CoachingPersonality.Strict => $"{conceptName} needs work. Current: {successRate:P0}.",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{conceptName}: {successRate:P0}",
    };

    /// <summary>
    /// Generates a mastery achievement message.
    /// </summary>
    public static string GetMasteryMessage(
        CoachingPersonality personality,
        string conceptName) => personality switch
    {
        CoachingPersonality.Encouraging => $"You've mastered {conceptName}! Excellent work!",
        CoachingPersonality.Analytical => $"{conceptName}: Mastery achieved (>85% success rate).",
        CoachingPersonality.Strict => $"{conceptName}: Mastered.",
        CoachingPersonality.Silent => string.Empty,
        _ => $"{conceptName} mastered.",
    };

    /// <summary>
    /// Determines whether feedback should be shown based on personality and outcome.
    /// Silent personality suppresses non-critical feedback.
    /// </summary>
    public static bool ShouldShowFeedback(CoachingPersonality personality, ValidationOutcome outcome)
    {
        if (personality == CoachingPersonality.Silent)
        {
            // Silent only shows suboptimal decisions
            return outcome == ValidationOutcome.Suboptimal;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a hint should be shown based on personality and priority.
    /// </summary>
    public static bool ShouldShowHint(CoachingPersonality personality, HintPriority priority)
    {
        return personality switch
        {
            CoachingPersonality.Silent => priority >= HintPriority.Critical,
            CoachingPersonality.Strict => priority >= HintPriority.Normal,
            _ => true,
        };
    }
}
