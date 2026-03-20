using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Debuff;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles debuff cleansing with Esuna.
/// Uses priority-based debuff detection for lethal and high-priority debuffs.
/// </summary>
public sealed class EsunaHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Esuna;
    public string Name => "Esuna";

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        if (!context.InCombat) return false;

        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableEsuna)
        {
            context.Debug.EsunaState = "Disabled";
            return false;
        }

        if (player.Level < RoleActions.Esuna.MinLevel)
        {
            context.Debug.EsunaState = $"Level {player.Level} < {RoleActions.Esuna.MinLevel}";
            return false;
        }

        if (player.CurrentMp < RoleActions.Esuna.MpCost)
        {
            context.Debug.EsunaState = $"MP {player.CurrentMp} < {RoleActions.Esuna.MpCost}";
            return false;
        }

        var (target, statusId, priority) = FindBestEsunaTarget(context);
        if (target is null)
        {
            context.Debug.EsunaState = "No target";
            context.Debug.EsunaTarget = "None";
            return false;
        }

        if (priority != DebuffPriority.Lethal && (int)priority > config.RoleActions.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.RoleActions.EsunaPriorityThreshold}";
            return false;
        }

        if (isMoving)
        {
            context.Debug.EsunaState = "Moving";
            return false;
        }

        // Check if another Olympus instance is already cleansing this target
        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;
        if (partyCoord?.IsCleanseTargetReservedByOther(targetEntityId) == true)
        {
            context.Debug.EsunaState = "Reserved by other";
            return false;
        }

        // Reserve the target before executing
        if (partyCoord != null && !partyCoord.ReserveCleanseTarget(targetEntityId, statusId, RoleActions.Esuna.ActionId, (int)priority))
        {
            context.Debug.EsunaState = "Failed to reserve";
            return false;
        }

        var targetName = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaTarget = targetName;
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        if (ActionExecutor.ExecuteGcd(context, RoleActions.Esuna, target.GameObjectId,
            targetName, target.CurrentHp, "Esuna",
            appendThinAirNote: false))
        {
            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var priorityName = priority.ToString();
                var shortReason = priority == DebuffPriority.Lethal
                    ? $"Lethal debuff on {targetName}!"
                    : $"Cleansing {priorityName} debuff on {targetName}";

                var factors = new[]
                {
                    $"Target: {targetName}",
                    $"Debuff priority: {priorityName}",
                    $"Status ID: {statusId}",
                    priority == DebuffPriority.Lethal ? "LETHAL - must cleanse immediately!" : "Dispellable debuff detected",
                };

                var alternatives = priority == DebuffPriority.Lethal
                    ? new[] { "Nothing - lethal debuffs must be cleansed" }
                    : new[] { "Wait for debuff to expire", "Focus on healing instead", "Let co-healer handle it" };

                var tip = priority == DebuffPriority.Lethal
                    ? "Lethal debuffs kill if not cleansed! Always prioritize these over healing."
                    : "Look for the dispellable icon (white bar above debuff) to know what can be cleansed.";

                var explanationPriority = priority == DebuffPriority.Lethal
                    ? ExplanationPriority.Critical
                    : ExplanationPriority.High;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = RoleActions.Esuna.ActionId,
                    ActionName = "Esuna",
                    Category = "Utility",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Esuna cleanses dispellable debuffs from party members. Used on {targetName} to remove a {priorityName} priority debuff. {(priority == DebuffPriority.Lethal ? "This debuff would kill the target if not removed!" : "Removing debuffs improves party performance and safety.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.EsunaUsage,
                    Priority = explanationPriority,
                });
            }

            return true;
        }

        // Clear reservation if execution failed
        partyCoord?.ClearCleanseReservation(targetEntityId);
        return false;
    }

    private static (IBattleChara? target, uint statusId, DebuffPriority priority) FindBestEsunaTarget(IApolloContext context)
    {
        var player = context.Player;
        IBattleChara? bestTarget = null;
        uint bestStatusId = 0;
        var bestPriority = DebuffPriority.None;
        float bestRemainingTime = float.MaxValue;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (!DistanceHelper.IsInRange(player, member, RoleActions.Esuna.Range))
                continue;

            var (statusId, priority, remainingTime) = context.DebuffDetectionService.FindHighestPriorityDebuff(member);

            if (priority == DebuffPriority.None)
                continue;

            if (priority < bestPriority ||
                (priority == bestPriority && remainingTime < bestRemainingTime))
            {
                bestTarget = member;
                bestStatusId = statusId;
                bestPriority = priority;
                bestRemainingTime = remainingTime;
            }
        }

        return (bestTarget, bestStatusId, bestPriority);
    }
}
