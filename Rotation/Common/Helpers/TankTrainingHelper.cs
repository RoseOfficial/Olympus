using System;
using Olympus.Config;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in tank rotations.
/// Provides typed methods for mitigation, damage, and resource management.
/// </summary>
public static class TankTrainingHelper
{
    /// <summary>
    /// Records a mitigation decision explanation to the training service.
    /// </summary>
    public static void RecordMitigationDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        float selfHpPercent,
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
            Category = "Mitigation",
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
    /// Records an invulnerability decision explanation to the training service.
    /// </summary>
    public static void RecordInvulnDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        float selfHpPercent,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Critical)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Invulnerability",
            TargetName = null,
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
    /// Records a damage decision explanation to the training service.
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
    /// Records a burst window decision explanation to the training service.
    /// </summary>
    public static void RecordBurstDecision(
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
            Category = "Burst Window",
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
    /// Records a resource management decision (gauge spending).
    /// </summary>
    public static void RecordResourceDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        int gaugeValue,
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
            TargetName = null,
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
    /// Records a party mitigation decision (Divine Veil, Shake It Off, etc.).
    /// </summary>
    public static void RecordPartyMitigationDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
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
            Category = "Party Mitigation",
            TargetName = null,
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
    /// Records an enmity/aggro decision (Provoke, Shirk, etc.).
    /// </summary>
    public static void RecordEnmityDecision(
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
            Category = "Enmity",
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
    /// Records an interrupt decision (Interject, Low Blow).
    /// </summary>
    public static void RecordInterruptDecision(
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
            Category = "Interrupt",
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
