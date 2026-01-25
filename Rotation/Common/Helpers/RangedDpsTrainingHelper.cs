using Olympus.Config;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in ranged physical DPS rotations.
/// Thin wrappers over TrainingHelper.RecordDecision for ranged-specific categories.
/// </summary>
public static class RangedDpsTrainingHelper
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
    /// Records a proc usage decision (Straight Shot Ready, Verfire, etc.).
    /// </summary>
    public static void RecordProcDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string procName,
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
            service, actionId, actionName, DecisionCategory.Proc(procName), targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { ProcName = procName });
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
    /// Records a raid buff decision (Battle Voice, Radiant Finale, etc.).
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
    /// Records a song/dance decision (BRD songs, DNC dances).
    /// </summary>
    public static void RecordSongDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string currentSong,
        float songRemaining,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.Song(currentSong), null,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { CurrentSong = currentSong, SongRemaining = songRemaining });
    }

    /// <summary>
    /// Records a DoT management decision.
    /// </summary>
    public static void RecordDotDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string? targetName,
        float dotRemaining,
        string shortReason,
        string detailedReason,
        string[] factors,
        string[] alternatives,
        string tip,
        string conceptId,
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        TrainingHelper.RecordDecision(
            service, actionId, actionName, DecisionCategory.DotManagement, targetName,
            shortReason, detailedReason, factors, alternatives, tip, conceptId, priority,
            new DecisionContext { DotRemaining = dotRemaining });
    }

    /// <summary>
    /// Records a utility decision (Head Graze, Peloton, etc.).
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
