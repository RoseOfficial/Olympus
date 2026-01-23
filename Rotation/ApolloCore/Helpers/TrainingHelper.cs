using System;
using Olympus.Config;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper methods to reduce boilerplate when recording training decisions.
/// Provides typed methods for different decision categories.
/// </summary>
public static class TrainingHelper
{
    /// <summary>
    /// Records a healing decision explanation to the training service.
    /// </summary>
    public static void RecordHealDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        float targetHpPercent,
        int healAmount,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Healing",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }

    /// <summary>
    /// Records a defensive cooldown decision explanation to the training service.
    /// </summary>
    public static void RecordDefensiveDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.High)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Defensive",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }

    /// <summary>
    /// Records a damage/DPS decision explanation to the training service.
    /// </summary>
    public static void RecordDamageDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Low)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Damage",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }

    /// <summary>
    /// Records a utility action decision explanation to the training service.
    /// </summary>
    public static void RecordUtilityDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Utility",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }

    /// <summary>
    /// Records a buff/oGCD weaving decision explanation to the training service.
    /// </summary>
    public static void RecordBuffDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Buff",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }

    /// <summary>
    /// Records a resource management decision explanation (MP, gauge, charges).
    /// </summary>
    public static void RecordResourceDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Resource Management",
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
        });
    }
}
