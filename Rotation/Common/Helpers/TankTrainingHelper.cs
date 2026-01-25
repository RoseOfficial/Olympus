using Olympus.Config;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in tank rotations.
/// Thin wrappers over TrainingHelper.RecordDecision for tank-specific categories.
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Mitigation, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { SelfHpPercent = selfHpPercent });
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Invulnerability, null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { SelfHpPercent = selfHpPercent });
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Damage, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.BurstWindow, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.ResourceManagement, null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { GaugeValue = gaugeValue });
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.PartyMitigation, null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Enmity, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Interrupt, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
    }
}
