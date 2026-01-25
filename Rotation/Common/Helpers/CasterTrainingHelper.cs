using System;
using Olympus.Config;
using Olympus.Services.Training;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helper methods for recording training decisions in caster DPS rotations.
/// Provides typed methods for damage, burst windows, procs, and resource management.
/// </summary>
public static class CasterTrainingHelper
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
    /// Records a proc usage decision (Firestarter, Thunderhead, etc.).
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
    /// Records a resource management decision (MP, gauge spending).
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
    /// Records a raid buff decision (Searing Light, Embolden, etc.).
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
    /// Records a phase/element transition decision (Astral Fire/Umbral Ice).
    /// </summary>
    public static void RecordPhaseDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string currentPhase,
        string nextPhase,
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
            Category = $"Phase ({currentPhase} → {nextPhase})",
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
    /// Records a movement optimization decision (Swiftcast, Triplecast, slide casting).
    /// </summary>
    public static void RecordMovementDecision(
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
            Category = "Movement Optimization",
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
    /// Records a summon/pet decision (SMN primals, demi-summons).
    /// </summary>
    public static void RecordSummonDecision(
        ITrainingService? service,
        uint actionId,
        string actionName,
        string summonName,
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
            Category = $"Summon ({summonName})",
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

    /// <summary>
    /// Records a melee combo decision (RDM melee phase).
    /// </summary>
    public static void RecordMeleeComboDecision(
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
        ExplanationPriority priority = ExplanationPriority.Normal)
    {
        if (service?.IsTrainingEnabled != true)
            return;

        service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = actionId,
            ActionName = actionName,
            Category = $"Melee Combo Step {comboStep}",
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
