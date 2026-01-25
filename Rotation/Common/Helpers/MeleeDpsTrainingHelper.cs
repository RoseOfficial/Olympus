using Olympus.Config;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in melee DPS rotations.
/// Thin wrappers over TrainingHelper.RecordDecision for melee-specific categories.
/// </summary>
public static class MeleeDpsTrainingHelper
{
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
        ExplanationPriority priority = ExplanationPriority.High)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.BurstWindow, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
    }

    /// <summary>
    /// Records a positional decision explanation to the training service.
    /// </summary>
    public static void RecordPositionalDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        bool hitPositional,
        string position,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Positional(hitPositional), targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { HitPositional = hitPositional, Position = position });
    }

    /// <summary>
    /// Records a combo decision explanation to the training service.
    /// </summary>
    public static void RecordComboDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        int comboStep,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Low)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Combo(comboStep), targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { ComboStep = comboStep });
    }

    /// <summary>
    /// Records a resource management decision (gauge spending).
    /// </summary>
    public static void RecordResourceDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string resourceName,
        int resourceValue,
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
            new DecisionContext { ResourceName = resourceName, ResourceValue = resourceValue });
    }

    /// <summary>
    /// Records a raid buff decision (Battle Litany, Brotherhood, etc.).
    /// </summary>
    public static void RecordRaidBuffDecision(
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
            service, actionId, actionName, DecisionCategory.RaidBuff, null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
    }

    /// <summary>
    /// Records a defensive/utility decision (True North, Feint, etc.).
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
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Utility, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority);
    }

    /// <summary>
    /// Records an AoE rotation decision.
    /// </summary>
    public static void RecordAoeDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        int enemyCount,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Low)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.AoE(enemyCount), null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { EnemyCount = enemyCount });
    }
}
