using System;
using Olympus.Config;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper methods to reduce boilerplate when recording training decisions.
/// Provides a unified API plus typed convenience methods for different decision categories.
/// </summary>
public static class TrainingHelper
{
    /// <summary>
    /// Creates a new fluent decision builder for recording training decisions.
    /// Use this as the entry point for the fluent API.
    /// </summary>
    /// <example>
    /// TrainingHelper.Decision(service)
    ///     .Action(actionId, actionName)
    ///     .AsMitigation(selfHpPercent)
    ///     .Reason("Using Vengeance", "Tankbuster mitigation")
    ///     .Factors("Tankbuster incoming", "HP below 70%")
    ///     .Tip("Use Vengeance early for full duration")
    ///     .Concept(WarConcepts.Vengeance)
    ///     .Record();
    /// </example>
    public static DecisionBuilder Decision(ITrainingService? service)
        => new DecisionBuilder(service);

    /// <summary>
    /// Core unified method for recording any training decision.
    /// All role-specific helpers delegate to this method.
    /// </summary>
    public static void RecordDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string category,
        string? targetName,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal,
        DecisionContext? context = null)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = category,
            TargetName = targetName,
            ShortReason = shortReason,
            DetailedReason = detailedReason,
            Factors = factors,
            Alternatives = alternatives,
            Tip = tip,
            ConceptId = conceptId,
            Priority = priority,
            Context = context,
        });
    }

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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.Healing, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { TargetHpPercent = targetHpPercent, HealAmount = healAmount });
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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.Defensive, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.Damage, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.Utility, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.Buff, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        RecordDecision(
            service, actionId, actionName, DecisionCategory.ResourceManagement, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
    }
}
