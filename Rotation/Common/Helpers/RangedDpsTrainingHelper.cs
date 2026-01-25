using System;
using Olympus.Config;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in ranged physical DPS rotations.
/// Provides typed methods for damage, burst windows, procs, and resource management.
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
        ExplanationPriority priority = ExplanationPriority.High)
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
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = $"Proc ({procName})",
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
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "Raid Buff",
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
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = $"Song ({currentSong})",
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
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = "DoT Management",
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
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = $"AoE ({enemyCount} targets)",
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
}
